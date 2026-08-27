using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmProgram.Infrastructure.Notifications;

public sealed class TelegramNotificationChannel : INotificationChannel, ITestableNotificationChannel
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
    private readonly int _maxAttempts;
    private readonly TimeSpan _retryDelay;

    public TelegramNotificationChannel(
        ISettingsStore settingsStore,
        HttpClient httpClient,
        IOptions<NotificationsOptions> notificationsOptions,
        ILogger<TelegramNotificationChannel> logger)
    {
        _settingsStore = settingsStore;
        _httpClient = httpClient;
        _maxAttempts = ResolveRetryCount(notificationsOptions.Value.DefaultRetryCount);
        _retryDelay = ResolveRetryDelay(notificationsOptions.Value.RetryDelaySeconds);
        _logger = logger;
    }

    public string Name => ChannelName;

    public async Task SendAsync(AlertMessage message, CancellationToken cancellationToken = default)
    {
        await SendWithResultAsync(message, cancellationToken);
    }

    public async Task<NotificationDispatchResult> SendWithResultAsync(
        AlertMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var settings = await _settingsStore.LoadAsync(cancellationToken);
        if (!CanSend(settings))
        {
            const string reason = "Канал выключен или настройки Telegram невалидны";
            _logger.LogInformation("Пропуск отправки в Telegram для {EventType}: {Reason}", message.EventType, reason);
            return NotificationDispatchResult.Skipped(ChannelName, reason);
        }

        var token = settings.TelegramBotToken.Trim();
        var chatId = settings.TelegramChatId.Trim();
        var text = BuildText(message);
        var url = $"https://api.telegram.org/bot{token}/sendMessage";

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
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

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Telegram sendMessage выполнен: тип {EventType}, хост {HostName}, CorrelationId={CorrelationId}",
                        message.EventType,
                        message.HostName,
                        message.CorrelationId);
                    return NotificationDispatchResult.Success(ChannelName);
                }

                var error = $"HTTP {(int)response.StatusCode}: {Truncate(safeResponse)}";
                if (attempt < _maxAttempts && IsTransientStatusCode(response.StatusCode))
                {
                    _logger.LogWarning(
                        "Telegram sendMessage временно не удался (попытка {Attempt}/{MaxAttempts}): {Error}. CorrelationId={CorrelationId}, EventType={EventType}",
                        attempt,
                        _maxAttempts,
                        error,
                        message.CorrelationId,
                        message.EventType);
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                    continue;
                }

                _logger.LogWarning(
                    "Telegram sendMessage не удался: {Error}, тип {EventType}, хост {HostName}, CorrelationId={CorrelationId}",
                    error,
                    message.EventType,
                    message.HostName,
                    message.CorrelationId);
                return NotificationDispatchResult.Failed(ChannelName, error);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < _maxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Ошибка сети при отправке в Telegram (попытка {Attempt}/{MaxAttempts}) для {EventType} на {HostName}. CorrelationId={CorrelationId}",
                    attempt,
                    _maxAttempts,
                    message.EventType,
                    message.HostName,
                    message.CorrelationId);
                await DelayBeforeRetryAsync(attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                var error = $"{ex.GetType().Name}: {Redact(ex.Message, token)}";
                _logger.LogError(
                    ex,
                    "Ошибка сети при отправке в Telegram, тип {EventType}, хост {HostName}, CorrelationId={CorrelationId}: {Error}",
                    message.EventType,
                    message.HostName,
                    message.CorrelationId,
                    error);
                return NotificationDispatchResult.Failed(ChannelName, error);
            }
        }

        return NotificationDispatchResult.Failed(ChannelName, "Telegram sendMessage завершился без успешной попытки.");
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

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code == 408 || code == 429 || code >= 500;
    }

    private static int ResolveRetryCount(int configuredRetries)
    {
        if (configuredRetries < 1)
        {
            return 1;
        }

        return Math.Min(configuredRetries, 5);
    }

    private static TimeSpan ResolveRetryDelay(int configuredDelaySeconds)
    {
        if (configuredDelaySeconds < 1)
        {
            return TimeSpan.FromSeconds(1);
        }

        return TimeSpan.FromSeconds(Math.Min(configuredDelaySeconds, 10));
    }

    private async Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        var multiplier = Math.Pow(2, Math.Max(0, attempt - 1));
        var delayMs = (int)Math.Min(_retryDelay.TotalMilliseconds * multiplier, TimeSpan.FromSeconds(30).TotalMilliseconds);
        await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
    }

    private sealed record SendMessageRequest(
        [property: JsonPropertyName("chat_id")] string ChatId,
        [property: JsonPropertyName("text")] string Text);
}
