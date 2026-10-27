# The Norns

A passive DPS meter, real-time ISK tracker, and anomaly companion for EVE Online. Drawn as a dedicated hardware terminal with a recessed OLED display, it sits always-on-top and shows live fleet telemetry as you rat.

<p align="center">
  <img src="https://github.com/user-attachments/assets/f3b02356-2159-42ac-b320-91f8f6bbcef3" width="269" alt="The Norns — DPS Meter" />
  &nbsp;
  <img src="https://github.com/user-attachments/assets/dff9e554-f469-4b86-b1a5-0a2537ca84dc" width="269" alt="The Norns — Settings" />
  &nbsp;
  <img src="https://github.com/user-attachments/assets/875f0ab9-9c12-46df-bc68-f7239340d2f8" width="269" alt="The Norns — Session Statistics" />
</p>

It runs 100% locally on your PC: no cloud, no account, no telemetry. It only **reads** the combat logs that the EVE client writes to `Documents\EVE\logs\Gamelogs` and the titles of open `EVE - <character>` windows.

- It never sends anything to the game: no key presses, no clicks, no screen capture / OCR.
- It does not read game memory, network traffic or cache files.
- It does not modify the EVE client or its settings.

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
- No more manual tallying, no spreadsheets, and no forgotten clicks to record site clears.

### 4. Smart Grid Alerts
- 🚨 **Titan / Capital on Grid:** Within seconds of taking or dealing damage to a Titan spawn, the entire hardware hull heats up to glowing bright red — giving you immediate warning to align out and save your fleet.
- 💎 **Faction Commander Spawns:** Never leave an Asteroid Commander Battleship (Shadow Serpentis, Dread Guristas, Dark Blood, True Sansha, Domination) behind unlooted again. Alerts you the moment a commander appears on grid.

### 5. NPC Faction & Damage Profile Breakdown
- Automatically detects the enemy NPC faction from log signatures.
- Displays incoming damage distribution across all four types (EM, Thermal, Kinetic, Explosive).

### 6. Hardware Body UI
- Industrial Mordu's Legion steel-blue hardware body with a recessed OLED display.
- Drag by any part of the hull; clicks on the display pass through to the game beneath.
- Hardware LOG lamp indicates active log tailing and pulses red when no EVE clients are found.
- Click the **ノルン** badge to open in-place settings: UI scaling (100% / 125% / 150%), log directory detection, session history, and update checks.

### 7. Multi-Language Support
Available in 6 languages, auto-detected or selectable:
- English
- Ukrainian (Українська)
- Spanish (Español)
- German (Deutsch)
- Italian (Italiano)
- French (Français)

---

## Planned Features (Roadmap)

- **Competition Mode:** For multiple fleet packs operating in the same system, replacing the smartbombing battleship with a fast and agile frigate anchor.
- **Classic Mode:** For players dedicated to 2010-era pure smartbombing battleship fleets (Machariel, Praxis, Rokh without EDENCOM).
- **Chinese (中文) Localization.**
- **Extended Faction Spawn Statistics:** In-depth metrics on commander spawn rates and loot drops.
- **Anomaly Timers:** Live stopwatch for site clear times.
- **Compact & "Potato" Modes:** Minimal footprint and ultra-low rendering modes for heavy multi-client rigs.
- **Customizable UI Themes:** Player-selectable colorways and skins.

---

## Install

Run `TheNorns-Setup-1.1.0.exe`. It installs for your Windows user profile, requires no administrator privileges, and can be cleanly uninstalled from Windows Settings → Apps. 

Alternatively, extract `TheNorns-1.1.0-portable.zip` and run `TheNorns.exe` directly.

Your settings and session statistics are stored in `%LOCALAPPDATA%\The Norns`.

## Build

Built with Roslyn from Visual Studio Build Tools 2022 on Windows 10/11 (.NET Framework 4.8):

```powershell
powershell -ExecutionPolicy Bypass -File native\build.ps1 -Out <folder>          # the app
powershell -ExecutionPolicy Bypass -File native\build.ps1 -Out <folder> -Setup   # + installer and portable zip
```

Self-tests: `TheNorns.exe --selftest-dps <folder>` and `TheNorns-Setup-1.1.0.exe --selftest <folder>`.

## Credits

- Orbitron and Space Grotesk (SIL OFL 1.1), Zarame Gothic by _mokuzai_ (SIL OFL 1.1, outlines of ノルン embedded).
- The power display animation after loadersz by lumberjacque (MIT).
- License copies are in `licenses\`.

## Legal

The Norns is a free, independent, fan-made tool. It is not affiliated with, endorsed, or sponsored by CCP hf. EVE, EVE Online, and CCP are trademarks or registered trademarks of CCP hf.

The Norns only reads the combat log files the EVE client writes on your PC; it does not interact with the game client or its servers. Use of third-party tools is at your own risk.

MIT License — see `LICENSE`.
