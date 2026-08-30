using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.Notifications;

public sealed class HttpWebhookNotificationChannel : INotificationChannel, ITestableNotificationChannel
{
    public const string ChannelName = "Webhook";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ISettingsStore _settingsStore;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpWebhookNotificationChannel> _logger;

    public HttpWebhookNotificationChannel(
        ISettingsStore settingsStore,
        HttpClient httpClient,
        ILogger<HttpWebhookNotificationChannel> logger)
    {
        _settingsStore = settingsStore;
        _httpClient = httpClient;
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
            const string reason = "Канал выключен или настройки HTTPS webhook невалидны";
            _logger.LogInformation("Пропуск отправки в Webhook для {EventType}: {Reason}", message.EventType, reason);
            return NotificationDispatchResult.Skipped(ChannelName, reason);
        }

        var webhookUrl = settings.WebhookUrl!.Trim();
        var payload = new WebhookPayload(
            message.EventType.ToString(),
            message.Subject,
            message.Body,
            message.HostName,
            message.CreatedAt,
            message.CorrelationId);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "HTTPS webhook выполнен: тип {EventType}, хост {HostName}, CorrelationId={CorrelationId}",
                    message.EventType,
                    message.HostName,
                    message.CorrelationId);
                return NotificationDispatchResult.Success(ChannelName);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = $"HTTP {(int)response.StatusCode}: {Truncate(body)}";
            _logger.LogWarning(
                "HTTPS webhook не удался: {Error}, тип {EventType}, хост {HostName}, CorrelationId={CorrelationId}",
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
        catch (Exception ex)
        {
            var error = $"{ex.GetType().Name}: {ex.Message}";
            _logger.LogError(
                ex,
                "Ошибка отправки в HTTPS webhook, тип {EventType}, хост {HostName}, CorrelationId={CorrelationId}: {Error}",
                message.EventType,
                message.HostName,
                message.CorrelationId,
                error);
            return NotificationDispatchResult.Failed(ChannelName, error);
        }
    }

    private static bool CanSend(UserSettings settings) =>
        settings.WebhookEnabled
        && settings.IsValid
        && !string.IsNullOrWhiteSpace(settings.WebhookUrl);

    private static string Truncate(string text, int maxLength = 300) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";

    private sealed record WebhookPayload(
        [property: JsonPropertyName("eventType")] string EventType,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("body")] string Body,
        [property: JsonPropertyName("hostName")] string? HostName,
        [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
        [property: JsonPropertyName("correlationId")] string? CorrelationId);
}
