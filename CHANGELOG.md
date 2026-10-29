# Changelog

## 1.4.0 — unreleased

- Competition mode (Settings → Modes): for several fleet packs in one system. The anchor is a frigate runner that the Thunderchilds' Vorton projectors hit, and the lightning arcs on to the NPCs. The runner's resists, repair and one Vorton shot's damage (EM and Kinetic, per ship on average) are entered in a small window; a "?" card explains the formula and every field.
- Lightning rod module in place of Smartbombs in Competition mode: the damage the group puts through the runner per second (its Vorton hits in 10 s × one shot), what the runner takes against its repair, and an ECG of the runner — a bright blood-red line kicked by every hit on a damped spring, the NPCs' hits as a thin white line.
- Formula cards can scroll with the mouse wheel.
- ★ spawns count only commanders and faction NPCs: titans and dreadnoughts are no longer in the count (they still sound the alert and heat the hull).

## 1.3.0 — 2026-10-28

- Update check at start: on by default, can be turned off in Settings → General (the only network request; it still runs when you press it in About). A newer release makes the version plate on the bottom band blink in the accent; a click opens About → Download.
- No client found: the small power display flickers red like a short circuit, with a vertical line of broken pixels.
- Dreadnoughts: a pirate faction dreadnought on grid (Angel / Domination, Blood / Dark Blood, Guristas / Dread Guristas, Sansha's / True Sansha's, Serpentis / Shadow Serpentis Dreadnought) heats the hull like a titan, in a rich yellow; a titan's red goes over it. Dreadnoughts count as ★ spawns.
- Alert sound: three short pulses when a titan or a dreadnought spawns (once per spawn). Off by default; Settings → General, with a ▶ key to hear it.
- Re-warp: when the fleet warps out of a site without hitting an NPC, the anomaly timer shows in red the time since the warp before and counts down to 00:00.
- Compact mode (Settings → General): no power display or printed band, a narrower damage display, and the ISK grid folded into one row of column averages; a commander's or titan's column is drawn over its neighbours in its own colour.
- Potato mode (Settings → General): for rigs with many clients — 5 fps, no glow, halo, glass light or shimmer, a plain hull, the formula window without animation.
- Settings: the tabs scroll (◀ ▶ on the left, four shown, the open one in the accent), the languages in three columns, the switch rows a little tighter.

## 1.2.0 — 2026-10-27

- Anomaly clear time: after the fleet warp the Vorton lines sink and the time of the site just left (warp to warp) counts up from 00:00 in the middle of the graph; the first hit on an NPC fades it out and the lines come back.
- Faction spawns: a ★ count of this session's rare spawns (commanders, faction NPCs, titans) next to the anomaly counter; a click opens Statistics, where every session shows its ★ count and the total. One spawn per NPC name; the same name after 10 minutes is a new spawn.
- The anchor's smartbombs as a dotted line in the Vorton graph.
- Smartbomb columns clear at the fleet warp instead of showing the last cycle of the site just left.
- Over keys the pointer shows a pulsing ring; the app's own pointer now shows over the whole body, the screen too, and the screen drags the window like the hull.
- Tooltips appear after a short rest on a key.
- The ノルン name turns accent under the mouse (no bar under it); the NPC faction on the small display is 1 px larger and 2 px lower.

## 1.1.0 — 2026-10-27

First public release of The Norns.
- A passive DPS meter for EVE Online, built from the game's own combat logs (`Documents\EVE\logs\Gamelogs`).
- Hardware body design in Mordu's Legion steel-blue with recessed OLED screen.
- Eff. DPS for Vorton Projector fleets and per-module Smartbomb damage tracking.
- ISK bounty heat grid with anomaly counter, session totals, and rare spawn detection (commanders and titans).
- NPC faction detection with incoming damage profile breakdown.
- Multi-client automatic pilot discovery from open EVE windows (Pilot-1...8).
- Full offline operation: no accounts, no telemetry, no game memory reading, no input injection.
- Six languages: English, Ukrainian, Spanish, German, Italian, French.
- Setup installer and standalone portable zip.

## 1.1.0-beta.14 — 2026-10-27

