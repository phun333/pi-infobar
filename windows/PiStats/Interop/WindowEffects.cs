using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PiStats.Interop;

/// <summary>
/// Win32/DWM helpers to give a WPF window the translucent, rounded look that
/// the macOS app gets from NSVisualEffectView (material .menu + corner radius).
/// </summary>
public static class WindowEffects
{
    // DwmSetWindowAttribute attributes (dwmapi.h)
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    // DWM_WINDOW_CORNER_PREFERENCE
    private const int DWMWCP_ROUND = 2;

    // DWM_SYSTEMBACKDROP_TYPE
    private const int DWMSBT_MAINWINDOW = 2;   // Mica
    private const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic (best match for a dropdown)

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    /// <summary>Apply rounded corners + acrylic backdrop. No-ops gracefully on older Windows.</summary>
    public static void ApplyAcrylic(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        int corner = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));

        int backdrop = DWMSBT_TRANSIENTWINDOW;
        DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
    }
}
