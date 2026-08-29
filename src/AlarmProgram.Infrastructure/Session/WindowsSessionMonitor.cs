using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Health;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace AlarmProgram.Infrastructure.Session;

public sealed class WindowsSessionMonitor : ISessionMonitor
{
    private readonly ILogger<WindowsSessionMonitor> _logger;
    private readonly object _sync = new();
    private bool _started;
    private bool _disposed;

    public WindowsSessionMonitor(ILogger<WindowsSessionMonitor> logger)
    {
        _logger = logger;
    }

    public event EventHandler<MachineEvent>? SessionEventDetected;

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
                _logger.LogInformation("Session monitor пропущен: не Windows");
                return;
            }

            SystemEvents.SessionSwitch += OnSessionSwitch;
            _started = true;
        }

        _logger.LogInformation("Монитор блокировки сессии Windows запущен");
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
                SystemEvents.SessionSwitch -= OnSessionSwitch;
            }

            _disposed = true;
            _started = false;
        }
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        try
        {
            MachineEvent? machineEvent = e.Reason switch
            {
                SessionSwitchReason.SessionLock => SystemHealthRules.SessionLock(),
                SessionSwitchReason.SessionUnlock => SystemHealthRules.SessionUnlock(),
                _ => null
            };

            if (machineEvent is not null)
            {
                SessionEventDetected?.Invoke(this, machineEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка обработки SessionSwitch");
        }
    }
}
