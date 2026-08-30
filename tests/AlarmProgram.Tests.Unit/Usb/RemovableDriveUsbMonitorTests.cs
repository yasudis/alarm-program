using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.Usb;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlarmProgram.Tests.Unit.Usb;

public class RemovableDriveUsbMonitorTests
{
    [Fact]
    public void Poll_seeds_without_alerts_then_emits_connect_and_disconnect()
    {
        var drives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var monitor = new RemovableDriveUsbMonitor(
            NullLogger<RemovableDriveUsbMonitor>.Instance,
            () => drives);
        var captured = new List<MachineEvent>();
        monitor.UsbEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        monitor.Start();

        var settings = new UserSettings
        {
            NotifyOnUsbConnected = true,
            NotifyOnUsbDisconnected = true
        };

        drives["E:"] = "OLD";
        monitor.Poll(settings);
        Assert.Empty(captured);

        drives["F:"] = "NEW";
        monitor.Poll(settings);
        Assert.Single(captured);
        Assert.Equal(MachineEventType.UsbConnected, captured[0].Type);
        Assert.Contains("F:", captured[0].Message);

        drives.Remove("E:");
        monitor.Poll(settings);
        Assert.Equal(2, captured.Count);
        Assert.Equal(MachineEventType.UsbDisconnected, captured[1].Type);
        Assert.Contains("E:", captured[1].Message);
    }

    [Fact]
    public void Poll_respects_disabled_flags()
    {
        var drives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var monitor = new RemovableDriveUsbMonitor(
            NullLogger<RemovableDriveUsbMonitor>.Instance,
            () => drives);
        var captured = new List<MachineEvent>();
        monitor.UsbEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        monitor.Start();

        monitor.Poll(new UserSettings());
        drives["E:"] = "USB";
        monitor.Poll(new UserSettings
        {
            NotifyOnUsbConnected = false,
            NotifyOnUsbDisconnected = false
        });

        Assert.Empty(captured);
    }
}
