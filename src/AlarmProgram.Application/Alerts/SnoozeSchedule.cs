namespace AlarmProgram.Application.Alerts;

public static class SnoozeSchedule
{
    public static bool TryResolveLocalTime(string? value, DateTimeOffset now, out DateTimeOffset until)
    {
        until = default;
        if (!TimeSpan.TryParse(value, out var time)
            || time < TimeSpan.Zero
            || time >= TimeSpan.FromDays(1))
        {
            return false;
        }

        var localNow = now.ToLocalTime();
        var targetLocal = localNow.Date.Add(time);
        var target = new DateTimeOffset(targetLocal, localNow.Offset);
        if (target <= localNow)
        {
            target = target.AddDays(1);
        }

        until = target.ToUniversalTime();
        return true;
    }
}
