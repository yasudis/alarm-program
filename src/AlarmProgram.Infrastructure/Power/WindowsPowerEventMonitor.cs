using AlarmProgram.Application.Abstractions;
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
        if (e.Mode != PowerModes.Resume)
        {
            return;
        }

        try
        {
            var machineEvent = new MachineEvent
            {
                Type = MachineEventType.SystemResume,
                OccurredAt = DateTimeOffset.UtcNow,
                Source = "PowerMonitor",
                HostName = Environment.MachineName,
                Message = "Система вышла из режима сна/гибернации"
            };

            PowerEventDetected?.Invoke(this, machineEvent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка обработки события питания");
        }
    }
}
