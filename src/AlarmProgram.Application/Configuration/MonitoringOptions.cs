namespace AlarmProgram.Application.Configuration;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public int PollIntervalSeconds { get; set; } = 30;

    public int InitialLookbackMinutes { get; set; } = 10;

    public int RecoveryLookbackHours { get; set; } = 24;

    public int DeduplicationWindowSeconds { get; set; } = 180;

    public int DefaultHeartbeatIntervalMinutes { get; set; } = 60;
}
