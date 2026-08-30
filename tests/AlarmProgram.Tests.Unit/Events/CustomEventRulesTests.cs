using AlarmProgram.Application.Contracts;
using AlarmProgram.Application.Events;
using AlarmProgram.Domain;

namespace AlarmProgram.Tests.Unit.Events;

public class CustomEventRulesTests
{
    [Fact]
    public void TryClassify_maps_configured_event_id()
    {
        var settings = new UserSettings { CustomEventIds = "7045, 7040" };
        var raw = new RawSystemEvent
        {
            OccurredAt = DateTimeOffset.UtcNow,
            EventId = 7045,
            Source = "Service Control Manager",
            Message = "A service was installed",
            HostName = "TEST-PC"
        };

        var result = CustomEventRules.TryClassify(raw, settings);

        Assert.NotNull(result);
        Assert.Equal(MachineEventType.CustomEvent, result.Type);
        Assert.Equal(7045, result.EventId);
        Assert.Equal("Service Control Manager", result.Source);
    }

    [Fact]
    public void TryClassify_ignores_ids_not_in_list()
    {
        var settings = new UserSettings { CustomEventIds = "7045" };
        var raw = new RawSystemEvent
        {
            OccurredAt = DateTimeOffset.UtcNow,
            EventId = 1,
            Source = "System",
            HostName = "TEST-PC"
        };

        Assert.Null(CustomEventRules.TryClassify(raw, settings));
    }
}
