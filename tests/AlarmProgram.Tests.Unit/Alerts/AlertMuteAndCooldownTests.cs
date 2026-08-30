using AlarmProgram.Application.Alerts;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Alerts;

public class AlertMuteAndCooldownTests
{
    private readonly AlertFilter _filter = new();

    [Fact]
    public void ShouldNotify_is_false_when_muted_for_regular_events()
    {
        var mute = new AlertMuteState();
        mute.MuteFor(TimeSpan.FromMinutes(30));

        Assert.False(_filter.ShouldNotify(CreateEvent(MachineEventType.Startup), ValidSettings(), mute, lastSentOfType: null));
        Assert.True(_filter.ShouldNotify(CreateEvent(MachineEventType.UnexpectedShutdown), ValidSettings(), mute, lastSentOfType: null));
        Assert.True(_filter.ShouldNotify(CreateEvent(MachineEventType.BlueScreen), ValidSettings(), mute, lastSentOfType: null));
        Assert.True(_filter.ShouldNotify(CreateEvent(MachineEventType.DefenderThreat), ValidSettings(), mute, lastSentOfType: null));
    }

    [Fact]
    public void ShouldNotify_is_true_after_mute_is_cleared()
    {
        var mute = new AlertMuteState();
        mute.MuteFor(TimeSpan.FromMinutes(30));
        mute.ClearMute();

        Assert.True(_filter.ShouldNotify(CreateEvent(MachineEventType.Startup), ValidSettings(), mute, lastSentOfType: null));
    }

    [Fact]
    public void ShouldNotify_respects_cooldown_window()
    {
        var settings = ValidSettings();
        settings.AlertCooldownMinutes = 10;
        var lastSent = DateTimeOffset.UtcNow.AddMinutes(-2);

        Assert.False(_filter.ShouldNotify(
            CreateEvent(MachineEventType.Heartbeat),
            settings,
            muteState: null,
            lastSentOfType: lastSent));
        Assert.True(_filter.ShouldNotify(
            CreateEvent(MachineEventType.UnexpectedShutdown),
            settings,
            muteState: null,
            lastSentOfType: lastSent));
    }

    [Fact]
    public void ShouldNotify_allows_event_after_cooldown_expires()
    {
        var settings = ValidSettings();
        settings.AlertCooldownMinutes = 5;
        var lastSent = DateTimeOffset.UtcNow.AddMinutes(-6);

        Assert.True(_filter.ShouldNotify(
            CreateEvent(MachineEventType.LowDiskSpace),
            settings,
            muteState: null,
            lastSentOfType: lastSent));
    }

    private static UserSettings ValidSettings() => new()
    {
        TelegramEnabled = true,
        TelegramBotToken = "123456789:AAExampleTelegramBotTokenValue123456",
        TelegramChatId = "42",
        NotifyOnLowDiskSpace = true
    };

    private static MachineEvent CreateEvent(MachineEventType type) => new()
    {
        Type = type,
        OccurredAt = DateTimeOffset.UtcNow,
        Source = "Test",
        HostName = "TEST-PC"
    };
}
