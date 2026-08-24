using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Contracts;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Application.Alerts;

public sealed class AlertOrchestrator
{
    private readonly IEventCollector _collector;
    private readonly IEventClassifier _classifier;
    private readonly AlertFilter _filter;
    private readonly IAlertFormatter _formatter;
    private readonly IReadOnlyList<INotificationChannel> _channels;
    private readonly ISettingsStore _settingsStore;
    private readonly ILogger<AlertOrchestrator> _logger;

    public AlertOrchestrator(
        IEventCollector collector,
        IEventClassifier classifier,
        AlertFilter filter,
        IAlertFormatter formatter,
        IEnumerable<INotificationChannel> channels,
        ISettingsStore settingsStore,
        ILogger<AlertOrchestrator> logger)
    {
        _collector = collector;
        _classifier = classifier;
        _filter = filter;
        _formatter = formatter;
        _channels = channels.ToArray();
        _settingsStore = settingsStore;
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

        if (!_filter.ShouldNotify(machineEvent, settings))
        {
            _logger.LogInformation(
                "Событие {EventType} на {HostName} отфильтровано",
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
            _logger.LogWarning("Нет зарегистрированных каналов уведомлений");
            return;
        }

        foreach (var channel in _channels)
        {
            try
            {
                await channel.SendAsync(message, cancellationToken);
                _logger.LogInformation(
                    "Алерт {EventType} отправлен в {Channel} для {HostName}",
                    message.EventType,
                    channel.Name,
                    message.HostName);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Ошибка отправки алерта {EventType} в {Channel} для {HostName}",
                    message.EventType,
                    channel.Name,
                    message.HostName);
            }
        }
    }
}
