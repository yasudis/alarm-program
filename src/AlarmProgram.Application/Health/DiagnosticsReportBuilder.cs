using AlarmProgram.Domain;

namespace AlarmProgram.Application.Health;

public static class DiagnosticsReportBuilder
{
    public static string Build(
        string applicationVersion,
        HostStatusFacts facts,
        UserSettings settings,
        IReadOnlyList<AlertJournalEntry> recentAlerts,
        string logsDirectory)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(recentAlerts);

        var channels = new List<string>();
        if (settings.TelegramEnabled)
        {
            channels.Add("Telegram");
        }

        if (settings.DiscordEnabled)
        {
            channels.Add("Discord");
        }

        if (settings.WebhookEnabled)
        {
            channels.Add("Webhook");
        }

        if (settings.EmailEnabled)
        {
            channels.Add("Email");
        }

        var snapshot = StatusSnapshotBuilder.Build(facts);
        var lines = new List<string>
        {
            $"Alarm Program {applicationVersion}",
            $"Хост: {Environment.MachineName}",
            snapshot.Message ?? string.Empty,
            $"Каналы: {(channels.Count == 0 ? "нет" : string.Join(", ", channels))}",
            $"Логи: {logsDirectory}",
            "Последние алерты:"
        };

        if (recentAlerts.Count == 0)
        {
            lines.Add("- нет записей");
        }
        else
        {
            foreach (var entry in recentAlerts.Take(8))
            {
                lines.Add(
                    $"- {entry.Timestamp:yyyy-MM-dd HH:mm} {entry.EventType} {entry.Status} {entry.Channel}: {entry.Subject}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}
