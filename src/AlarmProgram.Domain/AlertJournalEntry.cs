namespace AlarmProgram.Domain;

public sealed class AlertJournalEntry
{
    public required DateTimeOffset Timestamp { get; init; }

    public required MachineEventType EventType { get; init; }

    public required string Subject { get; init; }

    public required string Status { get; init; }

    public string? Channel { get; init; }

    public string? HostName { get; init; }

    public string? CorrelationId { get; init; }

    public string? Details { get; init; }
}
