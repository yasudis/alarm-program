using AlarmProgram.Application.Health;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Health;

public class DailyDigestBuilderTests
{
    [Fact]
    public void ShouldSend_requires_enabled_and_local_time_after_digest_time_once_per_day()
    {
        var localDateTime = new DateTime(2026, 8, 30, 10, 0, 0, DateTimeKind.Unspecified);
        var offset = TimeZoneInfo.Local.GetUtcOffset(localDateTime);
        var now = new DateTimeOffset(localDateTime, offset);
        var localDate = DateOnly.FromDateTime(localDateTime);

        Assert.False(DailyDigestBuilder.ShouldSend(false, TimeSpan.FromHours(9), now, null));
        Assert.True(DailyDigestBuilder.ShouldSend(true, TimeSpan.FromHours(9), now, null));
        Assert.False(DailyDigestBuilder.ShouldSend(true, TimeSpan.FromHours(9), now, localDate));
        Assert.False(DailyDigestBuilder.ShouldSend(true, TimeSpan.FromHours(11), now, null));
    }

    [Fact]
    public void Build_summarizes_alerts_and_skips_heartbeat_digest()
    {
        var now = DateTimeOffset.Parse("2026-08-30T12:00:00Z");
        var entries = new[]
        {
            new AlertJournalEntry
            {
                Timestamp = now.AddHours(-1),
                EventType = MachineEventType.Startup,
                Subject = "on",
                Status = "Sent"
            },
            new AlertJournalEntry
            {
                Timestamp = now.AddHours(-2),
                EventType = MachineEventType.Startup,
                Subject = "on2",
                Status = "Sent"
            },
            new AlertJournalEntry
            {
                Timestamp = now.AddHours(-3),
                EventType = MachineEventType.Heartbeat,
                Subject = "hb",
                Status = "Sent"
            },
            new AlertJournalEntry
            {
                Timestamp = now.AddDays(-2),
                EventType = MachineEventType.Shutdown,
                Subject = "old",
                Status = "Sent"
            }
        };

        var digest = DailyDigestBuilder.Build(entries, now);

        Assert.Equal(MachineEventType.DailyDigest, digest.Type);
        Assert.Contains("алертов: 2", digest.Message);
        Assert.Contains("Startup: 2", digest.Message);
        Assert.DoesNotContain("Heartbeat", digest.Message);
        Assert.DoesNotContain("Shutdown", digest.Message);
    }

    [Fact]
    public void Build_handles_empty_window()
    {
        var digest = DailyDigestBuilder.Build([], DateTimeOffset.UtcNow);

        Assert.Equal(MachineEventType.DailyDigest, digest.Type);
        Assert.Contains("не было", digest.Message);
    }
}
