using AlarmProgram.Application.Contracts;

namespace AlarmProgram.Application.Abstractions;

public interface IEventCollector
{
    Task<IReadOnlyList<RawSystemEvent>> CollectAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
}
