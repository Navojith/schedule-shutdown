using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace ScheduleShutdown.App.Services;

public static class WindowFocus
{
    /// <summary>
    /// Shows, restores and activates the window. From the tray, Explorer keeps the foreground and
    /// <see cref="Window.Activate"/> alone leaves the window behind others, so this also asks for the
    /// foreground and, if Windows refuses, raises it by briefly making it topmost.
    /// </summary>
    public static void BringToFront(Window window)
    {
        window.AppWindow.Show();
        if (window.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            if (presenter.State == OverlappedPresenterState.Minimized)
                presenter.Restore();

            window.Activate();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            if (!SetForegroundWindow(hwnd) && !presenter.IsAlwaysOnTop)
            {
                presenter.IsAlwaysOnTop = true;
                presenter.IsAlwaysOnTop = false;
            }
        }
        else
        {
            window.Activate();
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hwnd);
}
