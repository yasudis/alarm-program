using AlarmProgram.Application.Contracts;
using AlarmProgram.Application.Events;
using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.Events;

namespace AlarmProgram.Tests.Unit.Events;

public class EventClassifierTests
{
    private readonly EventClassifier _classifier = new();

    [Theory]
    [InlineData(12, MachineEventType.Startup)]
    [InlineData(6005, MachineEventType.Startup)]
    [InlineData(6009, MachineEventType.Startup)]
    [InlineData(13, MachineEventType.Shutdown)]
    [InlineData(6006, MachineEventType.Shutdown)]
    [InlineData(41, MachineEventType.UnexpectedShutdown)]
    [InlineData(1076, MachineEventType.UnexpectedShutdown)]
    [InlineData(6008, MachineEventType.UnexpectedShutdown)]
    [InlineData(7001, MachineEventType.UserLogon)]
    [InlineData(7002, MachineEventType.UserLogoff)]
    public void Classify_maps_known_event_ids(int eventId, MachineEventType expectedType)
    {
        var result = _classifier.Classify(CreateRaw(eventId));

        Assert.NotNull(result);
        Assert.Equal(expectedType, result.Type);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal("TEST-PC", result.HostName);
        Assert.Equal("Microsoft-Windows-EventLog", result.Source);
    }

    [Theory]
    [InlineData("The process explorer.exe has initiated the restart of computer WIN")]
    [InlineData("initiated the reboot of computer")]
    [InlineData("Пользователь инициировал перезагрузку компьютера")]
    [InlineData("инициирован перезапуск системы")]
    [InlineData("Плановый рестарт Windows Update")]
    public void Classify_maps_1074_restart_messages_to_restart(string message)
    {
        var result = _classifier.Classify(CreateRaw(1074, message));

        Assert.NotNull(result);
        Assert.Equal(MachineEventType.Restart, result.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("The process explorer.exe has initiated the shutdown of computer WIN")]
    [InlineData("initiated the power off of computer")]
    [InlineData("Пользователь инициировал выключение компьютера")]
    public void Classify_maps_1074_without_restart_keywords_to_shutdown(string? message)
    {
        var result = _classifier.Classify(CreateRaw(1074, message));

        Assert.NotNull(result);
        Assert.Equal(MachineEventType.Shutdown, result.Type);
    }

    [Fact]
    public void Classify_returns_null_for_unknown_event_id()
    {
        var result = _classifier.Classify(CreateRaw(9999, "unrelated"));

        Assert.Null(result);
    }

    [Fact]
    public void Classify_uses_machine_name_when_host_is_missing()
    {
        var raw = CreateRaw(12);
        raw = new RawSystemEvent
        {
            OccurredAt = raw.OccurredAt,
            EventId = raw.EventId,
            Source = raw.Source,
            Message = raw.Message,
            HostName = " "
        };

        var result = _classifier.Classify(raw);

        Assert.NotNull(result);
        Assert.Equal(Environment.MachineName, result.HostName);
    }

    [Fact]
    public void Classify_covers_all_windows_event_log_candidate_ids()
    {
        foreach (var eventId in WindowsEventLogReader.CandidateEventIds)
        {
            var result = _classifier.Classify(CreateRaw(eventId, "initiated the restart of computer"));
            Assert.NotNull(result);
            Assert.NotEqual(MachineEventType.Unknown, result.Type);
        }
    }

    [Fact]
    public void Classify_throws_when_raw_event_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => _classifier.Classify(null!));
    }

    private static RawSystemEvent CreateRaw(int eventId, string? message = "sample") => new()
    {
        OccurredAt = new DateTimeOffset(2026, 8, 24, 10, 30, 0, TimeSpan.Zero),
        EventId = eventId,
        Source = "Microsoft-Windows-EventLog",
        Message = message,
        HostName = "TEST-PC"
    };
}
