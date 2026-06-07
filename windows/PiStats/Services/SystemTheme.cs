using Microsoft.Win32;

namespace PiStats.Services;

/// <summary>Reads the Windows light/dark theme so the tray icon stays visible.</summary>
public static class SystemTheme
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    /// True when the taskbar/system tray uses the light theme (light background).
    public static bool IsLightTaskbar
    {
        get
        {
            try
            {
                using var k = Registry.CurrentUser.OpenSubKey(Key, false);
                // SystemUsesLightTheme drives the taskbar/tray color (1 = light).
                return (k?.GetValue("SystemUsesLightTheme") as int?) == 1;
            }
            catch { return false; }
        }
    }
}
