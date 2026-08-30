using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Health;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.HttpWatch;

public sealed class SystemHttpEndpointWatchdog : IHttpEndpointWatchdog
{
    private readonly IHttpEndpointProbe _probe;
    private readonly ILogger<SystemHttpEndpointWatchdog> _logger;
    private readonly object _sync = new();
    private readonly HashSet<string> _downReported = new(StringComparer.OrdinalIgnoreCase);
    private bool _started;
    private bool _disposed;

    public SystemHttpEndpointWatchdog(IHttpEndpointProbe probe, ILogger<SystemHttpEndpointWatchdog> logger)
    {
        _probe = probe;
        _logger = logger;
    }

    public event EventHandler<MachineEvent>? HttpEventDetected;

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

        _logger.LogInformation("Watchdog HTTP-эндпоинтов запущен");
    }

    public async Task PollAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!_started || _disposed || !settings.NotifyOnHttpEndpointDown)
        {
            return;
        }

        foreach (var url in settings.GetWatchedHttpEndpoints())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var isHealthy = await _probe.IsHealthyAsync(url, cancellationToken);
                var alert = SystemHealthRules.HttpEndpointDown(url, isHealthy);

                lock (_sync)
                {
                    if (alert is null)
                    {
                        _downReported.Remove(url);
                        continue;
                    }

                    if (!_downReported.Add(url))
                    {
                        continue;
                    }
                }

                HttpEventDetected?.Invoke(this, alert);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка HTTP-проверки {Url}", url);
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            _started = false;
            _downReported.Clear();
        }
    }
}
