using AlarmProgram.Application.Health;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Health;

public class DiagnosticsReportBuilderTests
{
    [Fact]
    public void Build_omits_secrets_and_includes_status_and_journal()
    {
        var facts = new HostStatusFacts(
            TimeSpan.FromHours(2),
            "192.168.1.10",
            NetworkAvailable: true,
            DiskSummary: "C:\\ свободно 20%",
            RebootPending: false,
            IsMuted: false,
            MutedUntil: null,
            MonitoringPaused: false);
        var settings = new UserSettings
        {
            TelegramEnabled = true,
            TelegramBotToken = "123456789:AAExampleTelegramBotTokenValue123456",
            TelegramChatId = "42",
            EmailEnabled = true,
            SmtpHost = "smtp.example.com",
            SmtpFrom = "alerts@example.com",
            SmtpTo = "ops@example.com",
            SmtpPassword = "super-secret"
        };
        var alerts = new[]
        {
            new AlertJournalEntry
            {
                Timestamp = new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero),
                EventType = MachineEventType.Startup,
                Subject = "ПК включился",
                Status = "Sent",
                Channel = "Telegram"
            }
        };

        var report = DiagnosticsReportBuilder.Build("1.2.3", facts, settings, alerts, @"C:\logs");

        Assert.Contains("Alarm Program 1.2.3", report);
        Assert.Contains("Telegram", report);
        Assert.Contains("Email", report);
        Assert.Contains("ПК включился", report);
        Assert.Contains(@"C:\logs", report);
        Assert.DoesNotContain("super-secret", report);
        Assert.DoesNotContain(settings.TelegramBotToken, report);
        Assert.Contains("Uptime: 2ч 0м", report);
    }

    [Fact]
    public void Build_shows_empty_journal_and_no_channels()
    {
        var facts = new HostStatusFacts(
            TimeSpan.Zero,
            null,
            NetworkAvailable: false,
            DiskSummary: "н/д",
            RebootPending: false,
            IsMuted: false,
            MutedUntil: null,
            MonitoringPaused: false);

        var report = DiagnosticsReportBuilder.Build(
            "1.0.0",
            facts,
            new UserSettings(),
            [],
            "/tmp/logs");

        Assert.Contains("Каналы: нет", report);
        Assert.Contains("- нет записей", report);
    }
}
