namespace AlarmProgram.Application.Health;

public static class HeartbeatSnapshotBuilder
{
    public static string Format(
        int intervalMinutes,
        string? primaryIp,
        TimeSpan uptime,
        int? diskFreePercent,
        int? memoryUsedPercent)
    {
        var parts = new List<string>
        {
            $"Периодический heartbeat каждые {intervalMinutes} мин.",
            $"IP={primaryIp ?? "-"}",
            $"Uptime={FormatUptime(uptime)}"
        };

        if (diskFreePercent is not null)
        {
            parts.Add($"Диск={diskFreePercent}% свободно");
        }

        if (memoryUsedPercent is not null)
        {
            parts.Add($"RAM={memoryUsedPercent}%");
        }

        return string.Join(" ", parts);
    }

    public static string FormatUptime(TimeSpan uptime)
    {
        if (uptime < TimeSpan.Zero)
        {
            uptime = TimeSpan.Zero;
        }

        if (uptime.TotalDays >= 1)
        {
            return $"{(int)uptime.TotalDays}д {uptime.Hours}ч {uptime.Minutes}м";
        }

        if (uptime.TotalHours >= 1)
        {
            return $"{(int)uptime.TotalHours}ч {uptime.Minutes}м";
        }

        return $"{uptime.Minutes}м";
    }
}
