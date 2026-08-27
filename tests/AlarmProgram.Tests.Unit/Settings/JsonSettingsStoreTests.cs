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
            NotifyOnRestart = false
        };

        await store.SaveAsync(original);
        var restored = await store.LoadAsync();

        Assert.Equal(original.TelegramBotToken, restored.TelegramBotToken);
        Assert.Equal(original.TelegramChatId, restored.TelegramChatId);
        Assert.Equal(original.DiscordWebhookUrl, restored.DiscordWebhookUrl);
        Assert.True(restored.TelegramEnabled);
        Assert.True(restored.DiscordEnabled);
        Assert.False(restored.NotifyOnRestart);
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

        await store.SaveAsync(new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = token,
            TelegramChatId = "777",
            DiscordEnabled = true,
            DiscordWebhookUrl = webhook
        });

        var fileText = await File.ReadAllTextAsync(path);
        var restored = await store.LoadAsync();

        Assert.DoesNotContain(token, fileText);
        Assert.DoesNotContain("secret-token", fileText);
        Assert.Contains("777", fileText);
        Assert.Contains("enc.v1:", fileText);
        Assert.Equal(token, restored.TelegramBotToken);
        Assert.Equal(webhook, restored.DiscordWebhookUrl);
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
