using AlarmProgram.Application.Health;

namespace AlarmProgram.Tests.Unit.Health;

public class HostUptimeFormatterTests
{
    [Theory]
    [InlineData(0, 0, 0, "0м")]
    [InlineData(0, 0, 9, "9м")]
    [InlineData(0, 3, 5, "3ч 5м")]
    [InlineData(2, 4, 1, "2д 4ч 1м")]
    public void Format_renders_compact_russian_units(int days, int hours, int minutes, string expected)
    {
        var uptime = TimeSpan.FromDays(days) + TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);

        Assert.Equal(expected, HostUptimeFormatter.Format(uptime));
    }

    [Fact]
    public void Format_clamps_negative_values()
    {
        Assert.Equal("0м", HostUptimeFormatter.Format(TimeSpan.FromMinutes(-12)));
    }
}
