using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IResourceMonitor : IDisposable
{
    event EventHandler<MachineEvent>? ResourceEventDetected;

    void Start();

    void Poll(UserSettings settings);
}
