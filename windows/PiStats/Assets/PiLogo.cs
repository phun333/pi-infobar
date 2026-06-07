using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PiStats.Assets;

/// <summary>
/// The Pi "P + i" block mark, ported from the macOS PiLogoShape (logo-auto.svg).
/// Provides the Geometry (for XAML Paths) and rendered icons (tray + bitmaps).
/// </summary>
public static class PiLogo
{
    /// Raw path in the original 800x800 SVG space, even-odd fill.
    /// Outer P + inner hole + the "i" dot. Bounding box: 165.29..634.72.
    private const string PathData =
        // Outer P boundary
        "M165.29,165.29 L517.36,165.29 L517.36,400 L400,400 L400,517.36 " +
        "L282.65,517.36 L282.65,634.72 L165.29,634.72 Z " +
        // Inner hole
        "M282.65,282.65 L282.65,400 L400,400 L400,282.65 Z " +
        // i dot
        "M517.36,400 L634.72,400 L634.72,634.72 L517.36,634.72 Z";

    /// A frozen, reusable Geometry. EvenOdd so the hole punches through.
    public static Geometry Geometry { get; } = BuildGeometry();

    /// Bounding box of the mark in SVG space (used to normalize when drawing).
    public static Rect Bounds { get; } = new(165.29, 165.29, 469.43, 469.43);

    private static Geometry BuildGeometry()
    {
        var g = Geometry.Parse(PathData);
        if (g is PathGeometry pg)
            pg.FillRule = FillRule.EvenOdd;
        g.Freeze();
        return g;
    }

    /// <summary>
    /// Render the mark to a System.Drawing.Icon for the system tray.
    /// (H.NotifyIcon only consumes Uri-backed ImageSources, so we hand it a
    /// real GDI icon instead.)
    /// </summary>
    public static System.Drawing.Icon RenderTrayIcon(int size, Color color)
    {
        var rtb = Render(size, color);
        using var ms = EncodePng(rtb);
        using var bitmap = new System.Drawing.Bitmap(ms);
        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    /// <summary>Render the mark to a frozen WPF bitmap of the given pixel size.</summary>
    public static BitmapSource RenderIcon(int size, Color color, double scale = 1.0)
    {
        var rtb = Render((int)(size * scale), color);
        using var ms = EncodePng(rtb);
        var img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = ms;
        img.EndInit();
        img.Freeze();
        return img;
    }

    private static RenderTargetBitmap Render(int px, Color color)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var b = Bounds;
            double s = px / b.Width;
            var transform = new TransformGroup();
            transform.Children.Add(new TranslateTransform(-b.X, -b.Y));
            transform.Children.Add(new ScaleTransform(s, s));

            var geo = Geometry.Clone();
            geo.Transform = transform;
            dc.DrawGeometry(new SolidColorBrush(color), null, geo);
        }

        var rtb = new RenderTargetBitmap(px, px, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze();
        return rtb;
    }

    /// <summary>
    /// Render the app icon: white π on a rounded gradient "squircle"
    /// (the Windows equivalent of the macOS AppIcon).
    /// </summary>
    public static byte[] RenderAppIconPng(int size)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            double inset = size * 0.07;
            var rect = new Rect(inset, inset, size - 2 * inset, size - 2 * inset);
            double radius = rect.Width * 0.225;

            var bg = new LinearGradientBrush(
                Color.FromRgb(0x6E, 0x9B, 0xFF), Color.FromRgb(0x3D, 0x6B, 0xEF),
                new Point(0, 0), new Point(1, 1));
            dc.DrawRoundedRectangle(bg, null, rect, radius, radius);

            // White π mark, centered at ~52% of the icon.
            double mark = size * 0.52;
            double offset = (size - mark) / 2.0;
            var b = Bounds;
            double s = mark / b.Width;
            var transform = new TransformGroup();
            transform.Children.Add(new TranslateTransform(-b.X, -b.Y));
            transform.Children.Add(new ScaleTransform(s, s));
            transform.Children.Add(new TranslateTransform(offset, offset));

            var geo = Geometry.Clone();
            geo.Transform = transform;
            dc.DrawGeometry(Brushes.White, null, geo);
        }

        var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);
        using var ms = EncodePng(rtb);
        return ms.ToArray();
    }

    /// <summary>Write a multi-resolution .ico (PNG-compressed entries, Vista+).</summary>
    public static void WriteIcoFile(string path, int[]? sizes = null)
    {
        sizes ??= new[] { 16, 24, 32, 48, 64, 128, 256 };
        var frames = sizes.Select(s => (s, png: RenderAppIconPng(s))).ToList();

        using var fs = File.Create(path);
        using var w = new BinaryWriter(fs);

        // ICONDIR
        w.Write((ushort)0);            // reserved
        w.Write((ushort)1);            // type = icon
        w.Write((ushort)frames.Count); // image count

        int offset = 6 + frames.Count * 16;
        foreach (var (s, png) in frames)
        {
            w.Write((byte)(s >= 256 ? 0 : s)); // width (0 = 256)
            w.Write((byte)(s >= 256 ? 0 : s)); // height
            w.Write((byte)0);                  // palette
            w.Write((byte)0);                  // reserved
            w.Write((ushort)1);                // color planes
            w.Write((ushort)32);               // bits per pixel
            w.Write((uint)png.Length);         // bytes in resource
            w.Write((uint)offset);             // image offset
            offset += png.Length;
        }
        foreach (var (_, png) in frames) w.Write(png);
    }

    private static MemoryStream EncodePng(BitmapSource source)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        var ms = new MemoryStream();
        encoder.Save(ms);
        ms.Position = 0;
        return ms;
    }
}
