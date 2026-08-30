using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IServiceWatchdog : IDisposable
{
    event EventHandler<MachineEvent>? ServiceEventDetected;

    void Start();

    void Poll(UserSettings settings);
}
