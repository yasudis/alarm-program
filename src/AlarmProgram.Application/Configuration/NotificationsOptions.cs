namespace AlarmProgram.Application.Configuration;

public sealed class NotificationsOptions
{
    public const string SectionName = "Notifications";

    public int DefaultRetryCount { get; set; } = 3;

    public int RetryDelaySeconds { get; set; } = 2;
}
