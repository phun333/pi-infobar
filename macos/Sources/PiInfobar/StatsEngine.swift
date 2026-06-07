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

    nonisolated static var sessionsDir: URL {
        let home = FileManager.default.homeDirectoryForCurrentUser
        if UserDefaults.standard.bool(forKey: SettingsKeys.remoteSyncEnabled) {
            return home.appendingPathComponent(".pi/remote-agent-sessions")
        }
        return home.appendingPathComponent(".pi/agent/sessions")
    }

    nonisolated static var cacheURL: URL {
        let home = FileManager.default.homeDirectoryForCurrentUser
        if UserDefaults.standard.bool(forKey: SettingsKeys.remoteSyncEnabled) {
            return home.appendingPathComponent(".pi/pi-infobar-remote-cache.json")
        }
        return home.appendingPathComponent(".pi/pi-infobar-cache.json")
    }

    // MARK: - Public API

    nonisolated static func performRemoteSync() async throws {
        let defaults = UserDefaults.standard
        let host = defaults.string(forKey: SettingsKeys.remoteHost) ?? ""
        let port = defaults.string(forKey: SettingsKeys.remotePort) ?? "22"
        let user = defaults.string(forKey: SettingsKeys.remoteUser) ?? ""
        let keyPath = defaults.string(forKey: SettingsKeys.remoteKeyPath) ?? "~/.ssh/id_rsa"
        let remotePath = defaults.string(forKey: SettingsKeys.remotePath) ?? "~/.pi/agent/sessions"

        guard !host.isEmpty, !user.isEmpty else {
            throw NSError(domain: "PiStats", code: 1, userInfo: [NSLocalizedDescriptionKey: "Remote host and username must be configured in Settings."])
        }

        try await RemoteSync.sync(
            host: host,
            port: port,
            user: user,
            keyPath: keyPath,
            remotePath: remotePath,
            localPath: sessionsDir
        )
    }

    func load(force: Bool = false) {
        loading = true
        progress = 0
        error = nil
        Task.detached(priority: .userInitiated) {
            do {
                if UserDefaults.standard.bool(forKey: SettingsKeys.remoteSyncEnabled) {
                    do {
                        try await Self.performRemoteSync()
                    } catch {
                        let cacheExists = FileManager.default.fileExists(atPath: Self.cacheURL.path)
                        if !cacheExists {
                            throw error
                        }
                        NSLog("Remote sync failed: \(error.localizedDescription). Using cached data.")
                    }
                }

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

    private var todayAgg: DayAgg? {
        let key = Self.dayFormatter.string(from: Date())
        return days.first(where: { $0.date == key })
    }

    /// Today's cost — used for the menu bar title.
    var todayCost: Double { todayAgg?.cost ?? 0 }
    var todayLines: Int { todayAgg?.langLines.values.reduce(0, +) ?? 0 }
    var todayMessages: Int { (todayAgg.map { $0.userMsgs + $0.asstMsgs }) ?? 0 }
    var todaySessions: Int { todayAgg?.sessionIds.count ?? 0 }
    var totalCostAll: Double { days.reduce(0) { $0 + $1.cost } }

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
        var filledSpend: [DaySpend] = []
        let startDate: Date
        let endDate = cal.startOfDay(for: Date())

        if let nDays = range.days {
            startDate = cal.date(byAdding: .day, value: -(nDays - 1), to: endDate)!
        } else if let earliest = spend.map({ $0.day }).min() {
            startDate = earliest
        } else {
            startDate = endDate
        }

        var cur = startDate
        let spendMap = Dictionary(uniqueKeysWithValues: spend.map { ($0.date, $0) })

        while cur <= endDate {
            let curKey = dayFormatter.string(from: cur)
            if let existing = spendMap[curKey] {
                filledSpend.append(existing)
            } else {
                filledSpend.append(DaySpend(date: curKey, day: cur, cost: 0.0))
            }
            guard let next = cal.date(byAdding: .day, value: 1, to: cur) else { break }
            cur = next
        }

        s.dailySpend = filledSpend

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

    nonisolated(unsafe) static let isoParser: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        return f
    }()

    nonisolated(unsafe) static let isoParserNoFrac: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime]
        return f
    }()

    nonisolated static func parseDate(_ s: String) -> Date? {
        if let d = isoParser.date(from: s) { return d }
        return isoParserNoFrac.date(from: s)
    }
}
