using Microsoft.Win32;

namespace PiStats.Services;

/// <summary>
/// Start-with-Windows via the HKCU Run key (the Windows analogue of the
/// macOS SMAppService launch-at-login).
/// </summary>
public static class LaunchAtLogin
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "PiStats";

    private static string? ExePath => Environment.ProcessPath;

    public static bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
                var val = key?.GetValue(ValueName) as string;
                return !string.IsNullOrEmpty(val);
            }
            catch { return false; }
        }
    }

    public static void Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true)
                          ?? Registry.CurrentUser.CreateSubKey(RunKey);
            if (key == null) return;

            if (enabled)
            {
                var exe = ExePath;
                if (!string.IsNullOrEmpty(exe))
                    key.SetValue(ValueName, $"\"{exe}\"");
            }
            else
            {
                if (key.GetValue(ValueName) != null)
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch { /* best effort */ }
    }
}
