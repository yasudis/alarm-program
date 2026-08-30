using AlarmProgram.Application.Journal;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Journal;

public class AlertJournalFilterTests
{
    [Fact]
    public void Apply_returns_all_when_query_is_empty()
    {
        var entries = new[] { Entry("Startup", "ПК включился"), Entry("Heartbeat", "ПК в сети") };

        var result = AlertJournalFilter.Apply(entries, "  ");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Apply_filters_by_type_subject_and_channel()
    {
        var entries = new[]
        {
            Entry("Startup", "ПК включился", channel: "Telegram"),
            Entry("BlueScreen", "Синий экран / BugCheck", channel: "Email"),
            Entry("Heartbeat", "ПК в сети", channel: "Telegram", details: "correlation-abc")
        };

        var byType = AlertJournalFilter.Apply(entries, "blue");
        var byChannel = AlertJournalFilter.Apply(entries, "email");
        var byDetails = AlertJournalFilter.Apply(entries, "correlation-abc");

        Assert.Equal(MachineEventType.BlueScreen, Assert.Single(byType).EventType);
        Assert.Equal("Email", Assert.Single(byChannel).Channel);
        Assert.Equal(MachineEventType.Heartbeat, Assert.Single(byDetails).EventType);
    }

    [Fact]
    public void Apply_throws_when_entries_are_null()
    {
        Assert.Throws<ArgumentNullException>(() => AlertJournalFilter.Apply(null!, "x"));
    }

    private static AlertJournalEntry Entry(
        string type,
        string subject,
        string channel = "Telegram",
        string? details = null) => new()
    {
        Timestamp = DateTimeOffset.UtcNow,
        EventType = Enum.Parse<MachineEventType>(type),
        Subject = subject,
        Status = "Sent",
        Channel = channel,
        HostName = "TEST-PC",
        Details = details
    };
}
