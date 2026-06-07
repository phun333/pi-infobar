using System.Globalization;
using System.IO;
using System.Text;
using PiStats.Models;

namespace PiStats.Core;

/// <summary>Headless self-test: parse real logs and produce a text report.</summary>
public static class Diagnostics
{
    public static string Dump()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Sessions dir : {StatsEngine.SessionsDir}");
        sb.AppendLine($"Exists       : {Directory.Exists(StatsEngine.SessionsDir)}");

        var files = SessionParser.SessionFiles(StatsEngine.SessionsDir);
        sb.AppendLine($"Files (.jsonl): {files.Count}");
        sb.AppendLine($"Signature    : {SessionParser.Signature(files)}");
        sb.AppendLine();

        var agg = SessionParser.BuildAggregate(StatsEngine.SessionsDir, StatsEngine.CacheFile,
            force: true, onProgress: null);
        sb.AppendLine($"Days parsed  : {agg.Days.Count}");
        sb.AppendLine();

        var s = StatsEngine.Summarize(agg.Days, TimeRange.All);
        sb.AppendLine("=== Summary (All) ===");
        sb.AppendLine($"Total cost   : ${s.TotalCost.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Today cost   : ${s.TodayCost.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Avg/day      : ${s.AvgCostPerDay.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Sessions     : {s.SessionCount}");
        sb.AppendLine($"Messages     : {s.TotalMessages} (user {s.UserMsgs}, asst {s.AsstMsgs})");
        sb.AppendLine($"Active days  : {s.DaysActive}");
        sb.AppendLine($"Tokens       : in {s.InTok}, out {s.OutTok}, cr {s.CrTok}, cw {s.CwTok}");
        sb.AppendLine();

        sb.AppendLine("Top languages:");
        foreach (var l in s.Languages.Take(8))
            sb.AppendLine($"  {l.Name,-12} {l.Lines,8} lines  {l.Edits,5} edits");
        sb.AppendLine();

        sb.AppendLine("Top models:");
        foreach (var m in s.Models.Take(8))
            sb.AppendLine($"  {m.DisplayName,-22} ${m.Cost.ToString("F2", CultureInfo.InvariantCulture),9}  {m.Count} calls");
        sb.AppendLine();

        sb.AppendLine("Top projects:");
        foreach (var p in s.Projects.Take(10))
            sb.AppendLine($"  {p.Name,-28} ${p.Cost.ToString("F2", CultureInfo.InvariantCulture),9}  {p.Sessions} sess");
        sb.AppendLine();

        sb.AppendLine("Tools:");
        foreach (var t in s.Tools.Take(12))
            sb.AppendLine($"  {t.Name,-14} {t.Count}");

        return sb.ToString();
    }
}
