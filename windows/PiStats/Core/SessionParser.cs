using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using PiStats.Models;

namespace PiStats.Core;

/// <summary>
/// Reads Pi *.jsonl session logs and aggregates them per day.
/// Direct port of the Swift Parser.swift + the aggregate-building parts.
/// </summary>
public static class SessionParser
{
    // MARK: - File discovery & signature

    /// One level deep: files directly in sessionsDir, plus *.jsonl inside subdirs.
    public static List<string> SessionFiles(string sessionsDir)
    {
        var files = new List<string>();
        if (!Directory.Exists(sessionsDir)) return files;

        foreach (var entry in Directory.EnumerateFileSystemEntries(sessionsDir))
        {
            if (Directory.Exists(entry))
            {
                foreach (var inner in Directory.EnumerateFiles(entry, "*.jsonl"))
                    files.Add(inner);
            }
            else if (entry.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(entry);
            }
        }
        return files;
    }

    /// Cheap FNV-1a signature of (name + size + mtime) over the sorted file list.
    public static string Signature(IEnumerable<string> files)
    {
        ulong hash = 1469598103934665603UL; // FNV offset basis

        void Mix(string s)
        {
            foreach (var b in Encoding.UTF8.GetBytes(s))
            {
                hash ^= b;
                hash *= 1099511628211UL;
            }
        }

        foreach (var path in files.OrderBy(p => p, StringComparer.Ordinal))
        {
            long size = 0, mtime = 0;
            try
            {
                var fi = new FileInfo(path);
                size = fi.Length;
                mtime = new DateTimeOffset(fi.LastWriteTimeUtc).ToUnixTimeSeconds();
            }
            catch { /* ignore unreadable */ }

            Mix(Path.GetFileName(path));
            Mix(size.ToString(CultureInfo.InvariantCulture));
            Mix(mtime.ToString(CultureInfo.InvariantCulture));
        }
        return hash.ToString("x");
    }

    // MARK: - Aggregate building (with disk cache)

    public static Aggregate BuildAggregate(string sessionsDir, string cacheFile,
                                           bool force, Action<double>? onProgress = null)
    {
        var files = SessionFiles(sessionsDir);
        var sig = Signature(files);

        if (!force && TryLoadCache(cacheFile, out var cached) && cached!.Signature == sig)
        {
            onProgress?.Invoke(1);
            return cached;
        }

        var dayMap = new Dictionary<string, DayAgg>();
        int total = Math.Max(files.Count, 1);

        for (int i = 0; i < files.Count; i++)
        {
            ParseFile(files[i], dayMap);
            onProgress?.Invoke((double)(i + 1) / total);
        }

        var days = dayMap.Values.OrderBy(d => d.Date, StringComparer.Ordinal).ToList();
        var agg = new Aggregate { Signature = sig, Days = days, GeneratedAt = DateTime.Now };

        TrySaveCache(cacheFile, agg);
        return agg;
    }

    private static bool TryLoadCache(string cacheFile, out Aggregate? agg)
    {
        agg = null;
        try
        {
            if (!File.Exists(cacheFile)) return false;
            var json = File.ReadAllText(cacheFile);
            agg = JsonSerializer.Deserialize<Aggregate>(json);
            return agg != null;
        }
        catch { return false; }
    }

    private static void TrySaveCache(string cacheFile, Aggregate agg)
    {
        try
        {
            var json = JsonSerializer.Serialize(agg);
            File.WriteAllText(cacheFile, json);
        }
        catch { /* best effort */ }
    }

    // MARK: - Per-file parsing

