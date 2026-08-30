using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.UI.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly IAutostartService _autostartService;
    private readonly IReadOnlyList<INotificationChannel> _channels;
    private readonly ILogger<SettingsViewModel> _logger;

    public SettingsViewModel(
        ISettingsStore settingsStore,
        IAutostartService autostartService,
        IEnumerable<INotificationChannel> channels,
        ILogger<SettingsViewModel> logger)
    {
        _settingsStore = settingsStore;
        _autostartService = autostartService;
        _channels = channels.ToArray();
        _logger = logger;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveSettingsCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendTestAlertCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportSettingsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportSettingsCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _telegramBotToken = string.Empty;

    [ObservableProperty]
    private string _telegramChatId = string.Empty;

    [ObservableProperty]
    private string _discordWebhookUrl = string.Empty;

    [ObservableProperty]
    private string _webhookUrl = string.Empty;

    [ObservableProperty]
    private bool _telegramEnabled;

    [ObservableProperty]
    private bool _discordEnabled;

    [ObservableProperty]
    private bool _webhookEnabled;

    [ObservableProperty]
    private bool _emailEnabled;

    [ObservableProperty]
    private string _smtpHost = string.Empty;

    [ObservableProperty]
    private int _smtpPort = 587;

    [ObservableProperty]
    private string _smtpUser = string.Empty;

    [ObservableProperty]
    private string _smtpPassword = string.Empty;

    [ObservableProperty]
    private string _smtpFrom = string.Empty;

    [ObservableProperty]
    private string _smtpTo = string.Empty;

    [ObservableProperty]
    private bool _smtpUseSsl = true;

    [ObservableProperty]
    private bool _notifyOnStartup = true;

    [ObservableProperty]
    private bool _notifyOnShutdown = true;

    [ObservableProperty]
    private bool _notifyOnRestart = true;

    [ObservableProperty]
    private bool _notifyOnUnexpectedShutdown = true;

    [ObservableProperty]
    private bool _notifyOnUserLogon;

    [ObservableProperty]
    private bool _notifyOnUserLogoff;

    [ObservableProperty]
    private bool _notifyOnIpChange;

    [ObservableProperty]
    private bool _notifyOnNetworkOffline = true;

    [ObservableProperty]
    private bool _notifyOnNetworkOnline = true;

    [ObservableProperty]
    private bool _notifyOnSystemResume;

    [ObservableProperty]
    private bool _notifyOnSessionLock;

    [ObservableProperty]
    private bool _notifyOnSessionUnlock;

    [ObservableProperty]
    private bool _notifyOnLowDiskSpace = true;

    [ObservableProperty]
    private bool _notifyOnBatteryLow = true;

    [ObservableProperty]
    private bool _notifyOnAcPowerLost = true;

    [ObservableProperty]
    private bool _notifyOnAcPowerRestored = true;

    [ObservableProperty]
    private bool _notifyOnProcessDown;

    [ObservableProperty]
    private string _watchedProcessNames = string.Empty;

    [ObservableProperty]
    private bool _notifyOnHighCpu;

    [ObservableProperty]
    private bool _notifyOnHighMemory;

    [ObservableProperty]
    private int _highCpuThresholdPercent = 90;

    [ObservableProperty]
    private int _highMemoryThresholdPercent = 90;

    [ObservableProperty]
    private bool _notifyOnRdpConnected;

    [ObservableProperty]
    private bool _notifyOnRdpDisconnected;

    [ObservableProperty]
    private bool _notifyOnServiceDown;

    [ObservableProperty]
    private string _watchedServiceNames = string.Empty;

    [ObservableProperty]
    private bool _notifyOnUsbConnected;

    [ObservableProperty]
    private bool _notifyOnUsbDisconnected;

    [ObservableProperty]
    private bool _dailyDigestEnabled;

    [ObservableProperty]
    private string _dailyDigestTime = "09:00";

    [ObservableProperty]
    private int _journalRetentionDays;

    [ObservableProperty]
    private bool _notifyOnFailedLogon = true;

    [ObservableProperty]
    private bool _notifyOnApplicationCrash = true;

    [ObservableProperty]
    private bool _notifyOnRebootPending = true;

    [ObservableProperty]
    private bool _notifyOnBlueScreen = true;

    [ObservableProperty]
    private bool _notifyOnWindowsUpdateFailed = true;

    [ObservableProperty]
    private bool _notifyOnDefenderThreat = true;

    [ObservableProperty]
    private bool _notifyOnAdminGroupChanged = true;

    [ObservableProperty]
    private bool _notifyOnHostUnreachable;

    [ObservableProperty]
    private string _watchedHosts = string.Empty;

    [ObservableProperty]
    private bool _notifyOnHttpEndpointDown;

    [ObservableProperty]
    private string _watchedHttpEndpoints = string.Empty;

    [ObservableProperty]
    private bool _playSoundOnCriticalAlerts = true;

    [ObservableProperty]
    private bool _showTrayBalloonOnCriticalAlerts = true;

    [ObservableProperty]
    private int _lowDiskSpaceThresholdPercent = 10;

    [ObservableProperty]
    private int _batteryLowThresholdPercent = 15;

    [ObservableProperty]
    private int _alertCooldownMinutes;

    [ObservableProperty]
    private bool _heartbeatEnabled;

    [ObservableProperty]
    private int _heartbeatIntervalMinutes = 60;

    [ObservableProperty]
    private bool _quietHoursEnabled;

    [ObservableProperty]
    private string _quietHoursStart = "23:00";

    [ObservableProperty]
    private string _quietHoursEnd = "07:00";

    [ObservableProperty]
    private bool _runAtWindowsStartup;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _alertBodyTemplate = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private string _saveResultMessage = string.Empty;

    [ObservableProperty]
    private string _testResultMessage = string.Empty;

    public bool NeedsOnboarding => !TelegramEnabled && !DiscordEnabled && !WebhookEnabled && !EmailEnabled;

    partial void OnTelegramEnabledChanged(bool value) => OnPropertyChanged(nameof(NeedsOnboarding));

    partial void OnDiscordEnabledChanged(bool value) => OnPropertyChanged(nameof(NeedsOnboarding));

    partial void OnWebhookEnabledChanged(bool value) => OnPropertyChanged(nameof(NeedsOnboarding));

    partial void OnEmailEnabledChanged(bool value) => OnPropertyChanged(nameof(NeedsOnboarding));

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        SaveResultMessage = string.Empty;
        TestResultMessage = string.Empty;

        try
        {
            var settings = await _settingsStore.LoadAsync(cancellationToken);
            Apply(settings);
            ValidationMessage = string.Empty;
        }
        catch (Exception ex)
        {
            SaveResultMessage = "Не удалось загрузить настройки. Проверьте логи.";
            _logger.LogError(ex, "Ошибка загрузки пользовательских настроек");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private async Task SaveSettingsAsync()
    {
        IsBusy = true;
        TestResultMessage = string.Empty;

        try
        {
            var saved = await TrySaveCurrentSettingsAsync();
            if (saved)
            {
                SaveResultMessage = "Настройки сохранены.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private async Task SendTestAlertAsync()
    {
        IsBusy = true;
        TestResultMessage = string.Empty;

        try
        {
            if (!await TrySaveCurrentSettingsAsync())
            {
                return;
            }

            var testMessage = BuildTestAlert();
            var results = await DispatchTestAsync(testMessage);

            var successful = results.Where(result => result.IsSuccess).Select(result => result.Channel).ToArray();
            var failed = results.Where(result => !result.IsSuccess && !result.IsSkipped).ToArray();
            var skipped = results.Where(result => result.IsSkipped).ToArray();

            if (successful.Length > 0 && failed.Length == 0)
            {
                TestResultMessage = $"Тест отправлен успешно: {string.Join(", ", successful)}.";
                return;
            }

            if (successful.Length > 0)
            {
                var errors = string.Join("; ", failed.Select(item => $"{item.Channel}: {item.Error}"));
                TestResultMessage = $"Частичный успех ({string.Join(", ", successful)}). Ошибки: {errors}";
                return;
            }

            if (failed.Length > 0)
            {
                TestResultMessage = string.Join(
                    Environment.NewLine,
                    failed.Select(item => $"{item.Channel}: {item.Error}"));
                return;
            }

            if (skipped.Length > 0)
            {
                TestResultMessage = string.Join(
                    Environment.NewLine,
                    skipped.Select(item => $"{item.Channel}: {item.Error}"));
                return;
            }

            TestResultMessage = "Нет доступных каналов для тестовой отправки.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private async Task ExportSettingsAsync()
    {
        IsBusy = true;
        try
        {
            if (!await TrySaveCurrentSettingsAsync())
            {
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                FileName = "alarm-program-settings.json",
                AddExtension = true,
                DefaultExt = "json"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            await _settingsStore.ExportPlainAsync(dialog.FileName);
            SaveResultMessage = $"Настройки экспортированы: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка экспорта настроек");
            SaveResultMessage = "Не удалось экспортировать настройки.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private async Task ImportSettingsAsync()
    {
        IsBusy = true;
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            await _settingsStore.ImportPlainAsync(dialog.FileName);
            var settings = await _settingsStore.LoadAsync();
            Apply(settings);
            try
            {
                _autostartService.SetEnabled(settings.RunAtWindowsStartup);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Автозапуск не обновлен после импорта");
            }

            SaveResultMessage = "Настройки импортированы и сохранены.";
            ValidationMessage = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка импорта настроек");
            SaveResultMessage = "Не удалось импортировать настройки.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteActions() => !IsBusy;

    private async Task<bool> TrySaveCurrentSettingsAsync()
    {
        var settings = BuildSettingsModel();
        var validationErrors = settings.Validate();
        if (validationErrors.Count > 0)
        {
            ValidationMessage = string.Join(Environment.NewLine, validationErrors);
            SaveResultMessage = "Настройки не сохранены: исправьте ошибки.";
            return false;
        }

        ValidationMessage = string.Empty;

        try
        {
            await _settingsStore.SaveAsync(settings);
            try
            {
                _autostartService.SetEnabled(settings.RunAtWindowsStartup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось обновить автозапуск Windows");
                SaveResultMessage = "Настройки сохранены, но автозапуск не обновлен. См. логи.";
                return true;
            }

            return true;
        }
        catch (Exception ex)
        {
            SaveResultMessage = "Ошибка сохранения настроек. Подробности в логах.";
            _logger.LogError(ex, "Ошибка сохранения пользовательских настроек");
            return false;
        }
    }

    private async Task<IReadOnlyList<NotificationDispatchResult>> DispatchTestAsync(
        AlertMessage message,
        CancellationToken cancellationToken = default)
    {
        var results = new List<NotificationDispatchResult>();
        foreach (var channel in _channels)
        {
            try
            {
                if (channel is ITestableNotificationChannel testableChannel)
                {
                    results.Add(await testableChannel.SendWithResultAsync(message, cancellationToken));
                    continue;
                }

                await channel.SendAsync(message, cancellationToken);
                results.Add(NotificationDispatchResult.Success(channel.Name));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Ошибка тестовой отправки в канал {Channel}. CorrelationId={CorrelationId}",
                    channel.Name,
                    message.CorrelationId);
                results.Add(NotificationDispatchResult.Failed(channel.Name, ex.Message));
            }
        }

        return results;
    }

    private UserSettings BuildSettingsModel() => new()
    {
        TelegramBotToken = TelegramBotToken.Trim(),
        TelegramChatId = TelegramChatId.Trim(),
        DiscordWebhookUrl = string.IsNullOrWhiteSpace(DiscordWebhookUrl) ? null : DiscordWebhookUrl.Trim(),
        WebhookUrl = string.IsNullOrWhiteSpace(WebhookUrl) ? null : WebhookUrl.Trim(),
        TelegramEnabled = TelegramEnabled,
        DiscordEnabled = DiscordEnabled,
        WebhookEnabled = WebhookEnabled,
        EmailEnabled = EmailEnabled,
        SmtpHost = SmtpHost.Trim(),
        SmtpPort = SmtpPort,
        SmtpUser = SmtpUser.Trim(),
        SmtpPassword = SmtpPassword,
        SmtpFrom = SmtpFrom.Trim(),
        SmtpTo = SmtpTo.Trim(),
        SmtpUseSsl = SmtpUseSsl,
        NotifyOnStartup = NotifyOnStartup,
        NotifyOnShutdown = NotifyOnShutdown,
        NotifyOnRestart = NotifyOnRestart,
        NotifyOnUnexpectedShutdown = NotifyOnUnexpectedShutdown,
        NotifyOnUserLogon = NotifyOnUserLogon,
        NotifyOnUserLogoff = NotifyOnUserLogoff,
        NotifyOnIpChange = NotifyOnIpChange,
        NotifyOnNetworkOffline = NotifyOnNetworkOffline,
        NotifyOnNetworkOnline = NotifyOnNetworkOnline,
        NotifyOnSystemResume = NotifyOnSystemResume,
        NotifyOnSessionLock = NotifyOnSessionLock,
        NotifyOnSessionUnlock = NotifyOnSessionUnlock,
        NotifyOnLowDiskSpace = NotifyOnLowDiskSpace,
        NotifyOnBatteryLow = NotifyOnBatteryLow,
        NotifyOnAcPowerLost = NotifyOnAcPowerLost,
        NotifyOnAcPowerRestored = NotifyOnAcPowerRestored,
        NotifyOnProcessDown = NotifyOnProcessDown,
        WatchedProcessNames = WatchedProcessNames.Trim(),
        NotifyOnHighCpu = NotifyOnHighCpu,
        NotifyOnHighMemory = NotifyOnHighMemory,
        HighCpuThresholdPercent = HighCpuThresholdPercent,
        HighMemoryThresholdPercent = HighMemoryThresholdPercent,
        NotifyOnRdpConnected = NotifyOnRdpConnected,
        NotifyOnRdpDisconnected = NotifyOnRdpDisconnected,
        NotifyOnServiceDown = NotifyOnServiceDown,
        WatchedServiceNames = WatchedServiceNames.Trim(),
        NotifyOnUsbConnected = NotifyOnUsbConnected,
        NotifyOnUsbDisconnected = NotifyOnUsbDisconnected,
        DailyDigestEnabled = DailyDigestEnabled,
        DailyDigestTime = ParseTimeOrDefault(DailyDigestTime, TimeSpan.FromHours(9)),
        JournalRetentionDays = JournalRetentionDays,
        NotifyOnFailedLogon = NotifyOnFailedLogon,
        NotifyOnApplicationCrash = NotifyOnApplicationCrash,
        NotifyOnRebootPending = NotifyOnRebootPending,
        NotifyOnBlueScreen = NotifyOnBlueScreen,
        NotifyOnWindowsUpdateFailed = NotifyOnWindowsUpdateFailed,
        NotifyOnDefenderThreat = NotifyOnDefenderThreat,
        NotifyOnAdminGroupChanged = NotifyOnAdminGroupChanged,
        NotifyOnHostUnreachable = NotifyOnHostUnreachable,
        WatchedHosts = WatchedHosts.Trim(),
        NotifyOnHttpEndpointDown = NotifyOnHttpEndpointDown,
        WatchedHttpEndpoints = WatchedHttpEndpoints.Trim(),
        PlaySoundOnCriticalAlerts = PlaySoundOnCriticalAlerts,
        ShowTrayBalloonOnCriticalAlerts = ShowTrayBalloonOnCriticalAlerts,
        LowDiskSpaceThresholdPercent = LowDiskSpaceThresholdPercent,
        BatteryLowThresholdPercent = BatteryLowThresholdPercent,
        AlertCooldownMinutes = AlertCooldownMinutes,
        HeartbeatEnabled = HeartbeatEnabled,
        HeartbeatIntervalMinutes = HeartbeatIntervalMinutes,
        QuietHoursEnabled = QuietHoursEnabled,
        QuietHoursStart = ParseTimeOrDefault(QuietHoursStart, TimeSpan.FromHours(23)),
        QuietHoursEnd = ParseTimeOrDefault(QuietHoursEnd, TimeSpan.FromHours(7)),
        RunAtWindowsStartup = RunAtWindowsStartup,
        MinimizeToTray = MinimizeToTray,
        DisplayName = DisplayName.Trim(),
        AlertBodyTemplate = string.IsNullOrWhiteSpace(AlertBodyTemplate) ? null : AlertBodyTemplate
    };

    private void Apply(UserSettings settings)
    {
        TelegramBotToken = settings.TelegramBotToken;
        TelegramChatId = settings.TelegramChatId;
        DiscordWebhookUrl = settings.DiscordWebhookUrl ?? string.Empty;
        WebhookUrl = settings.WebhookUrl ?? string.Empty;
        TelegramEnabled = settings.TelegramEnabled;
        DiscordEnabled = settings.DiscordEnabled;
        WebhookEnabled = settings.WebhookEnabled;
        EmailEnabled = settings.EmailEnabled;
        SmtpHost = settings.SmtpHost ?? string.Empty;
        SmtpPort = settings.SmtpPort <= 0 ? 587 : settings.SmtpPort;
        SmtpUser = settings.SmtpUser ?? string.Empty;
        SmtpPassword = settings.SmtpPassword ?? string.Empty;
        SmtpFrom = settings.SmtpFrom ?? string.Empty;
        SmtpTo = settings.SmtpTo ?? string.Empty;
        SmtpUseSsl = settings.SmtpUseSsl;
        NotifyOnStartup = settings.NotifyOnStartup;
        NotifyOnShutdown = settings.NotifyOnShutdown;
        NotifyOnRestart = settings.NotifyOnRestart;
        NotifyOnUnexpectedShutdown = settings.NotifyOnUnexpectedShutdown;
        NotifyOnUserLogon = settings.NotifyOnUserLogon;
        NotifyOnUserLogoff = settings.NotifyOnUserLogoff;
        NotifyOnIpChange = settings.NotifyOnIpChange;
        NotifyOnNetworkOffline = settings.NotifyOnNetworkOffline;
        NotifyOnNetworkOnline = settings.NotifyOnNetworkOnline;
        NotifyOnSystemResume = settings.NotifyOnSystemResume;
        NotifyOnSessionLock = settings.NotifyOnSessionLock;
        NotifyOnSessionUnlock = settings.NotifyOnSessionUnlock;
        NotifyOnLowDiskSpace = settings.NotifyOnLowDiskSpace;
        NotifyOnBatteryLow = settings.NotifyOnBatteryLow;
        NotifyOnAcPowerLost = settings.NotifyOnAcPowerLost;
        NotifyOnAcPowerRestored = settings.NotifyOnAcPowerRestored;
        NotifyOnProcessDown = settings.NotifyOnProcessDown;
        WatchedProcessNames = settings.WatchedProcessNames ?? string.Empty;
        NotifyOnHighCpu = settings.NotifyOnHighCpu;
        NotifyOnHighMemory = settings.NotifyOnHighMemory;
        HighCpuThresholdPercent = settings.HighCpuThresholdPercent;
        HighMemoryThresholdPercent = settings.HighMemoryThresholdPercent;
        NotifyOnRdpConnected = settings.NotifyOnRdpConnected;
        NotifyOnRdpDisconnected = settings.NotifyOnRdpDisconnected;
        NotifyOnServiceDown = settings.NotifyOnServiceDown;
        WatchedServiceNames = settings.WatchedServiceNames ?? string.Empty;
        NotifyOnUsbConnected = settings.NotifyOnUsbConnected;
        NotifyOnUsbDisconnected = settings.NotifyOnUsbDisconnected;
        DailyDigestEnabled = settings.DailyDigestEnabled;
        DailyDigestTime = FormatTime(settings.DailyDigestTime);
        JournalRetentionDays = settings.JournalRetentionDays;
        NotifyOnFailedLogon = settings.NotifyOnFailedLogon;
        NotifyOnApplicationCrash = settings.NotifyOnApplicationCrash;
        NotifyOnRebootPending = settings.NotifyOnRebootPending;
        NotifyOnBlueScreen = settings.NotifyOnBlueScreen;
        NotifyOnWindowsUpdateFailed = settings.NotifyOnWindowsUpdateFailed;
        NotifyOnDefenderThreat = settings.NotifyOnDefenderThreat;
        NotifyOnAdminGroupChanged = settings.NotifyOnAdminGroupChanged;
        NotifyOnHostUnreachable = settings.NotifyOnHostUnreachable;
        WatchedHosts = settings.WatchedHosts ?? string.Empty;
        NotifyOnHttpEndpointDown = settings.NotifyOnHttpEndpointDown;
        WatchedHttpEndpoints = settings.WatchedHttpEndpoints ?? string.Empty;
        PlaySoundOnCriticalAlerts = settings.PlaySoundOnCriticalAlerts;
        ShowTrayBalloonOnCriticalAlerts = settings.ShowTrayBalloonOnCriticalAlerts;
        LowDiskSpaceThresholdPercent = settings.LowDiskSpaceThresholdPercent;
        BatteryLowThresholdPercent = settings.BatteryLowThresholdPercent;
        AlertCooldownMinutes = settings.AlertCooldownMinutes;
        HeartbeatEnabled = settings.HeartbeatEnabled;
        HeartbeatIntervalMinutes = settings.HeartbeatIntervalMinutes;
        QuietHoursEnabled = settings.QuietHoursEnabled;
        QuietHoursStart = FormatTime(settings.QuietHoursStart);
        QuietHoursEnd = FormatTime(settings.QuietHoursEnd);
        RunAtWindowsStartup = settings.RunAtWindowsStartup || _autostartService.IsEnabled;
        MinimizeToTray = settings.MinimizeToTray;
        DisplayName = settings.DisplayName ?? string.Empty;
        AlertBodyTemplate = settings.AlertBodyTemplate ?? string.Empty;
    }

    private static AlertMessage BuildTestAlert() => new()
    {
        EventType = MachineEventType.Startup,
        Subject = "Тестовое уведомление Alarm Program",
        Body =
            "Это тестовое уведомление из приложения Alarm Program." + Environment.NewLine
            + $"Хост: {Environment.MachineName}" + Environment.NewLine
            + $"Время: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
        CreatedAt = DateTimeOffset.UtcNow,
        HostName = Environment.MachineName,
        CorrelationId = Guid.NewGuid().ToString("N")
    };

    private static TimeSpan ParseTimeOrDefault(string value, TimeSpan fallback) =>
        TimeSpan.TryParse(value, out var parsed)
        && parsed >= TimeSpan.Zero
        && parsed < TimeSpan.FromDays(1)
            ? parsed
            : fallback;

    private static string FormatTime(TimeSpan value) =>
        $"{(int)value.TotalHours:00}:{value.Minutes:00}";
}
