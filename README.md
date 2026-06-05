# Pi Stats — menu bar usage dashboard

A native macOS menu-bar app that reads your local **Pi** agent sessions
(`~/.pi/agent/sessions/**/*.jsonl`) and shows a rich usage dashboard — inspired by
"OpenCode Stats", but for Pi and with **per-language coding stats**.

Everything is computed locally. Nothing leaves your machine.

## What it shows

The menu bar item displays **today's spend** (e.g. `📊 $9.75`). Click it for a popover with tabs:

- **Overview** — Total cost, sessions, messages, active days, avg/day, today + a daily-spend bar chart.
- **Languages** — *Which languages you code in most*, by lines written, with a donut + ranked bars
  (TypeScript, JavaScript, Python, Swift, Go, …). Detected from `edit`/`write` tool calls.
- **Models** — Cost & call count per model (Claude Opus, GPT, …).
- **Projects** — Cost & session count per project (from each session's `cwd`).
- **Usage** — Token breakdown (input / output / cache read / cache write) and tool-call counts.

A time-range switch (**1d / 7d / 30d / All**) filters every tab.

## How it works

- `Parser.swift` streams every session `.jsonl`, aggregating into **per-day** buckets.
- Results are cached to `~/.pi/pi-infobar-cache.json`, keyed by a signature of the session
  files (path + size + mtime), so only the first launch parses the full history; later launches
  are instant. The cache auto-rebuilds when sessions change (or via the refresh button).
- Range filtering / summarizing happens in-memory over the day buckets.

## Build & run

Requires the Swift 6 toolchain (Command Line Tools — no full Xcode needed).

```bash
./build_app.sh          # builds build/Pi Stats.app
open "build/Pi Stats.app"
```

It's a menu-bar-only app (`LSUIElement`), so there's no Dock icon. Quit from the popover footer.

### Launch at login (optional)

Drag `build/Pi Stats.app` to `/Applications`, then add it under
System Settings → General → Login Items.

## Project layout

```
Sources/PiInfobar/
  App.swift          NSStatusItem + NSPopover host, menu-bar title
  Models.swift       Aggregate / summary data models, TimeRange
  LanguageMap.swift  extension → language name / color / SF Symbol
  StatsEngine.swift  ObservableObject; load + summarize per range
  Parser.swift       jsonl scanning, per-day aggregation, disk cache
  PopoverView.swift  header, range picker, tab bar, footer
  Tabs.swift         Overview / Languages / Models / Projects / Usage
  Components.swift   StatCard, BarRow, formatting helpers
```
