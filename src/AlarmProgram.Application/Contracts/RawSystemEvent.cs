namespace AlarmProgram.Application.Contracts;

public sealed class RawSystemEvent
{
    public required DateTimeOffset OccurredAt { get; init; }

    public required int EventId { get; init; }

    public required string Source { get; init; }

    public string? Message { get; init; }

    public string? HostName { get; init; }
}
