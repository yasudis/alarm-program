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
    [InlineData(1002, MachineEventType.ApplicationHang)]
    [InlineData(1116, MachineEventType.DefenderThreat)]
    [InlineData(1117, MachineEventType.DefenderThreat)]
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
            var result = _classifier.Classify(CreateRaw(eventId, MessageFor(eventId), SourceFor(eventId)));
            Assert.NotNull(result);
            Assert.NotEqual(MachineEventType.Unknown, result.Type);
        }
    }

    [Theory]
    [InlineData(20, "Microsoft-Windows-WindowsUpdateClient", MachineEventType.WindowsUpdateFailed)]
    [InlineData(7, "disk", MachineEventType.DiskError)]
    [InlineData(11, "disk", MachineEventType.DiskError)]
    [InlineData(51, "Ntfs", MachineEventType.DiskError)]
    [InlineData(153, "stornvme", MachineEventType.DiskError)]
    [InlineData(5001, "Microsoft-Windows-Windows Defender", MachineEventType.DefenderThreat)]
    [InlineData(1001, "Microsoft-Windows-WER-SystemErrorReporting", MachineEventType.Bsod)]
    [InlineData(4720, "Microsoft-Windows-Security-Auditing", MachineEventType.UserAccountCreated)]
    public void Classify_maps_source_gated_event_ids(int eventId, string source, MachineEventType expectedType)
    {
        var result = _classifier.Classify(CreateRaw(eventId, "failure", source));

        Assert.NotNull(result);
        Assert.Equal(expectedType, result.Type);
    }

    [Theory]
    [InlineData(20, "Microsoft-Windows-EventLog")]
    [InlineData(7, "Service Control Manager")]
    [InlineData(5001, "Microsoft-Windows-EventLog")]
    [InlineData(1001, "Application Error")]
    public void Classify_ignores_source_gated_ids_from_unrelated_providers(int eventId, string source)
    {
        Assert.Null(_classifier.Classify(CreateRaw(eventId, "unrelated", source)));
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

    [Fact]
    public void Classify_maps_admin_group_change_only_for_administrators()
    {
        var admin = _classifier.Classify(CreateRaw(
            4732,
            "A member was added to security-enabled local group Administrators. SID S-1-5-32-544",
            "Microsoft-Windows-Security-Auditing"));
        var other = _classifier.Classify(CreateRaw(
            4732,
            "A member was added to Users",
            "Microsoft-Windows-Security-Auditing"));

        Assert.Equal(MachineEventType.AdminGroupChanged, admin!.Type);
        Assert.Null(other);
    }

    [Fact]
    public void Classify_maps_1001_with_bugcheck_message_even_without_source()
    {
        var result = _classifier.Classify(CreateRaw(1001, "The computer has rebooted from a bugcheck", "Unknown"));

        Assert.Equal(MachineEventType.Bsod, result!.Type);
    }

    private static string MessageFor(int eventId) => eventId switch
    {
        4732 or 4728 => "A member was added to Administrators",
        1001 => "The computer has rebooted from a bugcheck",
        _ => "initiated the restart of computer"
    };

    private static string SourceFor(int eventId) => eventId switch
    {
        20 => "Microsoft-Windows-WindowsUpdateClient",
        7 or 11 or 51 or 153 => "disk",
        5001 => "Microsoft-Windows-Windows Defender",
        1001 => "Microsoft-Windows-WER-SystemErrorReporting",
        4720 or 4732 or 4728 => "Microsoft-Windows-Security-Auditing",
        _ => "Microsoft-Windows-EventLog"
    };
}
