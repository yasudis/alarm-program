using AlarmProgram.Application.Health;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Health;

public class WeeklyDigestBuilderTests
{
    [Fact]
    public void ShouldSend_requires_matching_weekday_and_time()
    {
        var mondayMorning = Local(new DateTime(2026, 8, 31, 10, 0, 0)); // Monday
        var tuesdayMorning = Local(new DateTime(2026, 9, 1, 10, 0, 0)); // Tuesday
        var mondayDate = DateOnly.FromDateTime(mondayMorning.DateTime);

        Assert.False(WeeklyDigestBuilder.ShouldSend(false, DayOfWeek.Monday, TimeSpan.FromHours(9), mondayMorning, null));
        Assert.True(WeeklyDigestBuilder.ShouldSend(true, DayOfWeek.Monday, TimeSpan.FromHours(9), mondayMorning, null));
        Assert.False(WeeklyDigestBuilder.ShouldSend(true, DayOfWeek.Monday, TimeSpan.FromHours(9), tuesdayMorning, null));
        Assert.False(WeeklyDigestBuilder.ShouldSend(true, DayOfWeek.Monday, TimeSpan.FromHours(9), mondayMorning, mondayDate));
        Assert.False(WeeklyDigestBuilder.ShouldSend(true, DayOfWeek.Monday, TimeSpan.FromHours(11), mondayMorning, null));
    }

    [Fact]
    public void Build_summarizes_week_and_skips_heartbeat_digests()
    {
        var now = DateTimeOffset.Parse("2026-08-30T12:00:00Z");
        var entries = new[]
        {
            new AlertJournalEntry
            {
                Timestamp = now.AddDays(-1),
                EventType = MachineEventType.Bsod,
                Subject = "bsod",
                Status = "Sent",
                Channel = "Telegram"
            },
            new AlertJournalEntry
            {
                Timestamp = now.AddDays(-2),
                EventType = MachineEventType.Bsod,
                Subject = "bsod2",
                Status = "Queued",
                Channel = "Discord"
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
                Timestamp = now.AddDays(-8),
                EventType = MachineEventType.Startup,
                Subject = "old",
                Status = "Sent"
            }
        };

        var digest = WeeklyDigestBuilder.Build(entries, now);

        Assert.Equal(MachineEventType.WeeklyDigest, digest.Type);
        Assert.Contains("алертов: 2", digest.Message);
        Assert.Contains("Bsod: 2", digest.Message);
        Assert.Contains("По каналам:", digest.Message);
        Assert.Contains("Telegram: 1", digest.Message);
        Assert.Contains("Discord: 1", digest.Message);
        Assert.Contains("По статусам доставки:", digest.Message);
        Assert.Contains("Sent: 1", digest.Message);
        Assert.Contains("Queued: 1", digest.Message);
        Assert.DoesNotContain("Heartbeat", digest.Message);
        Assert.DoesNotContain("Startup", digest.Message);
    }

    [Fact]
    public void Build_handles_empty_window()
    {
        var digest = WeeklyDigestBuilder.Build([], DateTimeOffset.UtcNow);

        Assert.Equal(MachineEventType.WeeklyDigest, digest.Type);
        Assert.Contains("не было", digest.Message);
    }

    private static DateTimeOffset Local(DateTime unspecifiedLocal)
    {
        var offset = TimeZoneInfo.Local.GetUtcOffset(unspecifiedLocal);
        return new DateTimeOffset(unspecifiedLocal, offset);
    }
}
