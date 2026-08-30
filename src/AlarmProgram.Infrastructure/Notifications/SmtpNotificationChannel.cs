using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.Notifications;

public sealed class SmtpNotificationChannel : INotificationChannel, ITestableNotificationChannel
{
    public const string ChannelName = "Email";

    private readonly ISettingsStore _settingsStore;
    private readonly ISmtpMailSender _mailSender;
    private readonly ILogger<SmtpNotificationChannel> _logger;

    public SmtpNotificationChannel(
        ISettingsStore settingsStore,
        ISmtpMailSender mailSender,
        ILogger<SmtpNotificationChannel> logger)
    {
        _settingsStore = settingsStore;
        _mailSender = mailSender;
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
            const string reason = "Канал выключен или настройки SMTP невалидны";
            _logger.LogInformation("Пропуск отправки Email для {EventType}: {Reason}", message.EventType, reason);
            return NotificationDispatchResult.Skipped(ChannelName, reason);
        }

        var recipients = settings.GetSmtpRecipients();
        var request = new SmtpMailRequest
        {
            Host = settings.SmtpHost.Trim(),
            Port = settings.SmtpPort,
            UseSsl = settings.SmtpUseSsl,
            UserName = string.IsNullOrWhiteSpace(settings.SmtpUser) ? null : settings.SmtpUser.Trim(),
            Password = settings.SmtpPassword,
            From = settings.SmtpFrom.Trim(),
            To = recipients,
            Subject = string.IsNullOrWhiteSpace(message.Subject) ? "Alarm Program" : message.Subject,
            Body = string.IsNullOrWhiteSpace(message.Body) ? message.Subject : message.Body
        };

        try
        {
            await _mailSender.SendAsync(request, cancellationToken);
            _logger.LogInformation(
                "Email отправлен: тип {EventType}, хост {HostName}, CorrelationId={CorrelationId}",
                message.EventType,
                message.HostName,
                message.CorrelationId);
            return NotificationDispatchResult.Success(ChannelName);
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
                "Ошибка отправки Email, тип {EventType}, хост {HostName}, CorrelationId={CorrelationId}: {Error}",
                message.EventType,
                message.HostName,
                message.CorrelationId,
                error);
            return NotificationDispatchResult.Failed(ChannelName, error);
        }
    }

    private static bool CanSend(UserSettings settings) =>
        settings.EmailEnabled
        && settings.IsValid
        && !string.IsNullOrWhiteSpace(settings.SmtpHost)
        && !string.IsNullOrWhiteSpace(settings.SmtpFrom)
        && settings.GetSmtpRecipients().Count > 0;
}
