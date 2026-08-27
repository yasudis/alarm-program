using AlarmProgram.UI.Services;
using AlarmProgram.UI.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace AlarmProgram.UI;

public partial class MainWindow : Window
{
    private readonly TrayIconService _trayIconService;
    private bool _forceClose;

    public MainWindow(MainViewModel viewModel, TrayIconService trayIconService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _trayIconService = trayIconService;
        _trayIconService.Initialize();
    }

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (WindowState != WindowState.Minimized)
        {
            return;
        }

        if (DataContext is MainViewModel viewModel && viewModel.Settings.MinimizeToTray)
        {
            Hide();
            _trayIconService.ShowBalloon("Alarm Program", "Приложение свернуто в трей. Мониторинг продолжает работать.");
        }
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_forceClose)
        {
            return;
        }

        if (DataContext is MainViewModel viewModel && viewModel.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            Hide();
            _trayIconService.ShowBalloon("Alarm Program", "Приложение продолжает работать в трее.");
        }
    }
}
