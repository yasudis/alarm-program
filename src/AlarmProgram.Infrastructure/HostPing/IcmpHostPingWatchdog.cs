using System.Net.NetworkInformation;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Health;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.HostPing;

public sealed class IcmpHostPingWatchdog : IHostPingWatchdog
{
    private readonly ILogger<IcmpHostPingWatchdog> _logger;
    private readonly Func<string, bool> _isReachable;
    private readonly object _sync = new();
    private readonly HashSet<string> _missingReported = new(StringComparer.OrdinalIgnoreCase);
    private bool _started;
    private bool _disposed;

    public IcmpHostPingWatchdog(ILogger<IcmpHostPingWatchdog> logger)
        : this(logger, PingHost)
    {
    }

    public IcmpHostPingWatchdog(ILogger<IcmpHostPingWatchdog> logger, Func<string, bool> isReachable)
    {
        _logger = logger;
        _isReachable = isReachable;
    }

    public event EventHandler<MachineEvent>? HostEventDetected;

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

        _logger.LogInformation("Ping watchdog хостов запущен");
    }

    public void Poll(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!_started || _disposed || (!settings.NotifyOnHostUnreachable && !settings.NotifyOnHostRestored))
        {
            return;
        }

        foreach (var host in settings.GetWatchedHosts())
        {
            try
            {
                var reachable = _isReachable(host);
                MachineEvent? alert = null;
                lock (_sync)
                {
                    if (!reachable)
                    {
                        if (_missingReported.Add(host) && settings.NotifyOnHostUnreachable)
                        {
                            alert = SystemHealthRules.HostUnreachable(host, isReachable: false);
                        }
                    }
                    else if (_missingReported.Remove(host) && settings.NotifyOnHostRestored)
                    {
                        alert = SystemHealthRules.HostRestored(host, becameReachable: true);
                    }
                }

                if (alert is not null)
                {
                    HostEventDetected?.Invoke(this, alert);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка ping хоста {Host}", host);
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            _started = false;
            _missingReported.Clear();
        }
    }

    internal static bool PingHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        try
        {
            using var ping = new Ping();
            var reply = ping.Send(host, 2000);
            return reply?.Status == IPStatus.Success;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
