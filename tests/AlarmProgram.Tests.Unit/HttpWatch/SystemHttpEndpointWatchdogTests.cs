using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.HttpWatch;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlarmProgram.Tests.Unit.HttpWatch;

public class SystemHttpEndpointWatchdogTests
{
    [Fact]
    public async Task PollAsync_emits_once_when_endpoint_is_down()
    {
        var probe = new FakeHttpProbe(healthy: false);
        var watchdog = new SystemHttpEndpointWatchdog(probe, NullLogger<SystemHttpEndpointWatchdog>.Instance);
        var captured = new List<MachineEvent>();
        watchdog.HttpEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        watchdog.Start();

        var settings = new UserSettings
        {
            NotifyOnHttpEndpointDown = true,
            WatchedHttpEndpoints = "https://example.com/health"
        };

        await watchdog.PollAsync(settings);
        await watchdog.PollAsync(settings);

        var alert = Assert.Single(captured);
        Assert.Equal(MachineEventType.HttpEndpointDown, alert.Type);
        Assert.Contains("https://example.com/health", alert.Message);
        Assert.Equal(2, probe.Calls);
    }

    [Fact]
    public async Task PollAsync_does_not_emit_when_endpoint_is_healthy()
    {
        var watchdog = new SystemHttpEndpointWatchdog(new FakeHttpProbe(healthy: true), NullLogger<SystemHttpEndpointWatchdog>.Instance);
        var captured = new List<MachineEvent>();
        watchdog.HttpEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        watchdog.Start();

        await watchdog.PollAsync(new UserSettings
        {
            NotifyOnHttpEndpointDown = true,
            WatchedHttpEndpoints = "https://example.com/health"
        });

        Assert.Empty(captured);
    }

    [Fact]
    public async Task PollAsync_skips_when_disabled()
    {
        var probe = new FakeHttpProbe(healthy: false);
        var watchdog = new SystemHttpEndpointWatchdog(probe, NullLogger<SystemHttpEndpointWatchdog>.Instance);
        watchdog.Start();

        await watchdog.PollAsync(new UserSettings
        {
            NotifyOnHttpEndpointDown = false,
            WatchedHttpEndpoints = "https://example.com/health"
        });

        Assert.Equal(0, probe.Calls);
    }

    private sealed class FakeHttpProbe : IHttpEndpointProbe
    {
        public FakeHttpProbe(bool healthy) => Healthy = healthy;

        public bool Healthy { get; }

        public int Calls { get; private set; }

        public Task<bool> IsHealthyAsync(string url, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Healthy);
        }
    }
}
