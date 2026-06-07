using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using PiStats.Core;
using PiStats.Models;

namespace PiStats.Controls;

/// <summary>
/// Imperative WPF builders that reproduce the macOS SwiftUI components
/// (StatCard, BarRow, SectionHeader, token rows, donut + daily-spend charts).
/// </summary>
public static class UiBuilder
{
    public static readonly Color Accent = Color.FromRgb(0x4D, 0x7C, 0xFF);

    // White at a given alpha (matches SwiftUI .primary/.secondary opacities).
    public static SolidColorBrush W(byte a) => new(Color.FromArgb(a, 0xFF, 0xFF, 0xFF));
    public static SolidColorBrush Solid(Color c) => new(c);

    // MARK: - Section header

    public static UIElement SectionHeader(string title, string? trailing = null)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var t = new TextBlock
        {
            Text = title, Foreground = Brushes.White,
            FontSize = 13, FontWeight = FontWeights.Bold
        };
        Grid.SetColumn(t, 0);
        grid.Children.Add(t);

        if (trailing != null)
        {
            var tr = new TextBlock
            {
                Text = trailing, Foreground = W(0x99),
                FontSize = 11, FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(tr, 2);
            grid.Children.Add(tr);
        }
        return grid;
    }

    // MARK: - Stat card

