namespace AlarmProgram.Application.Configuration;

public sealed class AlertJournalOptions
{
    public const string SectionName = "AlertJournal";

    public string FilePath { get; set; } = "%AppData%/AlarmProgram/alert-journal.json";

    public int MaxEntries { get; set; } = 100;
}
