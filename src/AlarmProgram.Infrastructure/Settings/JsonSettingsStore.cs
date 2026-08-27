using System.Text.Json;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmProgram.Infrastructure.Settings;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ISecretProtector _secretProtector;
    private readonly ILogger<JsonSettingsStore> _logger;
    private readonly string _filePath;

    public JsonSettingsStore(
        IOptions<SettingsStoreOptions> options,
        ISecretProtector secretProtector,
        ILogger<JsonSettingsStore> logger)
    {
        _secretProtector = secretProtector;
        _logger = logger;
        _filePath = ResolveFilePath(options.Value.FilePath);
    }

    public async Task<UserSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInformation("Файл настроек не найден, используются значения по умолчанию: {Path}", _filePath);
            return new UserSettings();
        }

        await using var stream = File.OpenRead(_filePath);
        var dto = await JsonSerializer.DeserializeAsync<SettingsFileDto>(stream, JsonOptions, cancellationToken)
                  ?? new SettingsFileDto();

        return new UserSettings
        {
            TelegramBotToken = _secretProtector.Unprotect(dto.TelegramBotToken),
            TelegramChatId = dto.TelegramChatId,
            DiscordWebhookUrl = string.IsNullOrEmpty(dto.DiscordWebhookUrl)
                ? dto.DiscordWebhookUrl
                : _secretProtector.Unprotect(dto.DiscordWebhookUrl),
            TelegramEnabled = dto.TelegramEnabled,
            DiscordEnabled = dto.DiscordEnabled,
            NotifyOnStartup = dto.NotifyOnStartup,
            NotifyOnShutdown = dto.NotifyOnShutdown,
            NotifyOnRestart = dto.NotifyOnRestart,
            NotifyOnUnexpectedShutdown = dto.NotifyOnUnexpectedShutdown
        };
    }

    public async Task SaveAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var validationErrors = settings.Validate();
        if (validationErrors.Count > 0)
        {
            var message = "Настройки не прошли валидацию: " + string.Join(" ", validationErrors);
            _logger.LogWarning(message);
            throw new InvalidOperationException(message);
        }

        var dto = new SettingsFileDto
        {
            TelegramBotToken = _secretProtector.Protect(settings.TelegramBotToken),
            TelegramChatId = settings.TelegramChatId,
            DiscordWebhookUrl = string.IsNullOrEmpty(settings.DiscordWebhookUrl)
                ? settings.DiscordWebhookUrl
                : _secretProtector.Protect(settings.DiscordWebhookUrl),
            TelegramEnabled = settings.TelegramEnabled,
            DiscordEnabled = settings.DiscordEnabled,
            NotifyOnStartup = settings.NotifyOnStartup,
            NotifyOnShutdown = settings.NotifyOnShutdown,
            NotifyOnRestart = settings.NotifyOnRestart,
            NotifyOnUnexpectedShutdown = settings.NotifyOnUnexpectedShutdown
        };

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var tempPath = _filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, _filePath, overwrite: true);

        _logger.LogInformation("Настройки сохранены: {Path}", _filePath);
    }

    private static string ResolveFilePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "%AppData%/AlarmProgram/settings.json"
            : configuredPath;

        path = path.Replace("%AppData%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StringComparison.OrdinalIgnoreCase);
        return Path.GetFullPath(path);
    }
}
