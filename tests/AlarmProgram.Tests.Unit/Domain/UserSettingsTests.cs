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

    [Fact]
    public void IsEventEnabled_covers_service_usb_and_digest()
    {
        var settings = new UserSettings
        {
            NotifyOnServiceDown = true,
            NotifyOnUsbConnected = true,
            NotifyOnUsbDisconnected = false,
            DailyDigestEnabled = true,
            NotifyOnFailedLogon = false,
            NotifyOnApplicationCrash = true,
            NotifyOnRebootPending = false
        };

        Assert.True(settings.IsEventEnabled(MachineEventType.ServiceDown));
        Assert.True(settings.IsEventEnabled(MachineEventType.UsbConnected));
        Assert.False(settings.IsEventEnabled(MachineEventType.UsbDisconnected));
        Assert.True(settings.IsEventEnabled(MachineEventType.DailyDigest));
        Assert.False(settings.IsEventEnabled(MachineEventType.FailedLogon));
        Assert.True(settings.IsEventEnabled(MachineEventType.ApplicationCrash));
        Assert.False(settings.IsEventEnabled(MachineEventType.RebootPending));
    }

    [Fact]
    public void IsEventEnabled_covers_package9_security_and_watchdogs()
    {
        var settings = new UserSettings
        {
            NotifyOnBlueScreen = true,
            NotifyOnWindowsUpdateFailed = false,
            NotifyOnDefenderThreat = true,
            NotifyOnAdminGroupChanged = false,
            NotifyOnHostUnreachable = true,
            NotifyOnHttpEndpointDown = false
        };

        Assert.True(settings.IsEventEnabled(MachineEventType.BlueScreen));
        Assert.False(settings.IsEventEnabled(MachineEventType.WindowsUpdateFailed));
        Assert.True(settings.IsEventEnabled(MachineEventType.DefenderThreat));
        Assert.False(settings.IsEventEnabled(MachineEventType.AdminGroupChanged));
        Assert.True(settings.IsEventEnabled(MachineEventType.HostUnreachable));
        Assert.False(settings.IsEventEnabled(MachineEventType.HttpEndpointDown));
    }

    [Fact]
    public void ParseWatchedHosts_strips_urls_and_dedupes()
    {
        var hosts = UserSettings.ParseWatchedHosts("8.8.8.8, https://nas.local/ping, NAS.local");

        Assert.Equal(new[] { "8.8.8.8", "nas.local" }, hosts);
    }

    [Fact]
    public void Validate_requires_hosts_when_ping_watchdog_enabled()
    {
        var settings = new UserSettings { NotifyOnHostUnreachable = true };

        Assert.False(settings.IsValid);
        Assert.Contains(settings.Validate(), error => error.Contains("хост", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_accepts_ping_and_http_watchdogs()
    {
        var settings = new UserSettings
        {
            NotifyOnHostUnreachable = true,
            WatchedHosts = "8.8.8.8, gateway.local",
            NotifyOnHttpEndpointDown = true,
            WatchedHttpEndpoints = "https://example.com/health, http://192.168.1.1"
        };

        Assert.True(settings.IsValid);
        Assert.Equal(2, settings.GetWatchedHosts().Count);
        Assert.Equal(2, settings.GetWatchedHttpEndpoints().Count);
    }

    [Fact]
    public void Validate_rejects_invalid_http_endpoint()
    {
        var settings = new UserSettings
        {
            NotifyOnHttpEndpointDown = true,
            WatchedHttpEndpoints = "ftp://example.com/health"
        };

        Assert.False(settings.IsValid);
        Assert.Contains(settings.Validate(), error => error.Contains("HTTP URL", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HasEnabledChannel_includes_email()
    {
        var settings = new UserSettings
        {
            EmailEnabled = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpFrom = "alerts@example.com",
            SmtpTo = "ops@example.com"
        };

        Assert.True(settings.HasEnabledChannel);
        Assert.True(settings.IsValid);
    }

    [Fact]
    public void Validate_requires_smtp_fields_when_email_is_enabled()
    {
        var settings = new UserSettings { EmailEnabled = true };

        Assert.False(settings.IsValid);
        Assert.Contains(settings.Validate(), error => error.Contains("SMTP-хост", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseEmailAddresses_splits_and_dedupes()
    {
        var emails = UserSettings.ParseEmailAddresses("ops@example.com, a@b.co, ops@example.com");

        Assert.Equal(new[] { "ops@example.com", "a@b.co" }, emails);
    }

    [Fact]
    public void Validate_requires_service_names_when_service_watchdog_enabled()
    {
        var settings = new UserSettings { NotifyOnServiceDown = true };

        Assert.False(settings.IsValid);
        Assert.Contains(settings.Validate(), error => error.Contains("службы", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseWatchedServiceNames_trims_and_dedupes()
    {
        var names = UserSettings.ParseWatchedServiceNames("Spooler, wuauserv, Spooler");

        Assert.Equal(new[] { "Spooler", "wuauserv" }, names);
    }

    [Fact]
    public void Validate_rejects_invalid_journal_retention()
    {
        var settings = new UserSettings { JournalRetentionDays = 400 };

        Assert.False(settings.IsValid);
        Assert.Contains(settings.Validate(), error => error.Contains("журнала", StringComparison.OrdinalIgnoreCase));
    }
}
