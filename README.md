# Pi Stats — menu bar usage dashboard

A native macOS menu-bar app that reads your local **Pi** agent sessions
(`~/.pi/agent/sessions/**/*.jsonl`) and shows a rich usage dashboard — inspired by
"OpenCode Stats", but for Pi and with **per-language coding stats**.

Everything is computed locally. Nothing leaves your machine.

## Install (for users)

1. Download **Pi-Stats.dmg** from the [Releases](https://github.com/phun333/pi-infobar/releases) page.
2. Open it and drag **Pi Stats** into **Applications**.
3. First launch only: right-click **Pi Stats** → **Open** (the build is unsigned, so
   macOS shows a one-time Gatekeeper prompt). After that it opens normally.
4. The **π** mark appears in your menu bar. Click it for the dashboard, or open
   **Settings** (gear icon) to tweak what shows.

To start it automatically: **Settings → General → Launch at login**.

## What it shows

The menu bar item displays **today's spend** (e.g. `📊 $9.75`). Click it for a popover with tabs:

- **Overview** — Total cost, sessions, messages, active days, avg/day, today + a daily-spend bar chart.
- **Languages** — *Which languages you code in most*, by lines written, with a donut + ranked bars
  (TypeScript, JavaScript, Python, Swift, Go, …). Detected from `edit`/`write` tool calls.
- **Models** — Cost & call count per model (Claude Opus, GPT, …).
- **Projects** — Cost & session count per project (from each session's `cwd`).
- **Usage** — Token breakdown (input / output / cache read / cache write) and tool-call counts.

A time-range switch (**1d / 7d / 30d / All**) filters every tab.

## Settings

Open with the gear icon in the dashboard header.

- **Menu Bar** — show/hide the π icon, and choose what the menu-bar number means:
  today's cost, total cost, lines today, messages today, sessions today, or nothing
  (icon only). Live preview included.
- **General** — Launch at login, plus the default tab and time range the dashboard opens on.
- **About** — version and data source.

## How it works

- `Parser.swift` streams every session `.jsonl`, aggregating into **per-day** buckets.
- Results are cached to `~/.pi/pi-infobar-cache.json`, keyed by a signature of the session
  files (path + size + mtime), so only the first launch parses the full history; later launches
  are instant. The cache auto-rebuilds when sessions change (or via the refresh button).
- Range filtering / summarizing happens in-memory over the day buckets.

## Build from source

Requires the Swift 6 toolchain (Command Line Tools — no full Xcode needed).

```bash
./build_app.sh                 # builds build/Pi Stats.app (generates the icon too)
open "build/Pi Stats.app"
```

It's a menu-bar-only app (`LSUIElement`), so there's no Dock icon. Quit with **⌘Q** or
the Quit row in the footer.

## Packaging a release

```bash
./release.sh                   # → dist/Pi-Stats.dmg + dist/Pi-Stats.zip
./release.sh v0.1.0            # …and publishes a GitHub release (needs `gh`)
```

The DMG is a drag-to-Applications installer. The build is **ad-hoc signed** (no Apple
Developer account), so users do a one-time right-click → Open. For a frictionless,
Gatekeeper-clean install you'd add a Developer ID signature + notarization in `release.sh`.

### App icon

`Tools/make_icon.sh` renders the π mark on a gradient squircle and builds
`Resources/AppIcon.icns` (regenerated automatically by `build_app.sh` if missing).

## Project layout

```
Sources/PiInfobar/
  App.swift          NSStatusItem + borderless translucent panel, menu-bar title
  Models.swift       Aggregate / summary data models, TimeRange
  LanguageMap.swift  extension → language name / color / SF Symbol
  PiLogo.swift       vector π mark (Shape + template menu-bar image)
  SettingsStore.swift  UserDefaults keys, MenuBarMetric, LaunchAtLogin
  Settings.swift     SettingsWindowController + NavigationSplitView panes
  StatsEngine.swift  ObservableObject; load + summarize per range
  Parser.swift       jsonl scanning, per-day aggregation, disk cache
  PopoverView.swift  header, range picker, tab bar, footer
  Tabs.swift         Overview / Languages / Models / Projects / Usage
  Components.swift   StatCard, BarRow, MenuRow, formatting helpers
Tools/
  render_icon.swift  draws the 1024px app icon
  make_icon.sh       builds Resources/AppIcon.icns
build_app.sh         assembles build/Pi Stats.app
release.sh           builds dist/Pi-Stats.dmg + .zip (+ optional gh release)
```
