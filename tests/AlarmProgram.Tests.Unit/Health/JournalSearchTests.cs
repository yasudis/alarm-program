using AlarmProgram.Application.Health;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Health;

public class JournalSearchTests
{
    [Fact]
    public void Apply_returns_all_when_filter_is_empty()
    {
        var entries = new[]
        {
            Entry(MachineEventType.Startup, "on"),
            Entry(MachineEventType.Bsod, "crash")
        };

        var result = JournalSearch.Apply(entries, "  ");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Apply_filters_by_type_subject_or_channel()
    {
        var entries = new[]
        {
            Entry(MachineEventType.Startup, "ПК включился", "Telegram"),
            Entry(MachineEventType.Bsod, "аварийная перезагрузка", "Discord")
        };

        var byType = JournalSearch.Apply(entries, "bsod");
        var byChannel = JournalSearch.Apply(entries, "telegram");
        var bySubject = JournalSearch.Apply(entries, "аварийная");

        Assert.Single(byType);
        Assert.Equal(MachineEventType.Bsod, byType[0].EventType);
        Assert.Single(byChannel);
        Assert.Equal(MachineEventType.Startup, byChannel[0].EventType);
        Assert.Single(bySubject);
        Assert.Equal(MachineEventType.Bsod, bySubject[0].EventType);
    }

    private static AlertJournalEntry Entry(MachineEventType type, string subject, string channel = "Telegram") => new()
    {
        Timestamp = DateTimeOffset.UtcNow,
        EventType = type,
        Subject = subject,
        Status = "Sent",
        Channel = channel,
        HostName = "TEST-PC"
    };
}
