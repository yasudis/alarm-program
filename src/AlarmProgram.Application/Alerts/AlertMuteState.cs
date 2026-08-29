namespace AlarmProgram.Application.Alerts;

public sealed class AlertMuteState : IAlertMuteState
{
    private readonly object _sync = new();
    private DateTimeOffset? _mutedUntil;

    public DateTimeOffset? MutedUntil
    {
        get
        {
            lock (_sync)
            {
                return _mutedUntil;
            }
        }
    }

    public bool IsMuted
    {
        get
        {
            lock (_sync)
            {
                return _mutedUntil is { } until && until > DateTimeOffset.UtcNow;
            }
        }
    }

    public event EventHandler? Changed;

    public void MuteFor(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            ClearMute();
            return;
        }

        lock (_sync)
        {
            _mutedUntil = DateTimeOffset.UtcNow.Add(duration);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ClearMute()
    {
        lock (_sync)
        {
            _mutedUntil = null;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool IsActiveAt(DateTimeOffset timestamp)
    {
        lock (_sync)
        {
            return _mutedUntil is { } until && timestamp < until;
        }
    }
}
