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
