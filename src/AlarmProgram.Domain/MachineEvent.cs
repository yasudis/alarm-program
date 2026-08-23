namespace AlarmProgram.Domain;

public sealed class MachineEvent
{
    public required MachineEventType Type { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public required string Source { get; init; }

    public int? EventId { get; init; }

    public string? HostName { get; init; }

    public string? Message { get; init; }
}
