using AlarmProgram.Application.Configuration;
using AlarmProgram.Domain;
using AlarmProgram.Infrastructure.Journal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AlarmProgram.Tests.Unit.Journal;

public class FileAlertJournalTests
{
    [Fact]
    public async Task AppendAsync_then_GetRecentAsync_returns_newest_first()
    {
        var path = Path.Combine(Path.GetTempPath(), "AlarmProgramTests", Guid.NewGuid().ToString("N"), "journal.json");
        var journal = CreateJournal(path);

        await journal.AppendAsync(new AlertJournalEntry
        {
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5),
            EventType = MachineEventType.Startup,
            Subject = "first",
            Status = "Sent"
        });
        await journal.AppendAsync(new AlertJournalEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventType = MachineEventType.Heartbeat,
            Subject = "second",
            Status = "Sent",
            CorrelationId = "abc"
        });

        var recent = await journal.GetRecentAsync(10);

        Assert.Equal(2, recent.Count);
        Assert.Equal("second", recent[0].Subject);
        Assert.Equal("first", recent[1].Subject);
        Assert.Equal("abc", recent[0].CorrelationId);
    }

    private static FileAlertJournal CreateJournal(string path) =>
        new(
            Options.Create(new AlertJournalOptions { FilePath = path, MaxEntries = 20 }),
            NullLogger<FileAlertJournal>.Instance);
}
