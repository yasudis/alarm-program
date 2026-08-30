using AlarmProgram.Domain;

namespace AlarmProgram.Application.Alerts;

public static class LocalAlertRules
{
    public static bool ShouldPlaySound(MachineEventType eventType) =>
        eventType is MachineEventType.UnexpectedShutdown
            or MachineEventType.ProcessDown
            or MachineEventType.ServiceDown
            or MachineEventType.ApplicationHang
            or MachineEventType.DefenderThreat
            or MachineEventType.DiskError;

    public static bool ShouldShowBalloon(MachineEventType eventType) =>
        ShouldPlaySound(eventType)
        || eventType is MachineEventType.FailedLogon
            or MachineEventType.ApplicationCrash
            or MachineEventType.RebootPending
            or MachineEventType.WindowsUpdateFailed;

    public static bool BypassesAntiSpam(MachineEventType eventType) =>
        eventType is MachineEventType.UnexpectedShutdown
            or MachineEventType.FailedLogon
            or MachineEventType.ApplicationCrash
            or MachineEventType.ApplicationHang
            or MachineEventType.DefenderThreat
            or MachineEventType.DiskError;

    public static bool IsUserRequested(MachineEventType eventType) =>
        eventType is MachineEventType.StatusSnapshot;
}
