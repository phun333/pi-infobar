# Pi Stats — Windows

Native system-tray dashboard for Pi agent usage (.NET 8 / WPF).
A faithful port of the macOS app: same data, same design language.

## Status

Work in progress — see the port roadmap below.

- [x] **1. Scaffold** — tray icon (π), translucent rounded popover, build pipeline
- [x] **2. Models** — DayAgg, Aggregate, StatsSummary, TimeRange
- [x] **3. Core** — Parser + StatsEngine + LanguageMap (verified vs real logs)
- [x] **4. Wiring** — engine load, periodic refresh, tray tooltip, live popover cards
- [x] **5. UI tabs** — Overview/Languages/Models/Projects/Usage, range picker,
      daily-spend bars, language donut, bar rows, token bars (pure WPF)
- [x] **6. Settings** — settings window (Menu Bar/General/Remote/About),
      JSON-backed SettingsStore, launch-at-login via HKCU Run key,
      gear button + tray menu, metric-driven tray tooltip
- [x] **7. Remote sync** — SSH pull of *.jsonl over ssh.exe + tar.exe (no
      rsync needed), Local/Remote switch in popover, Test Connection +
      key picker in settings, separate remote cache
- [x] **8. Packaging** — embedded app icon (π squircle), single-file
      self-contained .exe via `build.ps1`

**The Windows port is feature-complete** — a faithful reproduction of the
macOS app.

## Remote sync requirements

Uses the built-in Windows **OpenSSH client** (`ssh.exe`) and **`tar.exe`**
(both ship with Windows 10/11). If `ssh` is missing, install it via
*Settings → Apps → Optional features → OpenSSH Client*.

## Dev self-tests

```powershell
dotnet run -- --dump   # parse real logs, write report to %TEMP%\pistats-dump.txt
dotnet run -- --shot   # render each tab to %TEMP%\pistats-<tab>.png
```

## Data source

Reads `%USERPROFILE%\.pi\agent\sessions\**\*.jsonl` — identical format to macOS.

## Run (dev)

Requires the .NET 8 SDK:

```powershell
cd windows/PiStats
dotnet run        # launches into the system tray (π icon)
```

Left-click the tray icon to toggle the popover; right-click for the menu.

## Build a distributable .exe

```powershell
cd windows
.\build.ps1                    # self-contained single .exe (no .NET needed)
.\build.ps1 -FrameworkDependent  # small .exe, needs .NET 8 Desktop Runtime
```

The result is `windows\dist\PiStats.exe` — a single portable file you can
double-click or drop in your Startup folder.

See the [root README](../README.md) for the overall project.
