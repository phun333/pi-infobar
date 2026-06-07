using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PiStats.Core;
using PiStats.Models;
using PiStats.Interop;

namespace PiStats.Views;

public partial class PopoverWindow : Window
{
    private readonly StatsEngine _engine;
    private TimeRange _range = TimeRange.All;

    public PopoverWindow(StatsEngine engine)
    {
        InitializeComponent();
        _engine = engine;
        _engine.PropertyChanged += OnEngineChanged;

        Deactivated += (_, _) => HidePopover();
        SourceInitialized += (_, _) => WindowEffects.ApplyAcrylic(this);
        Loaded += (_, _) => RefreshUi();
    }

    private void OnEngineChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Marshal back to the UI thread (engine loads on a background task).
        Dispatcher.Invoke(RefreshUi);
    }

    private void RefreshUi()
    {
        bool firstLoad = _engine.Loading && _engine.LastUpdated == null;
        LoadingPanel.Visibility = firstLoad ? Visibility.Visible : Visibility.Collapsed;
        ContentPanel.Visibility = firstLoad ? Visibility.Collapsed : Visibility.Visible;

        LoadProgress.Value = _engine.Progress;
        LoadingText.Text = $"Reading Pi sessions… {(int)(_engine.Progress * 100)}%";

        SubtitleText.Text = _engine.Loading
            ? "Updating…"
            : _engine.LastUpdated is { } d
                ? $"Updated {d:t}"
                : "";

        if (!firstLoad) BuildCards();
    }

    private void BuildCards()
    {
        var s = _engine.Summary(_range);
        CardsGrid.Children.Clear();

        CardsGrid.Children.Add(Card("Total", Fmt.Money(s.TotalCost), Color.FromRgb(0x4C, 0xAF, 0x50)));
        CardsGrid.Children.Add(Card("Sessions", Fmt.Int(s.SessionCount), Color.FromRgb(0x4D, 0x7C, 0xFF)));
        CardsGrid.Children.Add(Card("Messages", Fmt.Int(s.TotalMessages), Color.FromRgb(0xAF, 0x7C, 0xFF)));
        CardsGrid.Children.Add(Card("Active Days", Fmt.Int(s.DaysActive), Color.FromRgb(0xFF, 0x9F, 0x40)));
        CardsGrid.Children.Add(Card("Avg/Day", Fmt.Money(s.AvgCostPerDay), Color.FromRgb(0x40, 0xC4, 0xC4)));
        CardsGrid.Children.Add(Card("Today", Fmt.Money(s.TodayCost), Color.FromRgb(0xFF, 0x5A, 0x5A)));
    }

    private static UIElement Card(string title, string value, Color accent)
    {
        var panel = new StackPanel { Margin = new Thickness(4) };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)),
            FontSize = 11,
            FontWeight = FontWeights.Medium
        });
        panel.Children.Add(new TextBlock
        {
            Text = value,
            Foreground = Brushes.White,
            FontSize = 19,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 4, 0, 0)
        });

        return new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x12, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(4),
            Child = panel
        };
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

    // MARK: - Actions

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        => await _engine.LoadAsync(force: true);

    private void QuitButton_Click(object sender, RoutedEventArgs e)
        => Application.Current.Shutdown();
}
