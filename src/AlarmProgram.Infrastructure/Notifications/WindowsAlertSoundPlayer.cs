using System.Runtime.InteropServices;
using AlarmProgram.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.Notifications;

public sealed class WindowsAlertSoundPlayer : IAlertSoundPlayer
{
    private const uint MbIconExclamation = 0x00000030;
    private readonly ILogger<WindowsAlertSoundPlayer> _logger;

    public WindowsAlertSoundPlayer(ILogger<WindowsAlertSoundPlayer> logger)
    {
        _logger = logger;
    }

    public void PlayCritical()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            _ = NativeMethods.MessageBeep(MbIconExclamation);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось проиграть системный звук");
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MessageBeep(uint uType);
    }
}
