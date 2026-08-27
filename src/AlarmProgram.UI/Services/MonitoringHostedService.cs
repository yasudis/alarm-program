using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Alerts;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmProgram.UI.Services;

public sealed class MonitoringHostedService : BackgroundService
{
    private readonly AlertOrchestrator _orchestrator;
    private readonly IEventCollector _collector;
    private readonly IEventClassifier _classifier;
    private readonly ISettingsStore _settingsStore;
    private readonly MonitoringOptions _monitoringOptions;
    private readonly ILogger<MonitoringHostedService> _logger;

    public MonitoringHostedService(
        AlertOrchestrator orchestrator,
        IEventCollector collector,
        IEventClassifier classifier,
        ISettingsStore settingsStore,
        IOptions<MonitoringOptions> monitoringOptions,
        ILogger<MonitoringHostedService> logger)
    {
        _orchestrator = orchestrator;
        _collector = collector;
        _classifier = classifier;
        _settingsStore = settingsStore;
        _monitoringOptions = monitoringOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = ResolvePollInterval(_monitoringOptions.PollIntervalSeconds);
        var startupAt = DateTimeOffset.UtcNow;
        var since = startupAt - ResolveInitialLookback(_monitoringOptions.InitialLookbackMinutes);

        await TryRecoverPreviousShutdownAsync(startupAt, stoppingToken);
        _logger.LogInformation(
            "Фоновый мониторинг событий запущен. Интервал: {PollIntervalSeconds} сек, начальный lookback: {LookbackMinutes} мин",
            (int)pollInterval.TotalSeconds,
            _monitoringOptions.InitialLookbackMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _orchestrator.ProcessAsync(since, stoppingToken);
                since = DateTimeOffset.UtcNow - pollInterval;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка фонового мониторинга системных событий");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Фоновый мониторинг событий остановлен");
    }

    private async Task TryRecoverPreviousShutdownAsync(
        DateTimeOffset startupAt,
        CancellationToken cancellationToken)
    {
        var lookback = ResolveRecoveryLookback(_monitoringOptions.RecoveryLookbackHours);
        var since = startupAt - lookback;
        var rawEvents = await _collector.CollectAsync(since, cancellationToken);
        var settings = await _settingsStore.LoadAsync(cancellationToken);

        var previousRawEvent = rawEvents
            .OrderByDescending(item => item.OccurredAt)
            .FirstOrDefault(item =>
            {
                var machineEvent = _classifier.Classify(item);
                return machineEvent is not null
                       && item.OccurredAt < startupAt
                       && IsShutdownRelated(machineEvent.Type);
            });

        if (previousRawEvent is null)
        {
            _logger.LogInformation("Предыдущее shutdown/restart событие не найдено");
            return;
        }

        _logger.LogInformation(
            "Восстановлено предыдущее событие перед стартом: EventId={EventId}, At={OccurredAt}",
            previousRawEvent.EventId,
            previousRawEvent.OccurredAt);
        await _orchestrator.ProcessRawEventAsync(previousRawEvent, settings, cancellationToken);
    }

    private static bool IsShutdownRelated(MachineEventType type) =>
        type is MachineEventType.Shutdown or MachineEventType.Restart or MachineEventType.UnexpectedShutdown;

    private static TimeSpan ResolvePollInterval(int pollIntervalSeconds) =>
        TimeSpan.FromSeconds(pollIntervalSeconds < 5 ? 30 : Math.Min(pollIntervalSeconds, 300));

    private static TimeSpan ResolveInitialLookback(int initialLookbackMinutes) =>
        TimeSpan.FromMinutes(initialLookbackMinutes < 1 ? 10 : Math.Min(initialLookbackMinutes, 1440));

    private static TimeSpan ResolveRecoveryLookback(int recoveryLookbackHours) =>
        TimeSpan.FromHours(recoveryLookbackHours < 1 ? 24 : Math.Min(recoveryLookbackHours, 168));
}
