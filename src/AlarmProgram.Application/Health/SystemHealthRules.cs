using AlarmProgram.Domain;

namespace AlarmProgram.Application.Health;

public static class SystemHealthRules
{
    public static MachineEvent? LowDiskSpace(
        string driveName,
        long totalBytes,
        long freeBytes,
        int thresholdPercent)
    {
        if (string.IsNullOrWhiteSpace(driveName) || totalBytes <= 0 || freeBytes < 0)
        {
            return null;
        }

        var freePercent = (int)Math.Floor(freeBytes * 100d / totalBytes);
        if (freePercent > thresholdPercent)
        {
            return null;
        }

        return Create(
            MachineEventType.LowDiskSpace,
            "DiskMonitor",
            $"Диск {driveName}: свободно {freePercent}% ({FormatBytes(freeBytes)} из {FormatBytes(totalBytes)}). Порог {thresholdPercent}%.");
    }

    public static MachineEvent? BatteryLow(int percent, bool onBattery, int thresholdPercent)
    {
        if (!onBattery || percent is < 0 or > 100 || percent > thresholdPercent)
        {
            return null;
        }

        return Create(
            MachineEventType.BatteryLow,
            "PowerMonitor",
            $"Заряд батареи {percent}%. Порог {thresholdPercent}%.");
    }

    public static MachineEvent? AcPowerChange(bool previousOnAc, bool currentOnAc)
    {
        if (previousOnAc == currentOnAc)
        {
            return null;
        }

        return currentOnAc
            ? Create(MachineEventType.AcPowerRestored, "PowerMonitor", "Питание от сети восстановлено.")
            : Create(MachineEventType.AcPowerLost, "PowerMonitor", "Переход на питание от батареи.");
    }

    public static MachineEvent SessionLock() =>
        Create(MachineEventType.SessionLock, "SessionMonitor", "Сессия Windows заблокирована.");

    public static MachineEvent SessionUnlock() =>
        Create(MachineEventType.SessionUnlock, "SessionMonitor", "Сессия Windows разблокирована.");

    public static MachineEvent RdpConnected() =>
        Create(MachineEventType.RdpConnected, "SessionMonitor", "Установлено удалённое подключение (RDP).");

    public static MachineEvent RdpDisconnected() =>
        Create(MachineEventType.RdpDisconnected, "SessionMonitor", "Удалённое подключение (RDP) разорвано.");

    public static MachineEvent? ProcessDown(string processName, bool isRunning)
    {
        if (string.IsNullOrWhiteSpace(processName) || isRunning)
        {
            return null;
        }

        return Create(
            MachineEventType.ProcessDown,
            "ProcessWatchdog",
            $"Процесс {processName} не запущен.");
    }

    public static MachineEvent? HighCpu(int percent, int thresholdPercent)
    {
        if (percent is < 0 or > 100 || percent < thresholdPercent)
        {
            return null;
        }

        return Create(
            MachineEventType.HighCpu,
            "ResourceMonitor",
            $"Загрузка CPU {percent}%. Порог {thresholdPercent}%.");
    }

    public static MachineEvent? HighMemory(int percent, int thresholdPercent)
    {
        if (percent is < 0 or > 100 || percent < thresholdPercent)
        {
            return null;
        }

        return Create(
            MachineEventType.HighMemory,
            "ResourceMonitor",
            $"Использование памяти {percent}%. Порог {thresholdPercent}%.");
    }

    private static MachineEvent Create(MachineEventType type, string source, string message) => new()
    {
        Type = type,
        OccurredAt = DateTimeOffset.UtcNow,
        Source = source,
        HostName = Environment.MachineName,
        Message = message
    };

    private static string FormatBytes(long bytes)
    {
        const double mega = 1024d * 1024d;
        if (bytes >= mega * 1024d)
        {
            return $"{bytes / (mega * 1024d):0.0} ГБ";
        }

        return $"{bytes / mega:0.0} МБ";
    }
}
