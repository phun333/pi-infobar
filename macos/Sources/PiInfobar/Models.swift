import Foundation
import SwiftUI

// MARK: - Time range filter

enum TimeRange: String, CaseIterable, Identifiable {
    case day = "1d"
    case week = "7d"
    case month = "30d"
    case all = "All"

    var id: String { rawValue }

    var label: String { rawValue }

    /// Number of days to include, nil = everything.
    var days: Int? {
        switch self {
        case .day: return 1
        case .week: return 7
        case .month: return 30
        case .all: return nil
        }
    }
}

// MARK: - Per-day aggregate (the cached unit)

struct DayAgg: Codable {
    var date: String                 // yyyy-MM-dd
    var cost: Double = 0
    var inTok: Int = 0
    var outTok: Int = 0
    var crTok: Int = 0               // cache read
    var cwTok: Int = 0               // cache write
    var userMsgs: Int = 0
    var asstMsgs: Int = 0
    var toolResults: Int = 0
    var sessionIds: [String] = []
    var sessionStart: [String: Double] = [:]   // sessionId -> earliest ts (epoch seconds)
    var sessionEnd: [String: Double] = [:]     // sessionId -> latest ts (epoch seconds)
    var sessionPath: [String: String] = [:]    // sessionId -> source .jsonl file path
    var langLines: [String: Int] = [:]   // language -> lines written
    var langEdits: [String: Int] = [:]   // language -> edit/write count
    var modelCost: [String: Double] = [:]
    var modelCount: [String: Int] = [:]
    var projectCost: [String: Double] = [:]
    var projectSessions: [String: [String]] = [:]
    var toolCount: [String: Int] = [:]
}

// MARK: - Full aggregate (cached to disk)

struct Aggregate: Codable {
    var signature: String           // hash of session files state
    var days: [DayAgg]
    var generatedAt: Date
}

// MARK: - Derived view-model rows

struct LangStat: Identifiable {
    var id: String { name }
    let name: String
    let lines: Int
    let edits: Int
    let color: Color
    let symbol: String
}

struct ModelStat: Identifiable {
    var id: String { name }
    let name: String
    let displayName: String
    let cost: Double
    let count: Int
    let color: Color
}

struct ProjectStat: Identifiable {
    var id: String { name }
    let name: String
    let cost: Double
    let sessions: Int
}

struct ToolStat: Identifiable {
    var id: String { name }
    let name: String
    let count: Int
}

struct DaySpend: Identifiable {
    var id: String { date }
    let date: String
    let day: Date
    let cost: Double
}

struct SessionInfo: Identifiable {
    var id: String { sessionId }
    let sessionId: String
    let date: String
    let project: String
    let duration: Double   // seconds (0 if unknown / single message)
    let filePath: String?  // source .jsonl path (for reveal in Finder)
}

// MARK: - Summary for a selected range

struct StatsSummary {
    var totalCost: Double = 0
    var inTok = 0, outTok = 0, crTok = 0, cwTok = 0
    var sessionCount = 0
    var userMsgs = 0
    var asstMsgs = 0
    var toolResults = 0
    var daysActive = 0
    var todayCost: Double = 0
    var avgCostPerDay: Double = 0

    var languages: [LangStat] = []
    var models: [ModelStat] = []
    var projects: [ProjectStat] = []
    var tools: [ToolStat] = []
    var dailySpend: [DaySpend] = []
    var sessions: [SessionInfo] = []

    var totalTokens: Int { inTok + outTok + crTok + cwTok }
    var totalMessages: Int { userMsgs + asstMsgs }
}
