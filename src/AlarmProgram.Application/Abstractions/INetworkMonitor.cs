using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface INetworkMonitor : IDisposable
{
    event EventHandler<MachineEvent>? NetworkEventDetected;

    string? CurrentPrimaryIp { get; }

    bool IsNetworkAvailable { get; }

    void Start();
}
