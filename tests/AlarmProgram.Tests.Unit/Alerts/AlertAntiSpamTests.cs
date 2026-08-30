using AlarmProgram.Application.Alerts;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Alerts;

public class AlertAntiSpamTests
{
    private readonly AlertFilter _filter = new();

    [Fact]
    public void ShouldNotify_suppresses_regular_events_during_startup_grace()
    {
        var settings = ValidSettings();
        settings.StartupGracePeriodMinutes = 5;
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        Assert.False(_filter.ShouldNotify(
            CreateEvent(MachineEventType.Startup),
            settings,
            muteState: null,
            lastSentOfType: null,
            now: DateTimeOffset.UtcNow,
            monitoringStartedAt: startedAt));
    }

    [Fact]
    public void ShouldNotify_allows_hang_and_defender_during_startup_grace()
    {
        var settings = ValidSettings();
        settings.StartupGracePeriodMinutes = 5;
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        Assert.True(_filter.ShouldNotify(
            CreateEvent(MachineEventType.ApplicationHang),
            settings,
            muteState: null,
            lastSentOfType: null,
            now: DateTimeOffset.UtcNow,
            monitoringStartedAt: startedAt));
        Assert.True(_filter.ShouldNotify(
            CreateEvent(MachineEventType.DefenderThreat),
            settings,
            muteState: null,
            lastSentOfType: null,
            now: DateTimeOffset.UtcNow,
            monitoringStartedAt: startedAt));
    }

    [Fact]
    public void ShouldNotify_allows_regular_event_after_grace_expires()
    {
        var settings = ValidSettings();
        settings.StartupGracePeriodMinutes = 2;
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-3);

        Assert.True(_filter.ShouldNotify(
            CreateEvent(MachineEventType.Heartbeat),
            settings,
            muteState: null,
            lastSentOfType: null,
            now: DateTimeOffset.UtcNow,
            monitoringStartedAt: startedAt));
    }

    [Fact]
    public void ShouldNotify_enforces_hourly_rate_limit_for_regular_events()
    {
        var settings = ValidSettings();
        settings.MaxAlertsPerHour = 3;

        Assert.False(_filter.ShouldNotify(
            CreateEvent(MachineEventType.Startup),
            settings,
            muteState: null,
            lastSentOfType: null,
            sentCountInLastHour: 3));
        Assert.True(_filter.ShouldNotify(
            CreateEvent(MachineEventType.DiskError),
            settings,
            muteState: null,
            lastSentOfType: null,
            sentCountInLastHour: 3));
    }

    [Fact]
    public void ShouldNotify_status_snapshot_bypasses_mute_and_rate_limit()
    {
        var mute = new AlertMuteState();
        mute.MuteFor(TimeSpan.FromMinutes(30));
        var settings = ValidSettings();
        settings.MaxAlertsPerHour = 1;
        settings.QuietHoursEnabled = true;
        settings.QuietHoursStart = TimeSpan.FromHours(1);
        settings.QuietHoursEnd = TimeSpan.FromHours(1);

        Assert.True(_filter.ShouldNotify(
            CreateEvent(MachineEventType.StatusSnapshot),
            settings,
            mute,
            lastSentOfType: DateTimeOffset.UtcNow,
            sentCountInLastHour: 50));
    }

    [Fact]
    public void ShouldNotify_status_snapshot_respects_disabled_flag()
    {
        var settings = ValidSettings();
        settings.NotifyOnStatusSnapshot = false;

        Assert.False(_filter.ShouldNotify(CreateEvent(MachineEventType.StatusSnapshot), settings));
    }

    [Fact]
    public void ShouldNotify_critical_only_mode_drops_regular_events()
    {
        var settings = ValidSettings();
        settings.CriticalAlertsOnly = true;

        Assert.False(_filter.ShouldNotify(CreateEvent(MachineEventType.Startup), settings));
        Assert.True(_filter.ShouldNotify(CreateEvent(MachineEventType.Bsod), settings));
        Assert.True(_filter.ShouldNotify(CreateEvent(MachineEventType.StatusSnapshot), settings));
    }

    private static UserSettings ValidSettings() => new()
    {
        TelegramEnabled = true,
        TelegramBotToken = "123456789:AAExampleTelegramBotTokenValue123456",
        TelegramChatId = "42",
        NotifyOnApplicationHang = true,
        NotifyOnDefenderThreat = true,
        NotifyOnDiskError = true,
        NotifyOnStatusSnapshot = true,
        NotifyOnBsod = true,
        HeartbeatEnabled = true
    };

    private static MachineEvent CreateEvent(MachineEventType type) => new()
    {
        Type = type,
        OccurredAt = DateTimeOffset.UtcNow,
        Source = "Test",
        HostName = "TEST-PC"
    };
}
