import SwiftUI
import AppKit

// MARK: - Activation policy (menu-bar-only app needs Dock presence while a window is open)

@MainActor
enum AppActivationPolicy {
    private static var count = 0
    static func enter() {
        count += 1
        NSApp.setActivationPolicy(.regular)
        NSApp.activate(ignoringOtherApps: true)
    }
    static func leave() {
        count = max(0, count - 1)
        guard count == 0 else { return }
        Task { @MainActor in NSApp.setActivationPolicy(.accessory) }
    }
}

// MARK: - Window controller

@MainActor
final class SettingsWindowController: NSObject, NSWindowDelegate {
    static let shared = SettingsWindowController()
    private var window: NSWindow?

    static func show() { shared.present() }

    private func present() {
        if let window {
            AppActivationPolicy.enter()
            window.makeKeyAndOrderFront(nil)
            return
        }
        let win = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 560, height: 460),
            styleMask: [.titled, .closable, .fullSizeContentView],
            backing: .buffered, defer: false
        )
        win.title = "Pi Stats Settings"
        win.titlebarAppearsTransparent = false
        win.isReleasedWhenClosed = false
        win.center()
        win.delegate = self
        win.contentView = NSHostingView(rootView: SettingsView())
        win.minSize = NSSize(width: 520, height: 420)
        window = win

        AppActivationPolicy.enter()
        win.makeKeyAndOrderFront(nil)
    }

    func windowWillClose(_ notification: Notification) {
        AppActivationPolicy.leave()
    }
}

// MARK: - Settings root

enum SettingsTab: String, CaseIterable, Identifiable {
    case menuBar, general, about
    var id: String { rawValue }
    var title: String {
        switch self {
        case .menuBar: return "Menu Bar"
        case .general: return "General"
        case .about:   return "About"
        }
    }
    var icon: String {
        switch self {
        case .menuBar: return "menubar.rectangle"
        case .general: return "gearshape"
        case .about:   return "info.circle"
        }
    }
}

struct SettingsView: View {
    @State private var selection: SettingsTab = .menuBar

    var body: some View {
        NavigationSplitView {
            List(SettingsTab.allCases, selection: $selection) { tab in
                Label(tab.title, systemImage: tab.icon).tag(tab)
            }
            .listStyle(.sidebar)
            .navigationSplitViewColumnWidth(min: 160, ideal: 170, max: 200)
        } detail: {
            switch selection {
            case .menuBar: MenuBarPane()
            case .general: GeneralPane()
            case .about:   AboutPane()
            }
        }
        .navigationTitle("Settings")
        .frame(minWidth: 520, minHeight: 420)
    }
}

// MARK: - Menu Bar pane

struct MenuBarPane: View {
    @AppStorage(SettingsKeys.showMenuBarIcon) private var showIcon = true
    @AppStorage(SettingsKeys.menuBarMetric) private var metricRaw = MenuBarMetric.todayCost.rawValue

    var body: some View {
        Form {
            Section("Menu Bar") {
                Toggle(isOn: $showIcon) {
                    VStack(alignment: .leading, spacing: 2) {
                        Text("Show Pi icon")
                        Text("Hide it to show only the number, or keep it as a compact mark.")
                            .font(.caption).foregroundStyle(.secondary)
                    }
                }
                .toggleStyle(.switch)

                Picker(selection: $metricRaw) {
                    ForEach(MenuBarMetric.allCases) { m in
                        Text(m.label).tag(m.rawValue)
                    }
                } label: {
                    VStack(alignment: .leading, spacing: 2) {
                        Text("Show in menu bar")
                        Text("Pick what the number next to the icon means.")
                            .font(.caption).foregroundStyle(.secondary)
                    }
                }
                .pickerStyle(.menu)
            }

            Section {
                LabeledContent("Preview") {
                    HStack(spacing: 5) {
                        if showIcon {
                            PiLogoShape().fill(.primary, style: FillStyle(eoFill: true))
                                .frame(width: 13, height: 13)
                        }
                        Text(previewText)
                            .font(.system(size: 12, weight: .semibold, design: .rounded))
                            .monospacedDigit()
                    }
                    .padding(.horizontal, 8).padding(.vertical, 3)
                    .background(RoundedRectangle(cornerRadius: 6).fill(Color.primary.opacity(0.06)))
                }
            }
        }
        .formStyle(.grouped)
        .scrollContentBackground(.hidden)
    }

    private var previewText: String {
        switch MenuBarMetric(rawValue: metricRaw) ?? .todayCost {
        case .todayCost:     return "$9.75"
        case .totalCost:     return "$2,060"
        case .todayLines:    return "1,240 ln"
        case .todayMessages: return "318 msg"
        case .todaySessions: return "4 sess"
        case .none:          return showIcon ? "" : "$9.75"
        }
    }
}

// MARK: - General pane

struct GeneralPane: View {
    @AppStorage(SettingsKeys.defaultRange) private var rangeRaw = TimeRange.all.rawValue
    @AppStorage(SettingsKeys.defaultTab) private var tabRaw = Tab.overview.rawValue
    @State private var launchAtLogin = LaunchAtLogin.isEnabled

    var body: some View {
        Form {
            Section("Startup") {
                Toggle(isOn: $launchAtLogin) {
                    VStack(alignment: .leading, spacing: 2) {
                        Text("Launch at login")
                        Text("Start Pi Stats automatically when you log in.")
                            .font(.caption).foregroundStyle(.secondary)
                    }
                }
                .toggleStyle(.switch)
                .onChange(of: launchAtLogin) { _, newValue in
                    LaunchAtLogin.set(newValue)
                    launchAtLogin = LaunchAtLogin.isEnabled
                }
            }

            Section("Default View") {
                Picker("Open on tab", selection: $tabRaw) {
                    ForEach(Tab.allCases) { t in Text(t.rawValue).tag(t.rawValue) }
                }
                Picker("Time range", selection: $rangeRaw) {
                    ForEach(TimeRange.allCases) { r in Text(r.label).tag(r.rawValue) }
                }
            }
        }
        .formStyle(.grouped)
        .scrollContentBackground(.hidden)
    }
}

// MARK: - About pane

struct AboutPane: View {
    private var version: String {
        let v = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "0.1.0"
        return "Version \(v)"
    }
    var body: some View {
        Form {
            Section {
                HStack(spacing: 14) {
                    ZStack {
                        RoundedRectangle(cornerRadius: 14, style: .continuous)
                            .fill(LinearGradient(colors: [Color(hex: 0x7C5CFF), Color(hex: 0x4D7CFF)],
                                                 startPoint: .topLeading, endPoint: .bottomTrailing))
                            .frame(width: 56, height: 56)
                        PiLogoShape().fill(.white, style: FillStyle(eoFill: true))
                            .frame(width: 30, height: 30)
                    }
                    VStack(alignment: .leading, spacing: 3) {
                        Text("Pi Stats").font(.system(size: 16, weight: .bold))
                        Text(version).font(.caption).foregroundStyle(.secondary)
                        Text("Local usage dashboard for the Pi agent.")
                            .font(.caption).foregroundStyle(.secondary)
                    }
                    Spacer()
                }
            }
            Section("Data") {
                LabeledContent("Source", value: "~/.pi/agent/sessions")
                LabeledContent("Privacy", value: "100% local — nothing leaves your Mac")
            }
        }
        .formStyle(.grouped)
        .scrollContentBackground(.hidden)
    }
}
