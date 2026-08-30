using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IHostWatchdog : IDisposable
{
    event EventHandler<MachineEvent>? HostEventDetected;

    void Start();

    Task PollAsync(UserSettings settings, CancellationToken cancellationToken = default);
}
