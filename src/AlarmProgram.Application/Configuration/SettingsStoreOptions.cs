namespace AlarmProgram.Application.Configuration;

public sealed class SettingsStoreOptions
{
    public const string SectionName = "SettingsStore";

    public string FilePath { get; set; } = "%AppData%/AlarmProgram/settings.json";
}
