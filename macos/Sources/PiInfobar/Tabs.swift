import SwiftUI
import Charts

// MARK: - Overview

struct OverviewTab: View {
    let s: StatsSummary

    private let cols = [GridItem(.flexible(), spacing: 8), GridItem(.flexible(), spacing: 8)]

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            LazyVGrid(columns: [GridItem(.flexible(), spacing: 8),
                                GridItem(.flexible(), spacing: 8),
                                GridItem(.flexible(), spacing: 8)], spacing: 8) {
                StatCard(icon: "dollarsign.circle.fill", iconColor: .green,
                         title: "Total", value: Fmt.money(s.totalCost))
                StatCard(icon: "bubble.left.and.bubble.right.fill", iconColor: .blue,
                         title: "Sessions", value: Fmt.int(s.sessionCount))
                StatCard(icon: "ellipsis.message.fill", iconColor: .purple,
                         title: "Messages", value: Fmt.int(s.totalMessages))
                StatCard(icon: "calendar", iconColor: .orange,
                         title: "Active Days", value: Fmt.int(s.daysActive))
                StatCard(icon: "chart.line.uptrend.xyaxis", iconColor: .teal,
                         title: "Avg/Day", value: Fmt.money(s.avgCostPerDay))
                StatCard(icon: "clock.fill", iconColor: .red,
                         title: "Today", value: Fmt.money(s.todayCost))
            }

            if !s.dailySpend.isEmpty {
                VStack(alignment: .leading, spacing: 8) {
                    SectionHeader(title: "Daily Spend",
                                  trailing: "\(s.dailySpend.count) days")
                    DailySpendChart(data: s.dailySpend)
                        .frame(height: 150)
                }
            }

            if let top = s.languages.first {
                VStack(alignment: .leading, spacing: 8) {
                    SectionHeader(title: "Top Language")
                    BarRow(rank: 1, icon: top.symbol, color: top.color, title: top.name,
                           subtitle: "\(Fmt.int(top.edits)) edits",
                           value: "\(Fmt.int(top.lines)) ln", fraction: 1)
                }
            }
        }
    }
}

struct DailySpendChart: View {
    let data: [DaySpend]
    @State private var selectedDate: Date?

    /// The day the cursor is currently hovering over (matched by calendar day).
    private var selected: DaySpend? {
        guard let selectedDate else { return nil }
        let cal = Calendar.current
        return data.first { cal.isDate($0.day, inSameDayAs: selectedDate) }
    }

    var body: some View {
        Chart(data) { d in
            BarMark(
                x: .value("Day", d.day, unit: .day),
                y: .value("Cost", d.cost)
            )
            .foregroundStyle(LinearGradient(colors: [Color(hex: 0x6FCF73), Color(hex: 0x4CAF50)],
                                            startPoint: .top, endPoint: .bottom))
            .cornerRadius(2)
            .opacity(selected == nil || selected?.id == d.id ? 1 : 0.35)

            // Cursor rule + tooltip for the hovered day.
            if let sel = selected {
                RuleMark(x: .value("Day", sel.day, unit: .day))
                    .foregroundStyle(Color.primary.opacity(0.18))
                    .lineStyle(StrokeStyle(lineWidth: 1))
                    .annotation(position: .top, spacing: 4,
                                overflowResolution: .init(x: .fit(to: .chart), y: .disabled)) {
                        VStack(alignment: .leading, spacing: 1) {
                            Text(sel.day, format: .dateTime.weekday(.abbreviated).month().day())
                                .font(.system(size: 9, weight: .medium))
                                .foregroundStyle(.secondary)
                            Text(Fmt.money(sel.cost))
                                .font(.system(size: 12, weight: .semibold))
                                .foregroundStyle(Color(hex: 0x4CAF50))
                        }
                        .padding(.horizontal, 7)
                        .padding(.vertical, 4)
                        .background(.regularMaterial, in: RoundedRectangle(cornerRadius: 6))
                        .overlay(RoundedRectangle(cornerRadius: 6)
                            .strokeBorder(Color.primary.opacity(0.08)))
                    }
            }
        }
        .chartXSelection(value: $selectedDate)
        .chartYAxis {
            AxisMarks(position: .leading) { value in
                AxisGridLine().foregroundStyle(Color.primary.opacity(0.06))
                AxisValueLabel {
                    if let v = value.as(Double.self) {
                        Text(Fmt.moneyShort(v)).font(.system(size: 8.5))
                    }
                }
            }
        }
        .chartXAxis {
            AxisMarks(values: .automatic(desiredCount: 4)) { _ in
                AxisValueLabel(format: .dateTime.month().day(), centered: false)
                    .font(.system(size: 8.5))
            }
        }
    }
}

