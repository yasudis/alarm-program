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
    [InlineData(4625, MachineEventType.FailedLogon)]
    [InlineData(1000, MachineEventType.ApplicationCrash)]
    [InlineData(1116, MachineEventType.DefenderThreat)]
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
    public void Classify_maps_bugcheck_1001_to_blue_screen()
    {
        var result = _classifier.Classify(CreateRaw(1001, "The computer has rebooted from a bugcheck.", "Microsoft-Windows-WER-SystemErrorReporting"));

        Assert.NotNull(result);
        Assert.Equal(MachineEventType.BlueScreen, result.Type);
    }

    [Fact]
    public void Classify_ignores_1001_from_unrelated_source()
    {
        Assert.Null(_classifier.Classify(CreateRaw(1001, "unrelated", "Microsoft-Windows-EventLog")));
    }

    [Fact]
    public void Classify_maps_windows_update_20_to_failed_update()
    {
        var result = _classifier.Classify(CreateRaw(20, "Installation Failure", "Microsoft-Windows-WindowsUpdateClient"));

        Assert.NotNull(result);
        Assert.Equal(MachineEventType.WindowsUpdateFailed, result.Type);
    }

    [Fact]
    public void Classify_ignores_event_20_from_other_sources()
    {
        Assert.Null(_classifier.Classify(CreateRaw(20, "something else", "Microsoft-Windows-EventLog")));
    }

    [Theory]
    [InlineData("A member was added to security-enabled local group Administrators")]
    [InlineData("Группа Администраторы")]
    [InlineData("Target SID: S-1-5-32-544")]
    public void Classify_maps_4732_administrators_membership(string message)
    {
        var result = _classifier.Classify(CreateRaw(4732, message, "Microsoft-Windows-Security-Auditing"));

        Assert.NotNull(result);
        Assert.Equal(MachineEventType.AdminGroupChanged, result.Type);
    }

    [Fact]
    public void Classify_ignores_4732_for_other_groups()
    {
        Assert.Null(_classifier.Classify(CreateRaw(4732, "A member was added to Users", "Microsoft-Windows-Security-Auditing")));
    }

    [Fact]
    public void Classify_covers_all_windows_event_log_candidate_ids()
    {
        foreach (var eventId in WindowsEventLogReader.CandidateEventIds)
        {
            var (source, message) = SampleFor(eventId);
            var result = _classifier.Classify(CreateRaw(eventId, message, source));
            Assert.NotNull(result);
            Assert.NotEqual(MachineEventType.Unknown, result.Type);
        }
    }

    [Fact]
    public void Classify_throws_when_raw_event_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => _classifier.Classify(null!));
    }

    private static RawSystemEvent CreateRaw(int eventId, string? message = "sample", string source = "Microsoft-Windows-EventLog") => new()
    {
        OccurredAt = new DateTimeOffset(2026, 8, 24, 10, 30, 0, TimeSpan.Zero),
        EventId = eventId,
        Source = source,
        Message = message,
        HostName = "TEST-PC"
    };

    private static (string Source, string Message) SampleFor(int eventId) => eventId switch
    {
        20 => ("Microsoft-Windows-WindowsUpdateClient", "Installation Failure"),
        1001 => ("BugCheck", "The computer has rebooted from a bugcheck."),
        1116 => ("Microsoft-Windows-Windows Defender", "Antimalware malware detected"),
        4732 => ("Microsoft-Windows-Security-Auditing", "A member was added to Administrators"),
        1074 => ("Microsoft-Windows-EventLog", "initiated the restart of computer"),
        _ => ("Microsoft-Windows-EventLog", "initiated the restart of computer")
    };
}
