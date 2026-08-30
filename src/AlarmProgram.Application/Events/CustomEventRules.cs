using AlarmProgram.Application.Contracts;
using AlarmProgram.Domain;

namespace AlarmProgram.Application.Events;

public static class CustomEventRules
{
    public static MachineEvent? TryClassify(RawSystemEvent rawEvent, UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(rawEvent);
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.GetCustomEventIds().Contains(rawEvent.EventId))
        {
            return null;
        }

        return new MachineEvent
        {
            Type = MachineEventType.CustomEvent,
            OccurredAt = rawEvent.OccurredAt,
            Source = string.IsNullOrWhiteSpace(rawEvent.Source) ? "Unknown" : rawEvent.Source,
            EventId = rawEvent.EventId,
            HostName = string.IsNullOrWhiteSpace(rawEvent.HostName)
                ? Environment.MachineName
                : rawEvent.HostName,
            Message = string.IsNullOrWhiteSpace(rawEvent.Message)
                ? $"Пользовательский Event ID {rawEvent.EventId}."
                : rawEvent.Message
        };
    }
}
