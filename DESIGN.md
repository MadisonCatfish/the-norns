# The Norns — design system rules

Version 1.0.0 wore the first, flat look; since 1.1.0 the window is the Norns body (`Body.cs`). This file sets the rules the redesign and every later UI change follow. The **current** values are written down so the new ones replace them in one place.

## 1. Principles

1. **The owner decides the look.** UI, colours, sizes, fonts, icons and motion change only on the owner's request. A change is shown as a sketch first (an image or an artifact); code follows the OK.
2. **One source of truth.** Every colour, size, radius, font size and duration is a token (`Theme` in `Ui.cs`, `Glass` in `Glass.cs`). No literal values in drawing code; a new need gets a new token.
3. **Compact.** The meter sits next to the game; it takes the least space that keeps the data readable. Autolayout: elements are placed from the tokens, never nudged by hand to "look right".
4. **Data first.** Numbers and curves have the most contrast; labels and chrome are quieter. Semantic colours (good / glancing / miss, in / out) never double as decoration.
5. **Every size scales with `K`** (system DPI / 96). Design at 100 % (K = 1); check at 125 % and 150 %.

## 2. Tokens

### Colours (1.0.0 look; the body has its own, `BodyTheme`)

| Token | Value | Use |
|---|---|---|
| `Back` | 18, 18, 20 | window background |
| `Dot` | 40, 40, 44 | background dot grid |
| `Card` | 30, 30, 33 | card surface |
| `CardInner` | 38, 38, 42 | inner panel, fields |
| `Border` | 52, 52, 58 | outlines |
| `Pill` / `PillHover` | 36, 36, 40 / 50, 50, 56 | buttons |
| `Text` | 232, 232, 236 | main text |
| `Muted` | 140, 140, 150 | labels, hints |
| `Mint` | 110, 230, 150 | accent, "on", good hits |
| `Yellow` | 240, 222, 70 | glancing, warnings |
| `Red` | 240, 86, 86 | misses, damage taken, errors |
| `Blue` | 96, 156, 250 | focus, info |
| `Pink` | 236, 92, 190 | pilot colour |
| `Green` | 84, 214, 120 | done |
| Pilot colours (`Pilots.Colors`) | Mint, Blue, Pink, Yellow, 168 120 240, 251 146 60, 45 212 191, 244 63 94 | one per pilot, in order |

Rules for the new palette:
- Named by role, not hue: background, surface, surface-raised, border, text, text-muted, accent, good, warn, bad, info.
- Text contrast at least 4.5 : 1 on its surface; numbers on graphs at least 3 : 1.
- 8 pilot colours, each telling apart from its neighbours and from the semantic colours, also for red-green colour blindness.
- Dark theme is the default (the game is dark); a light theme only if the owner asks.

### Type (current)

- Segoe UI; titles in Segoe UI Semibold; the log / license box in Consolas.
- Scale (pt): caption 8, small 9, control 9.25, body 9.5, H1 16.
- Five sizes only, no others. A new font family (the redesign may bring one) must be embedded or present on every Windows 10/11.
- Numbers use tabular figures where they line up.

### Spacing, radii, sizes (current)

- **Spacing grid:** 4 px. Card padding 8 (horizontal), 4 (top). Gap 8, pad 16, margin 24.
- **Glass:**
  - rim 8;
  - title row 42 (cards) / 54 (DPS window header);
  - outer radius 24, inner 16, corner smoothing 0.85 (squircle).
- **Controls:**
  - dialog footer buttons 38 high, at least 110 wide;
  - toolbar buttons 34;
  - text fields 36;
  - dense rows 30;
  - header icon buttons 24 × 24 with a 4 px gap.
- **DPS window:**
  - width `266 × K`;
  - height `(256 + 15 × (pilots − 3)) × K` from the 4th pilot on;
  - hit bars 7 px per pilot.

## 3. Icons