- Rare spawns on the ISK grid: the bounty tick of a commander or a faction spawn (Commander in the name, or Shadow Serpentis, Dread Guristas, Dark Blood, True Sansha, Domination) is drawn in our blue, a titan's in alarm red. Only the spawn's own tick counts: at least 4x the site's median tick, within 4 s of a hit on it.
- A titan on grid (a hit to or from it in the last 20 s): the hull heats up to bright red in about a second and cools down in about 10 s once it is gone.

## 1.1.0-beta.13 — 2026-10-27

- The settings have tabs: General, Game logs, Storage, Statistics, About.
- General: language; window size 100 / 125 / 150 % (applied at once); remember the window position (on by default).
- Game logs: the combat log folder with a found / not found mark, Select (pick a folder) and Detect (Documents\EVE\logs\Gamelogs, also in OneDrive); kept in `config.json`. The open-folder and reset keys moved here.
- Storage: the app's data folder, its size per kind, and a key that opens it.
- Statistics: every session is kept on this PC (`%LOCALAPPDATA%\The Norns\sessions`): sites done, their time and ISK. The tab shows the totals, this session and the earlier ones.
- About: version, What's new (this changelog, scrolled with the wheel), Credits, Legal & info, and Check for updates, which goes to the network only when pressed (it says "No release page yet" until the project is published).
- Installer: the progress page is the bar alone, and the bar takes at least 1.3 s to fill, so a fast install no longer looks like a glitch.

## 1.1.0-beta.12 — 2026-10-27

- Settings on the body's screen: a click on ノルン opens them in place of the meter, another click (or their Close key) closes them. Language (the six, applied at once and kept in `config.json`), Game logs (open the combat log folder, reset the anomaly counter) and a Statistics section that comes later.
- The LOG / RST keys are gone from the strip; their actions are in the settings.
- A damage display on the strip, level with the power display and right-aligned to the dot: the detected NPC faction and the damage types it deals with their share (EVE University wiki), Material icons for EM, Thermal, Kinetic and Explosive; omni for Sleepers, Drifters and CONCORD. The faction badge left the ISK row.
- The function badge opens a formula window instead of a tooltip: a small OLED display in a rim of the hull slides up with its perspective flattening and floats over the body's bottom right corner; the text says in plain words how the number and the graph are computed (all six languages). Close key or Esc.
- Smartbomb columns glide to each new cycle instead of jumping.
- Close sits in the strip's right corner, as tall as the power display.
- No EVE client found: the lamp flashes red in bursts of six, from very fast to one last slow flash.

## 1.1.0-beta.11 — 2026-10-26

The program is now called **The Norns** everywhere: window, `TheNorns.exe`, `TheNorns-Setup-<version>.exe`, shortcuts, the Apps entry and `%LOCALAPPDATA%\The Norns`.

- The installer replaces an earlier build of the app (its files, shortcuts and Apps entry); the app moves the earlier build's settings over on its first start.
- A new app icon: ノ, the first letter of ノルン, in Zarame Gothic on the steel-blue body with the accent lamp (`tools\make_icon.ps1`).
- The installer is a piece of the body too: steel-blue hull, ノルン and the lamp on the top strip, the pages on the black OLED screen in Orbitron and Space Grotesk, hardware keys with an accent LED, lamp switches, the "working" animation while it installs.
- A legal note in the installer, `README.md` and `LICENSE`: a free, independent fan-made tool, not affiliated with CCP hf.; the trademark line; what the app does not do (no game memory, network traffic or cache files, no changes to the client).
- Fixed: the first font size the body asked for was drawn in Segoe UI instead of Orbitron / Space Grotesk (the LOG / RST markings and the smallest screen labels).
- Clean-up: unused code that held EVE window handles is gone.

## 1.1.0-beta.10 — 2026-10-26

- The body name ノルン in Zarame Gothic by _mokuzai_ (SIL OFL 1.1); only its outlines are embedded, the license is in licenses\.

## 1.1.0-beta.9 — 2026-10-26

- The formula badges are square.

## 1.1.0-beta.8 — 2026-10-26

