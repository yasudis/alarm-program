using AlarmProgram.Application.Abstractions;

namespace AlarmProgram.Infrastructure.Notifications;

public sealed class DeferredTrayBalloonNotifier : ITrayBalloonNotifier
{
    public Action<string, string>? Handler { get; set; }

    public void Show(string title, string text)
    {
        Handler?.Invoke(title, text);
    }
}
