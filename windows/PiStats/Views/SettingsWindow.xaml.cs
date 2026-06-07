using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using PiStats.Core;
using PiStats.Interop;
using PiStats.Models;
using PiStats.Services;

namespace PiStats.Views;

public partial class SettingsWindow : Window
{
    private static SettingsWindow? _instance;
    private readonly SettingsStore _s = SettingsStore.Shared;

    private static SolidColorBrush White => Brushes.White;
    private static SolidColorBrush Secondary => new(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF));
    private static SolidColorBrush Tertiary => new(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF));

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
        SourceInitialized += (_, _) => WindowEffects.EnableDarkTitleBar(this);
        Sidebar.SelectedIndex = 0;
    }

    public void SelectPane(int i) { Sidebar.SelectedIndex = i; UpdateLayout(); }

    public void RenderPng(string path)
    {
        double w = Width, h = Height - 30;
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
        Add(PaneTitle("Menu Bar"));
        Add(SectionLabel("APPEARANCE"));
        Add(Group(
            ToggleRow("Show tray icon",
                "On Windows the tray always needs an icon; kept for parity.",
                _s.ShowTrayIcon, v => _s.ShowTrayIcon = v),
            ComboRow("Show in tooltip", "What the hover tooltip reports.",
                MenuBarMetricExtensions.AllCases.Select(m => (m.Label(), (object)m)).ToList(),
                _s.MenuBarMetric, v => _s.MenuBarMetric = (MenuBarMetric)v)
        ));
    }

    private void BuildGeneralPane()
    {
        Add(PaneTitle("General"));
        Add(SectionLabel("STARTUP"));
        Add(Group(
            ToggleRow("Launch at login",
                "Start Pi Stats automatically when you sign in to Windows.",
                LaunchAtLogin.IsEnabled, v => LaunchAtLogin.Set(v))
        ));

        Add(SectionLabel("DEFAULT VIEW", topMargin: 20));
        Add(Group(
            ComboRow("Open on tab", null,
                new[] { "Overview", "Languages", "Models", "Projects", "Usage" }
                    .Select(t => (t, (object)t)).ToList(),
                _s.DefaultTab, v => _s.DefaultTab = (string)v),
            ComboRow("Time range", null,
                TimeRangeExtensions.AllCases.Select(r => (r.Label(), (object)r)).ToList(),
                _s.DefaultRange, v => _s.DefaultRange = (TimeRange)v)
        ));
    }

    private void BuildRemotePane()
    {
        Add(PaneTitle("Remote"));
        Add(SectionLabel("REMOTE SERVER"));
        Add(Group(
            ToggleRow("Connect to remote server",
                "Pull *.jsonl logs from a remote machine over SSH.",
                _s.RemoteSyncEnabled, v => _s.RemoteSyncEnabled = v)
        ));

        Add(SectionLabel("SSH CONNECTION", topMargin: 20));
        Add(Group(
            TextFieldRow("Host", _s.RemoteHost, v => _s.RemoteHost = v, "e.g. 192.168.1.100"),
            TextFieldRow("Port", _s.RemotePort, v => _s.RemotePort = v, "22"),
            TextFieldRow("Username", _s.RemoteUser, v => _s.RemoteUser = v, "e.g. ubuntu"),
            TextFieldRow("SSH key path", _s.RemoteKeyPath, v => _s.RemoteKeyPath = v, @"C:\Users\you\.ssh\id_rsa"),
            TextFieldRow("Remote path", _s.RemotePath, v => _s.RemotePath = v, "~/.pi/agent/sessions")
        ));

        Add(Note("Full sync / connection test is enabled in a later build step; values here are saved."));
    }

    private void BuildAboutPane()
    {
        Add(PaneTitle("About"));

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.2.0";

        // Header card with logo
        var logoRow = new Grid { Margin = new Thickness(14, 12, 14, 12) };
        logoRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        logoRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var logo = new System.Windows.Shapes.Path
        {
            Data = Assets.PiLogo.Geometry, Fill = White, Stretch = Stretch.Uniform,
            Width = 40, Height = 40, VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(logo, 0);
        logoRow.Children.Add(logo);
        var info = new StackPanel { Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(Text("Pi Stats", 16, FontWeights.Bold, White));
        info.Children.Add(Text($"Version {version.Split('+')[0]}", 12, FontWeights.Normal, Secondary, top: 2));
        info.Children.Add(Text("Local usage dashboard for the Pi agent.", 11.5, FontWeights.Normal, Secondary, top: 2));
        Grid.SetColumn(info, 1);
        logoRow.Children.Add(info);
        Add(Card(logoRow));

        Add(SectionLabel("DATA", topMargin: 20));
        bool remote = _s.RemoteSyncEnabled && _s.RemoteHost.Length > 0;
        Add(Group(
            KeyValueRow("Source", remote ? $"Remote: {_s.RemoteHost}" : StatsEngine.SessionsDir),
            KeyValueRow("Privacy", remote ? "Syncs over SSH from your remote host"
                                          : "100% local — nothing leaves your PC")
        ));
    }

    // MARK: - Builders

    private void Add(UIElement e) => PaneHost.Children.Add(e);

    private static TextBlock Text(string t, double size, FontWeight weight, Brush brush, double top = 0)
        => new() { Text = t, FontSize = size, FontWeight = weight, Foreground = brush,
                   TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, top, 0, 0) };

    private static UIElement PaneTitle(string t)
        => new TextBlock { Text = t, FontSize = 20, FontWeight = FontWeights.Bold,
                           Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 16) };

    private static UIElement SectionLabel(string t, double topMargin = 0)
        => new TextBlock { Text = t, FontSize = 10.5, FontWeight = FontWeights.SemiBold,
                           Foreground = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)),
                           Margin = new Thickness(2, topMargin, 0, 7) };

    private static UIElement Note(string t)
        => new TextBlock { Text = t, FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)),
                           TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 10, 0, 0) };

    private static Border Card(UIElement child) => new()
    {
        CornerRadius = new CornerRadius(10),
        Background = new SolidColorBrush(Color.FromArgb(0x12, 0xFF, 0xFF, 0xFF)),
        BorderBrush = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF)),
        BorderThickness = new Thickness(1),
        Child = child
    };

    private static Border Group(params UIElement[] rows)
    {
        var sp = new StackPanel();
        for (int i = 0; i < rows.Length; i++)
        {
            if (i > 0)
                sp.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromArgb(0x12, 0xFF, 0xFF, 0xFF)),
                    Margin = new Thickness(14, 0, 0, 0)
                });
            sp.Children.Add(rows[i]);
        }
        return Card(sp);
    }

    private static UIElement Row(string title, string? subtitle, UIElement control)
    {
        var grid = new Grid { Margin = new Thickness(14, 11, 14, 11) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        texts.Children.Add(Text(title, 13, FontWeights.Normal, White));
        if (subtitle != null)
            texts.Children.Add(Text(subtitle, 11, FontWeights.Normal, Secondary, top: 2));
        Grid.SetColumn(texts, 0);
        grid.Children.Add(texts);

        if (control is FrameworkElement fe)
        {
            fe.VerticalAlignment = VerticalAlignment.Center;
            fe.Margin = new Thickness(12, 0, 0, 0);
        }
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
        return grid;
    }

    private UIElement ToggleRow(string title, string? subtitle, bool value, Action<bool> onChange)
    {
        var tb = new ToggleButton
        {
            IsChecked = value,
            Style = (Style)FindResource("ToggleSwitch")
        };
        tb.Checked += (_, _) => onChange(true);
        tb.Unchecked += (_, _) => onChange(false);
        return Row(title, subtitle, tb);
    }

    private UIElement ComboRow(string title, string? subtitle,
        List<(string label, object value)> items, object current, Action<object> onChange)
    {
        var combo = new ComboBox
        {
            Style = (Style)FindResource("DarkCombo"),
            Width = 160
        };
        foreach (var (label, _) in items) combo.Items.Add(label);
        combo.SelectedIndex = Math.Max(0, items.FindIndex(i => Equals(i.value, current)));
        combo.SelectionChanged += (_, _) =>
        {
            int idx = combo.SelectedIndex;
            if (idx >= 0 && idx < items.Count) onChange(items[idx].value);
        };
        return Row(title, subtitle, combo);
    }

    private UIElement TextFieldRow(string title, string value, Action<string> onChange, string placeholder)
    {
        var panel = new StackPanel { Margin = new Thickness(14, 10, 14, 10) };
        panel.Children.Add(Text(title, 12, FontWeights.Normal, Secondary));
        var tb = new TextBox
        {
            Text = value,
            Style = (Style)FindResource("DarkText"),
            Margin = new Thickness(0, 5, 0, 0)
        };
        tb.LostFocus += (_, _) => onChange(tb.Text);
        panel.Children.Add(tb);
        return panel;
    }

    private static UIElement KeyValueRow(string key, string value)
    {
        var grid = new Grid { Margin = new Thickness(14, 10, 14, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var k = Text(key, 12.5, FontWeights.SemiBold, White);
        Grid.SetColumn(k, 0);
        grid.Children.Add(k);
        var v = Text(value, 12, FontWeights.Normal, Secondary);
        v.TextAlignment = TextAlignment.Right;
        Grid.SetColumn(v, 1);
        grid.Children.Add(v);
        return grid;
    }
}
