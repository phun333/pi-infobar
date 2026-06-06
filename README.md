<div align="center">

<img src="docs/screenshots/icon.png" width="120" alt="Pi Stats icon" />

# Pi Stats

**A native macOS menu-bar dashboard for your [Pi](https://pi.dev) agent usage.**

See your total spend, the languages you actually code in, model costs, projects
and token usage — all computed locally from your session logs. Local by default,
with optional SSH sync from your own remote Pi server.

<a href="https://github.com/phun333/pi-infobar/releases/tag/v0.1.0"><img src="https://img.shields.io/badge/download-v0.1.0-4D7CFF?style=flat-square" alt="Download v0.1.0" /></a>
<img src="https://img.shields.io/badge/platform-macOS%2014%2B-111?style=flat-square" alt="macOS 14+" />
<img src="https://img.shields.io/badge/built%20with-SwiftUI-F05138?style=flat-square&logo=swift&logoColor=white" alt="SwiftUI" />
<img src="https://img.shields.io/badge/privacy-local--first-4EAA25?style=flat-square" alt="local-first" />

</div>

<br />

<div align="center">
<img src="docs/screenshots/overview.png" width="320" alt="Overview" />
&nbsp;
<img src="docs/screenshots/languages.png" width="320" alt="Languages" />
</div>

<br />

<div align="center">
<img src="docs/screenshots/models.png" width="260" alt="Models" />
&nbsp;
<img src="docs/screenshots/projects.png" width="260" alt="Projects" />
&nbsp;
<img src="docs/screenshots/usage.png" width="260" alt="Usage" />
</div>

<br />

## Features

- **Cost** — today's spend in the menu bar; total / avg / daily-spend chart (hover any day for its exact spend).
- **Languages** — donut + ranked bars by lines written (TypeScript, Python, Swift, Go…).
- **Models, Projects, Tokens & Tools** — cost and counts, per item.
- **Time ranges** — `1d / 7d / 30d / All` across every tab.
- **Native** — translucent rounded panel, ⌘Q, launch-at-login, Settings window.
- **Remote (optional)** — sync session logs from your own server over SSH/rsync, then browse them locally; flip between Local and Remote from the popover.
- **Private** — local by default (reads only `~/.pi/agent/sessions`, no network). Remote mode only pulls logs from the host *you* configure — nothing is ever uploaded.

## Download

The app is **unsigned** (no paid Apple Developer account), so macOS quarantines it on
download and may say *“Pi Stats is damaged and can’t be opened”*. That's Gatekeeper, not
a broken app — you just have to clear the quarantine flag once.

### Easiest: one-line install

Paste this into **Terminal** — it downloads, installs to Applications, unquarantines, and opens:

```bash
curl -L https://github.com/phun333/pi-infobar/releases/download/v0.1.0/Pi-Stats.zip -o /tmp/PiStats.zip && \
  ditto -xk /tmp/PiStats.zip /Applications && \
  xattr -dr com.apple.quarantine "/Applications/Pi Stats.app" && \
  open "/Applications/Pi Stats.app"
```

### Manual

1. Download **[`Pi-Stats.dmg`](https://github.com/phun333/pi-infobar/releases/tag/v0.1.0)**,
   open it, drag **Pi Stats** into **Applications**. (If you already opened it and it got
   moved to Trash, put it back in Applications first — don't double-click yet.)
2. Run this once to clear the quarantine flag:
   ```bash
   xattr -dr com.apple.quarantine "/Applications/Pi Stats.app"
   ```
3. Now open it normally. The **π** mark appears in your menu bar.
   Universal binary (Apple Silicon + Intel).

## How it works

Streams every `~/.pi/agent/sessions/**/*.jsonl`, aggregates per day, and caches the
result. Cost comes from each message's recorded `usage.cost` (no estimates); languages
from the file extension of every `edit`/`write`; projects from each session's `cwd`.

**Remote mode** (optional) uses `rsync` over SSH to pull the matching `*.jsonl` files
from a server you configure in Settings into a separate local cache, then runs the exact
same local aggregation. Data only ever flows from your host to your Mac — never out.

## Build

Swift 6 toolchain (Command Line Tools, no full Xcode):

```bash
./build_app.sh && open "build/Pi Stats.app"   # build + run
./release.sh v0.2.0                            # package DMG/zip + GitHub release
```

## License

MIT — see [LICENSE](LICENSE).
