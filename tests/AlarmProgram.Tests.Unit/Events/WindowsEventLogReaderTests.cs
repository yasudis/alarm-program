using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Alerts;
using AlarmProgram.Infrastructure;
using AlarmProgram.Infrastructure.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlarmProgram.Tests.Unit.Events;

public class WindowsEventLogReaderTests
{
    [Fact]
    public async Task CollectAsync_returns_events_with_timestamp_id_and_source()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var reader = new WindowsEventLogReader(NullLogger<WindowsEventLogReader>.Instance);
        var events = await reader.CollectAsync(DateTimeOffset.UtcNow.AddDays(-7));

        Assert.All(events, rawEvent =>
        {
            Assert.False(string.IsNullOrWhiteSpace(rawEvent.Source));
            Assert.True(rawEvent.EventId >= 0);
            Assert.NotEqual(default, rawEvent.OccurredAt);
            Assert.Contains(rawEvent.EventId, WindowsEventLogReader.CandidateEventIds);
        });
    }

    [Fact]
    public void AddInfrastructure_registers_event_collector_and_settings_store()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddInfrastructure();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IEventCollector>());
        Assert.NotNull(provider.GetRequiredService<ISettingsStore>());
        Assert.NotNull(provider.GetRequiredService<ISecretProtector>());
        Assert.NotNull(provider.GetRequiredService<IEventClassifier>());
        Assert.NotNull(provider.GetRequiredService<IAlertFormatter>());
        Assert.NotNull(provider.GetRequiredService<INotificationChannel>());
        Assert.NotNull(provider.GetRequiredService<IAlertJournal>());
        Assert.NotNull(provider.GetRequiredService<IAlertOutbox>());
        Assert.NotNull(provider.GetRequiredService<INetworkMonitor>());
        Assert.NotNull(provider.GetRequiredService<IPowerEventMonitor>());
        Assert.NotNull(provider.GetRequiredService<ISessionMonitor>());
        Assert.NotNull(provider.GetRequiredService<IDiskSpaceMonitor>());
        Assert.NotNull(provider.GetRequiredService<IWindowsEventLogWriter>());
        Assert.NotNull(provider.GetRequiredService<IAlertMuteState>());
        Assert.NotNull(provider.GetRequiredService<IAutostartService>());
        Assert.NotNull(provider.GetRequiredService<IDiagnosticsService>());
        Assert.NotNull(provider.GetRequiredService<AlertOrchestrator>());
        Assert.NotNull(provider.GetRequiredService<IAlertMuteState>());
    }
}
