namespace AlarmProgram.Application.Abstractions;

public interface IMonitoringController
{
    bool IsRunning { get; }

    bool IsPaused { get; }

    string StatusText { get; }

    event EventHandler? StatusChanged;

    void Pause();

    void Resume();
}
