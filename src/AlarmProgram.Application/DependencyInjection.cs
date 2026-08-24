using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Alerts;
using AlarmProgram.Application.Configuration;
using AlarmProgram.Application.Events;
using Microsoft.Extensions.DependencyInjection;

namespace AlarmProgram.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<AppOptions>()
            .BindConfiguration(AppOptions.SectionName);

        services.AddOptions<NotificationsOptions>()
            .BindConfiguration(NotificationsOptions.SectionName);

        services.AddOptions<LoggingFileOptions>()
            .BindConfiguration(LoggingFileOptions.SectionName);

        services.AddSingleton<IEventClassifier, EventClassifier>();
        services.AddSingleton<IAlertFormatter, AlertFormatter>();
        services.AddSingleton<AlertFilter>();
        services.AddSingleton<AlertOrchestrator>();

        return services;
    }
}
