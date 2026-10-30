using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace ScheduleShutdown.App.Services;

public static class WindowSizing
{
    /// <summary>
    /// Resizes to a size in DIPs (<see cref="AppWindow.Resize"/> takes physical pixels), shrunk to fit
    /// the work area so small screens (1024 × 768 at 125 %) still show the whole window.
    /// </summary>
    public static void Resize(Window window, int width, int height)
    {
        var area = WorkArea(window);
        var scale = Scale(window);
        window.AppWindow.Resize(new SizeInt32(
            Math.Min((int)Math.Round(width * scale), area.Width),
            Math.Min((int)Math.Round(height * scale), area.Height)));
    }

    /// <summary>Stops the user from resizing below a size in DIPs, capped at the work area.</summary>
    public static void SetMinimumSize(Window window, int width, int height)
    {
        if (window.AppWindow.Presenter is not OverlappedPresenter presenter)
            return;

        var area = WorkArea(window);
        var scale = Scale(window);
        presenter.PreferredMinimumWidth = Math.Min((int)Math.Round(width * scale), area.Width);
        presenter.PreferredMinimumHeight = Math.Min((int)Math.Round(height * scale), area.Height);
    }

    /// <summary>Centres the window in the work area of the display it's on.</summary>
    public static void Center(Window window)
    {
        var area = WorkArea(window);
        var size = window.AppWindow.Size;
        window.AppWindow.Move(new PointInt32(area.X + (area.Width - size.Width) / 2, area.Y + (area.Height - size.Height) / 2));
    }

    /// <summary>Moves the window just enough to put it fully inside the work area of its display.</summary>
    public static void KeepInWorkArea(Window window)
    {
        var area = WorkArea(window);
        var position = window.AppWindow.Position;
        var size = window.AppWindow.Size;
        var x = Math.Max(area.X, Math.Min(position.X, area.X + area.Width - size.Width));
        var y = Math.Max(area.Y, Math.Min(position.Y, area.Y + area.Height - size.Height));
        if (x != position.X || y != position.Y)
            window.AppWindow.Move(new PointInt32(x, y));
    }

    private static RectInt32 WorkArea(Window window) =>
        DisplayArea.GetFromWindowId(window.AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;

    private static double Scale(Window window)
    {
        var dpi = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(window));
        return dpi > 0 ? dpi / 96.0 : 1.0;
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);
}
