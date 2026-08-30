using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IHttpEndpointWatchdog : IDisposable
{
    event EventHandler<MachineEvent>? HttpEventDetected;

    void Start();

    Task PollAsync(UserSettings settings, CancellationToken cancellationToken = default);
}
