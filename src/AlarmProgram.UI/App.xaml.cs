using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Infrastructure;
using AlarmProgram.UI.Services;
using AlarmProgram.UI.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System.IO;
using System.Windows;

namespace AlarmProgram.UI;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private TrayIconService? _trayIconService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .UseSerilog((context, _, loggerConfiguration) =>
            {
                var fileSection = context.Configuration.GetSection(LoggingFileOptions.SectionName);
                var logPath = ExpandPath(
                    fileSection["Path"] ?? "%AppData%/AlarmProgram/logs/alarm-.log");
                var retainedFileCountLimit = int.TryParse(
                    fileSection["RetainedFileCountLimit"],
                    out var count)
                    ? count
                    : 14;

                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

                loggerConfiguration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.File(
                        logPath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: retainedFileCountLimit,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
            })
            .ConfigureServices((_, services) =>
            {
                services.AddInfrastructure();
                services.AddSingleton<MonitoringHostedService>();
                services.AddSingleton<IMonitoringController>(sp => sp.GetRequiredService<MonitoringHostedService>());
                services.AddHostedService(sp => sp.GetRequiredService<MonitoringHostedService>());
                services.AddSingleton<TrayIconService>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        var appOptions = _host.Services.GetRequiredService<IOptions<AppOptions>>().Value;
        var settings = await _host.Services.GetRequiredService<ISettingsStore>().LoadAsync();

        logger.LogInformation(
            "Приложение {ApplicationName} запущено в окружении {Environment}. Telegram: {TelegramEnabled}, Discord: {DiscordEnabled}",
            appOptions.ApplicationName,
            appOptions.Environment,
            settings.TelegramEnabled,
            settings.DiscordEnabled);

        if (!settings.HasEnabledChannel)
        {
            _host.Services.GetRequiredService<IWindowsEventLogWriter>()
                .WriteWarning("Alarm Program запущен без настроенных каналов уведомлений.");
        }

        var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
        await mainViewModel.InitializeAsync();
        _trayIconService = _host.Services.GetRequiredService<TrayIconService>();
        _host.Services.GetRequiredService<MainWindow>().Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _trayIconService?.Dispose();

        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        await Log.CloseAndFlushAsync();
        base.OnExit(e);
    }

    private static string ExpandPath(string path) =>
        path.Replace("%AppData%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
}
