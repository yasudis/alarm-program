using AlarmProgram.Application.Configuration;
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

        return services;
    }
}
