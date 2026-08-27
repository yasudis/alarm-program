using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Application.Contracts;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmProgram.Application.Alerts;

public sealed class AlertOrchestrator
{
    private readonly IEventCollector _collector;
    private readonly IEventClassifier _classifier;
    private readonly AlertFilter _filter;
    private readonly IAlertFormatter _formatter;
    private readonly IReadOnlyList<INotificationChannel> _channels;
    private readonly ISettingsStore _settingsStore;
    private readonly IAlertJournal _alertJournal;
    private readonly ILogger<AlertOrchestrator> _logger;
    private readonly TimeSpan _deduplicationWindow;
    private readonly object _deduplicationLock = new();
    private readonly Dictionary<string, DateTimeOffset> _seenEvents = new(StringComparer.Ordinal);

    public AlertOrchestrator(
        IEventCollector collector,
        IEventClassifier classifier,
        AlertFilter filter,
        IAlertFormatter formatter,
        IEnumerable<INotificationChannel> channels,
        ISettingsStore settingsStore,
        IAlertJournal alertJournal,
        IOptions<MonitoringOptions> monitoringOptions,
        ILogger<AlertOrchestrator> logger)
    {
        _collector = collector;
        _classifier = classifier;
        _filter = filter;
        _formatter = formatter;
        _channels = channels.ToArray();
        _settingsStore = settingsStore;
        _alertJournal = alertJournal;
        _deduplicationWindow = ResolveDeduplicationWindow(monitoringOptions.Value.DeduplicationWindowSeconds);
        _logger = logger;
    }

    public async Task ProcessAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken);
        var rawEvents = await _collector.CollectAsync(since, cancellationToken);

        _logger.LogInformation(
            "Обработка {Count} сырых событий начиная с {Since}",
            rawEvents.Count,
            since);

        foreach (var rawEvent in rawEvents.OrderBy(item => item.OccurredAt))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessRawEventAsync(rawEvent, settings, cancellationToken);
        }
    }

    public async Task ProcessMachineEventAsync(
        MachineEvent machineEvent,
        UserSettings? settings = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(machineEvent);
        settings ??= await _settingsStore.LoadAsync(cancellationToken);
        await ProcessClassifiedEventAsync(machineEvent, settings, cancellationToken);
    }

    public async Task ProcessRawEventAsync(
        RawSystemEvent rawEvent,
        UserSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rawEvent);
        ArgumentNullException.ThrowIfNull(settings);

        MachineEvent? machineEvent;
        try
        {
            machineEvent = _classifier.Classify(rawEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Ошибка классификации события {EventId} от {Source}",
                rawEvent.EventId,
                rawEvent.Source);
            return;
        }

        if (machineEvent is null)
        {
            _logger.LogDebug(
                "Событие {EventId} от {Source} не классифицировано",
                rawEvent.EventId,
                rawEvent.Source);
            return;
        }

        await ProcessClassifiedEventAsync(machineEvent, settings, cancellationToken);
    }

    private async Task ProcessClassifiedEventAsync(
        MachineEvent machineEvent,
        UserSettings settings,
        CancellationToken cancellationToken)
    {
        if (IsDuplicate(machineEvent))
        {
            _logger.LogDebug(
                "Событие {EventType} на {HostName} пропущено как дубликат",
                machineEvent.Type,
                machineEvent.HostName);
            return;
        }

        if (!_filter.ShouldNotify(machineEvent, settings))
        {
            _logger.LogInformation(
                "Событие {EventType} на {HostName} отфильтровано (quiet hours/settings)",
                machineEvent.Type,
                machineEvent.HostName);
            return;
        }

        AlertMessage message;
        try
        {
            message = _formatter.Format(machineEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Ошибка форматирования события {EventType} на {HostName}",
                machineEvent.Type,
                machineEvent.HostName);
            return;
        }

        if (_channels.Count == 0)
        {
            _logger.LogWarning(
                "Нет зарегистрированных каналов уведомлений. CorrelationId={CorrelationId}",
                message.CorrelationId);
            return;
        }

        foreach (var channel in _channels)
        {
            try
            {
                await channel.SendAsync(message, cancellationToken);
                _logger.LogInformation(
                    "Алерт {EventType} отправлен в {Channel} для {HostName}. CorrelationId={CorrelationId}",
                    message.EventType,
                    channel.Name,
                    message.HostName,
                    message.CorrelationId);

                await _alertJournal.AppendAsync(
                    new AlertJournalEntry
                    {
                        Timestamp = DateTimeOffset.UtcNow,
                        EventType = message.EventType,
                        Subject = message.Subject,
                        Status = "Sent",
                        Channel = channel.Name,
                        HostName = message.HostName,
                        CorrelationId = message.CorrelationId
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Ошибка отправки алерта {EventType} в {Channel} для {HostName}. CorrelationId={CorrelationId}. ErrorType={ErrorType}",
                    message.EventType,
                    channel.Name,
                    message.HostName,
                    message.CorrelationId,
                    ex.GetType().Name);

                await _alertJournal.AppendAsync(
                    new AlertJournalEntry
                    {
                        Timestamp = DateTimeOffset.UtcNow,
                        EventType = message.EventType,
                        Subject = message.Subject,
                        Status = "Failed",
                        Channel = channel.Name,
                        HostName = message.HostName,
                        CorrelationId = message.CorrelationId,
                        Details = ex.Message
                    },
                    cancellationToken);
            }
        }
    }

    private bool IsDuplicate(MachineEvent machineEvent)
    {
        var now = DateTimeOffset.UtcNow;
        var occurredAt = machineEvent.OccurredAt.ToUniversalTime().ToString("O");
        var key = $"{machineEvent.Type}|{machineEvent.EventId}|{machineEvent.HostName}|{machineEvent.Source}|{occurredAt}";

        lock (_deduplicationLock)
        {
            PurgeExpired(now);
            if (_seenEvents.ContainsKey(key))
            {
                return true;
            }

            _seenEvents[key] = now;
            return false;
        }
    }

    private void PurgeExpired(DateTimeOffset now)
    {
        if (_seenEvents.Count == 0)
        {
            return;
        }

        var expireBefore = now - _deduplicationWindow;
        var expiredKeys = _seenEvents
            .Where(item => item.Value <= expireBefore)
            .Select(item => item.Key)
            .ToArray();

        foreach (var key in expiredKeys)
        {
            _seenEvents.Remove(key);
        }
    }

    private static TimeSpan ResolveDeduplicationWindow(int deduplicationWindowSeconds)
    {
        if (deduplicationWindowSeconds < 1)
        {
            return TimeSpan.FromSeconds(180);
        }

        return TimeSpan.FromSeconds(deduplicationWindowSeconds);
    }
}
