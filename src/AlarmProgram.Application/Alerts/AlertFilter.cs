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
        ArgumentNullException.ThrowIfNull(settings);
        return ShouldRaiseLocally(machineEvent, settings, muteState, lastSentOfType, now)
               && settings.HasEnabledChannel;
    }

    public bool ShouldRaiseLocally(
        MachineEvent machineEvent,
        UserSettings settings,
        IAlertMuteState? muteState,
        DateTimeOffset? lastSentOfType,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(machineEvent);
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.IsEventEnabled(machineEvent.Type) || !settings.IsValid)
        {
            return false;
        }

        var timestamp = now ?? DateTimeOffset.UtcNow;
        var isCritical = LocalAlertRules.BypassesQuietHoursAndMute(machineEvent.Type);

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
