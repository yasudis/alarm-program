using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;

namespace AlarmProgram.Application.Alerts;

public sealed class AlertFormatter : IAlertFormatter
{
    public AlertMessage Format(MachineEvent machineEvent, UserSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(machineEvent);

        var machineName = string.IsNullOrWhiteSpace(machineEvent.HostName)
            ? Environment.MachineName
            : machineEvent.HostName;
        var displayName = settings?.DisplayName?.Trim();
        var hostLabel = string.IsNullOrWhiteSpace(displayName)
            ? machineName
            : $"{displayName} ({machineName})";
        var timestamp = machineEvent.OccurredAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
        var subject = SubjectFor(machineEvent.Type);
        var eventId = machineEvent.EventId?.ToString() ?? "-";
        var correlationId = Guid.NewGuid().ToString("N");
        var originalMessage = machineEvent.Message?.Trim() ?? string.Empty;

        string body;
        var template = settings?.AlertBodyTemplate?.Trim();
        if (!string.IsNullOrWhiteSpace(template))
        {
            body = ApplyTemplate(
                template,
                subject,
                hostLabel,
                displayName ?? string.Empty,
                machineName,
                timestamp,
                machineEvent.Type.ToString(),
                machineEvent.Source,
                eventId,
                originalMessage,
                correlationId);
        }
        else
        {
            body =
                $"{subject}{Environment.NewLine}" +
                $"Хост: {hostLabel}{Environment.NewLine}" +
                $"Время: {timestamp}{Environment.NewLine}" +
                $"Тип: {machineEvent.Type}{Environment.NewLine}" +
                $"Источник: {machineEvent.Source} (Event ID {eventId}){Environment.NewLine}" +
                $"CorrelationId: {correlationId}";

            if (!string.IsNullOrWhiteSpace(originalMessage))
            {
                body += $"{Environment.NewLine}{Environment.NewLine}{originalMessage}";
            }
        }

        return new AlertMessage
        {
            EventType = machineEvent.Type,
            Subject = subject,
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow,
            HostName = hostLabel,
            CorrelationId = correlationId
        };
    }

    private static string ApplyTemplate(
        string template,
        string subject,
        string hostLabel,
        string displayName,
        string machineName,
        string timestamp,
        string type,
        string source,
        string eventId,
        string message,
        string correlationId) =>
        template
            .Replace("{Subject}", subject, StringComparison.OrdinalIgnoreCase)
            .Replace("{Host}", hostLabel, StringComparison.OrdinalIgnoreCase)
            .Replace("{DisplayName}", displayName, StringComparison.OrdinalIgnoreCase)
            .Replace("{MachineName}", machineName, StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", timestamp, StringComparison.OrdinalIgnoreCase)
            .Replace("{Type}", type, StringComparison.OrdinalIgnoreCase)
            .Replace("{Source}", source, StringComparison.OrdinalIgnoreCase)
            .Replace("{EventId}", eventId, StringComparison.OrdinalIgnoreCase)
            .Replace("{Message}", message, StringComparison.OrdinalIgnoreCase)
            .Replace("{CorrelationId}", correlationId, StringComparison.OrdinalIgnoreCase);

    private static string SubjectFor(MachineEventType eventType) => eventType switch
    {
        MachineEventType.Startup => "ПК включился",
        MachineEventType.Shutdown => "ПК выключился",
        MachineEventType.Restart => "ПК перезагрузился",
        MachineEventType.UnexpectedShutdown => "Некорректное выключение ПК",
        MachineEventType.UserLogon => "Вход пользователя в Windows",
        MachineEventType.UserLogoff => "Выход пользователя из Windows",
        MachineEventType.Heartbeat => "Heartbeat: ПК в сети",
        MachineEventType.IpChanged => "Сменился IP-адрес",
        MachineEventType.NetworkOffline => "Сеть недоступна",
        MachineEventType.NetworkOnline => "Сеть восстановлена",
        MachineEventType.SystemResume => "ПК вышел из режима сна",
        MachineEventType.SessionLock => "Экран заблокирован",
        MachineEventType.SessionUnlock => "Экран разблокирован",
        MachineEventType.LowDiskSpace => "Мало места на диске",
        MachineEventType.BatteryLow => "Низкий заряд батареи",
        MachineEventType.AcPowerLost => "Переход на батарею",
        MachineEventType.AcPowerRestored => "Питание от сети восстановлено",
        MachineEventType.ProcessDown => "Процесс не запущен",
        MachineEventType.HighCpu => "Высокая загрузка CPU",
        MachineEventType.HighMemory => "Высокое использование памяти",
        MachineEventType.RdpConnected => "RDP-подключение",
        MachineEventType.RdpDisconnected => "RDP-отключение",
        _ => "Событие ПК"
    };
}
