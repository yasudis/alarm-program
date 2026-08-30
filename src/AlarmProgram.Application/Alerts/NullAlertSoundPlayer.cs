using AlarmProgram.Application.Abstractions;

namespace AlarmProgram.Application.Alerts;

public sealed class NullAlertSoundPlayer : IAlertSoundPlayer
{
    public static NullAlertSoundPlayer Instance { get; } = new();

    public void PlayCritical()
    {
    }
}
