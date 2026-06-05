<div align="center">

<img src="docs/screenshots/icon.png" width="120" alt="Pi Stats icon" />

# Pi Stats

**A native macOS menu-bar dashboard for your [Pi](https://pi.dev) agent usage.**

See your total spend, the languages you actually code in, model costs, projects
and token usage — all computed locally from your session logs. Nothing leaves your Mac.

<a href="https://github.com/phun333/pi-infobar/releases/tag/v0.1.0"><img src="https://img.shields.io/github/v/release/phun333/pi-infobar?style=flat-square&color=4D7CFF&label=download" alt="Latest release" /></a>
<img src="https://img.shields.io/badge/platform-macOS%2014%2B-111?style=flat-square" alt="macOS 14+" />
<img src="https://img.shields.io/badge/built%20with-SwiftUI-F05138?style=flat-square&logo=swift&logoColor=white" alt="SwiftUI" />
<img src="https://img.shields.io/badge/privacy-100%25%20local-4EAA25?style=flat-square" alt="100% local" />

</div>

<br />

<div align="center">
<img src="docs/screenshots/overview.png" width="320" alt="Overview" />
&nbsp;
<img src="docs/screenshots/languages.png" width="320" alt="Languages" />
</div>

<br />

## Features

- **Cost at a glance** — today's spend lives right in your menu bar.
- **Languages you actually code in** — a donut + ranked bars by *lines written*,
  detected from every `edit`/`write` (TypeScript, Python, Swift, Go…).
- **Models** — cost and call counts per model (Claude, GPT…).
- **Projects** — spend and session counts, per repo.
- **Tokens & tools** — input/output/cache breakdown and tool-call frequency.
- **Time ranges** — `1d / 7d / 30d / All`, applied to every tab.
- **Native & light** — a translucent, rounded menu-bar panel (no triangle),
  ⌘Q to quit, launch-at-login, and a real Settings window.
- **Private** — reads only your local `~/.pi/agent/sessions`. No network, ever.

<div align="center">
<img src="docs/screenshots/models.png" width="260" alt="Models" />
&nbsp;
<img src="docs/screenshots/projects.png" width="260" alt="Projects" />
&nbsp;
<img src="docs/screenshots/usage.png" width="260" alt="Usage" />
</div>

<br />

## Download

Grab the latest build from the release page:

**[Download Pi Stats v0.1.0 →](https://github.com/phun333/pi-infobar/releases/tag/v0.1.0)**

1. Download **`Pi-Stats.dmg`** from that page.
2. Open it and drag **Pi Stats** into **Applications**.
3. First launch only: right-click **Pi Stats** → **Open** (unsigned build → a one-time
   Gatekeeper prompt). It opens normally after that.
4. The **π** mark appears in your menu bar — click it for the dashboard.

> To start it automatically: **Settings → General → Launch at login**.

## Settings

Open with the gear icon in the dashboard header.

| Tab | What you can change |
|-----|---------------------|
| **Menu Bar** | Show/hide the π icon · pick the menu-bar number (today/total cost, lines, messages, sessions, or icon-only) with a live preview |
| **General** | Launch at login · default tab · default time range |
| **About** | Version & data source |

## How it works

```
~/.pi/agent/sessions/**/*.jsonl
        │  stream + parse every message
        ▼
   per-day aggregates  ──cache──▶  ~/.pi/pi-infobar-cache.json
        │  (keyed by file size+mtime — only first run parses fully)
        ▼
   summarize for the selected range  ──▶  SwiftUI dashboard
```

- **Cost** comes straight from each assistant message's recorded `usage.cost` — no estimates.
- **Languages** are detected from the file extension of every `edit`/`write` tool call,
  counting newlines in the written text as "lines".
- **Projects** come from each session's `cwd`.

## Build from source

Requires the Swift 6 toolchain (Command Line Tools — no full Xcode needed).

```bash
./build_app.sh                 # → build/Pi Stats.app (also generates the icon)
open "build/Pi Stats.app"
```

It's a menu-bar-only app (`LSUIElement`) — no Dock icon. Quit with ⌘Q.

## Releasing

```bash
./release.sh                   # → dist/Pi-Stats.dmg + dist/Pi-Stats.zip
./release.sh v0.2.0            # …and publishes a GitHub release (needs `gh`)
```

The DMG is a drag-to-Applications installer, ad-hoc signed. For a Gatekeeper-clean
install, add a Developer ID signature + notarization to `release.sh`.

## Project layout

```
Sources/PiInfobar/
  App.swift          NSStatusItem + borderless translucent panel, menu-bar title
  Models.swift       aggregate / summary models, TimeRange
  LanguageMap.swift  extension → language name / color / SF Symbol
  PiLogo.swift       vector π mark (Shape + template menu-bar image)
  SettingsStore.swift  UserDefaults keys, MenuBarMetric, LaunchAtLogin
  Settings.swift     SettingsWindowController + NavigationSplitView panes
  StatsEngine.swift  load + summarize per range
  Parser.swift       jsonl scanning, per-day aggregation, disk cache
  PopoverView.swift  header, range picker, tab bar, footer
  Tabs.swift         Overview / Languages / Models / Projects / Usage
  Components.swift   StatCard, BarRow, MenuRow, formatting helpers
Tools/
  render_icon.swift  draws the 1024px app icon
  make_icon.sh       builds Resources/AppIcon.icns
build_app.sh         assembles build/Pi Stats.app
release.sh           builds the DMG + zip (+ optional gh release)
```

## License

MIT — see [LICENSE](LICENSE).
