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

    [Fact]
    public void Apply_uses_and_logic_for_multiple_terms()
    {
        var entries = new[]
        {
            Entry(MachineEventType.Startup, "ПК включился", "Telegram", status: "Sent"),
            Entry(MachineEventType.Startup, "ПК включился", "Discord", status: "Queued"),
            Entry(MachineEventType.Bsod, "Синий экран", "Telegram", status: "Sent")
        };

        var result = JournalSearch.Apply(entries, "startup telegram sent");

        Assert.Single(result);
        Assert.Equal(MachineEventType.Startup, result[0].EventType);
        Assert.Equal("Telegram", result[0].Channel);
    }

    [Fact]
    public void Apply_supports_field_prefixes_and_negation()
    {
        var entries = new[]
        {
            Entry(MachineEventType.Bsod, "Critical", "Telegram", host: "PC-1"),
            Entry(MachineEventType.Bsod, "Critical", "Discord", host: "PC-2")
        };

        var result = JournalSearch.Apply(entries, "type:bsod !channel:discord host:pc-1");

        Assert.Single(result);
        Assert.Equal("PC-1", result[0].HostName);
    }

    [Fact]
    public void Apply_supports_correlation_id_and_date_filters()
    {
        var current = Entry(
            MachineEventType.Startup,
            "on",
            correlationId: "abc-123",
            timestamp: Local(new DateTime(2026, 9, 1, 11, 0, 0)));
        var older = Entry(
            MachineEventType.Startup,
            "old",
            correlationId: "xyz-777",
            timestamp: Local(new DateTime(2026, 9, 2, 11, 0, 0)));
        var entries = new[] { current, older };

        var byCorrelation = JournalSearch.Apply(entries, "cid:abc-123");
        var byDate = JournalSearch.Apply(entries, "date:2026-09-01");

        Assert.Single(byCorrelation);
        Assert.Equal("abc-123", byCorrelation[0].CorrelationId);
        Assert.Single(byDate);
        Assert.Equal("abc-123", byDate[0].CorrelationId);
    }

    private static AlertJournalEntry Entry(
        MachineEventType type,
        string subject,
        string channel = "Telegram",
        string status = "Sent",
        string host = "TEST-PC",
        string correlationId = "cid-1",
        DateTimeOffset? timestamp = null) => new()
    {
        Timestamp = timestamp ?? DateTimeOffset.UtcNow,
        EventType = type,
        Subject = subject,
        Status = status,
        Channel = channel,
        HostName = host,
        CorrelationId = correlationId
    };

    private static DateTimeOffset Local(DateTime unspecifiedLocal)
    {
        var offset = TimeZoneInfo.Local.GetUtcOffset(unspecifiedLocal);
        return new DateTimeOffset(unspecifiedLocal, offset);
    }
}
