using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Health;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace AlarmProgram.Infrastructure.Firewall;

public sealed class WindowsFirewallMonitor : IFirewallMonitor
{
    private readonly ILogger<WindowsFirewallMonitor> _logger;
    private readonly Func<bool> _isDisabled;
    private readonly object _sync = new();
    private bool _started;
    private bool _disposed;
    private bool _wasDisabled;

    public WindowsFirewallMonitor(ILogger<WindowsFirewallMonitor> logger)
        : this(logger, DetectFromRegistry)
    {
    }

    public WindowsFirewallMonitor(ILogger<WindowsFirewallMonitor> logger, Func<bool> isDisabled)
    {
        _logger = logger;
        _isDisabled = isDisabled;
    }

    public event EventHandler<MachineEvent>? FirewallEventDetected;

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

        _logger.LogInformation("Монитор брандмауэра Windows запущен");
    }

    public void Poll(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!_started || _disposed || !settings.NotifyOnFirewallDisabled)
        {
            return;
        }

        try
        {
            var disabled = _isDisabled();
            var alert = SystemHealthRules.FirewallDisabled(disabled);
            lock (_sync)
            {
                if (alert is null)
                {
                    _wasDisabled = false;
                    return;
                }

                if (_wasDisabled)
                {
                    return;
                }

                _wasDisabled = true;
            }

            FirewallEventDetected?.Invoke(this, alert);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка проверки брандмауэра Windows");
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
            return IsProfileDisabled(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile")
                   || IsProfileDisabled(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile")
                   || IsProfileDisabled(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile");
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool IsProfileDisabled(string subKey)
    {
        using var key = Registry.LocalMachine.OpenSubKey(subKey);
        if (key is null)
        {
            return false;
        }

        return key.GetValue("EnableFirewall") is int enabled && enabled == 0;
    }
}
