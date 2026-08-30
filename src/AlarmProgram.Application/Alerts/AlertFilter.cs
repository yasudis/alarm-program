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
        DateTimeOffset? now = null,
        DateTimeOffset? monitoringStartedAt = null,
        int sentCountInLastHour = 0)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ShouldRaiseLocally(
                   machineEvent,
                   settings,
                   muteState,
                   lastSentOfType,
                   now,
                   monitoringStartedAt,
                   sentCountInLastHour)
               && settings.HasEnabledChannel;
    }

    public bool ShouldRaiseLocally(
        MachineEvent machineEvent,
        UserSettings settings,
        IAlertMuteState? muteState,
        DateTimeOffset? lastSentOfType,
        DateTimeOffset? now = null,
        DateTimeOffset? monitoringStartedAt = null,
        int sentCountInLastHour = 0)
    {
        ArgumentNullException.ThrowIfNull(machineEvent);
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.IsEventEnabled(machineEvent.Type) || !settings.IsValid)
        {
            return false;
        }

        if (LocalAlertRules.IsUserRequested(machineEvent.Type))
        {
            return true;
        }

        if (settings.CriticalAlertsOnly && !LocalAlertRules.IsCritical(machineEvent.Type))
        {
            return false;
        }

        var timestamp = now ?? DateTimeOffset.UtcNow;
        var isCritical = machineEvent.Type == MachineEventType.UnexpectedShutdown;
        var bypassesAntiSpam = isCritical || LocalAlertRules.BypassesAntiSpam(machineEvent.Type);

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

        if (!bypassesAntiSpam
            && settings.StartupGracePeriodMinutes > 0
            && monitoringStartedAt is { } startedAt
            && timestamp - startedAt < TimeSpan.FromMinutes(settings.StartupGracePeriodMinutes))
        {
            return false;
        }

        if (!bypassesAntiSpam
            && settings.MaxAlertsPerHour > 0
            && sentCountInLastHour >= settings.MaxAlertsPerHour)
        {
            return false;
        }

        return true;
    }
}
