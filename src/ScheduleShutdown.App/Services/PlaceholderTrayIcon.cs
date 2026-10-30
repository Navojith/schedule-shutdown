using System.Drawing;
using System.Drawing.Drawing2D;

namespace ScheduleShutdown.App.Services;

/// <summary>
/// A power symbol on an accent-coloured circle, drawn at startup. Stands in until the real app icon
/// lands in Phase 6. Drawn as shapes because the Segoe Fluent Icons glyph didn't render in-process.
/// </summary>
public static class PlaceholderTrayIcon
{
    private const int Size = 64;

    public static Icon Create(Windows.UI.Color accent)
    {
        using var bitmap = new Bitmap(Size, Size);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var background = new SolidBrush(Color.FromArgb(accent.A, accent.R, accent.G, accent.B));
            graphics.FillEllipse(background, 0, 0, Size - 1, Size - 1);

            // An arc open at the top plus a vertical stroke.
            using var pen = new Pen(Color.White, Size * 0.09f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            const float inset = Size * 0.27f;
            graphics.DrawArc(pen, inset, inset, Size - 2 * inset, Size - 2 * inset, -55, 290);
            graphics.DrawLine(pen, Size / 2f, Size * 0.2f, Size / 2f, Size * 0.48f);
        }

        // Icon.FromHandle doesn't own the HICON; the app keeps this one icon for its lifetime anyway.
        return Icon.FromHandle(bitmap.GetHicon());
    }
}
