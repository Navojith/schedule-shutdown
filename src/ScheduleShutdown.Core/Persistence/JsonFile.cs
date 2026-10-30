using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScheduleShutdown.Core.Persistence;

/// <summary>
/// Small JSON files under the app's data folder. Writes go through a temp file so a crash never leaves a
/// half-written file. I/O errors are reported to <c>onError</c> and never thrown.
/// </summary>
internal static class JsonFile
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Returns the parsed file, or null if it's missing, unreadable or not valid JSON for <typeparamref name="T"/>.</summary>
    public static T? Read<T>(string path, Action<string>? onError)
        where T : class
    {
        string json;
        try
        {
            if (!File.Exists(path))
                return null;
            json = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            onError?.Invoke($"Couldn't read {path}: {ex.Message}");
            return null;
        }

        try
        {
            var value = JsonSerializer.Deserialize<T>(json, SerializerOptions);
            if (value is null)
                onError?.Invoke($"Ignored {path}: empty.");
            return value;
        }
        catch (JsonException ex)
        {
            onError?.Invoke($"Ignored {path}: {ex.Message}");
            return null;
        }
    }

    public static void Write<T>(string path, T value, Action<string>? onError)
    {
        var temp = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(temp, JsonSerializer.Serialize(value, SerializerOptions));
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            onError?.Invoke($"Couldn't save {path}: {ex.Message}");
        }
    }

    public static void Delete(string path, Action<string>? onError)
    {
        try
        {
            File.Delete(path);
        }
        catch (DirectoryNotFoundException)
        {
            // Nothing was ever saved.
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            onError?.Invoke($"Couldn't delete {path}: {ex.Message}");
        }
    }
}
