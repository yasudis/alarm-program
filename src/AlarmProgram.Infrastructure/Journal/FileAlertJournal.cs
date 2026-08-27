using System.Text.Json;
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
        PropertyNameCaseInsensitive = true
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
