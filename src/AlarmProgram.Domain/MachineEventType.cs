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
    UserLogoff = 11,
    SessionLock = 12,
    SessionUnlock = 13,
    LowDiskSpace = 14,
    BatteryLow = 15,
    AcPowerLost = 16,
    AcPowerRestored = 17,
    ProcessDown = 18,
    HighCpu = 19,
    HighMemory = 20,
    RdpConnected = 21,
    RdpDisconnected = 22
}
