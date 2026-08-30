using AlarmProgram.Application.Alerts;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Alerts;

public class AlertFormatterTests
{
    private readonly AlertFormatter _formatter = new();
    private static readonly DateTimeOffset OccurredAt = new(2026, 8, 24, 10, 30, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(MachineEventType.Startup, "ПК включился")]
    [InlineData(MachineEventType.Shutdown, "ПК выключился")]
    [InlineData(MachineEventType.Restart, "ПК перезагрузился")]
    [InlineData(MachineEventType.UnexpectedShutdown, "Некорректное выключение ПК")]
    [InlineData(MachineEventType.UserLogon, "Вход пользователя в Windows")]
    [InlineData(MachineEventType.UserLogoff, "Выход пользователя из Windows")]
    [InlineData(MachineEventType.Heartbeat, "Heartbeat: ПК в сети")]
    [InlineData(MachineEventType.IpChanged, "Сменился IP-адрес")]
    [InlineData(MachineEventType.NetworkOffline, "Сеть недоступна")]
    [InlineData(MachineEventType.NetworkOnline, "Сеть восстановлена")]
    [InlineData(MachineEventType.SystemResume, "ПК вышел из режима сна")]
    [InlineData(MachineEventType.SessionLock, "Экран заблокирован")]
    [InlineData(MachineEventType.SessionUnlock, "Экран разблокирован")]
    [InlineData(MachineEventType.LowDiskSpace, "Мало места на диске")]
    [InlineData(MachineEventType.BatteryLow, "Низкий заряд батареи")]
    [InlineData(MachineEventType.AcPowerLost, "Переход на батарею")]
    [InlineData(MachineEventType.AcPowerRestored, "Питание от сети восстановлено")]
    [InlineData(MachineEventType.ProcessDown, "Процесс не запущен")]
    [InlineData(MachineEventType.HighCpu, "Высокая загрузка CPU")]
    [InlineData(MachineEventType.HighMemory, "Высокое использование памяти")]
    [InlineData(MachineEventType.RdpConnected, "RDP-подключение")]
    [InlineData(MachineEventType.RdpDisconnected, "RDP-отключение")]
    public void Format_uses_stable_subject_and_includes_host_and_timestamp(
        MachineEventType eventType,
        string expectedSubject)
    {
        var alert = _formatter.Format(CreateEvent(eventType, "Kernel details"));

        Assert.Equal(expectedSubject, alert.Subject);
        Assert.Equal(eventType, alert.EventType);
        Assert.Equal("TEST-PC", alert.HostName);
        Assert.False(string.IsNullOrWhiteSpace(alert.CorrelationId));
        Assert.StartsWith(expectedSubject, alert.Body);
        Assert.Contains("Хост: TEST-PC", alert.Body);
        Assert.Contains("Время: 2026-08-24 10:30:00 UTC", alert.Body);
        Assert.Contains($"Тип: {eventType}", alert.Body);
        Assert.Contains("Источник: EventLog (Event ID 6005)", alert.Body);
        Assert.Contains("CorrelationId:", alert.Body);
        Assert.Contains("Kernel details", alert.Body);
    }

    [Fact]
    public void Format_omits_empty_original_message()
    {
        var alert = _formatter.Format(CreateEvent(MachineEventType.Startup, "  "));

        Assert.DoesNotContain("\r\n\r\n", alert.Body);
        Assert.DoesNotContain("\n\n", alert.Body);
        Assert.Contains("Хост: TEST-PC", alert.Body);
    }

    [Fact]
    public void Format_falls_back_to_machine_name_when_host_is_missing()
    {
        var machineEvent = new MachineEvent
        {
            Type = MachineEventType.Shutdown,
            OccurredAt = OccurredAt,
            Source = "EventLog",
            EventId = 6006,
            HostName = null,
            Message = null
        };

        var alert = _formatter.Format(machineEvent);

        Assert.Equal(Environment.MachineName, alert.HostName);
        Assert.Contains($"Хост: {Environment.MachineName}", alert.Body);
        Assert.Contains("Event ID 6006", alert.Body);
    }

    [Fact]
    public void Format_uses_display_name_and_custom_template()
    {
        var settings = new UserSettings
        {
            DisplayName = "Домашний ПК",
            AlertBodyTemplate = "{Subject} | {DisplayName} | {Host} | {Type} | {Message}"
        };

        var alert = _formatter.Format(CreateEvent(MachineEventType.IpChanged, "old -> new"), settings);

        Assert.Equal("Сменился IP-адрес", alert.Subject);
        Assert.Equal("Домашний ПК (TEST-PC)", alert.HostName);
        Assert.Contains("Домашний ПК", alert.Body);
        Assert.Contains("IpChanged", alert.Body);
        Assert.Contains("old -> new", alert.Body);
    }

    [Fact]
    public void Format_throws_when_event_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => _formatter.Format(null!));
    }

    private static MachineEvent CreateEvent(MachineEventType type, string? message) => new()
    {
        Type = type,
        OccurredAt = OccurredAt,
        Source = "EventLog",
        EventId = 6005,
        HostName = "TEST-PC",
        Message = message
    };
}
