import SwiftUI
import AppKit

@main
struct PiInfobarApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var appDelegate

    var body: some Scene {
        Settings { EmptyView() } // no standard window; everything lives in the menu bar
    }
}

@MainActor
final class AppDelegate: NSObject, NSApplicationDelegate {
    private var statusItem: NSStatusItem!
    private var popover: NSPopover!
    private let engine = StatsEngine()
    private var refreshTimer: Timer?
    private var titleObserver: AnyObject?

    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.setActivationPolicy(.accessory)

        // Status bar item.
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        if let button = statusItem.button {
            button.action = #selector(togglePopover(_:))
            button.target = self
            updateTitle(cost: 0, loading: true)
        }

        // Popover.
        popover = NSPopover()
        popover.contentSize = NSSize(width: 380, height: 560)
        popover.behavior = .transient
        popover.animates = true
        let root = PopoverView(engine: engine, onQuit: { NSApp.terminate(nil) },
                               onRefresh: { [weak self] in self?.engine.load(force: true) })
        popover.contentViewController = NSHostingController(rootView: root)

        // Load data and keep the menu bar title in sync.
        engine.load()
        observeEngine()

        // Periodic refresh (every 5 minutes) to pick up new sessions.
        refreshTimer = Timer.scheduledTimer(withTimeInterval: 300, repeats: true) { [weak self] _ in
            self?.engine.load()
        }
    }

    private func observeEngine() {
        // Poll the engine briefly to update the title (simple + robust).
        Timer.scheduledTimer(withTimeInterval: 1.0, repeats: true) { [weak self] _ in
            guard let self else { return }
            Task { @MainActor in
                self.updateTitle(cost: self.engine.todayCost, loading: self.engine.loading)
            }
        }
    }

    @MainActor
    private func updateTitle(cost: Double, loading: Bool) {
        guard let button = statusItem.button else { return }
        button.image = PiLogoShape.menuBarImage(size: 14)
        button.imagePosition = .imageLeading
        if loading && cost == 0 {
            button.title = " …"
        } else {
            button.title = String(format: " $%.2f", cost)
        }
        button.font = .monospacedDigitSystemFont(ofSize: 12, weight: .semibold)
    }

    @objc private func togglePopover(_ sender: AnyObject?) {
        guard let button = statusItem.button else { return }
        if popover.isShown {
            popover.performClose(sender)
        } else {
            NSApp.activate(ignoringOtherApps: true)
            popover.show(relativeTo: button.bounds, of: button, preferredEdge: .minY)
            popover.contentViewController?.view.window?.makeKey()
        }
    }
}
