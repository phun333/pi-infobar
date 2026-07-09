import XCTest
@testable import PiInfobar

final class PiInfobarTests: XCTestCase {

    func testLineCount() {
        XCTAssertEqual(StatsEngine.lineCount(""), 0)
        XCTAssertEqual(StatsEngine.lineCount("hello"), 1)
        XCTAssertEqual(StatsEngine.lineCount("hello\n"), 1)
        XCTAssertEqual(StatsEngine.lineCount("hello\nworld"), 2)
        XCTAssertEqual(StatsEngine.lineCount("hello\nworld\n"), 2)
        XCTAssertEqual(StatsEngine.lineCount("\n"), 1)
        XCTAssertEqual(StatsEngine.lineCount("\n\n"), 2)
    }

    func testDecodeProjectName() {
        let home = NSHomeDirectory()
        let username = (home as NSString).lastPathComponent
        
        let path1 = "--Users-\(username)-Desktop-oss-project-diary-opencode--"
        XCTAssertEqual(StatsEngine.decodeProjectName(path1), "project-diary-opencode")

        let path2 = "--Users-\(username)-Desktop-projects-opentv--"
        XCTAssertEqual(StatsEngine.decodeProjectName(path2), "opentv")

        let path3 = "--Users-\(username)--"
        XCTAssertEqual(StatsEngine.decodeProjectName(path3), username)
        
        let path4 = "simple-project"
        XCTAssertEqual(StatsEngine.decodeProjectName(path4), "simple-project")
    }

    func testParseFile() throws {
        let fm = FileManager.default
        let tempDir = fm.temporaryDirectory
        let fileURL = tempDir.appendingPathComponent("test-session.jsonl")

        let jsonl = """
        {"type":"session","version":3,"id":"session-123","timestamp":"2026-06-06T00:00:01.000Z","cwd":"/Users/shrijit/Desktop/oss/pi-infobar"}
        {"type":"message","id":"msg-1","timestamp":"2026-06-06T00:00:02.000Z","message":{"role":"user","content":[{"type":"text","text":"Hello world"}]}}
        {"type":"message","id":"msg-2","timestamp":"2026-06-06T00:00:05.000Z","message":{"role":"assistant","content":[{"type":"thinking","thinking":"Replying to user"},{"type":"text","text":"Hi there!"},{"type":"toolCall","id":"call-1","name":"write","arguments":{"path":"Sources/PiInfobar/App.swift","content":"import SwiftUI\\n// New line\\n"}}],"model":"gpt-4-turbo","usage":{"input":100,"output":50,"cacheRead":10,"cacheWrite":20,"totalTokens":180,"cost":{"input":0.001,"output":0.002,"cacheRead":0.0001,"cacheWrite":0.0002,"total":0.0033}}}}
        {"type":"message","id":"msg-3","timestamp":"2026-06-06T00:00:10.000Z","message":{"role":"toolResult","toolCallId":"call-1","toolName":"write","content":[{"type":"text","text":"Success"}]}}
        """

        try jsonl.write(to: fileURL, atomically: true, encoding: .utf8)
        defer { try? fm.removeItem(at: fileURL) }

        var dayMap: [String: DayAgg] = [:]
        StatsEngine.parseFile(fileURL, into: &dayMap)

        let dayKey = "2026-06-06"
        let dayAgg = try XCTUnwrap(dayMap[dayKey])

        XCTAssertEqual(dayAgg.userMsgs, 1)
        XCTAssertEqual(dayAgg.asstMsgs, 1)
        XCTAssertEqual(dayAgg.toolResults, 1)

        XCTAssertEqual(dayAgg.inTok, 100)
        XCTAssertEqual(dayAgg.outTok, 50)
        XCTAssertEqual(dayAgg.crTok, 10)
        XCTAssertEqual(dayAgg.cwTok, 20)

        XCTAssertEqual(dayAgg.cost, 0.0033, accuracy: 0.00001)
        XCTAssertEqual(try XCTUnwrap(dayAgg.projectCost["pi-infobar"]), 0.0033, accuracy: 0.00001)
        XCTAssertEqual(try XCTUnwrap(dayAgg.modelCost["gpt-4-turbo"]), 0.0033, accuracy: 0.00001)

        XCTAssertEqual(dayAgg.toolCount["write"], 1)

        XCTAssertEqual(dayAgg.langLines["Swift"], 2)
        XCTAssertEqual(dayAgg.langEdits["Swift"], 1)
    }

