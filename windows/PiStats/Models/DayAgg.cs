using System.Text.Json.Serialization;

namespace PiStats.Models;

/// <summary>
/// Per-day aggregate — the cached unit. Port of the Swift DayAgg.
/// JSON keys are kept identical to the macOS app's cache for parity.
/// </summary>
public sealed class DayAgg
{
    [JsonPropertyName("date")] public string Date { get; set; } = "";  // yyyy-MM-dd
    [JsonPropertyName("cost")] public double Cost { get; set; }
    [JsonPropertyName("inTok")] public int InTok { get; set; }
    [JsonPropertyName("outTok")] public int OutTok { get; set; }
    [JsonPropertyName("crTok")] public int CrTok { get; set; }   // cache read
    [JsonPropertyName("cwTok")] public int CwTok { get; set; }   // cache write
    [JsonPropertyName("userMsgs")] public int UserMsgs { get; set; }
    [JsonPropertyName("asstMsgs")] public int AsstMsgs { get; set; }
    [JsonPropertyName("toolResults")] public int ToolResults { get; set; }

    [JsonPropertyName("sessionIds")] public List<string> SessionIds { get; set; } = new();
    [JsonPropertyName("langLines")] public Dictionary<string, int> LangLines { get; set; } = new();
    [JsonPropertyName("langEdits")] public Dictionary<string, int> LangEdits { get; set; } = new();
    [JsonPropertyName("modelCost")] public Dictionary<string, double> ModelCost { get; set; } = new();
    [JsonPropertyName("modelCount")] public Dictionary<string, int> ModelCount { get; set; } = new();
    [JsonPropertyName("projectCost")] public Dictionary<string, double> ProjectCost { get; set; } = new();
    [JsonPropertyName("projectSessions")] public Dictionary<string, List<string>> ProjectSessions { get; set; } = new();
    [JsonPropertyName("toolCount")] public Dictionary<string, int> ToolCount { get; set; } = new();

    public DayAgg() { }
    public DayAgg(string date) { Date = date; }
}

/// <summary>Full aggregate cached to disk. Port of the Swift Aggregate.</summary>
public sealed class Aggregate
{
    [JsonPropertyName("signature")] public string Signature { get; set; } = "";
    [JsonPropertyName("days")] public List<DayAgg> Days { get; set; } = new();
    [JsonPropertyName("generatedAt")] public DateTime GeneratedAt { get; set; }
}
