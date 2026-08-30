namespace AlarmProgram.Application.Alerts;

public interface IAlertMuteState
{
    DateTimeOffset? MutedUntil { get; }

    bool IsMuted { get; }

    event EventHandler? Changed;

    void MuteFor(TimeSpan duration);

    void MuteUntil(DateTimeOffset until);

    void ClearMute();

    bool IsActiveAt(DateTimeOffset timestamp);
}
