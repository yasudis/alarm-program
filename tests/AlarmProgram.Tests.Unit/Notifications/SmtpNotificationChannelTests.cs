using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlarmProgram.Tests.Unit.Notifications;

public class SmtpNotificationChannelTests
{
    [Fact]
    public async Task SendAsync_sends_mail_when_email_is_enabled()
    {
        var sender = new CapturingSmtpSender();
        var channel = new SmtpNotificationChannel(
            new FakeSettingsStore(ValidEmailSettings()),
            sender,
            NullLogger<SmtpNotificationChannel>.Instance);

        await channel.SendAsync(CreateAlert());

        var request = Assert.Single(sender.Requests);
        Assert.Equal("smtp.example.com", request.Host);
        Assert.Equal(587, request.Port);
        Assert.True(request.UseSsl);
        Assert.Equal("alerts@example.com", request.From);
        Assert.Equal(new[] { "ops@example.com", "oncall@example.com" }, request.To);
        Assert.Equal("Некорректное выключение ПК", request.Subject);
        Assert.Contains("TEST-PC", request.Body);
    }

    [Fact]
    public async Task SendAsync_skips_when_email_is_disabled()
    {
        var settings = ValidEmailSettings();
        settings.EmailEnabled = false;
        var sender = new CapturingSmtpSender();
        var channel = new SmtpNotificationChannel(
            new FakeSettingsStore(settings),
            sender,
            NullLogger<SmtpNotificationChannel>.Instance);

        await channel.SendAsync(CreateAlert());

        Assert.Empty(sender.Requests);
    }

    [Fact]
    public async Task SendWithResultAsync_returns_failed_when_sender_throws()
    {
        var channel = new SmtpNotificationChannel(
            new FakeSettingsStore(ValidEmailSettings()),
            new ThrowingSmtpSender(),
            NullLogger<SmtpNotificationChannel>.Instance);

        var result = await channel.SendWithResultAsync(CreateAlert());

        Assert.False(result.IsSuccess);
        Assert.False(result.IsSkipped);
        Assert.Equal("Email", result.Channel);
        Assert.Contains("SmtpException", result.Error);
    }

    private static UserSettings ValidEmailSettings() => new()
    {
        EmailEnabled = true,
        SmtpHost = "smtp.example.com",
        SmtpPort = 587,
        SmtpUseSsl = true,
        SmtpFrom = "alerts@example.com",
        SmtpTo = "ops@example.com, oncall@example.com"
    };

    private static AlertMessage CreateAlert() => new()
    {
        EventType = MachineEventType.UnexpectedShutdown,
        Subject = "Некорректное выключение ПК",
        Body = "Хост: TEST-PC",
        CreatedAt = DateTimeOffset.UtcNow,
        HostName = "TEST-PC",
        CorrelationId = "cid-mail"
    };

    private sealed class CapturingSmtpSender : ISmtpMailSender
    {
        public List<SmtpMailRequest> Requests { get; } = [];

        public Task SendAsync(SmtpMailRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingSmtpSender : ISmtpMailSender
    {
        public Task SendAsync(SmtpMailRequest request, CancellationToken cancellationToken = default) =>
            throw new System.Net.Mail.SmtpException("relay denied");
    }

    private sealed class FakeSettingsStore : ISettingsStore
    {
        private readonly UserSettings _settings;

        public FakeSettingsStore(UserSettings settings) => _settings = settings;

        public Task<UserSettings> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_settings);

        public Task SaveAsync(UserSettings settings, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ExportPlainAsync(string filePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ImportPlainAsync(string filePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
