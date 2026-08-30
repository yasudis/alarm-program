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
