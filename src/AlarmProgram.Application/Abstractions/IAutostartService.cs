namespace AlarmProgram.Application.Abstractions;

public interface IAutostartService
{
    bool IsEnabled { get; }

    void SetEnabled(bool enabled);
}
