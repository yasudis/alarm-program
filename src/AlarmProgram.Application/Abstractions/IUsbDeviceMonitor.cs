using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IUsbDeviceMonitor : IDisposable
{
    event EventHandler<MachineEvent>? UsbEventDetected;

    void Start();

    void Poll(UserSettings settings);
}
