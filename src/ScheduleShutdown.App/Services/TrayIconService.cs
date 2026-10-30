using System.Runtime.InteropServices;
using System.Windows.Input;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace ScheduleShutdown.App.Services;

/// <summary>Notification-area icon with a countdown tooltip and an Open / Cancel shutdown / Exit menu.</summary>
public sealed class TrayIconService : IDisposable
{
    private TaskbarIcon? _icon;
    private string _toolTip = "";

    public void Create(string toolTip, ICommand open, ICommand cancel, ICommand exit)
    {
        var menu = new MenuFlyout();
        menu.Items.Add(new MenuFlyoutItem { Text = "Open", Command = open });
        menu.Items.Add(new MenuFlyoutItem { Text = "Cancel shutdown", Command = cancel });
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(new MenuFlyoutItem { Text = "Exit", Command = exit });

        var accent = Application.Current.Resources.TryGetValue("SystemAccentColor", out var value) && value is Color color
            ? color
            : Colors.SteelBlue;

        // Windows ties the icon GUID to the exe path, so derive it from the path.
        var id = TrayIcon.CreateUniqueGuidForProcessPath();
        RemoveStaleIcon(id);

        _toolTip = toolTip;
        _icon = new TaskbarIcon
        {
            Id = id,
            ToolTipText = toolTip,
            Icon = PlaceholderTrayIcon.Create(accent),
            ContextMenuMode = ContextMenuMode.PopupMenu,
            ContextFlyout = menu,
            LeftClickCommand = open,
            NoLeftClickDelay = true,
        };

        // Efficiency Mode would throttle the countdown timer, so keep it off.
        _icon.ForceCreate(false);
    }

    public void SetToolTip(string toolTip)
    {
        if (_icon is null || toolTip == _toolTip)
            return;

        _toolTip = toolTip;
        _icon.ToolTipText = toolTip;
    }

    public void Dispose()
    {
        _icon?.Dispose();
        _icon = null;
    }

    /// <summary>
    /// A killed or crashed instance leaves its icon registered under the same GUID until the user hovers
    /// it, and that blocks adding ours. Deleting by GUID clears it; failure just means there was none.
    /// </summary>
    private static void RemoveStaleIcon(Guid id)
    {
        var data = new NotifyIconData
        {
            Size = Marshal.SizeOf<NotifyIconData>(),
            Flags = NifGuid,
            GuidItem = id,
        };
        Shell_NotifyIconW(NimDelete, ref data);
    }

    private const uint NimDelete = 0x2;
    private const uint NifGuid = 0x20;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int Size;
        public nint Window;
        public uint Id;
        public uint Flags;
        public uint CallbackMessage;
        public nint Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string? Tip;
        public uint State;
        public uint StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string? Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string? InfoTitle;
        public uint InfoFlags;
        public Guid GuidItem;
        public nint BalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIconW(uint message, ref NotifyIconData data);
}
