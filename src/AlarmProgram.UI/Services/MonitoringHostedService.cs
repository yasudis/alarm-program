using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Alerts;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Application.Health;
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
    private readonly IAlertJournal _alertJournal;
    private readonly INetworkMonitor _networkMonitor;
    private readonly IPowerEventMonitor _powerEventMonitor;
    private readonly ISessionMonitor _sessionMonitor;
    private readonly IDiskSpaceMonitor _diskSpaceMonitor;
    private readonly IProcessWatchdog _processWatchdog;
    private readonly IServiceWatchdog _serviceWatchdog;
    private readonly IUsbDeviceMonitor _usbDeviceMonitor;
    private readonly IResourceMonitor _resourceMonitor;
    private readonly IPendingRebootMonitor _pendingRebootMonitor;
    private readonly IHostWatchdog _hostWatchdog;
    private readonly IHttpEndpointWatchdog _httpEndpointWatchdog;
    private readonly ISystemSnapshotProvider _systemSnapshotProvider;
    private readonly IAlertMuteState _muteState;
    private readonly IWindowsEventLogWriter _windowsEventLogWriter;
    private readonly MonitoringOptions _monitoringOptions;
    private readonly ILogger<MonitoringHostedService> _logger;
    private readonly object _stateLock = new();
    private bool _isPaused;
    private bool _isRunning;
    private DateTimeOffset? _lastHeartbeatAt;
    private DateOnly? _lastDailyDigestLocalDate;

    public MonitoringHostedService(
        AlertOrchestrator orchestrator,
        IEventCollector collector,
        IEventClassifier classifier,
        ISettingsStore settingsStore,
        IAlertJournal alertJournal,
        INetworkMonitor networkMonitor,
        IPowerEventMonitor powerEventMonitor,
        ISessionMonitor sessionMonitor,
        IDiskSpaceMonitor diskSpaceMonitor,
        IProcessWatchdog processWatchdog,
        IServiceWatchdog serviceWatchdog,
        IUsbDeviceMonitor usbDeviceMonitor,
        IResourceMonitor resourceMonitor,
        IPendingRebootMonitor pendingRebootMonitor,
        IHostWatchdog hostWatchdog,
        IHttpEndpointWatchdog httpEndpointWatchdog,
        ISystemSnapshotProvider systemSnapshotProvider,
        IAlertMuteState muteState,
        IWindowsEventLogWriter windowsEventLogWriter,
        IOptions<MonitoringOptions> monitoringOptions,
        ILogger<MonitoringHostedService> logger)
    {
        _orchestrator = orchestrator;
        _collector = collector;
        _classifier = classifier;
        _settingsStore = settingsStore;
        _alertJournal = alertJournal;
        _networkMonitor = networkMonitor;
        _powerEventMonitor = powerEventMonitor;
        _sessionMonitor = sessionMonitor;
        _diskSpaceMonitor = diskSpaceMonitor;
        _processWatchdog = processWatchdog;
        _serviceWatchdog = serviceWatchdog;
        _usbDeviceMonitor = usbDeviceMonitor;
        _resourceMonitor = resourceMonitor;
        _pendingRebootMonitor = pendingRebootMonitor;
        _hostWatchdog = hostWatchdog;
        _httpEndpointWatchdog = httpEndpointWatchdog;
        _systemSnapshotProvider = systemSnapshotProvider;
        _muteState = muteState;
        _windowsEventLogWriter = windowsEventLogWriter;
        _monitoringOptions = monitoringOptions.Value;
        _logger = logger;
        _muteState.Changed += (_, _) => RaiseStatusChanged();
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
            bool isRunning;
            bool isPaused;
            lock (_stateLock)
            {
                isRunning = _isRunning;
                isPaused = _isPaused;
            }

            if (!isRunning)
            {
                return "Мониторинг останавливается…";
            }

            if (isPaused)
            {
                return "Мониторинг на паузе";
            }

            if (_muteState.IsMuted)
            {
                return $"Мониторинг включен (тишина до {_muteState.MutedUntil:HH:mm})";
            }

            return "Мониторинг включен";
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

        _networkMonitor.NetworkEventDetected += OnNetworkEventDetected;
        _powerEventMonitor.PowerEventDetected += OnPowerEventDetected;
        _sessionMonitor.SessionEventDetected += OnExternalEventDetected;
        _diskSpaceMonitor.DiskEventDetected += OnExternalEventDetected;
        _processWatchdog.ProcessEventDetected += OnExternalEventDetected;
        _serviceWatchdog.ServiceEventDetected += OnExternalEventDetected;
        _usbDeviceMonitor.UsbEventDetected += OnExternalEventDetected;
        _resourceMonitor.ResourceEventDetected += OnExternalEventDetected;
        _pendingRebootMonitor.RebootEventDetected += OnExternalEventDetected;
        _hostWatchdog.HostEventDetected += OnExternalEventDetected;
        _httpEndpointWatchdog.HttpEventDetected += OnExternalEventDetected;
        _networkMonitor.Start();
        _powerEventMonitor.Start();
        _sessionMonitor.Start();
        _diskSpaceMonitor.Start();
        _processWatchdog.Start();
        _serviceWatchdog.Start();
        _usbDeviceMonitor.Start();
        _resourceMonitor.Start();
        _pendingRebootMonitor.Start();
        _hostWatchdog.Start();
        _httpEndpointWatchdog.Start();

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
                    await TrySendDailyDigestAsync(stoppingToken);
                    var settings = await _settingsStore.LoadAsync(stoppingToken);
                    _diskSpaceMonitor.Poll(settings);
                    _processWatchdog.Poll(settings);
                    _serviceWatchdog.Poll(settings);
                    _usbDeviceMonitor.Poll(settings);
                    _resourceMonitor.Poll(settings);
                    _pendingRebootMonitor.Poll(settings);
                    await _hostWatchdog.PollAsync(settings, stoppingToken);
                    await _httpEndpointWatchdog.PollAsync(settings, stoppingToken);
                    _powerEventMonitor.Poll(settings);
                    await TryPurgeJournalAsync(settings, stoppingToken);
                    await _orchestrator.FlushOutboxAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка фонового мониторинга системных событий");
                _windowsEventLogWriter.WriteError($"Ошибка фонового мониторинга: {ex.GetType().Name}: {ex.Message}");
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

        _networkMonitor.NetworkEventDetected -= OnNetworkEventDetected;
        _powerEventMonitor.PowerEventDetected -= OnPowerEventDetected;
        _sessionMonitor.SessionEventDetected -= OnExternalEventDetected;
        _diskSpaceMonitor.DiskEventDetected -= OnExternalEventDetected;
        _processWatchdog.ProcessEventDetected -= OnExternalEventDetected;
        _serviceWatchdog.ServiceEventDetected -= OnExternalEventDetected;
        _usbDeviceMonitor.UsbEventDetected -= OnExternalEventDetected;
        _resourceMonitor.ResourceEventDetected -= OnExternalEventDetected;
        _pendingRebootMonitor.RebootEventDetected -= OnExternalEventDetected;
        _hostWatchdog.HostEventDetected -= OnExternalEventDetected;
        _httpEndpointWatchdog.HttpEventDetected -= OnExternalEventDetected;
        SetRunning(false);
        _logger.LogInformation("Фоновый мониторинг событий остановлен");
    }

    public override void Dispose()
    {
        _networkMonitor.Dispose();
        _powerEventMonitor.Dispose();
        _sessionMonitor.Dispose();
        _diskSpaceMonitor.Dispose();
        _processWatchdog.Dispose();
        _serviceWatchdog.Dispose();
        _usbDeviceMonitor.Dispose();
        _resourceMonitor.Dispose();
        _pendingRebootMonitor.Dispose();
        _hostWatchdog.Dispose();
        _httpEndpointWatchdog.Dispose();
        base.Dispose();
    }

    private void OnNetworkEventDetected(object? sender, MachineEvent machineEvent)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (IsPaused)
                {
                    return;
                }

                await _orchestrator.ProcessMachineEventAsync(machineEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки сетевого события {EventType}", machineEvent.Type);
            }
        });
    }

    private void OnPowerEventDetected(object? sender, MachineEvent machineEvent) =>
        OnExternalEventDetected(sender, machineEvent);

    private void OnExternalEventDetected(object? sender, MachineEvent machineEvent)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (IsPaused)
                {
                    return;
                }

                await _orchestrator.ProcessMachineEventAsync(machineEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки события {EventType}", machineEvent.Type);
                _windowsEventLogWriter.WriteError(
                    $"Ошибка обработки {machineEvent.Type}: {ex.GetType().Name}: {ex.Message}");
            }
        });
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

        var snapshot = _systemSnapshotProvider.Capture(_networkMonitor.CurrentPrimaryIp);
        var heartbeat = new MachineEvent
        {
            Type = MachineEventType.Heartbeat,
            OccurredAt = now,
            Source = "AlarmProgram",
            EventId = null,
            HostName = Environment.MachineName,
            Message = HeartbeatSnapshotBuilder.Format(
                intervalMinutes,
                snapshot.PrimaryIp,
                snapshot.Uptime,
                snapshot.SystemDriveFreePercent,
                snapshot.MemoryUsedPercent)
        };

        await _orchestrator.ProcessMachineEventAsync(heartbeat, settings, cancellationToken);
        _lastHeartbeatAt = now;
    }

    private async Task TrySendDailyDigestAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (!DailyDigestBuilder.ShouldSend(
                settings.DailyDigestEnabled,
                settings.DailyDigestTime,
                now,
                _lastDailyDigestLocalDate))
        {
            return;
        }

        if (!settings.IsValid || !settings.HasEnabledChannel)
        {
            return;
        }

        if (settings.IsWithinQuietHours(now))
        {
            return;
        }

        var recent = await _alertJournal.GetRecentAsync(200, cancellationToken);
        var digest = DailyDigestBuilder.Build(recent, now);
        await _orchestrator.ProcessMachineEventAsync(digest, settings, cancellationToken);
        _lastDailyDigestLocalDate = DateOnly.FromDateTime(now.ToLocalTime().DateTime);
    }

    private async Task TryPurgeJournalAsync(UserSettings settings, CancellationToken cancellationToken)
    {
        if (settings.JournalRetentionDays <= 0)
        {
            return;
        }

        var removed = await _alertJournal.PurgeOlderThanAsync(
            TimeSpan.FromDays(settings.JournalRetentionDays),
            cancellationToken);
        if (removed > 0)
        {
            _logger.LogInformation("Автоочистка журнала удалила {Count} записей", removed);
        }
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
