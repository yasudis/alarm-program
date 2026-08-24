using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Contracts;
using AlarmProgram.Domain;

namespace AlarmProgram.Application.Events;

public sealed class EventClassifier : IEventClassifier
{
    private static readonly Dictionary<int, MachineEventType> EventIdMap = new()
    {
        [12] = MachineEventType.Startup,
        [13] = MachineEventType.Shutdown,
        [41] = MachineEventType.UnexpectedShutdown,
        [1076] = MachineEventType.UnexpectedShutdown,
        [6005] = MachineEventType.Startup,
        [6006] = MachineEventType.Shutdown,
        [6008] = MachineEventType.UnexpectedShutdown,
        [6009] = MachineEventType.Startup
    };

    public MachineEvent? Classify(RawSystemEvent rawEvent)
    {
        ArgumentNullException.ThrowIfNull(rawEvent);

        var type = ResolveType(rawEvent);
        if (type is null)
        {
            return null;
        }

        return new MachineEvent
        {
            Type = type.Value,
            OccurredAt = rawEvent.OccurredAt,
            Source = string.IsNullOrWhiteSpace(rawEvent.Source) ? "Unknown" : rawEvent.Source,
            EventId = rawEvent.EventId,
            HostName = string.IsNullOrWhiteSpace(rawEvent.HostName)
                ? Environment.MachineName
                : rawEvent.HostName,
            Message = rawEvent.Message
        };
    }

    private static MachineEventType? ResolveType(RawSystemEvent rawEvent)
    {
        if (rawEvent.EventId == 1074)
        {
            return IsRestartMessage(rawEvent.Message)
                ? MachineEventType.Restart
                : MachineEventType.Shutdown;
        }

        return EventIdMap.TryGetValue(rawEvent.EventId, out var type)
            ? type
            : null;
    }

    private static bool IsRestartMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return ContainsAny(
            message,
            "restart",
            "reboot",
            "перезагруз",
            "перезапуск",
            "рестарт");
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
