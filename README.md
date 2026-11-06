# The Norns

A passive DPS meter, real-time ISK tracker, and anomaly companion for EVE Online. Drawn as a dedicated hardware terminal with a recessed OLED display, it sits always-on-top and shows live fleet telemetry as you rat.

<p align="center">
  <img src="https://github.com/user-attachments/assets/fc03bee2-de22-491c-8c1e-9e4b8fdb6778" width="269" alt="The Norns — DPS meter with the minimize and mode keys" />
  &nbsp;
  <img src="https://github.com/user-attachments/assets/c34cd217-339a-4300-bd0b-9e9e0e87d220" width="269" alt="The Norns — Settings → Display: power display animations" />
  &nbsp;
  <img src="https://github.com/user-attachments/assets/98e6f4fa-cd90-47fa-92e9-aaea5b1944dd" width="269" alt="The Norns — Statistics: Read logs" />
</p>

<p align="center">
  <a href="https://the-norns.com/"><b>Website</b></a> &nbsp;•&nbsp;
  <a href="https://the-norns.com/source.html"><b>Source &amp; Build</b></a> &nbsp;•&nbsp;
  <a href="https://the-norns.com/eula.txt"><b>EULA</b></a> &nbsp;•&nbsp;
  <a href="https://the-norns.com/roadmap.html"><b>Roadmap</b></a> &nbsp;•&nbsp;
  <a href="https://github.com/MadisonCatfish/the-norns/releases"><b>Releases</b></a>
</p>

It runs 100% locally on your PC: no cloud, no account, no telemetry. It only **reads** the combat logs that the EVE client writes to `Documents\EVE\logs\Gamelogs` and the titles of open `EVE - <character>` windows.

- It never sends anything to the game: no key presses, no clicks, no screen capture / OCR.
- It does not read game memory, network traffic or cache files.
- It does not modify the EVE client or its settings.
- Carousel (off by default) only brings the EVE window you pick to the front, as clicking it on the taskbar would, and can minimize the one you leave; its live picture is the window thumbnail Windows itself draws (as in Alt+Tab).
- It goes to the network only for updates: the update check at start and when you press it (it can be turned off in Settings → General), and the installer's download when you press Update.

---

## Current Fleet Focus

The Norns is currently optimized for the popular **EDENCOM + Smartbombing Fleet Leader** doctrine:
- **EDENCOM DPS:** Your gunboats (Skybreaker, Stormbringer, Thunderchild) providing primary Vorton bounce DPS.
- **Smartbomb Anchor Battleship:** Your fleet leader (Nestor, Machariel, Praxis, etc.) fitted with smartbombs to clear frigate waves and anchor the fleet.
- **Intelligent Friendly Fire Filtering:** Vorton hits against your own fleet anchor are automatically filtered out, ensuring clean, true NPC DPS calculations.

---

## Features

### 1. Real-Time DPS Metrics
Whether you are just training skills for your Skybreakers / Stormbringers or fine-tuning an endgame Thunderchild fleet, live numbers and smooth splines let you clearly evaluate your skill progression, bounce efficiency, and damage ramp-up:
- **Eff. DPS · Vorton Projector:** Per-pilot outgoing DPS over the last 60 seconds (one line per pilot) and fleet totals over the last 10 seconds.
- **Eff. DPS · Smartbombs:** Per-module tracking on your anchor battleship. Compares this cycle against the module's best cycle in the site.

### 2. Smart Real-Time ISK Tracker
- **Bounty Heat Grid:** Visualizes bounty payouts across all active clients in real time as a heat matrix.
- **Per-Anomaly & Session Totals:** Live breakdown of ISK earned in the current anomaly alongside the cumulative session total.

### 3. Automated Anomaly Counting
- Tracks completed anomalies automatically on fleet warp.
- Shows each anomaly's clear time (warp to warp) in the middle of the graph while the fleet moves to the next site.
- Knows a re-warp: a warp with no damage since the last one turns the timer red and rushes it down to 00:00 instead of reporting a clear time; after more than two minutes without combat the timer stays dark.
- No more manual tallying, no spreadsheets, and no forgotten clicks to record site clears.

