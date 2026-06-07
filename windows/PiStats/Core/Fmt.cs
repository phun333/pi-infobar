using System.Globalization;

namespace PiStats.Core;

/// <summary>Number/money/token formatting — port of the Swift Fmt helpers.</summary>
public static class Fmt
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Money(double v) =>
        v >= 1000 ? "$" + v.ToString("F0", Inv) : "$" + v.ToString("F2", Inv);

    public static string MoneyShort(double v) =>
        v >= 1000 ? "$" + (v / 1000).ToString("F1", Inv) + "k" : "$" + v.ToString("F0", Inv);

    public static string Int(int v) => v.ToString("N0", Inv);

    public static string Tokens(int v)
    {
        double d = v;
        if (d >= 1_000_000_000) return (d / 1e9).ToString("F1", Inv) + "B";
        if (d >= 1_000_000) return (d / 1e6).ToString("F1", Inv) + "M";
        if (d >= 1_000) return (d / 1e3).ToString("F1", Inv) + "K";
        return v.ToString(Inv);
    }
}
