using System.Text;
using AlarmProgram.Domain;

namespace AlarmProgram.Application.Health;

public static class WeeklyDigestBuilder
{
    public static MachineEvent Build(
        IReadOnlyList<AlertJournalEntry> entries,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var since = now - TimeSpan.FromDays(7);
        var recent = entries
            .Where(entry =>
                entry.Timestamp >= since
                && entry.EventType != MachineEventType.DailyDigest
                && entry.EventType != MachineEventType.WeeklyDigest
                && entry.EventType != MachineEventType.Heartbeat)
            .ToArray();

        if (recent.Length == 0)
        {
            return SystemHealthRules.WeeklyDigest(0, "За последние 7 дней алертов не было.");
        }

        var byType = recent
            .GroupBy(entry => entry.EventType)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key.ToString(), StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine($"За последние 7 дней алертов: {recent.Length}.");
        builder.AppendLine("По типам:");
        foreach (var group in byType)
        {
            builder.AppendLine($"- {group.Key}: {group.Count()}");
        }

        var byChannel = recent
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Channel))
            .GroupBy(entry => entry.Channel!, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();
        if (byChannel.Length > 0)
        {
            builder.AppendLine("По каналам:");
            foreach (var group in byChannel)
            {
                builder.AppendLine($"- {group.Key}: {group.Count()}");
            }
        }

        var byStatus = recent
            .GroupBy(entry => string.IsNullOrWhiteSpace(entry.Status) ? "Unknown" : entry.Status, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToArray();
        if (byStatus.Length > 0)
        {
            builder.AppendLine("По статусам доставки:");
            foreach (var group in byStatus)
            {
                builder.AppendLine($"- {group.Key}: {group.Count()}");
            }
        }

        return SystemHealthRules.WeeklyDigest(recent.Length, builder.ToString().TrimEnd());
    }

    public static bool ShouldSend(
        bool enabled,
        DayOfWeek digestDay,
        TimeSpan digestTime,
        DateTimeOffset now,
        DateOnly? lastSentLocalDate)
    {
        if (!enabled)
        {
            return false;
        }

        var localNow = now.ToLocalTime();
        if (localNow.DayOfWeek != digestDay)
        {
            return false;
        }

        return DailyDigestBuilder.ShouldSend(true, digestTime, now, lastSentLocalDate);
    }
}
