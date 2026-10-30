using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.Core.Persistence;

/// <summary>Persists <see cref="AppSettings"/>. A missing or bad file gives the defaults; unknown values fall back one by one.</summary>
public sealed class JsonSettingsStore(string path, Action<string>? onError = null)
{
    public string Path { get; } = path;

    public AppSettings Load()
    {
        var file = JsonFile.Read<SettingsFile>(Path, onError);
        if (file is null)
            return AppSettings.Default;

        var theme = file.Theme is { } value && Enum.IsDefined(value) ? value : AppSettings.Default.Theme;
        return new AppSettings(theme);
    }

    public void Save(AppSettings settings) => JsonFile.Write(Path, new SettingsFile(settings.Theme), onError);

    private sealed record SettingsFile(AppTheme? Theme);
}
