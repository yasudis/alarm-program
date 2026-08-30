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

    public string? WebhookUrl { get; set; }

    public bool TelegramEnabled { get; set; }

    public bool DiscordEnabled { get; set; }

    public bool WebhookEnabled { get; set; }

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

    public TimeSpan DailyDigestTime { get; set; } = TimeSpan.FromHours(9);

    public int JournalRetentionDays { get; set; }

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

    public bool HasEnabledChannel => TelegramEnabled || DiscordEnabled || WebhookEnabled;

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
        MachineEventType.ProcessDown => NotifyOnProcessDown,
        MachineEventType.HighCpu => NotifyOnHighCpu,
        MachineEventType.HighMemory => NotifyOnHighMemory,
        MachineEventType.RdpConnected => NotifyOnRdpConnected,
        MachineEventType.RdpDisconnected => NotifyOnRdpDisconnected,
        MachineEventType.ServiceDown => NotifyOnServiceDown,
        MachineEventType.UsbConnected => NotifyOnUsbConnected,
        MachineEventType.UsbDisconnected => NotifyOnUsbDisconnected,
        MachineEventType.DailyDigest => DailyDigestEnabled,
        _ => false
    };

    public IReadOnlyList<string> GetTelegramChatIds() => ParseTelegramChatIds(TelegramChatId);

    public IReadOnlyList<string> GetWatchedProcessNames() => ParseWatchedProcessNames(WatchedProcessNames);

    public IReadOnlyList<string> GetWatchedServiceNames() => ParseWatchedServiceNames(WatchedServiceNames);

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

    public static IReadOnlyList<string> ParseWatchedProcessNames(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeProcessName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<string> ParseWatchedServiceNames(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeServiceName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string NormalizeProcessName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmed = name.Trim().Trim('"');
        if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^4];
        }

        return trimmed;
    }

    public static string NormalizeServiceName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return name.Trim().Trim('"');
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

        if (WebhookEnabled)
        {
            if (string.IsNullOrWhiteSpace(WebhookUrl))
            {
                errors.Add("HTTPS webhook URL обязателен.");
            }
            else if (!IsValidHttpsWebhook(WebhookUrl))
            {
                errors.Add("Некорректный формат HTTPS webhook URL.");
            }
        }

        if (NotifyOnProcessDown)
        {
            var processNames = GetWatchedProcessNames();
            if (processNames.Count == 0)
            {
                errors.Add("Укажите хотя бы одно имя процесса для watchdog.");
            }
            else if (processNames.Count > 10)
            {
                errors.Add("Можно указать не более 10 процессов.");
            }
            else if (processNames.Any(name => name.Length > 64 || name.Contains('\\') || name.Contains('/') || name.Contains(':')))
            {
                errors.Add("Некорректное имя процесса. Укажите имя без пути, например nginx или notepad.");
            }
        }

        if (NotifyOnServiceDown)
        {
            var serviceNames = GetWatchedServiceNames();
            if (serviceNames.Count == 0)
            {
                errors.Add("Укажите хотя бы одно имя службы для watchdog.");
            }
            else if (serviceNames.Count > 10)
            {
                errors.Add("Можно указать не более 10 служб.");
            }
            else if (serviceNames.Any(name => name.Length > 128 || name.Contains('\\') || name.Contains('/') || name.Contains(':')))
            {
                errors.Add("Некорректное имя службы. Укажите Service Name, например Spooler или wuauserv.");
            }
        }

        if (HeartbeatEnabled && (HeartbeatIntervalMinutes < 5 || HeartbeatIntervalMinutes > 1440))
        {
            errors.Add("Интервал heartbeat должен быть от 5 до 1440 минут.");
        }

        if (DailyDigestEnabled
            && (DailyDigestTime < TimeSpan.Zero || DailyDigestTime >= TimeSpan.FromDays(1)))
        {
            errors.Add("Некорректное время ежедневного дайджеста.");
        }

        if (JournalRetentionDays is < 0 or > 365)
        {
            errors.Add("Срок хранения журнала должен быть от 0 до 365 дней (0 — без автоочистки).");
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

        if (HighCpuThresholdPercent is < 50 or > 99)
        {
            errors.Add("Порог CPU должен быть от 50 до 99%.");
        }

        if (HighMemoryThresholdPercent is < 50 or > 99)
        {
            errors.Add("Порог памяти должен быть от 50 до 99%.");
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

    private static bool IsValidHttpsWebhook(string url)
    {
        if (url.Length > 2048)
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps && !string.IsNullOrWhiteSpace(uri.Host);
    }
}
