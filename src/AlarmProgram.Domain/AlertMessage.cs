namespace AlarmProgram.Domain;

public sealed class AlertMessage
{
    public required MachineEventType EventType { get; init; }

    public required string Subject { get; init; }

    public required string Body { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public string? HostName { get; init; }
}
