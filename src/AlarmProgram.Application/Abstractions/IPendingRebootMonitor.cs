using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IPendingRebootMonitor : IDisposable
{
    event EventHandler<MachineEvent>? RebootEventDetected;

    void Start();

    void Poll(UserSettings settings);
}
