using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IProcessWatchdog : IDisposable
{
    event EventHandler<MachineEvent>? ProcessEventDetected;

    void Start();

    void Poll(UserSettings settings);
}
