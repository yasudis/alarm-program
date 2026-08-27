using System.Diagnostics;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmProgram.Infrastructure.Diagnostics;

public sealed class FileDiagnosticsService : IDiagnosticsService
{
    private readonly ILogger<FileDiagnosticsService> _logger;

    public FileDiagnosticsService(
        IOptions<LoggingFileOptions> loggingOptions,
        ILogger<FileDiagnosticsService> logger)
    {
        _logger = logger;
        LogsDirectory = ResolveLogsDirectory(loggingOptions.Value.Path);
    }

    public string LogsDirectory { get; }

    public void OpenLogsFolder()
    {
        Directory.CreateDirectory(LogsDirectory);

        if (!OperatingSystem.IsWindows())
        {
            _logger.LogInformation("Каталог логов: {LogsDirectory}", LogsDirectory);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{LogsDirectory}\"",
            UseShellExecute = true
        });
    }

    private static string ResolveLogsDirectory(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "%AppData%/AlarmProgram/logs/alarm-.log"
            : configuredPath;

        path = path.Replace(
            "%AppData%",
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            StringComparison.OrdinalIgnoreCase);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        return string.IsNullOrWhiteSpace(directory)
            ? Path.GetFullPath("%AppData%/AlarmProgram/logs".Replace(
                "%AppData%",
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                StringComparison.OrdinalIgnoreCase))
            : directory;
    }
}
