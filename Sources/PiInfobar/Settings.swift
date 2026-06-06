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
            contentRect: NSRect(x: 0, y: 0, width: 580, height: 500),
            styleMask: [.titled, .closable, .fullSizeContentView],
            backing: .buffered, defer: false
        )
        win.title = "Pi Stats Settings"
        win.titlebarAppearsTransparent = false
        win.isReleasedWhenClosed = false
        win.center()
        win.delegate = self
        win.contentView = NSHostingView(rootView: SettingsView())
        win.minSize = NSSize(width: 540, height: 460)
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
    case menuBar, general, remote, about
    var id: String { rawValue }
    var title: String {
        switch self {
        case .menuBar: return "Menu Bar"
        case .general: return "General"
        case .remote:  return "Remote"
        case .about:   return "About"
        }
    }
    var icon: String {
        switch self {
        case .menuBar: return "menubar.rectangle"
        case .general: return "gearshape"
        case .remote:  return "server.rack"
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
            case .remote:  RemotePane()
            case .about:   AboutPane()
            }
        }
        .navigationTitle("Settings")
        .frame(minWidth: 540, minHeight: 460)
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
    @AppStorage(SettingsKeys.remoteSyncEnabled) private var remoteEnabled = false
    @AppStorage(SettingsKeys.remoteHost) private var remoteHost = ""

    private var version: String {
        let v = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "0.1.0"
        return "Version \(v)"
    }
    var body: some View {
        Form {
            Section {
                HStack(spacing: 14) {
                    PiLogoShape().fill(.primary, style: FillStyle(eoFill: true))
                        .frame(width: 44, height: 44)
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
                if remoteEnabled && !remoteHost.isEmpty {
                    LabeledContent("Source", value: "Remote: \(remoteHost)")
                } else {
                    LabeledContent("Source", value: "~/.pi/agent/sessions")
                }
                if remoteEnabled && !remoteHost.isEmpty {
                    LabeledContent("Privacy", value: "Syncs over SSH from your remote host")
                } else {
                    LabeledContent("Privacy", value: "100% local — nothing leaves your Mac")
                }
            }
        }
        .formStyle(.grouped)
        .scrollContentBackground(.hidden)
    }
}

// MARK: - Remote pane

struct RemotePane: View {
    @AppStorage(SettingsKeys.remoteSyncEnabled) private var remoteSyncEnabled = false
    @AppStorage(SettingsKeys.remoteHost) private var remoteHost = ""
    @AppStorage(SettingsKeys.remotePort) private var remotePort = "22"
    @AppStorage(SettingsKeys.remoteUser) private var remoteUser = ""
    @AppStorage(SettingsKeys.remoteKeyPath) private var remoteKeyPath = "~/.ssh/id_rsa"
    @AppStorage(SettingsKeys.remotePath) private var remotePath = "~/.pi/agent/sessions"

    @State private var testingConnection = false
    @State private var testResult: String? = nil
    @State private var testSuccess = false

    var body: some View {
        Form {
            Section("Remote Server Connection") {
                Toggle(isOn: $remoteSyncEnabled) {
                    VStack(alignment: .leading, spacing: 2) {
                        Text("Connect to Remote Server")
                        Text("Sync session log files from a remote host via SSH/rsync.")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
                .toggleStyle(.switch)
            }

            if remoteSyncEnabled {
                Section {
                    TextField("Host", text: $remoteHost,
                              prompt: Text("e.g. 192.168.1.100"))
                        .multilineTextAlignment(.trailing)

                    TextField("Port", text: $remotePort, prompt: Text("22"))
                        .multilineTextAlignment(.trailing)

                    TextField("Username", text: $remoteUser,
                              prompt: Text("e.g. ubuntu"))
                        .multilineTextAlignment(.trailing)

                    LabeledContent("SSH Key") {
                        HStack(spacing: 6) {
                            TextField("Key path", text: $remoteKeyPath,
                                      prompt: Text("~/.ssh/id_rsa"))
                                .multilineTextAlignment(.trailing)
                                .textFieldStyle(.plain)
                            Button("Choose…") { chooseSSHKey() }
                                .controlSize(.small)
                        }
                    }
                } header: {
                    Text("SSH Connection")
                } footer: {
                    Text("On first connection the host's key is trusted automatically (accept-new). Only connect to servers you control.")
                }

                Section {
                    TextField("Remote Path", text: $remotePath,
                              prompt: Text("~/.pi/agent/sessions"))
                        .multilineTextAlignment(.trailing)
                } header: {
                    Text("Remote Pi Directory")
                } footer: {
                    Text("The directory on the remote server where Pi session logs are saved.")
                }

                Section("Connection Status") {
                    VStack(alignment: .leading, spacing: 8) {
                        HStack(spacing: 12) {
                            Button("Test SSH Connection") {
                                testSSHConnection()
                            }
                            .disabled(testingConnection || remoteHost.isEmpty || remoteUser.isEmpty)
                            .controlSize(.small)

                            if testingConnection {
                                ProgressView()
                                    .controlSize(.small)
                            }
                        }

                        if let result = testResult {
                            HStack(spacing: 6) {
                                Image(systemName: testSuccess ? "checkmark.circle.fill" : "exclamationmark.circle.fill")
                                    .foregroundStyle(testSuccess ? .green : .red)
                                Text(result)
                                    .font(.caption)
                                    .foregroundStyle(.secondary)
                            }
                        }
                    }
                }
            }
        }
        .formStyle(.grouped)
        .scrollContentBackground(.hidden)
        .contentMargins(.top, 8, for: .scrollContent)
    }

    private func chooseSSHKey() {
        let panel = NSOpenPanel()
        panel.canChooseFiles = true
        panel.canChooseDirectories = false
        panel.allowsMultipleSelection = false
        
        let home = FileManager.default.homeDirectoryForCurrentUser
        let sshDir = home.appendingPathComponent(".ssh")
        if FileManager.default.fileExists(atPath: sshDir.path) {
            panel.directoryURL = sshDir
        } else {
            panel.directoryURL = home
        }
        
        panel.begin { response in
            if response == .OK, let url = panel.url {
                let path = url.path
                if path.hasPrefix(home.path) {
                    let relative = path.replacingOccurrences(of: home.path, with: "~")
                    remoteKeyPath = relative
                } else {
                    remoteKeyPath = path
                }
            }
        }
    }

    private func testSSHConnection() {
        testingConnection = true
        testResult = nil
        testSuccess = false

        Task {
            do {
                try await RemoteSync.testConnection(
                    host: remoteHost,
                    port: remotePort,
                    user: remoteUser,
                    keyPath: remoteKeyPath
                )
                await MainActor.run {
                    testingConnection = false
                    testSuccess = true
                    testResult = "Connection successful!"
                }
            } catch {
                await MainActor.run {
                    testingConnection = false
                    testSuccess = false
                    testResult = error.localizedDescription
                }
            }
        }
    }
}
