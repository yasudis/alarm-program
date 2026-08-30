using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.Firewall;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlarmProgram.Tests.Unit.Firewall;

public class WindowsFirewallMonitorTests
{
    [Fact]
    public void Poll_emits_once_while_firewall_stays_disabled()
    {
        var disabled = true;
        var monitor = new WindowsFirewallMonitor(
            NullLogger<WindowsFirewallMonitor>.Instance,
            () => disabled);
        var captured = new List<MachineEvent>();
        monitor.FirewallEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        monitor.Start();

        var settings = new UserSettings { NotifyOnFirewallDisabled = true };
        monitor.Poll(settings);
        monitor.Poll(settings);

        var alert = Assert.Single(captured);
        Assert.Equal(MachineEventType.FirewallDisabled, alert.Type);
    }

    [Fact]
    public void Poll_emits_again_after_restore_and_disable()
    {
        var disabled = true;
        var monitor = new WindowsFirewallMonitor(
            NullLogger<WindowsFirewallMonitor>.Instance,
            () => disabled);
        var captured = new List<MachineEvent>();
        monitor.FirewallEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        monitor.Start();
        var settings = new UserSettings { NotifyOnFirewallDisabled = true };

        monitor.Poll(settings);
        disabled = false;
        monitor.Poll(settings);
        disabled = true;
        monitor.Poll(settings);

        Assert.Equal(2, captured.Count);
    }

    [Fact]
    public void Poll_skips_when_disabled()
    {
        var monitor = new WindowsFirewallMonitor(
            NullLogger<WindowsFirewallMonitor>.Instance,
            () => true);
        var captured = new List<MachineEvent>();
        monitor.FirewallEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        monitor.Start();

        monitor.Poll(new UserSettings { NotifyOnFirewallDisabled = false });

        Assert.Empty(captured);
    }
}
