using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using PiStats.Core;
using PiStats.Models;
using static PiStats.Controls.UiBuilder;

namespace PiStats.Controls;

/// <summary>Builds the body of each popover tab from a StatsSummary.</summary>
public static class TabBuilder
{
    private static readonly Color[] ProjectPalette =
    {
        Color.FromRgb(0x4D,0x7C,0xFF), Color.FromRgb(0xAF,0x7C,0xFF), Color.FromRgb(0xFF,0x6F,0xB0),
        Color.FromRgb(0xFF,0x9F,0x40), Color.FromRgb(0x40,0xC4,0xC4), Color.FromRgb(0x7C,0x6F,0xFF),
        Color.FromRgb(0x4C,0xAF,0x50), Color.FromRgb(0xFF,0x5A,0x5A), Color.FromRgb(0x4F,0xC3,0xF7),
        Color.FromRgb(0x66,0xD9,0xAE)
    };

    public static UIElement Build(string tab, StatsSummary s) => tab switch
    {
        "Overview" => Overview(s),
        "Languages" => Languages(s),
        "Models" => Models(s),
        "Projects" => Projects(s),
        "Usage" => Usage(s),
        _ => Overview(s)
    };

    private static StackPanel Stack(double spacing = 14)
        => new() { Margin = new Thickness(0) };

    private static void AddSpaced(StackPanel host, UIElement child, double top)
    {
        if (child is FrameworkElement fe) fe.Margin = new Thickness(
            fe.Margin.Left, top, fe.Margin.Right, fe.Margin.Bottom);
        host.Children.Add(child);
    }

    // MARK: - Overview

    private static UIElement Overview(StatsSummary s)
    {
        var host = Stack();

        var cards = new UniformGrid { Columns = 3 };
        cards.Children.Add(Card("Total", Fmt.Money(s.TotalCost), Color.FromRgb(0x4C, 0xAF, 0x50)));
        cards.Children.Add(Card("Sessions", Fmt.Int(s.SessionCount), Color.FromRgb(0x4D, 0x7C, 0xFF)));
        cards.Children.Add(Card("Messages", Fmt.Int(s.TotalMessages), Color.FromRgb(0xAF, 0x7C, 0xFF)));
        cards.Children.Add(Card("Active Days", Fmt.Int(s.DaysActive), Color.FromRgb(0xFF, 0x9F, 0x40)));
        cards.Children.Add(Card("Avg/Day", Fmt.Money(s.AvgCostPerDay), Color.FromRgb(0x40, 0xC4, 0xC4)));
        cards.Children.Add(Card("Today", Fmt.Money(s.TodayCost), Color.FromRgb(0xFF, 0x5A, 0x5A)));
        AddSpaced(host, cards, 0);

        if (s.DailySpend.Count > 0)
        {
            AddSpaced(host, SectionHeader("Daily Spend", $"{s.DailySpend.Count} days"), 16);
            AddSpaced(host, DailySpendChart(s.DailySpend, 150), 8);
        }

        if (s.Languages.Count > 0)
        {
            var top = s.Languages[0];
            AddSpaced(host, SectionHeader("Top Language"), 16);
            AddSpaced(host, BarRow(top.Color, top.Symbol, top.Name,
                $"{Fmt.Int(top.Edits)} edits", $"{Fmt.Int(top.Lines)} ln", 1.0), 8);
        }

        return host;
    }

    // MARK: - Languages

