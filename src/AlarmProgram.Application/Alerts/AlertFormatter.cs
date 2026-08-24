using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;

namespace AlarmProgram.Application.Alerts;

public sealed class AlertFormatter : IAlertFormatter
{
    public AlertMessage Format(MachineEvent machineEvent)
    {
        ArgumentNullException.ThrowIfNull(machineEvent);

        var hostName = string.IsNullOrWhiteSpace(machineEvent.HostName)
            ? Environment.MachineName
            : machineEvent.HostName;
        var timestamp = machineEvent.OccurredAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
        var subject = SubjectFor(machineEvent.Type);
        var eventId = machineEvent.EventId?.ToString() ?? "-";

        var body =
            $"{subject}{Environment.NewLine}" +
            $"Хост: {hostName}{Environment.NewLine}" +
            $"Время: {timestamp}{Environment.NewLine}" +
            $"Тип: {machineEvent.Type}{Environment.NewLine}" +
            $"Источник: {machineEvent.Source} (Event ID {eventId})";

        if (!string.IsNullOrWhiteSpace(machineEvent.Message))
        {
            body += $"{Environment.NewLine}{Environment.NewLine}{machineEvent.Message.Trim()}";
        }

        return new AlertMessage
        {
            EventType = machineEvent.Type,
            Subject = subject,
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow,
            HostName = hostName
        };
    }

    private static string SubjectFor(MachineEventType eventType) => eventType switch
    {
        MachineEventType.Startup => "ПК включился",
        MachineEventType.Shutdown => "ПК выключился",
        MachineEventType.Restart => "ПК перезагрузился",
        MachineEventType.UnexpectedShutdown => "Некорректное выключение ПК",
        _ => "Событие ПК"
    };
}
