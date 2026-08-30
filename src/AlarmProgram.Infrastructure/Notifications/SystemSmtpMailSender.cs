using System.Net;
using System.Net.Mail;
using AlarmProgram.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.Notifications;

public sealed class SystemSmtpMailSender : ISmtpMailSender
{
    private readonly ILogger<SystemSmtpMailSender> _logger;

    public SystemSmtpMailSender(ILogger<SystemSmtpMailSender> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(SmtpMailRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        using var message = new MailMessage
        {
            From = new MailAddress(request.From),
            Subject = request.Subject,
            Body = request.Body
        };

        foreach (var to in request.To)
        {
            message.To.Add(to);
        }

        using var client = new SmtpClient(request.Host, request.Port)
        {
            EnableSsl = request.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000
        };

        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            client.Credentials = new NetworkCredential(request.UserName, request.Password ?? string.Empty);
        }

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (SmtpException ex)
        {
            _logger.LogWarning(ex, "SMTP-ошибка при отправке на {Host}:{Port}", request.Host, request.Port);
            throw;
        }
    }
}
