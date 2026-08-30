using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.Reboot;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlarmProgram.Tests.Unit.Reboot;

public class WindowsPendingRebootMonitorTests
{
    [Fact]
    public void Poll_emits_once_while_reboot_stays_pending()
    {
        var pending = true;
        var monitor = new WindowsPendingRebootMonitor(
            NullLogger<WindowsPendingRebootMonitor>.Instance,
            () => pending);
        var captured = new List<MachineEvent>();
        monitor.RebootEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        monitor.Start();

        var settings = new UserSettings { NotifyOnRebootPending = true };
        monitor.Poll(settings);
        monitor.Poll(settings);

        var alert = Assert.Single(captured);
        Assert.Equal(MachineEventType.RebootPending, alert.Type);
    }

    [Fact]
    public void Poll_emits_again_after_pending_clears_and_returns()
    {
        var pending = true;
        var monitor = new WindowsPendingRebootMonitor(
            NullLogger<WindowsPendingRebootMonitor>.Instance,
            () => pending);
        var captured = new List<MachineEvent>();
        monitor.RebootEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        monitor.Start();
        var settings = new UserSettings { NotifyOnRebootPending = true };

        monitor.Poll(settings);
        pending = false;
        monitor.Poll(settings);
        pending = true;
        monitor.Poll(settings);

        Assert.Equal(2, captured.Count);
    }

    [Fact]
    public void Poll_skips_when_disabled()
    {
        var monitor = new WindowsPendingRebootMonitor(
            NullLogger<WindowsPendingRebootMonitor>.Instance,
            () => true);
        var captured = new List<MachineEvent>();
        monitor.RebootEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        monitor.Start();

        monitor.Poll(new UserSettings { NotifyOnRebootPending = false });

        Assert.Empty(captured);
    }

    [Fact]
    public void IsRebootPending_uses_injected_detector()
    {
        var monitor = new WindowsPendingRebootMonitor(
            NullLogger<WindowsPendingRebootMonitor>.Instance,
            () => true);

        Assert.True(monitor.IsRebootPending());
    }
}
