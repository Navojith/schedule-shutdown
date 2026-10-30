using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.App.Services;

/// <summary>
/// Applies the chosen theme to every open window: the XAML content (Mica follows it) and the caption
/// buttons. Windows register themselves with <see cref="Attach"/>. UI thread only.
/// </summary>
public sealed class ThemeService
{
    private readonly List<Window> _windows = [];

    public AppTheme Theme { get; private set; }

    public void SetTheme(AppTheme theme)
    {
        Theme = theme;
        foreach (var window in _windows)
            Apply(window);
    }

    /// <summary>Also sets the window's icon, so every window gets both from one call.</summary>
    public void Attach(Window window)
    {
        _windows.Add(window);
        window.Closed += (_, _) => _windows.Remove(window);
        window.AppWindow.SetIcon(AppAssets.IconPath);
        Apply(window);
    }

    private void Apply(Window window)
    {
        if (window.Content is FrameworkElement root)
        {
            root.RequestedTheme = Theme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }

        window.AppWindow.TitleBar.PreferredTheme = Theme switch
        {
            AppTheme.Light => TitleBarTheme.Light,
            AppTheme.Dark => TitleBarTheme.Dark,
            _ => TitleBarTheme.UseDefaultAppMode,
        };
    }
}
