using System.Diagnostics.Eventing.Reader;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.Events;

public sealed class WindowsEventLogReader : IEventCollector
{
    public static readonly int[] CandidateEventIds =
    [
        12,
        13,
        41,
        1074,
        1076,
        6005,
        6006,
        6008,
        6009,
        7001,
        7002
    ];

    private const int MaxEvents = 500;
    private readonly ILogger<WindowsEventLogReader> _logger;

    public WindowsEventLogReader(ILogger<WindowsEventLogReader> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<RawSystemEvent>> CollectAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var events = ReadSystemLog(since, cancellationToken);
            _logger.LogInformation(
                "Прочитано {Count} системных событий начиная с {Since}",
                events.Count,
                since);
            return Task.FromResult<IReadOnlyList<RawSystemEvent>>(events);
        }
        catch (EventLogNotFoundException ex)
        {
            _logger.LogWarning(ex, "Журнал System не найден");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Нет доступа к журналу System");
        }
        catch (EventLogException ex)
        {
            _logger.LogWarning(ex, "Ошибка чтения журнала System");
        }

        return Task.FromResult<IReadOnlyList<RawSystemEvent>>(Array.Empty<RawSystemEvent>());
    }

    private List<RawSystemEvent> ReadSystemLog(DateTimeOffset since, CancellationToken cancellationToken)
    {
        var results = new List<RawSystemEvent>();
        var queryString =
            $"*[System[({BuildEventIdFilter()}) and TimeCreated[@SystemTime>='{since.UtcDateTime:o}']]]";

        var query = new EventLogQuery("System", PathType.LogName, queryString)
        {
            ReverseDirection = true
        };

        using var reader = new EventLogReader(query);
        for (var record = reader.ReadEvent(); record is not null; record = reader.ReadEvent())
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (record)
            {
                results.Add(Map(record));
            }

            if (results.Count >= MaxEvents)
            {
                break;
            }
        }

        return results;
    }

    internal static RawSystemEvent Map(EventRecord record)
    {
        var occurredAt = record.TimeCreated ?? DateTime.UtcNow;
        if (occurredAt.Kind == DateTimeKind.Unspecified)
        {
            occurredAt = DateTime.SpecifyKind(occurredAt, DateTimeKind.Utc);
        }

        return new RawSystemEvent
        {
            OccurredAt = new DateTimeOffset(occurredAt),
            EventId = record.Id,
            Source = record.ProviderName ?? "Unknown",
            Message = TryFormat(record),
            HostName = string.IsNullOrWhiteSpace(record.MachineName) ? Environment.MachineName : record.MachineName
        };
    }

    private static string BuildEventIdFilter() =>
        string.Join(" or ", CandidateEventIds.Select(id => $"EventID={id}"));

    private static string? TryFormat(EventRecord record)
    {
        try
        {
            return record.FormatDescription();
        }
        catch (EventLogException)
        {
            return null;
        }
    }
}
