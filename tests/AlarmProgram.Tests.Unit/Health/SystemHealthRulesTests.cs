using AlarmProgram.Application.Health;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Health;

public class SystemHealthRulesTests
{
    [Fact]
    public void LowDiskSpace_returns_alert_when_below_threshold()
    {
        var alert = SystemHealthRules.LowDiskSpace("C:\\", totalBytes: 1000, freeBytes: 50, thresholdPercent: 10);

        Assert.NotNull(alert);
        Assert.Equal(MachineEventType.LowDiskSpace, alert.Type);
        Assert.Contains("5%", alert.Message);
        Assert.Contains("C:\\", alert.Message);
    }

    [Fact]
    public void LowDiskSpace_returns_null_when_above_threshold()
    {
        Assert.Null(SystemHealthRules.LowDiskSpace("C:\\", 1000, 200, thresholdPercent: 10));
    }

    [Fact]
    public void BatteryLow_returns_alert_only_on_battery_below_threshold()
    {
        Assert.NotNull(SystemHealthRules.BatteryLow(10, onBattery: true, thresholdPercent: 15));
        Assert.Null(SystemHealthRules.BatteryLow(10, onBattery: false, thresholdPercent: 15));
        Assert.Null(SystemHealthRules.BatteryLow(40, onBattery: true, thresholdPercent: 15));
    }

    [Fact]
    public void AcPowerChange_detects_plug_and_unplug()
    {
        var lost = SystemHealthRules.AcPowerChange(previousOnAc: true, currentOnAc: false);
        var restored = SystemHealthRules.AcPowerChange(previousOnAc: false, currentOnAc: true);

        Assert.Equal(MachineEventType.AcPowerLost, lost!.Type);
        Assert.Equal(MachineEventType.AcPowerRestored, restored!.Type);
        Assert.Null(SystemHealthRules.AcPowerChange(true, true));
    }

    [Fact]
    public void Session_helpers_create_lock_and_unlock_events()
    {
        Assert.Equal(MachineEventType.SessionLock, SystemHealthRules.SessionLock().Type);
        Assert.Equal(MachineEventType.SessionUnlock, SystemHealthRules.SessionUnlock().Type);
    }

    [Fact]
    public void ProcessDown_returns_alert_only_when_process_is_missing()
    {
        var down = SystemHealthRules.ProcessDown("nginx", isRunning: false);
        Assert.NotNull(down);
        Assert.Equal(MachineEventType.ProcessDown, down.Type);
        Assert.Contains("nginx", down.Message);
        Assert.Null(SystemHealthRules.ProcessDown("nginx", isRunning: true));
        Assert.Null(SystemHealthRules.ProcessDown(" ", isRunning: false));
    }

    [Fact]
    public void HighCpu_and_HighMemory_alert_at_or_above_threshold()
    {
        Assert.NotNull(SystemHealthRules.HighCpu(90, thresholdPercent: 90));
        Assert.Null(SystemHealthRules.HighCpu(89, thresholdPercent: 90));
        Assert.NotNull(SystemHealthRules.HighMemory(95, thresholdPercent: 90));
        Assert.Null(SystemHealthRules.HighMemory(40, thresholdPercent: 90));
    }

    [Fact]
    public void Rdp_helpers_create_connect_and_disconnect_events()
    {
        Assert.Equal(MachineEventType.RdpConnected, SystemHealthRules.RdpConnected().Type);
        Assert.Equal(MachineEventType.RdpDisconnected, SystemHealthRules.RdpDisconnected().Type);
    }
}
