namespace AlarmProgram.Application.Abstractions;

public interface ITrayBalloonNotifier
{
    void Show(string title, string text);
}
