namespace AlarmProgram.Domain;

public enum MachineEventType
{
    Unknown = 0,
    Startup = 1,
    Shutdown = 2,
    Restart = 3,
    UnexpectedShutdown = 4,
    UserLogon = 5,
    Heartbeat = 6,
    IpChanged = 7,
    NetworkOffline = 8,
    NetworkOnline = 9,
    SystemResume = 10,
    UserLogoff = 11
}
