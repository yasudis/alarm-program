using System.Net;
using System.Text;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlarmProgram.Tests.Unit.Notifications;

public class DiscordNotificationChannelTests
{
    [Fact]
    public async Task SendAsync_posts_webhook_request_when_discord_enabled()
    {
        var settings = new UserSettings
        {
            DiscordEnabled = true,
            DiscordWebhookUrl = "https://discord.com/api/webhooks/1/secret-token"
        };
        var handler = new StubHandler();
        var channel = new DiscordNotificationChannel(
            new FakeSettingsStore(settings),
            new HttpClient(handler),
            NullLogger<DiscordNotificationChannel>.Instance);

        await channel.SendAsync(CreateAlert());

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal(settings.DiscordWebhookUrl, handler.LastUri!.ToString());
        Assert.Contains("ПК включился", handler.LastBody);
    }

    [Fact]
    public async Task SendAsync_skips_request_when_channel_is_disabled()
    {
        var settings = new UserSettings
        {
            DiscordEnabled = false,
            DiscordWebhookUrl = "https://discord.com/api/webhooks/1/secret-token"
        };
        var handler = new StubHandler();
        var channel = new DiscordNotificationChannel(
            new FakeSettingsStore(settings),
            new HttpClient(handler),
            NullLogger<DiscordNotificationChannel>.Instance);

        await channel.SendAsync(CreateAlert());

        Assert.Null(handler.LastUri);
    }

    [Fact]
    public async Task SendWithResultAsync_returns_failed_when_api_rejects_request()
    {
        var settings = new UserSettings
        {
            DiscordEnabled = true,
            DiscordWebhookUrl = "https://discord.com/api/webhooks/1/secret-token"
        };
        var handler = new StubHandler
        {
            StatusCode = HttpStatusCode.BadRequest,
            ResponseBody = "invalid webhook"
        };
        var channel = new DiscordNotificationChannel(
            new FakeSettingsStore(settings),
            new HttpClient(handler),
            NullLogger<DiscordNotificationChannel>.Instance);

        var result = await channel.SendWithResultAsync(CreateAlert());

        Assert.False(result.IsSuccess);
        Assert.False(result.IsSkipped);
        Assert.Contains("HTTP 400", result.Error);
    }

    private static AlertMessage CreateAlert() => new()
    {
        EventType = MachineEventType.Startup,
        Subject = "ПК включился",
        Body = "ПК включился\nХост: TEST-PC",
        CreatedAt = DateTimeOffset.UtcNow,
        HostName = "TEST-PC"
    };

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

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpMethod? LastMethod { get; private set; }

        public Uri? LastUri { get; private set; }

        public string? LastBody { get; private set; }

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.NoContent;

        public string ResponseBody { get; set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastMethod = request.Method;
            LastUri = request.RequestUri;
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
