using AlarmProgram.Application.Configuration;
using AlarmProgram.Infrastructure;
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
                    .WriteTo.File(
                        logPath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: retainedFileCountLimit);
            })
            .ConfigureServices((_, services) =>
            {
                services.AddInfrastructure();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        var appOptions = _host.Services.GetRequiredService<IOptions<AppOptions>>().Value;

        logger.LogInformation(
            "Приложение {ApplicationName} запущено в окружении {Environment}",
            appOptions.ApplicationName,
            appOptions.Environment);

        _host.Services.GetRequiredService<MainWindow>().Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
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
