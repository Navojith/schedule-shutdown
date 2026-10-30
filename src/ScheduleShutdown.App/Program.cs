using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace ScheduleShutdown.App;

/// <summary>
/// Custom entry point (<c>DISABLE_XAML_GENERATED_MAIN</c>): a second launch hands its activation to the
/// running instance and exits before any UI starts, so two timers can never compete.
/// </summary>
public static class Program
{
    private const string InstanceKey = "ScheduleShutdown";

    /// <summary>Activations redirected from later launches. Raised on a background thread.</summary>
    public static event EventHandler<AppActivationArguments>? Redirected;

    [STAThread]
    private static int Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        var instance = AppInstance.FindOrRegisterForKey(InstanceKey);
        if (!instance.IsCurrent)
        {
            Redirect(instance);
            return 0;
        }

        instance.Activated += (_, args) => Redirected?.Invoke(null, args);

        Application.Start(_ =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            new App();
        });
        return 0;
    }

    private static void Redirect(AppInstance target)
    {
        // This process was just launched by the user, so it may hand the foreground to the running one.
        AllowSetForegroundWindow(target.ProcessId);

        var args = AppInstance.GetCurrent().GetActivatedEventArgs();
        var done = CreateEvent(0, bManualReset: true, bInitialState: false, null);
        try
        {
            // The redirect must not block the STA thread, so it runs elsewhere while this thread pumps COM.
            Task.Run(() =>
            {
                try
                {
                    target.RedirectActivationToAsync(args).AsTask().Wait();
                }
                finally
                {
                    SetEvent(done);
                }
            });
            _ = CoWaitForMultipleObjects(0, uint.MaxValue, 1, [done], out _);
        }
        finally
        {
            CloseHandle(done);
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CreateEvent(nint lpEventAttributes, bool bManualReset, bool bInitialState, string? lpName);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetEvent(nint hEvent);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint hObject);

    [DllImport("ole32.dll")]
    private static extern uint CoWaitForMultipleObjects(uint dwFlags, uint dwTimeout, uint cHandles, nint[] pHandles, out uint lpdwIndex);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(uint dwProcessId);
}
