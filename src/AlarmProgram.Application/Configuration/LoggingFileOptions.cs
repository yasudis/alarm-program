namespace AlarmProgram.Application.Configuration;

public sealed class LoggingFileOptions
{
    public const string SectionName = "Logging:File";

    public string Path { get; set; } = "%AppData%/AlarmProgram/logs/alarm-.log";

    public int RetainedFileCountLimit { get; set; } = 14;
}
