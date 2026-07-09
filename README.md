<div align="center">

<img src="docs/screenshots/icon.png" width="120" alt="Pi Stats icon" />

# Pi Stats

**A native menu-bar / system-tray dashboard for your [Pi](https://pi.dev) agent usage.**

See your total spend, the languages you actually code in, model costs, projects
and token usage — all computed locally from your session logs. Local by default,
with optional SSH sync from your own remote Pi server.

> **Available on macOS *and* Windows** — same data, same design.
> The macOS app (SwiftUI) lives in [`macos/`](macos/); the Windows app (WPF)
> lives in [`windows/`](windows/).

<a href="https://github.com/phun333/pi-infobar/releases/tag/v0.4.0"><img src="https://img.shields.io/badge/download-v0.4.0-4D7CFF?style=flat-square" alt="Download v0.4.0" /></a>
<img src="https://img.shields.io/badge/platform-macOS%2014%2B%20%7C%20Windows%2010%2F11-111?style=flat-square" alt="macOS 14+ | Windows 10/11" />
<img src="https://img.shields.io/badge/built%20with-SwiftUI%20%2B%20WPF-F05138?style=flat-square" alt="SwiftUI + WPF" />
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

## What's New in v0.4.0

- **Session list in Overview** — each session is now listed with its **UUID**, **duration** and **project** folder. Click a row to **reveal the session `.jsonl` file in Finder**, or hit **Copy** to grab its UUID.
- **Toggle it off** — a new **Settings → General → Show session list** switch lets you hide the per-session list in the Overview tab if you prefer just the summary.

## Features

- **Cost** — today's spend in the menu bar; total / avg / daily-spend chart (hover any day for its exact spend).
- **Languages** — donut + ranked bars by lines written (TypeScript, Python, Swift, Go…).
- **Models, Projects, Tokens & Tools** — cost and counts, per item.
- **Time ranges** — `1d / 7d / 30d / All` across every tab.
- **Native** — translucent rounded panel, launch-at-login, Settings window. Menu bar on macOS, system tray on Windows.
- **Remote (optional)** — sync session logs from your own server over SSH, then browse them locally; flip between Local and Remote from the popover.
- **Private** — local by default (reads only your `~/.pi/agent/sessions`, no network). Remote mode only pulls logs from the host *you* configure — nothing is ever uploaded.
- **Cross-platform** — identical feature set on macOS and Windows.

## Download

Grab the latest from the **[v0.4.0 release](https://github.com/phun333/pi-infobar/releases/tag/v0.4.0)**.

### 🤖 macOS

The app is **unsigned** (no paid Apple Developer account), so macOS quarantines it on
download and may say *“Pi Stats is damaged and can’t be opened”*. That's Gatekeeper, not
a broken app — you just have to clear the quarantine flag once.

**Easiest — one-line install.** Paste into **Terminal** (downloads, installs, unquarantines, opens):

```bash
curl -L https://github.com/phun333/pi-infobar/releases/download/v0.4.0/Pi-Stats.zip -o /tmp/PiStats.zip && \
  ditto -xk /tmp/PiStats.zip /Applications && \
  xattr -dr com.apple.quarantine "/Applications/Pi Stats.app" && \
  open "/Applications/Pi Stats.app"
```

**Manual.** Download **`Pi-Stats.dmg`**, drag **Pi Stats** into **Applications**, then run
`xattr -dr com.apple.quarantine "/Applications/Pi Stats.app"` once and open it. The **π**
mark appears in your menu bar. Universal binary (Apple Silicon + Intel).

### 🪟 Windows

1. Download **`PiStats.exe`** (single portable file — no installer, no .NET required).
2. Double-click it. The **π** icon appears in your system tray.

Because the app is **unsigned**, Windows SmartScreen may show *“Windows protected your
PC”* on first launch. Click **More info → Run anyway** (one time). It's not malware —
just the Windows equivalent of macOS Gatekeeper.

**Start with Windows / install for real:** clone the repo and run
`windows\install.ps1` (copies the exe to `%LOCALAPPDATA%\PiStats`, adds a Start-Menu
shortcut, and enables auto-start at sign-in). Remove it any time with
`windows\uninstall.ps1`. Or just enable **Settings → General → Launch at login** in-app.

> Remote sync on Windows uses the built-in OpenSSH client (`ssh.exe`) + `tar.exe`
> (both ship with Windows 10/11).

## How it works

Streams every `~/.pi/agent/sessions/**/*.jsonl` (`%USERPROFILE%\.pi\agent\sessions`
on Windows), aggregates per day, and caches the result. Cost comes from each message's
recorded `usage.cost` (no estimates); languages from the file extension of every
`edit`/`write`; projects from each session's `cwd`.

**Remote mode** (optional) pulls the matching `*.jsonl` files from a server you configure
in Settings into a separate local cache, then runs the exact same local aggregation
(macOS uses `rsync`; Windows streams a `tar` over `ssh`). Data only ever flows from your
host to your machine — never out.

## Build

**macOS** — Swift 6 toolchain (Command Line Tools, no full Xcode):

```bash
cd macos
./build_app.sh && open "build/Pi Stats.app"   # build + run
./release.sh v0.4.0                            # package DMG/zip + GitHub release
```

**Windows** — .NET 8 SDK:

```powershell
cd windows
dotnet run --project PiStats   # dev run (system tray)
.\build.ps1                     # -> windows\dist\PiStats.exe (single file)
```

## License

MIT — see [LICENSE](LICENSE).
