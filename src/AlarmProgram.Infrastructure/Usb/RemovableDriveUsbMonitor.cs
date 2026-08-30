using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Health;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.Usb;

public sealed class RemovableDriveUsbMonitor : IUsbDeviceMonitor
{
    private readonly ILogger<RemovableDriveUsbMonitor> _logger;
    private readonly Func<IReadOnlyDictionary<string, string>> _listRemovableDrives;
    private readonly object _sync = new();
    private readonly Dictionary<string, string> _knownDrives = new(StringComparer.OrdinalIgnoreCase);
    private bool _started;
    private bool _disposed;
    private bool _seeded;

    public RemovableDriveUsbMonitor(ILogger<RemovableDriveUsbMonitor> logger)
        : this(logger, ListRemovableDrives)
    {
    }

    public RemovableDriveUsbMonitor(
        ILogger<RemovableDriveUsbMonitor> logger,
        Func<IReadOnlyDictionary<string, string>> listRemovableDrives)
    {
        _logger = logger;
        _listRemovableDrives = listRemovableDrives;
    }

    public event EventHandler<MachineEvent>? UsbEventDetected;

    public void Start()
    {
        lock (_sync)
        {
            if (_started || _disposed)
            {
                return;
            }

            _started = true;
            _seeded = false;
            _knownDrives.Clear();
        }

        _logger.LogInformation("Монитор съёмных дисков (USB) запущен");
    }

    public void Poll(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!_started || _disposed)
        {
            return;
        }

        if (!settings.NotifyOnUsbConnected && !settings.NotifyOnUsbDisconnected)
        {
            return;
        }

        Dictionary<string, string> current;
        try
        {
            current = _listRemovableDrives().ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка опроса съёмных дисков");
            return;
        }

        List<MachineEvent> toRaise = [];
        lock (_sync)
        {
            if (!_seeded)
            {
                foreach (var pair in current)
                {
                    _knownDrives[pair.Key] = pair.Value;
                }

                _seeded = true;
                return;
            }

            foreach (var pair in current)
            {
                if (_knownDrives.ContainsKey(pair.Key))
                {
                    _knownDrives[pair.Key] = pair.Value;
                    continue;
                }

                _knownDrives[pair.Key] = pair.Value;
                if (settings.NotifyOnUsbConnected)
                {
                    var alert = SystemHealthRules.UsbConnected(pair.Key, pair.Value);
                    if (alert is not null)
                    {
                        toRaise.Add(alert);
                    }
                }
            }

            var removedKeys = _knownDrives.Keys.Where(key => !current.ContainsKey(key)).ToArray();
            foreach (var key in removedKeys)
            {
                var label = _knownDrives[key];
                _knownDrives.Remove(key);
                if (settings.NotifyOnUsbDisconnected)
                {
                    var alert = SystemHealthRules.UsbDisconnected(key, label);
                    if (alert is not null)
                    {
                        toRaise.Add(alert);
                    }
                }
            }
        }

        foreach (var alert in toRaise)
        {
            UsbEventDetected?.Invoke(this, alert);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            _started = false;
            _seeded = false;
            _knownDrives.Clear();
        }
    }

    private static IReadOnlyDictionary<string, string> ListRemovableDrives()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.DriveType != DriveType.Removable || !drive.IsReady)
                {
                    continue;
                }

                var key = drive.Name.TrimEnd('\\', '/');
                var label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.DriveFormat : drive.VolumeLabel;
                result[key] = label;
            }
            catch
            {
                // Ignore drives that disappear mid-enumeration.
            }
        }

        return result;
    }
}
