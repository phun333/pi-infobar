import Foundation
import SwiftUI
import ServiceManagement

/// What to display next to (or instead of) the logo in the menu bar.
enum MenuBarMetric: String, CaseIterable, Identifiable {
    case todayCost
    case totalCost
    case todayLines
    case todayMessages
    case todaySessions
    case none

    var id: String { rawValue }

    var label: String {
        switch self {
        case .todayCost:     return "Today's cost"
        case .totalCost:     return "Total cost"
        case .todayLines:    return "Lines today"
        case .todayMessages: return "Messages today"
        case .todaySessions: return "Sessions today"
        case .none:          return "Nothing (icon only)"
        }
    }
}

/// Centralised UserDefaults keys so the SwiftUI views (@AppStorage) and the
/// AppDelegate read/write the exact same settings.
enum SettingsKeys {
    static let showMenuBarIcon = "showMenuBarIcon"
    static let menuBarMetric   = "menuBarMetric"
    static let defaultRange    = "defaultRange"
    static let defaultTab      = "defaultTab"
    static let launchAtLogin   = "launchAtLogin"
    static let remoteSyncEnabled = "remoteSyncEnabled"
    static let remoteHost        = "remoteHost"
    static let remotePort        = "remotePort"
    static let remoteUser        = "remoteUser"
    static let remoteKeyPath     = "remoteKeyPath"
    static let remotePath        = "remotePath"
}

enum SettingsStore {
    static var showMenuBarIcon: Bool {
        UserDefaults.standard.object(forKey: SettingsKeys.showMenuBarIcon) as? Bool ?? true
    }
    static var menuBarMetric: MenuBarMetric {
        MenuBarMetric(rawValue: UserDefaults.standard.string(forKey: SettingsKeys.menuBarMetric) ?? "")
            ?? .todayCost
    }
    static var defaultRange: TimeRange {
        TimeRange(rawValue: UserDefaults.standard.string(forKey: SettingsKeys.defaultRange) ?? "")
            ?? .all
    }
    static var defaultTab: Tab {
        Tab(rawValue: UserDefaults.standard.string(forKey: SettingsKeys.defaultTab) ?? "")
            ?? .overview
    }
}

/// Launch-at-login via SMAppService (macOS 13+).
@MainActor
enum LaunchAtLogin {
    static var isEnabled: Bool {
        SMAppService.mainApp.status == .enabled
    }

    static func set(_ enabled: Bool) {
        do {
            if enabled {
                if SMAppService.mainApp.status != .enabled {
                    try SMAppService.mainApp.register()
                }
            } else {
                if SMAppService.mainApp.status == .enabled {
                    try SMAppService.mainApp.unregister()
                }
            }
        } catch {
            NSLog("LaunchAtLogin error: \(error.localizedDescription)")
        }
    }
}
