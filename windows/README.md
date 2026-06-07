# Pi Stats — Windows

Native system-tray dashboard for Pi agent usage (.NET 8 / WPF).
A faithful port of the macOS app: same data, same design language.

## Status

Work in progress — see the port roadmap below.

- [x] **1. Scaffold** — tray icon (π), translucent rounded popover, build pipeline
- [x] **2. Models** — DayAgg, Aggregate, StatsSummary, TimeRange
- [x] **3. Core** — Parser + StatsEngine + LanguageMap (verified vs real logs)
- [x] **4. Wiring** — engine load, periodic refresh, tray tooltip, live popover cards
- [ ] 5. UI tabs (Overview, Languages, Models, Projects, Usage) + charts
- [ ] 6. Settings window + launch-at-login
- [ ] 7. Remote sync (SSH)
- [ ] 8. Packaging (.exe / MSIX)

## Data source

Reads `%USERPROFILE%\.pi\agent\sessions\**\*.jsonl` — identical format to macOS.

## Build & run

Requires the .NET 8 SDK:

```powershell
cd windows/PiStats
dotnet run        # launches into the system tray (π icon)
```

Left-click the tray icon to toggle the popover; right-click for the menu.

See the [root README](../README.md) for the overall project.
