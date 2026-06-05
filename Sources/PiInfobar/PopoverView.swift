import SwiftUI
import Charts

enum Tab: String, CaseIterable, Identifiable {
    case overview = "Overview"
    case languages = "Languages"
    case models = "Models"
    case projects = "Projects"
    case usage = "Usage"
    var id: String { rawValue }
}

struct PopoverView: View {
    @ObservedObject var engine: StatsEngine
    let onQuit: () -> Void
    let onRefresh: () -> Void

    @State private var tab: Tab = .overview
    @State private var range: TimeRange = .all

    private var summary: StatsSummary { engine.summary(for: range) }

    var body: some View {
        VStack(spacing: 0) {
            header
            Divider().opacity(0.5)
            tabBar
            Divider().opacity(0.5)

            if engine.loading && engine.lastUpdated == nil {
                loadingView
            } else if engine.error != nil {
                CenteredMessage(icon: "exclamationmark.triangle",
                                title: "Couldn't read Pi data",
                                subtitle: engine.error)
            } else {
                ScrollView {
                    content
                        .padding(14)
                }
            }

            Divider().opacity(0.5)
            footer
        }
        .frame(width: 380, height: 560)
    }

    // MARK: Header

    private var header: some View {
        HStack(spacing: 9) {
            ZStack {
                RoundedRectangle(cornerRadius: 8, style: .continuous)
                    .fill(LinearGradient(colors: [Color(hex: 0x7C5CFF), Color(hex: 0x4D7CFF)],
                                         startPoint: .topLeading, endPoint: .bottomTrailing))
                    .frame(width: 26, height: 26)
                PiLogoShape()
                    .fill(.white, style: FillStyle(eoFill: true))
                    .frame(width: 15, height: 15)
            }
            VStack(alignment: .leading, spacing: 0) {
                Text("Pi Stats")
                    .font(.system(size: 14, weight: .bold))
                if engine.loading {
                    Text("Updating…").font(.system(size: 10)).foregroundStyle(.secondary)
                } else if let d = engine.lastUpdated {
                    Text("Updated \(d.formatted(date: .omitted, time: .shortened))")
                        .font(.system(size: 10)).foregroundStyle(.secondary)
                }
            }
            Spacer()
            rangePicker
            Button(action: onRefresh) {
                Image(systemName: "arrow.clockwise")
                    .font(.system(size: 12, weight: .semibold))
                    .rotationEffect(.degrees(engine.loading ? 360 : 0))
                    .animation(engine.loading ? .linear(duration: 1).repeatForever(autoreverses: false) : .default,
                               value: engine.loading)
            }
            .buttonStyle(.plain)
            .foregroundStyle(.secondary)
            .help("Rescan sessions")
        }
        .padding(.horizontal, 14)
        .padding(.vertical, 11)
    }

    private var rangePicker: some View {
        HStack(spacing: 1) {
            ForEach(TimeRange.allCases) { r in
                Button(action: { withAnimation(.easeOut(duration: 0.15)) { range = r } }) {
                    Text(r.label)
                        .font(.system(size: 10.5, weight: .semibold))
                        .padding(.horizontal, 7).padding(.vertical, 3)
                        .foregroundStyle(range == r ? Color.white : Color.secondary)
                        .background(
                            RoundedRectangle(cornerRadius: 5, style: .continuous)
                                .fill(range == r ? Color.accentColor : Color.clear)
                        )
                }
                .buttonStyle(.plain)
            }
        }
        .padding(2)
        .background(RoundedRectangle(cornerRadius: 7, style: .continuous)
            .fill(Color.primary.opacity(0.06)))
    }

    // MARK: Tabs

    private var tabBar: some View {
        HStack(spacing: 2) {
            ForEach(Tab.allCases) { t in
                Button(action: { tab = t }) {
                    Text(t.rawValue)
                        .font(.system(size: 11.5, weight: tab == t ? .bold : .medium))
                        .foregroundStyle(tab == t ? Color.accentColor : Color.secondary)
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 8)
                        .background(alignment: .bottom) {
                            Rectangle()
                                .fill(tab == t ? Color.accentColor : Color.clear)
                                .frame(height: 2)
                        }
                }
                .buttonStyle(.plain)
            }
        }
        .padding(.horizontal, 8)
    }

    // MARK: Content

    @ViewBuilder
    private var content: some View {
        switch tab {
        case .overview:  OverviewTab(s: summary)
        case .languages: LanguagesTab(s: summary)
        case .models:    ModelsTab(s: summary)
        case .projects:  ProjectsTab(s: summary)
        case .usage:     UsageTab(s: summary)
        }
    }

    private var loadingView: some View {
        VStack(spacing: 14) {
            ProgressView(value: engine.progress)
                .frame(width: 180)
            Text("Reading Pi sessions… \(Int(engine.progress * 100))%")
                .font(.system(size: 11)).foregroundStyle(.secondary)
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
    }

    // MARK: Footer

    private var footer: some View {
        VStack(spacing: 0) {
            HStack {
                Text("\(summary.daysActive) active days · \(Fmt.int(summary.totalMessages)) messages")
                    .font(.system(size: 10.5))
                    .foregroundStyle(.secondary)
                Spacer()
            }
            .padding(.horizontal, 14)
            .padding(.vertical, 7)

            Divider().opacity(0.5)

            MenuRow(title: "Quit", systemImage: "power", shortcut: "⌘Q", action: onQuit)
                .padding(4)
        }
    }
}
