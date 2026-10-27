# The Norns

A DPS meter for EVE Online, built from the game's own combat log. A small always-on-top window, drawn as a piece of hardware, shows what your fleet deals, live.

<p align="center">
  <img src="https://github.com/user-attachments/assets/f3b02356-2159-42ac-b320-91f8f6bbcef3" width="269" alt="The Norns — DPS Meter" />
  &nbsp;
  <img src="https://github.com/user-attachments/assets/dff9e554-f469-4b86-b1a5-0a2537ca84dc" width="269" alt="The Norns — Settings" />
  &nbsp;
  <img src="https://github.com/user-attachments/assets/875f0ab9-9c12-46df-bc68-f7239340d2f8" width="269" alt="The Norns — Session Statistics" />
</p>

It runs fully on your PC: no AI, no cloud, no account. It only **reads** the combat logs that the EVE client writes to `Documents\EVE\logs\Gamelogs` and the titles of the open EVE windows.

- It never sends anything to the game: no key presses, no clicks, no screen reading.
- It does not read game memory, network traffic or cache files.
- It does not modify the EVE client or its settings.

## What it shows

| On the screen | What it shows |
|---|---|
| **Eff. DPS · Vorton Projector** | Vorton damage per second of every pilot over the last 60 s, one line per pilot, and the fleet's total over the last 10 s. |
| **Eff. DPS · Smartbombs** | One column per smartbomb module (found from the log): this cycle against the module's best cycle in the site, and the DPS of the last cycle. |
| **ISK** | Every bounty tick of every client in the current site as a heat grid, the number of sites done and the session total. |
| **Damage display** (on the body) | The detected NPC faction and the damage types it deals, with their share. |

- Click the function badge next to a title: a small window explains how its number is computed.
- **Fleet filter:** hits on the fleet's own ships (the anchor) are left out. A new site starts with every fleet warp.
- **Pilots** are found by themselves: every `EVE - <character>` window that has a combat log.
- **Body:** the lamp is lit while a combat log is read and flashes red when no EVE client is open. Click ノルン for the settings: language, window size, the combat log folder, the app data, session statistics and About. Drag the window by any part of the body. Clicks on the screen pass through to the game. One copy runs at a time.
- **Languages:** English, Ukrainian, Spanish, German, Italian, French. The app uses the Windows language until you pick one in the settings.

## Install

Run `TheNorns-Setup-1.1.0.exe`. It installs for your own Windows user, needs no administrator rights, and can be removed from Windows Settings → Apps. Or unpack `TheNorns-1.1.0-portable.zip` and run `TheNorns.exe`.

Your settings and session statistics live in `%LOCALAPPDATA%\The Norns`.

## Build

Windows 10/11 with Visual Studio Build Tools 2022 (Roslyn), .NET Framework 4.8:

```powershell
powershell -ExecutionPolicy Bypass -File native\build.ps1 -Out <folder>          # the app
powershell -ExecutionPolicy Bypass -File native\build.ps1 -Out <folder> -Setup   # + installer and portable zip
```

Self-tests (the output folder must exist): `TheNorns.exe --selftest-dps <folder> [--lang <code>]` and `TheNorns-Setup-<version>.exe --selftest <folder>`.

The icon is made by `tools\make_icon.ps1`, the body's name by `tools\make_name_path.ps1` (both from Zarame Gothic).

## Credits

- Orbitron and Space Grotesk (SIL OFL 1.1), Zarame Gothic by _mokuzai_ (SIL OFL 1.1, only the outlines of ノルン are embedded).
- The "working" animation after loadersz by lumberjacque (MIT).
- Their licenses are in `licenses\`.

## Legal

The Norns is a free, independent, fan-made tool. It is not affiliated with, endorsed or sponsored by CCP hf. EVE, EVE Online and CCP are trademarks or registered trademarks of CCP hf.

The Norns only reads the combat log files the EVE client writes on your PC; it does not interact with the game client or its servers. Use of third-party tools is at your own risk.

MIT License — see `LICENSE`.
