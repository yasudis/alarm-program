using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.ProcessWatch;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlarmProgram.Tests.Unit.ProcessWatch;

public class SystemProcessWatchdogTests
{
    [Fact]
    public void Poll_emits_once_when_watched_process_is_missing()
    {
        var watchdog = new SystemProcessWatchdog(NullLogger<SystemProcessWatchdog>.Instance);
        var captured = new List<MachineEvent>();
        watchdog.ProcessEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        watchdog.Start();

        var settings = new UserSettings
        {
            NotifyOnProcessDown = true,
            WatchedProcessNames = "definitely-missing-process-xyz987"
        };

        watchdog.Poll(settings);
        watchdog.Poll(settings);

        var alert = Assert.Single(captured);
        Assert.Equal(MachineEventType.ProcessDown, alert.Type);
        Assert.Contains("definitely-missing-process-xyz987", alert.Message);
    }

    [Fact]
    public void Poll_does_not_emit_for_running_process()
    {
        var current = Environment.ProcessPath is not null
            ? Path.GetFileNameWithoutExtension(Environment.ProcessPath)
            : "dotnet";
        var watchdog = new SystemProcessWatchdog(NullLogger<SystemProcessWatchdog>.Instance);
        var captured = new List<MachineEvent>();
        watchdog.ProcessEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        watchdog.Start();

        var settings = new UserSettings
        {
            NotifyOnProcessDown = true,
            WatchedProcessNames = current
        };

        watchdog.Poll(settings);

        Assert.Empty(captured);
    }

    [Fact]
    public void Poll_skips_when_watchdog_disabled()
    {
        var watchdog = new SystemProcessWatchdog(NullLogger<SystemProcessWatchdog>.Instance);
        var captured = new List<MachineEvent>();
        watchdog.ProcessEventDetected += (_, machineEvent) => captured.Add(machineEvent);
        watchdog.Start();

        watchdog.Poll(new UserSettings
        {
            NotifyOnProcessDown = false,
            WatchedProcessNames = "definitely-missing-process-xyz987"
        });

        Assert.Empty(captured);
    }
}
