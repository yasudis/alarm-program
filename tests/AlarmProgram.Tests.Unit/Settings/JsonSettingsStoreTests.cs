using System.Text;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.Security;
using AlarmProgram.Infrastructure.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AlarmProgram.Tests.Unit.Settings;

public class JsonSettingsStoreTests
{
    [Fact]
    public async Task LoadAsync_returns_defaults_when_file_is_missing()
    {
        var path = Path.Combine(Path.GetTempPath(), "AlarmProgramTests", Guid.NewGuid().ToString("N"), "settings.json");
        var store = CreateStore(path);

        var settings = await store.LoadAsync();

        Assert.False(settings.TelegramEnabled);
        Assert.True(settings.NotifyOnStartup);
        Assert.Equal(string.Empty, settings.TelegramBotToken);
    }

    [Fact]
    public async Task SaveAsync_then_LoadAsync_roundtrips_user_settings()
    {
        var path = Path.Combine(Path.GetTempPath(), "AlarmProgramTests", Guid.NewGuid().ToString("N"), "settings.json");
        var store = CreateStore(path);

        var original = new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "123456789:AAExampleTelegramBotTokenValue123456",
            TelegramChatId = "777",
            DiscordEnabled = true,
            DiscordWebhookUrl = "https://discord.com/api/webhooks/1/secret-token",
            NotifyOnRestart = false,
            NotifyOnUserLogon = true,
            HeartbeatEnabled = true,
            HeartbeatIntervalMinutes = 30,
            QuietHoursEnabled = true,
            QuietHoursStart = TimeSpan.FromHours(22),
            QuietHoursEnd = TimeSpan.FromHours(6),
            RunAtWindowsStartup = true,
            MinimizeToTray = false,
            WebhookEnabled = true,
            WebhookUrl = "https://hooks.example.com/alarm",
            NotifyOnProcessDown = true,
            WatchedProcessNames = "nginx",
            NotifyOnHighMemory = true,
            HighMemoryThresholdPercent = 80,
            NotifyOnRdpDisconnected = true,
            NotifyOnServiceDown = true,
            WatchedServiceNames = "Spooler",
            NotifyOnUsbConnected = true,
            DailyDigestEnabled = true,
            DailyDigestTime = TimeSpan.FromHours(8),
            JournalRetentionDays = 14,
            EmailEnabled = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 465,
            SmtpUser = "bot",
            SmtpPassword = "smtp-secret",
            SmtpFrom = "alerts@example.com",
            SmtpTo = "ops@example.com",
            SmtpUseSsl = true,
            NotifyOnFailedLogon = true,
            NotifyOnApplicationCrash = false,
            PlaySoundOnCriticalAlerts = false,
            NotifyOnApplicationHang = false,
            NotifyOnDefenderThreat = true,
            NotifyOnWindowsUpdateFailed = false,
            StartupGracePeriodMinutes = 3,
            MaxAlertsPerHour = 12,
            NotifyOnBsod = false,
            NotifyOnUserAccountCreated = true,
            NotifyOnAdminGroupChanged = false,
            NotifyOnFirewallDisabled = true,
            NotifyOnHostUnreachable = true,
            NotifyOnHostRestored = true,
            WatchedHosts = "8.8.8.8",
            NotifyOnCustomEvent = true,
            CustomEventIds = "7045,7040",
            CriticalAlertsOnly = true,
            WeeklyDigestEnabled = true,
            WeeklyDigestDay = DayOfWeek.Friday,
            WeeklyDigestTime = TimeSpan.FromHours(18)
        };

        await store.SaveAsync(original);
        var restored = await store.LoadAsync();

