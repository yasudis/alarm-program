using AlarmProgram.Application.Alerts;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Alerts;

public class AlertMuteUntilTests
{
    [Fact]
    public void MuteUntil_suppresses_regular_events_until_deadline()
    {
        var mute = new AlertMuteState();
        var until = DateTimeOffset.UtcNow.AddHours(2);
        mute.MuteUntil(until);

        Assert.True(mute.IsMuted);
        Assert.True(mute.IsActiveAt(DateTimeOffset.UtcNow.AddMinutes(5)));
        Assert.False(mute.IsActiveAt(until.AddMinutes(1)));

        var filter = new AlertFilter();
        Assert.False(filter.ShouldNotify(
            CreateEvent(MachineEventType.Startup),
            ValidSettings(),
            mute,
            lastSentOfType: null));
        Assert.True(filter.ShouldNotify(
            CreateEvent(MachineEventType.UnexpectedShutdown),
            ValidSettings(),
            mute,
            lastSentOfType: null));
    }

    [Fact]
    public void MuteUntil_in_the_past_clears_mute()
    {
        var mute = new AlertMuteState();
        mute.MuteFor(TimeSpan.FromMinutes(30));
        mute.MuteUntil(DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.False(mute.IsMuted);
    }

    private static UserSettings ValidSettings() => new()
    {
        TelegramEnabled = true,
        TelegramBotToken = "123456789:AAExampleTelegramBotTokenValue123456",
        TelegramChatId = "42"
    };

    private static MachineEvent CreateEvent(MachineEventType type) => new()
    {
        Type = type,
        OccurredAt = DateTimeOffset.UtcNow,
        Source = "Test",
        HostName = "TEST-PC"
    };
}
