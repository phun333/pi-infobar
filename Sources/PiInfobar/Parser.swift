import Foundation

extension StatsEngine {

    /// Compute a cheap signature of the sessions directory (paths + sizes + mtimes).
    nonisolated static func signature(of files: [URL]) -> String {
        var hash: UInt64 = 1469598103934665603 // FNV offset
        func mix(_ s: String) {
            for b in s.utf8 {
                hash ^= UInt64(b)
                hash = hash &* 1099511628211
            }
        }
        for url in files.sorted(by: { $0.path < $1.path }) {
            let attrs = try? FileManager.default.attributesOfItem(atPath: url.path)
            let size = (attrs?[.size] as? Int) ?? 0
            let mtime = (attrs?[.modificationDate] as? Date)?.timeIntervalSince1970 ?? 0
            mix(url.lastPathComponent)
            mix(String(size))
            mix(String(Int(mtime)))
        }
        return String(hash, radix: 16)
    }

    nonisolated static func sessionFiles() -> [URL] {
        let fm = FileManager.default
        guard let dirs = try? fm.contentsOfDirectory(at: sessionsDir,
                                                     includingPropertiesForKeys: nil) else { return [] }
        var files: [URL] = []
        for d in dirs {
            var isDir: ObjCBool = false
            if fm.fileExists(atPath: d.path, isDirectory: &isDir), isDir.boolValue {
                if let inner = try? fm.contentsOfDirectory(at: d, includingPropertiesForKeys: nil) {
                    files.append(contentsOf: inner.filter { $0.pathExtension == "jsonl" })
                }
            } else if d.pathExtension == "jsonl" {
                files.append(d)
            }
        }
        return files
    }

    /// Build (or load from cache) the full aggregate.
    nonisolated static func buildAggregate(force: Bool,
                                           onProgress: @escaping (Double) -> Void) throws -> Aggregate {
        let files = sessionFiles()
        let sig = signature(of: files)

        // Try cache.
        if !force, let data = try? Data(contentsOf: cacheURL),
           let cached = try? JSONDecoder().decode(Aggregate.self, from: data),
           cached.signature == sig {
            onProgress(1)
            return cached
        }

        // (re)parse everything.
        var dayMap: [String: DayAgg] = [:]
        let total = max(files.count, 1)

        for (idx, file) in files.enumerated() {
            parseFile(file, into: &dayMap)
            onProgress(Double(idx + 1) / Double(total))
        }

        let days = dayMap.values.sorted { $0.date < $1.date }
        let agg = Aggregate(signature: sig, days: days, generatedAt: Date())

        if let data = try? JSONEncoder().encode(agg) {
            try? data.write(to: cacheURL)
        }
        return agg
    }

    // MARK: - Per-file parsing

    nonisolated static func parseFile(_ url: URL, into dayMap: inout [String: DayAgg]) {
        guard let content = try? String(contentsOf: url, encoding: .utf8) else { return }

        var project = url.deletingLastPathComponent().lastPathComponent
        project = decodeProjectName(project)
        var sessionId = url.deletingPathExtension().lastPathComponent

        for sub in content.split(separator: "\n", omittingEmptySubsequences: true) {
            let line = String(sub)
            guard let data = line.data(using: .utf8),
                  let obj = try? JSONSerialization.jsonObject(with: data) as? [String: Any]
            else { continue }

            let type = obj["type"] as? String

            if type == "session" {
                if let cwd = obj["cwd"] as? String, !cwd.isEmpty {
                    project = (cwd as NSString).lastPathComponent
                }
                if let sid = obj["id"] as? String { sessionId = sid }
                continue
            }

            guard type == "message",
                  let msg = obj["message"] as? [String: Any] else { continue }

            let tsString = (obj["timestamp"] as? String) ?? ""
            guard let date = parseDate(tsString) else { continue }
            let dayKey = dayFormatter.string(from: date)

            var day = dayMap[dayKey] ?? DayAgg(date: dayKey)

            let role = msg["role"] as? String

            // Mark session active this day.
            if !day.sessionIds.contains(sessionId) {
                day.sessionIds.append(sessionId)
            }
            // Track project sessions.
            var ps = day.projectSessions[project] ?? []
            if !ps.contains(sessionId) { ps.append(sessionId) }
            day.projectSessions[project] = ps

            switch role {
            case "user":
                day.userMsgs += 1
            case "toolResult":
                day.toolResults += 1
            case "assistant":
                day.asstMsgs += 1
                if let usage = msg["usage"] as? [String: Any] {
                    day.inTok += (usage["input"] as? Int) ?? 0
                    day.outTok += (usage["output"] as? Int) ?? 0
                    day.crTok += (usage["cacheRead"] as? Int) ?? 0
                    day.cwTok += (usage["cacheWrite"] as? Int) ?? 0
                    if let cost = usage["cost"] as? [String: Any],
                       let total = cost["total"] as? Double {
                        day.cost += total
                        day.projectCost[project, default: 0] += total
                        if let model = msg["model"] as? String {
                            day.modelCost[model, default: 0] += total
                        }
                    }
                }
                if let model = msg["model"] as? String {
                    day.modelCount[model, default: 0] += 1
                }
            default:
                break
            }

            // Tool calls → languages.
            if let contentArr = msg["content"] as? [[String: Any]] {
                for c in contentArr {
                    guard (c["type"] as? String) == "toolCall",
                          let name = c["name"] as? String else { continue }
                    day.toolCount[name, default: 0] += 1
                    guard name == "edit" || name == "write" else { continue }
                    guard let args = c["arguments"] as? [String: Any],
                          let path = args["path"] as? String else { continue }
                    let ext = (path as NSString).pathExtension
                    guard let lang = LanguageMap.language(forExtension: ext) else { continue }

                    var addedLines = 0
                    if name == "write", let body = args["content"] as? String {
                        addedLines = lineCount(body)
                    } else if name == "edit" {
                        if let newText = args["newText"] as? String {
                            addedLines = lineCount(newText)
                        } else if let edits = args["edits"] as? [[String: Any]] {
                            for e in edits {
                                if let nt = e["newText"] as? String {
                                    addedLines += lineCount(nt)
                                }
                            }
                        }
                    }
                    day.langEdits[lang, default: 0] += 1
                    day.langLines[lang, default: 0] += addedLines
                }
            }

            dayMap[dayKey] = day
        }
    }

    nonisolated static func lineCount(_ s: String) -> Int {
        if s.isEmpty { return 0 }
        var n = 1
        for ch in s.utf8 where ch == 0x0A { n += 1 }
        return n
    }

    /// Decode the encoded directory name back to a project (best effort).
    nonisolated static func decodeProjectName(_ encoded: String) -> String {
        // e.g. "--Users-ali-Documents-GitHub-wraith-internal--"
        var s = encoded
        while s.hasPrefix("-") { s.removeFirst() }
        while s.hasSuffix("-") { s.removeLast() }
        let parts = s.split(separator: "-")
        return parts.last.map(String.init) ?? encoded
    }
}
