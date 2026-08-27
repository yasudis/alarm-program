using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.UI.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly IReadOnlyList<INotificationChannel> _channels;
    private readonly ILogger<SettingsViewModel> _logger;

    public SettingsViewModel(
        ISettingsStore settingsStore,
        IEnumerable<INotificationChannel> channels,
        ILogger<SettingsViewModel> logger)
    {
        _settingsStore = settingsStore;
        _channels = channels.ToArray();
        _logger = logger;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveSettingsCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendTestAlertCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _telegramBotToken = string.Empty;

    [ObservableProperty]
    private string _telegramChatId = string.Empty;

    [ObservableProperty]
    private string _discordWebhookUrl = string.Empty;

    [ObservableProperty]
    private bool _telegramEnabled;

    [ObservableProperty]
    private bool _discordEnabled;

    [ObservableProperty]
    private bool _notifyOnStartup = true;

    [ObservableProperty]
    private bool _notifyOnShutdown = true;

    [ObservableProperty]
    private bool _notifyOnRestart = true;

    [ObservableProperty]
    private bool _notifyOnUnexpectedShutdown = true;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private string _saveResultMessage = string.Empty;

    [ObservableProperty]
    private string _testResultMessage = string.Empty;

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
                _logger.LogError(ex, "Ошибка тестовой отправки в канал {Channel}", channel.Name);
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
        TelegramEnabled = TelegramEnabled,
        DiscordEnabled = DiscordEnabled,
        NotifyOnStartup = NotifyOnStartup,
        NotifyOnShutdown = NotifyOnShutdown,
        NotifyOnRestart = NotifyOnRestart,
        NotifyOnUnexpectedShutdown = NotifyOnUnexpectedShutdown
    };

    private void Apply(UserSettings settings)
    {
        TelegramBotToken = settings.TelegramBotToken;
        TelegramChatId = settings.TelegramChatId;
        DiscordWebhookUrl = settings.DiscordWebhookUrl ?? string.Empty;
        TelegramEnabled = settings.TelegramEnabled;
        DiscordEnabled = settings.DiscordEnabled;
        NotifyOnStartup = settings.NotifyOnStartup;
        NotifyOnShutdown = settings.NotifyOnShutdown;
        NotifyOnRestart = settings.NotifyOnRestart;
        NotifyOnUnexpectedShutdown = settings.NotifyOnUnexpectedShutdown;
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
        HostName = Environment.MachineName
    };
}
