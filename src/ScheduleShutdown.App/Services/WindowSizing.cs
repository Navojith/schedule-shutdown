using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace ScheduleShutdown.App.Services;

public static class WindowSizing
{
    /// <summary>Resizes to a size in DIPs: <see cref="AppWindow.Resize"/> takes physical pixels.</summary>
    public static void Resize(Window window, int width, int height)
    {
        var scale = Scale(window);
        window.AppWindow.Resize(new SizeInt32((int)Math.Round(width * scale), (int)Math.Round(height * scale)));
    }

    /// <summary>Stops the user from resizing below a size in DIPs.</summary>
    public static void SetMinimumSize(Window window, int width, int height)
    {
        if (window.AppWindow.Presenter is not OverlappedPresenter presenter)
            return;

        var scale = Scale(window);
        presenter.PreferredMinimumWidth = (int)Math.Round(width * scale);
        presenter.PreferredMinimumHeight = (int)Math.Round(height * scale);
    }

    /// <summary>Centres the window in the work area of the display it's on.</summary>
    public static void Center(Window window)
    {
        var area = DisplayArea.GetFromWindowId(window.AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var size = window.AppWindow.Size;
        window.AppWindow.Move(new PointInt32(area.X + (area.Width - size.Width) / 2, area.Y + (area.Height - size.Height) / 2));
    }

    private static double Scale(Window window)
    {
        var dpi = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(window));
        return dpi > 0 ? dpi / 96.0 : 1.0;
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);
}
