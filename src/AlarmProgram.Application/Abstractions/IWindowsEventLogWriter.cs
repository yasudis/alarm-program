namespace AlarmProgram.Application.Abstractions;

public interface IWindowsEventLogWriter
{
    void WriteWarning(string message);

    void WriteError(string message);
}
