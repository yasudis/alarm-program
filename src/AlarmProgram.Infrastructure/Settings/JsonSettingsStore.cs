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

        return MapFromDto(dto, decryptSecrets: true);
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

        var dto = MapToDto(settings, encryptSecrets: true);
        await WriteDtoAsync(_filePath, dto, cancellationToken);
        _logger.LogInformation("Настройки сохранены: {Path}", _filePath);
    }

    public async Task ExportPlainAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var settings = await LoadAsync(cancellationToken);
        var dto = MapToDto(settings, encryptSecrets: false);
        await WriteDtoAsync(filePath, dto, cancellationToken);
        _logger.LogInformation("Настройки экспортированы (plain) в {Path}", filePath);
    }

    public async Task ImportPlainAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Файл импорта настроек не найден.", filePath);
        }

        await using var stream = File.OpenRead(filePath);
        var dto = await JsonSerializer.DeserializeAsync<SettingsFileDto>(stream, JsonOptions, cancellationToken)
                  ?? throw new InvalidOperationException("Не удалось прочитать файл настроек.");

        var settings = MapFromDto(dto, decryptSecrets: false);
        await SaveAsync(settings, cancellationToken);
        _logger.LogInformation("Настройки импортированы из {Path}", filePath);
    }

    private UserSettings MapFromDto(SettingsFileDto dto, bool decryptSecrets) => new()
    {
        TelegramBotToken = decryptSecrets
            ? _secretProtector.Unprotect(dto.TelegramBotToken)
            : dto.TelegramBotToken,
        TelegramChatId = dto.TelegramChatId,
        DiscordWebhookUrl = string.IsNullOrEmpty(dto.DiscordWebhookUrl)
            ? dto.DiscordWebhookUrl
            : decryptSecrets
                ? _secretProtector.Unprotect(dto.DiscordWebhookUrl)
                : dto.DiscordWebhookUrl,
        TelegramEnabled = dto.TelegramEnabled,
        DiscordEnabled = dto.DiscordEnabled,
        NotifyOnStartup = dto.NotifyOnStartup,
        NotifyOnShutdown = dto.NotifyOnShutdown,
        NotifyOnRestart = dto.NotifyOnRestart,
        NotifyOnUnexpectedShutdown = dto.NotifyOnUnexpectedShutdown,
        NotifyOnUserLogon = dto.NotifyOnUserLogon,
        NotifyOnUserLogoff = dto.NotifyOnUserLogoff,
        NotifyOnIpChange = dto.NotifyOnIpChange,
        NotifyOnNetworkOffline = dto.NotifyOnNetworkOffline,
        NotifyOnNetworkOnline = dto.NotifyOnNetworkOnline,
        NotifyOnSystemResume = dto.NotifyOnSystemResume,
        HeartbeatEnabled = dto.HeartbeatEnabled,
        HeartbeatIntervalMinutes = dto.HeartbeatIntervalMinutes <= 0 ? 60 : dto.HeartbeatIntervalMinutes,
        QuietHoursEnabled = dto.QuietHoursEnabled,
        QuietHoursStart = ParseTime(dto.QuietHoursStart, TimeSpan.FromHours(23)),
        QuietHoursEnd = ParseTime(dto.QuietHoursEnd, TimeSpan.FromHours(7)),
        RunAtWindowsStartup = dto.RunAtWindowsStartup,
        MinimizeToTray = dto.MinimizeToTray,
        DisplayName = dto.DisplayName ?? string.Empty,
        AlertBodyTemplate = dto.AlertBodyTemplate
    };

    private SettingsFileDto MapToDto(UserSettings settings, bool encryptSecrets) => new()
    {
        TelegramBotToken = encryptSecrets
            ? _secretProtector.Protect(settings.TelegramBotToken)
            : settings.TelegramBotToken,
        TelegramChatId = settings.TelegramChatId,
        DiscordWebhookUrl = string.IsNullOrEmpty(settings.DiscordWebhookUrl)
            ? settings.DiscordWebhookUrl
            : encryptSecrets
                ? _secretProtector.Protect(settings.DiscordWebhookUrl)
                : settings.DiscordWebhookUrl,
        TelegramEnabled = settings.TelegramEnabled,
        DiscordEnabled = settings.DiscordEnabled,
        NotifyOnStartup = settings.NotifyOnStartup,
        NotifyOnShutdown = settings.NotifyOnShutdown,
        NotifyOnRestart = settings.NotifyOnRestart,
        NotifyOnUnexpectedShutdown = settings.NotifyOnUnexpectedShutdown,
        NotifyOnUserLogon = settings.NotifyOnUserLogon,
        NotifyOnUserLogoff = settings.NotifyOnUserLogoff,
        NotifyOnIpChange = settings.NotifyOnIpChange,
        NotifyOnNetworkOffline = settings.NotifyOnNetworkOffline,
        NotifyOnNetworkOnline = settings.NotifyOnNetworkOnline,
        NotifyOnSystemResume = settings.NotifyOnSystemResume,
        HeartbeatEnabled = settings.HeartbeatEnabled,
        HeartbeatIntervalMinutes = settings.HeartbeatIntervalMinutes,
        QuietHoursEnabled = settings.QuietHoursEnabled,
        QuietHoursStart = FormatTime(settings.QuietHoursStart),
        QuietHoursEnd = FormatTime(settings.QuietHoursEnd),
        RunAtWindowsStartup = settings.RunAtWindowsStartup,
        MinimizeToTray = settings.MinimizeToTray,
        DisplayName = settings.DisplayName ?? string.Empty,
        AlertBodyTemplate = settings.AlertBodyTemplate
    };

    private static async Task WriteDtoAsync(
        string filePath,
        SettingsFileDto dto,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var tempPath = filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, filePath, overwrite: true);
    }

    private static string ResolveFilePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "%AppData%/AlarmProgram/settings.json"
            : configuredPath;

        path = path.Replace("%AppData%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StringComparison.OrdinalIgnoreCase);
        return Path.GetFullPath(path);
    }

    private static TimeSpan ParseTime(string? value, TimeSpan fallback)
    {
        if (TimeSpan.TryParse(value, out var parsed)
            && parsed >= TimeSpan.Zero
            && parsed < TimeSpan.FromDays(1))
        {
            return parsed;
        }

        return fallback;
    }

    private static string FormatTime(TimeSpan value) =>
        $"{(int)value.TotalHours:00}:{value.Minutes:00}";
}
