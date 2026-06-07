namespace PiStats.Services;

/// <summary>What the tray tooltip shows — port of the Swift MenuBarMetric.</summary>
public enum MenuBarMetric
{
    TodayCost,
    TotalCost,
    TodayLines,
    TodayMessages,
    TodaySessions,
    None
}

public static class MenuBarMetricExtensions
{
    public static string Label(this MenuBarMetric m) => m switch
    {
        MenuBarMetric.TodayCost => "Today's cost",
        MenuBarMetric.TotalCost => "Total cost",
        MenuBarMetric.TodayLines => "Lines today",
        MenuBarMetric.TodayMessages => "Messages today",
        MenuBarMetric.TodaySessions => "Sessions today",
        MenuBarMetric.None => "Nothing (icon only)",
        _ => ""
    };

    public static IReadOnlyList<MenuBarMetric> AllCases { get; } = new[]
    {
        MenuBarMetric.TodayCost, MenuBarMetric.TotalCost, MenuBarMetric.TodayLines,
        MenuBarMetric.TodayMessages, MenuBarMetric.TodaySessions, MenuBarMetric.None
    };
}
