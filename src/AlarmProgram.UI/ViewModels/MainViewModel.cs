using System.Collections.ObjectModel;
using System.Reflection;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Alerts;
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
    private readonly IAlertMuteState _muteState;
    private readonly ISettingsStore _settingsStore;
    private readonly IAlertJournal _alertJournal;
    private readonly AlertOrchestrator _orchestrator;
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(
        IOptions<AppOptions> appOptions,
        SettingsViewModel settingsViewModel,
        IMonitoringController monitoringController,
        IAlertMuteState muteState,
        ISettingsStore settingsStore,
        IAlertJournal alertJournal,
        AlertOrchestrator orchestrator,
        IDiagnosticsService diagnosticsService,
        ILogger<MainViewModel> logger)
    {
        _appOptions = appOptions.Value;
        Settings = settingsViewModel;
        _monitoringController = monitoringController;
        _muteState = muteState;
        _settingsStore = settingsStore;
        _alertJournal = alertJournal;
        _orchestrator = orchestrator;
        _diagnosticsService = diagnosticsService;
        _logger = logger;

        ApplicationVersion = ResolveVersion();
        ApplicationTitle = $"{_appOptions.ApplicationName} {ApplicationVersion}";
        StatusMessage = _monitoringController.StatusText;
        _monitoringController.StatusChanged += OnMonitoringStatusChanged;
        _muteState.Changed += OnMuteChanged;
    }

    public string ApplicationTitle { get; }

    public string ApplicationVersion { get; }

    public SettingsViewModel Settings { get; }

    public ObservableCollection<AlertJournalEntry> RecentAlerts { get; } = [];

    public bool IsMonitoringPaused => _monitoringController.IsPaused;

    public bool IsMuted => _muteState.IsMuted;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _setupHint = string.Empty;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await Settings.InitializeAsync(cancellationToken);
        await RefreshJournalAsync(cancellationToken);
        await RefreshSetupHintAsync(cancellationToken);
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

    [RelayCommand]
    private void MuteFor30Minutes()
    {
        _muteState.MuteFor(TimeSpan.FromMinutes(30));
        StatusMessage = _monitoringController.StatusText;
        OnPropertyChanged(nameof(IsMuted));
    }

    [RelayCommand]
    private void ClearMute()
    {
        _muteState.ClearMute();
        StatusMessage = _monitoringController.StatusText;
        OnPropertyChanged(nameof(IsMuted));
    }

    [RelayCommand]
    private async Task ExportJournalAsync()
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"alarm-journal-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
                AddExtension = true,
                DefaultExt = "csv"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            await _alertJournal.ExportCsvAsync(dialog.FileName);
            StatusMessage = $"Журнал экспортирован: {dialog.FileName}";
            await RefreshJournalAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось экспортировать журнал алертов");
            StatusMessage = "Не удалось экспортировать журнал.";
        }
    }

    [RelayCommand]
    private async Task ClearJournalAsync()
    {
        try
        {
            var confirm = System.Windows.MessageBox.Show(
                "Очистить журнал последних алертов?",
                "Alarm Program",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            if (confirm != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }

            await _alertJournal.ClearAsync();
            await RefreshJournalAsync(CancellationToken.None);
            StatusMessage = "Журнал алертов очищен.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось очистить журнал алертов");
            StatusMessage = "Не удалось очистить журнал.";
        }
    }

    [RelayCommand]
    private async Task RetryOutboxAsync()
    {
        try
        {
            await _orchestrator.FlushOutboxAsync();
            await RefreshJournalAsync(CancellationToken.None);
            StatusMessage = "Повторная отправка outbox выполнена.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось повторить отправку из outbox");
            StatusMessage = "Не удалось повторить отправку из outbox.";
        }
    }

    private bool CanPause() => !_monitoringController.IsPaused;

    private bool CanResume() => _monitoringController.IsPaused;

    private void OnMuteChanged(object? sender, EventArgs e) => OnMonitoringStatusChanged(sender, e);

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
        OnPropertyChanged(nameof(IsMuted));
        PauseMonitoringCommand.NotifyCanExecuteChanged();
        ResumeMonitoringCommand.NotifyCanExecuteChanged();
    }

    private async Task RefreshSetupHintAsync(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _settingsStore.LoadAsync(cancellationToken);
            SetupHint = settings.HasEnabledChannel
                ? string.Empty
                : "Первый запуск: включите Telegram, Discord или HTTPS webhook, заполните поля, сохраните настройки и нажмите «Тестовая отправка».";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось проверить настройки первого запуска");
            SetupHint = "Не удалось загрузить настройки. Проверьте файл settings.json.";
        }
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

    private static string ResolveVersion()
    {
        var assembly = typeof(App).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+');
            return plus > 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    }
}
