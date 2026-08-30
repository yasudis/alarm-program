namespace AlarmProgram.Application.Health;

public static class HostUptimeFormatter
{
    public static string Format(TimeSpan uptime)
    {
        if (uptime < TimeSpan.Zero)
        {
            uptime = TimeSpan.Zero;
        }

        var days = (int)uptime.TotalDays;
        var hours = uptime.Hours;
        var minutes = uptime.Minutes;

        if (days > 0)
        {
            return $"{days}д {hours}ч {minutes}м";
        }

        if (hours > 0)
        {
            return $"{hours}ч {minutes}м";
        }

        return $"{Math.Max(minutes, 0)}м";
    }
}
