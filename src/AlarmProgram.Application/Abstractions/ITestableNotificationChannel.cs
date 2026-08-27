using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface ITestableNotificationChannel
{
    Task<NotificationDispatchResult> SendWithResultAsync(
        AlertMessage message,
        CancellationToken cancellationToken = default);
}
