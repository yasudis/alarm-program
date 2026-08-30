namespace AlarmProgram.Infrastructure.Settings;

internal sealed class SettingsFileDto
{
    public string TelegramBotToken { get; set; } = string.Empty;

    public string TelegramChatId { get; set; } = string.Empty;

    public string? DiscordWebhookUrl { get; set; }

    public string? WebhookUrl { get; set; }

    public bool TelegramEnabled { get; set; }

    public bool DiscordEnabled { get; set; }

    public bool WebhookEnabled { get; set; }

    public bool EmailEnabled { get; set; }

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public string SmtpUser { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public string SmtpFrom { get; set; } = string.Empty;

    public string SmtpTo { get; set; } = string.Empty;

    public bool SmtpUseSsl { get; set; } = true;

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

    public bool NotifyOnProcessDown { get; set; }

    public string WatchedProcessNames { get; set; } = string.Empty;

    public bool NotifyOnHighCpu { get; set; }

    public bool NotifyOnHighMemory { get; set; }

    public int HighCpuThresholdPercent { get; set; } = 90;

    public int HighMemoryThresholdPercent { get; set; } = 90;

    public bool NotifyOnRdpConnected { get; set; }

    public bool NotifyOnRdpDisconnected { get; set; }

    public bool NotifyOnServiceDown { get; set; }

    public string WatchedServiceNames { get; set; } = string.Empty;

    public bool NotifyOnUsbConnected { get; set; }

    public bool NotifyOnUsbDisconnected { get; set; }

    public bool DailyDigestEnabled { get; set; }

    public string DailyDigestTime { get; set; } = "09:00";

    public int JournalRetentionDays { get; set; }

    public bool NotifyOnFailedLogon { get; set; } = true;

    public bool NotifyOnApplicationCrash { get; set; } = true;

    public bool NotifyOnRebootPending { get; set; } = true;

    public bool NotifyOnApplicationHang { get; set; } = true;

    public bool NotifyOnDefenderThreat { get; set; } = true;

    public bool NotifyOnWindowsUpdateFailed { get; set; } = true;

    public bool NotifyOnDiskError { get; set; } = true;

    public bool NotifyOnStatusSnapshot { get; set; } = true;

    public int StartupGracePeriodMinutes { get; set; }

    public int MaxAlertsPerHour { get; set; }

    public bool PlaySoundOnCriticalAlerts { get; set; } = true;

    public bool ShowTrayBalloonOnCriticalAlerts { get; set; } = true;
}
