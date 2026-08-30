using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Alerts;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Application.Contracts;
using AlarmProgram.Application.Events;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AlarmProgram.Tests.Unit.Alerts;

public class AlertLocalEffectsTests
{
    [Fact]
    public async Task ProcessMachineEventAsync_plays_sound_and_shows_balloon_even_without_channels()
    {
        var sound = new CapturingSoundPlayer();
        var balloon = new CapturingBalloonNotifier();
        var orchestrator = new AlertOrchestrator(
            new EmptyCollector(),
            new EventClassifier(),
            new AlertFilter(),
            new AlertFormatter(),
            Array.Empty<INotificationChannel>(),
            new FakeSettingsStore(new UserSettings
            {
                NotifyOnUnexpectedShutdown = true,
                PlaySoundOnCriticalAlerts = true,
                ShowTrayBalloonOnCriticalAlerts = true
            }),
            new FakeJournal(),
            new FakeOutbox(),
            new AlertMuteState(),
            new NullWriter(),
            Options.Create(new MonitoringOptions { DeduplicationWindowSeconds = 180 }),
            NullLogger<AlertOrchestrator>.Instance,
            sound,
            balloon);

        await orchestrator.ProcessMachineEventAsync(new MachineEvent
        {
            Type = MachineEventType.UnexpectedShutdown,
            OccurredAt = DateTimeOffset.UtcNow,
            Source = "EventLog",
            EventId = 41,
            HostName = "TEST-PC"
        });

        Assert.Equal(1, sound.PlayCount);
        Assert.Single(balloon.Items);
        Assert.Equal("Некорректное выключение ПК", balloon.Items[0].Title);
    }

    [Fact]
    public async Task ProcessMachineEventAsync_does_not_play_sound_when_option_disabled()
    {
        var sound = new CapturingSoundPlayer();
        var orchestrator = new AlertOrchestrator(
            new EmptyCollector(),
            new EventClassifier(),
            new AlertFilter(),
            new AlertFormatter(),
            Array.Empty<INotificationChannel>(),
            new FakeSettingsStore(new UserSettings
            {
                NotifyOnProcessDown = true,
                PlaySoundOnCriticalAlerts = false,
                ShowTrayBalloonOnCriticalAlerts = false
            }),
            new FakeJournal(),
            new FakeOutbox(),
            new AlertMuteState(),
            new NullWriter(),
            Options.Create(new MonitoringOptions { DeduplicationWindowSeconds = 180 }),
            NullLogger<AlertOrchestrator>.Instance,
            sound,
            NullTrayBalloonNotifier.Instance);

        await orchestrator.ProcessMachineEventAsync(new MachineEvent
        {
            Type = MachineEventType.ProcessDown,
            OccurredAt = DateTimeOffset.UtcNow,
            Source = "ProcessWatchdog",
            HostName = "TEST-PC",
            Message = "nginx"
        });

        Assert.Equal(0, sound.PlayCount);
    }

    private sealed class CapturingSoundPlayer : IAlertSoundPlayer
    {
        public int PlayCount { get; private set; }

        public void PlayCritical() => PlayCount++;
    }

    private sealed class CapturingBalloonNotifier : ITrayBalloonNotifier
    {
        public List<(string Title, string Text)> Items { get; } = [];

        public void Show(string title, string text) => Items.Add((title, text));
    }

    private sealed class EmptyCollector : IEventCollector
    {
        public Task<IReadOnlyList<RawSystemEvent>> CollectAsync(
            DateTimeOffset since,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RawSystemEvent>>([]);
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

    private sealed class FakeJournal : IAlertJournal
    {
        public Task AppendAsync(AlertJournalEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<AlertJournalEntry>> GetRecentAsync(
            int maxCount = 50,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AlertJournalEntry>>([]);

        public Task ExportCsvAsync(string filePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ExportJsonAsync(string filePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> PurgeOlderThanAsync(TimeSpan maxAge, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class FakeOutbox : IAlertOutbox
    {
        public Task EnqueueAsync(AlertMessage message, string channelName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<OutboxItem>> GetPendingAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboxItem>>([]);

        public Task RemoveAsync(string id, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAttemptAsync(string id, string? error, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NullWriter : IWindowsEventLogWriter
    {
        public void WriteWarning(string message)
        {
        }

        public void WriteError(string message)
        {
        }
    }
}
