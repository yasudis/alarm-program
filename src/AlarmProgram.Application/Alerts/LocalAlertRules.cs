using AlarmProgram.Domain;

namespace AlarmProgram.Application.Alerts;

public static class LocalAlertRules
{
    public static bool ShouldPlaySound(MachineEventType eventType) =>
        eventType is MachineEventType.UnexpectedShutdown
            or MachineEventType.ProcessDown
            or MachineEventType.ServiceDown;

    public static bool ShouldShowBalloon(MachineEventType eventType) =>
        ShouldPlaySound(eventType)
        || eventType is MachineEventType.FailedLogon
            or MachineEventType.ApplicationCrash
            or MachineEventType.RebootPending;
}
