namespace AlarmProgram.Application.Abstractions;

public interface IDiagnosticsService
{
    string LogsDirectory { get; }

    void OpenLogsFolder();
}
