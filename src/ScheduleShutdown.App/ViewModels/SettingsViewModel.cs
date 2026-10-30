using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScheduleShutdown.App.Services;
using ScheduleShutdown.Core.Models;
using ScheduleShutdown.Core.Persistence;

namespace ScheduleShutdown.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly JsonSettingsStore _store;
    private readonly ThemeService _theme;
    private readonly AppLog _log;

    public SettingsViewModel(JsonSettingsStore store, ThemeService theme, AppEnvironment environment, AppLog log)
    {
        _store = store;
        _theme = theme;
        _log = log;

        ThemeIndex = (int)theme.Theme;
        IsDryRun = environment.IsDryRun;
        DataFolder = Path.GetDirectoryName(log.Path)!;

        var version = typeof(SettingsViewModel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        VersionText = $"Version {version.Split('+')[0]}";
    }

    /// <summary>Index into System / Light / Dark, matching <see cref="AppTheme"/>.</summary>
    [ObservableProperty]
    public partial int ThemeIndex { get; set; }

    public bool IsDryRun { get; }

    public string VersionText { get; }

    /// <summary>Holds app.log and the saved schedule.</summary>
    public string DataFolder { get; }

    [RelayCommand]
    private void OpenDataFolder()
    {
        try
        {
            Directory.CreateDirectory(DataFolder);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{DataFolder}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log.Write($"Couldn't open {DataFolder}: {ex.Message}");
        }
    }

    partial void OnThemeIndexChanged(int value)
    {
        var theme = Enum.IsDefined((AppTheme)value) ? (AppTheme)value : AppTheme.System;
        if (theme == _theme.Theme)
            return;

        _theme.SetTheme(theme);
        _store.Save(new AppSettings(theme));
    }
}
