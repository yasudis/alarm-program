using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Health;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.Disk;

public sealed class SystemDiskSpaceMonitor : IDiskSpaceMonitor
{
    private readonly ILogger<SystemDiskSpaceMonitor> _logger;
    private readonly object _sync = new();
    private bool _started;
    private bool _disposed;
    private bool _wasBelowThreshold;

    public SystemDiskSpaceMonitor(ILogger<SystemDiskSpaceMonitor> logger)
    {
        _logger = logger;
    }

    public event EventHandler<MachineEvent>? DiskEventDetected;

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

        _logger.LogInformation("Монитор свободного места на диске запущен");
    }

    public void Poll(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!_started || _disposed)
        {
            return;
        }

        try
        {
            var systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            var drive = new DriveInfo(systemDrive);
            if (!drive.IsReady)
            {
                return;
            }

            var alert = SystemHealthRules.LowDiskSpace(
                drive.Name,
                drive.TotalSize,
                drive.AvailableFreeSpace,
                settings.LowDiskSpaceThresholdPercent);

            lock (_sync)
            {
                if (alert is null)
                {
                    _wasBelowThreshold = false;
                    return;
                }

                if (_wasBelowThreshold)
                {
                    return;
                }

                _wasBelowThreshold = true;
            }

            DiskEventDetected?.Invoke(this, alert);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка проверки свободного места на диске");
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
}
