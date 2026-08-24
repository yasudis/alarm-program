using System.Net;
using System.Text;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlarmProgram.Tests.Unit.Notifications;

public class TelegramNotificationChannelTests
{
    private const string Token = "123456789:AAExampleTelegramBotTokenValue123456";
    private const string ChatId = "-1001234567890";

    [Fact]
    public async Task SendAsync_posts_sendMessage_with_chat_id_and_text()
    {
        var handler = new StubHandler();
        var channel = CreateChannel(ValidSettings(), handler);
        var message = CreateAlert();

        await channel.SendAsync(message);

        Assert.NotNull(handler.LastUri);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal($"https://api.telegram.org/bot{Token}/sendMessage", handler.LastUri!.ToString());
        Assert.Contains($"\"chat_id\":\"{ChatId}\"", handler.LastBody);
        Assert.Contains("ПК включился", handler.LastBody);
        Assert.Contains("TEST-PC", handler.LastBody);
    }

    [Fact]
    public async Task SendAsync_does_not_throw_on_http_error()
    {
        var handler = new StubHandler
        {
            StatusCode = HttpStatusCode.BadRequest,
            ResponseBody = """{"ok":false,"error_code":400,"description":"Bad Request: chat not found"}"""
        };
        var channel = CreateChannel(ValidSettings(), handler);

        var exception = await Record.ExceptionAsync(() => channel.SendAsync(CreateAlert()));

        Assert.Null(exception);
        Assert.NotNull(handler.LastUri);
    }

    [Fact]
    public async Task SendAsync_does_not_throw_on_network_error()
    {
        var handler = new StubHandler { ThrowOnSend = true };
        var channel = CreateChannel(ValidSettings(), handler);

        var exception = await Record.ExceptionAsync(() => channel.SendAsync(CreateAlert()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAsync_skips_http_when_telegram_is_disabled()
    {
        var settings = ValidSettings();
        settings.TelegramEnabled = false;
        var handler = new StubHandler();
        var channel = CreateChannel(settings, handler);

        await channel.SendAsync(CreateAlert());

        Assert.Null(handler.LastUri);
    }

    [Fact]
    public async Task SendAsync_skips_http_when_settings_are_invalid()
    {
        var settings = new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "bad",
            TelegramChatId = ChatId
        };
        var handler = new StubHandler();
        var channel = CreateChannel(settings, handler);

        await channel.SendAsync(CreateAlert());

        Assert.Null(handler.LastUri);
    }

    [Fact]
    public async Task SendAsync_does_not_log_bot_token()
    {
        var logger = new CollectingLogger<TelegramNotificationChannel>();
        var handler = new StubHandler
        {
            StatusCode = HttpStatusCode.Unauthorized,
            ResponseBody = $$"""{"ok":false,"description":"unauthorized {{Token}}"}"""
        };
        var channel = new TelegramNotificationChannel(
            new FakeSettingsStore(ValidSettings()),
            new HttpClient(handler),
            logger);

        await channel.SendAsync(CreateAlert());

        Assert.All(logger.Messages, log => Assert.DoesNotContain(Token, log));
        Assert.Contains(logger.Messages, log => log.Contains("***", StringComparison.Ordinal));
    }

    private static TelegramNotificationChannel CreateChannel(UserSettings settings, StubHandler handler) =>
        new(
            new FakeSettingsStore(settings),
            new HttpClient(handler),
            NullLogger<TelegramNotificationChannel>.Instance);

    private static UserSettings ValidSettings() => new()
    {
        TelegramEnabled = true,
        TelegramBotToken = Token,
        TelegramChatId = ChatId
    };

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
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpMethod? LastMethod { get; private set; }

        public Uri? LastUri { get; private set; }

        public string? LastBody { get; private set; }

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        public string ResponseBody { get; set; } = """{"ok":true}""";

        public bool ThrowOnSend { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (ThrowOnSend)
            {
                throw new HttpRequestException($"Failed to POST {request.RequestUri}");
            }

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

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            if (exception is not null)
            {
                Messages.Add(exception.ToString());
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
