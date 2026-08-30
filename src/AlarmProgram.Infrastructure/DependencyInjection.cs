using AlarmProgram.Application;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Infrastructure.Autostart;
using AlarmProgram.Infrastructure.Diagnostics;
using AlarmProgram.Infrastructure.Disk;
using AlarmProgram.Infrastructure.Events;
using AlarmProgram.Infrastructure.Journal;
using AlarmProgram.Infrastructure.Network;
using AlarmProgram.Infrastructure.Notifications;
using AlarmProgram.Infrastructure.Outbox;
using AlarmProgram.Infrastructure.Power;
using AlarmProgram.Infrastructure.ProcessWatch;
using AlarmProgram.Infrastructure.Reboot;
using AlarmProgram.Infrastructure.Resources;
using AlarmProgram.Infrastructure.Security;
using AlarmProgram.Infrastructure.ServiceWatch;
using AlarmProgram.Infrastructure.Session;
using AlarmProgram.Infrastructure.Settings;
using AlarmProgram.Infrastructure.Usb;
using Microsoft.Extensions.DependencyInjection;

namespace AlarmProgram.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddApplication();
        services.AddOptions<SettingsStoreOptions>();

        services.AddSingleton<ISecretProtector, DpapiSecretProtector>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<IAlertJournal, FileAlertJournal>();
        services.AddSingleton<IAlertOutbox, FileAlertOutbox>();
        services.AddSingleton<IAutostartService, WindowsAutostartService>();
        services.AddSingleton<IDiagnosticsService, FileDiagnosticsService>();
        services.AddSingleton<IWindowsEventLogWriter, WindowsEventLogWriter>();
        services.AddSingleton<INetworkMonitor, SystemNetworkMonitor>();
        services.AddSingleton<IPowerEventMonitor, WindowsPowerEventMonitor>();
        services.AddSingleton<ISessionMonitor, WindowsSessionMonitor>();
        services.AddSingleton<IDiskSpaceMonitor, SystemDiskSpaceMonitor>();
        services.AddSingleton<IProcessWatchdog, SystemProcessWatchdog>();
        services.AddSingleton<IServiceWatchdog, SystemServiceWatchdog>();
        services.AddSingleton<IUsbDeviceMonitor, RemovableDriveUsbMonitor>();
        services.AddSingleton<IResourceMonitor, WindowsResourceMonitor>();
        services.AddSingleton<IPendingRebootMonitor, WindowsPendingRebootMonitor>();
        services.AddSingleton<IEventCollector, WindowsEventLogReader>();
        services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(30) });
        services.AddSingleton<ISmtpMailSender, SystemSmtpMailSender>();
        services.AddSingleton<IAlertSoundPlayer, WindowsAlertSoundPlayer>();
        services.AddSingleton<ITrayBalloonNotifier, DeferredTrayBalloonNotifier>();
        services.AddSingleton<INotificationChannel, TelegramNotificationChannel>();
        services.AddSingleton<INotificationChannel, DiscordNotificationChannel>();
        services.AddSingleton<INotificationChannel, HttpWebhookNotificationChannel>();
        services.AddSingleton<INotificationChannel, SmtpNotificationChannel>();

        return services;
    }
}
