namespace AlarmProgram.Application.Abstractions;

public sealed record NotificationDispatchResult(
    string Channel,
    bool IsSuccess,
    bool IsSkipped,
    string? Error)
{
    public static NotificationDispatchResult Success(string channel) =>
        new(channel, IsSuccess: true, IsSkipped: false, Error: null);

    public static NotificationDispatchResult Skipped(string channel, string reason) =>
        new(channel, IsSuccess: false, IsSkipped: true, Error: reason);

    public static NotificationDispatchResult Failed(string channel, string error) =>
        new(channel, IsSuccess: false, IsSkipped: false, Error: error);
}
