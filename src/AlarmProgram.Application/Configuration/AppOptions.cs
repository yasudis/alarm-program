namespace AlarmProgram.Application.Configuration;

public sealed class AppOptions
{
    public const string SectionName = "App";

    public string ApplicationName { get; set; } = "Alarm Program";

    public string Environment { get; set; } = "Production";
}