// MARK: - Languages (headline)

struct LanguagesTab: View {
    let s: StatsSummary
    var body: some View {
        if s.languages.isEmpty {
            CenteredMessage(icon: "chevron.left.forwardslash.chevron.right",
                            title: "No code edits yet",
                            subtitle: "Languages appear once you edit or write files in Pi.")
        } else {
            let maxLines = max(s.languages.map { $0.lines }.max() ?? 1, 1)
            let totalLines = max(s.languages.reduce(0) { $0 + $1.lines }, 1)
            VStack(alignment: .leading, spacing: 12) {
                LanguageDonut(langs: Array(s.languages.prefix(8)), totalLines: totalLines)
                    .frame(height: 150)
                SectionHeader(title: "Languages", trailing: "by lines written")
                VStack(spacing: 12) {
                    ForEach(Array(s.languages.prefix(12).enumerated()), id: \.element.id) { idx, l in
                        BarRow(rank: idx + 1, icon: l.symbol, color: l.color, title: l.name,
                               subtitle: "\(Fmt.int(l.edits)) edits · \(pct(l.lines, totalLines))",
                               value: "\(Fmt.int(l.lines)) ln",
                               fraction: Double(l.lines) / Double(maxLines))
                    }
                }
            }
        }
    }
    private func pct(_ a: Int, _ b: Int) -> String {
        String(format: "%.0f%%", Double(a) / Double(b) * 100)
    }
}

struct LanguageDonut: View {
    let langs: [LangStat]
    let totalLines: Int
    var body: some View {
        HStack(spacing: 16) {
            Chart(langs) { l in
                SectorMark(
                    angle: .value("Lines", l.lines),
                    innerRadius: .ratio(0.62),
                    angularInset: 1.5
                )
                .foregroundStyle(l.color)
                .cornerRadius(2)
            }
            .frame(width: 130, height: 130)
            .chartBackground { _ in
                VStack(spacing: 0) {
                    Text(Fmt.tokens(totalLines))
                        .font(.system(size: 16, weight: .bold, design: .rounded))
                    Text("lines").font(.system(size: 9)).foregroundStyle(.secondary)
                }
            }
            VStack(alignment: .leading, spacing: 5) {
                ForEach(langs.prefix(6)) { l in
                    HStack(spacing: 6) {
                        Circle().fill(l.color).frame(width: 7, height: 7)
                        Text(l.name).font(.system(size: 10.5, weight: .medium))
                        Spacer()
                        Text(String(format: "%.0f%%", Double(l.lines) / Double(totalLines) * 100))
                            .font(.system(size: 10, weight: .semibold, design: .rounded))
                            .foregroundStyle(.secondary)
                    }
                }
            }
        }
    }
}

// MARK: - Models

