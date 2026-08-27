using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.Notifications;

public sealed class DiscordNotificationChannel : INotificationChannel, ITestableNotificationChannel
{
    public const string ChannelName = "Discord";
    private const int MaxDiscordTextLength = 2000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ISettingsStore _settingsStore;
    private readonly HttpClient _httpClient;
    private readonly ILogger<DiscordNotificationChannel> _logger;

    public DiscordNotificationChannel(
        ISettingsStore settingsStore,
        HttpClient httpClient,
        ILogger<DiscordNotificationChannel> logger)
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
            const string reason = "Канал выключен или настройки Discord невалидны";
            _logger.LogInformation("Пропуск отправки в Discord для {EventType}: {Reason}", message.EventType, reason);
            return NotificationDispatchResult.Skipped(ChannelName, reason);
        }

        var webhookUrl = settings.DiscordWebhookUrl!.Trim();
        var content = BuildContent(message);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(
                JsonSerializer.Serialize(new DiscordWebhookRequest(content), JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Discord webhook выполнен: тип {EventType}, хост {HostName}, CorrelationId={CorrelationId}",
                    message.EventType,
                    message.HostName,
                    message.CorrelationId);
                return NotificationDispatchResult.Success(ChannelName);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = $"HTTP {(int)response.StatusCode}: {Truncate(body)}";
            _logger.LogWarning(
                "Discord webhook не удался: {Error}, тип {EventType}, хост {HostName}, CorrelationId={CorrelationId}",
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
                "Ошибка отправки в Discord, тип {EventType}, хост {HostName}, CorrelationId={CorrelationId}: {Error}",
                message.EventType,
                message.HostName,
                message.CorrelationId,
                error);
            return NotificationDispatchResult.Failed(ChannelName, error);
        }
    }

    private static bool CanSend(UserSettings settings) =>
        settings.DiscordEnabled
        && settings.IsValid
        && !string.IsNullOrWhiteSpace(settings.DiscordWebhookUrl);

    private static string BuildContent(AlertMessage message)
    {
        var text = string.IsNullOrWhiteSpace(message.Body) ? message.Subject : message.Body;
        if (text.Length <= MaxDiscordTextLength)
        {
            return text;
        }

        return text[..(MaxDiscordTextLength - 1)] + "…";
    }

    private static string Truncate(string text, int maxLength = 300) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";

    private sealed record DiscordWebhookRequest(
        [property: JsonPropertyName("content")] string Content);
}