    func testSummarizeAndGapFilling() {
        let cal = Calendar.current
        let today = cal.startOfDay(for: Date())
        
        let f = StatsEngine.dayFormatter
        let todayStr = f.string(from: today)
        let threeDaysAgoStr = f.string(from: cal.date(byAdding: .day, value: -3, to: today)!)
        
        let day1 = DayAgg(date: threeDaysAgoStr, cost: 5.0, inTok: 100, outTok: 50, userMsgs: 2, asstMsgs: 2)
        let day2 = DayAgg(date: todayStr, cost: 10.0, inTok: 200, outTok: 100, userMsgs: 4, asstMsgs: 4)
        
        let days = [day1, day2]
        
        // Test 7d range
        let summary7d = StatsEngine.summarize(days: days, range: .week)
        XCTAssertEqual(summary7d.dailySpend.count, 7)
        XCTAssertEqual(summary7d.totalCost, 15.0)
        XCTAssertEqual(summary7d.daysActive, 2)
        XCTAssertEqual(summary7d.avgCostPerDay, 7.5, accuracy: 0.00001)
        
        // Verify daily spend entries (should be sorted and filled with 0 where appropriate)
        let spendMap = Dictionary(uniqueKeysWithValues: summary7d.dailySpend.map { ($0.date, $0.cost) })
        XCTAssertEqual(spendMap[threeDaysAgoStr], 5.0)
        XCTAssertEqual(spendMap[todayStr], 10.0)
        
        let yesterdayStr = f.string(from: cal.date(byAdding: .day, value: -1, to: today)!)
        XCTAssertEqual(spendMap[yesterdayStr], 0.0)
    }

    func testSummarizeSessions() {
        let cal = Calendar.current
        let today = cal.startOfDay(for: Date())
        let f = StatsEngine.dayFormatter
        let todayStr = f.string(from: today)
        let olderStr = f.string(from: cal.date(byAdding: .day, value: -2, to: today)!)

        var older = DayAgg(date: olderStr, cost: 1.0)
        older.sessionIds = ["sess-A"]
        older.sessionStart = ["sess-A": 1000]
        older.sessionEnd = ["sess-A": 1000 + 90 * 60]  // 90 minutes
        older.sessionPath = ["sess-A": "/tmp/sess-A.jsonl"]
        older.projectSessions = ["proj-x": ["sess-A"]]

        var newer = DayAgg(date: todayStr, cost: 2.0)
        // Two sessions on the same day to exercise the tie-breaker
        newer.sessionIds = ["sess-C", "sess-B"]
        newer.sessionStart = ["sess-B": 5000, "sess-C": 8000]
        newer.sessionEnd = ["sess-B": 5000 + 45, "sess-C": 8000]  // B: 45s, C: single msg (0)
        newer.projectSessions = ["proj-y": ["sess-B", "sess-C"]]

        let summary = StatsEngine.summarize(days: [older, newer], range: .week)

        // All unique sessions are present
        XCTAssertEqual(summary.sessions.count, 3)

        // Newest first, with sessionId as deterministic tie-breaker within a day
        XCTAssertEqual(summary.sessions.map { $0.sessionId }, ["sess-B", "sess-C", "sess-A"])

        // Date and project mapping is correct
        let byId = Dictionary(uniqueKeysWithValues: summary.sessions.map { ($0.sessionId, $0) })
        XCTAssertEqual(byId["sess-A"]?.date, olderStr)
        XCTAssertEqual(byId["sess-A"]?.project, "proj-x")
        XCTAssertEqual(byId["sess-B"]?.date, todayStr)
        XCTAssertEqual(byId["sess-B"]?.project, "proj-y")

        // Duration derived from first/last activity (0 when only one timestamp)
        XCTAssertEqual(byId["sess-A"]?.duration, 90 * 60, accuracy: 0.001)
        XCTAssertEqual(byId["sess-B"]?.duration, 45, accuracy: 0.001)
        XCTAssertEqual(byId["sess-C"]?.duration, 0, accuracy: 0.001)

        // File path is carried through for reveal-in-Finder
        XCTAssertEqual(byId["sess-A"]?.filePath, "/tmp/sess-A.jsonl")
        XCTAssertNil(byId["sess-C"]?.filePath)

        // Sorting is deterministic across repeated runs
        let summary2 = StatsEngine.summarize(days: [older, newer], range: .week)
        XCTAssertEqual(summary.sessions.map { $0.sessionId },
                       summary2.sessions.map { $0.sessionId })
    }
}
