using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Infrastructure.Notifications;
using AlarmProgram.UI.ViewModels;

namespace AlarmProgram.UI.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly MainViewModel _mainViewModel;
    private NotifyIcon? _notifyIcon;
    private bool _disposed;

    public TrayIconService(MainViewModel mainViewModel, ITrayBalloonNotifier trayBalloonNotifier)
    {
        _mainViewModel = mainViewModel;
        if (trayBalloonNotifier is DeferredTrayBalloonNotifier deferred)
        {
            deferred.Handler = ShowBalloon;
        }
    }

    public void Initialize()
    {
        if (!OperatingSystem.IsWindows() || _notifyIcon is not null)
        {
            return;
        }

        _notifyIcon = new NotifyIcon
        {
            Text = $"Alarm Program {_mainViewModel.ApplicationVersion}",
            Visible = true,
            Icon = SystemIcons.Information
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Открыть", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Пауза мониторинга", null, (_, _) => _mainViewModel.PauseMonitoringCommand.Execute(null));
        menu.Items.Add("Возобновить мониторинг", null, (_, _) => _mainViewModel.ResumeMonitoringCommand.Execute(null));
        menu.Items.Add("Открыть логи", null, (_, _) => _mainViewModel.OpenLogsFolderCommand.Execute(null));
        menu.Items.Add("Тишина 30 мин", null, (_, _) => _mainViewModel.MuteFor30MinutesCommand.Execute(null));
        menu.Items.Add("Тишина до 08:00", null, (_, _) => _mainViewModel.SnoozeUntilMorningCommand.Execute(null));
        menu.Items.Add("Снять тишину", null, (_, _) => _mainViewModel.ClearMuteCommand.Execute(null));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => System.Windows.Application.Current.Shutdown());
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    public void ShowBalloon(string title, string text)
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.ShowBalloonTip(2500);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }

    private static void ShowMainWindow()
    {
        var window = System.Windows.Application.Current.MainWindow;
        if (window is null)
        {
            return;
        }

        if (!window.IsVisible)
        {
            window.Show();
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }
}
