import Foundation
import SwiftUI

/// Maps file extensions to a friendly language name, brand-ish color and an SF Symbol.
enum LanguageMap {

    struct Info {
        let name: String
        let color: Color
        let symbol: String
    }

    /// extension (lowercased, no dot) -> language key
    static let extToLang: [String: String] = [
        "ts": "TypeScript", "tsx": "TypeScript", "mts": "TypeScript", "cts": "TypeScript",
        "js": "JavaScript", "jsx": "JavaScript", "mjs": "JavaScript", "cjs": "JavaScript",
        "py": "Python", "pyi": "Python",
        "swift": "Swift",
        "go": "Go",
        "rs": "Rust",
        "rb": "Ruby",
        "java": "Java",
        "kt": "Kotlin",
        "c": "C", "h": "C",
        "cpp": "C++", "cc": "C++", "hpp": "C++", "cxx": "C++",
        "cs": "C#",
        "php": "PHP",
        "css": "CSS", "scss": "CSS", "sass": "CSS", "less": "CSS",
        "html": "HTML", "htm": "HTML", "vue": "Vue", "svelte": "Svelte",
        "md": "Markdown", "mdx": "Markdown",
        "json": "JSON", "yaml": "YAML", "yml": "YAML", "toml": "TOML",
        "sh": "Shell", "bash": "Shell", "zsh": "Shell",
        "sql": "SQL",
        "tex": "LaTeX", "ltx": "LaTeX",
        "lua": "Lua",
        "dart": "Dart",
        "ex": "Elixir", "exs": "Elixir",
        "r": "R",
        "scala": "Scala",
        "xml": "XML",
        "graphql": "GraphQL", "gql": "GraphQL",
        "prisma": "Prisma",
        "dockerfile": "Docker",
    ]

    static let info: [String: Info] = [
        "TypeScript": Info(name: "TypeScript", color: Color(hex: 0x3178C6), symbol: "chevron.left.forwardslash.chevron.right"),
        "JavaScript": Info(name: "JavaScript", color: Color(hex: 0xF7DF1E), symbol: "curlybraces"),
        "Python":     Info(name: "Python",     color: Color(hex: 0x3776AB), symbol: "ladybug"),
        "Swift":      Info(name: "Swift",       color: Color(hex: 0xF05138), symbol: "swift"),
        "Go":         Info(name: "Go",          color: Color(hex: 0x00ADD8), symbol: "g.circle"),
        "Rust":       Info(name: "Rust",        color: Color(hex: 0xDEA584), symbol: "gearshape.2"),
        "Ruby":       Info(name: "Ruby",        color: Color(hex: 0xCC342D), symbol: "diamond"),
        "Java":       Info(name: "Java",        color: Color(hex: 0xE76F00), symbol: "cup.and.saucer"),
        "Kotlin":     Info(name: "Kotlin",      color: Color(hex: 0x7F52FF), symbol: "k.circle"),
        "C":          Info(name: "C",           color: Color(hex: 0x555555), symbol: "c.circle"),
        "C++":        Info(name: "C++",         color: Color(hex: 0x00599C), symbol: "plus.forwardslash.minus"),
        "C#":         Info(name: "C#",          color: Color(hex: 0x68217A), symbol: "number"),
        "PHP":        Info(name: "PHP",         color: Color(hex: 0x777BB4), symbol: "p.circle"),
        "CSS":        Info(name: "CSS",         color: Color(hex: 0x663399), symbol: "paintbrush"),
        "HTML":       Info(name: "HTML",        color: Color(hex: 0xE34F26), symbol: "globe"),
        "Vue":        Info(name: "Vue",         color: Color(hex: 0x42B883), symbol: "v.circle"),
        "Svelte":     Info(name: "Svelte",      color: Color(hex: 0xFF3E00), symbol: "s.circle"),
        "Markdown":   Info(name: "Markdown",    color: Color(hex: 0x6C757D), symbol: "doc.text"),
        "JSON":       Info(name: "JSON",        color: Color(hex: 0xCB9B00), symbol: "curlybraces.square"),
        "YAML":       Info(name: "YAML",        color: Color(hex: 0xCB171E), symbol: "list.bullet.indent"),
        "TOML":       Info(name: "TOML",        color: Color(hex: 0x9C4221), symbol: "list.dash"),
        "Shell":      Info(name: "Shell",       color: Color(hex: 0x4EAA25), symbol: "terminal"),
        "SQL":        Info(name: "SQL",         color: Color(hex: 0x336791), symbol: "cylinder.split.1x2"),
        "LaTeX":      Info(name: "LaTeX",       color: Color(hex: 0x008080), symbol: "function"),
        "Lua":        Info(name: "Lua",         color: Color(hex: 0x000080), symbol: "moon"),
        "Dart":       Info(name: "Dart",        color: Color(hex: 0x0175C2), symbol: "d.circle"),
        "Elixir":     Info(name: "Elixir",      color: Color(hex: 0x6E4A7E), symbol: "drop"),
        "R":          Info(name: "R",           color: Color(hex: 0x276DC3), symbol: "r.circle"),
        "Scala":      Info(name: "Scala",       color: Color(hex: 0xDC322F), symbol: "s.square"),
        "XML":        Info(name: "XML",         color: Color(hex: 0xF1662A), symbol: "chevron.left.slash.chevron.right"),
        "GraphQL":    Info(name: "GraphQL",     color: Color(hex: 0xE10098), symbol: "point.3.connected.trianglepath.dotted"),
        "Prisma":     Info(name: "Prisma",      color: Color(hex: 0x2D3748), symbol: "tablecells"),
        "Docker":     Info(name: "Docker",      color: Color(hex: 0x2496ED), symbol: "shippingbox"),
    ]

    static func language(forExtension ext: String) -> String? {
        extToLang[ext.lowercased()]
    }

    static func infoFor(_ language: String) -> Info {
        info[language] ?? Info(name: language, color: Color(hex: 0x8E8E93), symbol: "doc")
    }
}

extension Color {
    init(hex: UInt32) {
        let r = Double((hex >> 16) & 0xFF) / 255.0
        let g = Double((hex >> 8) & 0xFF) / 255.0
        let b = Double(hex & 0xFF) / 255.0
        self.init(.sRGB, red: r, green: g, blue: b, opacity: 1.0)
    }
}
