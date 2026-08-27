namespace AlarmProgram.Application.Configuration;

public sealed class AlertOutboxOptions
{
    public const string SectionName = "AlertOutbox";

    public string FilePath { get; set; } = "%AppData%/AlarmProgram/alert-outbox.json";

    public int MaxItems { get; set; } = 200;
}
