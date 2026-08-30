using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IHostPingWatchdog : IDisposable
{
    event EventHandler<MachineEvent>? HostEventDetected;

    void Start();

    void Poll(UserSettings settings);
}
