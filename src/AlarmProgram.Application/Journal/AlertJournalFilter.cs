using AlarmProgram.Domain;

namespace AlarmProgram.Application.Journal;

public static class AlertJournalFilter
{
    public static IReadOnlyList<AlertJournalEntry> Apply(
        IEnumerable<AlertJournalEntry> entries,
        string? searchText)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var source = entries as IReadOnlyList<AlertJournalEntry> ?? entries.ToArray();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return source;
        }

        var query = searchText.Trim();
        return source
            .Where(entry => Matches(entry, query))
            .ToArray();
    }

    private static bool Matches(AlertJournalEntry entry, string query) =>
        Contains(entry.EventType.ToString(), query)
        || Contains(entry.Subject, query)
        || Contains(entry.Status, query)
        || Contains(entry.Channel, query)
        || Contains(entry.HostName, query)
        || Contains(entry.Details, query);

    private static bool Contains(string? value, string query) =>
        !string.IsNullOrEmpty(value)
        && value.Contains(query, StringComparison.OrdinalIgnoreCase);
}
