using System.Diagnostics.Eventing.Reader;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Contracts;
using AlarmProgram.Domain;
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
        7002,
        4625,
        1000,
        1002,
        1116,
        1117,
        5001,
        20,
        7,
        11,
        51,
        153,
        1001,
        4720,
        4732,
        4728
    ];

    private static readonly (string LogName, int[] EventIds)[] LogQueries =
    [
        ("System", [12, 13, 41, 1074, 1076, 6005, 6006, 6008, 6009, 7001, 7002, 7, 11, 51, 153, 20, 1001]),
        ("Security", [4625, 4720, 4732, 4728]),
        ("Application", [1000, 1002]),
        ("Microsoft-Windows-Windows Defender/Operational", [1116, 1117, 5001])
    ];

    private const int MaxEvents = 500;
    private readonly ILogger<WindowsEventLogReader> _logger;
    private readonly ISettingsStore? _settingsStore;

    public WindowsEventLogReader(ILogger<WindowsEventLogReader> logger)
        : this(logger, settingsStore: null)
    {
    }

    public WindowsEventLogReader(ILogger<WindowsEventLogReader> logger, ISettingsStore? settingsStore)
    {
        _logger = logger;
        _settingsStore = settingsStore;
    }

    public async Task<IReadOnlyList<RawSystemEvent>> CollectAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var extraIds = Array.Empty<int>();
        if (_settingsStore is not null)
        {
            try
            {
                var settings = await _settingsStore.LoadAsync(cancellationToken);
                extraIds = settings.GetCustomEventIds().ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось загрузить пользовательские Event ID");
            }
        }

        var events = new List<RawSystemEvent>();
        foreach (var (logName, eventIds) in LogQueries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var merged = extraIds.Length == 0
                ? eventIds
                : eventIds.Concat(extraIds).Distinct().ToArray();
            events.AddRange(ReadLog(logName, merged, since, cancellationToken));
        }

        var ordered = events
            .OrderByDescending(item => item.OccurredAt)
            .Take(MaxEvents)
            .ToArray();

        _logger.LogInformation(
            "Прочитано {Count} системных событий начиная с {Since}",
            ordered.Length,
            since);
        return ordered;
    }

    private List<RawSystemEvent> ReadLog(
        string logName,
        int[] eventIds,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = new List<RawSystemEvent>();
            var queryString =
                $"*[System[({BuildEventIdFilter(eventIds)}) and TimeCreated[@SystemTime>='{since.UtcDateTime:o}']]]";

            var query = new EventLogQuery(logName, PathType.LogName, queryString)
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
        catch (EventLogNotFoundException ex)
        {
            _logger.LogWarning(ex, "Журнал {LogName} не найден", logName);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Нет доступа к журналу {LogName}", logName);
        }
        catch (EventLogException ex)
        {
            _logger.LogWarning(ex, "Ошибка чтения журнала {LogName}", logName);
        }

        return [];
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

    private static string BuildEventIdFilter(IEnumerable<int> eventIds) =>
        string.Join(" or ", eventIds.Select(id => $"EventID={id}"));

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
