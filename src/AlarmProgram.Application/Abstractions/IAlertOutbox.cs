using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IAlertOutbox
{
    Task EnqueueAsync(AlertMessage message, string channelName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxItem>> GetPendingAsync(CancellationToken cancellationToken = default);

    Task RemoveAsync(string id, CancellationToken cancellationToken = default);

    Task UpdateAttemptAsync(string id, string? error, CancellationToken cancellationToken = default);
}
