using AlarmProgram.Application.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Options;

namespace AlarmProgram.UI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly AppOptions _appOptions;

    public MainViewModel(
        IOptions<AppOptions> appOptions,
        SettingsViewModel settingsViewModel)
    {
        _appOptions = appOptions.Value;
        ApplicationTitle = _appOptions.ApplicationName;
        StatusMessage = $"Мониторинг готов ({_appOptions.Environment})";
        Settings = settingsViewModel;
    }

    public string ApplicationTitle { get; }
    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    private string _statusMessage;

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        Settings.InitializeAsync(cancellationToken);
}
