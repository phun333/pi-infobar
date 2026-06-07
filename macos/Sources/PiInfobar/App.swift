import SwiftUI
import AppKit

@main
struct PiInfobarApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var appDelegate

    var body: some Scene {
        Settings { EmptyView() } // no standard window; everything lives in the menu bar
    }
}

/// Borderless panel that can take key focus (for ⌘Q / Esc) and shows a
/// translucent, rounded dropdown — no popover triangle.
final class StatsPanel: NSPanel {
    override var canBecomeKey: Bool { true }
    override var canBecomeMain: Bool { false }
}

@MainActor
final class AppDelegate: NSObject, NSApplicationDelegate {
    private var statusItem: NSStatusItem!
    private var panel: StatsPanel!
    private let engine = StatsEngine()
    private var refreshTimer: Timer?
    private var keyMonitor: Any?

    private let panelSize = NSSize(width: 380, height: 560)

    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.setActivationPolicy(.accessory)

        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        if let button = statusItem.button {
            button.action = #selector(togglePanel(_:))
            button.target = self
            updateTitle(cost: 0, loading: true)
        }

        buildPanel()

        engine.load()
        startTitleSync()

        refreshTimer = Timer.scheduledTimer(withTimeInterval: 300, repeats: true) { [weak self] _ in
            Task { @MainActor in self?.engine.load() }
        }
    }

    // MARK: Panel construction

    private func buildPanel() {
        panel = StatsPanel(
            contentRect: NSRect(origin: .zero, size: panelSize),
            styleMask: [.borderless],
            backing: .buffered,
            defer: false
        )
        panel.isOpaque = false
        panel.backgroundColor = .clear
        panel.hasShadow = true
        panel.level = .popUpMenu
        panel.isMovable = false
        panel.hidesOnDeactivate = false
        panel.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .transient]
        panel.animationBehavior = .utilityWindow

        // Translucent rounded container.
        let blur = NSVisualEffectView(frame: NSRect(origin: .zero, size: panelSize))
        blur.material = .menu
        blur.blendingMode = .behindWindow
        blur.state = .active
        blur.wantsLayer = true
        blur.layer?.cornerRadius = 14
        blur.layer?.cornerCurve = .continuous
        blur.layer?.masksToBounds = true
        blur.layer?.borderWidth = 1
        blur.layer?.borderColor = NSColor.white.withAlphaComponent(0.08).cgColor

        let root = PopoverView(engine: engine,
                               onQuit: { NSApp.terminate(nil) },
                               onRefresh: { [weak self] in self?.engine.load(force: true) },
                               onSettings: { [weak self] in
                                   self?.hidePanel()
                                   SettingsWindowController.show()
                               })
        let hosting = NSHostingView(rootView: root)
        hosting.frame = blur.bounds
        hosting.autoresizingMask = [.width, .height]
        blur.addSubview(hosting)

        panel.contentView = blur
    }

    // MARK: Show / hide

    @objc private func togglePanel(_ sender: AnyObject?) {
        if panel.isVisible { hidePanel() } else { showPanel() }
    }

    private func showPanel() {
        guard let button = statusItem.button, let win = button.window else { return }
        let rectInWindow = button.convert(button.bounds, to: nil)
        let screenRect = win.convertToScreen(rectInWindow)

        let gap: CGFloat = 6
        var origin = NSPoint(x: screenRect.maxX - panelSize.width,
                             y: screenRect.minY - panelSize.height - gap)
        if let screen = win.screen ?? NSScreen.main {
            let vf = screen.visibleFrame
            origin.x = min(max(origin.x, vf.minX + 8), vf.maxX - panelSize.width - 8)
        }

        panel.setContentSize(panelSize)
        panel.setFrameOrigin(origin)
        NSApp.activate(ignoringOtherApps: true)
        panel.makeKeyAndOrderFront(nil)

        // Key handling (⌘Q, Esc) while the panel is up.
        keyMonitor = NSEvent.addLocalMonitorForEvents(matching: .keyDown) { [weak self] event in
            guard let self else { return event }
            if event.modifierFlags.contains(.command),
               event.charactersIgnoringModifiers?.lowercased() == "q" {
                NSApp.terminate(nil); return nil
            }
            if event.keyCode == 53 { self.hidePanel(); return nil } // Esc
            return event
        }

        NotificationCenter.default.addObserver(
            self, selector: #selector(panelResignedKey),
            name: NSWindow.didResignKeyNotification, object: panel)
    }

    @objc private func panelResignedKey() { hidePanel() }

    private func hidePanel() {
        if let m = keyMonitor { NSEvent.removeMonitor(m); keyMonitor = nil }
        NotificationCenter.default.removeObserver(
            self, name: NSWindow.didResignKeyNotification, object: panel)
        panel.orderOut(nil)
    }

    // MARK: Menu bar title

    private func startTitleSync() {
        Timer.scheduledTimer(withTimeInterval: 1.0, repeats: true) { [weak self] _ in
            Task { @MainActor in
                guard let self else { return }
                self.updateTitle(cost: self.engine.todayCost, loading: self.engine.loading)
            }
        }
    }

    @MainActor
    private func updateTitle(cost: Double, loading: Bool) {
        guard let button = statusItem.button else { return }
        button.font = .monospacedDigitSystemFont(ofSize: 12, weight: .semibold)

        var showIcon = SettingsStore.showMenuBarIcon
        let metric = SettingsStore.menuBarMetric

        let text: String
        if loading && engine.lastUpdated == nil {
            text = "…"
        } else {
            text = metricText(metric)
        }
        // Never end up with a completely empty status item.
        if !showIcon && text.isEmpty { showIcon = true }

        button.image = showIcon ? PiLogoShape.menuBarImage(size: 14) : nil
        button.imagePosition = showIcon ? .imageLeading : .noImage
        button.title = text.isEmpty ? "" : (showIcon ? " \(text)" : text)
    }

    @MainActor
    private func metricText(_ metric: MenuBarMetric) -> String {
        switch metric {
        case .todayCost:     return String(format: "$%.2f", engine.todayCost)
        case .totalCost:     return Fmt.money(engine.totalCostAll)
        case .todayLines:    return "\(Fmt.int(engine.todayLines)) ln"
        case .todayMessages: return "\(Fmt.int(engine.todayMessages)) msg"
        case .todaySessions: return "\(engine.todaySessions) sess"
        case .none:          return ""
        }
    }
}
