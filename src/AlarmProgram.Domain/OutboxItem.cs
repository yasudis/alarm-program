namespace AlarmProgram.Domain;

public sealed class OutboxItem
{
    public required string Id { get; init; }

    public required AlertMessage Message { get; init; }

    public required string ChannelName { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }
}
