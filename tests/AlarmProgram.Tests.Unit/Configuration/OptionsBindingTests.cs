using AlarmProgram.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AlarmProgram.Tests.Unit.Configuration;

public class OptionsBindingTests
{
    [Fact]
    public void App_options_are_bound_from_configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:ApplicationName"] = "Alarm Program Test",
                ["App:Environment"] = "Development",
                ["Notifications:DefaultRetryCount"] = "5",
                ["Notifications:RetryDelaySeconds"] = "4",
                ["Monitoring:PollIntervalSeconds"] = "45",
                ["Monitoring:InitialLookbackMinutes"] = "15",
                ["Monitoring:RecoveryLookbackHours"] = "36",
                ["Monitoring:DeduplicationWindowSeconds"] = "240"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<AppOptions>().BindConfiguration(AppOptions.SectionName);
        services.AddOptions<NotificationsOptions>().BindConfiguration(NotificationsOptions.SectionName);
        services.AddOptions<MonitoringOptions>().BindConfiguration(MonitoringOptions.SectionName);

        using var provider = services.BuildServiceProvider();

        var appOptions = provider.GetRequiredService<IOptions<AppOptions>>().Value;
        var notificationsOptions = provider.GetRequiredService<IOptions<NotificationsOptions>>().Value;
        var monitoringOptions = provider.GetRequiredService<IOptions<MonitoringOptions>>().Value;

        Assert.Equal("Alarm Program Test", appOptions.ApplicationName);
        Assert.Equal("Development", appOptions.Environment);
        Assert.Equal(5, notificationsOptions.DefaultRetryCount);
        Assert.Equal(4, notificationsOptions.RetryDelaySeconds);
        Assert.Equal(45, monitoringOptions.PollIntervalSeconds);
        Assert.Equal(15, monitoringOptions.InitialLookbackMinutes);
        Assert.Equal(36, monitoringOptions.RecoveryLookbackHours);
        Assert.Equal(240, monitoringOptions.DeduplicationWindowSeconds);
    }
}
