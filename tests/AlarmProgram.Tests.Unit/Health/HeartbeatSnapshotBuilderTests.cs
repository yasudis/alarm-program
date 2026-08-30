using AlarmProgram.Application.Health;

namespace AlarmProgram.Tests.Unit.Health;

public class HeartbeatSnapshotBuilderTests
{
    [Fact]
    public void Format_includes_ip_uptime_disk_and_memory()
    {
        var text = HeartbeatSnapshotBuilder.Format(
            intervalMinutes: 30,
            primaryIp: "192.168.1.10",
            uptime: TimeSpan.FromHours(26) + TimeSpan.FromMinutes(15),
            diskFreePercent: 42,
            memoryUsedPercent: 61);

        Assert.Contains("каждые 30 мин", text);
        Assert.Contains("IP=192.168.1.10", text);
        Assert.Contains("Uptime=1д 2ч 15м", text);
        Assert.Contains("Диск=42% свободно", text);
        Assert.Contains("RAM=61%", text);
    }

    [Fact]
    public void Format_omits_optional_metrics_and_falls_back_ip()
    {
        var text = HeartbeatSnapshotBuilder.Format(
            intervalMinutes: 60,
            primaryIp: null,
            uptime: TimeSpan.FromMinutes(12),
            diskFreePercent: null,
            memoryUsedPercent: null);

        Assert.Contains("IP=-", text);
        Assert.Contains("Uptime=12м", text);
        Assert.DoesNotContain("Диск=", text);
        Assert.DoesNotContain("RAM=", text);
    }

    [Theory]
    [InlineData(90, "1ч 30м")]
    [InlineData(5, "5м")]
    public void FormatUptime_uses_compact_units(int minutes, string expected)
    {
        Assert.Equal(expected, HeartbeatSnapshotBuilder.FormatUptime(TimeSpan.FromMinutes(minutes)));
    }
}
