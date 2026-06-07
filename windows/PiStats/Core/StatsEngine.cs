using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Media;
using PiStats.Models;

namespace PiStats.Core;

/// <summary>
/// Loads/caches the per-day aggregates and derives range summaries.
/// Port of the Swift StatsEngine (ObservableObject -> INotifyPropertyChanged).
/// </summary>
public sealed partial class StatsEngine : INotifyPropertyChanged
{
    private bool _loading = true;
    private double _progress;
    private string? _error;
    private DateTime? _lastUpdated;

    public bool Loading { get => _loading; private set => Set(ref _loading, value); }
    public double Progress { get => _progress; private set => Set(ref _progress, value); }
    public string? Error { get => _error; private set => Set(ref _error, value); }
    public DateTime? LastUpdated { get => _lastUpdated; private set => Set(ref _lastUpdated, value); }

    private List<DayAgg> _days = new();

    // MARK: - Paths (switch between local and remote-synced data)

    private static bool RemoteEnabled => Services.SettingsStore.Shared.RemoteSyncEnabled;

    public static string SessionsDir =>
        RemoteEnabled ? PiPaths.RemoteSessionsDir : PiPaths.LocalSessionsDir;
    public static string CacheFile =>
        RemoteEnabled ? PiPaths.RemoteCacheFile : PiPaths.LocalCacheFile;

    // MARK: - Loading

    public async Task LoadAsync(bool force = false)
    {
        Loading = true;
        Progress = 0;
        Error = null;

        try
        {
            if (RemoteEnabled)
            {
                try
                {
                    await PerformRemoteSyncAsync();
                }
                catch (Exception ex)
                {
                    // Fall back to cached remote data if the sync fails.
                    if (!System.IO.File.Exists(CacheFile)) throw;
                    System.Diagnostics.Debug.WriteLine($"Remote sync failed: {ex.Message}. Using cache.");
                }
            }

            var sessionsDir = SessionsDir;
            var cacheFile = CacheFile;
            var agg = await Task.Run(() =>
                SessionParser.BuildAggregate(sessionsDir, cacheFile, force,
                    p => Progress = p));

            _days = agg.Days;
            LastUpdated = agg.GeneratedAt;
            Progress = 1;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            Loading = false;
        }
    }

    public StatsSummary Summary(TimeRange range) => Summarize(_days, range);

    // MARK: - Remote sync

    public static async Task PerformRemoteSyncAsync()
    {
        var s = Services.SettingsStore.Shared;
        await Services.RemoteSync.SyncAsync(
            host: s.RemoteHost,
            port: s.RemotePort,
            user: s.RemoteUser,
            keyPath: s.RemoteKeyPath,
            remotePath: string.IsNullOrWhiteSpace(s.RemotePath) ? "~/.pi/agent/sessions" : s.RemotePath,
            localPath: PiPaths.RemoteSessionsDir);
    }

    // MARK: - Today metrics (for the tray title)

    private DayAgg? TodayAgg()
    {
        var key = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return _days.FirstOrDefault(d => d.Date == key);
    }

    public double TodayCost => TodayAgg()?.Cost ?? 0;
    public int TodayLines => TodayAgg()?.LangLines.Values.Sum() ?? 0;
    public int TodayMessages => TodayAgg() is { } d ? d.UserMsgs + d.AsstMsgs : 0;
    public int TodaySessions => TodayAgg()?.SessionIds.Count ?? 0;
    public double TotalCostAll => _days.Sum(d => d.Cost);

    // MARK: - Summarizing

