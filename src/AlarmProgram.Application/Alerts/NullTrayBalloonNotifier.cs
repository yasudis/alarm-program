using AlarmProgram.Application.Abstractions;

namespace AlarmProgram.Application.Alerts;

public sealed class NullTrayBalloonNotifier : ITrayBalloonNotifier
{
    public static NullTrayBalloonNotifier Instance { get; } = new();

    public void Show(string title, string text)
    {
    }
}
