using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PiStats.Controls;
using PiStats.Core;
using PiStats.Interop;
using PiStats.Models;

namespace PiStats.Views;

public partial class PopoverWindow : Window
{
    private static readonly string[] Tabs = { "Overview", "Languages", "Models", "Projects", "Usage" };
    private static readonly SolidColorBrush AccentBrush = new(UiBuilder.Accent);
    private static readonly SolidColorBrush Secondary = new(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF));

    private readonly StatsEngine _engine;
    private TimeRange _range = Services.SettingsStore.Shared.DefaultRange;
    private string _tab = Services.SettingsStore.Shared.DefaultTab;

    /// Set by App to open the Settings window.
    public Action? OnSettings { get; set; }

    private readonly List<(Border chip, TimeRange range, TextBlock text)> _rangeChips = new();
    private readonly List<(Border tab, string name, TextBlock text, Border underline)> _tabItems = new();

    public PopoverWindow(StatsEngine engine)
    {
        InitializeComponent();
        _engine = engine;
        _engine.PropertyChanged += OnEngineChanged;

        BuildRangePicker();
        BuildTabBar();

        Deactivated += (_, _) => HidePopover();
        SourceInitialized += (_, _) => WindowEffects.ApplyAcrylic(this);
        Loaded += (_, _) => RefreshUi();
    }

    // MARK: - Range picker

    private void BuildRangePicker()
    {
        foreach (var r in TimeRangeExtensions.AllCases)
        {
            var text = new TextBlock
            {
                Text = r.Label(), FontSize = 10.5, FontWeight = FontWeights.SemiBold,
                Foreground = Secondary,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var chip = new Border
            {
                CornerRadius = new CornerRadius(5),
                Background = Brushes.Transparent,
                Padding = new Thickness(7, 3, 7, 3),
                Cursor = Cursors.Hand,
                Child = text
            };
            var captured = r;
            chip.MouseLeftButtonUp += (_, _) => { _range = captured; UpdateRangeStyles(); RebuildBody(); };
            _rangeChips.Add((chip, r, text));
            RangePanel.Children.Add(chip);
        }
        UpdateRangeStyles();
    }

    private void UpdateRangeStyles()
    {
        foreach (var (chip, range, text) in _rangeChips)
        {
            bool sel = range == _range;
            chip.Background = sel ? AccentBrush : Brushes.Transparent;
            text.Foreground = sel ? Brushes.White : Secondary;
        }
    }

    // MARK: - Tab bar

    private void BuildTabBar()
    {
        const double tabWidth = 72;
        foreach (var name in Tabs)
        {
            var text = new TextBlock
            {
                Text = name, FontSize = 11.5, Foreground = Secondary,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var underline = new Border
            {
                Height = 2, Background = Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            var inner = new Grid();
            inner.Children.Add(text);
            inner.Children.Add(underline);

            var tab = new Border
            {
                Width = tabWidth, Padding = new Thickness(0, 8, 0, 8),
                Background = Brushes.Transparent, Cursor = Cursors.Hand,
                Child = inner
            };
            var captured = name;
            tab.MouseLeftButtonUp += (_, _) => { _tab = captured; UpdateTabStyles(); RebuildBody(); };
            _tabItems.Add((tab, name, text, underline));
            TabPanel.Children.Add(tab);
        }
        UpdateTabStyles();
    }

    private void UpdateTabStyles()
    {
        foreach (var (_, name, text, underline) in _tabItems)
        {
            bool sel = name == _tab;
            text.Foreground = sel ? AccentBrush : Secondary;
            text.FontWeight = sel ? FontWeights.Bold : FontWeights.Medium;
            underline.Background = sel ? AccentBrush : Brushes.Transparent;
        }
    }

    // MARK: - Engine updates

    private void OnEngineChanged(object? sender, PropertyChangedEventArgs e)
        => Dispatcher.Invoke(RefreshUi);

    private void RefreshUi()
    {
        bool firstLoad = _engine.Loading && _engine.LastUpdated == null;
        LoadingPanel.Visibility = firstLoad ? Visibility.Visible : Visibility.Collapsed;
        BodyScroll.Visibility = firstLoad ? Visibility.Collapsed : Visibility.Visible;

        LoadProgress.Value = _engine.Progress;
        LoadingText.Text = $"Reading Pi sessions… {(int)(_engine.Progress * 100)}%";

        SubtitleText.Text = _engine.Loading
            ? "Updating…"
            : _engine.LastUpdated is { } d ? $"Updated {d:t}" : "";

        if (!firstLoad) RebuildBody();
    }

    private void RebuildBody()
    {
        if (_engine.Loading && _engine.LastUpdated == null) return;
        var summary = _engine.Summary(_range);
        BodyHost.Children.Clear();
        BodyHost.Children.Add(TabBuilder.Build(_tab, summary));
    }

    // MARK: - Show / hide

    public void ShowPopover()
    {
        var wa = SystemParameters.WorkArea;
        const double gap = 8;
        Left = wa.Right - Width - gap;
        Top = wa.Bottom - Height - gap;

        Show();
        Activate();
        Topmost = true;
        RefreshUi();
    }

    public void HidePopover() => Hide();

    // MARK: - Test capture (used by `--shot` to verify rendering)

    public void SelectTab(string name)
    {
        _tab = name;
        UpdateTabStyles();
        RebuildBody();
        UpdateLayout();
    }

    public void RenderPng(string path)
    {
        UpdateLayout();
        var root = (UIElement)Content;
        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
            (int)Width, (int)Height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(root);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
        using var fs = System.IO.File.Create(path);
        encoder.Save(fs);
    }

    // MARK: - Actions

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        => await _engine.LoadAsync(force: true);

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        HidePopover();
        OnSettings?.Invoke();
    }

    private void QuitButton_Click(object sender, RoutedEventArgs e)
        => Application.Current.Shutdown();
}
