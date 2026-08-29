using System.Text.RegularExpressions;

namespace AlarmProgram.Domain;

public sealed class UserSettings
{
    private static readonly Regex TelegramTokenPattern =
        new(@"^\d{8,12}:[A-Za-z0-9_-]{30,}$", RegexOptions.Compiled);

    private static readonly Regex TelegramChatIdPattern =
        new(@"^(@[A-Za-z][A-Za-z0-9_]{4,}|-?\d{1,20})$", RegexOptions.Compiled);

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

    public bool NotifyOnSessionLock { get; set; }

    public bool NotifyOnSessionUnlock { get; set; }

    public bool NotifyOnLowDiskSpace { get; set; } = true;

    public bool NotifyOnBatteryLow { get; set; } = true;

    public bool NotifyOnAcPowerLost { get; set; } = true;

    public bool NotifyOnAcPowerRestored { get; set; } = true;

    public int LowDiskSpaceThresholdPercent { get; set; } = 10;

    public int BatteryLowThresholdPercent { get; set; } = 15;

    public int AlertCooldownMinutes { get; set; }

    public bool HeartbeatEnabled { get; set; }

    public int HeartbeatIntervalMinutes { get; set; } = 60;

    public bool QuietHoursEnabled { get; set; }

    public TimeSpan QuietHoursStart { get; set; } = TimeSpan.FromHours(23);

    public TimeSpan QuietHoursEnd { get; set; } = TimeSpan.FromHours(7);

    public bool RunAtWindowsStartup { get; set; }

    public bool MinimizeToTray { get; set; } = true;

    public string DisplayName { get; set; } = string.Empty;

    public string? AlertBodyTemplate { get; set; }

    public bool IsValid => Validate().Count == 0;

    public bool HasEnabledChannel => TelegramEnabled || DiscordEnabled;

    public bool IsEventEnabled(MachineEventType eventType) => eventType switch
    {
        MachineEventType.Startup => NotifyOnStartup,
        MachineEventType.Shutdown => NotifyOnShutdown,
        MachineEventType.Restart => NotifyOnRestart,
        MachineEventType.UnexpectedShutdown => NotifyOnUnexpectedShutdown,
        MachineEventType.UserLogon => NotifyOnUserLogon,
        MachineEventType.UserLogoff => NotifyOnUserLogoff,
        MachineEventType.Heartbeat => HeartbeatEnabled,
        MachineEventType.IpChanged => NotifyOnIpChange,
        MachineEventType.NetworkOffline => NotifyOnNetworkOffline,
        MachineEventType.NetworkOnline => NotifyOnNetworkOnline,
        MachineEventType.SystemResume => NotifyOnSystemResume,
        MachineEventType.SessionLock => NotifyOnSessionLock,
        MachineEventType.SessionUnlock => NotifyOnSessionUnlock,
        MachineEventType.LowDiskSpace => NotifyOnLowDiskSpace,
        MachineEventType.BatteryLow => NotifyOnBatteryLow,
        MachineEventType.AcPowerLost => NotifyOnAcPowerLost,
        MachineEventType.AcPowerRestored => NotifyOnAcPowerRestored,
        _ => false
    };

    public IReadOnlyList<string> GetTelegramChatIds() => ParseTelegramChatIds(TelegramChatId);

    public static IReadOnlyList<string> ParseTelegramChatIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw
            .Split([',', ';', '\n', '\r', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public bool IsWithinQuietHours(DateTimeOffset timestamp)
    {
        if (!QuietHoursEnabled)
        {
            return false;
        }

        var localTime = timestamp.ToLocalTime().TimeOfDay;
        if (QuietHoursStart == QuietHoursEnd)
        {
            return true;
        }

        if (QuietHoursStart < QuietHoursEnd)
        {
            return localTime >= QuietHoursStart && localTime < QuietHoursEnd;
        }

        // Overnight window, e.g. 23:00 -> 07:00
        return localTime >= QuietHoursStart || localTime < QuietHoursEnd;
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (TelegramEnabled)
        {
            if (string.IsNullOrWhiteSpace(TelegramBotToken))
            {
                errors.Add("Telegram bot token обязателен.");
            }
            else if (!TelegramTokenPattern.IsMatch(TelegramBotToken.Trim()))
            {
                errors.Add("Некорректный формат Telegram bot token.");
            }

            var chatIds = GetTelegramChatIds();
            if (chatIds.Count == 0)
            {
                errors.Add("Telegram chat id обязателен.");
            }
            else if (chatIds.Any(id => !TelegramChatIdPattern.IsMatch(id)))
            {
                errors.Add("Некорректный формат Telegram chat id. Можно несколько через запятую.");
            }
        }

        if (DiscordEnabled)
        {
            if (string.IsNullOrWhiteSpace(DiscordWebhookUrl))
            {
                errors.Add("Discord webhook URL обязателен.");
            }
            else if (!IsValidDiscordWebhook(DiscordWebhookUrl))
            {
                errors.Add("Некорректный формат Discord webhook URL.");
            }
        }

        if (HeartbeatEnabled && (HeartbeatIntervalMinutes < 5 || HeartbeatIntervalMinutes > 1440))
        {
            errors.Add("Интервал heartbeat должен быть от 5 до 1440 минут.");
        }

        if (QuietHoursEnabled)
        {
            if (QuietHoursStart < TimeSpan.Zero || QuietHoursStart >= TimeSpan.FromDays(1))
            {
                errors.Add("Некорректное время начала тихих часов.");
            }

            if (QuietHoursEnd < TimeSpan.Zero || QuietHoursEnd >= TimeSpan.FromDays(1))
            {
                errors.Add("Некорректное время окончания тихих часов.");
            }
        }

        if (!string.IsNullOrWhiteSpace(DisplayName) && DisplayName.Trim().Length > 64)
        {
            errors.Add("Отображаемое имя устройства не должно превышать 64 символа.");
        }

        if (!string.IsNullOrWhiteSpace(AlertBodyTemplate) && AlertBodyTemplate.Length > 4000)
        {
            errors.Add("Шаблон сообщения слишком длинный (макс. 4000 символов).");
        }

        if (LowDiskSpaceThresholdPercent is < 1 or > 50)
        {
            errors.Add("Порог свободного места на диске должен быть от 1 до 50%.");
        }

        if (BatteryLowThresholdPercent is < 1 or > 50)
        {
            errors.Add("Порог низкого заряда батареи должен быть от 1 до 50%.");
        }

        if (AlertCooldownMinutes is < 0 or > 1440)
        {
            errors.Add("Cooldown алертов должен быть от 0 до 1440 минут.");
        }

        return errors;
    }

    private static bool IsValidDiscordWebhook(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var host = uri.Host;
        var isDiscordHost =
            host.Equals("discord.com", StringComparison.OrdinalIgnoreCase)
            || host.Equals("discordapp.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".discord.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".discordapp.com", StringComparison.OrdinalIgnoreCase);

        return isDiscordHost
            && uri.AbsolutePath.StartsWith("/api/webhooks/", StringComparison.OrdinalIgnoreCase);
    }
}