struct ModelsTab: View {
    let s: StatsSummary
    var body: some View {
        if s.models.isEmpty {
            CenteredMessage(icon: "cpu", title: "No model usage")
        } else {
            let maxCost = max(s.models.map { $0.cost }.max() ?? 1, 0.0001)
            VStack(alignment: .leading, spacing: 12) {
                SectionHeader(title: "Models", trailing: "by cost")
                VStack(spacing: 12) {
                    ForEach(Array(s.models.enumerated()), id: \.element.id) { idx, m in
                        BarRow(rank: idx + 1, icon: "cpu", color: m.color, title: m.displayName,
                               subtitle: "\(Fmt.int(m.count)) calls",
                               value: Fmt.money(m.cost),
                               fraction: m.cost / maxCost)
                    }
                }
            }
        }
    }
}

// MARK: - Projects

struct ProjectsTab: View {
    let s: StatsSummary
    var body: some View {
        if s.projects.isEmpty {
            CenteredMessage(icon: "folder", title: "No projects")
        } else {
            let maxCost = max(s.projects.map { $0.cost }.max() ?? 1, 0.0001)
            VStack(alignment: .leading, spacing: 12) {
                SectionHeader(title: "Projects", trailing: "by cost")
                VStack(spacing: 12) {
                    ForEach(Array(s.projects.prefix(15).enumerated()), id: \.element.id) { idx, p in
                        BarRow(rank: idx + 1, icon: "folder.fill",
                               color: projectColor(idx), title: p.name,
                               subtitle: "\(p.sessions) session\(p.sessions == 1 ? "" : "s")",
                               value: Fmt.money(p.cost),
                               fraction: p.cost / maxCost)
                    }
                }
            }
        }
    }
    private func projectColor(_ i: Int) -> Color {
        let palette: [Color] = [.blue, .purple, .pink, .orange, .teal, .indigo, .green, .red, .cyan, .mint]
        return palette[i % palette.count]
    }
}

// MARK: - Usage (tokens + tools)

struct UsageTab: View {
    let s: StatsSummary
    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            SectionHeader(title: "Tokens", trailing: Fmt.tokens(s.totalTokens))
            VStack(spacing: 9) {
                tokenRow("Input", s.inTok, .blue)
                tokenRow("Output", s.outTok, .green)
                tokenRow("Cache Read", s.crTok, .orange)
                tokenRow("Cache Write", s.cwTok, .purple)
            }

            if !s.tools.isEmpty {
                let maxC = max(s.tools.map { $0.count }.max() ?? 1, 1)
                SectionHeader(title: "Tool Calls", trailing: Fmt.int(s.tools.reduce(0) { $0 + $1.count }))
                VStack(spacing: 12) {
                    ForEach(Array(s.tools.enumerated()), id: \.element.id) { idx, t in
                        BarRow(rank: idx + 1, icon: toolIcon(t.name),
                               color: .secondary, title: t.name,
                               subtitle: "",
                               value: Fmt.int(t.count),
                               fraction: Double(t.count) / Double(maxC))
                    }
                }
            }
        }
    }

    private func tokenRow(_ name: String, _ value: Int, _ color: Color) -> some View {
        let total = max(s.totalTokens, 1)
        return VStack(alignment: .leading, spacing: 4) {
            HStack {
                Circle().fill(color).frame(width: 7, height: 7)
                Text(name).font(.system(size: 11.5, weight: .medium))
                Spacer()
                Text(Fmt.tokens(value))
                    .font(.system(size: 11.5, weight: .bold, design: .rounded))
            }
            GeometryReader { geo in
                ZStack(alignment: .leading) {
                    Capsule().fill(Color.primary.opacity(0.06)).frame(height: 5)
                    Capsule().fill(color)
                        .frame(width: max(4, geo.size.width * Double(value) / Double(total)), height: 5)
                }
            }.frame(height: 5)
        }
    }

    private func toolIcon(_ name: String) -> String {
        switch name {
        case "bash": return "terminal"
        case "read": return "doc.text"
        case "edit": return "pencil"
        case "write": return "square.and.pencil"
        case "mcp": return "puzzlepiece.extension"
        default: return "wrench.and.screwdriver"
        }
    }
}
