using System.Text.Json;
using System.Text.Json.Serialization;
using ScheduleShutdown.Core.Abstractions;
using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.Core.Persistence;

/// <summary>
/// Persists the active schedule as a small JSON file. Writes go through a temp file so a crash never
/// leaves a half-written state. I/O errors are reported to <paramref name="onError"/> and never thrown:
/// losing persistence must not stop the running schedule.
/// </summary>
public sealed class JsonStateStore(string path, Action<string>? onError = null) : IStateStore
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string Path { get; } = path;

    public ScheduleState? Load()
    {
        string json;
        try
        {
            if (!File.Exists(Path))
                return null;
            json = File.ReadAllText(Path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            onError?.Invoke($"Couldn't read {Path}: {ex.Message}");
            return null;
        }

        try
        {
            var file = JsonSerializer.Deserialize<StateFile>(json, SerializerOptions);
            if (file is not { Version: CurrentVersion, Target: { } target, Mode: { } mode } || !Enum.IsDefined(mode))
            {
                onError?.Invoke($"Ignored {Path}: unsupported or incomplete state.");
                return null;
            }

            return new ScheduleState(target, mode, new ShutdownOptions(file.Force));
        }
        catch (JsonException ex)
        {
            onError?.Invoke($"Ignored {Path}: {ex.Message}");
            return null;
        }
    }

    public void Save(ScheduleState state)
    {
        var file = new StateFile(CurrentVersion, state.Target, state.Mode, state.Options.Force);
        var temp = Path + ".tmp";
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path))!);
            File.WriteAllText(temp, JsonSerializer.Serialize(file, SerializerOptions));
            File.Move(temp, Path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            onError?.Invoke($"Couldn't save {Path}: {ex.Message}");
        }
    }

    public void Clear()
    {
        try
        {
            File.Delete(Path);
        }
        catch (DirectoryNotFoundException)
        {
            // Nothing was ever saved.
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            onError?.Invoke($"Couldn't delete {Path}: {ex.Message}");
        }
    }

    private sealed record StateFile(int Version, DateTimeOffset? Target, ScheduleMode? Mode, bool Force);
}