- Screen titles: EFF. DPS · VORTON PROJECTOR (the graph and the number now count Vorton damage only) and EFF. DPS · SMARTBOMBS.
- A badge with the Material "function" symbol next to each title; hovering it explains how the number is computed (in every UI language).
- The markings on the bottom edge 10 % larger.

## 1.1.0-beta.7 — 2026-10-26

- The NPC faction badge is back, after the session total, in the faction's colour (from their ships and emblems: Guristas yellow, Blood Raiders crimson, Serpentis green, Sleepers cold blue, ...).
- Badges and their text larger (16 px at 150 %); the session total's badge darker.
- ノルン one size larger and bold; the LOG lamp's halo smaller.
- The ISK grid's halos 70 % fainter; the wipe texture on the power display 50 % fainter.

## 1.1.0-beta.6 — 2026-10-26

- The power display's animation slows down smoothly while the fleet warps between sites (a fleet warp, and no damage on NPCs for more than 8 s since it) and picks up again with the first hit in the next site.
- Soft blurred halos around the LOG lamp and the ISK grid's dots.

## 1.1.0-beta.5 — 2026-10-26

- Smartbombs: the number of modules is the first value two complete cycles after the warp agree on; a cycle cut by NPCs dying mid-volley no longer decides it (it showed 3 instead of 7).
- Smartbomb columns: light when a module deals little, deep blue at full damage.

## 1.1.0-beta.4 — 2026-10-26

- The body's name is printed in Japanese, ノルン, in white and 25 % larger; the LOG lamp sits right of it without its caption, more saturated, 5 % larger, with a fainter rim.
- Close is pale grey and turns saturated red under the mouse; it now sits before the LOG / RST keys, and all the keys are aligned to the right.
- The ISK badges use the font of the bottom markings and read clearly.

## 1.1.0-beta.3 — 2026-10-26

- Power display: the animation 40 % larger (cut at the glass edge), colour #E1FF00.
- LOG / RST keys and their plate with smaller corners.
- Screen titles as large as the numbers next to them.
- ISK grid: every dot fades in and out at its own pace and shimmers slightly, so new ticks and a reset show smoothly, not all at once.

## 1.1.0-beta.2 — 2026-10-26

Fixes after the first live run of the Norns body.

- The window is 25 % larger, and the text is drawn without hinting and with more contrast, so it reads as in the sketch.
- Smaller corners everywhere, as in the sketch.
- Smartbombs are counted once per site, from the first complete cycle after the fleet warp; NPCs of the same name no longer double the count (a ship has at most 8 high slots).

## 1.1.0-beta — 2026-10-26

The Norns body, first build.

- A new window: a steel-blue hardware body (Mordu's Legion), dragged by any part of it, with a recessed OLED screen; clicks on the screen still pass through to the game.
- Top strip: LOG lamp (lit while a combat log is read, blinking while waiting), LOG and RST keys, a power display with the "working" animation (indicator only), a blue Close key.
- Screen: DPS OUT · TOTAL (pilot lines white to grey), SMARTBOMBS · DPS (one column per module, found from the log), the ISK heat grid of every bounty tick of every client, the anomaly counter, the session total and the current anomaly's ISK.
- Thundpack filtering is always on; Hits on target, DPS in and the faction badge are gone.
- An electric-blue pointer on the body; the screen glass lights up under the mouse.
- Embedded fonts Orbitron and Space Grotesk (OFL); third-party licenses in `licenses\`.

## 1.0.0 — 2026-10-26

First release.

- DPS window: DPS out per pilot and in total, hits on target, DPS in. Pilots are found from the open EVE windows.
- Thundpack mode for a Thunderchild fleet with a Nestor anchor:
  - friendly fire is left out;
  - Total ISK/per anomaly from the bounties of all your clients, with the number of sites done and a matrix of bounty ticks;
  - a new site starts on every fleet warp.
- NPC faction badge.
- Bounties and warps from before the start are not counted; warps from every client; decimal bounty amounts; Cyrillic names; NPCs with brackets in the name.
- Its own installer and portable zip.
- Six languages: English, Ukrainian, Spanish, German, Italian, French.
