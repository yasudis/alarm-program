using System.Text.Json;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Domain;

public class UserSettingsTests
{
    [Fact]
    public void Default_settings_are_valid_until_channels_are_enabled()
    {
        var settings = new UserSettings();

        Assert.True(settings.IsValid);
        Assert.Empty(settings.Validate());
        Assert.True(settings.NotifyOnStartup);
        Assert.True(settings.NotifyOnShutdown);
        Assert.True(settings.NotifyOnRestart);
        Assert.True(settings.NotifyOnUnexpectedShutdown);
    }

    [Fact]
    public void Validate_requires_telegram_token_and_chat_id_when_telegram_is_enabled()
    {
        var settings = new UserSettings { TelegramEnabled = true };

        var errors = settings.Validate();

        Assert.False(settings.IsValid);
        Assert.Contains(errors, error => error.Contains("token", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("chat id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_accepts_well_formed_telegram_and_discord_settings()
    {
        var settings = new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "123456789:AAExampleTelegramBotTokenValue123456",
            TelegramChatId = "-1001234567890",
            DiscordEnabled = true,
            DiscordWebhookUrl = "https://discord.com/api/webhooks/123/abc"
        };

        Assert.True(settings.IsValid);
    }

    [Fact]
    public void Validate_rejects_invalid_discord_webhook()
    {
        var settings = new UserSettings
        {
            DiscordEnabled = true,
            DiscordWebhookUrl = "https://example.com/not-a-webhook"
        };

        Assert.False(settings.IsValid);
        Assert.Contains(settings.Validate(), error => error.Contains("Discord webhook", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UserSettings_can_be_serialized_and_deserialized()
    {
        var settings = new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "123456789:AAExampleTelegramBotTokenValue123456",
            TelegramChatId = "42",
            NotifyOnShutdown = false
        };

        var json = JsonSerializer.Serialize(settings);
        var restored = JsonSerializer.Deserialize<UserSettings>(json);

        Assert.NotNull(restored);
        Assert.Equal(settings.TelegramBotToken, restored.TelegramBotToken);
        Assert.Equal(settings.TelegramChatId, restored.TelegramChatId);
        Assert.False(restored.NotifyOnShutdown);
        Assert.True(restored.TelegramEnabled);
    }

    [Fact]
    public void IsEventEnabled_respects_notification_flags()
    {
        var settings = new UserSettings
        {
            NotifyOnStartup = true,
            NotifyOnShutdown = false,
            NotifyOnRestart = true,
            NotifyOnUnexpectedShutdown = false,
            NotifyOnUserLogon = true,
            HeartbeatEnabled = false
        };

        Assert.True(settings.IsEventEnabled(MachineEventType.Startup));
        Assert.False(settings.IsEventEnabled(MachineEventType.Shutdown));
        Assert.True(settings.IsEventEnabled(MachineEventType.Restart));
        Assert.False(settings.IsEventEnabled(MachineEventType.UnexpectedShutdown));
        Assert.True(settings.IsEventEnabled(MachineEventType.UserLogon));
        Assert.False(settings.IsEventEnabled(MachineEventType.Heartbeat));
        Assert.False(settings.IsEventEnabled(MachineEventType.Unknown));
    }

    [Fact]
    public void IsWithinQuietHours_handles_overnight_window()
    {
        var settings = new UserSettings
        {
            QuietHoursEnabled = true,
            QuietHoursStart = TimeSpan.FromHours(23),
            QuietHoursEnd = TimeSpan.FromHours(7)
        };

        var today = DateTime.Today;
        var late = new DateTimeOffset(DateTime.SpecifyKind(today.AddHours(23).AddMinutes(30), DateTimeKind.Local));
        var early = new DateTimeOffset(DateTime.SpecifyKind(today.AddHours(3), DateTimeKind.Local));
        var day = new DateTimeOffset(DateTime.SpecifyKind(today.AddHours(12), DateTimeKind.Local));

        Assert.True(settings.IsWithinQuietHours(late));
        Assert.True(settings.IsWithinQuietHours(early));
        Assert.False(settings.IsWithinQuietHours(day));
    }

    [Fact]
    public void Validate_rejects_invalid_heartbeat_interval()
    {
        var settings = new UserSettings
        {
            HeartbeatEnabled = true,
            HeartbeatIntervalMinutes = 2
        };

        Assert.Contains(settings.Validate(), error => error.Contains("heartbeat", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IsEventEnabled_covers_network_and_resume_flags()
    {
        var settings = new UserSettings
        {
            NotifyOnIpChange = true,
            NotifyOnNetworkOffline = false,
            NotifyOnNetworkOnline = true,
            NotifyOnSystemResume = true,
            NotifyOnUserLogoff = true
        };

        Assert.True(settings.IsEventEnabled(MachineEventType.IpChanged));
        Assert.False(settings.IsEventEnabled(MachineEventType.NetworkOffline));
        Assert.True(settings.IsEventEnabled(MachineEventType.NetworkOnline));
        Assert.True(settings.IsEventEnabled(MachineEventType.SystemResume));
        Assert.True(settings.IsEventEnabled(MachineEventType.UserLogoff));
    }

    [Fact]
    public void ParseTelegramChatIds_splits_comma_separated_values()
    {
        var ids = UserSettings.ParseTelegramChatIds("42, -100123, @mychannel");

        Assert.Equal(new[] { "42", "-100123", "@mychannel" }, ids);
    }

    [Fact]
    public void Validate_accepts_multiple_telegram_chat_ids()
    {
        var settings = new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "123456789:AAExampleTelegramBotTokenValue123456",
            TelegramChatId = "42, -1001234567890"
        };

        Assert.True(settings.IsValid);
        Assert.Equal(2, settings.GetTelegramChatIds().Count);
    }

    [Fact]
    public void Validate_rejects_invalid_chat_id_in_list()
    {
        var settings = new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "123456789:AAExampleTelegramBotTokenValue123456",
            TelegramChatId = "42, not-a-chat"
        };

        Assert.False(settings.IsValid);
        Assert.Contains(settings.Validate(), error => error.Contains("chat id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IsEventEnabled_covers_session_disk_and_power_flags()
    {
        var settings = new UserSettings
        {
            NotifyOnSessionLock = true,
            NotifyOnSessionUnlock = false,
            NotifyOnLowDiskSpace = true,
            NotifyOnBatteryLow = false,
            NotifyOnAcPowerLost = true,
            NotifyOnAcPowerRestored = false
        };

        Assert.True(settings.IsEventEnabled(MachineEventType.SessionLock));
        Assert.False(settings.IsEventEnabled(MachineEventType.SessionUnlock));
        Assert.True(settings.IsEventEnabled(MachineEventType.LowDiskSpace));
        Assert.False(settings.IsEventEnabled(MachineEventType.BatteryLow));
        Assert.True(settings.IsEventEnabled(MachineEventType.AcPowerLost));
        Assert.False(settings.IsEventEnabled(MachineEventType.AcPowerRestored));
    }

    [Fact]
    public void HasEnabledChannel_includes_https_webhook()
    {
        var settings = new UserSettings
        {
            WebhookEnabled = true,
            WebhookUrl = "https://example.com/hooks/alarm"
        };

        Assert.True(settings.HasEnabledChannel);
        Assert.True(settings.IsValid);
    }

    [Fact]
    public void Validate_rejects_http_webhook()
    {
        var settings = new UserSettings
        {
            WebhookEnabled = true,
            WebhookUrl = "http://example.com/hook"
        };

        Assert.False(settings.IsValid);
        Assert.Contains(settings.Validate(), error => error.Contains("HTTPS webhook", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseWatchedProcessNames_strips_exe_and_deduplicates()
    {
        var names = UserSettings.ParseWatchedProcessNames("nginx.exe, notepad, NGINX");

        Assert.Equal(new[] { "nginx", "notepad" }, names);
    }

    [Fact]
    public void Validate_requires_process_names_when_watchdog_enabled()
    {
        var settings = new UserSettings { NotifyOnProcessDown = true };

        Assert.False(settings.IsValid);
        Assert.Contains(settings.Validate(), error => error.Contains("процесса", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IsEventEnabled_covers_watchdog_resources_and_rdp()
    {
        var settings = new UserSettings
        {
            NotifyOnProcessDown = true,
            NotifyOnHighCpu = true,
            NotifyOnHighMemory = false,
            NotifyOnRdpConnected = true,
            NotifyOnRdpDisconnected = false
        };

        Assert.True(settings.IsEventEnabled(MachineEventType.ProcessDown));
        Assert.True(settings.IsEventEnabled(MachineEventType.HighCpu));
        Assert.False(settings.IsEventEnabled(MachineEventType.HighMemory));
        Assert.True(settings.IsEventEnabled(MachineEventType.RdpConnected));
        Assert.False(settings.IsEventEnabled(MachineEventType.RdpDisconnected));
    }
}
