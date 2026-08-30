using AlarmProgram.Application.Health;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Health;

public class StatusSnapshotBuilderTests
{
    [Fact]
    public void Build_includes_uptime_ip_disk_and_mute()
    {
        var facts = new HostStatusFacts(
            TimeSpan.FromHours(26).Add(TimeSpan.FromMinutes(15)),
            "10.0.0.8",
            NetworkAvailable: true,
            DiskSummary: "C:\\ свободно 42%",
            RebootPending: true,
            IsMuted: true,
            MutedUntil: new DateTimeOffset(2026, 8, 30, 8, 0, 0, TimeSpan.Zero),
            MonitoringPaused: false);

        var snapshot = StatusSnapshotBuilder.Build(facts, new DateTimeOffset(2026, 8, 30, 7, 0, 0, TimeSpan.Zero));

        Assert.Equal(MachineEventType.StatusSnapshot, snapshot.Type);
        Assert.Equal("StatusSnapshot", snapshot.Source);
        Assert.Contains("Uptime: 1д 2ч 15м", snapshot.Message);
        Assert.Contains("IP: 10.0.0.8", snapshot.Message);
        Assert.Contains("Сеть: доступна", snapshot.Message);
        Assert.Contains("Диск: C:\\ свободно 42%", snapshot.Message);
        Assert.Contains("Перезагрузка ожидается: да", snapshot.Message);
        Assert.Contains("Мониторинг: включён", snapshot.Message);
        Assert.Contains("тишина до", snapshot.Message);
    }

    [Fact]
    public void Build_handles_offline_and_missing_ip()
    {
        var facts = new HostStatusFacts(
            TimeSpan.FromMinutes(9),
            PrimaryIp: "  ",
            NetworkAvailable: false,
            DiskSummary: " ",
            RebootPending: false,
            IsMuted: false,
            MutedUntil: null,
            MonitoringPaused: true);

        var snapshot = StatusSnapshotBuilder.Build(facts);

        Assert.Contains("IP: -", snapshot.Message);
        Assert.Contains("Сеть: нет", snapshot.Message);
        Assert.Contains("Диск: н/д", snapshot.Message);
        Assert.Contains("Перезагрузка ожидается: нет", snapshot.Message);
        Assert.Contains("Тишина: нет", snapshot.Message);
        Assert.Contains("Мониторинг: пауза", snapshot.Message);
        Assert.Contains("Uptime: 9м", snapshot.Message);
    }
}
