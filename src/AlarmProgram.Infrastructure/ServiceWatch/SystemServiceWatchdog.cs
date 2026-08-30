using System.ServiceProcess;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Health;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.ServiceWatch;

public sealed class SystemServiceWatchdog : IServiceWatchdog
{
    private readonly ILogger<SystemServiceWatchdog> _logger;
    private readonly Func<string, bool?> _isServiceRunning;
    private readonly object _sync = new();
    private readonly HashSet<string> _downReported = new(StringComparer.OrdinalIgnoreCase);
    private bool _started;
    private bool _disposed;

    public SystemServiceWatchdog(ILogger<SystemServiceWatchdog> logger)
        : this(logger, QueryIsServiceRunning)
    {
    }

    public SystemServiceWatchdog(
        ILogger<SystemServiceWatchdog> logger,
        Func<string, bool?> isServiceRunning)
    {
        _logger = logger;
        _isServiceRunning = isServiceRunning;
    }

    public event EventHandler<MachineEvent>? ServiceEventDetected;

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
                _logger.LogInformation("Watchdog служб пропущен: не Windows");
                return;
            }

            _started = true;
        }

        _logger.LogInformation("Watchdog Windows-служб запущен");
    }

    public void Poll(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!_started || _disposed || !settings.NotifyOnServiceDown)
        {
            return;
        }

        foreach (var serviceName in settings.GetWatchedServiceNames())
        {
            try
            {
                var isRunning = _isServiceRunning(serviceName);
                if (isRunning is null)
                {
                    continue;
                }

                var alert = SystemHealthRules.ServiceDown(serviceName, isRunning.Value);
                lock (_sync)
                {
                    if (alert is null)
                    {
                        _downReported.Remove(serviceName);
                        continue;
                    }

                    if (!_downReported.Add(serviceName))
                    {
                        continue;
                    }
                }

                ServiceEventDetected?.Invoke(this, alert);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка проверки службы {ServiceName}", serviceName);
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

    private static bool? QueryIsServiceRunning(string serviceName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            using var controller = new ServiceController(serviceName);
            return controller.Status == ServiceControllerStatus.Running;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
