using AlarmProgram.Domain;

namespace AlarmProgram.Application.Alerts;

public sealed class AlertFilter
{
    public bool ShouldNotify(MachineEvent machineEvent, UserSettings settings) =>
        ShouldNotify(machineEvent, settings, muteState: null, lastSentOfType: null);

    public bool ShouldNotify(
        MachineEvent machineEvent,
        UserSettings settings,
        IAlertMuteState? muteState,
        DateTimeOffset? lastSentOfType,
        DateTimeOffset? now = null)
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

        var timestamp = now ?? DateTimeOffset.UtcNow;
        var isCritical = machineEvent.Type == MachineEventType.UnexpectedShutdown;

        if (!isCritical && settings.IsWithinQuietHours(machineEvent.OccurredAt))
        {
            return false;
        }

        if (!isCritical && muteState is not null && muteState.IsActiveAt(timestamp))
        {
            return false;
        }

        if (!isCritical
            && settings.AlertCooldownMinutes > 0
            && lastSentOfType is { } lastSent
            && timestamp - lastSent < TimeSpan.FromMinutes(settings.AlertCooldownMinutes))
        {
            return false;
        }

        return true;
    }
}
