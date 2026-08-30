using AlarmProgram.Application.Alerts;

namespace AlarmProgram.Tests.Unit.Alerts;

public class SnoozeScheduleTests
{
    [Fact]
    public void TryResolveLocalTime_uses_today_when_time_is_in_the_future()
    {
        var local = new DateTime(2026, 8, 30, 7, 0, 0, DateTimeKind.Unspecified);
        var offset = TimeZoneInfo.Local.GetUtcOffset(local);
        var now = new DateTimeOffset(local, offset);

        Assert.True(SnoozeSchedule.TryResolveLocalTime("08:00", now, out var until));

        var untilLocal = until.ToLocalTime();
        Assert.Equal(new TimeSpan(8, 0, 0), untilLocal.TimeOfDay);
        Assert.Equal(new DateOnly(2026, 8, 30), DateOnly.FromDateTime(untilLocal.DateTime));
    }

    [Fact]
    public void TryResolveLocalTime_rolls_to_next_day_when_time_already_passed()
    {
        var local = new DateTime(2026, 8, 30, 9, 0, 0, DateTimeKind.Unspecified);
        var offset = TimeZoneInfo.Local.GetUtcOffset(local);
        var now = new DateTimeOffset(local, offset);

        Assert.True(SnoozeSchedule.TryResolveLocalTime("08:00", now, out var until));

        var untilLocal = until.ToLocalTime();
        Assert.Equal(new TimeSpan(8, 0, 0), untilLocal.TimeOfDay);
        Assert.Equal(new DateOnly(2026, 8, 31), DateOnly.FromDateTime(untilLocal.DateTime));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("25:00")]
    [InlineData("not-a-time")]
    public void TryResolveLocalTime_rejects_invalid_values(string? value)
    {
        Assert.False(SnoozeSchedule.TryResolveLocalTime(value, DateTimeOffset.Now, out _));
    }
}
