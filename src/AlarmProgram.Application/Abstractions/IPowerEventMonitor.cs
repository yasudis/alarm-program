using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IPowerEventMonitor : IDisposable
{
    event EventHandler<MachineEvent>? PowerEventDetected;

    void Start();
}
