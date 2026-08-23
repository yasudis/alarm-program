namespace AlarmProgram.Domain;

public enum MachineEventType
{
    Unknown = 0,
    Startup = 1,
    Shutdown = 2,
    Restart = 3,
    UnexpectedShutdown = 4
}
