using System.Text.Json;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmProgram.Infrastructure.Outbox;

public sealed class FileAlertOutbox : IAlertOutbox
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<FileAlertOutbox> _logger;
    private readonly string _filePath;
    private readonly int _maxItems;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileAlertOutbox(
        IOptions<AlertOutboxOptions> options,
        ILogger<FileAlertOutbox> logger)
    {
        _logger = logger;
        _filePath = ResolveFilePath(options.Value.FilePath);
        _maxItems = options.Value.MaxItems < 10 ? 200 : Math.Min(options.Value.MaxItems, 1000);
    }

    public async Task EnqueueAsync(
        AlertMessage message,
        string channelName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(channelName);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await ReadAllAsync(cancellationToken);
            items.Insert(
                0,
                new OutboxItem
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Message = message,
                    ChannelName = channelName,
                    CreatedAt = DateTimeOffset.UtcNow,
                    AttemptCount = 0
                });

            if (items.Count > _maxItems)
            {
                items = items.Take(_maxItems).ToList();
            }

            await WriteAllAsync(items, cancellationToken);
            _logger.LogInformation(
                "Алерт поставлен в outbox: Channel={Channel}, CorrelationId={CorrelationId}",
                channelName,
                message.CorrelationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<OutboxItem>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await ReadAllAsync(cancellationToken);
            return items
                .OrderBy(item => item.CreatedAt)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await ReadAllAsync(cancellationToken);
            var remaining = items.Where(item => !string.Equals(item.Id, id, StringComparison.Ordinal)).ToList();
            await WriteAllAsync(remaining, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpdateAttemptAsync(
        string id,
        string? error,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await ReadAllAsync(cancellationToken);
            var item = items.FirstOrDefault(entry => string.Equals(entry.Id, id, StringComparison.Ordinal));
            if (item is null)
            {
                return;
            }

            item.AttemptCount += 1;
            item.LastError = error;
            await WriteAllAsync(items, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task WriteAllAsync(List<OutboxItem> items, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(items, JsonOptions);
        var tempPath = _filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, _filePath, overwrite: true);
    }

    private async Task<List<OutboxItem>> ReadAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_filePath);
        var items = await JsonSerializer.DeserializeAsync<List<OutboxItem>>(stream, JsonOptions, cancellationToken);
        return items ?? [];
    }

    private static string ResolveFilePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "%AppData%/AlarmProgram/alert-outbox.json"
            : configuredPath;

        path = path.Replace(
            "%AppData%",
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            StringComparison.OrdinalIgnoreCase);
        return Path.GetFullPath(path);
    }
}
