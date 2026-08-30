using AlarmProgram.Domain;
using System.Globalization;

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

        var terms = ParseTerms(filter);
        if (terms.Count == 0)
        {
            return source;
        }

        return source
            .Where(entry => terms.All(term => Matches(entry, term)))
            .ToArray();
    }

    public static bool Matches(AlertJournalEntry entry, string term)
    {
        return Matches(entry, ParseTerm(term));
    }

    private static bool Matches(AlertJournalEntry entry, SearchTerm term)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrWhiteSpace(term.Value))
        {
            return true;
        }

        var matched = term.Field switch
        {
            "type" => Contains(entry.EventType.ToString(), term.Value),
            "status" => Contains(entry.Status, term.Value),
            "channel" => Contains(entry.Channel, term.Value),
            "subject" => Contains(entry.Subject, term.Value),
            "host" => Contains(entry.HostName, term.Value),
            "cid" or "correlation" => Contains(entry.CorrelationId, term.Value),
            "date" => MatchesDate(entry.Timestamp, term.Value),
            _ => MatchesCommonFields(entry, term.Value)
        };

        return term.Negated ? !matched : matched;
    }

    private static bool MatchesCommonFields(AlertJournalEntry entry, string term) =>
        Contains(entry.EventType.ToString(), term)
        || Contains(entry.Status, term)
        || Contains(entry.Channel, term)
        || Contains(entry.Subject, term)
        || Contains(entry.HostName, term)
        || Contains(entry.CorrelationId, term)
        || MatchesDate(entry.Timestamp, term);

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrEmpty(value)
        && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesDate(DateTimeOffset timestamp, string value)
    {
        if (!DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return false;
        }

        return DateOnly.FromDateTime(timestamp.ToLocalTime().DateTime) == date;
    }

    private static IReadOnlyList<SearchTerm> ParseTerms(string filter)
    {
        var parts = filter.Split(
            [' ', '\t', '\n', '\r'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return [];
        }

        return parts
            .Select(ParseTerm)
            .Where(term => !string.IsNullOrWhiteSpace(term.Value))
            .ToArray();
    }

    private static SearchTerm ParseTerm(string raw)
    {
        var term = raw.Trim();
        var negated = term.StartsWith('!');
        if (negated)
        {
            term = term[1..].Trim();
        }

        var colonIndex = term.IndexOf(':');
        if (colonIndex > 0 && colonIndex < term.Length - 1)
        {
            return new SearchTerm(
                term[..colonIndex].Trim().ToLowerInvariant(),
                term[(colonIndex + 1)..].Trim(),
                negated);
        }

        return new SearchTerm(null, term, negated);
    }

    private sealed record SearchTerm(string? Field, string Value, bool Negated);
}
