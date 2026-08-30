using AlarmProgram.Application.Alerts;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Alerts;

public class LocalAlertRulesTests
{
    [Theory]
    [InlineData(MachineEventType.UnexpectedShutdown, true)]
    [InlineData(MachineEventType.ProcessDown, true)]
    [InlineData(MachineEventType.ServiceDown, true)]
    [InlineData(MachineEventType.ApplicationHang, true)]
    [InlineData(MachineEventType.DefenderThreat, true)]
    [InlineData(MachineEventType.DiskError, true)]
    [InlineData(MachineEventType.Bsod, true)]
    [InlineData(MachineEventType.FirewallDisabled, true)]
    [InlineData(MachineEventType.HostUnreachable, true)]
    [InlineData(MachineEventType.FailedLogon, false)]
    [InlineData(MachineEventType.Startup, false)]
    public void ShouldPlaySound_covers_critical_watchdog_events(MachineEventType type, bool expected)
    {
        Assert.Equal(expected, LocalAlertRules.ShouldPlaySound(type));
    }

    [Theory]
    [InlineData(MachineEventType.UnexpectedShutdown, true)]
    [InlineData(MachineEventType.FailedLogon, true)]
    [InlineData(MachineEventType.ApplicationCrash, true)]
    [InlineData(MachineEventType.RebootPending, true)]
    [InlineData(MachineEventType.WindowsUpdateFailed, true)]
    [InlineData(MachineEventType.UserAccountCreated, true)]
    [InlineData(MachineEventType.AdminGroupChanged, true)]
    [InlineData(MachineEventType.Heartbeat, false)]
    public void ShouldShowBalloon_covers_critical_and_security_events(MachineEventType type, bool expected)
    {
        Assert.Equal(expected, LocalAlertRules.ShouldShowBalloon(type));
    }

    [Fact]
    public void ShouldRaiseLocally_works_without_notification_channels()
    {
        var filter = new AlertFilter();
        var settings = new UserSettings
        {
            NotifyOnUnexpectedShutdown = true,
            PlaySoundOnCriticalAlerts = true
        };

        Assert.False(settings.HasEnabledChannel);
        Assert.True(filter.ShouldRaiseLocally(
            CreateEvent(MachineEventType.UnexpectedShutdown),
            settings,
            muteState: null,
            lastSentOfType: null));
        Assert.False(filter.ShouldNotify(
            CreateEvent(MachineEventType.UnexpectedShutdown),
            settings));
    }

    [Fact]
    public void IsCritical_covers_security_and_bsod()
    {
        Assert.True(LocalAlertRules.IsCritical(MachineEventType.Bsod));
        Assert.True(LocalAlertRules.IsCritical(MachineEventType.AdminGroupChanged));
        Assert.True(LocalAlertRules.IsCritical(MachineEventType.HostUnreachable));
        Assert.False(LocalAlertRules.IsCritical(MachineEventType.Heartbeat));
        Assert.True(LocalAlertRules.BypassesAntiSpam(MachineEventType.Bsod));
        Assert.True(LocalAlertRules.BypassesAntiSpam(MachineEventType.UserAccountCreated));
    }

    private static MachineEvent CreateEvent(MachineEventType type) => new()
    {
        Type = type,
        OccurredAt = DateTimeOffset.UtcNow,
        Source = "Test",
        HostName = "TEST-PC"
    };
}
