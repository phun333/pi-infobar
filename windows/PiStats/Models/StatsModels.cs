using System.Windows.Media;

namespace PiStats.Models;

// MARK: - Derived view-model rows (ported from Swift)

public sealed record LangStat(string Name, int Lines, int Edits, Color Color, string Symbol);

public sealed record ModelStat(string Name, string DisplayName, double Cost, int Count, Color Color);

public sealed record ProjectStat(string Name, double Cost, int Sessions);

public sealed record ToolStat(string Name, int Count);

public sealed record DaySpend(string Date, DateTime Day, double Cost);

// MARK: - Summary for a selected range

public sealed class StatsSummary
{
    public double TotalCost { get; set; }
    public int InTok { get; set; }
    public int OutTok { get; set; }
    public int CrTok { get; set; }
    public int CwTok { get; set; }
    public int SessionCount { get; set; }
    public int UserMsgs { get; set; }
    public int AsstMsgs { get; set; }
    public int ToolResults { get; set; }
    public int DaysActive { get; set; }
    public double TodayCost { get; set; }
    public double AvgCostPerDay { get; set; }

    public List<LangStat> Languages { get; set; } = new();
    public List<ModelStat> Models { get; set; } = new();
    public List<ProjectStat> Projects { get; set; } = new();
    public List<ToolStat> Tools { get; set; } = new();
    public List<DaySpend> DailySpend { get; set; } = new();

    public int TotalTokens => InTok + OutTok + CrTok + CwTok;
    public int TotalMessages => UserMsgs + AsstMsgs;
}