### 4. Smart Grid Alerts
- 🚨 **Titan / Capital on Grid:** Within seconds of taking or dealing damage to a Titan spawn, the entire hardware hull heats up to glowing bright red — giving you immediate warning to align out and save your fleet.
- 🟡 **Dreadnought on Grid:** A pirate faction dreadnought (Angel / Domination, Blood / Dark Blood, Guristas / Dread Guristas, Sansha's / True Sansha's, Serpentis / Shadow Serpentis) heats the hull the same way, in a rich yellow.
- 🔊 **Alert sound:** Three short pulses when a titan or a dreadnought spawns. Off by default; Settings → General, with a ▶ key to hear it.
- 💎 **Faction Commander Spawns:** Never leave an Asteroid Commander Battleship (Shadow Serpentis, Dread Guristas, Dark Blood, True Sansha, Domination) behind unlooted again. Alerts you the moment a commander appears on grid, and counts the session's rare spawns (★) next to the anomaly counter and in the session statistics.

### 5. NPC Faction & Damage Profile Breakdown
- Automatically detects the enemy NPC faction from log signatures.
- Displays incoming damage distribution across all four types (EM, Thermal, Kinetic, Explosive).

### 6. Hardware Body UI
- Industrial Mordu's Legion steel-blue hardware body with a recessed OLED display.
- Drag by any part of the hull or the display.
- Three keys at the top right: minimize (yellow) hides the body to the tray, the mode key (green) switches compact and full mode, and Close.
- **Tray:** The Norns lives in the notification area instead of the taskbar. Click the icon to hide or show the body; its menu opens Settings and Statistics, toggles Compact and Potato mode and checks for updates. A dot on the icon shows the state, it flashes red or yellow when a titan or a dreadnought lands, and hovering it shows the fleet's DPS and the site's ISK. "Close to tray" and "Start in the tray" are in Settings → General.
- **Power display animations:** pick the small display's animation in Settings → Display, each with a live preview: Working (the sphere), Cube wave, ノルン and Flow field.
- Hardware LOG lamp indicates active log tailing; when no EVE client is found, the small power display flickers red like a short circuit and the empty graph says so.
- The version plate on the bottom band blinks when a newer release is out; a click opens About, where **Update to …** downloads the release's installer from GitHub, checks its SHA-256 and starts it in one click.
- **Compact mode:** a shorter body for many-client screens — no power display or printed band, and the ISK grid folded into one row of column averages.
- **Potato mode:** 5 fps, no glow or shimmer, a plain hull: about 3.5 % of one CPU core instead of about 30 %.
- Click the **ノルン** badge to open in-place settings: UI scaling (100% / 125% / 150%), log directory detection, and update checks.
- **Statistics** (a key in the settings' header): every session with its sites, rare spawns, titans, dreadnoughts and ISK, kept on your PC across updates, in columns: the session, its length, the mean time per site, sites, rare spawns and ISK; a calendar picks a day or a period. **Read logs** goes through your existing combat logs (read-only) and adds the faction commanders, titans and pirate dreadnoughts found there to the sessions you already have, each spawn counted once.

### 7. Competition Mode
- For several fleet packs in one system: the anchor is a fast frigate runner instead of a smartbombing battleship. The Thunderchilds' Vorton projectors hit the runner and the lightning arcs on to the NPCs.
- The Smartbombs module becomes **Lightning rod**: the damage the group puts through the runner, the runner's intake against its repair, and a live ECG of every hit on it.
- The runner's resists, repair and the Vorton shot's damage are entered in Settings → Modes.

<p align="center">
  <img src="https://github.com/user-attachments/assets/856bd071-d10b-4311-bebe-8cb03a0e5fcc" width="640" alt="The Norns — Competition mode in a site: Vorton lightning through the runner and the Lightning rod ECG" />
</p>

### 8. Carousel
- Off by default (Settings → Carousel). The active EVE window's live picture on the power display, with its character's name.
- Next / previous hotkeys, the wheel over the power display, or a click on the client squares beside the body.
- The order is set by dragging rows in the clients table; the window you leave can be minimized, and a pin keeps one open.

### 9. Multi-Language Support
Available in 7 languages, auto-detected or selectable:
- English
- Ukrainian (Українська)
- Spanish (Español)
- German (Deutsch)
- Italian (Italiano)
- French (Français)
- Chinese, Simplified (简体中文)

---

## Planned Features (Roadmap)

- **Classic Mode:** For players dedicated to 2010-era pure smartbombing battleship fleets (Machariel, Praxis, Rokh without EDENCOM).
- **Customizable UI Themes:** Player-selectable colorways and skins.

---

## Install

Run `TheNorns-Setup-1.5.2.exe`. It installs for your Windows user profile, requires no administrator privileges, and can be cleanly uninstalled from Windows Settings → Apps. 

Alternatively, extract `TheNorns-1.5.2-portable.zip` and run `TheNorns.exe` directly.

Your settings and session statistics are stored in `%LOCALAPPDATA%\The Norns`.

## Build

Built with Roslyn from Visual Studio Build Tools 2022 on Windows 10/11 (.NET Framework 4.8):

```powershell
powershell -ExecutionPolicy Bypass -File native\build.ps1 -Out <folder>          # the app
powershell -ExecutionPolicy Bypass -File native\build.ps1 -Out <folder> -Setup   # + installer and portable zip
```

The release files are built the same way by GitHub Actions (`.github/workflows/build.yml`, Visual Studio 2022 on a Windows runner) on every pushed `v*` tag, and can be started by hand from the Actions tab.

Self-tests: `TheNorns.exe --selftest-dps <folder>` and `TheNorns-Setup-1.5.2.exe --selftest <folder>`.

## Credits

- Orbitron, Noto Sans and Noto Sans SC (SIL OFL 1.1), Zarame Gothic by _mokuzai_ (SIL OFL 1.1, outlines of ノルン embedded).
- The power display animation after loadersz by lumberjacque (MIT).
- The display glass's scratches after the free Light Scratches texture by [Poliigon](https://www.poliigon.com/texture/light-scratches-texture/1582).
- License copies are in `licenses\`.

## Privacy

The Norns collects no data about you, your characters or your game: no account, no telemetry, no analytics.

The only network request is the update check: an HTTPS request for the latest release page (`github.com/MadisonCatfish/the-norns/releases/latest`; the GitHub Releases API only if that page gives no answer) at start and when you press "Check for updates" in About. It sends nothing but the request itself; GitHub sees your IP address and the app's user agent, as with any visit (see the [GitHub Privacy Statement](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement)). Turn it off in Settings → General → "Check for updates at start". When a newer release is out, About shows "Update to <version>": only when you press it, the app downloads that release's installer from GitHub Releases, checks its SHA-256 against the release notes, starts it and closes itself.

Settings and session statistics stay on your PC, in `%LOCALAPPDATA%\The Norns`.

## Code signing

The release files are not code-signed yet. Until they have built up a reputation with Microsoft, Windows SmartScreen may warn about the installer ("Windows protected your PC" → More info → Run anyway).

To check a download, compare its SHA-256 with the one in the release notes: `Get-FileHash <file>` in PowerShell. The release files are built from this repository by GitHub Actions (`.github/workflows/build.yml`), which prints the same hashes.

Free code signing through the [SignPath Foundation](https://signpath.org/) is planned once the project is more widely used.

## Legal

The Norns is a free, independent, fan-made tool. It is not affiliated with, endorsed, or sponsored by CCP hf. EVE, EVE Online, and CCP are trademarks or registered trademarks of CCP hf.

The Norns only reads the combat log files the EVE client writes on your PC; it does not interact with the game client or its servers. Use of third-party tools is at your own risk.

MIT License — see `LICENSE`.
