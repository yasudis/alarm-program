using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.Notifications;

public sealed class TelegramNotificationChannel : INotificationChannel
{
    public const string ChannelName = "Telegram";
    private const int MaxTelegramTextLength = 4096;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ISettingsStore _settingsStore;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramNotificationChannel> _logger;

    public TelegramNotificationChannel(
        ISettingsStore settingsStore,
        HttpClient httpClient,
        ILogger<TelegramNotificationChannel> logger)
    {
        _settingsStore = settingsStore;
        _httpClient = httpClient;
        _logger = logger;
    }

    public string Name => ChannelName;

    public async Task SendAsync(AlertMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var settings = await _settingsStore.LoadAsync(cancellationToken);
        if (!CanSend(settings))
        {
            _logger.LogInformation(
                "Пропуск отправки в Telegram для {EventType}: канал выключен или настройки невалидны",
                message.EventType);
            return;
        }

        var token = settings.TelegramBotToken.Trim();
        var chatId = settings.TelegramChatId.Trim();
        var text = BuildText(message);
        var url = $"https://api.telegram.org/bot{token}/sendMessage";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(
                JsonSerializer.Serialize(new SendMessageRequest(chatId, text), JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var safeResponse = Redact(responseBody, token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Telegram sendMessage не удался: HTTP {StatusCode}, тип {EventType}, хост {HostName}, ответ: {Response}",
                    (int)response.StatusCode,
                    message.EventType,
                    message.HostName,
                    Truncate(safeResponse));
                return;
            }

            _logger.LogInformation(
                "Telegram sendMessage выполнен: тип {EventType}, хост {HostName}",
                message.EventType,
                message.HostName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Ошибка сети при отправке в Telegram, тип {EventType}, хост {HostName}: {ErrorType}: {Message}",
                message.EventType,
                message.HostName,
                ex.GetType().Name,
                Redact(ex.Message, token));
        }
    }

    private static bool CanSend(UserSettings settings) =>
        settings.TelegramEnabled
        && settings.IsValid
        && !string.IsNullOrWhiteSpace(settings.TelegramBotToken)
        && !string.IsNullOrWhiteSpace(settings.TelegramChatId);

    private static string BuildText(AlertMessage message)
    {
        var text = string.IsNullOrWhiteSpace(message.Body) ? message.Subject : message.Body;
        if (text.Length <= MaxTelegramTextLength)
        {
            return text;
        }

        return text[..(MaxTelegramTextLength - 1)] + "…";
    }

    private static string Redact(string text, string token)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token))
        {
            return text;
        }

        return text.Replace(token, "***", StringComparison.Ordinal);
    }

    private static string Truncate(string text, int maxLength = 300) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";

    private sealed record SendMessageRequest(
        [property: JsonPropertyName("chat_id")] string ChatId,
        [property: JsonPropertyName("text")] string Text);
}
