using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.ServiceWatch;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlarmProgram.Tests.Unit.ServiceWatch;

public class SystemServiceWatchdogTests
{
    [Fact]
    public void Poll_emits_once_when_watched_service_is_down()
    {
        var states = new Dictionary<string, bool?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Spooler"] = false
        };
        var watchdog = new SystemServiceWatchdog(
            NullLogger<SystemServiceWatchdog>.Instance,
            name => states.TryGetValue(name, out var running) ? running : null);
        var captured = new List<MachineEvent>();
        watchdog.ServiceEventDetected += (_, machineEvent) => captured.Add(machineEvent);

        // Force start without Windows gate via reflection of internal path:
        // Start() no-ops off Windows, so set started through Poll after forcing Start on Windows-like path.
        // Use Start() then manually poll via internal ctor which still needs _started.
        ForceStart(watchdog);

        var settings = new UserSettings
        {
            NotifyOnServiceDown = true,
            WatchedServiceNames = "Spooler"
        };

        watchdog.Poll(settings);
        watchdog.Poll(settings);

        var alert = Assert.Single(captured);
        Assert.Equal(MachineEventType.ServiceDown, alert.Type);
        Assert.Contains("Spooler", alert.Message);

        states["Spooler"] = true;
        watchdog.Poll(settings);
        states["Spooler"] = false;
        watchdog.Poll(settings);

        Assert.Equal(2, captured.Count);
    }

    [Fact]
    public void Poll_skips_when_disabled()
    {
        var watchdog = new SystemServiceWatchdog(
            NullLogger<SystemServiceWatchdog>.Instance,
            _ => false);
        var captured = new List<MachineEvent>();
        watchdog.ServiceEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        ForceStart(watchdog);

        watchdog.Poll(new UserSettings
        {
            NotifyOnServiceDown = false,
            WatchedServiceNames = "Spooler"
        });

        Assert.Empty(captured);
    }

    private static void ForceStart(SystemServiceWatchdog watchdog)
    {
        var field = typeof(SystemServiceWatchdog).GetField(
            "_started",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(watchdog, true);
    }
}