    private static UIElement Languages(StatsSummary s)
    {
        if (s.Languages.Count == 0)
            return CenteredMessage("No code edits yet",
                "Languages appear once you edit or write files in Pi.");

        int totalLines = Math.Max(s.Languages.Sum(l => l.Lines), 1);
        int maxLines = Math.Max(s.Languages.Max(l => l.Lines), 1);
        var host = Stack();

        // Donut + legend
        var top = new StackPanel { Orientation = Orientation.Horizontal };
        top.Children.Add(Donut(s.Languages.Take(8).ToList(), totalLines, 130));

        var legend = new StackPanel { Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        foreach (var l in s.Languages.Take(6))
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var dot = new System.Windows.Shapes.Ellipse { Width = 7, Height = 7, Fill = Solid(l.Color), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(dot, 0); row.Children.Add(dot);

            var nm = new TextBlock { Text = l.Name, Foreground = Brushes.White, FontSize = 10.5, FontWeight = FontWeights.Medium, Margin = new Thickness(6, 0, 0, 0) };
            Grid.SetColumn(nm, 1); row.Children.Add(nm);

            var pct = new TextBlock { Text = $"{100.0 * l.Lines / totalLines:F0}%", Foreground = W(0x99), FontSize = 10, FontWeight = FontWeights.SemiBold };
            Grid.SetColumn(pct, 2); row.Children.Add(pct);

            legend.Children.Add(row);
        }
        top.Children.Add(legend);
        AddSpaced(host, top, 0);

        AddSpaced(host, SectionHeader("Languages", "by lines written"), 12);

        var bars = new StackPanel();
        foreach (var l in s.Languages.Take(12))
        {
            AddSpaced(bars, BarRow(l.Color, l.Symbol, l.Name,
                $"{Fmt.Int(l.Edits)} edits · {100.0 * l.Lines / totalLines:F0}%",
                $"{Fmt.Int(l.Lines)} ln", (double)l.Lines / maxLines),
                bars.Children.Count == 0 ? 0 : 12);
        }
        AddSpaced(host, bars, 8);
        return host;
    }

    // MARK: - Models

    private static UIElement Models(StatsSummary s)
    {
        if (s.Models.Count == 0) return CenteredMessage("No model usage");

        double maxCost = Math.Max(s.Models.Max(m => m.Cost), 0.0001);
        var host = Stack();
        AddSpaced(host, SectionHeader("Models", "by cost"), 0);

        var bars = new StackPanel();
        foreach (var m in s.Models)
        {
            AddSpaced(bars, BarRow(m.Color, "AI", m.DisplayName,
                $"{Fmt.Int(m.Count)} calls", Fmt.Money(m.Cost), m.Cost / maxCost),
                bars.Children.Count == 0 ? 0 : 12);
        }
        AddSpaced(host, bars, 12);
        return host;
    }

    // MARK: - Projects

    private static UIElement Projects(StatsSummary s)
    {
        if (s.Projects.Count == 0) return CenteredMessage("No projects");

        double maxCost = Math.Max(s.Projects.Max(p => p.Cost), 0.0001);
        var host = Stack();
        AddSpaced(host, SectionHeader("Projects", "by cost"), 0);

        var bars = new StackPanel();
        int i = 0;
        foreach (var p in s.Projects.Take(15))
        {
            var color = ProjectPalette[i % ProjectPalette.Length];
            var glyph = string.IsNullOrEmpty(p.Name) ? "?" : p.Name[..1].ToUpperInvariant();
            AddSpaced(bars, BarRow(color, glyph, p.Name,
                $"{p.Sessions} session{(p.Sessions == 1 ? "" : "s")}", Fmt.Money(p.Cost), p.Cost / maxCost),
                bars.Children.Count == 0 ? 0 : 12);
            i++;
        }
        AddSpaced(host, bars, 12);
        return host;
    }

    // MARK: - Usage

    private static UIElement Usage(StatsSummary s)
    {
        var host = Stack();
        AddSpaced(host, SectionHeader("Tokens", Fmt.Tokens(s.TotalTokens)), 0);

        var tokens = new StackPanel();
        AddSpaced(tokens, TokenRow("Input", s.InTok, s.TotalTokens, Color.FromRgb(0x4D, 0x7C, 0xFF)), 0);
        AddSpaced(tokens, TokenRow("Output", s.OutTok, s.TotalTokens, Color.FromRgb(0x4C, 0xAF, 0x50)), 9);
        AddSpaced(tokens, TokenRow("Cache Read", s.CrTok, s.TotalTokens, Color.FromRgb(0xFF, 0x9F, 0x40)), 9);
        AddSpaced(tokens, TokenRow("Cache Write", s.CwTok, s.TotalTokens, Color.FromRgb(0xAF, 0x7C, 0xFF)), 9);
        AddSpaced(host, tokens, 9);

        if (s.Tools.Count > 0)
        {
            int maxC = Math.Max(s.Tools.Max(t => t.Count), 1);
            AddSpaced(host, SectionHeader("Tool Calls", Fmt.Int(s.Tools.Sum(t => t.Count))), 16);

            var bars = new StackPanel();
            foreach (var t in s.Tools)
            {
                var glyph = string.IsNullOrEmpty(t.Name) ? "?" : t.Name[..1].ToUpperInvariant();
                AddSpaced(bars, BarRow(Color.FromRgb(0x9E, 0x9E, 0x9E), glyph, t.Name,
                    "", Fmt.Int(t.Count), (double)t.Count / maxC),
                    bars.Children.Count == 0 ? 0 : 12);
            }
            AddSpaced(host, bars, 8);
        }
        return host;
    }
}
