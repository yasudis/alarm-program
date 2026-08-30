using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Alerts;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Application.Contracts;
using AlarmProgram.Application.Events;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AlarmProgram.Tests.Unit.Alerts;

public class AlertOrchestratorTests
{
    [Fact]
    public async Task ProcessAsync_classifies_formats_and_sends_enabled_event()
    {
        var raw = CreateRaw(12, "The operating system started.");
        var channel = new CapturingChannel();
        var orchestrator = CreateOrchestrator(
            new FakeCollector([raw]),
            ValidTelegramSettings(),
            channel);

        await orchestrator.ProcessAsync(DateTimeOffset.UtcNow.AddHours(-1));

        var sent = Assert.Single(channel.Sent);
        Assert.Equal(MachineEventType.Startup, sent.EventType);
        Assert.Equal("ПК включился", sent.Subject);
        Assert.Contains("TEST-PC", sent.Body);
        Assert.Contains("2026-08-24 10:30:00 UTC", sent.Body);
        Assert.Contains("The operating system started.", sent.Body);
    }

    [Fact]
    public async Task ProcessAsync_does_not_send_when_event_type_is_disabled()
    {
        var settings = ValidTelegramSettings();
        settings.NotifyOnStartup = false;
        var channel = new CapturingChannel();
        var orchestrator = CreateOrchestrator(
            new FakeCollector([CreateRaw(6005)]),
            settings,
            channel);

        await orchestrator.ProcessAsync(DateTimeOffset.UtcNow.AddHours(-1));

        Assert.Empty(channel.Sent);
    }

    [Fact]
    public async Task ProcessAsync_does_not_send_when_telegram_is_disabled()
    {
        var settings = ValidTelegramSettings();
        settings.TelegramEnabled = false;
        var channel = new CapturingChannel();
        var orchestrator = CreateOrchestrator(
            new FakeCollector([CreateRaw(6008)]),
            settings,
            channel);

        await orchestrator.ProcessAsync(DateTimeOffset.UtcNow.AddHours(-1));

        Assert.Empty(channel.Sent);
    }

    [Fact]
    public async Task ProcessAsync_does_not_send_when_settings_are_invalid()
    {
        var settings = new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "bad",
            TelegramChatId = "42"
        };
        var channel = new CapturingChannel();
        var orchestrator = CreateOrchestrator(
            new FakeCollector([CreateRaw(41)]),
            settings,
            channel);

        await orchestrator.ProcessAsync(DateTimeOffset.UtcNow.AddHours(-1));

