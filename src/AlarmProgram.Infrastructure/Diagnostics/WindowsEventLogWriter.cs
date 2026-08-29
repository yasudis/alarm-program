using System.Diagnostics;
using AlarmProgram.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.Diagnostics;

public sealed class WindowsEventLogWriter : IWindowsEventLogWriter
{
    public const string SourceName = "AlarmProgram";
    private const string LogName = "Application";
    private const int MaxMessageLength = 30000;

    private readonly ILogger<WindowsEventLogWriter> _logger;
    private readonly object _sync = new();
    private bool _sourceReady;
    private bool _sourceUnavailable;

    public WindowsEventLogWriter(ILogger<WindowsEventLogWriter> logger)
    {
        _logger = logger;
    }

    public void WriteWarning(string message) => Write(EventLogEntryType.Warning, message);

    public void WriteError(string message) => Write(EventLogEntryType.Error, message);

    private void Write(EventLogEntryType type, string message)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (!TryEnsureSource())
        {
            return;
        }

        try
        {
            var text = message.Length <= MaxMessageLength ? message : message[..MaxMessageLength] + "…";
            EventLog.WriteEntry(SourceName, text, type);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Не удалось записать в Windows Event Log");
        }
    }

    private bool TryEnsureSource()
    {
        lock (_sync)
        {
            if (_sourceReady)
            {
                return true;
            }

            if (_sourceUnavailable)
            {
                return false;
            }

            try
            {
                if (!EventLog.SourceExists(SourceName))
                {
                    EventLog.CreateEventSource(SourceName, LogName);
                }

                _sourceReady = true;
                return true;
            }
            catch (Exception ex)
            {
                _sourceUnavailable = true;
                _logger.LogInformation(
                    ex,
                    "Windows Event Log source {Source} недоступен (нужны права администратора при первом создании). Продолжаем без него.",
                    SourceName);
                return false;
            }
        }
    }
}
