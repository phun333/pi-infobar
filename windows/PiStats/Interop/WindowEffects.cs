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
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    // DWM_WINDOW_CORNER_PREFERENCE
    private const int DWMWCP_ROUND = 2;

    // DWM_SYSTEMBACKDROP_TYPE
    private const int DWMSBT_MAINWINDOW = 2;   // Mica
    private const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic (best match for a dropdown)

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// Force this window to the foreground so the tray overflow flyout (the
    /// "show hidden icons" panel) dismisses behind it instead of lingering.
    /// </summary>
    public static void BringToForeground(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd != IntPtr.Zero) SetForegroundWindow(hwnd);
    }

    /// <summary>Give a normal (titled) window a dark title bar.</summary>
    public static void EnableDarkTitleBar(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;
        int on = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int));
    }

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
