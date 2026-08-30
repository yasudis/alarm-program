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
            or MachineEventType.DiskError
            or MachineEventType.Bsod
            or MachineEventType.FirewallDisabled
            or MachineEventType.HostUnreachable;

    public static bool ShouldShowBalloon(MachineEventType eventType) =>
        ShouldPlaySound(eventType)
        || eventType is MachineEventType.FailedLogon
            or MachineEventType.ApplicationCrash
            or MachineEventType.RebootPending
            or MachineEventType.WindowsUpdateFailed
            or MachineEventType.UserAccountCreated
            or MachineEventType.AdminGroupChanged;

    public static bool BypassesAntiSpam(MachineEventType eventType) =>
        eventType is MachineEventType.UnexpectedShutdown
            or MachineEventType.FailedLogon
            or MachineEventType.ApplicationCrash
            or MachineEventType.ApplicationHang
            or MachineEventType.DefenderThreat
            or MachineEventType.DiskError
            or MachineEventType.Bsod
            or MachineEventType.FirewallDisabled
            or MachineEventType.AdminGroupChanged
            or MachineEventType.UserAccountCreated;

    public static bool IsCritical(MachineEventType eventType) =>
        BypassesAntiSpam(eventType)
        || eventType is MachineEventType.HostUnreachable;

    public static bool IsUserRequested(MachineEventType eventType) =>
        eventType is MachineEventType.StatusSnapshot;
}
