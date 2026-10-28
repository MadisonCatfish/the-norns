// Body.cs - the DPS meter as a piece of hardware (the Norns redesign, sketch "MorduSide" approved 26.10.2026):
//   * a Mordu's Legion steel-blue body with parametric-smoothed corners; dragged by any part of the body;
//   * top strip: ノルン + LOG lamp; right-aligned: Close, the LOG / RST plate, a printed dot, the power display
//     (a tilted mini screen running the loadersz "working" animation, indicator only); Close is pale grey, red under the mouse;
//   * a recessed OLED screen: DPS OUT · TOTAL (pilot lines white to dark grey), SMARTBOMBS · DPS (one column per
//     module), the ISK heat grid (every bounty tick of every client, acid-yellow scale, neighbours share heat);
//   * printed markings on the bottom edge.
// The whole body, the screen too, drags the window; the screen glass lights up where the mouse is.
// Geometry is in logical px (the sketch at 1/2) x Theme.K. Thundpack filtering is always on (owner, 26.10.2026).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TheNorns
{
    // the body's colours (sketch tokens); the screen is pure black like an OLED panel
    static class BodyTheme
    {
        public static readonly Color Hull = Color.FromArgb(78, 87, 104);          // #4E5768 Mordu's Legion steel blue
        public static readonly Color Plate = Color.FromArgb(64, 71, 85);          // the hull 18 % darker: anodised key plate
        public static readonly Color KeyTop = Color.FromArgb(44, 46, 50), KeyBottom = Color.FromArgb(25, 26, 29);
        public static readonly Color KeyRim = Color.FromArgb(12, 13, 15);
        public static readonly Color Icon = Color.FromArgb(126, 129, 136), IconHover = Color.FromArgb(190, 193, 200);
        public static readonly Color Silk = Color.FromArgb(222, 224, 228);       // printed markings
        public static readonly Color Name = Color.FromArgb(236, 235, 228);
        public static readonly Color Accent = Color.FromArgb(247, 255, 20);       // #F7FF14 acid yellow
        public static readonly Color Accent2 = Color.FromArgb(61, 43, 255);       // #3D2BFF its complement
        public static readonly Color Accent2Light = Color.FromArgb(140, 128, 255);
        public static readonly Color Ink = Color.FromArgb(242, 243, 245);         // the screen's white
        public static readonly Color BadgeFill = Color.FromArgb(32, 34, 38);
        public static readonly Color Pointer = Color.FromArgb(43, 123, 255);      // the app's arrow pointer in electric blue
        public static readonly Color PointerOutline = Color.FromArgb(150, 0, 18, 48);
        public const float Smoothing = 0.85f;
    }

    // Orbitron (screen) and Space Grotesk (body markings), OFL, embedded in the exe (native\res\fonts)
    static class NornsFonts
    {
        static PrivateFontCollection pfc;
        static FontFamily orbitron, grotesk;
        static readonly Dictionary<string, Font> cache = new Dictionary<string, Font>();

        static void Init()
        {
            if (pfc != null) return;
            pfc = new PrivateFontCollection();
            Load("Orbitron.ttf"); Load("SpaceGrotesk.ttf");
            foreach (var f in pfc.Families)
            {
                if (f.Name.IndexOf("Orbitron", StringComparison.OrdinalIgnoreCase) >= 0) orbitron = f;
                if (f.Name.IndexOf("Grotesk", StringComparison.OrdinalIgnoreCase) >= 0) grotesk = f;
            }
        }

        static void Load(string name)
        {
            try
            {
                using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("TheNorns.font." + name))
                {
                    if (s == null) return;
                    var data = new byte[s.Length];
                    s.Read(data, 0, data.Length);
                    IntPtr p = Marshal.AllocCoTaskMem(data.Length);   // GDI+ keeps reading it: never freed
                    Marshal.Copy(data, 0, p, data.Length);
                    pfc.AddMemoryFont(p, data.Length);
                }
            }
            catch { }
        }

        static Font Get(FontFamily fam, string fallback, float px, FontStyle style)
        {
            Init();
            var family = fam ?? new FontFamily(fallback);
            if (!family.IsStyleAvailable(style)) style = FontStyle.Regular;
            var key = family.Name + "|" + px.ToString("0.00", CultureInfo.InvariantCulture) + "|" + style;
            Font f;
            if (!cache.TryGetValue(key, out f)) cache[key] = f = new Font(family, px, style, GraphicsUnit.Pixel);
            return f;
        }

        // Init first: the family is read after the fonts are loaded (the very first size asked for used to get Segoe UI)
        public static Font Screen(float px, FontStyle style = FontStyle.Regular) { Init(); return Get(orbitron, "Segoe UI", px, style); }
        public static Font Body(float px, FontStyle style = FontStyle.Regular) { Init(); return Get(grotesk, "Segoe UI", px, style); }
    }

    // loadersz "working" (MIT, (c) 2026 lumberjacque, npm loadersz 0.8.0, src/core/modes/foundational.ts) redrawn in GDI:
    // tilted rings of small dots around a turning sphere, two bright dots orbiting each ring. The owner's settings:
    // speed 2.1, density 1.45 (10 rings of 41 dots), particle radius 1.7, colour = the accent.
    static class PowerAnim
    {
        const double Speed = 2.1, Density = 1.45, Particle = 1.7;

        static double Hash(double e) { double t = Math.Sin(e * 12.9898 + 78.233) * 43758.5453; return t - Math.Floor(t); }

        struct Dot { public float X, Y, Z, R, A; }

        // seconds: wall-clock seconds since the start; size: the canvas square (the sphere radius is 0.41 of it)
        public static void Draw(Graphics g, PointF center, float size, double seconds, Color color, float k)
        {
            double time = 0.6 + seconds * Speed;
            double radius = size * 0.41;
            double yaw = time * 0.18, pitch = 0.43;
            double cy = Math.Cos(yaw), sy = Math.Sin(yaw), cp = Math.Cos(pitch), sp = Math.Sin(pitch);
            Func<double, double, double, Dot> project = (x, y, z) =>
            {
                double l = x * cy - z * sy, u = x * sy + z * cy;
                return new Dot { X = (float)(center.X + l * radius), Y = (float)(center.Y - (y * cp - u * sp) * radius), Z = (float)(y * sp + u * cp) };
            };
            int rings = (int)Math.Round(7 * Density), per = (int)Math.Round(28 * Density);
            var dots = new List<Dot>(rings * (per + 2));
            for (int i = 0; i < rings; i++)
            {
                double a = 0.25 + Hash(i * 2.1) * 1.1, c = Hash(i * 1.13) * Math.PI * 2, rr = 0.44 + Hash(i * 4.37) * 0.48;
                double[] d = { Math.Sin(a) * Math.Cos(c), Math.Cos(a), Math.Sin(a) * Math.Sin(c) };
                double[] f = Math.Abs(d[1]) > 0.8 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
                double[] p = Norm(new[] { d[1] * f[2] - d[2] * f[1], d[2] * f[0] - d[0] * f[2], d[0] * f[1] - d[1] * f[0] });
                double[] m = { d[1] * p[2] - d[2] * p[1], d[2] * p[0] - d[0] * p[2], d[0] * p[1] - d[1] * p[0] };
                Func<double, Dot> on = th => project((p[0] * Math.Cos(th) + m[0] * Math.Sin(th)) * rr, (p[1] * Math.Cos(th) + m[1] * Math.Sin(th)) * rr, (p[2] * Math.Cos(th) + m[2] * Math.Sin(th)) * rr);
                for (int t = 0; t < per; t++)
                {
                    var o = on((double)t / per * Math.PI * 2);
                    o.R = 0.55f; o.A = 0.12f + (o.Z + 1) / 2 * 0.28f; dots.Add(o);
                }
                double ph = time * (0.65 + Hash(i) * 0.45) * (i % 2 == 1 ? 1 : -1) + i;
                for (int e = 0; e < 2; e++)
                {
                    var o = on(ph + e * Math.PI);
                    o.R = 1.1f + (o.Z + 1) / 2 * 1.4f; o.A = 0.8f; dots.Add(o);
                }
            }
            foreach (var o in dots.OrderBy(x => x.Z))
            {
                float r = o.R * (float)Particle * 0.28f * k;   // the demo's px at size 420 -> the mini screen
                using (var b = new SolidBrush(Color.FromArgb((int)(Math.Max(0, Math.Min(1, o.A)) * 255), color)))
                    g.FillEllipse(b, o.X - r, o.Y - r, 2 * r, 2 * r);
            }
        }

        static double[] Norm(double[] v) { double l = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]); if (l == 0) l = 1; return new[] { v[0] / l, v[1] / l, v[2] / l }; }
    }

    // The window
    partial class DpsForm : Form
    {
        public readonly bool Standalone;
        readonly Func<Rectangle?> anchor;
        readonly GameLogTail tail;
        readonly DpsStats stats;
        public DpsStats Stats { get { return stats; } }
        readonly Timer timer = new Timer { Interval = Potato ? PotatoInterval : 40 };   // 25 fps: the power display and the glass light move
        // Potato mode (owner, 28.10.2026; config.json "app": { "potato" }, off by default): for rigs running many clients.
        // 5 fps, the power display still, no glow, halo, glass light or shimmer, a plain hull; the numbers jump instead of gliding
        public static bool Potato = AppSetting("potato") is bool && (bool)AppSetting("potato");
        // the spawn alert sound (owner, 28.10.2026; config.json "app": { "spawn_sound" }, off by default): three short
        // pulses (synthesized, res\spawn_alert.wav) when a titan or a dreadnought spawns; no sound for the other spawns
        public static bool SpawnSound = AppSetting("spawn_sound") is bool && (bool)AppSetting("spawn_sound");
        static System.Media.SoundPlayer alertPlayer;
        int loudHeard;
        public static void PlayAlert()
        {
            try
            {
                if (alertPlayer == null) { alertPlayer = new System.Media.SoundPlayer(Assembly.GetExecutingAssembly().GetManifestResourceStream("TheNorns.spawn_alert.wav")); alertPlayer.Load(); }
                alertPlayer.Play();   // async
            }
            catch { }
        }
        // a new titan / dreadnought: the alert, unless it is an old one read back from the log at start
        void SoundSpawns(DateTime now)
        {
            if (stats.LoudSpawns == loudHeard) return;
            loudHeard = stats.LoudSpawns;
            if (SpawnSound && (now - stats.LastLoudSpawn).TotalSeconds < 60) PlayAlert();
        }
        const int PotatoInterval = 200;
        readonly ToolTip tip = new ToolTip { ShowAlways = true, UseAnimation = false, UseFading = false };
        const double TipDelay = 0.7;   // seconds on a key before its tip
        string tipText; Point tipAt; double tipDue;
        readonly Stopwatch clock = Stopwatch.StartNew();
        Bitmap shell;   // the static body, drawn once per size
        public DateTime? DemoNow;            // selftest: a fixed "now"
        public double? DemoSeconds;          // selftest: a fixed animation time
        public Point? DemoMouse;             // selftest: where the mouse lights the glass
        public bool? DemoFollowing;          // selftest: the LOG lamp state
        bool hoverName, downName, hoverClose, downClose;
        public bool ShowHoverName, ShowHoverClose;   // selftest: draw hover states
        static string PosFile { get { return Path.Combine(AppInfo.DataDir, "dps_pos.txt"); } }
        DateTime lastPoll = DateTime.MinValue;
        int clients = -1;              // open EVE client windows at the last poll (-1: not looked yet)
        public bool? DemoNoClients;    // selftest: the lamp's "no client" state
        Point? glare;   // the mouse over the screen glass, client px

        static Cursor pointer;
        internal static Cursor Pointer { get { return BluePointer; } }
        // the pulsing arrow over keys: 12 frames of Glass.PulseFrame, picked by the clock
        static Cursor[] pulses;
        static Cursor PulseCursor(double seconds)
        {
            if (pulses == null)
            {
                pulses = new Cursor[12];
                for (int i = 0; i < pulses.Length; i++) try { pulses[i] = Glass.MakePulse((double)i / pulses.Length, BodyTheme.Pointer, BodyTheme.PointerOutline); } catch { pulses[i] = Cursors.Default; }
            }
            return pulses[(int)(seconds / Glass.PulseLoop * pulses.Length) % pulses.Length];
        }
        static readonly Stopwatch pulseClock = Stopwatch.StartNew();
        static Cursor BluePointer { get { if (pointer == null) try { pointer = Glass.MakePointer(BodyTheme.Pointer, BodyTheme.PointerOutline); } catch { pointer = Cursors.Default; } return pointer; } }

        class VisualGraphState { public double VisualMax, VisualTotal; public readonly Dictionary<string, float[]> VisualSeries = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase); }
        readonly VisualGraphState stateOut = new VisualGraphState();

        float K { get { return Theme.K; } }
        // the whole body at 125 % of the sketch's half size (owner, 26.10.2026: the interface did not fit)
        // the body's size: 1.25 by default (owner, 26.10.2026), 1 / 1.25 / 1.5 from the settings (config.json "app": { "scale" })
        public static float UiScale = ReadScale();
        float Scale { get { return UiScale; } }
        static float ReadScale()
        {
            object v = AppSetting("scale");
            double d; return v != null && double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out d) && (d == 1 || d == 1.25 || d == 1.5) ? (float)d : 1.25f;
        }
        // remember where the window was (config.json "app": { "remember_position" }, on by default)
        public static bool RememberPos = !(AppSetting("remember_position") is bool) || (bool)AppSetting("remember_position");
        internal static object AppSetting(string key)
        {
            try
            {
                object a, v;
                if (File.Exists(AppInfo.ConfigPath) && ConfigStore.Load(AppInfo.ConfigPath).TryGetValue("app", out a) && a is Dictionary<string, object> && ((Dictionary<string, object>)a).TryGetValue(key, out v)) return v;
            }
            catch { }
            return null;
        }
        float U(float v) { return v * K * Scale; }

        // ---- layout (logical px from the sketch: 574 x 658 at 2x -> 287 x 329) ----
        const float W = 287, H = 329, Pad = 7, Strip = 35, Gap = 6, Band = 26.5f;
        // Compact mode (owner, 28.10.2026; config.json "app": { "compact" }): no power display and no printed band, the
        // damage display narrower without the faction's name, the Close key as tall as it, the top strip hugging them; the
        // bottom edge as thick as the sides; the ISK grid one row (each dot a column's average)
        public static bool Compact = AppSetting("compact") is bool && (bool)AppSetting("compact");
        const float CompactStrip = 22, CompactH = 232.5f;
        float StripH { get { return Compact ? CompactStrip : Strip; } }
        float BandH { get { return Compact ? Pad : Band; } }
        int DesiredWidth { get { return (int)Math.Round(U(W)); } }
        // compact: the settings need the full height, so the window grows while they are open
        int DesiredHeight { get { return (int)Math.Round(U(Compact && !settingsOpen ? CompactH : H)); } }

        RectangleF StripRect { get { return new RectangleF(U(Pad), U(Pad), U(W - 2 * Pad), U(StripH)); } }
        // right-aligned, right to left: Close (as tall as the power display, owner 27.10.2026), power display, printed dot
        // the key centred on the power display's glass and lip, 2 px lower than it (owner 27.10.2026)
        RectangleF CloseRect { get { if (Compact) { var d = DamageRect; return new RectangleF(StripRect.Right - U(14.2f), d.Top, U(14.2f), d.Height + U(1.5f)); } var p = PowerRect; return new RectangleF(StripRect.Right - U(14.2f), p.Top - U(1), U(14.2f), p.Height + U(1.5f) + U(2)); } }
        RectangleF PowerRect { get { var s = StripRect; return new RectangleF(s.Right - U(14.2f) - U(5) - U(53), s.Top + (s.Height - U(28)) / 2, U(53), U(28)); } }
        PointF DotCenter { get { var p = PowerRect; return new PointF(p.Left - U(5) - U(4.75f), p.Top + p.Height / 2); } }
        // ノルン is the key to the settings (owner 27.10.2026: they replace the LOG / RST keys)
        RectangleF NameRect { get { var s = StripRect; return RectangleF.FromLTRB(nameLeft - U(3), s.Top + U(3), nameRight + U(3), s.Bottom - U(3)); } }
        RectangleF CavityRect { get { return RectangleF.FromLTRB(U(Pad), U(Pad + StripH + Gap), Width - U(Pad), Height - U(BandH)); } }
        RectangleF BezelRect { get { var c = CavityRect; c.Inflate(-U(1.5f), -U(1.5f)); return c; } }
        RectangleF GlassRect { get { var c = CavityRect; c.Inflate(-U(4), -U(4)); return c; } }
        RectangleF BandRect { get { return RectangleF.FromLTRB(U(Pad + 7), Height - U(BandH), Width - U(Pad + 7), Height); } }

        // radii at 60 % with smoothing 0.6: the sketch's CSS superellipse corners read tighter than Figma smoothing at the same radius
        GraphicsPath Round(RectangleF r, float logicalRadius) { return Theme.Round(r, U(logicalRadius * 0.6f), 0.6f); }

        public DpsForm(IEnumerable<string> pilots, Func<Rectangle?> anchor, bool standalone = false)
        {
            Standalone = standalone;
            this.anchor = anchor;
            var ps = (pilots ?? Enumerable.Empty<string>()).ToList();
            tail = new GameLogTail(ps, standalone); stats = new DpsStats(ps) { Thundpack = true };
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = standalone; StartPosition = FormStartPosition.Manual;
            TopMost = true; Text = AppInfo.Name + " — DPS"; AutoScaleMode = AutoScaleMode.None;
            if (standalone) Icon = AppInfo.Icon;
            Size = new Size(DesiredWidth, DesiredHeight);
            StartSessionStats();
            timer.Tick += (s, e) =>
            {
                var now = DateTime.UtcNow;
                if ((now - lastPoll).TotalMilliseconds >= 1000)
                {
                    lastPoll = now;
                    if (replay != null) ReplayStep(now); else stats.Add(tail.Poll(stats), now);
                    SoundSpawns(now);
                    try { clients = Eve.FindWindows("EVE - ").Count; } catch { }
                    Follow();
                }
                // the glass light follows the mouse (polled, so it also follows while dragging)
                var mp = PointToClient(Cursor.Position);
                glare = GlassRect.Contains(mp) ? (Point?)mp : null;
                if (tipText != null && clock.Elapsed.TotalSeconds >= tipDue) { tip.Show(tipText, this, tipAt.X, tipAt.Y, 3000); tipText = null; }
                // the ring pulses on while the mouse rests on a key (WM_SETCURSOR comes only when it moves)
                if (OnPulse(mp) && ClientRectangle.Contains(mp) && WindowFromPoint(Cursor.Position) == Handle) SetCursor(PulseCursor(pulseClock.Elapsed.TotalSeconds).Handle);
                Render();
            };
        }

        public void AddDemo(IEnumerable<DmgEvent> es) { stats.Add(es, DemoNow ?? DateTime.UtcNow); }

        // --preview-rewarps: replays in real time a series of fleet warps from the owner's log (28.10.2026, 16:52-16:58):
        // a short site, then four re-warps of 50, 27, 30 and 28 s, then a site again. The real logs are not read meanwhile
        // (seconds from the start: warps, and spans with hits on NPCs)
        static readonly double[] ReplayWarps = { 2, 24, 74, 101, 131, 159 };
        static readonly double[][] ReplaySites = { new double[] { 5, 22 }, new double[] { 163, 200 } };
        DateTime? replay;
        public void StartReplay() { replay = DateTime.UtcNow; }
        void ReplayStep(DateTime now)
        {
            double t = (now - replay.Value).TotalSeconds, last = t - 1;
            foreach (var w in ReplayWarps) if (w > last && w <= t) stats.Tracker.OnWarp("Pilot-1", replay.Value.AddSeconds(w));
            var es = new List<DmgEvent>();
            foreach (var s in ReplaySites)
                if (t >= s[0] && t <= s[1])
                    for (int p = 1; p <= 5; p++)
                        es.Add(new DmgEvent { T = now.AddMilliseconds(-p * 90), Pilot = "Pilot-" + p, Out = true, Amount = 600 + 300 * Math.Sin(t + p), Other = "Core Lord Admiral", Quality = "Hits", Weapon = "Large Vorton Projector II" });
            foreach (var p in new[] { "Pilot-1", "Pilot-2", "Pilot-3", "Pilot-4", "Pilot-5" }) stats.EnsurePilot(p);
            stats.Add(es, now);
        }

        protected override bool ShowWithoutActivation { get { return !Standalone; } }
        const int WS_EX_LAYERED = 0x80000, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x8000000, WS_EX_TOPMOST = 0x8, WS_EX_APPWINDOW = 0x40000;
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOPMOST;
                if (!Standalone) cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                else cp.ExStyle |= WS_EX_APPWINDOW;
                return cp;
            }
        }

        // where the pulsing pointer shows: the keys, and in the settings only the spots that do something
        bool OnPulse(Point p) { return NameRect.Contains(p) || (Standalone && CloseRect.Contains(p)) || updateMark.Contains(p) || (!settingsOpen && (vortonFx.Contains(p) || sbFx.Contains(p) || spawnChip.Contains(p))) || (settingsOpen && spots.Any(sp => sp.Item2.Contains(p))); }
        bool OnKey(Point p) { return NameRect.Contains(p) || (settingsOpen && GlassRect.Contains(p)) || (Standalone && CloseRect.Contains(p)) || vortonFx.Contains(p) || sbFx.Contains(p) || spawnChip.Contains(p) || updateMark.Contains(p); }

        [DllImport("user32.dll")] static extern IntPtr SetCursor(IntPtr h);
        [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(Point p);
        const int WM_NCHITTEST = 0x84, WM_SETCURSOR = 0x20, HTCLIENT = 1, HTTRANSPARENT = -1, HTCAPTION = 2, WM_EXITSIZEMOVE = 0x0232;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                var sp = new Point((short)(m.LParam.ToInt64() & 0xFFFF), (short)((m.LParam.ToInt64() >> 16) & 0xFFFF));
                var p = PointToClient(sp);
                if (OnKey(p)) { m.Result = (IntPtr)HTCLIENT; return; }
                bool onBody;
                using (var body = Round(new RectangleF(0, 0, Width, Height), 24)) onBody = body.IsVisible(p);
                // the whole body, the screen too, is the handle to drag the window, so the app's own pointer shows over all of it
                // (owner, 27.10.2026: before, the screen let clicks through to the game and showed the game's cursor)
                m.Result = (IntPtr)(onBody ? (Standalone ? HTCAPTION : HTCLIENT) : HTTRANSPARENT);
                return;
            }
            if (m.Msg == WM_SETCURSOR)
            {
                int hit = (short)(m.LParam.ToInt64() & 0xFFFF);
                if (hit == HTCAPTION || hit == HTCLIENT) { SetCursor((OnPulse(PointToClient(Cursor.Position)) ? PulseCursor(pulseClock.Elapsed.TotalSeconds) : BluePointer).Handle); m.Result = (IntPtr)1; return; }
            }
            if (m.Msg == WM_EXITSIZEMOVE && Standalone) SavePos();
            base.WndProc(ref m);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool hn = NameRect.Contains(e.Location), hc = Standalone && CloseRect.Contains(e.Location);
            bool hv = !settingsOpen && vortonFx.Contains(e.Location), hs = !settingsOpen && sbFx.Contains(e.Location);
            bool hsp = !settingsOpen && spawnChip.Contains(e.Location);
            if (hsp != hoverSpawns) { hoverSpawns = hsp; Render(); }
            bool hup = updateMark.Contains(e.Location);
            if (hup != hoverUpdate) { hoverUpdate = hup; Render(); }
            var spot = settingsOpen ? SpotAt(e.Location) : null;
            mouseAt = e.Location;
            if (hn == hoverName && hc == hoverClose && (hv || hs) == hoverFx && spot == hotSpot) return;
            hoverName = hn; hoverClose = hc; hoverFx = hv || hs; hotSpot = spot;
            // the tip after a short rest on the key, not at once (owner, 27.10.2026)
            tip.Hide(this);
            tipText = hn ? L.T("Settings") : hc ? L.T("Close") : null;
            tipAt = hn ? new Point((int)NameRect.Left, (int)(NameRect.Bottom + U(4))) : hc ? new Point((int)CloseRect.Left, (int)(CloseRect.Bottom + U(4))) : Point.Empty;
            tipDue = clock.Elapsed.TotalSeconds + TipDelay;
            Render();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hoverName || downName || hoverClose || downClose || hoverFx || hotSpot != null || downSpot != null)
            {
                hoverName = downName = hoverClose = downClose = hoverFx = false; hotSpot = downSpot = null;
                tip.Hide(this); tipText = null;
                Render();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            tipText = null; tip.Hide(this);   // a click: no tip
            if (e.Button != MouseButtons.Left) return;
            if (updateMark.Contains(e.Location)) downUpdate = true;
            else if (NameRect.Contains(e.Location)) downName = true;
            else if (Standalone && CloseRect.Contains(e.Location)) downClose = true;
            else if (settingsOpen) downSpot = SpotAt(e.Location);
            else if (vortonFx.Contains(e.Location)) downFx = 1;
            else if (sbFx.Contains(e.Location)) downFx = 2;
            else if (spawnChip.Contains(e.Location)) downSpawns = true;
            Render();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            bool left = e.Button == MouseButtons.Left;
            if (downName) { downName = false; if (left && NameRect.Contains(e.Location)) ToggleSettings(); Render(); }
            if (downUpdate) { downUpdate = false; if (left && updateMark.Contains(e.Location)) { if (!settingsOpen) ToggleSettings(); page = "about"; } Render(); }
            if (downSpawns) { downSpawns = false; if (left && spawnChip.Contains(e.Location)) { ToggleSettings(); page = "stats"; sessions = null; } Render(); }
            if (downSpot != null)
            {
                var id = downSpot; downSpot = null;
                if (left && SpotAt(e.Location) == id) Activate(id);
                Render();
            }
            if (downFx != 0)
            {
                int which = downFx; downFx = 0;
                if (left && (which == 1 ? vortonFx : sbFx).Contains(e.Location)) FormulaWindow.Open(this, which == 1 ? FormulaWindow.Vorton : FormulaWindow.Smartbombs);
            }
            if (downClose) { downClose = false; Render(); if (left && Standalone && CloseRect.Contains(e.Location)) { SavePos(); Close(); } }
        }

        // self-tests render fixed states: the PC's saved window position is neither read nor written
        public static bool SelfTesting;

        void SavePos()
        {
            if (SelfTesting || !Standalone || !RememberPos) return;
            try { Directory.CreateDirectory(AppInfo.DataDir); File.WriteAllText(PosFile, Location.X + "," + Location.Y); }
            catch { }
        }

        void LoadPos()
        {
            if (SelfTesting || !Standalone) return;
            if (RememberPos)
            try
            {
                if (File.Exists(PosFile))
                {
                    var s = File.ReadAllText(PosFile).Trim().Split(',');
                    int x, y;
                    if (s.Length == 2 && int.TryParse(s[0], out x) && int.TryParse(s[1], out y))
                    {
                        var rect = new Rectangle(x, y, Width, Height);
                        if (Screen.AllScreens.Any(sc => sc.WorkingArea.IntersectsWith(rect))) { Location = new Point(x, y); return; }
                    }
                }
            }
            catch { }
            var wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(wa.Right - Width - (int)U(20), wa.Top + (int)U(60));
        }

        protected override void OnFormClosing(FormClosingEventArgs e) { SavePos(); base.OnFormClosing(e); }

        void OpenGameLogs()
        {
            try
            {
                var dir = GameLogTail.Dir;
                if (!Directory.Exists(dir))
                {
                    var p = Path.GetDirectoryName(dir);
                    dir = Directory.Exists(p) ? p : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }
                Process.Start("explorer.exe", dir);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, AppInfo.Name); }
        }

        public void ShowDps()
        {
            Follow();
            if (Standalone && Location == Point.Empty) LoadPos();
            if (!Visible) Show();
            Render();
            timer.Start();
            if (UpdateAtStart && !SelfTesting && Standalone && updateState == 0) CheckForUpdates(true);
        }
        public void HideDps() { timer.Stop(); Hide(); }

        void Follow()
        {
            var a = anchor != null ? anchor() : null;
            if (Size.Width != DesiredWidth || Size.Height != DesiredHeight) Size = new Size(DesiredWidth, DesiredHeight);
            if (a != null) { var p = new Point(a.Value.Right + (int)U(8), a.Value.Top); if (Location != p) Location = p; }
        }

        void Render()
        {
            if (!IsHandleCreated) return;
            using (var bmp = Draw()) AppChrome.PresentLayered(this, bmp);
        }

        static string Num(double v) { return Math.Round(v).ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "); }

        public static string FormatIsk(double isk)
        {
            if (isk <= 0) return "0.0 m. isk";
            if (isk >= 1000000000.0) return (isk / 1000000000.0).ToString("0.0", CultureInfo.InvariantCulture) + " b. isk";
            if (isk >= 1000000.0) return (isk / 1000000.0).ToString("0.0", CultureInfo.InvariantCulture) + " m. isk";
            if (isk >= 1000.0) return (isk / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + " k. isk";
            return Math.Round(isk).ToString(CultureInfo.InvariantCulture) + " isk";
        }

        // ================= drawing =================

        public Bitmap Draw()
        {
            if (shell == null || shell.Size != Size) { if (shell != null) shell.Dispose(); shell = DrawShell(); }
            var now = DemoNow ?? DateTime.UtcNow;
            var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CompositingMode = CompositingMode.SourceCopy; g.DrawImageUnscaled(shell, 0, 0); g.CompositingMode = CompositingMode.SourceOver;
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                bool following = DemoFollowing ?? tail.Following;
                double secs = DemoSeconds ?? clock.Elapsed.TotalSeconds;
                PaintLamp(g, following, DemoNoClients ?? clients == 0, secs);
                PaintDamage(g);
                PaintNameKey(g);
                PaintHeat(g, now);
                if (!Compact) PaintPower(g, following, DemoSeconds ?? AnimTime(now), DemoNoClients ?? clients == 0, secs);
                PaintClose(g, hoverClose || ShowHoverClose, downClose);
                PaintVersionPlate(g);
                PaintScreen(g, now);
            }
            return bmp;
        }

        // the parts that never change: body, strip markings, key plate, cavity and bezel, bottom markings
        Bitmap DrawShell()
        {
            var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                var all = new RectangleF(0, 0, Width, Height);
                using (var body = Round(all, 24))
                {
                    using (var b = new SolidBrush(BodyTheme.Hull)) g.FillPath(b, body);
                    // satin light from the top left, darker towards the bottom (not in Potato mode: a plain hull)
                    if (!Potato)
                    using (var lg = new LinearGradientBrush(all, Color.White, Color.Black, 80f))
                    {
                        lg.InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(36, 255, 255, 255), Color.FromArgb(5, 255, 255, 255), Color.FromArgb(66, 0, 0, 0) }, Positions = new[] { 0f, 0.45f, 1f } };
                        g.FillPath(lg, body);
                    }
                    var edge = all; edge.Inflate(-0.5f, -0.5f);
                    using (var ep = Round(edge, 24))
                    using (var lg = new LinearGradientBrush(all, Color.FromArgb(70, 255, 255, 255), Color.FromArgb(115, 0, 0, 0), 90f))
                    using (var pen = new Pen(lg, 1f))
                        g.DrawPath(pen, ep);
                }

                // ノルン (the Norns) printed in white; the LOG lamp right of it is drawn per frame
                var s = StripRect;
                float x0 = U(Pad + 7);
                var name = NamePath(U(9.5f));
                if (name != null)
                    using (name)
                    {
                        var nb0 = name.GetBounds();
                        using (var m = new Matrix()) { m.Translate(x0 - nb0.X, s.Top + (s.Height - nb0.Height) / 2 - nb0.Y); name.Transform(m); }
                        using (var nb = new SolidBrush(Color.FromArgb(244, 245, 247))) g.FillPath(nb, name);
                        nameLeft = x0; nameRight = x0 + nb0.Width; nameMid = s.Top + s.Height / 2;
                    }
                else
                    using (var jf = JapaneseFont(U(12.25f)))
                    {
                        var sz = g.MeasureString("ノルン", jf, 1000, StringFormat.GenericTypographic);
                        Weighted(g, "ノルン", jf, Color.FromArgb(244, 245, 247), x0 - U(1), s.Top + (s.Height - sz.Height) / 2, U(0.55f));
                        nameLeft = x0 - U(1); nameRight = x0 - U(1) + sz.Width; nameMid = s.Top + s.Height / 2;
                    }

                // the printed dot left of the power display
                var dc = DotCenter;
                if (!Compact)
                using (var b = new SolidBrush(Color.FromArgb(178, BodyTheme.Silk))) g.FillEllipse(b, dc.X - U(1.75f), dc.Y - U(1.75f), U(3.5f), U(3.5f));

                // the screen sits in a recess: the hull's wall is in shadow at the top and catches light at the bottom lip
                var cav = CavityRect;
                using (var cp = Round(cav, 17))
                {
                    using (var b = new SolidBrush(Potato ? Mix(BodyTheme.Hull, Color.Black, 0.3) : BodyTheme.Hull)) g.FillPath(b, cp);
                    if (!Potato)
                    using (var lg = new LinearGradientBrush(cav, Color.Black, Color.White, 90f))
                    {
                        lg.InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(140, 0, 0, 0), Color.FromArgb(64, 0, 0, 0), Color.FromArgb(26, 255, 255, 255) }, Positions = new[] { 0f, 0.55f, 1f } };
                        g.FillPath(lg, cp);
                    }
                    using (var lg = new LinearGradientBrush(cav, Color.FromArgb(0, 255, 255, 255), Color.FromArgb(56, 255, 255, 255), 90f))
                    using (var pen = new Pen(lg, 1f)) g.DrawPath(pen, cp);
                }
                var bz = BezelRect;
                using (var bp = Round(bz, 15.5f))
                using (var lg = new LinearGradientBrush(bz, Color.FromArgb(26, 27, 29), Color.FromArgb(11, 12, 13), 90f))
                {
                    var sh = bz; sh.Offset(0, U(1.5f));
                    using (var sp = Round(sh, 15.5f)) using (var sb = new SolidBrush(Color.FromArgb(110, 0, 0, 0))) g.FillPath(sb, sp);
                    g.FillPath(lg, bp);
                    using (var pen = new Pen(Color.FromArgb(26, 255, 255, 255), 1f)) g.DrawPath(pen, bp);
                }
                using (var gp = Round(GlassRect, 13)) using (var b = new SolidBrush(Color.Black)) g.FillPath(b, gp);

                updateMark = RectangleF.Empty;
                if (!Compact) PaintBand(g);
            }
            return bmp;
        }

        // a heavier cut of the embedded variable font (GDI+ only reaches its default instance): the outline, stroked
        internal static void Weighted(Graphics g, string text, Font f, Color c, float x, float y, float extra)
        {
            using (var path = new GraphicsPath())
            {
                path.AddString(text, f.FontFamily, (int)FontStyle.Regular, f.Size, new PointF(x, y), StringFormat.GenericTypographic);
                using (var b = new SolidBrush(c)) g.FillPath(b, path);
                using (var pen = new Pen(c, extra) { LineJoin = LineJoin.Round }) g.DrawPath(pen, path);
            }
        }

        // letter-spaced text (GDI+ has no tracking): one glyph at a time
        internal static float Spaced(Graphics g, string text, Font f, Brush b, float x, float y, float spacing)
        {
            var sf = StringFormat.GenericTypographic;
            foreach (var ch in text)
            {
                var s = ch.ToString();
                g.DrawString(s, f, b, x, y, sf);
                x += (ch == ' ' ? g.MeasureString("n", f, 1000, sf).Width * 0.6f : g.MeasureString(s, f, 1000, sf).Width) + spacing;
            }
            return x - spacing;
        }

        internal static float SpacedWidth(Graphics g, string text, Font f, float spacing)
        {
            var sf = StringFormat.GenericTypographic; float w = 0;
            foreach (var ch in text) w += (ch == ' ' ? g.MeasureString("n", f, 1000, sf).Width * 0.6f : g.MeasureString(ch.ToString(), f, 1000, sf).Width) + spacing;
            return Math.Max(0, w - spacing);
        }

        // printed markings on the bottom edge: left NRN-01 / PASSIVE · READ ONLY · NO INPUT, right the warning
        // triangle, three ticks, the version plate (a yellow box) and MOD 1.0
        void PaintBand(Graphics g)
        {
            Func<float, float> V = v => U(v * 1.1f);   // the markings 10 % larger than the sketch
            var r = BandRect;
            var f = NornsFonts.Body(V(5));
            float sp = V(0.72f), cy = r.Top + r.Height / 2, ty = cy - f.GetHeight(g) / 2;
            using (var silk = new SolidBrush(Color.FromArgb(215, BodyTheme.Silk)))
            using (var name = new SolidBrush(BodyTheme.Name))
            {
                float x = r.Left;
                foreach (var ch in "NRN-01")
                {
                    Weighted(g, ch.ToString(), f, BodyTheme.Name, x, ty, V(0.3f));
                    x += g.MeasureString(ch.ToString(), f, 1000, StringFormat.GenericTypographic).Width + sp;
                }
                x += V(6) - sp;
                using (var b = new SolidBrush(Color.FromArgb(191, 236, 235, 228))) g.FillRectangle(b, x, cy - V(2), V(2.5f), V(4));
                x += V(2.5f) + V(6);
                Spaced(g, "PASSIVE · READ ONLY · NO INPUT", f, silk, x, ty, sp);

                float rx = r.Right;
                float w = SpacedWidth(g, "MOD 1.0", f, sp);
                Spaced(g, "MOD 1.0", f, silk, rx - w, ty, sp); rx -= w + V(6);
                float kw = SpacedWidth(g, PlateText, f, sp) + V(6);
                var box = new RectangleF(rx - kw, cy - V(4.5f), kw, V(9));
                updateMark = box;   // the version plate, drawn per frame (it can blink)
                rx = box.Left - V(6);
                for (int i = 2; i >= 0; i--) g.FillRectangle(silk, rx - V(0.75f) - i * V(1.75f), cy - V(2.25f), V(0.75f), V(4.5f));
                rx -= V(5.25f) + V(6);
                using (var pen = new Pen(BodyTheme.Accent2Light, Math.Max(1f, V(0.65f))))
                {
                    var tri = new[] { new PointF(rx - V(3), cy - V(2.5f)), new PointF(rx, cy + V(2)), new PointF(rx - V(6), cy + V(2)) };
                    g.DrawPolygon(pen, tri);
                }
                using (var b = new SolidBrush(BodyTheme.Accent2Light)) g.FillRectangle(b, rx - V(3.35f), cy - V(1), V(0.7f), V(1.6f));
            }
        }

        // the LOG lamp: lit while a combat log is followed, slowly blinking while waiting for one
        float nameLeft, nameRight, nameMid;

        // the body name in Zarame Gothic (by _mokuzai_, SIL OFL 1.1): its outlines only (native\res\name_path.txt, made by
        // tools\make_name_path.ps1 - the font itself is 11 MB), scaled to the ink height asked for
        static PointF[] namePts; static byte[] nameTypes; static SizeF nameSize;
        internal static GraphicsPath NamePath(float inkHeight)
        {
            if (namePts == null)
                try
                {
                    using (var st = Assembly.GetExecutingAssembly().GetManifestResourceStream("TheNorns.name_path.txt"))
                    {
                        if (st == null) return null;
                        var lines = new StreamReader(st, Encoding.UTF8).ReadToEnd().Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")).ToList();
                        var wh = lines[0].Split(' ');
                        nameSize = new SizeF(float.Parse(wh[0], CultureInfo.InvariantCulture), float.Parse(wh[1], CultureInfo.InvariantCulture));
                        var pts = new List<PointF>(); var types = new List<byte>();
                        foreach (var l in lines.Skip(1))
                        {
                            var f = l.Split(' ');
                            pts.Add(new PointF(float.Parse(f[0], CultureInfo.InvariantCulture), float.Parse(f[1], CultureInfo.InvariantCulture)));
                            types.Add(byte.Parse(f[2], CultureInfo.InvariantCulture));
                        }
                        namePts = pts.ToArray(); nameTypes = types.ToArray();
                    }
                }
                catch { return null; }
            if (namePts == null || namePts.Length == 0) return null;
            var path = new GraphicsPath(namePts, nameTypes);
            float k = inkHeight / nameSize.Height;
            using (var m = new Matrix()) { m.Scale(k, k); path.Transform(m); }
            return path;
        }
        static Font JapaneseFont(float px)
        {
            foreach (var n in new[] { "Yu Gothic UI", "Yu Gothic", "Meiryo UI", "Meiryo", "MS Gothic" })
                try { var f = new Font(n, px, FontStyle.Bold, GraphicsUnit.Pixel); if (f.Name == n) return f; f.Dispose(); } catch { }
            return new Font("Segoe UI", px, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        void PaintLamp(Graphics g, bool following, bool noClients, double secs)
        {
            float d = U(6.825f), x = nameRight + U(6), y = nameMid - d / 2;
            if (noClients) { PaintAlarmLamp(g, x, y, d, secs); return; }
            double level = following ? 1 : 0.25 + 0.75 * Math.Max(0, Math.Sin(secs * Math.PI / 1.2));
            var a = BodyTheme.Accent;
            SoftGlow(g, new PointF(x + d / 2, y + d / 2), d / 2 + U(5), a, (int)(level * 150));
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(x, y, d, d);
                using (var pb = new PathGradientBrush(path))
                {
                    pb.CenterColor = Mix(Color.FromArgb(70, 72, 20), Color.FromArgb(255, 255, 200), level);
                    pb.SurroundColors = new[] { Mix(Color.FromArgb(40, 42, 12), Color.FromArgb(225, 240, 0), level) };
                    g.FillPath(pb, path);
                }
            }
            using (var pen = new Pen(Color.FromArgb(80, 12, 13, 15), U(0.6f))) g.DrawEllipse(pen, x, y, d, d);
        }

        // no EVE client found: the lamp flashes red (the Close key's red) in bursts of 6, from very fast to one last slow
        // flash - flash i lasts 0.07 + 0.8 * ln(1 + i) / ln 6 s - then 1.4 s dark before the next burst
        internal static readonly Color AlarmRed = Color.FromArgb(232, 22, 14);
        internal static double AlarmLevel(double secs)
        {
            double total = 0;
            for (int i = 0; i < 6; i++) total += Flash(i);
            double t = secs % (total + 1.4);
            for (int i = 0; i < 6; i++)
            {
                double f = Flash(i);
                if (t < f) { double s = Math.Sin(Math.PI * t / f); return s * s; }
                t -= f;
            }
            return 0;
        }
        static double Flash(int i) { return 0.07 + 0.8 * Math.Log(1 + i) / Math.Log(6); }

        void PaintAlarmLamp(Graphics g, float x, float y, float d, double secs)
        {
            double level = AlarmLevel(secs);
            var red = AlarmRed;
            SoftGlow(g, new PointF(x + d / 2, y + d / 2), d / 2 + U(5), red, (int)(level * 170));
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(x, y, d, d);
                using (var pb = new PathGradientBrush(path))
                {
                    pb.CenterColor = Mix(Color.FromArgb(70, 22, 18), Color.FromArgb(255, 200, 190), level);
                    pb.SurroundColors = new[] { Mix(Color.FromArgb(45, 14, 12), red, level) };
                    g.FillPath(pb, path);
                }
            }
            using (var pen = new Pen(Color.FromArgb(80, 12, 13, 15), U(0.6f))) g.DrawEllipse(pen, x, y, d, d);
        }

        // a soft, blurred halo: a radial gradient with a bell-shaped falloff (GDI+ has no blur)
        internal static void SoftGlow(Graphics g, PointF c, float radius, Color col, int alpha)
        {
            if (alpha <= 2 || radius <= 0 || Potato) return;
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(c.X - radius, c.Y - radius, 2 * radius, 2 * radius);
                using (var pb = new PathGradientBrush(path) { CenterColor = Color.FromArgb(Math.Min(255, alpha), col), SurroundColors = new[] { Color.FromArgb(0, col) } })
                {
                    pb.SetSigmaBellShape(1f, 1f);
                    g.FillPath(pb, path);
                }
            }
        }

        // the power display's clock: it runs at full speed in a site and slows down smoothly while the fleet warps
        // (a fleet warp, and no damage on NPCs for more than 8 s since it), then picks up again with the first hit
        double animTime, animSpeed = 1, animLast = -1;
        double AnimTime(DateTime now)
        {
            double t = clock.Elapsed.TotalSeconds, dt = animLast < 0 ? 0 : Math.Min(0.2, t - animLast);
            animLast = t;
            var warp = stats.Tracker.LastWarpTime;
            bool warping = warp > DateTime.MinValue && stats.LastNpcHit < warp && (now - warp).TotalSeconds > 8;
            animSpeed += ((warping ? 0.08 : 1.0) - animSpeed) * 0.04;
            animTime += dt * animSpeed;
            return animTime;
        }

        internal static Color Mix(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb((int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        // the power display: a small black glass, tilted back a little (its top edge narrower, the bottom lip thicker),
        // running the working animation while a log is followed; a wipe texture lies on the glass
        static Bitmap glassTexture;
        static Bitmap GlassTexture
        {
            get
            {
                if (glassTexture == null)
                    try { using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("TheNorns.power_glass.png")) if (s != null) glassTexture = new Bitmap(s); }
                    catch { }
                return glassTexture;
            }
        }

        // no EVE client (owner, 27.10.2026): the power display turns slowly in the lamp's red and now and then fails like a
        // short circuit, at random: in about one of five half-seconds it goes out for a moment (40-160 ms) or dims and
        // stutters. 1 = lit, 0 = off
        internal static double ShortLevel(double secs)
        {
            Func<double, double> hash = e => { double t = Math.Sin(e * 12.9898 + 78.233) * 43758.5453; return t - Math.Floor(t); };
            double slot = Math.Floor(secs / 0.5);
            if (hash(slot) > 0.2) return 1;
            double start = slot * 0.5 + hash(slot + 0.31) * 0.3, len = 0.04 + hash(slot + 0.67) * 0.12;
            if (secs < start || secs > start + len) return 1;
            if (hash(slot + 0.93) < 0.5) return 0;                                  // out for a moment
            return hash(Math.Floor(secs / 0.03) + 0.17) < 0.5 ? 0.25 : 0.7;      // dims and stutters
        }

        // and a failing column of the glass (owner's reference, 28.10.2026): now and then a thin vertical line of broken
        // pixels lights up at a random place for 0.08-0.3 s, flickering, sometimes with a fainter twin next to it.
        // Returns the line's place (0..1 of the width) and brightness, or null
        static double ShortHash(double e) { double t = Math.Sin(e * 12.9898 + 78.233) * 43758.5453; return t - Math.Floor(t); }
        internal static Tuple<double, double> ShortLine(double secs)
        {
            double slot = Math.Floor(secs / 0.7);
            if (ShortHash(slot + 0.11) > 0.3) return null;
            double start = slot * 0.7 + ShortHash(slot + 0.41) * 0.35, len = 0.08 + ShortHash(slot + 0.77) * 0.22;
            if (secs < start || secs > start + len) return null;
            double flick = ShortHash(Math.Floor(secs / 0.03) + 0.29);
            return Tuple.Create(0.12 + ShortHash(slot + 0.53) * 0.76, flick < 0.25 ? 0.15 : 0.55 + 0.45 * flick);
        }

        void PaintLineOfBrokenPixels(Graphics g, RectangleF face, double x, double bright, double seed)
        {
            float px = Math.Max(1f, U(1)), lx = face.Left + (float)(face.Width * x);
            for (float y = face.Top; y < face.Bottom; )
            {
                double h = ShortHash(seed + y * 0.37);
                float seg = px * (2 + (float)(h * 9));
                int a = h < 0.18 ? 0 : (int)(bright * (120 + 135 * ShortHash(seed + y * 1.7)));   // gaps and uneven pixels
                if (a > 0) using (var b = new SolidBrush(Color.FromArgb(Math.Min(255, a), Mix(AlarmRed, Color.FromArgb(255, 210, 200), 0.35)))) g.FillRectangle(b, lx, y, px, seg);
                y += seg;
            }
        }

        void PaintPower(Graphics g, bool following, double animSecs, bool noClients, double secs)
        {
            double shortLevel = noClients ? ShortLevel(secs) : 0;
            if (noClients) animSecs = secs * 0.15;   // slowed down, as while the fleet warps
            if (Potato) animSecs = 0;
            var r = PowerRect;
            var sh = r; sh.Offset(0, U(3.5f)); sh.Inflate(-U(1), 0);
            using (var sp = Round(sh, 11)) using (var b = new SolidBrush(Color.FromArgb(120, 0, 0, 0))) g.FillPath(b, sp);
            using (var lip = Round(new RectangleF(r.X, r.Y + U(1.5f), r.Width, r.Height), 11)) using (var b = new SolidBrush(Color.FromArgb(8, 9, 11))) g.FillPath(b, lip);
            var face = new RectangleF(r.X + U(0.6f), r.Y, r.Width - U(1.2f), r.Height);   // tilted back: the face is a touch narrower
            using (var fp = Round(face, 11))
            {
                using (var pb = new PathGradientBrush(fp) { CenterColor = Color.FromArgb(12, 13, 16), SurroundColors = new[] { Color.FromArgb(3, 3, 4) } }) g.FillPath(pb, fp);
                var st = g.Save();
                g.SetClip(fp);
                var dotColor = noClients ? Mix(Color.FromArgb(12, 13, 16), AlarmRed, shortLevel) : Color.FromArgb(225, 255, 0);
                if (!noClients || shortLevel > 0)
                    PowerAnim.Draw(g, new PointF(face.Left + face.Width / 2, face.Top + face.Height / 2), face.Height * 1.89f, following || noClients ? animSecs : 0, dotColor, K * Scale);
                var line = noClients ? ShortLine(secs) : null;
                if (line != null)
                {
                    double seed = Math.Floor(secs / 0.7);
                    PaintLineOfBrokenPixels(g, face, line.Item1, line.Item2, seed);
                    if (ShortHash(seed + 0.83) < 0.4) PaintLineOfBrokenPixels(g, face, line.Item1 + 2.5 * U(1) / face.Width, line.Item2 * 0.45, seed + 3);
                }
                var tex = Potato ? null : GlassTexture;
                if (tex != null)
                {
                    // the texture's light marks only (white, alpha = their brightness x 0.19), 150 % of the glass
                    var cm = new ColorMatrix(new[]
                    {
                        new float[] { 0, 0, 0, 0.19f * 0.30f, 0 }, new float[] { 0, 0, 0, 0.19f * 0.59f, 0 }, new float[] { 0, 0, 0, 0.19f * 0.11f, 0 },
                        new float[] { 0, 0, 0, 0, 0 }, new float[] { 1, 1, 1, 0, 1 }
                    });
                    using (var ia = new ImageAttributes())
                    {
                        ia.SetColorMatrix(cm);
                        float tw = face.Width * 1.5f, th = face.Height * 1.5f;
                        g.DrawImage(tex, new Rectangle((int)(face.Left - (tw - face.Width) / 2), (int)(face.Top - (th - face.Height) / 2), (int)tw, (int)th), 0, 0, tex.Width, tex.Height, GraphicsUnit.Pixel, ia);
                    }
                }
                g.Restore(st);
                using (var pen = new Pen(Color.FromArgb(28, 29, 32), U(1))) g.DrawPath(pen, fp);
                using (var pen = new Pen(Color.FromArgb(15, 255, 255, 255), 1f)) g.DrawPath(pen, fp);
            }
        }

        // Close: a pale grey anodised key in a dark casing, red under the mouse
        void PaintClose(Graphics g, bool hover, bool down)
        {
            if (!Standalone) return;
            CloseKey(g, CloseRect, hover, down, U(1));
        }

        // u = one logical px (U(1)) of the window that draws it
        internal static void CloseKey(Graphics g, RectangleF r, bool hover, bool down, float u)
        {
            Func<RectangleF, float, GraphicsPath> round = (rr, lr) => Theme.Round(rr, u * lr * 0.6f, 0.6f);
            using (var cp = round(r, 5))
            using (var lg = new LinearGradientBrush(r, Color.FromArgb(44, 46, 49), Color.FromArgb(23, 24, 26), 90f))
            {
                g.FillPath(lg, cp);
                using (var pen = new Pen(Color.FromArgb(30, 255, 255, 255), 1f)) g.DrawPath(pen, cp);
            }
            var face = RectangleF.Inflate(r, -2 * u, -2 * u);
            if (down) face.Offset(0, 0.5f * u);
            var c = down ? Color.FromArgb(160, 14, 10) : hover ? AlarmRed : Color.FromArgb(150, 154, 162);
            using (var fp = round(face, 3.5f))
            {
                using (var b = new SolidBrush(c)) g.FillPath(b, fp);
                using (var lg = new LinearGradientBrush(face, Color.Black, Color.White, 0f))
                {
                    lg.InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(46, 0, 0, 0), Color.FromArgb(41, 255, 255, 255), Color.FromArgb(10, 255, 255, 255), Color.FromArgb(56, 0, 0, 0) }, Positions = new[] { 0f, 0.3f, 0.6f, 1f } };
                    g.FillPath(lg, fp);
                }
                using (var lg = new LinearGradientBrush(face, Color.FromArgb(31, 255, 255, 255), Color.FromArgb(38, 0, 0, 0), 90f)) g.FillPath(lg, fp);
            }
        }

        // ---------------- titan / dreadnought alarm ----------------
        // a titan on grid (a hit to or from it in the last 20 s): the hull glows like heated metal, up to bright red in
        // about a second, and cools down slowly (about 10 s) once it is gone. A dreadnought heats it the same way, in a rich
        // yellow (owner, 28.10.2026); a titan's red goes over it
        double heat, heatY, heatClock = -1;
        public double? DemoHeat, DemoHeatY;   // selftest
        DateTime dreadDemo = DateTime.MinValue; bool dreadDemoHot;
        // --preview-dread: a dreadnought on grid for 6 s, then gone, every 16 s
        public void StartDreadDemo() { dreadDemo = DateTime.UtcNow; }
        static readonly Color[] HotRed = { Color.FromArgb(150, 16, 10), Color.FromArgb(205, 26, 12), Color.FromArgb(235, 52, 20), Color.FromArgb(255, 110, 40) };
        static readonly Color[] HotYellow = { Color.FromArgb(175, 120, 8), Color.FromArgb(225, 165, 14), Color.FromArgb(250, 200, 36), Color.FromArgb(255, 214, 80) };
        void PaintHeat(Graphics g, DateTime now)
        {
            double t = clock.Elapsed.TotalSeconds, dt = heatClock < 0 ? 0 : Math.Min(0.5, t - heatClock);
            heatClock = t;
            bool titan = (now - stats.LastTitan).TotalSeconds < 20;
            bool demoHot = dreadDemo != DateTime.MinValue && (DateTime.UtcNow - dreadDemo).TotalSeconds % 16 < 6;
            if (demoHot && !dreadDemoHot && SpawnSound) PlayAlert();   // the demo's dreadnought spawns: the alert, if it is on
            dreadDemoHot = demoHot;
            bool dread = (now - stats.LastDread).TotalSeconds < 20 || demoHot;
            heat += ((titan ? 1 : 0) - heat) * (1 - Math.Exp(-dt / (titan ? 0.35 : 3.5)));
            heatY += ((dread ? 1 : 0) - heatY) * (1 - Math.Exp(-dt / (dread ? 0.35 : 3.5)));
            PaintHot(g, DemoHeatY ?? heatY, HotYellow, t);
            PaintHot(g, DemoHeat ?? heat, HotRed, t);
        }
        // c: the top, the middle, the bottom of the heated hull, and the glow under it
        void PaintHot(Graphics g, double h, Color[] c, double t)
        {
            if (h < 0.01) return;
            var all = new RectangleF(0, 0, Width, Height);
            using (var body = Round(all, 24))
            using (var region = new Region(body))
            {
                using (var cav = Round(CavityRect, 17)) region.Exclude(cav);
                region.Exclude(DamageRect); region.Exclude(PowerRect); region.Exclude(CloseRect);   // the glass and the key stay as they are
                var st = g.Save();
                g.SetClip(region, CombineMode.Replace);
                double flick = 0.94 + 0.06 * Math.Sin(t * 7.3) * Math.Sin(t * 3.1);
                // heated steel: dull red at the top, bright red-orange at the bottom where the "heat" sits
                using (var lg = new LinearGradientBrush(all, Color.Black, Color.Black, 90f))
                {
                    lg.InterpolationColors = new ColorBlend
                    {
                        Colors = new[] { Color.FromArgb((int)(h * 125 * flick), c[0]), Color.FromArgb((int)(h * 150 * flick), c[1]), Color.FromArgb((int)(h * 170 * flick), c[2]) },
                        Positions = new[] { 0f, 0.55f, 1f }
                    };
                    g.FillRectangle(lg, all);
                }
                SoftGlow(g, new PointF(Width / 2f, Height), Width * 0.7f, c[3], (int)(h * 70 * flick));
                g.Restore(st);
            }
        }

        // ---------------- the damage display ----------------
        // a small black glass on the strip, level with the power display, between ノルン + lamp and the LOG / RST plate:
        // the detected NPC faction's name, and the damage types it deals with their share (EVE University wiki, "NPC damage
        // types", 27.10.2026). A new faction fades in. Icons: Material bolt / local_fire_department / trip_origin / flare.
        RectangleF DamageRect
        {
            get
            {
                // compact: left of the Close key, narrower, as tall as the faction's damage row
                if (Compact) { var s = StripRect; float h = U(17); return new RectangleF(s.Right - U(14.2f) - U(5) - U(56), s.Top + (s.Height - h) / 2, U(56), h); }
                // right-aligned to the printed dot, as far from it as the power display on its other side (owner 27.10.2026)
                var p = PowerRect; float right = DotCenter.X - (p.Left - DotCenter.X), minLeft = nameRight + U(6) + U(6.825f) + U(9);
                return RectangleF.FromLTRB(Math.Max(minLeft, right - U(74)), p.Top, right, p.Bottom);
            }
        }

        enum Dmg { EM, Thermal, Kinetic, Explosive }
        // the faction's damage dealt in %, the larger first; null = omni (all four alike); empty = varies / unknown
        static readonly Dictionary<string, Tuple<Dmg, int>[]> DamageDealt = new Dictionary<string, Tuple<Dmg, int>[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "Serpentis", new[] { Tuple.Create(Dmg.Thermal, 55), Tuple.Create(Dmg.Kinetic, 45) } },
            { "Angel Cartel", new[] { Tuple.Create(Dmg.Explosive, 62), Tuple.Create(Dmg.Kinetic, 22) } },
            { "Blood Raiders", new[] { Tuple.Create(Dmg.EM, 50), Tuple.Create(Dmg.Thermal, 48) } },
            { "Guristas", new[] { Tuple.Create(Dmg.Kinetic, 79), Tuple.Create(Dmg.Thermal, 18) } },
            { "Sansha", new[] { Tuple.Create(Dmg.EM, 53), Tuple.Create(Dmg.Thermal, 47) } },
            { "Rogue Drones", new Tuple<Dmg, int>[0] },
            { "Sleepers", null },
            { "Drifters", null },   // not on the wiki page: they deal all four types
            { "Triglavian", new[] { Tuple.Create(Dmg.Thermal, 60), Tuple.Create(Dmg.Explosive, 40) } },
            { "Amarr", new[] { Tuple.Create(Dmg.EM, 47), Tuple.Create(Dmg.Thermal, 42) } },
            { "Caldari", new[] { Tuple.Create(Dmg.Kinetic, 51), Tuple.Create(Dmg.Thermal, 48) } },
            { "Gallente", new[] { Tuple.Create(Dmg.Kinetic, 60), Tuple.Create(Dmg.Thermal, 39) } },
            { "Minmatar", new[] { Tuple.Create(Dmg.Explosive, 50), Tuple.Create(Dmg.Kinetic, 31) } },
            { "CONCORD", null },
            { "Mordu", new[] { Tuple.Create(Dmg.Kinetic, 70), Tuple.Create(Dmg.Thermal, 30) } },
        };
        static string DamageIcon(Dmg d)
        {
            switch (d)
            {
                case Dmg.EM: return MaterialIcon.Bolt;
                case Dmg.Thermal: return MaterialIcon.Fire;
                case Dmg.Kinetic: return MaterialIcon.TripOrigin;
                default: return MaterialIcon.Flare;
            }
        }
        static Color DamageColor(Dmg d)
        {
            switch (d)
            {
                case Dmg.EM: return Color.FromArgb(96, 160, 240);
                case Dmg.Thermal: return Color.FromArgb(232, 84, 56);
                case Dmg.Kinetic: return Color.FromArgb(196, 200, 210);
                default: return Color.FromArgb(238, 164, 50);
            }
        }

        string damageFaction; double damageSince = -10;
        public bool DemoDamageSettled = true;   // selftest: the faction shown fully (no fade)

        void PaintDamage(Graphics g)
        {
            var r = DamageRect;
            // the glass in a dark lip with its shadow on the hull, like the power display
            var sh = r; sh.Offset(0, U(3.5f)); sh.Inflate(-U(1), 0);
            using (var sp = Round(sh, 9)) using (var b = new SolidBrush(Color.FromArgb(120, 0, 0, 0))) g.FillPath(b, sp);
            using (var lip = Round(new RectangleF(r.X, r.Y + U(1.5f), r.Width, r.Height), 9)) using (var b = new SolidBrush(Color.FromArgb(8, 9, 11))) g.FillPath(b, lip);
            using (var fp = Round(r, 9))
            {
                using (var pb = new PathGradientBrush(fp) { CenterColor = Color.FromArgb(12, 13, 16), SurroundColors = new[] { Color.FromArgb(3, 3, 4) } }) g.FillPath(pb, fp);
                using (var pen = new Pen(Color.FromArgb(28, 29, 32), U(1))) g.DrawPath(pen, fp);
                using (var pen = new Pen(Color.FromArgb(15, 255, 255, 255), 1f)) g.DrawPath(pen, fp);
            }
            var faction = stats.DetectedFaction;
            double t = clock.Elapsed.TotalSeconds;
            if (faction != damageFaction) { damageFaction = faction; damageSince = t; }
            if (string.IsNullOrEmpty(faction))
            {
                using (var b = new SolidBrush(Color.FromArgb(70, BodyTheme.Ink))) Centered(g, "NO NPC", NornsFonts.Body(U(5.5f)), b, r);
                return;
            }
            double a = DemoDamageSettled && DemoNow != null ? 1 : Math.Min(1, (t - damageSince) / 0.9);
            a = a * a * (3 - 2 * a);   // a smooth fade in
            float pad = U(4);
            var nf = NornsFonts.Body(U(5.5f) + K);   // 1 px larger and 2 px lower (owner, 27.10.2026)
            if (!Compact)
            using (var b = new SolidBrush(Color.FromArgb((int)(a * 230), Mix(FactionColor(faction), BodyTheme.Ink, 0.35))))
                g.DrawString(faction.ToUpperInvariant(), nf, b, new RectangleF(r.Left + pad, r.Top + U(3) + 2 * K, r.Width - 2 * pad, nf.GetHeight(g) + U(1)),
                    new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            Tuple<Dmg, int>[] dealt;
            DamageDealt.TryGetValue(faction, out dealt);
            var row = Compact ? RectangleF.FromLTRB(r.Left + U(3), r.Top, r.Right - U(3), r.Bottom) : RectangleF.FromLTRB(r.Left + pad, r.Top + U(12), r.Right - pad, r.Bottom - U(3));
            var vf = NornsFonts.Screen(U(6.5f));
            if (dealt == null && DamageDealt.ContainsKey(faction))
            {
                // omni: the four icons side by side
                float ic = U(7.5f), step = row.Width / 4;
                for (int i = 0; i < 4; i++)
                    using (var b = new SolidBrush(Color.FromArgb((int)(a * 255), DamageColor((Dmg)i))))
                        MaterialIcon.Draw(g, DamageIcon((Dmg)i), new RectangleF(row.Left + step * i + (step - ic) / 2, row.Top + (row.Height - ic) / 2, ic, ic), b);
                return;
            }
            if (dealt == null || dealt.Length == 0)
            {
                using (var b = new SolidBrush(Color.FromArgb((int)(a * 150), BodyTheme.Ink))) Centered(g, dealt == null ? "?" : "VARIES", NornsFonts.Body(U(5.5f)), b, row);
                return;
            }
            // icon + share for each type, the cells spread across the glass
            float cell = row.Width / dealt.Length, isz = U(8);
            for (int i = 0; i < dealt.Length; i++)
            {
                var d = dealt[i];
                string v = d.Item2.ToString(CultureInfo.InvariantCulture);
                float tw = g.MeasureString(v, vf, 1000, StringFormat.GenericTypographic).Width, w = isz + U(2) + tw;
                float x = row.Left + cell * i + (cell - w) / 2, cy = row.Top + row.Height / 2;
                using (var ib = new SolidBrush(Color.FromArgb((int)(a * 255), DamageColor(d.Item1))))
                    MaterialIcon.Draw(g, DamageIcon(d.Item1), new RectangleF(x, cy - isz / 2, isz, isz), ib);
                using (var tb = new SolidBrush(Color.FromArgb((int)(a * 240), BodyTheme.Ink)))
                    g.DrawString(v, vf, tb, x + isz + U(2), cy - vf.GetHeight(g) / 2 + U(0.3f), StringFormat.GenericTypographic);
            }
        }

        static void Centered(Graphics g, string text, Font f, Brush b, RectangleF r)
        {
            var sz = g.MeasureString(text, f, 1000, StringFormat.GenericTypographic);
            g.DrawString(text, f, b, r.Left + (r.Width - sz.Width) / 2, r.Top + (r.Height - sz.Height) / 2, StringFormat.GenericTypographic);
        }

        // ---------------- the OLED screen ----------------
        void PaintScreen(Graphics g, DateTime now)
        {
            var glassR = GlassRect;
            using (var gp = Round(glassR, 13))
            {
                var st = g.Save();
                g.SetClip(gp);
                var c = RectangleF.FromLTRB(glassR.Left + U(10), glassR.Top + U(9), glassR.Right - U(10), glassR.Bottom - U(8));
                float gap = U(6), line = Math.Max(1f, U(0.5f));
                float avail = c.Height - 4 * gap - 2 * line;
                float h1 = avail * 1.1f / 3.7f, h2 = avail * 0.9f / 3.7f, h3 = avail * 1.7f / 3.7f;
                if (Compact) { h1 = U(60.8f); h2 = U(49.7f); h3 = avail - h1 - h2; }   // the graphs as tall as in the full body
                float y = c.Top;
                if (settingsOpen) PaintSettings(g, c);
                else
                {
                    PaintDpsOut(g, new RectangleF(c.Left, y, c.Width, h1), now); y += h1 + gap;
                    using (var b = new SolidBrush(Color.FromArgb(26, BodyTheme.Ink))) g.FillRectangle(b, c.Left, y, c.Width, line); y += line + gap;
                    PaintSmartbombs(g, new RectangleF(c.Left, y, c.Width, h2), now); y += h2 + gap;
                    using (var b = new SolidBrush(Color.FromArgb(26, BodyTheme.Ink))) g.FillRectangle(b, c.Left, y, c.Width, line); y += line + gap;
                    PaintIsk(g, new RectangleF(c.Left, y, c.Width, c.Bottom - y));
                }

                // the glass lights up where the mouse is
                var m = Potato ? null : DemoMouse ?? glare;
                if (m != null)
                {
                    float rr = U(70);
                    using (var path = new GraphicsPath())
                    {
                        path.AddEllipse(m.Value.X - rr, m.Value.Y - rr, 2 * rr, 2 * rr);
                        using (var pb = new PathGradientBrush(path) { CenterColor = Color.FromArgb(24, 255, 255, 255), SurroundColors = new[] { Color.FromArgb(0, 255, 255, 255) } })
                            g.FillPath(pb, path);
                    }
                }
                g.Restore(st);
            }
        }

        // emissive text: a faint halo, then the glyphs
        void Glow(Graphics g, string text, Font f, Color c, float x, float y, bool rightAligned = false)
        {
            text = text.ToUpperInvariant();
            if (rightAligned) x -= g.MeasureString(text, f, 1000, StringFormat.GenericTypographic).Width;
            if (!Potato)
            using (var halo = new SolidBrush(Color.FromArgb(28, c)))
                foreach (var o in new[] { new PointF(-0.6f, 0), new PointF(0.6f, 0), new PointF(0, -0.6f), new PointF(0, 0.6f) })
                    g.DrawString(text, f, halo, x + o.X * K, y + o.Y * K, StringFormat.GenericTypographic);
            using (var b = new SolidBrush(c)) g.DrawString(text, f, b, x, y, StringFormat.GenericTypographic);
        }

        Color Muted { get { return Color.FromArgb(175, BodyTheme.Ink); } }


        // the anomaly's clear time (owner, 27.10.2026): from warp to warp. After the fleet warp the Vorton lines sink, then
        // the time of the site just left counts up from 00:00 mid-graph in the headings' Orbitron; the first hit on an NPC
        // fades it out fast and the lines come back. The first warp of a session has no site before it: no time
        float quiet, timerA;            // 0..1: how far the lines are down, how much of the timer shows
        double lastQuietFrame = -1;
        public float? DemoQuiet, DemoTimerA, DemoCount;   // selftest
        public bool? DemoRewarp;                            // selftest
        double countStart = -1;            // when the count-up began (the timer's fade-in)
        const double CountSecs = 1.4;
        public float TimerThin = 1.4f;          // how much of the stroke is eaten (both edges together; owner, 27.10.2026)
        void StepQuiet(DateTime now)
        {
            var warp = stats.Tracker.LastWarpTime;
            bool still = warp != DateTime.MinValue && now >= warp && stats.LastNpcHit < warp;   // no hit since the warp
            double t = clock.Elapsed.TotalSeconds, dt = lastQuietFrame < 0 ? 0 : Math.Min(0.2, t - lastQuietFrame);
            lastQuietFrame = t;
            if (still)
            {
                quiet = Math.Min(1, quiet + (float)(dt / 0.7));                                        // lines down in 0.7 s
                if (quiet >= 1)
                {
                    if (timerA <= 0) countStart = t;
                    timerA = Math.Min(1, timerA + (float)(dt / 0.6));                                   // then the timer in 0.6 s
                }
            }
            else
            {
                timerA = Math.Max(0, timerA - (float)(dt / 0.15));                                   // out fast
                if (timerA <= 0) quiet = Math.Max(0, quiet - (float)(dt / 0.6));                      // then the lines back
            }
            if (DemoQuiet != null) quiet = DemoQuiet.Value;
            if (DemoTimerA != null) timerA = DemoTimerA.Value;
        }
        void PaintAnomalyTimer(Graphics g, RectangleF plot, DateTime now)
        {
            var tr = stats.Tracker;
            if (timerA <= 0 || tr.LastWarpTime == DateTime.MinValue || tr.PrevWarpTime == DateTime.MinValue) return;
            double a = timerA * timerA * (3 - 2 * timerA);
            // counts up from 00:00 to the clear time, slowing down at the end. A re-warp (no hit on an NPC between the two
            // warps: the site was taken; owner, 27.10.2026) shows in red the time it took and counts it down to 00:00
            bool rewarp = DemoRewarp ?? stats.LastNpcHit < tr.PrevWarpTime;
            double secs = Math.Min(99 * 60 + 59, (tr.LastWarpTime - tr.PrevWarpTime).TotalSeconds);
            int total;
            if (rewarp)
            {
                // the re-warp's time at once, held for 1 s, then counting down second by second to 00:00 (owner, 28.10.2026)
                double since = DemoCount.HasValue ? DemoCount.Value * (secs + 1) : countStart < 0 ? 0 : clock.Elapsed.TotalSeconds - countStart;
                total = (int)Math.Ceiling(Math.Max(0, secs - Math.Max(0, since - 1)));
            }
            else
            {
                double p = DemoCount ?? (countStart < 0 ? 1 : Math.Min(1, (clock.Elapsed.TotalSeconds - countStart) / CountSecs));
                p = 1 - Math.Pow(1 - p, 3);
                total = (int)Math.Round(secs * p);
            }
            var text = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
            var f = NornsFonts.Screen(U(26));
            // the digits in equal cells, so the time does not jitter as it counts
            float cell = g.MeasureString("0", f, 1000, StringFormat.GenericTypographic).Width, colon = g.MeasureString(":", f, 1000, StringFormat.GenericTypographic).Width;
            float w = 4 * cell + colon, h = f.GetHeight(g), x = plot.Left + (plot.Width - w) / 2, y = plot.Top + (plot.Height - h) / 2;
            // thinner than Orbitron's lightest (400): the glyph outlines, their edge eaten by the glass black
            using (var path = new GraphicsPath())
            {
                foreach (var ch in text)
                {
                    float cw = ch == ':' ? colon : cell;
                    float gw = g.MeasureString(ch.ToString(), f, 1000, StringFormat.GenericTypographic).Width;
                    path.AddString(ch.ToString(), f.FontFamily, (int)f.Style, f.Size, new PointF(x + (cw - gw) / 2, y + U(4) * (float)(1 - a)), StringFormat.GenericTypographic);
                    x += cw;
                }
                using (var b = new SolidBrush(Color.FromArgb((int)(a * 235), rewarp ? AlarmRed : BodyTheme.Ink))) g.FillPath(b, path);
                using (var pen = new Pen(Color.Black, U(TimerThin)) { LineJoin = LineJoin.Round }) g.DrawPath(pen, path);
            }
        }

        const string SbKey = "\0smartbombs";
        static readonly Color SbLineColor = Color.FromArgb(242, 243, 245);   // the smartbomb line: dotted, in the pilots' white (owner, 27.10.2026)
        void PaintDpsOut(Graphics g, RectangleF r, DateTime now)
        {
            var label = NornsFonts.Screen(U(9));
            var val = NornsFonts.Screen(U(9));
            double total = stats.Now(null, true, now);
            var state = stateOut;
            var pilots = stats.Pilots;
            var target = pilots.Select(p => stats.Series(p, true, now)).ToList();
            // the anchor's smartbombs as one more line, in the accent blue (owner, 27.10.2026)
            var sbr = stats.Smartbombs(now);
            var sbLine = sbr == null ? null : stats.Series(sbr.Pilot, true, now, "Smartbomb");
            if (sbLine != null) target.Add(sbLine);
            double tMax = Math.Max(100.0, target.SelectMany(s => s).DefaultIfEmpty(0).Max()) * 1.15;
            bool snap = DemoNow != null || state.VisualMax <= 0 || Potato;
            const double ease = 0.08;   // per frame at 25 fps
            state.VisualMax = snap ? tMax : state.VisualMax + (tMax - state.VisualMax) * ease;
            state.VisualTotal = snap ? total : state.VisualTotal + (total - state.VisualTotal) * ease * 2;
            for (int i = 0; i < target.Count; i++)
            {
                string key = i < pilots.Count ? pilots[i] : SbKey;
                float[] cur;
                if (snap || !state.VisualSeries.TryGetValue(key, out cur) || cur.Length != DpsStats.N) state.VisualSeries[key] = target[i].Select(v => (float)v).ToArray();
                else for (int j = 0; j < DpsStats.N; j++) cur[j] += (float)((target[i][j] - cur[j]) * ease);
            }
            if (sbLine == null) state.VisualSeries.Remove(SbKey);

            vortonFx = FormulaBadge(g, "Eff. DPS · Vorton Projector", label, r.Left, r.Top);
            Glow(g, Num(state.VisualTotal), val, BodyTheme.Ink, r.Right, r.Top, true);
            var plot = RectangleF.FromLTRB(r.Left, r.Top + U(14), r.Right, r.Bottom - U(1));
            using (var pen = new Pen(Color.FromArgb(46, BodyTheme.Ink), 1f)) g.DrawLine(pen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);
            StepQuiet(now);
            PaintAnomalyTimer(g, plot, now);
            float lift = 1 - quiet * quiet * (3 - 2 * quiet);   // the lines sink and fade after a warp
            var st = g.Save();
            g.SetClip(RectangleF.Inflate(plot, U(1), U(1)));
            for (int i = pilots.Count - 1; i >= 0; i--)   // pilot 1 (white) on top
            {
                float[] s;
                if (!state.VisualSeries.TryGetValue(pilots[i], out s)) continue;
                var pts = s.Select((v, j) => new PointF(plot.Left + plot.Width * j / (DpsStats.N - 1), Math.Max(plot.Top, plot.Bottom - (float)(plot.Height * (v / state.VisualMax)) * lift))).ToArray();
                using (var pen = new Pen(Color.FromArgb((int)(255 * lift), PilotGrey(i, pilots.Count)), U(0.8f)) { LineJoin = LineJoin.Round }) g.DrawCurve(pen, pts, 0.5f);
            }
            float[] sbs;
            if (state.VisualSeries.TryGetValue(SbKey, out sbs))
            {
                var pts = sbs.Select((v, j) => new PointF(plot.Left + plot.Width * j / (DpsStats.N - 1), Math.Max(plot.Top, plot.Bottom - (float)(plot.Height * (v / state.VisualMax)) * lift))).ToArray();
                // as quiet as the pilots' lines, dotted (owner, 27.10.2026: the solid blue drew too much attention)
                using (var pen = new Pen(Color.FromArgb((int)(SbLineColor.A * lift), SbLineColor), U(0.8f)) { LineJoin = LineJoin.Round, DashCap = DashCap.Round, DashPattern = new[] { 0.01f, 2.7f } })
                    g.DrawCurve(pen, pts, 0.5f);
            }
            g.Restore(st);
        }

        // a section title and, right of it, a badge with the Material "function" symbol: a click opens the formula window
        RectangleF vortonFx, sbFx;
        RectangleF FormulaBadge(Graphics g, string title, Font f, float x, float y)
        {
            Glow(g, title, f, Muted, x, y);
            float w = g.MeasureString(title.ToUpperInvariant(), f, 1000, StringFormat.GenericTypographic).Width;
            var r = new RectangleF(x + w + U(5), y + (f.GetHeight(g) - U(13)) / 2, U(13), U(13));   // square
            bool hover = (r.Contains(mouseAt) && hoverFx) || ShowHoverFx;
            using (var p = Round(r, 4))
            {
                if (hover) using (var b = new SolidBrush(BodyTheme.BadgeFill)) g.FillPath(b, p);
                using (var pen = new Pen(Color.FromArgb(hover ? 160 : 89, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
            }
            var ir = RectangleF.Inflate(r, -(r.Width - U(9)) / 2, -(r.Height - U(9)) / 2);
            using (var b = new SolidBrush(hover ? BodyTheme.Ink : Muted)) MaterialIcon.DrawSymbol(g, MaterialIcon.Function, ir, b);
            return r;
        }
        Point mouseAt; bool hoverFx; int downFx;   // 1 Vorton, 2 smartbombs
        public bool ShowHoverFx;   // selftest

        // pilots from white (#F2F3F5) down to dark grey (#4E5158), in order
        static Color PilotGrey(int i, int n)
        {
            double t = n <= 1 ? 0 : (double)i / (n - 1);
            return Mix(Color.FromArgb(242, 243, 245), Color.FromArgb(78, 81, 88), t);
        }

        void PaintSmartbombs(Graphics g, RectangleF r, DateTime now)
        {
            var reading = stats.Smartbombs(now);
            sbFx = FormulaBadge(g, "Eff. DPS · Smartbombs", NornsFonts.Screen(U(9)), r.Left, r.Top);
            Glow(g, reading != null ? Num(reading.Total) : "0", NornsFonts.Screen(U(9)), BodyTheme.Ink, r.Right, r.Top, true);
            int n = reading != null ? reading.Modules : 7;
            var area = RectangleF.FromLTRB(r.Left, r.Top + U(15), r.Right, r.Bottom);
            float gap = U(4), w = (area.Width - gap * (n - 1)) / n;
            SmartbombMotion(reading, n);
            for (int k = 0; k < n; k++)
            {
                var col = new RectangleF(area.Left + k * (w + gap), area.Top, w, area.Height);
                using (var tp = Round(col, 3.5f)) using (var b = new SolidBrush(Color.FromArgb(20, BodyTheme.Ink))) g.FillPath(b, tp);
                if (reading == null) continue;
                double t = sbShown[k];
                var nb = new List<double>(); if (k > 0) nb.Add(reading.Frac[k - 1]); if (k < n - 1) nb.Add(reading.Frac[k + 1]);
                double m = nb.Count > 0 ? 0.6 * t + 0.4 * nb.Average() : t;   // a column takes part of its neighbours' colour
                var c = SbColor(m);
                float h = (float)(col.Height * Math.Max(0, Math.Min(1, t)));
                if (h >= 0.5f)
                {
                    var bar = new RectangleF(col.Left, col.Bottom - h, col.Width, h);
                    using (var gp = Round(RectangleF.Inflate(bar, U(1.5f), U(1.5f)), 5)) using (var b = new SolidBrush(Color.FromArgb((int)((0.2 + m * 0.4) * 90), c))) g.FillPath(b, gp);
                    using (var bp = Round(bar, 3.5f)) using (var b = new SolidBrush(c)) g.FillPath(b, bp);
                }
            }
        }

        // the columns' motion: each one eases to its new value (time constant SbEase) instead of jumping
        const double SbEase = 0.45;
        double[] sbShown = new double[0], sbTarget = new double[0];
        double sbLastClock = -1;
        void SmartbombMotion(DpsStats.SmartbombReading reading, int n)
        {
            if (sbShown.Length != n) { sbShown = new double[n]; sbTarget = new double[n]; sbLastClock = -1; }
            double t = clock.Elapsed.TotalSeconds, dt = DemoSeconds != null || sbLastClock < 0 ? 0 : Math.Min(0.5, t - sbLastClock);
            if (Potato) dt = 1e3;   // the columns jump
            sbLastClock = t;
            for (int k = 0; k < n; k++)
            {
                double target = reading != null && k < reading.Frac.Length ? reading.Frac[k] : 0;
                if (Math.Abs(target - sbTarget[k]) > 0.005)
                {
                    if (sbTarget[k] <= 0) sbShown[k] = target;   // the very first value is shown at once
                    sbTarget[k] = target;
                }
                sbShown[k] += (target - sbShown[k]) * (1 - Math.Exp(-dt / SbEase));
            }
        }

        // selftest: a frame `seconds` after a new cycle replaced `previous` (the columns part way)
        public void DemoSmartbombChange(double[] previous, double seconds)
        {
            var reading = stats.Smartbombs(DemoNow ?? DateTime.UtcNow);
            int n = reading.Modules;
            sbShown = new double[n]; sbTarget = new double[n];
            for (int k = 0; k < n; k++)
            {
                sbTarget[k] = reading.Frac[k];
                sbShown[k] = previous[k] + (reading.Frac[k] - previous[k]) * (1 - Math.Exp(-seconds / SbEase));
            }
        }

        // light violet for a weak module -> the second accent -> deep blue at full damage (owner, 26.10.2026)
        static Color SbColor(double t)
        {
            return t < 0.5 ? Mix(Color.FromArgb(175, 166, 255), BodyTheme.Accent2, t * 2) : Mix(BodyTheme.Accent2, Color.FromArgb(28, 18, 150), (t - 0.5) * 2);
        }

        public RectangleF IskRect { get; private set; }

        // the version plate on the bottom band (owner, 28.10.2026): the yellow box shows the app's version; while a newer
        // one is out it blinks slowly in the same yellow. A click opens About
        RectangleF updateMark; bool hoverUpdate, downUpdate;
        static readonly Color PlateYellow = Color.FromArgb(232, 176, 32);
        static string PlateText { get { return AppInfo.Version; } }
        void PaintVersionPlate(Graphics g)
        {
            var box = updateMark;
            if (box.IsEmpty) return;
            Func<float, float> V = v => U(v * 1.1f);
            var f = NornsFonts.Body(V(5));
            float sp = V(0.72f), ty = box.Top + box.Height / 2 - f.GetHeight(g) / 2;
            // a newer version: the settings' accent, blinking every 2.4 s with quick edges and a held light (owner, 28.10.2026)
            double a = 1; var col = PlateYellow;
            if (updateState == 3)
            {
                double t = DemoSeconds ?? clock.Elapsed.TotalSeconds, c = 0.5 + 0.5 * Math.Cos(t * Math.PI * 2 / 2.4);
                double e = Math.Max(0, Math.Min(1, (c - 0.25) / 0.5)); e = e * e * (3 - 2 * e);
                a = 0.15 + 0.85 * e; col = BodyTheme.Accent;
                SoftGlow(g, new PointF(box.Left + box.Width / 2, box.Top + box.Height / 2), box.Width * 0.7f, col, (int)(e * 60));
            }
            if (hoverUpdate || ShowHoverUpdate) using (var hb = new SolidBrush(Color.FromArgb(40, col))) g.FillRectangle(hb, box);
            using (var pen = new Pen(Color.FromArgb((int)(217 * a), col), Math.Max(1f, V(0.5f)))) g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);
            using (var yb = new SolidBrush(Color.FromArgb((int)(255 * a), col))) Spaced(g, PlateText, f, yb, box.Left + V(3), ty, sp);
        }
        public bool ShowHoverUpdate;   // selftest
        RectangleF spawnChip; bool hoverSpawns, downSpawns;
        public bool ShowHoverSpawns;   // selftest

        // the heat grid's animation state: shown heat and presence per cell, and each cell's own pace and shimmer beat
        readonly float[] cellHeat = new float[260], cellLit = new float[260];
        static readonly double[] cellPace = new double[260], cellBeat = new double[260], cellPhase = new double[260];
        static DpsForm()
        {
            var rnd = new Random(11);
            for (int i = 0; i < 260; i++) { cellPace[i] = 0.03 + rnd.NextDouble() * 0.09; cellBeat[i] = 0.6 + rnd.NextDouble() * 1.4; cellPhase[i] = rnd.NextDouble() * Math.PI * 2; }
        }

        void PaintIsk(Graphics g, RectangleF r)
        {
            IskRect = r;
            double secs = DemoSeconds ?? clock.Elapsed.TotalSeconds;
            var tr = stats.Tracker;
            var bf = NornsFonts.Body(U(8.5f));
            float bh = U(15), x = r.Left;
            x = Badge(g, tr.AnomalyCount + " " + L.T("anom"), bf, x, r.Top, bh, false) + U(4);
            // the rare spawns of this session (owner, 27.10.2026: the count is enough); a click opens the statistics
            float sx = x;
            x = Badge(g, stats.Spawns.Count.ToString(CultureInfo.InvariantCulture), bf, x, r.Top, bh, hoverSpawns || ShowHoverSpawns, null, null, MaterialIcon.Star) + U(4);
            spawnChip = RectangleF.FromLTRB(sx, r.Top, x - U(4), r.Top + bh);
            x = Badge(g, FormatIsk(tr.TotalSessionIsk), bf, x, r.Top, bh, true, "Σ") + U(4);
            Glow(g, FormatIsk(tr.CurrentAnomalyIsk), NornsFonts.Screen(U(9)), BodyTheme.Ink, r.Right, r.Top, true);

            if (Compact) { PaintIskRow(g, RectangleF.FromLTRB(r.Left, r.Top + bh + U(5), r.Right, r.Bottom)); return; }
            // the heat grid: every bounty tick of the anomaly (all clients), filled column by column, 10 rows
            const int Rows = 10, Cols = 26;
            var grid = RectangleF.FromLTRB(r.Left, r.Top + bh + U(5), r.Right, r.Bottom);
            float gap = U(1.5f), cw = (grid.Width - gap * (Cols - 1)) / Cols, ch = (grid.Height - gap * (Rows - 1)) / Rows, d = Math.Min(cw, ch);
            var ticks = tr.Bounties;
            int first = Math.Max(0, ticks.Count - Rows * Cols);   // more ticks than cells: the newest ones
            int count = ticks.Count - first;
            var raw = new double[count];
            var sorted = ticks.Skip(first).OrderBy(v => v).ToList(); long median = sorted.Count > 0 ? sorted[sorted.Count / 2] : 0;
            for (int i = 0; i < count; i++) raw[i] = Math.Max(0, Math.Min(1, (Math.Log(Math.Max(1, ticks[first + i])) - Math.Log(3000)) / (Math.Log(270000) - Math.Log(3000))));
            for (int i = 0; i < Rows * Cols; i++)
            {
                int col = i / Rows, row = i % Rows;
                float cx = grid.Left + col * (cw + gap) + cw / 2, cy = grid.Top + row * (ch + gap) + ch / 2;
                var cell = new RectangleF(cx - d / 2, cy - d / 2, d, d);
                // target: lit (presence 1) at its heat, or an empty dot (presence 0)
                double target = 0, presence = i < count ? 1 : 0;
                if (i < count)
                {
                    // a cell takes 40 % of its 8 neighbours' heat (the nearer ones twice as much as the diagonal ones)
                    double sum = 0, wsum = 0;
                    for (int dc = -1; dc <= 1; dc++)
                        for (int dr = -1; dr <= 1; dr++)
                        {
                            if (dc == 0 && dr == 0) continue;
                            int nr = row + dr, nc = col + dc, ni = nc * Rows + nr;
                            if (nr < 0 || nr >= Rows || nc < 0 || ni >= count) continue;
                            double kw = dc != 0 && dr != 0 ? 0.5 : 1;
                            sum += raw[ni] * kw; wsum += kw;
                        }
                    target = wsum > 0 ? 0.6 * raw[i] + 0.4 * sum / wsum : raw[i];
                    // a slow shimmer, each cell on its own beat
                    if (!Potato) target *= 0.9 + 0.1 * Math.Sin(secs * cellBeat[i] + cellPhase[i]);
                }
                // every cell fades towards its target at its own pace: new ticks appear and a reset clears the grid
                // unevenly, not all at once (snapped in self-tests)
                if (DemoNow != null || Potato) { cellHeat[i] = (float)target; cellLit[i] = (float)presence; }
                else { cellHeat[i] += (float)((target - cellHeat[i]) * cellPace[i]); cellLit[i] += (float)((presence - cellLit[i]) * cellPace[i]); }
                double t = cellHeat[i], lit = cellLit[i];
                if (lit < 0.999) using (var b = new SolidBrush(Color.FromArgb((int)(89 * (1 - lit)), 30, 32, 20))) g.FillEllipse(b, cell);
                if (lit < 0.01) continue;
                // a commander's tick in the second accent, a titan's in the alarm red; the rest on the acid-yellow scale
                // only a tick at least 4x the site's median: the spawn's own bounty, not the small ships dying with it
                int kind = i < count && first + i < tr.BountyTimes.Count && ticks[first + i] >= 4 * median ? stats.KindNear(tr.BountyTimes[first + i]) : 0;
                var c = kind == 2 ? Mix(Color.FromArgb(120, 10, 6), AlarmRed, 0.4 + 0.6 * t) : kind == 1 ? Mix(Color.FromArgb(30, 20, 120), BodyTheme.Accent2Light, t) : HeatColor(t);
                if (kind != 0) t = Math.Max(t, 0.75);
                SoftGlow(g, new PointF(cx, cy), d / 2 + U(3 + (float)t * 7), c, (int)(t * t * 255 * lit * 0.3));
                using (var b = new SolidBrush(Color.FromArgb((int)((0.6 + t * 0.4) * 255 * lit), c))) g.FillEllipse(b, cell);
                if (!Potato) using (var lg = new LinearGradientBrush(cell, Color.FromArgb((int)(56 * lit), 255, 255, 255), Color.FromArgb((int)(71 * lit), 0, 0, 0), 90f)) g.FillEllipse(lg, cell);
                if (i == count - 1) using (var pen = new Pen(BodyTheme.Ink, Math.Max(1f, U(0.5f)))) g.DrawEllipse(pen, cell);
            }
        }

        // compact: the grid folded into one row - each dot is one column of the full grid (10 ticks), at their average
        // ISK. A column with a commander's or a titan's tick takes its colour outright, full and a little larger, drawn
        // over its neighbours (owner, 28.10.2026)
        void PaintIskRow(Graphics g, RectangleF r)
        {
            const int Rows = 10, Cols = 26;
            var tr = stats.Tracker; var ticks = tr.Bounties;
            float gap = U(1.5f), cw = (r.Width - gap * (Cols - 1)) / Cols, d = Math.Min(cw, r.Height);
            int first = Math.Max(0, ticks.Count - Rows * Cols), count = ticks.Count - first;
            var sorted = ticks.Skip(first).OrderBy(v => v).ToList(); long median = sorted.Count > 0 ? sorted[sorted.Count / 2] : 0;
            var marked = new List<Tuple<RectangleF, Color, bool>>();   // drawn last: on top
            for (int col = 0; col < Cols; col++)
            {
                float cx = r.Left + col * (cw + gap) + cw / 2, cy = r.Top + r.Height / 2;
                var cell = new RectangleF(cx - d / 2, cy - d / 2, d, d);
                int a = col * Rows, b = Math.Min(count, a + Rows);
                if (a >= count) { using (var e = new SolidBrush(Color.FromArgb(89, 30, 32, 20))) g.FillEllipse(e, cell); continue; }
                double avg = 0; int kind = 0;
                for (int i = a; i < b; i++)
                {
                    avg += ticks[first + i];
                    if (first + i < tr.BountyTimes.Count && ticks[first + i] >= 4 * median) kind = Math.Max(kind, stats.KindNear(tr.BountyTimes[first + i]));
                }
                avg /= b - a;
                double t = Math.Max(0, Math.Min(1, (Math.Log(Math.Max(1, avg)) - Math.Log(3000)) / (Math.Log(270000) - Math.Log(3000))));
                if (kind != 0) { marked.Add(Tuple.Create(RectangleF.Inflate(cell, d * 0.15f, d * 0.15f), kind == 2 ? AlarmRed : BodyTheme.Accent2Light, b == count)); continue; }
                var c = HeatColor(t);
                SoftGlow(g, new PointF(cx, cy), d / 2 + U(3 + (float)t * 7), c, (int)(t * t * 255 * 0.3));
                using (var br = new SolidBrush(Color.FromArgb((int)((0.6 + t * 0.4) * 255), c))) g.FillEllipse(br, cell);
                if (!Potato) using (var lg = new LinearGradientBrush(cell, Color.FromArgb(56, 255, 255, 255), Color.FromArgb(71, 0, 0, 0), 90f)) g.FillEllipse(lg, cell);
                if (b == count) using (var pen = new Pen(BodyTheme.Ink, Math.Max(1f, U(0.5f)))) g.DrawEllipse(pen, cell);
            }
            foreach (var m in marked)
            {
                var mc = m.Item1;
                SoftGlow(g, new PointF(mc.X + mc.Width / 2, mc.Y + mc.Height / 2), mc.Width / 2 + U(9), m.Item2, 110);
                using (var br = new SolidBrush(m.Item2)) g.FillEllipse(br, mc);
                if (!Potato) using (var lg = new LinearGradientBrush(mc, Color.FromArgb(56, 255, 255, 255), Color.FromArgb(71, 0, 0, 0), 90f)) g.FillEllipse(lg, mc);
                if (m.Item3) using (var pen = new Pen(BodyTheme.Ink, Math.Max(1f, U(0.5f)))) g.DrawEllipse(pen, mc);
            }
        }

        // the accent as a scale: almost black -> saturated acid yellow
        static Color HeatColor(double t)
        {
            var stops = new[] { Color.FromArgb(14, 15, 8), Color.FromArgb(52, 56, 10), Color.FromArgb(128, 134, 14), Color.FromArgb(200, 208, 18), Color.FromArgb(247, 255, 20) };
            double x = Math.Max(0, Math.Min(0.999, t)) * (stops.Length - 1);
            int j = (int)x;
            return Mix(stops[j], stops[j + 1], x - j);
        }

        // NPC factions in the colours of their ships and emblems (no official palette exists): Guristas yellow-black,
        // Blood Raiders crimson, Angels rust red, Serpentis green, Sansha jade, Sleepers cold blue, ...
        static Color FactionColor(string f)
        {
            switch (f)
            {
                case "Serpentis": return Color.FromArgb(127, 178, 65);
                case "Guristas": return Color.FromArgb(226, 182, 48);
                case "Angel Cartel": return Color.FromArgb(181, 64, 47);
                case "Blood Raiders": return Color.FromArgb(163, 22, 33);
                case "Sansha": return Color.FromArgb(63, 160, 106);
                case "Rogue Drones": return Color.FromArgb(138, 154, 123);
                case "Sleepers": return Color.FromArgb(90, 169, 200);
                case "Drifters": return Color.FromArgb(62, 143, 176);
                case "Triglavian": return Color.FromArgb(208, 38, 42);
                case "Amarr": return Color.FromArgb(212, 167, 44);
                case "Caldari": return Color.FromArgb(62, 124, 192);
                case "Gallente": return Color.FromArgb(62, 158, 107);
                case "Minmatar": return Color.FromArgb(176, 74, 50);
                case "CONCORD": return Color.FromArgb(58, 123, 213);
                case "Mordu": return Color.FromArgb(111, 125, 140);
                default: return BodyTheme.BadgeFill;
            }
        }

        // prefix: a symbol Orbitron has no glyph for (Σ), drawn in Segoe UI before the text
        // icon: a Material icon in place of the prefix
        float Badge(Graphics g, string text, Font f, float x, float y, float h, bool filled, string prefix = null, Color? fill = null, string icon = null)
        {
            text = text.ToUpperInvariant();
            var sf = StringFormat.GenericTypographic;
            var pf = prefix != null ? new Font("Segoe UI Semibold", f.Size, GraphicsUnit.Pixel) : null;
            try
            {
                float pw = pf != null ? g.MeasureString(prefix, pf, 1000, sf).Width + U(2.5f) : icon != null ? U(9) + U(2.5f) : 0;
                var sz = g.MeasureString(text, f, 1000, sf);
                var r = new RectangleF(x, y, pw + sz.Width + U(9), h);
                using (var p = Round(r, 4))
                {
                    if (filled) using (var b = new SolidBrush(fill ?? BodyTheme.BadgeFill)) g.FillPath(b, p);
                    else using (var pen = new Pen(Color.FromArgb(89, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
                }
                if (pf != null) using (var b = new SolidBrush(BodyTheme.Ink)) g.DrawString(prefix, pf, b, r.Left + U(4.5f), r.Top + (h - pf.GetHeight(g)) / 2, sf);
                if (icon != null) using (var b = new SolidBrush(BodyTheme.Ink)) MaterialIcon.Draw(g, icon, new RectangleF(r.Left + U(4), r.Top + (h - U(9)) / 2, U(9), U(9)), b);
                // dark text on a light faction colour, white otherwise
                var ink = fill != null && (fill.Value.R * 0.299 + fill.Value.G * 0.587 + fill.Value.B * 0.114) > 150 ? Color.FromArgb(16, 17, 20) : BodyTheme.Ink;
                Glow(g, text, f, ink, r.Left + U(4.5f) + pw, r.Top + (h - f.GetHeight(g)) / 2 + U(0.5f));
                return r.Right;
            }
            finally { if (pf != null) pf.Dispose(); }
        }

        protected override void Dispose(bool disposing) { if (disposing) { timer.Dispose(); tip.Dispose(); if (shell != null) shell.Dispose(); } base.Dispose(disposing); }

        // ================= self-test =================

        // --selftest-dps <dir>: the parser on sample log lines, the smartbomb module count, and the window drawn with
        // made-up combat for 3, 5 and 8 pilots plus its states
        public static void SelfTest(string dir)
        {
            SelfTesting = true;
            Directory.CreateDirectory(dir);
            var sb = new StringBuilder();
            var samples = new[]
            {
                "[ 2026.10.24 14:46:35 ] (combat) <color=0xff00ffff><b>1234</b> <color=0x77ffffff><font size=10>to</font> <b><color=0xffffffff>Sleepless Warden</b><font size=10><color=0x77ffffff> - Ultratidal Entropic Disintegrator II - Hits",
                "[ 2026.10.24 14:46:36 ] (combat) <color=0xffcc0000><b>345</b> <color=0x77ffffff><font size=10>from</font> <b><color=0xffffffff>Emergent Warden</b><font size=10><color=0x77ffffff> - Smashes",
                "[ 2026.10.24 14:46:37 ] (combat) <color=0xff00ffff><b>88</b> <color=0x77ffffff><font size=10>to</font> <b><color=0xffffffff>Awakened Warden</b><font size=10><color=0x77ffffff> - Quad 800mm Repeating Cannon II - Glances Off",
                "[ 2026.10.24 14:46:38 ] (combat) Your group of Quad 800mm Repeating Cannon II misses Awakened Warden completely - Quad 800mm Repeating Cannon II",
                "[ 2026.10.24 14:46:39 ] (combat) Sleepless Sentinel misses you completely",
                "[ 2026.10.24 14:46:40 ] (notify) Your Siege Module II is now active",
                "[ 2026.10.26 09:12:00 ] (combat) <color=0xff00ffff><b>13295</b> <color=0x77ffffff><font size=10>to</font> <b><color=0xffffffff>Pilot-5[DEMO](Nestor)</b><font size=10><color=0x77ffffff> - Large Vorton Projector II - Penetrates",
                "[ 2026.10.26 09:12:00 ] (combat) <color=0xff00ffff><b>13159</b> <color=0x77ffffff><font size=10>to</font> <b><color=0xffffffff>Core Grand Admiral</b><font size=10><color=0x77ffffff> - Large Vorton Projector II - Hits",
                "[ 2026.10.26 09:12:05 ] (combat) <color=0xff00ffff><b>224</b> <color=0x77ffffff><font size=10>to</font> <b><color=0xffffffff>Core Lord Admiral</b><font size=10><color=0x77ffffff> - Caldari Navy Large Graviton Smartbomb - Hits",
                "[ 2026.10.26 09:12:05 ] (combat) <color=0xffcc0000><b>224</b> <color=0x77ffffff><font size=10>from</font> <b><color=0xffffffff>Pilot-5[DEMO](Nestor)</b><font size=10><color=0x77ffffff> - Caldari Navy Large Graviton Smartbomb - Hits",
                "[ 2026.10.26 09:12:06 ] (combat) <color=0xffcc0000><b>580</b> <color=0x77ffffff><font size=10>from</font> <b><color=0xffffffff>Core Grand Admiral</b><font size=10><color=0x77ffffff> - Smashes",
            };
            foreach (var l in samples)
            {
                var e = GameLogParser.Parse(l, "Pilot-1");
                if (e == null) { sb.AppendLine("- (not damage)"); continue; }
                bool isFriendly = DpsStats.IsPlayerOrFriendly(e.Other);
                string faction = DpsStats.IdentifyFaction(e.Other);
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} [{3}] weapon:'{4}' faction:{5} -> friendly:{6} kept_in_thundpack:{7}",
                    e.T.ToString("HH:mm:ss"), e.Out ? "out" : "in", e.Amount, e.Quality, e.Weapon, faction ?? "none", isFriendly, !isFriendly));
            }

            var now = new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);
            var q = new[] { "Hits", "Smashes", "Penetrates", "Glances Off", "Misses" };
            Func<string[], List<DmgEvent>> makeDemo = pls =>
            {
                var rnd = new Random(7); var list = new List<DmgEvent>();
                for (int sec = 60; sec >= 0; sec--)
                    for (int i = 0; i < pls.Length; i++)
                    {
                        double ramp = 1 - sec / 90.0;
                        if (rnd.NextDouble() < 0.8)
                        {
                            var kq = q[rnd.Next(q.Length - (i == 2 ? 0 : 1))];
                            list.Add(new DmgEvent { T = now.AddSeconds(-sec), Pilot = pls[i], Out = true, Quality = kq, Other = "Sleepless Warden", Weapon = "Large Vorton Projector II", Amount = kq == "Misses" ? 0 : (2200 + 900 * (i % 3)) * ramp * (0.7 + rnd.NextDouble() * 0.6) });
                        }
                    }
                return list;
            };

            // smartbombs as in a real log (7 x Caldari Navy Large Graviton): every 9 s, 7 hits per NPC in range;
            // the last cycle loses NPCs halfway (modules 4-7 hit fewer of them)
            Func<string, List<DmgEvent>> makeSmartbombs = pilot =>
            {
                var list = new List<DmgEvent>();
                var npcs = new[] { "Core Grand Admiral", "Corelatis High Captain", "Corelatis High Captain", "Corelatis Captain Sentry", "Core Grand Admiral", "Core Grand Admiral" };
                for (int c = 5; c >= 0; c--)
                {
                    var t = now.AddSeconds(-c * 9 - 1);
                    for (int m = 0; m < 7; m++)
                        for (int i = 0; i < npcs.Length; i++)
                        {
                            if (c == 0 && m >= 3 && i >= 2 + (m - 3) / 2) continue;
                            list.Add(new DmgEvent { T = t, Pilot = pilot, Out = true, Amount = npcs[i].StartsWith("Core Grand") ? 195 : 263, Other = npcs[i], Quality = "Hits", Weapon = "Caldari Navy Large Graviton Smartbomb" });
                        }
                }
                // friendly fire on the fleet is not counted
                list.Add(new DmgEvent { T = now.AddSeconds(-1), Pilot = pilot, Out = true, Amount = 224, Other = "Pilot-1[DEMO](Thunderchild)", Quality = "Hits", Weapon = "Caldari Navy Large Graviton Smartbomb" });
                return list;
            };
            // the bounty ticks of one real anomaly (all clients, in time order)
            long[] realTicks = { 27000, 27000, 27000, 27000, 27000, 27750, 27750, 27750, 27750, 27750, 27750, 27750, 27750, 27750, 27750, 29250, 29250, 29250, 29250, 29250, 240000, 240000, 240000, 240000, 240000, 240000, 240000, 240000, 240000, 240000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 5175, 5175, 5175, 5175, 5175, 5400, 5400, 5400, 5400, 5400, 5400, 5400, 5400, 5400, 5400, 5625, 5625, 5625, 5625, 5625, 240000, 240000, 240000, 240000, 240000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 3600, 3600, 3600, 3600, 3600, 4050, 4050, 4050, 4050, 4050, 4050, 4050, 4050, 4050, 4050, 4275, 4275, 4275, 4275, 4275, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 4500, 4500, 4500, 4500, 4500, 4500, 4500, 4500, 4500, 4500, 4725, 4725, 4725, 4725, 4725, 4725, 4725, 4725, 4725, 4725, 240000, 240000, 240000, 240000, 240000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 42750, 42750, 42750, 42750, 42750, 42750, 42750, 42750, 42750, 42750, 45000, 45000, 45000, 45000, 45000, 47250, 47250, 47250, 47250, 47250, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 27750, 27750, 27750, 27750, 27750, 30750, 30750, 30750, 30750, 30750, 27000, 27000, 27000, 27000, 27000, 27750, 27750, 27750, 27750, 27750, 29250, 29250, 29250, 29250, 29250, 27750, 27750, 27750, 27750, 27750, 30750, 30750, 30750, 30750, 30750, 27750, 27750, 27750, 27750, 27750, 240000, 240000, 240000, 240000, 240000, 260000, 260000, 260000, 260000, 260000, 240000, 240000, 240000, 240000, 240000, 240000, 240000, 240000, 240000, 240000, 240000, 240000, 240000, 240000, 240000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000, 260000 };

            Action<DpsForm, int> fill = (f, nTicks) =>
            {
                f.Stats.Tracker.ResetAll();
                f.Stats.Tracker.AnomalyCount = 1;
                long sum = 0;
                for (int i = 0; i < nTicks; i++) { f.Stats.Tracker.Bounties.Add(realTicks[i]); f.Stats.Tracker.BountyTimes.Add(now.AddSeconds(-4 * (nTicks - i))); sum += realTicks[i]; }
                f.Stats.Tracker.CurrentAnomalyIsk = sum;
                f.Stats.Tracker.TotalSessionIsk = 951781750;
            };
            Action<DpsForm, string> save = (f, name) => { using (var b = f.Draw()) b.Save(Path.Combine(dir, name), ImageFormat.Png); };

            var pilots3 = new[] { "Pilot-1", "Pilot-2", "Pilot-3" };
            var pilots5 = new[] { "Pilot-1", "Pilot-2", "Pilot-3", "Pilot-4", "Pilot-5" };
            var pilots8 = new[] { "Pilot-1", "Pilot-2", "Pilot-3", "Pilot-4", "Pilot-5", "Pilot-6", "Pilot-7", "Pilot-8" };

            // 1) 5 pilots in a site: smartbombs, the whole anomaly's ticks, the lamp lit, the animation running
            using (var f = new DpsForm(pilots5, () => null, true) { DemoNow = now, DemoSeconds = 1.3, DemoFollowing = true })
            {
                f.AddDemo(makeDemo(pilots5)); f.AddDemo(makeSmartbombs("Pilot-5"));
                fill(f, realTicks.Length);
                f.Stats.Spawns.Add(Tuple.Create(now.AddMinutes(-62), "Shadow Serpentis Vice Admiral"));
                f.Stats.Spawns.Add(Tuple.Create(now.AddMinutes(-24), "Shadow Serpentis Flotilla Admiral"));
                save(f, "dps_window.png");
                f.ShowHoverSpawns = true; save(f, "dps_spawns_hover.png"); f.ShowHoverSpawns = false;
                // the version plate as it is, then blinking for a newer version (bright, dim, under the mouse); the General
                // switch; About with the download key
                save(f, "dps_version_plate.png");
                Potato = true; if (f.shell != null) { f.shell.Dispose(); f.shell = null; }
                save(f, "dps_potato.png");
                f.DemoSettings(true, null, false, "general"); save(f, "dps_potato_general.png"); f.DemoSettings(false, null, false);
                Potato = false; if (f.shell != null) { f.shell.Dispose(); f.shell = null; }
                Compact = true; f.Size = new Size(f.DesiredWidth, f.DesiredHeight); if (f.shell != null) { f.shell.Dispose(); f.shell = null; }
                save(f, "dps_compact.png");
                f.DemoSettings(true, null, false, "general"); save(f, "dps_compact_general.png"); f.DemoSettings(false, null, false);
                Compact = false; f.Size = new Size(f.DesiredWidth, f.DesiredHeight); if (f.shell != null) { f.shell.Dispose(); f.shell = null; }
                f.DemoUpdate("1.3.0");
                var keepSecs = f.DemoSeconds;
                f.DemoSeconds = 0; save(f, "dps_version_plate_new_bright.png");
                f.DemoSeconds = 1.2; save(f, "dps_version_plate_new_dim.png");
                f.ShowHoverUpdate = true; save(f, "dps_version_plate_hover.png"); f.ShowHoverUpdate = false;
                var pd = Path.Combine(dir, "plate_frames"); Directory.CreateDirectory(pd);
                var band = Rectangle.Intersect(Rectangle.Round(RectangleF.FromLTRB(f.Width * 0.5f, f.BandRect.Top - f.U(4), f.Width, f.BandRect.Bottom + f.U(4))), new Rectangle(0, 0, f.Width, f.Height));
                for (int i = 0; i < 60; i++) { f.DemoSeconds = i * 0.08; using (var fr = f.Draw()) using (var c = fr.Clone(band, fr.PixelFormat)) c.Save(Path.Combine(pd, i.ToString("00") + ".png"), ImageFormat.Png); }
                f.DemoSeconds = keepSecs;
                f.DemoSettings(true, null, false, "general"); save(f, "dps_update_general.png");
                f.DemoSettings(true, null, false, "about"); save(f, "dps_update_about.png");
                f.DemoSettings(false, null, false);
                var r = f.Stats.Smartbombs(now);
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "smartbombs: pilot {0}, modules {1}, total {2:0} DPS, per module {3}",
                    r.Pilot, r.Modules, r.Total, string.Join(" ", r.Dps.Select(v => v.ToString("0", CultureInfo.InvariantCulture)))));
                if (r.Modules != 7) throw new Exception("Expected 7 smartbombs, got " + r.Modules);
                // the animation moves; hovers; the glass lit by the mouse
                f.DemoSeconds = 2.9; save(f, "dps_power_frame2.png"); f.DemoSeconds = 1.3;
                f.ShowHoverName = true; save(f, "dps_hover_name.png"); f.ShowHoverName = false;
                // the key pointer's loop: 12 frames at 4x on a dark strip, the time under each
                {
                    const int n = 12; float z = 4;
                    int cw = (int)(32 * z);
                    using (var strip = new Bitmap(cw * n, cw + 24, PixelFormat.Format32bppArgb))
                    using (var sg = Graphics.FromImage(strip))
                    using (var tf = new Font("Segoe UI", 11, GraphicsUnit.Pixel))
                    {
                        sg.Clear(Color.FromArgb(38, 40, 46)); sg.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                        for (int i = 0; i < n; i++)
                        {
                            double ft = (double)i / n;
                            using (var fr = Glass.PulseFrame(ft, BodyTheme.Pointer, BodyTheme.PointerOutline, z)) sg.DrawImageUnscaled(fr, i * cw, 0);
                            sg.DrawString((int)Math.Round(ft * Glass.PulseLoop * 1000) + " ms", tf, Brushes.Gainsboro, i * cw + 6, cw + 4);
                        }
                        strip.Save(Path.Combine(dir, "cursor_pulse_storyboard.png"), ImageFormat.Png);
                    }
                }
                // the settings in place of the meter: as opened, a language row under the mouse, after a reset
                f.DemoSettings(true, null, false); save(f, "dps_settings.png");
                f.DemoSettings(true, "lang:de", false); save(f, "dps_settings_hover.png");
                // the body at 100 % and 150 %
                foreach (var sc in new[] { 1f, 1.5f })
                    using (var fs = new DpsForm(pilots5, () => null, true) { DemoNow = now, DemoSeconds = 1.3, DemoFollowing = true })
                    {
                        UiScale = sc; fs.Size = new Size(fs.DesiredWidth, fs.DesiredHeight);
                        fs.AddDemo(makeDemo(pilots5)); fs.AddDemo(makeSmartbombs("Pilot-5")); fs.Stats.DetectedFaction = "Serpentis";
                        fs.DemoSettings(true, null, false); save(fs, "dps_settings_scale_" + (int)(sc * 100) + ".png");
                    }
                UiScale = 1.25f;
                f.DemoLogDirOk = true; f.DemoSettings(true, "reset", true, "logs"); save(f, "dps_settings_logs.png");
                f.DemoLogDirOk = false; f.DemoSettings(true, "dir:detect", false, "logs"); save(f, "dps_settings_logs_missing.png"); f.DemoLogDirOk = null;
                f.DemoSessions = new List<SessionInfo>
                {
                    new SessionInfo { Start = now.AddDays(-1).AddHours(-3), End = now.AddDays(-1), Sites = 14, Isk = 1840000000, Spawns = 5 },
                    new SessionInfo { Start = now.AddDays(-2).AddHours(-2), End = now.AddDays(-2).AddMinutes(-20), Sites = 9, Isk = 1120000000, Spawns = 4 },
                    new SessionInfo { Start = now.AddDays(-4).AddHours(-1), End = now.AddDays(-4), Sites = 5, Isk = 610000000 },
                };
                f.DemoSettings(true, null, false, "stats"); save(f, "dps_settings_stats.png"); f.DemoSessions = null;
                f.DemoSettings(true, "data", false, "storage"); save(f, "dps_settings_storage.png");
                f.DemoSettings(true, "page:general", false, "about"); save(f, "dps_settings_about.png");
                f.DemoAboutView = "credits"; save(f, "dps_settings_about_credits.png");
                f.DemoAboutView = "legal"; save(f, "dps_settings_about_legal.png"); f.DemoAboutView = "news";
                f.DemoSettings(false, null, false);
                f.ShowHoverClose = true; save(f, "dps_hover_close.png"); f.ShowHoverClose = false;
                f.ShowHoverFx = true; save(f, "dps_hover_formula.png"); f.ShowHoverFx = false;
                // a commander and a titan in the site: their ticks on the grid, the hull heated
                {
                    var bt = f.Stats.Tracker.BountyTimes;
                    for (int qi = 0; qi < bt.Count; qi++) if (qi % 23 == 5) { f.Stats.DemoSpecial(bt[qi], 1); f.Stats.Tracker.Bounties[qi] = 1875000; } else if (qi % 37 == 11) { f.Stats.DemoSpecial(bt[qi], 2); f.Stats.Tracker.Bounties[qi] = 9000000; }
                    save(f, "dps_commander_titan_ticks.png");
                    f.DemoHeat = 0.45; save(f, "dps_titan_heating.png");
                    f.DemoHeat = 1; save(f, "dps_titan_hot.png");
                    f.DemoHeat = null;
                    f.DemoHeatY = 0.45; save(f, "dps_dread_heating.png");
                    f.DemoHeatY = 1; save(f, "dps_dread_hot.png");
                    f.DemoHeatY = null;
                    // compact: one commander and one titan in the site (the other demo spawns' ticks made ordinary)
                    var keepB = f.Stats.Tracker.Bounties.ToList(); int keptC = 0, keptT = 0;
                    long med = keepB.OrderBy(v => v).ElementAt(keepB.Count / 2);
                    for (int qi = 0; qi < bt.Count; qi++)
                    {
                        bool c1 = qi % 23 == 5, t2 = !c1 && qi % 37 == 11;
                        if (c1 && keptC++ == 2) continue;
                        if (t2 && keptT++ == 4) continue;
                        if (c1 || t2) f.Stats.Tracker.Bounties[qi] = med;
                    }
                    Compact = true; f.Size = new Size(f.DesiredWidth, f.DesiredHeight); if (f.shell != null) { f.shell.Dispose(); f.shell = null; }
                    save(f, "dps_compact_commander_titan.png");
                    for (int qi = 0; qi < keepB.Count; qi++) f.Stats.Tracker.Bounties[qi] = keepB[qi];
                    Compact = false; f.Size = new Size(f.DesiredWidth, f.DesiredHeight); if (f.shell != null) { f.shell.Dispose(); f.shell = null; }
                }
                // the damage display for other factions
                foreach (var fa in new[] { "Guristas", "Sleepers", "Rogue Drones", "Angel Cartel" }) { f.Stats.DetectedFaction = fa; save(f, "dps_damage_" + fa.Replace(" ", "_").ToLowerInvariant() + ".png"); }
                f.Stats.DetectedFaction = "Serpentis";
                // smartbombs: 0.2 s after a new cycle replaced a stronger one (the columns ease down)
                var prev = new[] { 1.0, 0.95, 1.0, 0.9, 1.0, 0.85, 0.8 };
                f.DemoSmartbombChange(prev, 0.2); save(f, "dps_sb_change_0.2s.png");
                f.DemoSmartbombChange(f.Stats.Smartbombs(now).Frac, 30);
                var gr = f.GlassRect;
                f.DemoMouse = new Point((int)(gr.Left + gr.Width * 0.35f), (int)(gr.Top + gr.Height * 0.3f)); save(f, "dps_glass_light.png"); f.DemoMouse = null;
                // half the anomaly
                fill(f, 52); save(f, "dps_anomaly_early.png");
            }
            // the anomaly timer: 47 s after a warp with the graph gone quiet, the fade, 3:12 into a site
            using (var f = new DpsForm(pilots5, () => null, true) { DemoNow = now, DemoSeconds = 0.5, DemoFollowing = true })
            {
                f.Stats.DetectedFaction = "Serpentis";
                // the site's lines, then a warp (no hits since): the lines sinking, down, the timer in; then the first
                // hit: the timer out, the lines back
                f.AddDemo(makeDemo(pilots5)); f.AddDemo(makeSmartbombs("Pilot-5"));
                f.DemoQuiet = 0; f.DemoTimerA = 0; save(f, "dps_timer_0_site.png");
                f.Stats.Tracker.PrevWarpTime = now.AddSeconds(-1 - 402);   // a site of 6:42
                f.Stats.Tracker.LastWarpTime = now.AddSeconds(-1);
                f.DemoQuiet = 0.5f; save(f, "dps_timer_1_sinking.png");
                f.DemoQuiet = 1; f.DemoTimerA = 0.45f; f.DemoCount = 0.15f; save(f, "dps_timer_2_fade_in.png");
                f.DemoTimerA = 1; f.DemoCount = 0.5f; save(f, "dps_timer_3_counting.png");
                f.DemoCount = 1; save(f, "dps_timer_3_clear_time.png");
                // a re-warp of 0:48 (no hits between the warps): red, 00:48 at once, then down to 00:00 in real time
                var keepPrev = f.Stats.Tracker.PrevWarpTime;
                f.Stats.Tracker.PrevWarpTime = f.Stats.Tracker.LastWarpTime.AddSeconds(-48); f.DemoRewarp = true;
                f.DemoCount = 0; save(f, "dps_rewarp_0.png");
                f.DemoCount = 0.3f; save(f, "dps_rewarp_1.png");
                f.DemoCount = 0.6f; save(f, "dps_rewarp_2.png");
                f.DemoCount = 1; save(f, "dps_rewarp_3.png");
                f.DemoRewarp = null; f.Stats.Tracker.PrevWarpTime = keepPrev;
                f.DemoTimerA = 0.4f; save(f, "dps_timer_4_first_hit.png");
                f.DemoTimerA = 0; f.DemoQuiet = 0.5f; save(f, "dps_timer_5_back.png");
                f.DemoQuiet = null; f.DemoTimerA = null; f.DemoCount = null;
                f.Stats.Tracker.LastWarpTime = DateTime.MinValue; f.DemoQuiet = 0; f.DemoTimerA = 0; save(f, "dps_sb_line.png");
            }
            // 2) 3 and 8 pilots, no smartbombs, an empty grid
            using (var f = new DpsForm(pilots3, () => null, true) { DemoNow = now, DemoSeconds = 0.5, DemoFollowing = true })
            {
                f.AddDemo(makeDemo(pilots3));
                save(f, "dps_3pilots.png");
            }
            using (var f = new DpsForm(pilots8, () => null, true) { DemoNow = now, DemoSeconds = 0.5, DemoFollowing = true })
            {
                f.AddDemo(makeDemo(pilots8)); f.AddDemo(makeSmartbombs("Pilot-8"));
                fill(f, 140);
                save(f, "dps_8pilots.png");
            }
            // 3) waiting for a combat log: lamp dim, animation still; no EVE client: the lamp flashes red
            using (var f = new DpsForm(new string[0], () => null, true) { DemoNow = now, DemoSeconds = 0, DemoFollowing = false })
            {
                save(f, "dps_waiting.png");
                f.DemoNoClients = true; f.DemoSeconds = 0.035; save(f, "dps_no_clients_flash.png");
                f.DemoSeconds = 4.6; save(f, "dps_no_clients_dark.png");
                // the power display's short circuit: 40 frames, 40 ms apart, cropped and 3x
                {
                    var pr = Rectangle.Round(RectangleF.Inflate(f.PowerRect, f.U(3), f.U(3)));
                    const int n = 40; int z = 3;
                    double t0 = 0; while (ShortLine(t0) == null && t0 < 60) t0 += 0.01; t0 = Math.Max(0, t0 - 0.2);   // from just before the first broken line
                    using (var strip = new Bitmap(pr.Width * z * 8, (pr.Height * z + 18) * (n / 8), PixelFormat.Format32bppArgb))
                    using (var sg = Graphics.FromImage(strip))
                    using (var tf = new Font("Segoe UI", 11, GraphicsUnit.Pixel))
                    {
                        sg.Clear(Color.FromArgb(38, 40, 46)); sg.InterpolationMode = InterpolationMode.NearestNeighbor;
                        for (int i = 0; i < n; i++)
                        {
                            f.DemoSeconds = t0 + i * 0.04;
                            using (var fr = f.Draw())
                            {
                                int cx = (i % 8) * pr.Width * z, cy = (i / 8) * (pr.Height * z + 18);
                                sg.DrawImage(fr, new Rectangle(cx, cy, pr.Width * z, pr.Height * z), pr, GraphicsUnit.Pixel);
                                sg.DrawString((int)Math.Round((t0 + i * 0.04) * 1000) + " ms", tf, Brushes.Gainsboro, cx + 4, cy + pr.Height * z + 2);
                            }
                        }
                        strip.Save(Path.Combine(dir, "power_short_storyboard.png"), ImageFormat.Png);
                    }
                    // the top strip (lamp and both small displays) for 8 s at 40 ms, for an animated preview
                    var top = new Rectangle(0, 0, f.Width, (int)(pr.Bottom + f.U(6)));
                    var fd = Path.Combine(dir, "short_frames"); Directory.CreateDirectory(fd);
                    for (int i = 0; i < 200; i++) { f.DemoSeconds = t0 + i * 0.04; using (var fr = f.Draw()) using (var c = fr.Clone(top, fr.PixelFormat)) c.Save(Path.Combine(fd, i.ToString("000") + ".png"), ImageFormat.Png); }
                    var frames = new StringBuilder();
                    for (int i = 0; i < 125; i++) frames.Append(i == 0 ? "" : ",").Append(ShortLevel(i * 0.04).ToString("0.00", CultureInfo.InvariantCulture));
                    File.WriteAllText(Path.Combine(dir, "power_short_levels.txt"), frames.ToString());
                }
                var lv = new StringBuilder("no-client lamp, 0.05 s steps over one burst: ");
                for (double t = 0; t < 5.2; t += 0.05) lv.Append(AlarmLevel(t) > 0.5 ? "#" : AlarmLevel(t) > 0.05 ? "+" : ".");
                sb.AppendLine(lv.ToString());
            }
            // 4) the formula windows over the body: on the way in, and open
            using (var f = new DpsForm(pilots5, () => null, true) { DemoNow = now, DemoSeconds = 1.3, DemoFollowing = true })
            {
                f.AddDemo(makeDemo(pilots5)); f.AddDemo(makeSmartbombs("Pilot-5"));
                fill(f, realTicks.Length);
                using (var b = f.Draw())
                {
                    FormulaWindow.SaveDemo(b, FormulaWindow.Smartbombs, 0.45, Path.Combine(dir, "dps_formula_opening.png"));
                    FormulaWindow.SaveDemo(b, FormulaWindow.Smartbombs, 1, Path.Combine(dir, "dps_formula_smartbombs.png"));
                    FormulaWindow.SaveDemo(b, FormulaWindow.Vorton, 1, Path.Combine(dir, "dps_formula_vorton.png"));
                }
            }
            // the owner's log 21:22 - 21:24: after the warp a cut cycle (5,5,10,14,14), one with NPCs dying mid-volley
            // (3,6,9 -> votes 3), then (21) and (7,21,21) -> 7 modules, not 3
            {
                var st = new DpsStats(new[] { "Pilot-1" }) { Thundpack = true };
                var t0 = now.AddSeconds(-60);
                st.Tracker.OnWarp("Pilot-4", t0);
                var cyclesCounts = new[] { new[] { 5, 5, 10, 14, 14 }, new[] { 3, 6, 9 }, new[] { 21 }, new[] { 7, 21, 21 } };
                var evs = new List<DmgEvent>();
                for (int c = 0; c < cyclesCounts.Length; c++)
                    for (int i = 0; i < cyclesCounts[c].Length; i++)
                        for (int h = 0; h < cyclesCounts[c][i]; h++)
                            evs.Add(new DmgEvent { T = t0.AddSeconds(10 + c * 9), Pilot = "Pilot-1", Out = true, Amount = 200, Other = "NPC " + i, Quality = "Hits", Weapon = "Caldari Navy Large Graviton Smartbomb" });
                st.Add(evs, now);
                var rr = st.Smartbombs(now);
                sb.AppendLine("smartbombs after a cut cycle: modules " + rr.Modules);
                if (rr.Modules != 7) throw new Exception("Expected 7 smartbombs after cut cycles, got " + rr.Modules);
            }
            File.WriteAllText(Path.Combine(dir, "selftest_dps.txt"), sb.ToString());

            if (FormatIsk(38300000) != "38.3 m. isk") throw new Exception("Expected '38.3 m. isk', got '" + FormatIsk(38300000) + "'");
            if (FormatIsk(1250000000) != "1.3 b. isk") throw new Exception("Expected '1.3 b. isk', got '" + FormatIsk(1250000000) + "'");
            if (FormatIsk(45000) != "45.0 k. isk") throw new Exception("Expected '45.0 k. isk', got '" + FormatIsk(45000) + "'");
            if (FormatIsk(0) != "0.0 m. isk") throw new Exception("Expected '0.0 m. isk', got '" + FormatIsk(0) + "'");
            var dmgObj = GameLogParser.ParseLine("[ 2026.10.26 09:11:55 ] (combat) 613 to Pilot-5[DEMO](Nestor) - Large Vorton Projector II - Hits", "Pilot-1");
            if (!(dmgObj is DmgEvent)) throw new Exception("Expected DmgEvent");
            var bEv = GameLogParser.ParseLine("[ 2026.10.26 09:11:48 ] (bounty) 29 250 ISK added to next bounty payout (payment adjusted)", "Pilot-1") as BountyEvent;
            if (bEv == null || bEv.Amount != 29250) throw new Exception("Expected BountyEvent with 29250 ISK");
            var wEv = GameLogParser.ParseLine("[ 2026.10.26 09:09:47 ] (notify) Following Pilot-4 in warp", "Pilot-1") as WarpFollowEvent;
            if (wEv == null || wEv.Leader != "Pilot-4") throw new Exception("Expected WarpFollowEvent with Pilot-4");
        }
    }
}
