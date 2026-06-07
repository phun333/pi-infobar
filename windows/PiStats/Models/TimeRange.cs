namespace PiStats.Models;

/// <summary>Time-range filter — port of the Swift TimeRange enum.</summary>
public enum TimeRange
{
    Day,    // 1d
    Week,   // 7d
    Month,  // 30d
    All
}

public static class TimeRangeExtensions
{
    /// Short label shown in the range picker ("1d", "7d", "30d", "All").
    public static string Label(this TimeRange range) => range switch
    {
        TimeRange.Day => "1d",
        TimeRange.Week => "7d",
        TimeRange.Month => "30d",
        TimeRange.All => "All",
        _ => "All"
    };

    /// Number of days to include; null = everything.
    public static int? Days(this TimeRange range) => range switch
    {
        TimeRange.Day => 1,
        TimeRange.Week => 7,
        TimeRange.Month => 30,
        TimeRange.All => null,
        _ => null
    };

    /// Parse a stored raw value ("1d"/"7d"/"30d"/"All") back to the enum.
    public static TimeRange FromRaw(string? raw) => raw switch
    {
        "1d" => TimeRange.Day,
        "7d" => TimeRange.Week,
        "30d" => TimeRange.Month,
        "All" => TimeRange.All,
        _ => TimeRange.All
    };

    public static IReadOnlyList<TimeRange> AllCases { get; } = new[]
    {
        TimeRange.Day, TimeRange.Week, TimeRange.Month, TimeRange.All
    };
}
