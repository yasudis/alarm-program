using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface INotificationChannel
{
    string Name { get; }

    Task SendAsync(AlertMessage message, CancellationToken cancellationToken = default);
}