        Assert.Equal(original.TelegramBotToken, restored.TelegramBotToken);
        Assert.Equal(original.TelegramChatId, restored.TelegramChatId);
        Assert.Equal(original.DiscordWebhookUrl, restored.DiscordWebhookUrl);
        Assert.True(restored.TelegramEnabled);
        Assert.True(restored.DiscordEnabled);
        Assert.False(restored.NotifyOnRestart);
        Assert.True(restored.NotifyOnUserLogon);
        Assert.True(restored.HeartbeatEnabled);
        Assert.Equal(30, restored.HeartbeatIntervalMinutes);
        Assert.True(restored.QuietHoursEnabled);
        Assert.Equal(TimeSpan.FromHours(22), restored.QuietHoursStart);
        Assert.Equal(TimeSpan.FromHours(6), restored.QuietHoursEnd);
        Assert.True(restored.RunAtWindowsStartup);
        Assert.False(restored.MinimizeToTray);
        Assert.Equal(string.Empty, restored.DisplayName);
        Assert.True(restored.WebhookEnabled);
        Assert.Equal("https://hooks.example.com/alarm", restored.WebhookUrl);
        Assert.True(restored.NotifyOnProcessDown);
        Assert.Equal("nginx", restored.WatchedProcessNames);
        Assert.True(restored.NotifyOnHighMemory);
        Assert.Equal(80, restored.HighMemoryThresholdPercent);
        Assert.True(restored.NotifyOnRdpDisconnected);
        Assert.True(restored.NotifyOnServiceDown);
        Assert.Equal("Spooler", restored.WatchedServiceNames);
        Assert.True(restored.NotifyOnUsbConnected);
        Assert.True(restored.DailyDigestEnabled);
        Assert.Equal(TimeSpan.FromHours(8), restored.DailyDigestTime);
        Assert.Equal(14, restored.JournalRetentionDays);
        Assert.True(restored.EmailEnabled);
        Assert.Equal("smtp.example.com", restored.SmtpHost);
        Assert.Equal(465, restored.SmtpPort);
        Assert.Equal("bot", restored.SmtpUser);
        Assert.Equal("smtp-secret", restored.SmtpPassword);
        Assert.Equal("alerts@example.com", restored.SmtpFrom);
        Assert.Equal("ops@example.com", restored.SmtpTo);
        Assert.True(restored.SmtpUseSsl);
        Assert.True(restored.NotifyOnFailedLogon);
        Assert.False(restored.NotifyOnApplicationCrash);
        Assert.False(restored.PlaySoundOnCriticalAlerts);
        Assert.False(restored.NotifyOnApplicationHang);
        Assert.True(restored.NotifyOnDefenderThreat);
        Assert.False(restored.NotifyOnWindowsUpdateFailed);
        Assert.Equal(3, restored.StartupGracePeriodMinutes);
        Assert.Equal(12, restored.MaxAlertsPerHour);
        Assert.False(restored.NotifyOnBsod);
        Assert.True(restored.NotifyOnUserAccountCreated);
        Assert.False(restored.NotifyOnAdminGroupChanged);
        Assert.True(restored.NotifyOnFirewallDisabled);
        Assert.True(restored.NotifyOnHostUnreachable);
        Assert.True(restored.NotifyOnHostRestored);
        Assert.Equal("8.8.8.8", restored.WatchedHosts);
        Assert.True(restored.NotifyOnCustomEvent);
        Assert.Equal("7045,7040", restored.CustomEventIds);
        Assert.True(restored.CriticalAlertsOnly);
        Assert.True(restored.WeeklyDigestEnabled);
        Assert.Equal(DayOfWeek.Friday, restored.WeeklyDigestDay);
        Assert.Equal(TimeSpan.FromHours(18), restored.WeeklyDigestTime);
    }

    [Fact]
    public async Task ExportPlainAsync_then_ImportPlainAsync_roundtrips_display_name_and_template()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AlarmProgramTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var store = CreateStore(Path.Combine(dir, "settings.json"));
        var exportPath = Path.Combine(dir, "backup.json");

        await store.SaveAsync(new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "123456789:AAExampleTelegramBotTokenValue123456",
            TelegramChatId = "777",
            DisplayName = "Workstation",
            AlertBodyTemplate = "{Subject} / {Time}",
            NotifyOnIpChange = true
        });

        await store.ExportPlainAsync(exportPath);
        var other = CreateStore(Path.Combine(dir, "other.json"));
        await other.ImportPlainAsync(exportPath);
        var restored = await other.LoadAsync();

        Assert.Equal("Workstation", restored.DisplayName);
        Assert.Equal("{Subject} / {Time}", restored.AlertBodyTemplate);
        Assert.True(restored.NotifyOnIpChange);
    }

    [Fact]
    public async Task SaveAsync_does_not_store_secrets_in_plaintext()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var path = Path.Combine(Path.GetTempPath(), "AlarmProgramTests", Guid.NewGuid().ToString("N"), "settings.json");
        var options = Options.Create(new SettingsStoreOptions { FilePath = path });
        var store = new JsonSettingsStore(options, new DpapiSecretProtector(), NullLogger<JsonSettingsStore>.Instance);
        const string token = "123456789:AAExampleTelegramBotTokenValue123456";
        const string webhook = "https://discord.com/api/webhooks/1/secret-token";
        const string genericWebhook = "https://hooks.example.com/secret-generic";
        const string smtpPassword = "smtp-secret-password";

        await store.SaveAsync(new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = token,
            TelegramChatId = "777",
            DiscordEnabled = true,
            DiscordWebhookUrl = webhook,
            WebhookEnabled = true,
            WebhookUrl = genericWebhook,
            EmailEnabled = true,
            SmtpHost = "smtp.example.com",
            SmtpFrom = "alerts@example.com",
            SmtpTo = "ops@example.com",
            SmtpPassword = smtpPassword
        });

        var fileText = await File.ReadAllTextAsync(path);
        var restored = await store.LoadAsync();

        Assert.DoesNotContain(token, fileText);
        Assert.DoesNotContain("secret-token", fileText);
        Assert.DoesNotContain("secret-generic", fileText);
        Assert.DoesNotContain(smtpPassword, fileText);
        Assert.Contains("777", fileText);
        Assert.Contains("enc.v1:", fileText);
        Assert.Equal(token, restored.TelegramBotToken);
        Assert.Equal(webhook, restored.DiscordWebhookUrl);
        Assert.Equal(genericWebhook, restored.WebhookUrl);
        Assert.Equal(smtpPassword, restored.SmtpPassword);
    }

    [Fact]
    public async Task SaveAsync_throws_when_settings_are_invalid()
    {
        var path = Path.Combine(Path.GetTempPath(), "AlarmProgramTests", Guid.NewGuid().ToString("N"), "settings.json");
        var store = CreateStore(path);
        var invalid = new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "bad-token-format",
            TelegramChatId = string.Empty
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(invalid));
        Assert.False(File.Exists(path));
    }

    private static JsonSettingsStore CreateStore(string filePath)
    {
        var options = Options.Create(new SettingsStoreOptions { FilePath = filePath });
        return new JsonSettingsStore(options, new FakeSecretProtector(), NullLogger<JsonSettingsStore>.Instance);
    }

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