- **Only canonical Google Material icons** (Material Symbols / Icons, outlined, 24 × 24 grid). Never draw an icon by hand, never mix icon sets.
- Drawn from their SVG path through `MaterialIcon` (`DpsGraph.cs`): add the official path data, name the constant after the Material name (`Refresh`, `Description`, `Close`).
- **Sizes:** 16, 20 or 24 px × K. Colour only from tokens: `Muted` at rest, `Text` on hover, the accent for "on".
- Every icon button has a tooltip that says what the click does.

## 4. App icon

Done in 1.1.0-beta.11: `native\TheNorns.ico`, ノ in Zarame Gothic on the steel-blue hull with the accent lamp. The brief it follows:
- One simple mark that reads at 16 px. Square-ish silhouette, works on light and dark taskbars.
- Delivered as a multi-size `.ico` (16, 24, 32, 48, 64, 128, 256 px) plus a preview sheet on dark and light backgrounds.
- Made by a script in `tools\` (`make_icon.ps1`), so it can be rebuilt; the build embeds it with `/win32icon`.
- Checked at 16 and 32 px for legibility before it is accepted.

## 5. Splash screen

Version 1.0.0 has none. If the redesign adds one:
- It never delays the meter: at most 1.5 s, skipped by a click; the window is ready behind it.
- Shown on the first start (and after an update); a config switch `"app": { "splash": false }` turns it off.
- Drawn by the app (GDI, no web runtime) from the same tokens as the window.
- It has a render-to-PNG self-test, like every other screen.

## 6. Global interface

- **Window:**
  - frameless, always on top;
  - dragged by its header; the position is remembered per PC;
  - clicks pass through the graph area to the game;
  - one instance.
- **Header:**
  - app status dot + title on the left;
  - icon buttons on the right in a fixed order: mode switch (Thundpack), logs, reset, close;
  - 24 × 24 px with a 4 px gap.
- **Cards:**
  - one card per kind of data, stacked;
  - title muted on the left, the key number on the right;
  - the same padding everywhere.
- **Graphs:**
  - lines at 60 s width, newest on the right;
  - smoothed spline, clipped to the card;
  - the scale moves smoothly, never jumps.
- **Text that does not fit:** ends in "…" and shows in full as a tooltip; a card title may scroll as a marquee (pause at rest, then scroll, then pause).
- **Tooltips:** after about 0.5 s of hover, on every control that hides text or has an icon only.
- **Motion:**
  - 25 fps while something moves, nothing redraws when nothing changes;
  - transitions 250–300 ms, cubic ease-out;
  - no motion that carries no meaning.
- **States:**
  - every control has rest, hover, pressed and disabled looks from tokens;
  - focus is visible for keyboard use.

## 7. Languages

- The layout is sized by the English text; a longer translation is trimmed with "…" plus a tooltip, the layout never grows for it.
- Game terms stay in English as in the EVE client (DPS, Thundpack, bounty, warp, Sleepers, …).
- Every new text goes through `L.T` / `L.F` and gets its uk, es, de, it, fr translation in `native\lang\` in the same change.

## 8. Process for a UI change

1. Tokens first: say which tokens change or are added, with values.
2. Sketch for the owner (before / after). Wait for the OK.
3. Code it only through the tokens.
4. Self-test renders before and after (`--selftest-dps`, installer `--selftest`), in English and in the longest translation (uk), at K = 1 and 1.5.
5. Note it in `FIXES.md` and `CHANGELOG.md`.

## Name and mood (branch The Norns)

- From the first public release the program is called **The Norns** (Norse mythology: the three who weave the threads of fate, past, present and future). Background: `UI moodboardpp name.txt`.
- Mood image: `UI moodboard\*.png`. A near-black navy ground and a single lightning bolt in an iridescent spectral glow (blue → violet → cyan → green → yellow → orange) with chromatic fringes.
- It is a reference for the redesign, not a decided palette. Palette, accent, icon and splash stay the owner's decisions below.

## Open decisions for the redesign (the owner's)

- New palette and accent.
- Typeface: Segoe UI or an embedded one.
- App icon concept.
- Splash: yes / no.
- Whether the installer gets the same new look (it shares `Theme`, so by default it does).
