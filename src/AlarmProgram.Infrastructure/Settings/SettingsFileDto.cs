namespace AlarmProgram.Infrastructure.Settings;

internal sealed class SettingsFileDto
{
    public string TelegramBotToken { get; set; } = string.Empty;

    public string TelegramChatId { get; set; } = string.Empty;

    public string? DiscordWebhookUrl { get; set; }

    public bool TelegramEnabled { get; set; }

    public bool DiscordEnabled { get; set; }

    public bool NotifyOnStartup { get; set; } = true;

    public bool NotifyOnShutdown { get; set; } = true;

    public bool NotifyOnRestart { get; set; } = true;

    public bool NotifyOnUnexpectedShutdown { get; set; } = true;

    public bool NotifyOnUserLogon { get; set; }

    public bool NotifyOnUserLogoff { get; set; }

    public bool NotifyOnIpChange { get; set; }

    public bool NotifyOnNetworkOffline { get; set; } = true;

    public bool NotifyOnNetworkOnline { get; set; } = true;

    public bool NotifyOnSystemResume { get; set; }

    public bool HeartbeatEnabled { get; set; }

    public int HeartbeatIntervalMinutes { get; set; } = 60;

    public bool QuietHoursEnabled { get; set; }

    public string QuietHoursStart { get; set; } = "23:00";

    public string QuietHoursEnd { get; set; } = "07:00";

    public bool RunAtWindowsStartup { get; set; }

    public bool MinimizeToTray { get; set; } = true;

    public string DisplayName { get; set; } = string.Empty;

    public string? AlertBodyTemplate { get; set; }

    public bool NotifyOnSessionLock { get; set; }

    public bool NotifyOnSessionUnlock { get; set; }

    public bool NotifyOnLowDiskSpace { get; set; } = true;

    public bool NotifyOnBatteryLow { get; set; } = true;

    public bool NotifyOnAcPowerLost { get; set; } = true;

    public bool NotifyOnAcPowerRestored { get; set; } = true;

    public int LowDiskSpaceThresholdPercent { get; set; } = 10;

    public int BatteryLowThresholdPercent { get; set; } = 15;

    public int AlertCooldownMinutes { get; set; }
}
