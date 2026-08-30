using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Health;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.HostWatch;

public sealed class SystemHostWatchdog : IHostWatchdog
{
    private readonly IIcmpPingProbe _pingProbe;
    private readonly ILogger<SystemHostWatchdog> _logger;
    private readonly object _sync = new();
    private readonly HashSet<string> _missingReported = new(StringComparer.OrdinalIgnoreCase);
    private bool _started;
    private bool _disposed;

    public SystemHostWatchdog(IIcmpPingProbe pingProbe, ILogger<SystemHostWatchdog> logger)
    {
        _pingProbe = pingProbe;
        _logger = logger;
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

        _logger.LogInformation("Watchdog ping-хостов запущен");
    }

    public async Task PollAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!_started || _disposed || !settings.NotifyOnHostUnreachable)
        {
            return;
        }

        foreach (var host in settings.GetWatchedHosts())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var isReachable = await _pingProbe.IsReachableAsync(host, cancellationToken);
                var alert = SystemHealthRules.HostUnreachable(host, isReachable);

                lock (_sync)
                {
                    if (alert is null)
                    {
                        _missingReported.Remove(host);
                        continue;
                    }

                    if (!_missingReported.Add(host))
                    {
                        continue;
                    }
                }

                HostEventDetected?.Invoke(this, alert);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка ping-проверки хоста {Host}", host);
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
}
