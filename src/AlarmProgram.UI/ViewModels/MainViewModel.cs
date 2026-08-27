using System.Collections.ObjectModel;
using System.Windows;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmProgram.UI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly AppOptions _appOptions;
    private readonly IMonitoringController _monitoringController;
    private readonly IAlertJournal _alertJournal;
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(
        IOptions<AppOptions> appOptions,
        SettingsViewModel settingsViewModel,
        IMonitoringController monitoringController,
        IAlertJournal alertJournal,
        IDiagnosticsService diagnosticsService,
        ILogger<MainViewModel> logger)
    {
        _appOptions = appOptions.Value;
        Settings = settingsViewModel;
        _monitoringController = monitoringController;
        _alertJournal = alertJournal;
        _diagnosticsService = diagnosticsService;
        _logger = logger;

        ApplicationTitle = _appOptions.ApplicationName;
        StatusMessage = _monitoringController.StatusText;
        _monitoringController.StatusChanged += OnMonitoringStatusChanged;
    }

    public string ApplicationTitle { get; }

    public SettingsViewModel Settings { get; }

    public ObservableCollection<AlertJournalEntry> RecentAlerts { get; } = [];

    public bool IsMonitoringPaused => _monitoringController.IsPaused;

    [ObservableProperty]
    private string _statusMessage;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await Settings.InitializeAsync(cancellationToken);
        await RefreshJournalAsync(cancellationToken);
        StatusMessage = _monitoringController.StatusText;
    }

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void PauseMonitoring()
    {
        _monitoringController.Pause();
        RefreshMonitoringUi();
    }

    [RelayCommand(CanExecute = nameof(CanResume))]
    private void ResumeMonitoring()
    {
        _monitoringController.Resume();
        RefreshMonitoringUi();
    }

    [RelayCommand]
    private async Task RefreshJournalAsync()
    {
        await RefreshJournalAsync(CancellationToken.None);
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        try
        {
            _diagnosticsService.OpenLogsFolder();
            StatusMessage = $"Каталог логов: {_diagnosticsService.LogsDirectory}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось открыть каталог логов");
            StatusMessage = "Не удалось открыть каталог логов.";
        }
    }

    private bool CanPause() => !_monitoringController.IsPaused;

    private bool CanResume() => _monitoringController.IsPaused;

    private void OnMonitoringStatusChanged(object? sender, EventArgs e)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            RefreshMonitoringUi();
            return;
        }

        dispatcher.Invoke(RefreshMonitoringUi);
    }

    private void RefreshMonitoringUi()
    {
        StatusMessage = _monitoringController.StatusText;
        OnPropertyChanged(nameof(IsMonitoringPaused));
        PauseMonitoringCommand.NotifyCanExecuteChanged();
        ResumeMonitoringCommand.NotifyCanExecuteChanged();
    }

    private async Task RefreshJournalAsync(CancellationToken cancellationToken)
    {
        try
        {
            var entries = await _alertJournal.GetRecentAsync(20, cancellationToken);
            RecentAlerts.Clear();
            foreach (var entry in entries)
            {
                RecentAlerts.Add(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось обновить журнал алертов");
        }
    }
}