        Assert.Empty(channel.Sent);
    }

    [Fact]
    public async Task ProcessAsync_skips_unclassified_event_ids()
    {
        var channel = new CapturingChannel();
        var orchestrator = CreateOrchestrator(
            new FakeCollector([CreateRaw(9999)]),
            ValidTelegramSettings(),
            channel);

        await orchestrator.ProcessAsync(DateTimeOffset.UtcNow.AddHours(-1));

        Assert.Empty(channel.Sent);
    }

    [Fact]
    public async Task ProcessAsync_does_not_throw_when_channel_fails()
    {
        var orchestrator = CreateOrchestrator(
            new FakeCollector([CreateRaw(13)]),
            ValidTelegramSettings(),
            new ThrowingChannel());

        var exception = await Record.ExceptionAsync(
            () => orchestrator.ProcessAsync(DateTimeOffset.UtcNow.AddHours(-1)));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ProcessAsync_sends_events_in_chronological_order()
    {
        var older = CreateRaw(12, occurredAt: new DateTimeOffset(2026, 8, 24, 9, 0, 0, TimeSpan.Zero));
        var newer = CreateRaw(13, occurredAt: new DateTimeOffset(2026, 8, 24, 10, 0, 0, TimeSpan.Zero));
        var channel = new CapturingChannel();
        var orchestrator = CreateOrchestrator(
            new FakeCollector([newer, older]),
            ValidTelegramSettings(),
            channel);

        await orchestrator.ProcessAsync(DateTimeOffset.UtcNow.AddHours(-1));

        Assert.Equal(2, channel.Sent.Count);
        Assert.Equal(MachineEventType.Startup, channel.Sent[0].EventType);
        Assert.Equal(MachineEventType.Shutdown, channel.Sent[1].EventType);
    }

    [Fact]
    public async Task ProcessAsync_skips_duplicate_events_within_deduplication_window()
    {
        var duplicate = CreateRaw(6005, occurredAt: new DateTimeOffset(2026, 8, 24, 10, 0, 0, TimeSpan.Zero));
        var channel = new CapturingChannel();
        var orchestrator = CreateOrchestrator(
            new FakeCollector([duplicate, duplicate]),
            ValidTelegramSettings(),
            channel);

        await orchestrator.ProcessAsync(DateTimeOffset.UtcNow.AddHours(-1));

        var sent = Assert.Single(channel.Sent);
        Assert.Equal(MachineEventType.Startup, sent.EventType);
    }

    [Fact]
    public async Task ProcessAsync_queues_failed_sends_into_outbox()
    {
        var outbox = new FakeAlertOutbox();
        var orchestrator = new AlertOrchestrator(
            new FakeCollector([CreateRaw(13)]),
            new EventClassifier(),
            new AlertFilter(),
            new AlertFormatter(),
            [new ThrowingChannel()],
            new FakeSettingsStore(ValidTelegramSettings()),
            new FakeAlertJournal(),
            outbox,
            new AlertMuteState(),
            new NullWindowsEventLogWriter(),
            Options.Create(new MonitoringOptions { DeduplicationWindowSeconds = 180 }),
            NullLogger<AlertOrchestrator>.Instance);

        await orchestrator.ProcessAsync(DateTimeOffset.UtcNow.AddHours(-1));

        var item = Assert.Single(outbox.Items);
        Assert.Equal("Telegram", item.Channel);
        Assert.Equal(MachineEventType.Shutdown, item.Message.EventType);
    }

    private static AlertOrchestrator CreateOrchestrator(
        IEventCollector collector,
        UserSettings settings,
        INotificationChannel channel) =>
        new(
            collector,
            new EventClassifier(),
            new AlertFilter(),
            new AlertFormatter(),
            [channel],
            new FakeSettingsStore(settings),
            new FakeAlertJournal(),
            new FakeAlertOutbox(),
            new AlertMuteState(),
            new NullWindowsEventLogWriter(),
            Options.Create(new MonitoringOptions { DeduplicationWindowSeconds = 180 }),
            NullLogger<AlertOrchestrator>.Instance);

    private static UserSettings ValidTelegramSettings() => new()
    {
        TelegramEnabled = true,
        TelegramBotToken = "123456789:AAExampleTelegramBotTokenValue123456",
        TelegramChatId = "42"
    };

    private static RawSystemEvent CreateRaw(
        int eventId,
        string? message = "payload",
        DateTimeOffset? occurredAt = null) => new()
    {
        OccurredAt = occurredAt ?? new DateTimeOffset(2026, 8, 24, 10, 30, 0, TimeSpan.Zero),
        EventId = eventId,
        Source = "System",
        Message = message,
        HostName = "TEST-PC"
    };

    private sealed class FakeCollector : IEventCollector
    {
        private readonly IReadOnlyList<RawSystemEvent> _events;

        public FakeCollector(IReadOnlyList<RawSystemEvent> events) => _events = events;

        public Task<IReadOnlyList<RawSystemEvent>> CollectAsync(
            DateTimeOffset since,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_events);
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

    private sealed class FakeAlertJournal : IAlertJournal
    {
        public Task AppendAsync(AlertJournalEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<AlertJournalEntry>> GetRecentAsync(
            int maxCount = 50,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AlertJournalEntry>>([]);

        public Task ExportCsvAsync(string filePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> PurgeOlderThanAsync(TimeSpan maxAge, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class FakeAlertOutbox : IAlertOutbox
    {
        public List<(AlertMessage Message, string Channel)> Items { get; } = [];

        public Task EnqueueAsync(AlertMessage message, string channelName, CancellationToken cancellationToken = default)
        {
            Items.Add((message, channelName));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OutboxItem>> GetPendingAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboxItem>>([]);

        public Task RemoveAsync(string id, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAttemptAsync(string id, string? error, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NullWindowsEventLogWriter : IWindowsEventLogWriter
    {
        public void WriteWarning(string message)
        {
        }

        public void WriteError(string message)
        {
        }
    }

    private sealed class CapturingChannel : INotificationChannel
    {
        public string Name => "Telegram";

        public List<AlertMessage> Sent { get; } = [];

        public Task SendAsync(AlertMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingChannel : INotificationChannel
    {
        public string Name => "Telegram";

        public Task SendAsync(AlertMessage message, CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("network down");
    }
}
