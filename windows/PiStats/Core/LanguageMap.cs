using System.Windows.Media;

namespace PiStats.Core;

/// <summary>
/// Maps file extensions to a friendly language name, brand-ish color and a glyph.
/// Port of the Swift LanguageMap. (SF Symbols replaced by a one-letter glyph;
/// the UI draws a colored chip with this letter.)
/// </summary>
public static class LanguageMap
{
    public sealed record Info(string Name, Color Color, string Symbol);

    /// extension (lowercased, no dot) -> language key
    private static readonly Dictionary<string, string> ExtToLang = new()
    {
        ["ts"] = "TypeScript", ["tsx"] = "TypeScript", ["mts"] = "TypeScript", ["cts"] = "TypeScript",
        ["js"] = "JavaScript", ["jsx"] = "JavaScript", ["mjs"] = "JavaScript", ["cjs"] = "JavaScript",
        ["py"] = "Python", ["pyi"] = "Python",
        ["swift"] = "Swift",
        ["go"] = "Go",
        ["rs"] = "Rust",
        ["rb"] = "Ruby",
        ["java"] = "Java",
        ["kt"] = "Kotlin",
        ["c"] = "C", ["h"] = "C",
        ["cpp"] = "C++", ["cc"] = "C++", ["hpp"] = "C++", ["cxx"] = "C++",
        ["cs"] = "C#",
        ["php"] = "PHP",
        ["css"] = "CSS", ["scss"] = "CSS", ["sass"] = "CSS", ["less"] = "CSS",
        ["html"] = "HTML", ["htm"] = "HTML", ["vue"] = "Vue", ["svelte"] = "Svelte",
        ["md"] = "Markdown", ["mdx"] = "Markdown",
        ["json"] = "JSON", ["yaml"] = "YAML", ["yml"] = "YAML", ["toml"] = "TOML",
        ["sh"] = "Shell", ["bash"] = "Shell", ["zsh"] = "Shell",
        ["sql"] = "SQL",
        ["tex"] = "LaTeX", ["ltx"] = "LaTeX",
        ["lua"] = "Lua",
        ["dart"] = "Dart",
        ["ex"] = "Elixir", ["exs"] = "Elixir",
        ["r"] = "R",
        ["scala"] = "Scala",
        ["xml"] = "XML",
        ["graphql"] = "GraphQL", ["gql"] = "GraphQL",
        ["prisma"] = "Prisma",
        ["dockerfile"] = "Docker",
    };

    private static readonly Dictionary<string, Info> InfoMap = new()
    {
        ["TypeScript"] = new("TypeScript", Hex(0x3178C6), "TS"),
        ["JavaScript"] = new("JavaScript", Hex(0xF7DF1E), "JS"),
        ["Python"]     = new("Python",     Hex(0x3776AB), "PY"),
        ["Swift"]      = new("Swift",      Hex(0xF05138), "SW"),
        ["Go"]         = new("Go",         Hex(0x00ADD8), "GO"),
        ["Rust"]       = new("Rust",       Hex(0xDEA584), "RS"),
        ["Ruby"]       = new("Ruby",       Hex(0xCC342D), "RB"),
        ["Java"]       = new("Java",       Hex(0xE76F00), "JV"),
        ["Kotlin"]     = new("Kotlin",     Hex(0x7F52FF), "KT"),
        ["C"]          = new("C",          Hex(0x555555), "C"),
        ["C++"]        = new("C++",        Hex(0x00599C), "C+"),
        ["C#"]         = new("C#",         Hex(0x68217A), "C#"),
        ["PHP"]        = new("PHP",        Hex(0x777BB4), "PH"),
        ["CSS"]        = new("CSS",        Hex(0x663399), "CS"),
        ["HTML"]       = new("HTML",       Hex(0xE34F26), "HT"),
        ["Vue"]        = new("Vue",        Hex(0x42B883), "VU"),
        ["Svelte"]     = new("Svelte",     Hex(0xFF3E00), "SV"),
        ["Markdown"]   = new("Markdown",   Hex(0x6C757D), "MD"),
        ["JSON"]       = new("JSON",       Hex(0xCB9B00), "{}"),
        ["YAML"]       = new("YAML",       Hex(0xCB171E), "YM"),
        ["TOML"]       = new("TOML",       Hex(0x9C4221), "TM"),
        ["Shell"]      = new("Shell",      Hex(0x4EAA25), "SH"),
        ["SQL"]        = new("SQL",        Hex(0x336791), "DB"),
        ["LaTeX"]      = new("LaTeX",      Hex(0x008080), "TX"),
        ["Lua"]        = new("Lua",        Hex(0x000080), "LU"),
        ["Dart"]       = new("Dart",       Hex(0x0175C2), "DT"),
        ["Elixir"]     = new("Elixir",     Hex(0x6E4A7E), "EX"),
        ["R"]          = new("R",          Hex(0x276DC3), "R"),
        ["Scala"]      = new("Scala",      Hex(0xDC322F), "SC"),
        ["XML"]        = new("XML",        Hex(0xF1662A), "XM"),
        ["GraphQL"]    = new("GraphQL",    Hex(0xE10098), "GQ"),
        ["Prisma"]     = new("Prisma",     Hex(0x2D3748), "PR"),
        ["Docker"]     = new("Docker",     Hex(0x2496ED), "DK"),
    };

    public static string? Language(string extension)
        => ExtToLang.TryGetValue(extension.ToLowerInvariant(), out var lang) ? lang : null;

    public static Info InfoFor(string language)
        => InfoMap.TryGetValue(language, out var info)
            ? info
            : new Info(language, Hex(0x8E8E93), language.Length > 0 ? language[..1].ToUpperInvariant() : "?");

    /// 0xRRGGBB -> opaque Color (mirrors the Swift Color(hex:) extension).
    public static Color Hex(uint hex) => Color.FromRgb(
        (byte)((hex >> 16) & 0xFF),
        (byte)((hex >> 8) & 0xFF),
        (byte)(hex & 0xFF));
}
