using System.Text;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.Journal;
using AlarmProgram.Infrastructure.Outbox;
using AlarmProgram.Infrastructure.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AlarmProgram.Tests.Integration;

public class SettingsOutboxJournalIntegrationTests
{
    [Fact]
    public async Task Settings_export_import_roundtrip_preserves_values()
    {
        var dir = CreateTempDir();
        var storePath = Path.Combine(dir, "settings.json");
        var exportPath = Path.Combine(dir, "export.json");
        var store = CreateSettingsStore(storePath);

        var original = new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "123456789:AAExampleTelegramBotTokenValue123456",
            TelegramChatId = "42",
            NotifyOnIpChange = true,
            NotifyOnUserLogoff = true,
            NotifyOnSystemResume = true,
            DisplayName = "Lab PC",
            AlertBodyTemplate = "{Subject} @ {Host}",
            NotifyOnNetworkOffline = false,
            WebhookEnabled = true,
            WebhookUrl = "https://hooks.example.com/alarm",
            NotifyOnProcessDown = true,
            WatchedProcessNames = "nginx, notepad.exe",
            NotifyOnHighCpu = true,
            HighCpuThresholdPercent = 85,
            NotifyOnRdpConnected = true,
            NotifyOnServiceDown = true,
            WatchedServiceNames = "Spooler",
            NotifyOnUsbDisconnected = true,
            DailyDigestEnabled = true,
            DailyDigestTime = TimeSpan.FromHours(9),
            JournalRetentionDays = 30
        };

        await store.SaveAsync(original);
        await store.ExportPlainAsync(exportPath);

        var importStore = CreateSettingsStore(Path.Combine(dir, "imported-settings.json"));
        await importStore.ImportPlainAsync(exportPath);
        var restored = await importStore.LoadAsync();

        Assert.Equal(original.TelegramBotToken, restored.TelegramBotToken);
        Assert.Equal(original.TelegramChatId, restored.TelegramChatId);
        Assert.Equal("Lab PC", restored.DisplayName);
        Assert.Equal("{Subject} @ {Host}", restored.AlertBodyTemplate);
        Assert.True(restored.NotifyOnIpChange);
        Assert.True(restored.NotifyOnUserLogoff);
        Assert.True(restored.NotifyOnSystemResume);
        Assert.False(restored.NotifyOnNetworkOffline);
        Assert.True(restored.WebhookEnabled);
        Assert.Equal("https://hooks.example.com/alarm", restored.WebhookUrl);
        Assert.True(restored.NotifyOnProcessDown);
        Assert.Equal("nginx, notepad.exe", restored.WatchedProcessNames);
        Assert.True(restored.NotifyOnHighCpu);
        Assert.Equal(85, restored.HighCpuThresholdPercent);
        Assert.True(restored.NotifyOnRdpConnected);
        Assert.True(restored.NotifyOnServiceDown);
        Assert.Equal("Spooler", restored.WatchedServiceNames);
        Assert.True(restored.NotifyOnUsbDisconnected);
        Assert.True(restored.DailyDigestEnabled);
        Assert.Equal(TimeSpan.FromHours(9), restored.DailyDigestTime);
        Assert.Equal(30, restored.JournalRetentionDays);
        Assert.Contains("123456789:AAExampleTelegramBotTokenValue123456", await File.ReadAllTextAsync(exportPath));
    }

    [Fact]
    public async Task Outbox_persists_and_removes_pending_items()
    {
        var dir = CreateTempDir();
        var outbox = new FileAlertOutbox(
            Options.Create(new AlertOutboxOptions
            {
                FilePath = Path.Combine(dir, "outbox.json"),
                MaxItems = 50
            }),
            NullLogger<FileAlertOutbox>.Instance);

        var message = new AlertMessage
        {
            EventType = MachineEventType.NetworkOffline,
            Subject = "Сеть недоступна",
            Body = "offline",
            CreatedAt = DateTimeOffset.UtcNow,
            HostName = "LAB",
            CorrelationId = "corr-1"
        };

        await outbox.EnqueueAsync(message, "Telegram");
        var pending = await outbox.GetPendingAsync();
        var item = Assert.Single(pending);
        Assert.Equal("Telegram", item.ChannelName);
        Assert.Equal("corr-1", item.Message.CorrelationId);

        await outbox.UpdateAttemptAsync(item.Id, "temporary");
        pending = await outbox.GetPendingAsync();
        Assert.Equal(1, pending[0].AttemptCount);
        Assert.Equal("temporary", pending[0].LastError);

        await outbox.RemoveAsync(item.Id);
        Assert.Empty(await outbox.GetPendingAsync());
    }

    [Fact]
    public async Task Journal_export_csv_is_readable_after_appends()
    {
        var dir = CreateTempDir();
        var journal = new FileAlertJournal(
            Options.Create(new AlertJournalOptions
            {
                FilePath = Path.Combine(dir, "journal.json"),
                MaxEntries = 20
            }),
            NullLogger<FileAlertJournal>.Instance);

        await journal.AppendAsync(new AlertJournalEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventType = MachineEventType.SystemResume,
            Subject = "ПК вышел из режима сна",
            Status = "Queued",
            Channel = "Discord",
            HostName = "LAB",
            CorrelationId = "c2"
        });

        var csvPath = Path.Combine(dir, "journal.csv");
        await journal.ExportCsvAsync(csvPath);
        var lines = await File.ReadAllLinesAsync(csvPath);

        Assert.True(lines.Length >= 2);
        Assert.StartsWith("Timestamp,EventType", lines[0]);
        Assert.Contains("SystemResume", lines[1]);
        Assert.Contains("Queued", lines[1]);
    }

    [Fact]
    public async Task Journal_clear_empties_recent_entries()
    {
        var dir = CreateTempDir();
        var journal = new FileAlertJournal(
            Options.Create(new AlertJournalOptions
            {
                FilePath = Path.Combine(dir, "journal.json"),
                MaxEntries = 20
            }),
            NullLogger<FileAlertJournal>.Instance);

        await journal.AppendAsync(new AlertJournalEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventType = MachineEventType.ProcessDown,
            Subject = "Процесс не запущен",
            Status = "Sent"
        });

        await journal.ClearAsync();
        Assert.Empty(await journal.GetRecentAsync());
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AlarmProgramIntegration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static JsonSettingsStore CreateSettingsStore(string path) =>
        new(
            Options.Create(new SettingsStoreOptions { FilePath = path }),
            new FakeSecretProtector(),
            NullLogger<JsonSettingsStore>.Instance);

    private sealed class FakeSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) =>
            string.IsNullOrEmpty(plaintext)
                ? plaintext
                : "fake:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));

        public string Unprotect(string protectedText)
        {
            if (!protectedText.StartsWith("fake:", StringComparison.Ordinal))
            {
                return protectedText;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(protectedText["fake:".Length..]));
        }
    }
}
