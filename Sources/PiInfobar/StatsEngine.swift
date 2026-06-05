import Foundation
import SwiftUI

@MainActor
final class StatsEngine: ObservableObject {

    @Published var loading = true
    @Published var progress = 0.0
    @Published var error: String?
    @Published var lastUpdated: Date?

    /// The parsed per-day aggregates (full history).
    private var days: [DayAgg] = []

    nonisolated static let sessionsDir: URL = {
        let home = FileManager.default.homeDirectoryForCurrentUser
        return home.appendingPathComponent(".pi/agent/sessions")
    }()

    nonisolated static let cacheURL: URL = {
        let home = FileManager.default.homeDirectoryForCurrentUser
        return home.appendingPathComponent(".pi/pi-infobar-cache.json")
    }()

    // MARK: - Public API

    func load(force: Bool = false) {
        loading = true
        progress = 0
        error = nil
        Task.detached(priority: .userInitiated) {
            do {
                let agg = try Self.buildAggregate(force: force) { p in
                    Task { @MainActor in self.progress = p }
                }
                await MainActor.run {
                    self.days = agg.days
                    self.lastUpdated = agg.generatedAt
                    self.loading = false
                    self.progress = 1
                }
            } catch {
                await MainActor.run {
                    self.error = error.localizedDescription
                    self.loading = false
                }
            }
        }
    }

    /// Build a summary for the given range from already-parsed days.
    func summary(for range: TimeRange) -> StatsSummary {
        Self.summarize(days: days, range: range)
    }

    /// Today's cost — used for the menu bar title.
    var todayCost: Double {
        let key = Self.dayFormatter.string(from: Date())
        return days.first(where: { $0.date == key })?.cost ?? 0
    }

    // MARK: - Aggregation / summarizing

    nonisolated static func summarize(days: [DayAgg], range: TimeRange) -> StatsSummary {
        let cal = Calendar.current
        let todayKey = dayFormatter.string(from: Date())

        // Filter days by range.
        let filtered: [DayAgg]
        if let nDays = range.days {
            let cutoff = cal.date(byAdding: .day, value: -(nDays - 1), to: cal.startOfDay(for: Date()))!
            filtered = days.filter { d in
                guard let dt = dayFormatter.date(from: d.date) else { return false }
                return dt >= cutoff
            }
        } else {
            filtered = days
        }

        var s = StatsSummary()
        var sessionSet = Set<String>()
        var langLines: [String: Int] = [:]
        var langEdits: [String: Int] = [:]
        var modelCost: [String: Double] = [:]
        var modelCount: [String: Int] = [:]
        var projCost: [String: Double] = [:]
        var projSessions: [String: Set<String>] = [:]
        var toolCount: [String: Int] = [:]
        var spend: [DaySpend] = []

        for d in filtered {
            s.totalCost += d.cost
            s.inTok += d.inTok; s.outTok += d.outTok
            s.crTok += d.crTok; s.cwTok += d.cwTok
            s.userMsgs += d.userMsgs; s.asstMsgs += d.asstMsgs
            s.toolResults += d.toolResults
            sessionSet.formUnion(d.sessionIds)
            for (k, v) in d.langLines { langLines[k, default: 0] += v }
            for (k, v) in d.langEdits { langEdits[k, default: 0] += v }
            for (k, v) in d.modelCost { modelCost[k, default: 0] += v }
            for (k, v) in d.modelCount { modelCount[k, default: 0] += v }
            for (k, v) in d.projectCost { projCost[k, default: 0] += v }
            for (k, ids) in d.projectSessions { projSessions[k, default: []].formUnion(ids) }
            for (k, v) in d.toolCount { toolCount[k, default: 0] += v }
            if d.date == todayKey { s.todayCost += d.cost }
            if let dt = dayFormatter.date(from: d.date) {
                spend.append(DaySpend(date: d.date, day: dt, cost: d.cost))
            }
        }

        s.sessionCount = sessionSet.count
        s.daysActive = filtered.filter { $0.cost > 0 || $0.asstMsgs > 0 }.count
        s.avgCostPerDay = s.daysActive > 0 ? s.totalCost / Double(s.daysActive) : 0

        // Languages
        s.languages = langLines.map { (lang, lines) in
            let info = LanguageMap.infoFor(lang)
            return LangStat(name: lang, lines: lines, edits: langEdits[lang] ?? 0,
                            color: info.color, symbol: info.symbol)
        }
        .filter { $0.edits > 0 }
        .sorted { $0.lines == $1.lines ? $0.edits > $1.edits : $0.lines > $1.lines }

        // Models
        s.models = modelCount.map { (m, c) in
            ModelStat(name: m, displayName: prettyModel(m), cost: modelCost[m] ?? 0,
                      count: c, color: modelColor(m))
        }
        .sorted { $0.cost > $1.cost }

        // Projects
        s.projects = projCost.map { (p, c) in
            ProjectStat(name: p, cost: c, sessions: projSessions[p]?.count ?? 0)
        }
        .sorted { $0.cost > $1.cost }

        // Tools
        s.tools = toolCount.map { ToolStat(name: $0.key, count: $0.value) }
            .sorted { $0.count > $1.count }

        // Daily spend (sorted ascending, fill gaps so the chart looks continuous)
        s.dailySpend = spend.sorted { $0.day < $1.day }

        return s
    }

    nonisolated static func prettyModel(_ id: String) -> String {
        var n = id
        n = n.replacingOccurrences(of: "claude-", with: "Claude ")
        n = n.replacingOccurrences(of: "gpt-", with: "GPT-")
        // strip date suffixes like -20251101
        if let r = n.range(of: #"-\d{8}$"#, options: .regularExpression) {
            n.removeSubrange(r)
        }
        n = n.replacingOccurrences(of: "opus", with: "Opus")
        n = n.replacingOccurrences(of: "sonnet", with: "Sonnet")
        n = n.replacingOccurrences(of: "haiku", with: "Haiku")
        return n
    }

    nonisolated static func modelColor(_ id: String) -> Color {
        if id.contains("opus") { return Color(hex: 0xD97757) }      // Anthropic warm
        if id.contains("sonnet") { return Color(hex: 0xCC8B5C) }
        if id.contains("haiku") { return Color(hex: 0xE0A971) }
        if id.contains("gpt") { return Color(hex: 0x10A37F) }       // OpenAI green
        if id.contains("gemini") { return Color(hex: 0x4285F4) }
        return Color(hex: 0x8E8E93)
    }

    // MARK: - Date formatting

    nonisolated static let dayFormatter: DateFormatter = {
        let f = DateFormatter()
        f.locale = Locale(identifier: "en_US_POSIX")
        f.timeZone = TimeZone.current
        f.dateFormat = "yyyy-MM-dd"
        return f
    }()

    nonisolated static let isoParser: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        return f
    }()

    nonisolated static let isoParserNoFrac: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime]
        return f
    }()

    nonisolated static func parseDate(_ s: String) -> Date? {
        if let d = isoParser.date(from: s) { return d }
        return isoParserNoFrac.date(from: s)
    }
}
