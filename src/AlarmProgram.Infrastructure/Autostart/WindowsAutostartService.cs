using AlarmProgram.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace AlarmProgram.Infrastructure.Autostart;

public sealed class WindowsAutostartService : IAutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "AlarmProgram";

    private readonly ILogger<WindowsAutostartService> _logger;

    public WindowsAutostartService(ILogger<WindowsAutostartService> logger)
    {
        _logger = logger;
    }

    public bool IsEnabled
    {
        get
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var value = key?.GetValue(ValueName) as string;
            return !string.IsNullOrWhiteSpace(value);
        }
    }

    public void SetEnabled(bool enabled)
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Автозапуск доступен только на Windows");
            return;
        }

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (enabled)
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath))
            {
                throw new InvalidOperationException("Не удалось определить путь исполняемого файла.");
            }

            key.SetValue(ValueName, $"\"{exePath}\"");
            _logger.LogInformation("Автозапуск включен: {ExePath}", exePath);
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
        _logger.LogInformation("Автозапуск отключен");
    }
}
