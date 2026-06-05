<div align="center">

<img src="docs/screenshots/icon.png" width="120" alt="Pi Stats icon" />

# Pi Stats

**A native macOS menu-bar dashboard for your [Pi](https://pi.dev) agent usage.**

See your total spend, the languages you actually code in, model costs, projects
and token usage — all computed locally from your session logs. Nothing leaves your Mac.

<a href="https://github.com/phun333/pi-infobar/releases/tag/v0.1.0"><img src="https://img.shields.io/badge/download-v0.1.0-4D7CFF?style=flat-square" alt="Download v0.1.0" /></a>
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

<div align="center">
<img src="docs/screenshots/models.png" width="260" alt="Models" />
&nbsp;
<img src="docs/screenshots/projects.png" width="260" alt="Projects" />
&nbsp;
<img src="docs/screenshots/usage.png" width="260" alt="Usage" />
</div>

<br />

## Features

- **Cost** — today's spend in the menu bar; total / avg / daily-spend chart.
- **Languages** — donut + ranked bars by lines written (TypeScript, Python, Swift, Go…).
- **Models, Projects, Tokens & Tools** — cost and counts, per item.
- **Time ranges** — `1d / 7d / 30d / All` across every tab.
- **Native** — translucent rounded panel, ⌘Q, launch-at-login, Settings window.
- **Private** — reads only `~/.pi/agent/sessions`. No network.

## Download

**[Download Pi Stats v0.1.0 →](https://github.com/phun333/pi-infobar/releases/tag/v0.1.0)**

Open the DMG, drag **Pi Stats** to Applications. First launch: right-click → **Open**
(unsigned build, one-time Gatekeeper prompt). The **π** mark then lives in your menu bar.

## How it works

Streams every `~/.pi/agent/sessions/**/*.jsonl`, aggregates per day, and caches the
result. Cost comes from each message's recorded `usage.cost` (no estimates); languages
from the file extension of every `edit`/`write`; projects from each session's `cwd`.

## Build

Swift 6 toolchain (Command Line Tools, no full Xcode):

```bash
./build_app.sh && open "build/Pi Stats.app"   # build + run
./release.sh v0.2.0                            # package DMG/zip + GitHub release
```

## License

MIT — see [LICENSE](LICENSE).
