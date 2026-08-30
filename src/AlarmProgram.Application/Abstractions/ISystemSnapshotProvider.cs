namespace AlarmProgram.Application.Abstractions;

public interface ISystemSnapshotProvider
{
    SystemSnapshot Capture(string? primaryIp);
}

public sealed class SystemSnapshot
{
    public string? PrimaryIp { get; init; }

    public TimeSpan Uptime { get; init; }

    public int? SystemDriveFreePercent { get; init; }

    public int? MemoryUsedPercent { get; init; }
}