    public static UIElement Card(string title, string value, Color accent)
    {
        var panel = new StackPanel();

        var head = new StackPanel { Orientation = Orientation.Horizontal };
        head.Children.Add(new Ellipse
        {
            Width = 7, Height = 7, Fill = Solid(accent),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        });
        head.Children.Add(new TextBlock
        {
            Text = title, Foreground = W(0x99),
            FontSize = 11, FontWeight = FontWeights.Medium,
            VerticalAlignment = VerticalAlignment.Center
        });
        panel.Children.Add(head);

        panel.Children.Add(new TextBlock
        {
            Text = value, Foreground = Brushes.White,
            FontSize = 18, FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 5, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        return new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = W(0x10),
            BorderBrush = W(0x12),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(11, 9, 11, 9),
            Margin = new Thickness(4),
            Child = panel
        };
    }

    // MARK: - Bar row (languages / models / projects / tools)

    public static UIElement BarRow(Color color, string glyph, string title,
                                   string subtitle, string value, double fraction)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Glyph chip
        var chip = new Border
        {
            Width = 28, Height = 28,
            CornerRadius = new CornerRadius(7),
            Background = Solid(Color.FromArgb(0x29, color.R, color.G, color.B)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            Child = new TextBlock
            {
                Text = glyph, Foreground = Solid(color),
                FontSize = glyph.Length > 1 ? 10 : 13, FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(chip, 0);
        grid.Children.Add(chip);

        // Right column: title+value, bar, subtitle
        var right = new StackPanel();
        Grid.SetColumn(right, 1);

        var titleRow = new Grid();
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var tt = new TextBlock
        {
            Text = title, Foreground = Brushes.White,
            FontSize = 12.5, FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(tt, 0);
        titleRow.Children.Add(tt);
        var vv = new TextBlock
        {
            Text = value, Foreground = Brushes.White,
            FontSize = 12, FontWeight = FontWeights.Bold,
            Margin = new Thickness(6, 0, 0, 0)
        };
        Grid.SetColumn(vv, 1);
        titleRow.Children.Add(vv);
        right.Children.Add(titleRow);

        // Progress track
        right.Children.Add(ProgressTrack(color, fraction, 5, new Thickness(0, 4, 0, 0)));

        if (!string.IsNullOrEmpty(subtitle))
        {
            right.Children.Add(new TextBlock
            {
                Text = subtitle, Foreground = W(0x99),
                FontSize = 10.5, Margin = new Thickness(0, 3, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }

        grid.Children.Add(right);
        return grid;
    }

    // MARK: - Token row (Usage tab)

    public static UIElement TokenRow(string name, int value, int total, Color color)
    {
        var panel = new StackPanel();

        var top = new Grid();
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var dot = new Ellipse { Width = 7, Height = 7, Fill = Solid(color), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(dot, 0);
        top.Children.Add(dot);

        var nm = new TextBlock
        {
            Text = name, Foreground = Brushes.White, FontSize = 11.5,
            FontWeight = FontWeights.Medium, Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(nm, 1);
        top.Children.Add(nm);

        var val = new TextBlock
        {
            Text = Fmt.Tokens(value), Foreground = Brushes.White,
            FontSize = 11.5, FontWeight = FontWeights.Bold
        };
        Grid.SetColumn(val, 2);
        top.Children.Add(val);

        panel.Children.Add(top);
        double frac = total > 0 ? (double)value / total : 0;
        panel.Children.Add(ProgressTrack(color, frac, 5, new Thickness(0, 4, 0, 0)));
        return panel;
    }

    // MARK: - Progress track (rounded background + proportional fill)

    public static UIElement ProgressTrack(Color color, double fraction, double height, Thickness margin)
    {
        fraction = Math.Clamp(fraction, 0, 1);
        var track = new Grid { Height = height, Margin = margin };

        track.Children.Add(new Border
        {
            Background = W(0x10),
            CornerRadius = new CornerRadius(height / 2)
        });

        var fillGrid = new Grid();
        fillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(0.0001, fraction), GridUnitType.Star) });
        fillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(0.0001, 1 - fraction), GridUnitType.Star) });
        var fill = new Border
        {
            Background = Solid(color),
            CornerRadius = new CornerRadius(height / 2),
            MinWidth = 4
        };
        Grid.SetColumn(fill, 0);
        fillGrid.Children.Add(fill);
        track.Children.Add(fillGrid);

        return track;
    }

    // MARK: - Centered empty/message state

    public static UIElement CenteredMessage(string title, string? subtitle = null)
    {
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(30)
        };
        panel.Children.Add(new TextBlock
        {
            Text = title, Foreground = Brushes.White,
            FontSize = 13, FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        if (subtitle != null)
        {
            panel.Children.Add(new TextBlock
            {
                Text = subtitle, Foreground = W(0x99), FontSize = 11,
                TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0), MaxWidth = 260
            });
        }
        return new Border { Height = 220, Child = panel };
    }

    // MARK: - Daily spend chart (bars + per-bar hover tooltip)

    public static UIElement DailySpendChart(IReadOnlyList<DaySpend> data, double height = 150)
    {
        var container = new Grid { Height = height };
        if (data.Count == 0) return container;

        double maxCost = Math.Max(data.Max(d => d.Cost), 0.0001);

        var bars = new UniformGrid { Rows = 1, Columns = data.Count, VerticalAlignment = VerticalAlignment.Stretch };
        var grad = new LinearGradientBrush(
            Color.FromRgb(0x6F, 0xCF, 0x73), Color.FromRgb(0x4C, 0xAF, 0x50), 90);

        foreach (var d in data)
        {
            double frac = d.Cost / maxCost;

            // Full-height transparent cell so hovering anywhere in the column
            // (not just the thin bar) shows that day's spend.
            var cell = new Grid
            {
                Margin = new Thickness(0.6, 0, 0.6, 0),
                Background = Brushes.Transparent,
                ToolTip = ChartTooltip(d)
            };
            ToolTipService.SetInitialShowDelay(cell, 80);
            ToolTipService.SetBetweenShowDelay(cell, 0);
            ToolTipService.SetPlacement(cell, System.Windows.Controls.Primitives.PlacementMode.Top);

            var bar = new Border
            {
                Background = grad,
                CornerRadius = new CornerRadius(2, 2, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                Height = Math.Max(d.Cost > 0 ? 2 : 0, frac * (height - 4)),
                IsHitTestVisible = false
            };
            cell.Children.Add(bar);
            bars.Children.Add(cell);
        }

        container.Children.Add(bars);
        return container;
    }

    /// Dark, styled tooltip showing a day's date + spend (matches the panel).
    private static ToolTip ChartTooltip(DaySpend d)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = d.Day.ToString("ddd, MMM d"),
            Foreground = W(0xAA), FontSize = 10, FontWeight = FontWeights.Medium
        });
        stack.Children.Add(new TextBlock
        {
            Text = Fmt.Money(d.Cost),
            Foreground = Solid(Color.FromRgb(0x6F, 0xCF, 0x73)),
            FontSize = 13, FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 1, 0, 0)
        });

        return new ToolTip
        {
            Background = Solid(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            BorderBrush = W(0x22),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(9, 6, 9, 6),
            HasDropShadow = true,
            Content = stack
        };
    }

    // MARK: - Donut chart (language share)

    public static UIElement Donut(IReadOnlyList<LangStat> langs, int totalLines, double size = 130)
    {
        var host = new Grid { Width = size, Height = size };
        if (langs.Count == 0 || totalLines <= 0) return host;

        double outer = size / 2;
        double inner = outer * 0.62;
        var center = new Point(outer, outer);

        double angle = -90; // start at top
        int sum = langs.Sum(l => l.Lines);
        if (sum <= 0) return host;

        foreach (var l in langs)
        {
            double sweep = 360.0 * l.Lines / sum;
            if (sweep <= 0) continue;
            host.Children.Add(DonutSegment(center, outer - 1.5, inner, angle, sweep, l.Color));
            angle += sweep;
        }

        // Center label
        var label = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.Children.Add(new TextBlock
        {
            Text = Fmt.Tokens(totalLines), Foreground = Brushes.White,
            FontSize = 16, FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        label.Children.Add(new TextBlock
        {
            Text = "lines", Foreground = W(0x99), FontSize = 9,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        host.Children.Add(label);

        return host;
    }

    private static Path DonutSegment(Point c, double rOuter, double rInner,
                                     double startDeg, double sweepDeg, Color color)
    {
        Point P(double r, double deg)
        {
            double rad = deg * Math.PI / 180.0;
            return new Point(c.X + r * Math.Cos(rad), c.Y + r * Math.Sin(rad));
        }

        double endDeg = startDeg + sweepDeg;
        bool large = sweepDeg > 180;

        var p0 = P(rOuter, startDeg);
        var p1 = P(rOuter, endDeg);
        var p2 = P(rInner, endDeg);
        var p3 = P(rInner, startDeg);

        var fig = new PathFigure { StartPoint = p0, IsClosed = true };
        fig.Segments.Add(new ArcSegment(p1, new Size(rOuter, rOuter), 0, large, SweepDirection.Clockwise, true));
        fig.Segments.Add(new LineSegment(p2, true));
        fig.Segments.Add(new ArcSegment(p3, new Size(rInner, rInner), 0, large, SweepDirection.Counterclockwise, true));

        var geo = new PathGeometry();
        geo.Figures.Add(fig);

        return new Path { Data = geo, Fill = Solid(color) };
    }
}
