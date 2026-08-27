using AlarmProgram.Application;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Infrastructure.Autostart;
using AlarmProgram.Infrastructure.Diagnostics;
using AlarmProgram.Infrastructure.Events;
using AlarmProgram.Infrastructure.Journal;
using AlarmProgram.Infrastructure.Network;
using AlarmProgram.Infrastructure.Notifications;
using AlarmProgram.Infrastructure.Outbox;
using AlarmProgram.Infrastructure.Power;
using AlarmProgram.Infrastructure.Security;
using AlarmProgram.Infrastructure.Settings;
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
        services.AddSingleton<INetworkMonitor, SystemNetworkMonitor>();
        services.AddSingleton<IPowerEventMonitor, WindowsPowerEventMonitor>();
        services.AddSingleton<IEventCollector, WindowsEventLogReader>();
        services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(30) });
        services.AddSingleton<INotificationChannel, TelegramNotificationChannel>();
        services.AddSingleton<INotificationChannel, DiscordNotificationChannel>();

        return services;
    }
}
