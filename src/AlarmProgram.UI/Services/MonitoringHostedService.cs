using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Alerts;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmProgram.UI.Services;

public sealed class MonitoringHostedService : BackgroundService, IMonitoringController
{
    private readonly AlertOrchestrator _orchestrator;
    private readonly IEventCollector _collector;
    private readonly IEventClassifier _classifier;
    private readonly ISettingsStore _settingsStore;
    private readonly MonitoringOptions _monitoringOptions;
    private readonly ILogger<MonitoringHostedService> _logger;
    private readonly object _stateLock = new();
    private bool _isPaused;
    private bool _isRunning;
    private DateTimeOffset? _lastHeartbeatAt;

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

    public bool IsRunning
    {
        get
        {
            lock (_stateLock)
            {
                return _isRunning;
            }
        }
    }

    public bool IsPaused
    {
        get
        {
            lock (_stateLock)
            {
                return _isPaused;
            }
        }
    }

    public string StatusText
    {
        get
        {
            lock (_stateLock)
            {
                if (!_isRunning)
                {
                    return "Мониторинг останавливается…";
                }

                return _isPaused
                    ? "Мониторинг на паузе"
                    : "Мониторинг включен";
            }
        }
    }

    public event EventHandler? StatusChanged;

    public void Pause()
    {
        lock (_stateLock)
        {
            _isPaused = true;
        }

        RaiseStatusChanged();
        _logger.LogInformation("Мониторинг поставлен на паузу пользователем");
    }

    public void Resume()
    {
        lock (_stateLock)
        {
            _isPaused = false;
        }

        RaiseStatusChanged();
        _logger.LogInformation("Мониторинг возобновлен пользователем");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = ResolvePollInterval(_monitoringOptions.PollIntervalSeconds);
        var startupAt = DateTimeOffset.UtcNow;
        var since = startupAt - ResolveInitialLookback(_monitoringOptions.InitialLookbackMinutes);

        SetRunning(true);
        await TryRecoverPreviousShutdownAsync(startupAt, stoppingToken);
        _logger.LogInformation(
            "Фоновый мониторинг событий запущен. Интервал: {PollIntervalSeconds} сек, начальный lookback: {LookbackMinutes} мин",
            (int)pollInterval.TotalSeconds,
            _monitoringOptions.InitialLookbackMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!IsPaused)
                {
                    await _orchestrator.ProcessAsync(since, stoppingToken);
                    since = DateTimeOffset.UtcNow - pollInterval;
                    await TrySendHeartbeatAsync(stoppingToken);
                }
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

        SetRunning(false);
        _logger.LogInformation("Фоновый мониторинг событий остановлен");
    }

    private async Task TrySendHeartbeatAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken);
        if (!settings.HeartbeatEnabled || !settings.IsValid || !settings.HasEnabledChannel)
        {
            return;
        }

        var intervalMinutes = settings.HeartbeatIntervalMinutes < 5
            ? _monitoringOptions.DefaultHeartbeatIntervalMinutes
            : settings.HeartbeatIntervalMinutes;
        var now = DateTimeOffset.UtcNow;

        if (_lastHeartbeatAt is not null
            && now - _lastHeartbeatAt < TimeSpan.FromMinutes(intervalMinutes))
        {
            return;
        }

        if (settings.IsWithinQuietHours(now))
        {
            return;
        }

        var heartbeat = new MachineEvent
        {
            Type = MachineEventType.Heartbeat,
            OccurredAt = now,
            Source = "AlarmProgram",
            EventId = null,
            HostName = Environment.MachineName,
            Message = $"Периодический heartbeat каждые {intervalMinutes} мин."
        };

        await _orchestrator.ProcessMachineEventAsync(heartbeat, settings, cancellationToken);
        _lastHeartbeatAt = now;
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

    private void SetRunning(bool isRunning)
    {
        lock (_stateLock)
        {
            _isRunning = isRunning;
        }

        RaiseStatusChanged();
    }

    private void RaiseStatusChanged() => StatusChanged?.Invoke(this, EventArgs.Empty);

    private static bool IsShutdownRelated(MachineEventType type) =>
        type is MachineEventType.Shutdown or MachineEventType.Restart or MachineEventType.UnexpectedShutdown;

    private static TimeSpan ResolvePollInterval(int pollIntervalSeconds) =>
        TimeSpan.FromSeconds(pollIntervalSeconds < 5 ? 30 : Math.Min(pollIntervalSeconds, 300));

    private static TimeSpan ResolveInitialLookback(int initialLookbackMinutes) =>
        TimeSpan.FromMinutes(initialLookbackMinutes < 1 ? 10 : Math.Min(initialLookbackMinutes, 1440));

    private static TimeSpan ResolveRecoveryLookback(int recoveryLookbackHours) =>
        TimeSpan.FromHours(recoveryLookbackHours < 1 ? 24 : Math.Min(recoveryLookbackHours, 168));
}
