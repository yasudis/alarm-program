using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Health;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace AlarmProgram.Infrastructure.Reboot;

public sealed class WindowsPendingRebootMonitor : IPendingRebootMonitor
{
    private readonly ILogger<WindowsPendingRebootMonitor> _logger;
    private readonly Func<bool> _isPending;
    private readonly object _sync = new();
    private bool _started;
    private bool _disposed;
    private bool _wasPending;

    public WindowsPendingRebootMonitor(ILogger<WindowsPendingRebootMonitor> logger)
        : this(logger, DetectFromRegistry)
    {
    }

    public WindowsPendingRebootMonitor(ILogger<WindowsPendingRebootMonitor> logger, Func<bool> isPending)
    {
        _logger = logger;
        _isPending = isPending;
    }

    public event EventHandler<MachineEvent>? RebootEventDetected;

    public void Start()
    {
        lock (_sync)
        {
            if (_started || _disposed)
            {
                return;
            }

            _started = true;
        }

        _logger.LogInformation("Монитор отложенной перезагрузки запущен");
    }

    public void Poll(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!_started || _disposed || !settings.NotifyOnRebootPending)
        {
            return;
        }

        try
        {
            var pending = _isPending();
            var alert = SystemHealthRules.RebootPending(pending);
            lock (_sync)
            {
                if (alert is null)
                {
                    _wasPending = false;
                    return;
                }

                if (_wasPending)
                {
                    return;
                }

                _wasPending = true;
            }

            RebootEventDetected?.Invoke(this, alert);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка проверки отложенной перезагрузки");
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            _started = false;
        }
    }

    internal static bool DetectFromRegistry()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            using var windowsUpdate = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
            if (windowsUpdate is not null)
            {
                return true;
            }

            using var servicing = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");
            if (servicing is not null)
            {
                return true;
            }

            using var sessionManager = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Session Manager");
            if (sessionManager?.GetValue("PendingFileRenameOperations") is string[] pending
                && pending.Any(item => !string.IsNullOrWhiteSpace(item)))
            {
                return true;
            }
        }
        catch (Exception)
        {
            return false;
        }

        return false;
    }
}
