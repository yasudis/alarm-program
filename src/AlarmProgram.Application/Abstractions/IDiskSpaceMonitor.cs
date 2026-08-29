using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IDiskSpaceMonitor : IDisposable
{
    event EventHandler<MachineEvent>? DiskEventDetected;

    void Start();

    void Poll(UserSettings settings);
}
