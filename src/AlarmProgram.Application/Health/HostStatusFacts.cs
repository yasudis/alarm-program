namespace AlarmProgram.Application.Health;

public sealed record HostStatusFacts(
    TimeSpan Uptime,
    string? PrimaryIp,
    bool NetworkAvailable,
    string DiskSummary,
    bool RebootPending,
    bool IsMuted,
    DateTimeOffset? MutedUntil,
    bool MonitoringPaused);
