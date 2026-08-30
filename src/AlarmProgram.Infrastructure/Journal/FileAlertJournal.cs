using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmProgram.Infrastructure.Journal;

public sealed class FileAlertJournal : IAlertJournal
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ILogger<FileAlertJournal> _logger;
    private readonly string _filePath;
    private readonly int _maxEntries;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileAlertJournal(
        IOptions<AlertJournalOptions> options,
        ILogger<FileAlertJournal> logger)
    {
        _logger = logger;
        _filePath = ResolveFilePath(options.Value.FilePath);
        _maxEntries = options.Value.MaxEntries < 10 ? 100 : Math.Min(options.Value.MaxEntries, 1000);
    }

    public async Task AppendAsync(AlertJournalEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var entries = await ReadAllAsync(cancellationToken);
            entries.Insert(0, entry);
            if (entries.Count > _maxEntries)
            {
                entries = entries.Take(_maxEntries).ToList();
            }

            await WriteAllAsync(entries, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось записать запись журнала алертов");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<AlertJournalEntry>> GetRecentAsync(
        int maxCount = 50,
        CancellationToken cancellationToken = default)
    {
        if (maxCount < 1)
        {
            maxCount = 1;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var entries = await ReadAllAsync(cancellationToken);
            return entries.Take(Math.Min(maxCount, _maxEntries)).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ExportCsvAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var entries = await ReadAllAsync(cancellationToken);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var builder = new StringBuilder();
            builder.AppendLine("Timestamp,EventType,Status,Channel,Subject,HostName,CorrelationId,Details");
            foreach (var entry in entries)
            {
                builder.Append(Escape(entry.Timestamp.ToString("O"))).Append(',');
                builder.Append(Escape(entry.EventType.ToString())).Append(',');
                builder.Append(Escape(entry.Status)).Append(',');
                builder.Append(Escape(entry.Channel)).Append(',');
                builder.Append(Escape(entry.Subject)).Append(',');
                builder.Append(Escape(entry.HostName)).Append(',');
                builder.Append(Escape(entry.CorrelationId)).Append(',');
                builder.AppendLine(Escape(entry.Details));
            }

            await File.WriteAllTextAsync(filePath, builder.ToString(), Encoding.UTF8, cancellationToken);
            _logger.LogInformation("Журнал алертов экспортирован в {Path}", filePath);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ExportJsonAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var entries = await ReadAllAsync(cancellationToken);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(entries, JsonOptions);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8, cancellationToken);
            _logger.LogInformation("Журнал алертов экспортирован в JSON: {Path}", filePath);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await WriteAllAsync([], cancellationToken);
            _logger.LogInformation("Журнал алертов очищен: {Path}", _filePath);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> PurgeOlderThanAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        if (maxAge <= TimeSpan.Zero)
        {
            return 0;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var cutoff = DateTimeOffset.UtcNow - maxAge;
            var entries = await ReadAllAsync(cancellationToken);
            var kept = entries.Where(entry => entry.Timestamp >= cutoff).ToList();
            var removed = entries.Count - kept.Count;
            if (removed == 0)
            {
                return 0;
            }

            await WriteAllAsync(kept, cancellationToken);
            _logger.LogInformation(
                "Автоочистка журнала: удалено {Removed} записей старше {Cutoff:O}",
                removed,
                cutoff);
            return removed;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task WriteAllAsync(List<AlertJournalEntry> entries, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(entries, JsonOptions);
        var tempPath = _filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, _filePath, overwrite: true);
    }

    private async Task<List<AlertJournalEntry>> ReadAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_filePath);
        var entries = await JsonSerializer.DeserializeAsync<List<AlertJournalEntry>>(
            stream,
            JsonOptions,
            cancellationToken);
        return entries ?? [];
    }

    private static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        return text;
    }

    private static string ResolveFilePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "%AppData%/AlarmProgram/alert-journal.json"
            : configuredPath;

        path = path.Replace(
            "%AppData%",
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            StringComparison.OrdinalIgnoreCase);
        return Path.GetFullPath(path);
    }
}
