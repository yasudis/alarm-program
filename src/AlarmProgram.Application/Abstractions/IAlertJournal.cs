using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IAlertJournal
{
    Task AppendAsync(AlertJournalEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertJournalEntry>> GetRecentAsync(
        int maxCount = 50,
        CancellationToken cancellationToken = default);
}
