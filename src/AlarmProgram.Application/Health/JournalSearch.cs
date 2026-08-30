using AlarmProgram.Domain;

namespace AlarmProgram.Application.Health;

public static class JournalSearch
{
    public static IReadOnlyList<AlertJournalEntry> Apply(
        IEnumerable<AlertJournalEntry> entries,
        string? filter)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var source = entries as IReadOnlyList<AlertJournalEntry> ?? entries.ToArray();
        if (string.IsNullOrWhiteSpace(filter))
        {
            return source;
        }

        var term = filter.Trim();
        return source
            .Where(entry => Matches(entry, term))
            .ToArray();
    }

    public static bool Matches(AlertJournalEntry entry, string term)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrWhiteSpace(term))
        {
            return true;
        }

        return Contains(entry.EventType.ToString(), term)
               || Contains(entry.Status, term)
               || Contains(entry.Channel, term)
               || Contains(entry.Subject, term)
               || Contains(entry.HostName, term);
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrEmpty(value)
        && value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
