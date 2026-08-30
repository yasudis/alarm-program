using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IFirewallMonitor : IDisposable
{
    event EventHandler<MachineEvent>? FirewallEventDetected;

    void Start();

    void Poll(UserSettings settings);
}
