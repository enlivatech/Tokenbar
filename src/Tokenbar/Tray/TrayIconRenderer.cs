using Tokenbar.Interop;

namespace Tokenbar.Tray;

/// <summary>
/// Draws the CodexBar-style meter: a thick top capsule (primary / session) over a thin bottom
/// capsule (secondary / weekly), filled left-to-right by remaining percent. Geometry is defined on
/// CodexBar's 36px canvas and scaled to the tray icon size for the current DPI.
/// </summary>
internal static class TrayIconRenderer
{
    private const double Canvas = 36;
    private const double BarX = 3, BarW = 30;
    private const double StrokeW = 2;
    private const int Supersample = 4;

    private readonly record struct Bar(double Y, double H, double? Remaining, double Alpha = 1.0);

    public static nint CreateIcon(int size, double? primaryRemaining, double? secondaryRemaining, bool stale, bool lightTaskbar)
    {
        var bars = Layout(primaryRemaining, secondaryRemaining);
        var pixels = Render(size, bars, stale);
        byte shade = lightTaskbar ? (byte)0 : (byte)255;
        return ToHIcon(size, pixels, shade);
    }

    /// <summary>Dev aid: writes raw 8-bit alpha (size×size) for visual inspection.</summary>
    public static void DumpAlpha(string file, int size, double? primaryRemaining, double? secondaryRemaining, bool stale)
    {
        var alpha = Render(size, Layout(primaryRemaining, secondaryRemaining), stale);
        File.WriteAllBytes(file, alpha.Select(a => (byte)Math.Round(Math.Clamp(a, 0, 1) * 255)).ToArray());
    }

    private static Bar[] Layout(double? primary, double? secondary)
    {
        // y measured from the top of the canvas (CodexBar uses bottom-up AppKit coordinates).
        var top = new Bar(36 - 31, 12, primary);
        var bottom = new Bar(36 - 13, 8, secondary);
        var single = new Bar(36 - 30, 16, primary ?? secondary);

        if (primary is not null && secondary is not null) return [top, bottom];
        if (primary is null && secondary is null) return [top, bottom with { Alpha = 0.45 }];
        return [single];
    }

    private static float[] Render(int size, Bar[] bars, bool stale)
    {
        double trackFill = stale ? 0.18 : 0.28;
        double trackStroke = stale ? 0.28 : 0.44;
        double fillAlpha = stale ? 0.55 : 1.0;
        double scale = size / Canvas;
        var alpha = new float[size * size];
        int n = Supersample;

        for (int py = 0; py < size; py++)
        for (int px = 0; px < size; px++)
        {
            double acc = 0;
            for (int sy = 0; sy < n; sy++)
            for (int sx = 0; sx < n; sx++)
            {
                double x = (px + (sx + 0.5) / n) / scale;
                double y = (py + (sy + 0.5) / n) / scale;
                double transparent = 1;
                foreach (var bar in bars)
                {
                    double r = bar.H / 2;
                    if (!InRoundRect(x, y, BarX, bar.Y, BarW, bar.H, r)) continue;

                    transparent *= 1 - trackFill * bar.Alpha;

                    bool inner = InRoundRect(x, y, BarX + StrokeW, bar.Y + StrokeW, BarW - 2 * StrokeW, bar.H - 2 * StrokeW, Math.Max(0, r - StrokeW));
                    if (!inner) transparent *= 1 - trackStroke * bar.Alpha;

                    if (bar.Remaining is double rem && x < BarX + BarW * Math.Clamp(rem, 0, 100) / 100)
                        transparent *= 1 - fillAlpha * bar.Alpha;
                }
                acc += 1 - transparent;
            }
            alpha[py * size + px] = (float)(acc / (n * n));
        }
        return alpha;
    }

    private static bool InRoundRect(double x, double y, double rx, double ry, double w, double h, double r)
    {
        if (w <= 0 || h <= 0) return false;
        if (x < rx || x > rx + w || y < ry || y > ry + h) return false;
        double cx = Math.Clamp(x, rx + r, rx + w - r);
        double cy = Math.Clamp(y, ry + r, ry + h - r);
        double dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= r * r;
    }

    private static unsafe nint ToHIcon(int size, float[] alpha, byte shade)
    {
        var header = new Native.BITMAPV5HEADER
        {
            bV5Size = sizeof(Native.BITMAPV5HEADER),
            bV5Width = size,
            bV5Height = -size,
            bV5Planes = 1,
            bV5BitCount = 32,
            bV5Compression = 3, // BI_BITFIELDS
            bV5RedMask = 0x00FF0000,
            bV5GreenMask = 0x0000FF00,
            bV5BlueMask = 0x000000FF,
            bV5AlphaMask = 0xFF000000,
        };
        nint color = Native.CreateDIBSection(0, ref header, 0, out nint bits, 0, 0);
        var dst = (uint*)bits;
        for (int i = 0; i < alpha.Length; i++)
        {
            uint a = (uint)Math.Round(Math.Clamp(alpha[i], 0, 1) * 255);
            uint c = shade;
            dst[i] = (a << 24) | (c << 16) | (c << 8) | c;
        }

        nint mask = Native.CreateBitmap(size, size, 1, 1, 0);
        var info = new Native.ICONINFO { fIcon = 1, hbmColor = color, hbmMask = mask };
        nint icon = Native.CreateIconIndirect(ref info);
        Native.DeleteObject(color);
        Native.DeleteObject(mask);
        return icon;
    }
}
