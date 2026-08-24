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
}
