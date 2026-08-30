using System.Diagnostics;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Health;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.ProcessWatch;

public sealed class SystemProcessWatchdog : IProcessWatchdog
{
    private readonly ILogger<SystemProcessWatchdog> _logger;
    private readonly object _sync = new();
    private readonly HashSet<string> _missingReported = new(StringComparer.OrdinalIgnoreCase);
    private bool _started;
    private bool _disposed;

    public SystemProcessWatchdog(ILogger<SystemProcessWatchdog> logger)
    {
        _logger = logger;
    }

    public event EventHandler<MachineEvent>? ProcessEventDetected;

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

        _logger.LogInformation("Watchdog процессов запущен");
    }

    public void Poll(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!_started || _disposed || !settings.NotifyOnProcessDown)
        {
            return;
        }

        foreach (var processName in settings.GetWatchedProcessNames())
        {
            try
            {
                var isRunning = Process.GetProcessesByName(processName).Length > 0;
                var alert = SystemHealthRules.ProcessDown(processName, isRunning);

                lock (_sync)
                {
                    if (alert is null)
                    {
                        _missingReported.Remove(processName);
                        continue;
                    }

                    if (!_missingReported.Add(processName))
                    {
                        continue;
                    }
                }

                ProcessEventDetected?.Invoke(this, alert);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка проверки процесса {ProcessName}", processName);
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
