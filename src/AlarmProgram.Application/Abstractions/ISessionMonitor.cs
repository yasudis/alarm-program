using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface ISessionMonitor : IDisposable
{
    event EventHandler<MachineEvent>? SessionEventDetected;

    void Start();
}
