using AlarmProgram.Domain;

namespace AlarmProgram.Application.Alerts;

public sealed class AlertFilter
{
    public bool ShouldNotify(MachineEvent machineEvent, UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(machineEvent);
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.IsEventEnabled(machineEvent.Type))
        {
            return false;
        }

        if (!settings.IsValid)
        {
            return false;
        }

        if (!settings.HasEnabledChannel)
        {
            return false;
        }

        if (machineEvent.Type != MachineEventType.UnexpectedShutdown
            && settings.IsWithinQuietHours(machineEvent.OccurredAt))
        {
            return false;
        }

        return true;
    }
}
