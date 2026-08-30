using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.HostPing;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlarmProgram.Tests.Unit.HostPing;

public class IcmpHostPingWatchdogTests
{
    [Fact]
    public void Poll_emits_unreachable_once_until_host_recovers()
    {
        var reachable = false;
        var watchdog = new IcmpHostPingWatchdog(
            NullLogger<IcmpHostPingWatchdog>.Instance,
            _ => reachable);
        var captured = new List<MachineEvent>();
        watchdog.HostEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        watchdog.Start();

        var settings = new UserSettings
        {
            NotifyOnHostUnreachable = true,
            NotifyOnHostRestored = true,
            WatchedHosts = "nas.local"
        };

        watchdog.Poll(settings);
        watchdog.Poll(settings);

        var down = Assert.Single(captured);
        Assert.Equal(MachineEventType.HostUnreachable, down.Type);
        Assert.Contains("nas.local", down.Message);
    }

    [Fact]
    public void Poll_emits_restored_after_host_comes_back()
    {
        var reachable = false;
        var watchdog = new IcmpHostPingWatchdog(
            NullLogger<IcmpHostPingWatchdog>.Instance,
            _ => reachable);
        var captured = new List<MachineEvent>();
        watchdog.HostEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        watchdog.Start();
        var settings = new UserSettings
        {
            NotifyOnHostUnreachable = true,
            NotifyOnHostRestored = true,
            WatchedHosts = "8.8.8.8"
        };

        watchdog.Poll(settings);
        reachable = true;
        watchdog.Poll(settings);

        Assert.Equal(2, captured.Count);
        Assert.Equal(MachineEventType.HostUnreachable, captured[0].Type);
        Assert.Equal(MachineEventType.HostRestored, captured[1].Type);
    }

    [Fact]
    public void Poll_skips_when_watchdog_flags_are_off()
    {
        var watchdog = new IcmpHostPingWatchdog(
            NullLogger<IcmpHostPingWatchdog>.Instance,
            _ => false);
        var captured = new List<MachineEvent>();
        watchdog.HostEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        watchdog.Start();

        watchdog.Poll(new UserSettings
        {
            NotifyOnHostUnreachable = false,
            WatchedHosts = "8.8.8.8"
        });

        Assert.Empty(captured);
    }
}
