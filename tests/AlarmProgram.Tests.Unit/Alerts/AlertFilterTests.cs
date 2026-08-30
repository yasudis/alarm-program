using AlarmProgram.Application.Alerts;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Alerts;

public class AlertFilterTests
{
    private readonly AlertFilter _filter = new();

    [Theory]
    [InlineData(MachineEventType.Startup, false, true, true, true)]
    [InlineData(MachineEventType.Shutdown, true, false, true, true)]
    [InlineData(MachineEventType.Restart, true, true, false, true)]
    [InlineData(MachineEventType.UnexpectedShutdown, true, true, true, false)]
    public void ShouldNotify_is_false_when_event_type_is_disabled(
        MachineEventType eventType,
        bool notifyStartup,
        bool notifyShutdown,
        bool notifyRestart,
        bool notifyUnexpected)
    {
        var settings = ValidTelegramSettings();
        settings.NotifyOnStartup = notifyStartup;
        settings.NotifyOnShutdown = notifyShutdown;
        settings.NotifyOnRestart = notifyRestart;
        settings.NotifyOnUnexpectedShutdown = notifyUnexpected;

        Assert.False(_filter.ShouldNotify(CreateEvent(eventType), settings));
    }

    [Theory]
    [InlineData(MachineEventType.Startup)]
    [InlineData(MachineEventType.Shutdown)]
    [InlineData(MachineEventType.Restart)]
    [InlineData(MachineEventType.UnexpectedShutdown)]
    public void ShouldNotify_is_true_when_type_and_telegram_channel_are_enabled(MachineEventType eventType)
    {
        Assert.True(_filter.ShouldNotify(CreateEvent(eventType), ValidTelegramSettings()));
    }

    [Fact]
    public void ShouldNotify_is_false_when_channels_are_disabled()
    {
        var settings = ValidTelegramSettings();
        settings.TelegramEnabled = false;

        Assert.False(_filter.ShouldNotify(CreateEvent(MachineEventType.Startup), settings));
    }

    [Fact]
    public void ShouldNotify_is_false_when_settings_are_invalid()
    {
        var settings = new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "not-a-token",
            TelegramChatId = "42"
        };

        Assert.False(settings.IsValid);
        Assert.False(_filter.ShouldNotify(CreateEvent(MachineEventType.Startup), settings));
    }

    [Fact]
    public void ShouldNotify_is_false_during_quiet_hours_for_non_critical_events()
    {
        var settings = ValidTelegramSettings();
        settings.QuietHoursEnabled = true;
        settings.QuietHoursStart = TimeSpan.FromHours(1);
        settings.QuietHoursEnd = TimeSpan.FromHours(1);

        var machineEvent = CreateEvent(MachineEventType.Startup);
        Assert.True(settings.IsWithinQuietHours(machineEvent.OccurredAt));
        Assert.False(_filter.ShouldNotify(machineEvent, settings));
    }

    [Fact]
    public void ShouldNotify_allows_unexpected_shutdown_during_quiet_hours()
    {
        var settings = ValidTelegramSettings();
        settings.QuietHoursEnabled = true;
        settings.QuietHoursStart = TimeSpan.FromHours(1);
        settings.QuietHoursEnd = TimeSpan.FromHours(1);

        Assert.True(_filter.ShouldNotify(CreateEvent(MachineEventType.UnexpectedShutdown), settings));
        Assert.True(_filter.ShouldNotify(CreateEvent(MachineEventType.BlueScreen), settings));
        Assert.True(_filter.ShouldNotify(CreateEvent(MachineEventType.DefenderThreat), settings));
    }

    [Fact]
    public void ShouldNotify_is_false_for_unknown_event_type()
    {
        Assert.False(_filter.ShouldNotify(CreateEvent(MachineEventType.Unknown), ValidTelegramSettings()));
    }

    [Fact]
    public void ShouldNotify_throws_when_arguments_are_null()
    {
        Assert.Throws<ArgumentNullException>(() => _filter.ShouldNotify(null!, ValidTelegramSettings()));
        Assert.Throws<ArgumentNullException>(() => _filter.ShouldNotify(CreateEvent(MachineEventType.Startup), null!));
    }

    private static UserSettings ValidTelegramSettings() => new()
    {
        TelegramEnabled = true,
        TelegramBotToken = "123456789:AAExampleTelegramBotTokenValue123456",
        TelegramChatId = "42"
    };

    private static MachineEvent CreateEvent(MachineEventType type) => new()
    {
        Type = type,
        OccurredAt = DateTimeOffset.UtcNow,
        Source = "System",
        HostName = "TEST-PC"
    };
}
