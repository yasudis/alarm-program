using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Domain;

public class MachineEventTests
{
    [Theory]
    [InlineData(MachineEventType.Startup)]
    [InlineData(MachineEventType.Shutdown)]
    [InlineData(MachineEventType.Restart)]
    [InlineData(MachineEventType.UnexpectedShutdown)]
    [InlineData(MachineEventType.UserLogon)]
    [InlineData(MachineEventType.Heartbeat)]
    [InlineData(MachineEventType.SessionLock)]
    [InlineData(MachineEventType.LowDiskSpace)]
    public void MachineEvent_supports_core_event_types(MachineEventType eventType)
    {
        var machineEvent = new MachineEvent
        {
            Type = eventType,
            OccurredAt = DateTimeOffset.UtcNow,
            Source = "System",
            EventId = 6005,
            HostName = "TEST-PC",
            Message = "Test event"
        };

        Assert.Equal(eventType, machineEvent.Type);
        Assert.Equal("System", machineEvent.Source);
        Assert.Equal("TEST-PC", machineEvent.HostName);
    }

    [Fact]
    public void AlertMessage_contains_formatted_payload()
    {
        var alert = new AlertMessage
        {
            EventType = MachineEventType.Restart,
            Subject = "PC restarted",
            Body = "Host TEST-PC restarted at 2026-08-23T10:00:00Z",
            CreatedAt = DateTimeOffset.Parse("2026-08-23T10:00:00Z"),
            HostName = "TEST-PC"
        };

        Assert.Equal(MachineEventType.Restart, alert.EventType);
        Assert.Contains("TEST-PC", alert.Body);
    }
}
