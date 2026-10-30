using System.Diagnostics;

namespace ScheduleShutdown.App.Services;

/// <summary>Appends timestamped lines to <c>%LOCALAPPDATA%\ScheduleShutdown\app.log</c> and the debug output.</summary>
public sealed class AppLog
{
    private readonly object _lock = new();

    public AppLog(string? path = null)
    {
        Path = path ?? System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScheduleShutdown", "app.log");
    }

    public string Path { get; }

    public void Write(string message)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}  {message}";
        Debug.WriteLine(line);

        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                File.AppendAllText(Path, line + Environment.NewLine);
            }
        }
        catch (IOException)
        {
            // Logging must never break the app.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
