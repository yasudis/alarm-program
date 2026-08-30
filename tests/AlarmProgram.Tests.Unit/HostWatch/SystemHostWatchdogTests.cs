using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.HostWatch;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlarmProgram.Tests.Unit.HostWatch;

public class SystemHostWatchdogTests
{
    [Fact]
    public async Task PollAsync_emits_once_when_host_is_unreachable()
    {
        var probe = new FakePingProbe(reachable: false);
        var watchdog = new SystemHostWatchdog(probe, NullLogger<SystemHostWatchdog>.Instance);
        var captured = new List<MachineEvent>();
        watchdog.HostEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        watchdog.Start();

        var settings = new UserSettings
        {
            NotifyOnHostUnreachable = true,
            WatchedHosts = "nas.local"
        };

        await watchdog.PollAsync(settings);
        await watchdog.PollAsync(settings);

        var alert = Assert.Single(captured);
        Assert.Equal(MachineEventType.HostUnreachable, alert.Type);
        Assert.Contains("nas.local", alert.Message);
        Assert.Equal(2, probe.Calls);
    }

    [Fact]
    public async Task PollAsync_does_not_emit_when_host_is_reachable()
    {
        var watchdog = new SystemHostWatchdog(new FakePingProbe(reachable: true), NullLogger<SystemHostWatchdog>.Instance);
        var captured = new List<MachineEvent>();
        watchdog.HostEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        watchdog.Start();

        await watchdog.PollAsync(new UserSettings
        {
            NotifyOnHostUnreachable = true,
            WatchedHosts = "8.8.8.8"
        });

        Assert.Empty(captured);
    }

    [Fact]
    public async Task PollAsync_skips_when_disabled()
    {
        var probe = new FakePingProbe(reachable: false);
        var watchdog = new SystemHostWatchdog(probe, NullLogger<SystemHostWatchdog>.Instance);
        var captured = new List<MachineEvent>();
        watchdog.HostEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        watchdog.Start();

        await watchdog.PollAsync(new UserSettings
        {
            NotifyOnHostUnreachable = false,
            WatchedHosts = "nas.local"
        });

        Assert.Empty(captured);
        Assert.Equal(0, probe.Calls);
    }

    [Fact]
    public async Task PollAsync_reemits_after_host_recovers_and_fails_again()
    {
        var probe = new FakePingProbe(reachable: false);
        var watchdog = new SystemHostWatchdog(probe, NullLogger<SystemHostWatchdog>.Instance);
        var captured = new List<MachineEvent>();
        watchdog.HostEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        watchdog.Start();
        var settings = new UserSettings { NotifyOnHostUnreachable = true, WatchedHosts = "nas.local" };

        await watchdog.PollAsync(settings);
        probe.Reachable = true;
        await watchdog.PollAsync(settings);
        probe.Reachable = false;
        await watchdog.PollAsync(settings);

        Assert.Equal(2, captured.Count);
    }

    private sealed class FakePingProbe : IIcmpPingProbe
    {
        public FakePingProbe(bool reachable) => Reachable = reachable;

        public bool Reachable { get; set; }

        public int Calls { get; private set; }

        public Task<bool> IsReachableAsync(string host, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Reachable);
        }
    }
}
