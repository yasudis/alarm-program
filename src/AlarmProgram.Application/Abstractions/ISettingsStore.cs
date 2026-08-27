using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface ISettingsStore
{
    Task<UserSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(UserSettings settings, CancellationToken cancellationToken = default);

    Task ExportPlainAsync(string filePath, CancellationToken cancellationToken = default);

    Task ImportPlainAsync(string filePath, CancellationToken cancellationToken = default);
}
