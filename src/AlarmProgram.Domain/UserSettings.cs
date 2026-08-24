using System.Text.RegularExpressions;

namespace AlarmProgram.Domain;

public sealed class UserSettings
{
    private static readonly Regex TelegramTokenPattern =
        new(@"^\d{8,12}:[A-Za-z0-9_-]{30,}$", RegexOptions.Compiled);

    public string TelegramBotToken { get; set; } = string.Empty;

    public string TelegramChatId { get; set; } = string.Empty;

    public string? DiscordWebhookUrl { get; set; }

    public bool TelegramEnabled { get; set; }

    public bool DiscordEnabled { get; set; }

    public bool NotifyOnStartup { get; set; } = true;

    public bool NotifyOnShutdown { get; set; } = true;

    public bool NotifyOnRestart { get; set; } = true;

    public bool NotifyOnUnexpectedShutdown { get; set; } = true;

    public bool IsValid => Validate().Count == 0;

    public bool HasEnabledChannel => TelegramEnabled || DiscordEnabled;

    public bool IsEventEnabled(MachineEventType eventType) => eventType switch
    {
        MachineEventType.Startup => NotifyOnStartup,
        MachineEventType.Shutdown => NotifyOnShutdown,
        MachineEventType.Restart => NotifyOnRestart,
        MachineEventType.UnexpectedShutdown => NotifyOnUnexpectedShutdown,
        _ => false
    };

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

            if (string.IsNullOrWhiteSpace(TelegramChatId))
            {
                errors.Add("Telegram chat id обязателен.");
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
