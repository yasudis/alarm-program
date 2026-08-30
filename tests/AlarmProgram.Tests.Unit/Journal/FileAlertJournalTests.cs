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

    [Fact]
    public async Task ExportCsvAsync_writes_header_and_rows()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AlarmProgramTests", Guid.NewGuid().ToString("N"));
        var journalPath = Path.Combine(dir, "journal.json");
        var csvPath = Path.Combine(dir, "export.csv");
        var journal = CreateJournal(journalPath);

        await journal.AppendAsync(new AlertJournalEntry
        {
            Timestamp = DateTimeOffset.Parse("2026-08-27T12:00:00Z"),
            EventType = MachineEventType.IpChanged,
            Subject = "Сменился IP-адрес",
            Status = "Sent",
            Channel = "Telegram",
            HostName = "HOME-PC",
            CorrelationId = "cid1",
            Details = "note,with,comma"
        });

        await journal.ExportCsvAsync(csvPath);
        var csv = await File.ReadAllTextAsync(csvPath);

        Assert.Contains("Timestamp,EventType,Status,Channel,Subject,HostName,CorrelationId,Details", csv);
        Assert.Contains("IpChanged", csv);
        Assert.Contains("Telegram", csv);
        Assert.Contains("\"note,with,comma\"", csv);
    }

    [Fact]
    public async Task ClearAsync_removes_all_entries()
    {
        var path = Path.Combine(Path.GetTempPath(), "AlarmProgramTests", Guid.NewGuid().ToString("N"), "journal.json");
        var journal = CreateJournal(path);

        await journal.AppendAsync(new AlertJournalEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventType = MachineEventType.Startup,
            Subject = "keep-me-not",
            Status = "Sent"
        });

        await journal.ClearAsync();
        var recent = await journal.GetRecentAsync(10);

        Assert.Empty(recent);
    }

    [Fact]
    public async Task PurgeOlderThanAsync_removes_only_old_entries()
    {
        var path = Path.Combine(Path.GetTempPath(), "AlarmProgramTests", Guid.NewGuid().ToString("N"), "journal.json");
        var journal = CreateJournal(path);

        await journal.AppendAsync(new AlertJournalEntry
        {
            Timestamp = DateTimeOffset.UtcNow.AddDays(-10),
            EventType = MachineEventType.Shutdown,
            Subject = "old",
            Status = "Sent"
        });
        await journal.AppendAsync(new AlertJournalEntry
        {
            Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
            EventType = MachineEventType.Startup,
            Subject = "fresh",
            Status = "Sent"
        });

        var removed = await journal.PurgeOlderThanAsync(TimeSpan.FromDays(7));
        var recent = await journal.GetRecentAsync(10);

        Assert.Equal(1, removed);
        Assert.Single(recent);
        Assert.Equal("fresh", recent[0].Subject);
    }

    private static FileAlertJournal CreateJournal(string path) =>
        new(
            Options.Create(new AlertJournalOptions { FilePath = path, MaxEntries = 20 }),
            NullLogger<FileAlertJournal>.Instance);
}