    public static void ParseFile(string path, Dictionary<string, DayAgg> dayMap)
    {
        string content;
        try { content = File.ReadAllText(path, Encoding.UTF8); }
        catch { return; }

        var project = DecodeProjectName(Path.GetFileName(Path.GetDirectoryName(path) ?? ""));
        var sessionId = Path.GetFileNameWithoutExtension(path);

        foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            JsonElement obj;
            try
            {
                using var doc = JsonDocument.Parse(line);
                obj = doc.RootElement.Clone();
            }
            catch { continue; }
            if (obj.ValueKind != JsonValueKind.Object) continue;

            var type = GetString(obj, "type");

            if (type == "session")
            {
                var cwd = GetString(obj, "cwd");
                if (!string.IsNullOrEmpty(cwd)) project = LastPathComponent(cwd!);
                var sid = GetString(obj, "id");
                if (!string.IsNullOrEmpty(sid)) sessionId = sid!;
                continue;
            }

            if (type != "message") continue;
            if (!TryGetObject(obj, "message", out var msg)) continue;

            var ts = GetString(obj, "timestamp") ?? "";
            if (!TryParseDate(ts, out var date)) continue;
            var dayKey = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (!dayMap.TryGetValue(dayKey, out var day))
            {
                day = new DayAgg(dayKey);
                dayMap[dayKey] = day;
            }

            var role = GetString(msg, "role");

            if (!day.SessionIds.Contains(sessionId)) day.SessionIds.Add(sessionId);

            if (!day.ProjectSessions.TryGetValue(project, out var ps))
            {
                ps = new List<string>();
                day.ProjectSessions[project] = ps;
            }
            if (!ps.Contains(sessionId)) ps.Add(sessionId);

            switch (role)
            {
                case "user":
                    day.UserMsgs++;
                    break;
                case "toolResult":
                    day.ToolResults++;
                    break;
                case "assistant":
                    day.AsstMsgs++;
                    if (TryGetObject(msg, "usage", out var usage))
                    {
                        day.InTok += GetInt(usage, "input");
                        day.OutTok += GetInt(usage, "output");
                        day.CrTok += GetInt(usage, "cacheRead");
                        day.CwTok += GetInt(usage, "cacheWrite");
                        if (TryGetObject(usage, "cost", out var cost) &&
                            cost.TryGetProperty("total", out var totalEl) &&
                            totalEl.ValueKind == JsonValueKind.Number)
                        {
                            var totalCost = totalEl.GetDouble();
                            day.Cost += totalCost;
                            day.ProjectCost[project] = day.ProjectCost.GetValueOrDefault(project) + totalCost;
                            var model = GetString(msg, "model");
                            if (!string.IsNullOrEmpty(model))
                                day.ModelCost[model!] = day.ModelCost.GetValueOrDefault(model!) + totalCost;
                        }
                    }
                    var mdl = GetString(msg, "model");
                    if (!string.IsNullOrEmpty(mdl))
                        day.ModelCount[mdl!] = day.ModelCount.GetValueOrDefault(mdl!) + 1;
                    break;
            }

            // Tool calls -> languages
            if (msg.TryGetProperty("content", out var contentArr) &&
                contentArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in contentArr.EnumerateArray())
                {
                    if (c.ValueKind != JsonValueKind.Object) continue;
                    if (GetString(c, "type") != "toolCall") continue;
                    var name = GetString(c, "name");
                    if (string.IsNullOrEmpty(name)) continue;

                    day.ToolCount[name!] = day.ToolCount.GetValueOrDefault(name!) + 1;
                    if (name != "edit" && name != "write") continue;

                    if (!TryGetObject(c, "arguments", out var args)) continue;
                    var filePath = GetString(args, "path");
                    if (string.IsNullOrEmpty(filePath)) continue;

                    var ext = PathExtension(filePath!);
                    var lang = LanguageMap.Language(ext);
                    if (lang == null) continue;

                    int addedLines = 0;
                    if (name == "write")
                    {
                        var body = GetString(args, "content");
                        if (body != null) addedLines = LineCount(body);
                    }
                    else // edit
                    {
                        var newText = GetString(args, "newText");
                        if (newText != null)
                        {
                            addedLines = LineCount(newText);
                        }
                        else if (args.TryGetProperty("edits", out var edits) &&
                                 edits.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var e in edits.EnumerateArray())
                            {
                                var nt = GetString(e, "newText");
                                if (nt != null) addedLines += LineCount(nt);
                            }
                        }
                    }

                    day.LangEdits[lang] = day.LangEdits.GetValueOrDefault(lang) + 1;
                    day.LangLines[lang] = day.LangLines.GetValueOrDefault(lang) + addedLines;
                }
            }
        }
    }

    // MARK: - Helpers

    public static int LineCount(string s)
    {
        if (s.Length == 0) return 0;
        int n = 0;
        foreach (var ch in s) if (ch == '\n') n++;
        if (!s.EndsWith('\n')) n++;
        return n;
    }

    /// Last segment of a path, splitting on both / and \ (Windows cwd uses \).
    private static string LastPathComponent(string p)
    {
        var trimmed = p.TrimEnd('/', '\\');
        int idx = trimmed.LastIndexOfAny(new[] { '/', '\\' });
        return idx >= 0 ? trimmed[(idx + 1)..] : trimmed;
    }

    /// Extension without the dot, lowercased.
    private static string PathExtension(string p)
    {
        var ext = Path.GetExtension(p);
        return ext.StartsWith('.') ? ext[1..] : ext;
    }

    /// Best-effort decode of an encoded session directory name into a project.
    /// Windows-aware: drive ("C:") and separators collapse to '-'.
    public static string DecodeProjectName(string encoded)
    {
        var s = encoded.Trim('-');

        var home = PiPaths.Home; // e.g. C:\Users\ali
        var homeEncoded = ReplaceSeparators(home).Trim('-');

        if (homeEncoded.Length > 0 && s.StartsWith(homeEncoded, StringComparison.OrdinalIgnoreCase))
        {
            s = s[homeEncoded.Length..].TrimStart('-');
        }

        string[] commonFolders =
        {
            "desktop", "documents", "downloads", "developer",
            "projects", "oss", "git", "github", "src", "source"
        };

        bool stripped = true;
        while (stripped)
        {
            stripped = false;
            foreach (var folder in commonFolders)
            {
                var prefix = folder + "-";
                if (s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    s = s[prefix.Length..].TrimStart('-');
                    stripped = true;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(s))
        {
            var parts = encoded.Split('-', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[^1] : encoded;
        }
        return s;
    }

    private static string ReplaceSeparators(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
            sb.Append(ch is '/' or '\\' or ':' ? '-' : ch);
        return sb.ToString();
    }

    // MARK: - Date parsing (ISO8601, with/without fractional seconds)

    public static bool TryParseDate(string s, out DateTime localDate)
    {
        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
        {
            localDate = dto.LocalDateTime;
            return true;
        }
        localDate = default;
        return false;
    }

    // MARK: - JsonElement accessors

    private static string? GetString(JsonElement obj, string key)
        => obj.ValueKind == JsonValueKind.Object
           && obj.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    private static int GetInt(JsonElement obj, string key)
    {
        if (obj.ValueKind == JsonValueKind.Object
            && obj.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number
            && v.TryGetInt64(out var l)) return (int)l;
        return 0;
    }

    private static bool TryGetObject(JsonElement obj, string key, out JsonElement value)
    {
        if (obj.ValueKind == JsonValueKind.Object
            && obj.TryGetProperty(key, out value) && value.ValueKind == JsonValueKind.Object)
            return true;
        value = default;
        return false;
    }
}
