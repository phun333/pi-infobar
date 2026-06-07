using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using PiStats.Models;

namespace PiStats.Services;

/// <summary>
/// Persistent user settings (the Windows equivalent of UserDefaults +
/// the Swift SettingsStore). Backed by %APPDATA%\PiStats\settings.json.
/// </summary>
public sealed class SettingsStore : INotifyPropertyChanged
{
    public static SettingsStore Shared { get; } = Load();

    private readonly string _path;
    private Model _m;

    private sealed class Model
    {
        public bool ShowTrayIcon { get; set; } = true;
        public string MenuBarMetric { get; set; } = Services.MenuBarMetric.TodayCost.ToString();
        public string DefaultRange { get; set; } = "All";
        public string DefaultTab { get; set; } = "Overview";

        // Remote (wired up in Step 7)
        public bool RemoteSyncEnabled { get; set; }
        public string RemoteHost { get; set; } = "";
        public string RemotePort { get; set; } = "22";
        public string RemoteUser { get; set; } = "";
        public string RemoteKeyPath { get; set; } = "";
        public string RemotePath { get; set; } = "~/.pi/agent/sessions";
    }

    private SettingsStore(string path, Model m) { _path = path; _m = m; }

    private static SettingsStore Load()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PiStats");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "settings.json");

        Model m;
        try { m = JsonSerializer.Deserialize<Model>(File.ReadAllText(path)) ?? new Model(); }
        catch { m = new Model(); }
        return new SettingsStore(path, m);
    }

    private void Save([CallerMemberName] string? changed = null)
    {
        try { File.WriteAllText(_path, JsonSerializer.Serialize(_m, new JsonSerializerOptions { WriteIndented = true })); }
        catch { /* best effort */ }
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(changed));
    }

    // MARK: - Typed accessors

    public bool ShowTrayIcon
    {
        get => _m.ShowTrayIcon;
        set { if (_m.ShowTrayIcon != value) { _m.ShowTrayIcon = value; Save(); } }
    }

    public MenuBarMetric MenuBarMetric
    {
        get => Enum.TryParse<MenuBarMetric>(_m.MenuBarMetric, out var v) ? v : MenuBarMetric.TodayCost;
        set { if (MenuBarMetric != value) { _m.MenuBarMetric = value.ToString(); Save(); } }
    }

    public TimeRange DefaultRange
    {
        get => TimeRangeExtensions.FromRaw(_m.DefaultRange);
        set { if (DefaultRange != value) { _m.DefaultRange = value.Label(); Save(); } }
    }

    public string DefaultTab
    {
        get => _m.DefaultTab;
        set { if (_m.DefaultTab != value) { _m.DefaultTab = value; Save(); } }
    }

    // Remote (Step 7)
    public bool RemoteSyncEnabled { get => _m.RemoteSyncEnabled; set { if (_m.RemoteSyncEnabled != value) { _m.RemoteSyncEnabled = value; Save(); } } }
    public string RemoteHost { get => _m.RemoteHost; set { if (_m.RemoteHost != value) { _m.RemoteHost = value; Save(); } } }
    public string RemotePort { get => _m.RemotePort; set { if (_m.RemotePort != value) { _m.RemotePort = value; Save(); } } }
    public string RemoteUser { get => _m.RemoteUser; set { if (_m.RemoteUser != value) { _m.RemoteUser = value; Save(); } } }
    public string RemoteKeyPath { get => _m.RemoteKeyPath; set { if (_m.RemoteKeyPath != value) { _m.RemoteKeyPath = value; Save(); } } }
    public string RemotePath { get => _m.RemotePath; set { if (_m.RemotePath != value) { _m.RemotePath = value; Save(); } } }

    public event PropertyChangedEventHandler? PropertyChanged;
}