    public static StatsSummary Summarize(List<DayAgg> days, TimeRange range)
    {
        var todayKey = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        List<DayAgg> filtered;
        var nDays = range.Days();
        if (nDays is int n)
        {
            var cutoff = DateTime.Today.AddDays(-(n - 1));
            filtered = days.Where(d =>
                DateTime.TryParseExact(d.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt) && dt >= cutoff).ToList();
        }
        else
        {
            filtered = days;
        }

        var s = new StatsSummary();
        var sessionSet = new HashSet<string>();
        var langLines = new Dictionary<string, int>();
        var langEdits = new Dictionary<string, int>();
        var modelCost = new Dictionary<string, double>();
        var modelCount = new Dictionary<string, int>();
        var projCost = new Dictionary<string, double>();
        var projSessions = new Dictionary<string, HashSet<string>>();
        var toolCount = new Dictionary<string, int>();
        var spend = new List<DaySpend>();

        static void Add<TK>(Dictionary<TK, int> map, TK k, int v) where TK : notnull
            => map[k] = map.GetValueOrDefault(k) + v;
        static void AddD<TK>(Dictionary<TK, double> map, TK k, double v) where TK : notnull
            => map[k] = map.GetValueOrDefault(k) + v;

        foreach (var d in filtered)
        {
            s.TotalCost += d.Cost;
            s.InTok += d.InTok; s.OutTok += d.OutTok;
            s.CrTok += d.CrTok; s.CwTok += d.CwTok;
            s.UserMsgs += d.UserMsgs; s.AsstMsgs += d.AsstMsgs;
            s.ToolResults += d.ToolResults;
            sessionSet.UnionWith(d.SessionIds);
            foreach (var (k, v) in d.LangLines) Add(langLines, k, v);
            foreach (var (k, v) in d.LangEdits) Add(langEdits, k, v);
            foreach (var (k, v) in d.ModelCost) AddD(modelCost, k, v);
            foreach (var (k, v) in d.ModelCount) Add(modelCount, k, v);
            foreach (var (k, v) in d.ProjectCost) AddD(projCost, k, v);
            foreach (var (k, ids) in d.ProjectSessions)
            {
                if (!projSessions.TryGetValue(k, out var set)) projSessions[k] = set = new();
                set.UnionWith(ids);
            }
            foreach (var (k, v) in d.ToolCount) Add(toolCount, k, v);
            if (d.Date == todayKey) s.TodayCost += d.Cost;
            if (DateTime.TryParseExact(d.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                spend.Add(new DaySpend(d.Date, dt, d.Cost));
        }

        s.SessionCount = sessionSet.Count;
        s.DaysActive = filtered.Count(d => d.Cost > 0 || d.AsstMsgs > 0);
        s.AvgCostPerDay = s.DaysActive > 0 ? s.TotalCost / s.DaysActive : 0;

        // Languages
        s.Languages = langLines.Select(kv =>
        {
            var info = LanguageMap.InfoFor(kv.Key);
            return new LangStat(kv.Key, kv.Value, langEdits.GetValueOrDefault(kv.Key),
                info.Color, info.Symbol);
        })
        .Where(l => l.Edits > 0)
        .OrderByDescending(l => l.Lines).ThenByDescending(l => l.Edits)
        .ToList();

        // Models
        s.Models = modelCount.Select(kv =>
            new ModelStat(kv.Key, PrettyModel(kv.Key), modelCost.GetValueOrDefault(kv.Key),
                kv.Value, ModelColor(kv.Key)))
            .OrderByDescending(m => m.Cost)
            .ToList();

        // Projects
        s.Projects = projCost.Select(kv =>
            new ProjectStat(kv.Key, kv.Value, projSessions.GetValueOrDefault(kv.Key)?.Count ?? 0))
            .OrderByDescending(p => p.Cost)
            .ToList();

        // Tools
        s.Tools = toolCount.Select(kv => new ToolStat(kv.Key, kv.Value))
            .OrderByDescending(t => t.Count)
            .ToList();

        // Daily spend (fill gaps so the chart is continuous)
        var endDate = DateTime.Today;
        DateTime startDate;
        if (nDays is int nn)
            startDate = endDate.AddDays(-(nn - 1));
        else if (spend.Count > 0)
            startDate = spend.Min(x => x.Day);
        else
            startDate = endDate;

        var spendMap = spend
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => g.First());

        var filledSpend = new List<DaySpend>();
        for (var cur = startDate; cur <= endDate; cur = cur.AddDays(1))
        {
            var key = cur.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            filledSpend.Add(spendMap.TryGetValue(key, out var ex)
                ? ex
                : new DaySpend(key, cur, 0.0));
        }
        s.DailySpend = filledSpend;

        return s;
    }

    // MARK: - Model display helpers

    public static string PrettyModel(string id)
    {
        var n = id.Replace("claude-", "Claude ").Replace("gpt-", "GPT-");
        n = DateSuffix().Replace(n, "");
        n = n.Replace("opus", "Opus").Replace("sonnet", "Sonnet").Replace("haiku", "Haiku");
        return n;
    }

    public static Color ModelColor(string id)
    {
        if (id.Contains("opus")) return LanguageMap.Hex(0xD97757);
        if (id.Contains("sonnet")) return LanguageMap.Hex(0xCC8B5C);
        if (id.Contains("haiku")) return LanguageMap.Hex(0xE0A971);
        if (id.Contains("gpt")) return LanguageMap.Hex(0x10A37F);
        if (id.Contains("gemini")) return LanguageMap.Hex(0x4285F4);
        return LanguageMap.Hex(0x8E8E93);
    }

    [GeneratedRegex(@"-\d{8}$")]
    private static partial Regex DateSuffix();

    // MARK: - INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
