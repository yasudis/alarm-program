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
                ["Notifications:RetryDelaySeconds"] = "4"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<AppOptions>().BindConfiguration(AppOptions.SectionName);
        services.AddOptions<NotificationsOptions>().BindConfiguration(NotificationsOptions.SectionName);

        using var provider = services.BuildServiceProvider();

        var appOptions = provider.GetRequiredService<IOptions<AppOptions>>().Value;
        var notificationsOptions = provider.GetRequiredService<IOptions<NotificationsOptions>>().Value;

        Assert.Equal("Alarm Program Test", appOptions.ApplicationName);
        Assert.Equal("Development", appOptions.Environment);
        Assert.Equal(5, notificationsOptions.DefaultRetryCount);
        Assert.Equal(4, notificationsOptions.RetryDelaySeconds);
    }
}
