using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PiStats.Core;
using PiStats.Models;
using PiStats.Services;

namespace PiStats.Views;

public partial class SettingsWindow : Window
{
    private static SettingsWindow? _instance;
    private readonly SettingsStore _s = SettingsStore.Shared;

    public static void Open(Window? owner = null)
    {
        if (_instance == null)
        {
            _instance = new SettingsWindow();
            _instance.Closed += (_, _) => _instance = null;
        }
        if (owner != null) _instance.Owner = owner;
        ((Window)_instance).Show();
        _instance.Activate();
    }

    public SettingsWindow()
    {
        InitializeComponent();
        Sidebar.SelectedIndex = 0;
    }

    public void SelectPane(int i) { Sidebar.SelectedIndex = i; UpdateLayout(); }

    public void RenderPng(string path)
    {
        double w = Width, h = Height - 30; // approx client area (minus title bar)
        var root = (FrameworkElement)Content;
        root.Measure(new Size(w, h));
        root.Arrange(new Rect(0, 0, w, h));
        root.UpdateLayout();
        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
            (int)w, (int)h, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(root);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
        using var fs = System.IO.File.Create(path);
        encoder.Save(fs);
    }

    private void Sidebar_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PaneHost.Children.Clear();
        switch (Sidebar.SelectedIndex)
        {
            case 0: BuildMenuBarPane(); break;
            case 1: BuildGeneralPane(); break;
            case 2: BuildRemotePane(); break;
            case 3: BuildAboutPane(); break;
        }
    }

    // MARK: - Panes

    private void BuildMenuBarPane()
    {
        Add(Header("Menu Bar"));

        Add(Toggle("Show tray icon",
            "On Windows the tray always needs an icon; this is kept for parity.",
            _s.ShowTrayIcon, v => _s.ShowTrayIcon = v));

        Add(Combo("Show in tooltip", "Pick what the hover tooltip reports.",
            MenuBarMetricExtensions.AllCases.Select(m => (m.Label(), (object)m)).ToList(),
            _s.MenuBarMetric,
            v => _s.MenuBarMetric = (MenuBarMetric)v));
    }

    private void BuildGeneralPane()
    {
        Add(Header("Startup"));
        Add(Toggle("Launch at login",
            "Start Pi Stats automatically when you sign in to Windows.",
            LaunchAtLogin.IsEnabled,
            v => { LaunchAtLogin.Set(v); }));

        Add(Header("Default View", topMargin: 22));
        Add(Combo("Open on tab", null,
            new[] { "Overview", "Languages", "Models", "Projects", "Usage" }
                .Select(t => (t, (object)t)).ToList(),
            _s.DefaultTab, v => _s.DefaultTab = (string)v));

        Add(Combo("Time range", null,
            TimeRangeExtensions.AllCases.Select(r => (r.Label(), (object)r)).ToList(),
            _s.DefaultRange, v => _s.DefaultRange = (TimeRange)v));
    }

    private void BuildRemotePane()
    {
        Add(Header("Remote Server"));
        Add(Info("Sync session logs from a remote host over SSH. Full sync/test is enabled in a later build step; settings here are saved."));

        Add(Toggle("Connect to remote server",
            "Pull *.jsonl logs from a remote machine instead of this PC.",
            _s.RemoteSyncEnabled, v => _s.RemoteSyncEnabled = v));

        Add(TextRow("Host", _s.RemoteHost, v => _s.RemoteHost = v, "e.g. 192.168.1.100"));
        Add(TextRow("Port", _s.RemotePort, v => _s.RemotePort = v, "22"));
        Add(TextRow("Username", _s.RemoteUser, v => _s.RemoteUser = v, "e.g. ubuntu"));
        Add(TextRow("SSH key path", _s.RemoteKeyPath, v => _s.RemoteKeyPath = v, @"C:\Users\you\.ssh\id_rsa"));
        Add(TextRow("Remote path", _s.RemotePath, v => _s.RemotePath = v, "~/.pi/agent/sessions"));
    }

    private void BuildAboutPane()
    {
        Add(Header("About"));

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.2.0";

        Add(KeyValue("Pi Stats", $"Version {version.Split('+')[0]}"));
        Add(KeyValue("Description", "Local usage dashboard for the Pi agent."));
        Add(KeyValue("Data source",
            _s.RemoteSyncEnabled && _s.RemoteHost.Length > 0
                ? $"Remote: {_s.RemoteHost}"
                : StatsEngine.SessionsDir));
        Add(KeyValue("Privacy",
            _s.RemoteSyncEnabled && _s.RemoteHost.Length > 0
                ? "Syncs over SSH from your remote host"
                : "100% local — nothing leaves your PC"));
    }

    // MARK: - Control builders

    private void Add(UIElement e) => PaneHost.Children.Add(e);

    private static UIElement Header(string text, double topMargin = 0) => new TextBlock
    {
        Text = text, FontSize = 16, FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, topMargin, 0, 10)
    };

    private static UIElement Info(string text) => new TextBlock
    {
        Text = text, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 0, 0, 12)
    };

    private static UIElement Toggle(string title, string? subtitle, bool value, Action<bool> onChange)
    {
        var cb = new CheckBox { IsChecked = value, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 2, 10, 0) };
        cb.Checked += (_, _) => onChange(true);
        cb.Unchecked += (_, _) => onChange(false);

        var texts = new StackPanel();
        texts.Children.Add(new TextBlock { Text = title, FontSize = 13 });
        if (subtitle != null)
            texts.Children.Add(new TextBlock { Text = subtitle, Foreground = Brushes.Gray, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });

        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
        row.Children.Add(cb);
        row.Children.Add(texts);
        return row;
    }

    private static UIElement Combo(string title, string? subtitle,
        List<(string label, object value)> items, object current, Action<object> onChange)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 13 });
        if (subtitle != null)
            panel.Children.Add(new TextBlock { Text = subtitle, Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(0, 2, 0, 0) });

        var combo = new ComboBox { Margin = new Thickness(0, 6, 0, 0), Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var (label, value) in items)
            combo.Items.Add(new ComboBoxItem { Content = label, Tag = value });
        combo.SelectedIndex = items.FindIndex(i => Equals(i.value, current));
        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedItem is ComboBoxItem ci && ci.Tag != null) onChange(ci.Tag);
        };
        panel.Children.Add(combo);
        return panel;
    }

    private static UIElement TextRow(string title, string value, Action<string> onChange, string placeholder)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 13 });
        var tb = new TextBox { Text = value, Margin = new Thickness(0, 4, 0, 0), Width = 300, HorizontalAlignment = HorizontalAlignment.Left };
        tb.LostFocus += (_, _) => onChange(tb.Text);
        panel.Children.Add(tb);
        return panel;
    }

    private static UIElement KeyValue(string key, string value)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        panel.Children.Add(new TextBlock { Text = key, FontWeight = FontWeights.SemiBold, FontSize = 13 });
        panel.Children.Add(new TextBlock { Text = value, Foreground = Brushes.Gray, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
        return panel;
    }
}
