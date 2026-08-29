using System.Runtime.InteropServices;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Health;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace AlarmProgram.Infrastructure.Power;

public sealed class WindowsPowerEventMonitor : IPowerEventMonitor
{
    private readonly ILogger<WindowsPowerEventMonitor> _logger;
    private readonly object _sync = new();
    private bool _started;
    private bool _disposed;
    private bool? _lastOnAc;
    private bool _batteryLowRaised;
    private UserSettings _lastSettings = new();

    public WindowsPowerEventMonitor(ILogger<WindowsPowerEventMonitor> logger)
    {
        _logger = logger;
    }

    public event EventHandler<MachineEvent>? PowerEventDetected;

    public void Start()
    {
        lock (_sync)
        {
            if (_started || _disposed)
            {
                return;
            }

            if (!OperatingSystem.IsWindows())
            {
                _logger.LogInformation("Power monitor пропущен: не Windows");
                return;
            }

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            _started = true;
        }

        _logger.LogInformation("Монитор питания Windows запущен");
    }

    public void Poll(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (_sync)
        {
            _lastSettings = settings;
        }

        EvaluatePowerStatus(settings);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            if (_started && OperatingSystem.IsWindows())
            {
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            }

            _disposed = true;
            _started = false;
        }
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        try
        {
            if (e.Mode == PowerModes.Resume)
            {
                Raise(new MachineEvent
                {
                    Type = MachineEventType.SystemResume,
                    OccurredAt = DateTimeOffset.UtcNow,
                    Source = "PowerMonitor",
                    HostName = Environment.MachineName,
                    Message = "Система вышла из режима сна/гибернации"
                });
                return;
            }

            if (e.Mode == PowerModes.StatusChange)
            {
                UserSettings settings;
                lock (_sync)
                {
                    settings = _lastSettings;
                }

                EvaluatePowerStatus(settings);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка обработки события питания");
        }
    }

    private void EvaluatePowerStatus(UserSettings settings)
    {
        if (!OperatingSystem.IsWindows() || !TryGetPowerStatus(out var onAc, out var batteryPercent))
        {
            return;
        }

        bool? previousOnAc;
        bool batteryLowAlreadyRaised;
        lock (_sync)
        {
            previousOnAc = _lastOnAc;
            batteryLowAlreadyRaised = _batteryLowRaised;
            _lastOnAc = onAc;
        }

        if (previousOnAc is { } knownPrevious)
        {
            var acEvent = SystemHealthRules.AcPowerChange(knownPrevious, onAc);
            if (acEvent is not null)
            {
                Raise(acEvent);
            }
        }

        var batteryEvent = SystemHealthRules.BatteryLow(
            batteryPercent,
            onBattery: !onAc,
            settings.BatteryLowThresholdPercent);

        lock (_sync)
        {
            if (batteryEvent is null)
            {
                _batteryLowRaised = false;
                return;
            }

            if (batteryLowAlreadyRaised)
            {
                return;
            }

            _batteryLowRaised = true;
        }

        if (batteryEvent is not null)
        {
            Raise(batteryEvent);
        }
    }

    private void Raise(MachineEvent machineEvent)
    {
        try
        {
            PowerEventDetected?.Invoke(this, machineEvent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка публикации события питания {EventType}", machineEvent.Type);
        }
    }

    private static bool TryGetPowerStatus(out bool onAc, out int batteryPercent)
    {
        onAc = true;
        batteryPercent = -1;

        if (!GetSystemPowerStatus(out var status))
        {
            return false;
        }

        onAc = status.ACLineStatus != 0;
        batteryPercent = status.BatteryLifePercent is > 100 ? -1 : status.BatteryLifePercent;
        return true;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus lpSystemPowerStatus);

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }
}
