// TuneWindow.cs - the Tuning window (owner, 20.11.2026): every parameter of the meter's logic (Tune.cs) on a page per
// group, with its value, its limits, its default and what it does, and the Inspector: what the logic sees right now
// (the log lines as parsed, the followed files, the smartbomb cycles and their votes, the anomaly timer's state, the
// alerts' heat, the Lightning rod). Only with tuning on (--tune or a test build); English only, as a developer tool.
// Changes apply at once and are kept in tuning.json. The formula cards' texts still show the default numbers.
// Keys: drag a track to set a value, Ctrl+wheel over a row steps it (Shift: x10), a double click on the value types it,
// ↺ resets it. The window is a normal, activatable window: it takes the keyboard (the body never does).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TheNorns
{
    // the last log lines as the parser read them (only with tuning on)
    static class Inspect
    {
        public sealed class LogLine { public DateTime Wall; public string Pilot, Kind, Text; }
        static readonly LinkedList<LogLine> lines = new LinkedList<LogLine>();
        const int Keep = 400;
        static readonly Regex Tag = new Regex("<[^>]*>");
        public static long Total;

        public static void Line(string pilot, string raw, object parsed)
        {
            string kind = parsed is DmgEvent ? (((DmgEvent)parsed).Out ? "DMG>" : "DMG<") : parsed is BountyEvent ? "ISK " : parsed is WarpFollowEvent ? "WARP" : "    ";
            var text = Regex.Replace(Tag.Replace(raw ?? "", ""), @"\s+", " ").Trim();
            if (text.Length > 200) text = text.Substring(0, 200) + "…";
            lines.AddLast(new LogLine { Wall = DateTime.Now, Pilot = pilot, Kind = kind, Text = text });
            while (lines.Count > Keep) lines.RemoveFirst();
            Total++;
        }
        public static IEnumerable<LogLine> Last(int n) { return lines.Skip(Math.Max(0, lines.Count - n)); }
        public static void Demo(IEnumerable<LogLine> l) { lines.Clear(); foreach (var x in l) lines.AddLast(x); }
    }

    partial class DpsForm
    {
        TuneWindow tuneWin;
        internal void OpenTuning()
        {
            if (!Tune.Enabled) return;
            if (tuneWin == null || tuneWin.IsDisposed) tuneWin = new TuneWindow(this);
            tuneWin.Show();
            tuneWin.Activate();
        }

        // ---- the Inspector's report: sections of coloured lines (English or Ukrainian: TuneWindow.T) ----
        internal sealed class InspectRow { public string Text; public Color Color; public bool Head; }

        internal List<InspectRow> InspectReport(DateTime now)
        {
            var r = new List<InspectRow>();
            Color ink = BodyTheme.Ink, dim = Color.FromArgb(150, 154, 162), acc = BodyTheme.Accent, red = BodyTheme.Alarm, dr = BodyTheme.Dread;
            Action<string> head = t => r.Add(new InspectRow { Text = TuneWindow.T(t), Color = acc, Head = true });
            Action<string, Color> line = (t, c) => r.Add(new InspectRow { Text = t, Color = c });
            Func<string, object[], string> F = (en, a) => string.Format(CultureInfo.InvariantCulture, TuneWindow.T(en), a);
            Func<string, string> T = TuneWindow.T;
            Func<DateTime, string> ago = t => t == DateTime.MinValue ? T("never") : InsAgo((now - t).TotalSeconds);
            var inv = CultureInfo.InvariantCulture;

            head("TUNING");
            int changed = Tune.All.Count(p => p.Changed);
            line(F("file      {0}", new object[] { Tune.FilePath }), dim);
            line(F("changed   {0} of {1}   version {2}", new object[] { changed, Tune.All.Count, Tune.Version }), changed > 0 ? acc : ink);
            if (Tune.LastError != null) line(F("error     {0}", new object[] { Tune.LastError }), red);

            head("LOG");
            line(F("folder    {0}   {1}", new object[] { GameLogTail.Dir ?? "-", tail.Status ?? "" }), ink);
            line(F("clients   {0}   lines read {1}", new object[] { clients < 0 ? "-" : clients.ToString(inv), Inspect.Total.ToString(inv) }), ink);
            foreach (var f in tail.FollowedFiles)
                line("  " + InsPad(f.Item1, 22) + System.IO.Path.GetFileName(f.Item2) + "  @" + (f.Item3 / 1024.0).ToString("0.0", inv) + " KB", ink);
            if (!tail.FollowedFiles.Any()) line("  " + T("no log followed"), dim);
            foreach (var l in Inspect.Last(14))
                line(l.Wall.ToString("HH:mm:ss", inv) + " " + l.Kind + " " + InsPad(l.Pilot, 14) + l.Text, l.Kind.StartsWith("DMG") ? ink : l.Kind == "ISK " ? acc : l.Kind == "WARP" ? BodyTheme.Faction : dim);

            head("VORTON PROJECTOR");
            line(F("fleet out {0} DPS   in {1} DPS   (last {2} s, kernel ±{3} s)", new object[] { Num(stats.Now(null, true, now)), Num(stats.Now(null, false, now)), Tune.Format(Tune.VortonNowS, Tune.VortonNowS.V), Tune.Format(Tune.VortonKernelS, Tune.VortonKernelS.V) }), ink);
            foreach (var p in stats.Pilots) line("  " + InsPad(p, 22) + F("{0} DPS out", new object[] { Num(stats.Now(p, true, now)) }), ink);
            line(F("last NPC hit {0}   faction {1}", new object[] { ago(stats.LastNpcHit), stats.DetectedFaction ?? "-" }), ink);

            head("SMARTBOMBS");
            var sb = stats.Smartbombs(now); var tr = stats.LastSbTrace;
            if (sb == null || tr == null) line(T("no smartbomb hit in the last minute"), dim);
            else
            {
                line(F("{0}   modules {1} ({2})   period {3} s ({4})   total {5} DPS", new object[] { tr.Pilot, tr.Modules, T(tr.Locked ? "decided for this site" : "most voted so far"),
                     tr.Period.ToString("0.0", inv), T(tr.PeriodMeasured ? "measured" : "default"), Num(sb.Total) }), ink);
                foreach (var c in tr.Cycles.Skip(Math.Max(0, tr.Cycles.Count - 8)))
                    line("  " + c.Start.ToLocalTime().ToString("HH:mm:ss", inv) + "  " + F("hits {0}names {1}GCD {2}", new object[] { InsPad(c.Hits.ToString(inv), 4), InsPad(c.Names.ToString(inv), 3), InsPad(c.Gcd.ToString(inv), 3) })
                         + (c.Vote > 0 ? F("vote {0}", new object[] { c.Vote }) : !c.AfterWarp ? T("before the warp") : !c.Complete ? T("running") : c.Decided ? T("counted (already decided)") : F("no vote (GCD < {0})", new object[] { Tune.SbMinGcd.I })),
                         c.Vote > 0 ? ink : dim);
                if (tr.LockedBefore) line("  " + T("(decided earlier on this site: these cycles no longer vote)"), dim);
                line("  " + T("bars") + "    " + string.Join("  ", sb.Frac.Select(x => (x * 100).ToString("0", inv) + "%")), ink);
            }

            head("ANOMALY TIMER");
            var at = stats.Tracker;
            bool rewarp = at.PrevWarpTime != DateTime.MinValue && stats.LastNpcHit < at.PrevWarpTime;
            line(F("sites {0}   last warp {1} ({2})   warp before {3}", new object[] { at.AnomalyCount, ago(at.LastWarpTime), at.LastWarpLeader == "" ? "-" : at.LastWarpLeader, ago(at.PrevWarpTime) }), ink);
            if (at.LastWarpTime != DateTime.MinValue && at.PrevWarpTime != DateTime.MinValue)
                line(F("warp to warp {0}", new object[] { InsAgo((at.LastWarpTime - at.PrevWarpTime).TotalSeconds) }) + (rewarp ? "   " + T("RE-WARP (no hit between the warps)") : ""), rewarp ? red : ink);
            line(F("lines down {0}%   timer {1}%   no client {2}", new object[] { (quiet * 100).ToString("0", inv), (timerA * 100).ToString("0", inv),
                 ClientsGone ? T("yes") : noClientSince >= 0 ? F("since {0} s", new object[] { (Clock - noClientSince).ToString("0.0", inv) }) : T("no") }), ink);

            head("ISK GRID");
            var ticks = at.Bounties;
            long med = ticks.Count > 0 ? ticks.OrderBy(v => v).ElementAt(ticks.Count / 2) : 0;
            line(F("site {0}   session {1}   ticks {2}   median {3}", new object[] { FormatIsk(at.CurrentAnomalyIsk), FormatIsk(at.TotalSessionIsk), ticks.Count, med.ToString("N0", inv) }), ink);
            if (ticks.Count > 0)
                line("  " + T("last ticks") + " " + string.Join("  ", ticks.Skip(Math.Max(0, ticks.Count - 8)).Select(v => v.ToString("N0", inv) + " (" + (IskHeat(v) * 100).ToString("0", inv) + "%)")), ink);

            head("ALERTS");
            double onGrid = Tune.OnGridS.V;
            bool titan = (now - stats.LastTitan).TotalSeconds < onGrid, dread = (now - stats.LastDread).TotalSeconds < onGrid;
            line(InsPad(T("titan"), 8) + (titan ? T("ON GRID") : "-") + "   " + F("last hit {0}   hull heat {1}%", new object[] { ago(stats.LastTitan), (heat * 100).ToString("0", inv) }), titan ? red : ink);
            line(InsPad(T("dread"), 8) + (dread ? T("ON GRID") : "-") + "   " + F("last hit {0}   hull heat {1}%", new object[] { ago(stats.LastDread), (heatY * 100).ToString("0", inv) }), dread ? dr : ink);
            line(F("★ spawns this run {0}   loud spawns {1}   last {2}", new object[] { stats.Spawns.Count, stats.LoudSpawns, ago(stats.LastLoudSpawn) }), ink);
            foreach (var sg in stats.Sightings.Skip(Math.Max(0, stats.Sightings.Count - 5)))
                line("  " + InsPad(sg.Kind == 2 ? T("titan") : sg.Kind == 3 ? T("dread") : "★", 8) + InsPad(sg.Name, 36) + F("seen {0} .. {1}", new object[] { ago(sg.First), ago(sg.Last) }), sg.Kind == 2 ? red : sg.Kind == 3 ? dr : BodyTheme.Faction);

            head("LIGHTNING ROD");
            double shot = VortonShot[0] + VortonShot[1];
            line(F("mode {0}   runner {1}   shot {2}   repair {3} HP/s", new object[] { Competition ? "Competition" : "Standard", stats.Runner ?? "-", shot.ToString("0.#", inv), RunnerRepair.ToString("0.#", inv) }), ink);
            line(F("through the rod {0}/s   runner takes {1}/s   (last {2} s)", new object[] { Num(stats.RodDps(now, shot)), Num(stats.RunnerVortonTaken(now) + stats.RunnerNpcTaken(now)), Tune.Format(Tune.RodWindowS, Tune.RodWindowS.V) }), ink);
            return r;
        }

        static string InsPad(string s, int n) { s = s ?? ""; return s.Length >= n ? s.Substring(0, n - 1) + " " : s.PadRight(n); }
        static string InsAgo(double s)
        {
            if (s < 0) s = 0;
            if (s < 60) return s.ToString("0", CultureInfo.InvariantCulture) + " " + TuneWindow.T("s");
            if (s < 3600) return ((int)s / 60) + ":" + ((int)s % 60).ToString("00") + " " + TuneWindow.T("min");
            return (s / 3600).ToString("0.0", CultureInfo.InvariantCulture) + " " + TuneWindow.T("h");
        }
    }

    sealed class TuneWindow : Form
    {
        readonly DpsForm body;
        readonly Timer timer = new Timer { Interval = 500 };
        string page;                 // a group of Tune, or "Inspector"
        float scroll;                // content scroll, px
        bool frozen;                 // the Inspector stops refreshing
        Tune.P drag;                 // the parameter whose track is held
        // the value being typed in, as the settings' number fields (Settings -> Modes): what was typed (null: nothing yet, the
        // first key replaces the number); a key over the limit turns it red with one shake
        internal Tune.P editing; internal string editText; internal Tune.P errP; double errAt = -10;
        // the app's mouse: a key acts on release over the same key (downId), the light on the glass under it (lightAt)
        internal string downId; PointF? lightAt; public PointF? DemoMouse;
        RectangleF navGlassR, mainGlassR, barTrack, barView; float barH, barMax; float? barGrip;
        Bitmap baseFrame, frame; bool dirty = true;
        static readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        static double Clock { get { return clock.Elapsed.TotalSeconds; } }
        // the tip under a hull key after a short rest on it (DpsForm: TipDelay 0.7 s)
        readonly ToolTip tip = new ToolTip { ShowAlways = true, UseAnimation = false, UseFading = false };
        string tipFor, tipText; Point tipAt; double tipDue;
        List<DpsForm.InspectRow> report;
        Point mouse = new Point(-1, -1);
        // hit areas of the last frame
        readonly List<Tuple<RectangleF, string>> keys = new List<Tuple<RectangleF, string>>();
        readonly List<Tuple<RectangleF, Tune.P>> tracks = new List<Tuple<RectangleF, Tune.P>>(), values = new List<Tuple<RectangleF, Tune.P>>(), rows = new List<Tuple<RectangleF, Tune.P>>(), resets = new List<Tuple<RectangleF, Tune.P>>();
        float contentH;

        // the window's language (owner, 21.11.2026; all the app's languages 22.11.2026): at first the app's; the header key
        // opens a list of L.Languages (owner, 22.11.2026: a dropdown). The texts are native\lang\tune.<code>.json (English -> that language), kept apart from
        // the app's own <code>.json; a missing entry shows the English.
        // owner, 23.11.2026: always the app's language (Settings -> General); the window has no language choice of its own
        static string langOverride;   // selftest only
        public static string Lang { get { return langOverride ?? (L.Known(L.Code) ? L.Code : "en"); } set { langOverride = value; } }
        static readonly Dictionary<string, Dictionary<string, string>> maps = new Dictionary<string, Dictionary<string, string>>();
        public static string T(string en)
        {
            if (en == null || Lang == "en") return en;
            Dictionary<string, string> m;
            if (!maps.TryGetValue(Lang, out m)) maps[Lang] = m = L.Load("tune." + Lang);
            string t; return m.TryGetValue(en, out t) && !string.IsNullOrEmpty(t) ? t : en;
        }
        // Orbitron is Latin only: Ukrainian titles in Segoe UI Semibold, Chinese in Noto Sans SC (as the app's settings do)
        static readonly Dictionary<float, Font> headFonts = new Dictionary<float, Font>();
        static Font HeadFont(float px)
        {
            if (Lang == "zh") return NornsFonts.Sc(px * NornsFonts.MatchSc(FontStyle.Regular, true));   // see BodyFont
            if (Lang != "uk") return NornsFonts.Screen(px);
            Font f; if (!headFonts.TryGetValue(px, out f)) headFonts[px] = f = new Font("Segoe UI Semibold", px * 1.05f, GraphicsUnit.Pixel);
            return f;
        }
        // the text face: Noto Sans (Latin, Cyrillic); Chinese in Noto Sans SC Regular, sized to the same height. GDI+ drops
        // strokes of some Bold ideographs at 14-18 px (何 你 环 不 开 ...); Regular draws all of them whole
        static Font BodyFont(float px, FontStyle style = FontStyle.Regular)
        {
            return Lang == "zh" ? NornsFonts.Sc(px * NornsFonts.MatchSc(FontStyle.Regular, false)) : NornsFonts.Body(px, style);
        }

        static float K { get { return Theme.K; } }
        static float U(float v) { return v * K; }
        public DateTime? DemoNow;    // selftest: a fixed clock

        public TuneWindow(DpsForm body)
        {
            this.body = body;
            Text = AppInfo.Name + " — Tuning";
            Icon = AppInfo.Icon;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            var wa = Screen.PrimaryScreen.WorkingArea;   // never larger than the screen (a large Windows scale)
            ClientSize = new Size(Math.Min((int)Math.Round(M(HW)), wa.Width - 20), Math.Min((int)Math.Round(M(HH)), wa.Height - 20));
            LoadPos();
            TopMost = true; MaximizeBox = false;   // the hull drags the window; a double click on it must not maximize it
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.None;
            page = Tune.Groups.First();
            timer.Tick += (s, e) => { if (page == "Inspector" && !frozen) { report = null; Redraw(); } else if (Tune.Version != shownVersion) Redraw(); };
            timer.Start();
            frameTimer.Tick += (s, e) => Frame();
            SyncLook();
            frameTimer.Start();
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape && editing == null) Hide(); };
        }
        int shownVersion = -1;

        protected override void Dispose(bool disposing) { if (disposing) { timer.Dispose(); frameTimer.Dispose(); tip.Dispose(); if (baseFrame != null) baseFrame.Dispose(); if (frame != null) frame.Dispose(); } base.Dispose(disposing); }

        // ---------------- the window as the body's (owner, 23.11.2026: the same UI one to one) ----------------
        // a layered window: the hull's own anti-aliased edge over the desktop, no WinForms painting (FormulaWindow, DpsForm)
        protected override CreateParams CreateParams { get { var cp = base.CreateParams; cp.ExStyle |= 0x80000 /* WS_EX_LAYERED */; return cp; } }
        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if (!Visible) SavePos(); if (Visible) { SyncLook(); Redraw(); } else { tip.Hide(this); tipText = null; } }
        protected override void OnShown(EventArgs e) { base.OnShown(e); Redraw(); }

        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SetCursor(IntPtr h);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr WindowFromPoint(Point p);
        // the hull is the handle that drags the window, its displays and keys are the window's; outside the hull's round
        // corners the desktop gets the mouse. The app's blue pointer over all of it, pulsing over what can be pressed
        // Settings -> General -> Remember window position: as the body (DpsForm.SavePos / LoadPos), in its own file
        static string PosFile { get { return System.IO.Path.Combine(AppInfo.DataDir, "tune_pos.txt"); } }
        void SavePos()
        {
            if (DpsForm.SelfTesting || !DpsForm.RememberPos || !IsHandleCreated) return;
            try { System.IO.Directory.CreateDirectory(AppInfo.DataDir); System.IO.File.WriteAllText(PosFile, Location.X + "," + Location.Y); }
            catch { }
        }
        void LoadPos()
        {
            if (DpsForm.SelfTesting || !DpsForm.RememberPos) return;
            try
            {
                if (!System.IO.File.Exists(PosFile)) return;
                var s = System.IO.File.ReadAllText(PosFile).Trim().Split(',');
                int x, y;
                if (s.Length == 2 && int.TryParse(s[0], out x) && int.TryParse(s[1], out y)
                    && Screen.AllScreens.Any(sc => sc.WorkingArea.IntersectsWith(new Rectangle(x, y, Width, Height))))
                { StartPosition = FormStartPosition.Manual; Location = new Point(x, y); }
            }
            catch { }
        }
        bool rememberSeen = DpsForm.RememberPos;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0xA3 /* WM_NCLBUTTONDBLCLK */) { m.Result = IntPtr.Zero; return; }   // a double click on the hull does nothing (owner, 24.11.2026: it maximized the window)
            if (m.Msg == 0x0232 /* WM_EXITSIZEMOVE */) SavePos();
            if (m.Msg == 0x84 /* WM_NCHITTEST */)
            {
                var p = PointToClient(new Point((short)(m.LParam.ToInt64() & 0xFFFF), (short)((m.LParam.ToInt64() >> 16) & 0xFFFF)));
                bool onBody;
                using (var hull = HRound(new RectangleF(0, 0, Width, Height), 24)) onBody = hull.IsVisible(p);
                m.Result = (IntPtr)(!onBody ? -1 /* HTTRANSPARENT */ : OnKey(p) ? 1 /* HTCLIENT */ : 2 /* HTCAPTION */);
                return;
            }
            if (m.Msg == 0x20 /* WM_SETCURSOR */)
            {
                int hit = (short)(m.LParam.ToInt64() & 0xFFFF);
                if (hit == 1 || hit == 2) { SetCursor((OnPulse(PointToClient(Cursor.Position)) ? DpsForm.PulsePointer : DpsForm.Pointer).Handle); m.Result = (IntPtr)1; return; }
            }
            base.WndProc(ref m);
        }
        bool OnKey(Point p) { return navGlassR.Contains(p) || mainGlassR.Contains(p) || keys.Any(k => k.Item1.Contains(p)); }
        bool OnPulse(Point p) { return SpotAt(p) != null || tracks.Any(t => t.Item1.Contains(p)) || barTrack.Contains(p); }
        string SpotAt(PointF p)
        {
            var k = keys.LastOrDefault(x => x.Item1.Contains(p)); if (k != null) return k.Item2;
            var r = resets.FirstOrDefault(x => x.Item1.Contains(p)); if (r != null) return "reset:" + r.Item2.Key;
            var v = values.FirstOrDefault(x => x.Item1.Contains(p)); if (v != null) return "field:" + v.Item2.Key;
            return null;
        }

        // ---------------- painting ----------------
        // what changes with the state is painted again on Redraw; each frame (25 fps) puts the small display's animation,
        // its glass marks and the cursor's light on the dirt over that, as the body draws its shell and its live parts
        void Redraw() { dirty = true; Present(); }
        // what the app's settings make of the look (Settings -> Display): when it changes, the window follows at once, as
        // the body does - Potato's 5 fps and plain hull, eye comfort, the hull's and the accent's colours, the size
        string look;
        internal void SyncLook()
        {
            if (DpsForm.RememberPos != rememberSeen) { rememberSeen = DpsForm.RememberPos; SavePos(); }   // switched on: kept from now, as the body
            var now = string.Join("|", DpsForm.Potato, DpsForm.EyeComfort, DpsForm.UiScale, Lang, DpsForm.PowerAnimName, BodyTheme.Hull.ToArgb(), BodyTheme.Accent.ToArgb(), BodyTheme.Ink.ToArgb(), BodyTheme.Lamp.ToArgb());
            if (now == look) return;
            bool first = look == null;
            look = now;
            frameTimer.Interval = DpsForm.Potato ? 200 : 40;
            if (DpsForm.Potato) lightAt = null;
            var wa = Screen.FromControl(this).WorkingArea;
            var size = new Size(Math.Min((int)Math.Round(M(HW)), wa.Width - 20), Math.Min((int)Math.Round(M(HH)), wa.Height - 20));
            if (ClientSize != size)
            {
                ClientSize = size;   // Settings -> Display -> Window size: the window grows or shrinks with the body
                if (!first) Location = new Point(Math.Max(wa.Left, Math.Min(Left, wa.Right - Width)), Math.Max(wa.Top, Math.Min(Top, wa.Bottom - Height)));   // and stays on the screen
            }
            if (!first) Redraw();
        }
        void Frame()
        {
            SyncLook();
            if (!Visible) return;
            var mp = PointToClient(Cursor.Position);   // polled: the light follows while the window is dragged
            lightAt = !DpsForm.Potato && (navGlassR.Contains(mp) || mainGlassR.Contains(mp)) ? (PointF?)mp : null;
            if (tipText != null && Clock >= tipDue) { tip.Show(tipText, this, tipAt.X, tipAt.Y, 3000); tipText = null; }
            if (OnPulse(mp) && ClientRectangle.Contains(mp) && WindowFromPoint(Cursor.Position) == Handle) SetCursor(DpsForm.PulsePointer.Handle);
            if (editing != null || Clock - errAt < 0.9) dirty = true;   // the caret blinks, the field shakes
            Present();
        }
        void Present()
        {
            if (!IsHandleCreated || !Visible) return;
            if (frame == null || frame.Size != ClientSize) { if (frame != null) frame.Dispose(); frame = new Bitmap(ClientSize.Width, ClientSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb); }
            Compose(frame, lightAt);
            AppChrome.PresentLayered(this, frame);
        }
        void Compose(Bitmap target, PointF? light)
        {
            if (dirty || baseFrame == null || baseFrame.Size != ClientSize)
            {
                if (baseFrame == null || baseFrame.Size != ClientSize) { if (baseFrame != null) baseFrame.Dispose(); baseFrame = new Bitmap(ClientSize.Width, ClientSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb); }
                using (var g = Graphics.FromImage(baseFrame)) Paint(g, ClientSize);
                dirty = false;
            }
            using (var g = Graphics.FromImage(target))
            {
                g.CompositingMode = CompositingMode.SourceCopy; g.DrawImageUnscaled(baseFrame, 0, 0); g.CompositingMode = CompositingMode.SourceOver;
                g.SmoothingMode = SmoothingMode.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                PaintAnim(g);
                PaintRunning(g);
                if (light != null && !DpsForm.Potato)   // Potato: no light on the glass, as the body
                {
                    var gr = navGlassR.Contains(light.Value) ? navGlassR : mainGlassR;
                    var st = g.Save();
                    using (var gp = HRound(gr, 13)) g.SetClip(gp);
                    // the keys on the glass stay clear, as on the body's screen
                    foreach (var k in keys) if (gr.Contains(k.Item1.Location)) g.SetClip(k.Item1, CombineMode.Exclude);
                    foreach (var k in resets.Concat(values)) g.SetClip(k.Item1, CombineMode.Exclude);
                    GlassDirt.Paint(g, gr, light.Value, M(1));
                    g.Restore(st);
                }
            }
        }
        // the small display's live part: the cube wave, then the glass's light marks and its rim (DpsForm.PaintPowerGlass)
        void PaintAnim(Graphics g)
        {
            if (animDisplay.IsEmpty) return;
            using (var fp = HRound(animDisplay, 9))
            {
                var st = g.Save();
                g.SetClip(fp, CombineMode.Intersect);
                double secs = DemoNow != null ? 4.2 : (DateTime.UtcNow - animStart).TotalSeconds;
                PowerAnims.Draw(DpsForm.PowerAnimName, "tune", g, animDisplay, DpsForm.Potato ? 0 : secs, BodyTheme.Lamp);
                var tex = DpsForm.Potato ? null : DpsForm.GlassTexture;
                if (tex != null)
                {
                    var cm = new System.Drawing.Imaging.ColorMatrix(new[]
                    {
                        new float[] { 0, 0, 0, 0.19f * 0.30f, 0 }, new float[] { 0, 0, 0, 0.19f * 0.59f, 0 }, new float[] { 0, 0, 0, 0.19f * 0.11f, 0 },
                        new float[] { 0, 0, 0, 0, 0 }, new float[] { 1, 1, 1, 0, 1 }
                    });
                    using (var ia = new System.Drawing.Imaging.ImageAttributes())
                    {
                        ia.SetColorMatrix(cm);
                        var face = animDisplay; float tw = face.Width * 1.5f, th = face.Height * 1.5f;
                        g.DrawImage(tex, new Rectangle((int)(face.Left - (tw - face.Width) / 2), (int)(face.Top - (th - face.Height) / 2), (int)tw, (int)th), 0, 0, tex.Width, tex.Height, GraphicsUnit.Pixel, ia);
                    }
                }
                g.Restore(st);
                using (var pen = new Pen(Color.FromArgb(28, 29, 32), M(1))) g.DrawPath(pen, fp);
                using (var pen = new Pen(Color.FromArgb(15, 255, 255, 255), 1f)) g.DrawPath(pen, fp);
            }
        }

        // selftest: a frame as the window would show it (the demo mouse lights the glass)
        public Bitmap Render()
        {
            dirty = true;
            var b = new Bitmap(ClientSize.Width, ClientSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Compose(b, DemoMouse);
            return b;
        }
        public void ShowPage(string p) { page = p; scroll = 0; report = null; }


        // ---------------- the body (owner, 22.11.2026: the first layout - the header, the pages on the left, the values on
        // the right, the footer - in the app's own look: the hull around, the pages on a black display of their own, the values
        // on the main black screen, the printed band). Sizes in the app's logical units (DpsForm: 287 x 329), at its size setting (100 / 125 / 150 %) ----------------
        const float HW = 640, HH = 480, HPad = 7, HStrip = 35, HGap = 6, HBand = 26.5f, NavW = 140;   // NavW: every page's name fits at Tab size, with its dot
        static float M(float v) { return v * K * DpsForm.UiScale; }
        static GraphicsPath HRound(RectangleF r, float lr) { return Theme.Round(r, M(lr * 0.6f), 0.6f); }
        static float HeadH { get { return M(HPad + HStrip); } }   // the strip drags the window
        static Color A(int a, Color c) { return Color.FromArgb(a, c); }
        static readonly Color Dim = BodyTheme.LabelDim, Faint = Color.FromArgb(110, BodyTheme.Ink);
        RectangleF animDisplay;
        readonly DateTime animStart = DateTime.UtcNow;
        readonly Timer frameTimer = new Timer { Interval = DpsForm.Potato ? 200 : 40 };   // 25 fps as the body (Potato: 5)

        // the title face: Orbitron; Ukrainian in Segoe UI Semibold, Chinese in Noto Sans SC Regular (see BodyFont)
        internal static Font TitleF(string text, float px)
        {
            if (L.Cjk(text)) return NornsFonts.Sc(px * NornsFonts.MatchSc(FontStyle.Regular, true));
            if (L.Cyrillic(text)) { Font f; if (!titleFonts.TryGetValue(px, out f)) titleFonts[px] = f = new Font("Segoe UI Semibold", px * NornsFonts.Match("Segoe UI Semibold", FontStyle.Regular, true), GraphicsUnit.Pixel); return f; }
            return NornsFonts.Screen(px);
        }
        static readonly Dictionary<float, Font> titleFonts = new Dictionary<float, Font>();
        internal static Font TextF(string text, float px) { return L.Cjk(text) ? NornsFonts.Sc(px * NornsFonts.MatchSc(FontStyle.Regular, false)) : NornsFonts.Body(px); }

        void Paint(Graphics g, Size size)
        {
            shownVersion = Tune.Version;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            keys.Clear(); tracks.Clear(); values.Clear(); rows.Clear(); resets.Clear(); running.Clear();
            g.Clear(Color.Transparent);
            float W = size.Width, H = size.Height;
            var all = new RectangleF(0, 0, W, H);

            // the hull: satin light from the top left, darker towards the bottom, a lit edge (DpsForm.DrawShell)
            using (var body = HRound(all, 24))
            {
                using (var b = new SolidBrush(BodyTheme.Hull)) g.FillPath(b, body);
                if (!DpsForm.Potato)
                    using (var lg = new LinearGradientBrush(all, Color.White, Color.Black, 80f))
                    {
                        lg.InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(36, 255, 255, 255), Color.FromArgb(5, 255, 255, 255), Color.FromArgb(66, 0, 0, 0) }, Positions = new[] { 0f, 0.45f, 1f } };
                        g.FillPath(lg, body);
                    }
                var edge = all; edge.Inflate(-0.5f, -0.5f);
                using (var ep = HRound(edge, 24))
                using (var lg = new LinearGradientBrush(all, Color.FromArgb(70, 255, 255, 255), Color.FromArgb(115, 0, 0, 0), 90f))
                using (var pen = new Pen(lg, 1f)) g.DrawPath(pen, ep);
            }

            // the strip: ノルン printed in white, the lamp (lit while a value differs from its default), TUNING printed
            var strip = new RectangleF(M(HPad), M(HPad), W - 2 * M(HPad), M(HStrip));
            float x0 = M(HPad + 7), nameRight = x0, mid = strip.Top + strip.Height / 2;
            var name = DpsForm.NamePath(M(9.5f));
            if (name != null)
                using (name)
                {
                    var nb0 = name.GetBounds();
                    using (var m = new Matrix()) { m.Translate(x0 - nb0.X, mid - nb0.Height / 2 - nb0.Y); name.Transform(m); }
                    using (var nb = new SolidBrush(Color.FromArgb(244, 245, 247))) g.FillPath(nb, name);
                    nameRight = x0 + nb0.Width;
                }
            int changed = Tune.All.Count(p => p.Changed);
            float d = M(6.825f), lx = nameRight + M(6);
            double level = changed > 0 ? 1 : 0.25;
            DpsForm.SoftGlow(g, new PointF(lx + d / 2, mid), d / 2 + M(5), BodyTheme.Accent, (int)(level * 150));
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(lx, mid - d / 2, d, d);
                using (var pb = new PathGradientBrush(path))
                {
                    pb.CenterColor = DpsForm.Mix(BodyTheme.LampCoreOff, BodyTheme.LampCoreOn, level);
                    pb.SurroundColors = new[] { DpsForm.Mix(BodyTheme.LampEdgeOff, BodyTheme.LampEdgeOn, level) };
                    g.FillPath(pb, path);
                }
            }
            using (var pen = new Pen(Color.FromArgb(80, 12, 13, 15), M(0.6f))) g.DrawEllipse(pen, lx, mid - d / 2, d, d);
            var tt = T("TUNING").ToUpperInvariant();
            var tf = TitleF(tt, M(7.2f));   // Caption, as the settings' section headings
            using (var b = new SolidBrush(Color.FromArgb(225, BodyTheme.Silk))) DpsForm.Spaced(g, tt, tf, b, lx + d + M(9), mid - tf.GetHeight(g) / 2, M(1));

            // right to left: Close, Reset all, Open file, Delete (owner, 23.11.2026: the custom tuning cleared and deleted,
            // after a warning) - the hull's keys - then the small display
            float kw = M(14.2f), kh = M(30), ky = mid - kh / 2;
            var close = new RectangleF(strip.Right - kw, ky, kw, kh);
            var reset = new RectangleF(close.Left - M(3) - kw, ky, kw, kh);
            var open = new RectangleF(reset.Left - M(3) - kw, ky, kw, kh);
            DpsForm.CloseKey(g, close, HotKey(close), Held("close", close), M(1));
            DpsForm.Key(g, reset, changed > 0 && HotKey(reset), changed > 0 && Held("resetall", reset), M(1), BodyTheme.Dread, MaterialIcon.Refresh);
            DpsForm.Key(g, open, HotKey(open), Held("openfile", open), M(1), BodyTheme.Accent, MaterialIcon.Folder);
            keys.Add(Tuple.Create(close, "close"));
            if (changed > 0) keys.Add(Tuple.Create(reset, "resetall"));
            keys.Add(Tuple.Create(open, "openfile"));
            var del = new RectangleF(open.Left - M(3) - kw, ky, kw, kh);
            DpsForm.Key(g, del, HotKey(del), Held("delete", del), M(1), BodyTheme.Alarm, TuneWarnWindow.DeleteIcon);
            keys.Add(Tuple.Create(del, "delete"));
            float dispRight = del.Left - M(8);
            if (page == "Inspector")
            {
                var fr = new RectangleF(del.Left - M(3) - kw, ky, kw, kh);
                DpsForm.Key(g, fr, frozen || HotKey(fr), Held("freeze", fr), M(1), BodyTheme.Accent, frozen ? MaterialIcon.Play : Pause);
                keys.Add(Tuple.Create(fr, "freeze"));
                dispRight = fr.Left - M(8);
            }
            // the small display on the strip (owner, 22.11.2026): an animation in the accent, as the body's power display -
            // the cube wave of PowerAnims, its own slot
            animDisplay = new RectangleF(dispRight - M(53), mid - M(14), M(53), M(28));
            PaintDisplay(g, animDisplay);   // its live part each frame (PaintAnim)

            // two screens in their recesses: the pages on the left, the values (or the inspector) on the right
            float top = M(HPad + HStrip + HGap), bottom = H - M(HBand);
            var navGlass = navGlassR = PaintRecess(g, RectangleF.FromLTRB(M(HPad), top, M(HPad + NavW), bottom));
            var glass = mainGlassR = PaintRecess(g, RectangleF.FromLTRB(M(HPad + NavW + HGap), top, W - M(HPad), bottom));
            PaintNav(g, navGlass);
            var content = RectangleF.FromLTRB(glass.Left + M(11), glass.Top + M(7), glass.Right - M(11), glass.Bottom - M(7));
            var state = g.Save();
            g.SetClip(RectangleF.FromLTRB(glass.Left, content.Top, glass.Right, content.Bottom));
            if (page == "Inspector") PaintInspector(g, RectangleF.FromLTRB(content.Left - M(6), content.Top, content.Right, content.Bottom));
            else PaintGroup(g, content);
            g.Restore(state);
            barTrack = RectangleF.Empty;
            if (contentH > content.Height + 1)
            {
                // the settings' bar: it can be pulled (Settings.AddBar)
                float right = glass.Right - M(4);
                barMax = contentH - content.Height; barView = content;
                barH = DpsForm.PaintScrollBar(g, content, right, Math.Min(scroll, barMax), barMax, M(1));
                barTrack = RectangleF.FromLTRB(right - M(7), content.Top, right + M(2), content.Bottom);
            }

            PaintHullBand(g, RectangleF.FromLTRB(M(HPad + 7), H - M(HBand), W - M(HPad + 7), H), changed);
        }

        bool HotKey(RectangleF r) { return r.Contains(mouse); }
        bool Held(string id, RectangleF r) { return downId == id && r.Contains(mouse); }
        static readonly StringFormat Centre = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

        // a small display on the hull: the glass in a dark lip with its shadow (DpsForm.PaintDamage)
        void PaintDisplay(Graphics g, RectangleF r)
        {
            var sh = r; sh.Offset(0, M(3.5f)); sh.Inflate(-M(1), 0);
            using (var sp = HRound(sh, 9)) using (var b = new SolidBrush(Color.FromArgb(120, 0, 0, 0))) g.FillPath(b, sp);
            using (var lip = HRound(new RectangleF(r.X, r.Y + M(1.5f), r.Width, r.Height), 9)) using (var b = new SolidBrush(Color.FromArgb(8, 9, 11))) g.FillPath(b, lip);
            using (var fp = HRound(r, 9))
            {
                using (var pb = new PathGradientBrush(fp) { CenterColor = Color.FromArgb(12, 13, 16), SurroundColors = new[] { Color.FromArgb(3, 3, 4) } }) g.FillPath(pb, fp);
                using (var pen = new Pen(Color.FromArgb(28, 29, 32), M(1))) g.DrawPath(pen, fp);
                using (var pen = new Pen(Color.FromArgb(15, 255, 255, 255), 1f)) g.DrawPath(pen, fp);
            }
        }

        // a screen in its recess: the hull's wall in shadow at the top, the lip catching light, the bezel, the black glass
        // (DpsForm.DrawShell); returns the glass
        RectangleF PaintRecess(Graphics g, RectangleF cav)
        {
            using (var cp = HRound(cav, 17))
            {
                using (var b = new SolidBrush(DpsForm.Potato ? DpsForm.Mix(BodyTheme.Hull, Color.Black, 0.3) : BodyTheme.Hull)) g.FillPath(b, cp);
                if (!DpsForm.Potato)
                    using (var lg = new LinearGradientBrush(cav, Color.Black, Color.White, 90f))
                    {
                        lg.InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(140, 0, 0, 0), Color.FromArgb(64, 0, 0, 0), Color.FromArgb(26, 255, 255, 255) }, Positions = new[] { 0f, 0.55f, 1f } };
                        g.FillPath(lg, cp);
                    }
                using (var lg = new LinearGradientBrush(cav, Color.FromArgb(0, 255, 255, 255), Color.FromArgb(56, 255, 255, 255), 90f))
                using (var pen = new Pen(lg, 1f)) g.DrawPath(pen, cp);
            }
            var bz = cav; bz.Inflate(-M(1.5f), -M(1.5f));
            using (var bp = HRound(bz, 15.5f))
            using (var lg = new LinearGradientBrush(bz, Color.FromArgb(26, 27, 29), Color.FromArgb(11, 12, 13), 90f))
            {
                var sh = bz; sh.Offset(0, M(1.5f));
                using (var sp = HRound(sh, 15.5f)) using (var sb = new SolidBrush(Color.FromArgb(110, 0, 0, 0))) g.FillPath(sb, sp);
                g.FillPath(lg, bp);
                using (var pen = new Pen(Color.FromArgb(26, 255, 255, 255), 1f)) g.DrawPath(pen, bp);
            }
            var glass = cav; glass.Inflate(-M(4), -M(4));
            using (var gp = HRound(glass, 13)) using (var b = new SolidBrush(Color.Black)) g.FillPath(b, gp);
            return glass;
        }

        // the pages on their own display: Orbitron capitals as the settings' tabs, the open one in the accent with a bar,
        // how many values differ on the right; a line, then the inspector
        void PaintNav(Graphics g, RectangleF glass)
        {
            float x = glass.Left + M(7), w = glass.Width - M(14), y = glass.Top + M(8), rh = M(21);
            foreach (var grp in Tune.Groups.Concat(new[] { "", "Inspector" }))
            {
                if (grp == "")
                {
                    y += M(5);
                    using (var b = new SolidBrush(A(26, BodyTheme.Ink))) g.FillRectangle(b, x + M(3), y, w - M(6), Math.Max(1f, M(0.5f)));
                    y += M(6);
                    continue;
                }
                var r = new RectangleF(x, y, w, rh);
                bool on = grp == page, hot = !on && HotKey(r);
                if (on || hot) using (var p = HRound(r, 6)) using (var b = new SolidBrush(on ? A(16, BodyTheme.Accent) : A(22, BodyTheme.Ink))) g.FillPath(b, p);
                if (on) using (var b = new SolidBrush(BodyTheme.Accent)) g.FillRectangle(b, r.Left, r.Top + M(4.5f), M(1.5f), r.Height - M(9));
                // owner, 23.11.2026: the names always in English (Orbitron, one face for every language), an icon for each,
                // and an accent dot where values differ (no count)
                var col = on ? BodyTheme.Accent : hot ? BodyTheme.Label : Color.FromArgb(130, 133, 140);
                float ic = M(8.5f), tx = r.Left + M(6);
                MaterialIcon.Draw(g, NavIcon(grp), new RectangleF(tx, r.Top + (rh - ic) / 2, ic, ic), Brushes(col));
                tx += ic + M(4);
                bool changed = Tune.All.Any(p => p.Group == grp && p.Changed);
                float dot = M(3.2f), room = r.Right - (changed ? M(6) + dot + M(3) : M(4)) - tx;
                var label = grp.ToUpperInvariant();
                // owner, 23.11.2026: one size for every name (Tab, as the settings' tabs); a name too long for its place
                // runs by, as the preview name on the body's power display (Potato: cut with "…")
                var f = NornsFonts.Screen(M(6.8f));
                float lw = g.MeasureString(label, f, 1000, StringFormat.GenericTypographic).Width, ly = r.Top + (rh - f.GetHeight(g)) / 2 + M(0.5f);
                var lr = new RectangleF(tx, ly, room, f.GetHeight(g) + M(1));
                if (lw > room && !DpsForm.Potato) running.Add(new Running { Text = label, Font = f, Color = col, Place = lr, Width = lw });
                else
                    using (var b = new SolidBrush(col))
                        g.DrawString(label, f, b, lr, new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                if (changed)   // values differ here: an accent dot, lit
                {
                    var c = new PointF(r.Right - M(6) - dot / 2, r.Top + rh / 2);
                    DpsForm.SoftGlow(g, c, M(5), BodyTheme.Accent, 110);
                    using (var b = new SolidBrush(BodyTheme.Accent)) g.FillEllipse(b, c.X - dot / 2, c.Y - dot / 2, dot, dot);
                }
                keys.Add(Tuple.Create(r, "page:" + grp));
                y += rh + M(1);
            }
        }
        const string Pause = "M6 19h4V5H6v14zm8-14v14h4V5h-4z";   // Material pause
        // the side menu's names too long for their place: painted each frame over the still frame (Compose)
        sealed class Running { public string Text; public Font Font; public Color Color; public RectangleF Place; public float Width; }
        readonly List<Running> running = new List<Running>();
        public double? MarqueeSecs;   // selftest: a fixed moment of the running line
        // the running line as the body's (DpsForm.PaintPreviewName): from the left edge on, then again after a gap, 39 units a second
        void PaintRunning(Graphics g)
        {
            if (running.Count == 0) return;
            double secs = MarqueeSecs ?? (DemoNow != null ? 0 : (DateTime.UtcNow - animStart).TotalSeconds);
            var st = g.Save();
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            foreach (var rn in running)
            {
                g.SetClip(RectangleF.FromLTRB(rn.Place.Left, rn.Place.Top - M(2), rn.Place.Right, rn.Place.Bottom + M(2)));
                float loop = rn.Width + M(18), x = rn.Place.Left - (float)(secs * M(39.1f) % loop);
                using (var b = new SolidBrush(rn.Color))
                {
                    g.DrawString(rn.Text, rn.Font, b, x, rn.Place.Top, StringFormat.GenericTypographic);
                    g.DrawString(rn.Text, rn.Font, b, x + loop, rn.Place.Top, StringFormat.GenericTypographic);
                }
            }
            g.Restore(st);
        }
        static string NavIcon(string grp)
        {
            switch (grp)
            {
                case "Log": return MaterialIcon.Description;
                case "Vorton projector": return MaterialIcon.Share;     // the arcs jump from target to target
                case "Smartbombs": return MaterialIcon.Flare;           // a blast all around
                case "Anomaly timer": return MaterialIcon.Timer;
                case "ISK grid": return MaterialIcon.Grid;
                case "Alerts": return MaterialIcon.Bell;
                case "Lightning rod": return MaterialIcon.Bolt;
                default: return MaterialIcon.Search;                    // Inspector
            }
        }

        // the bottom edge: NRN-01 printed, then what the window keeps (on a dark plate when values changed, as the band's
        // TEST BUILD), the three ticks and the version plate (DpsForm.PaintBand)
        void PaintHullBand(Graphics g, RectangleF r, int changed)
        {
            Func<float, float> V = v => M(v * 1.1f);   // owner, 23.11.2026: the band keeps the main window's sizes (5.5 / 7.5), off the type scale on purpose
            var f = NornsFonts.Body(V(5));
            float sp = V(0.72f), cy = r.Top + r.Height / 2, ty = cy - f.GetHeight(g) / 2;
            using (var silk = new SolidBrush(Color.FromArgb(215, BodyTheme.Silk)))
            {
                float x = r.Left;
                foreach (var ch in "NRN-01")
                {
                    DpsForm.Weighted(g, ch.ToString(), f, BodyTheme.Name, x, ty, V(0.3f));
                    x += g.MeasureString(ch.ToString(), f, 1000, StringFormat.GenericTypographic).Width + sp;
                }
                x += V(6) - sp;
                using (var b = new SolidBrush(Color.FromArgb(191, 236, 235, 228))) g.FillRectangle(b, x, cy - V(2), V(2.5f), V(4));
                x += V(2.5f) + V(6);
                var ver = AppInfo.Version;
                float vw = DpsForm.SpacedWidth(g, ver, f, sp) + V(6);
                var box = new RectangleF(r.Right - vw, cy - V(4.5f), vw, V(9));
                using (var pen = new Pen(Color.FromArgb(217, BodyTheme.Accent), Math.Max(1f, V(0.5f)))) g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);
                using (var yb = new SolidBrush(BodyTheme.Accent)) DpsForm.Spaced(g, ver, f, yb, box.Left + V(3), cy - f.GetHeight(g) / 2, sp);
                float rx = box.Left - V(6);
                for (int i = 2; i >= 0; i--) g.FillRectangle(silk, rx - V(0.75f) - i * V(1.75f), cy - V(2.25f), V(0.75f), V(4.5f));
                float room = rx - V(5.25f) - V(6) - x;
                string t = Tune.LastError ?? (changed == 0 ? T("All values are the defaults.")
                    : string.Format(CultureInfo.InvariantCulture, T(changed == 1 ? "{0} value differs from the defaults · kept in {1}" : "{0} values differ from the defaults · kept in {1}"), changed, System.IO.Path.GetFileName(Tune.FilePath)));
                t = t.TrimEnd('.').ToUpperInvariant();
                var tf = L.Cjk(t) ? TextF(t, V(5) + M(2)) : NornsFonts.Body(V(5) + M(2));
                float tsp = L.Cjk(t) ? 0 : sp;
                if (DpsForm.SpacedWidth(g, t, tf, tsp) > room)
                {
                    while (t.Length > 1 && DpsForm.SpacedWidth(g, t + "…", tf, tsp) > room) t = t.Substring(0, t.Length - 1).TrimEnd();
                    t += "…";
                }
                float tty = cy - tf.GetHeight(g) / 2;
                if (changed == 0 && Tune.LastError == null) DpsForm.Spaced(g, t, tf, silk, x, tty, tsp);
                else
                {
                    float ph = tf.GetHeight(g) + V(3);
                    var plate = new RectangleF(x - V(3), cy - ph / 2, DpsForm.SpacedWidth(g, t, tf, tsp) + V(6), ph);
                    using (var p = HRound(plate, 3)) using (var pb = new SolidBrush(BodyTheme.KeyRim)) g.FillPath(pb, p);
                    using (var tb = new SolidBrush(Tune.LastError != null ? BodyTheme.Alarm : BodyTheme.Accent)) DpsForm.Spaced(g, t, tf, tb, x, tty, tsp);
                }
            }
        }

        // the value being typed in: the settings' number field (Modes.NumberField) - the accent rim, the number, the caret,
        // the unit at the right; red with one shake when a key would pass the limit. Returns its place (the hit area)
        RectangleF PaintField(Graphics g, Tune.P p, Font val, Font uf, string ut, float right, float y)
        {
            var txt = editText ?? Tune.Format(p, p.V).Replace(" ", "");
            float tw = txt.Length == 0 ? 0 : g.MeasureString(txt, val, 1000, StringFormat.GenericTypographic).Width;
            float uw = ut == "" ? 0 : g.MeasureString(ut, uf, 1000, StringFormat.GenericTypographic).Width;
            float w = Math.Max(M(46), tw + uw + M(16));
            var r = new RectangleF(right + M(3) - w, y + M(1), w, M(14));
            double et = errP == p ? Clock - errAt : 10;
            bool err = et < 0.9;
            if (err && et < 0.35 && !DpsForm.Potato) r.Offset((float)(M(2.2f) * Math.Sin(et * 2 * Math.PI * 7) * Math.Exp(-et * 8)), 0);
            using (var path = HRound(r, 5))
            {
                using (var b = new SolidBrush(err ? A(46, BodyTheme.Alarm) : A(40, BodyTheme.Accent))) g.FillPath(b, path);
                using (var pen = new Pen(err ? BodyTheme.Alarm : BodyTheme.Accent, 1f)) g.DrawPath(pen, path);
            }
            float x = r.Left + M(4), ty = r.Top + (r.Height - val.GetHeight(g)) / 2 + M(0.5f);
            g.DrawString(txt, val, Brushes(BodyTheme.Ink), x, ty, StringFormat.GenericTypographic);
            if (DpsForm.SelfTesting || DpsForm.Potato || Clock % 1.0 < 0.55)   // the caret, blinking
                using (var b = new SolidBrush(BodyTheme.Accent)) g.FillRectangle(b, x + tw + M(1), r.Top + M(3), Math.Max(1f, M(0.7f)), r.Height - M(6));
            if (ut != "") g.DrawString(ut, uf, Brushes(A(120, BodyTheme.Ink)), r.Right - M(4) - uw, r.Top + (r.Height - uf.GetHeight(g)) / 2 + M(0.5f), StringFormat.GenericTypographic);
            return r;
        }

        // one row per value: the name, the value in the screen's face on the right; the key and its tag; the track (the
        // default a tick, the value a lamp); its limits; what it does in players' words.
        // Owner, 23.11.2026: only the app's own type scale, one size for each role on the page - the heading and the value
        // as the cards' titles and the modules' values (8.8), what it does as the cards' text (9.6), the key, the tag, the
        // unit, the limits and the default in Micro (6.4), the page note as the settings' notes (7.2). Nothing shrinks to fit:
        // a long heading and the help text wrap, and the row grows
        sealed class RowLay { public string Lt, Vt, Ut, Ht; public Font Lf, Uf, Hf; public float HeadW, HeadH, Uw, Vw, Vx, KeyH, HelpH, H; }
        static readonly StringFormat Wrap = new StringFormat();
        void PaintGroup(Graphics g, RectangleF area)
        {
            var list = Tune.All.Where(p => p.Group == page).ToList();
            float x0 = area.Left, x1 = area.Right - M(8);
            var note = T("Formula cards show the default numbers.");
            var mono = new Font("Consolas", M(6.4f), GraphicsUnit.Pixel);
            var val = NornsFonts.Screen(M(8.8f));
            var limF = NornsFonts.Body(M(6.4f));
            var tag = T("new events only"); var tgf = TextF(tag, M(6.4f));
            float valH = val.GetHeight(g), microH = limF.GetHeight(g), monoH = mono.GetHeight(g), tagH = tgf.GetHeight(g) + M(2.5f);
            var nf = TextF(note, M(7.2f));
            // the rows' heights first: the bar and the scroll need the whole page
            var lays = new List<RowLay>();
            float total = 0;
            foreach (var p in list)
            {
                var r = new RowLay { Vt = Tune.Format(p, p.V), Ut = T(p.Unit), Lt = T(p.Label).ToUpperInvariant(), Ht = T(p.Help) };
                r.Uf = TextF(r.Ut, M(6.4f));
                r.Uw = r.Ut == "" ? 0 : g.MeasureString(r.Ut, r.Uf, 1000, StringFormat.GenericTypographic).Width;
                r.Vw = g.MeasureString(r.Vt, val, 1000, StringFormat.GenericTypographic).Width;
                r.Vx = x1 - r.Uw - r.Vw - (r.Ut == "" ? 0 : M(2.5f));
                r.Lf = TitleF(r.Lt, M(8.8f));
                // the heading stops short of the value and of the field it opens (Modes.NumberField: 46 wide at least)
                r.HeadW = Math.Max(M(40), Math.Min(r.Vx - M(3), x1 + M(3) - M(46)) - M(6) - x0);
                r.HeadH = Math.Max(valH, g.MeasureString(r.Lt, r.Lf, new SizeF(r.HeadW, 10000), StringFormat.GenericTypographic).Height);
                r.KeyH = Math.Max(monoH, p.NewEventsOnly ? tagH : 0);
                r.Hf = TextF(r.Ht, M(9.6f));   // owner, 23.11.2026: 9.6, not the settings rows' 7.5
                r.HelpH = r.Ht == "" ? 0 : g.MeasureString(r.Ht, r.Hf, new SizeF(x1 - x0, 10000), Wrap).Height;
                r.H = M(5) + r.HeadH + M(1.5f) + r.KeyH + M(7) + M(4.5f) + microH + M(2.5f) + r.HelpH + M(5);
                lays.Add(r);
                total += r.H;
            }
            contentH = M(4) + total + M(5) + g.MeasureString(note, nf, new SizeF(x1 - x0, 10000), Wrap).Height + M(8);
            scroll = Math.Max(0, Math.Min(scroll, Math.Max(0, contentH - area.Height)));
            float y = area.Top + M(4) - scroll;
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i]; var L0 = lays[i];
                var row = new RectangleF(area.Left - M(4), y, x1 - area.Left + M(8), L0.H);
                rows.Add(Tuple.Create(row, p));
                bool rowHot = row.Contains(mouse);
                if (rowHot) using (var rp = HRound(RectangleF.Inflate(row, 0, -M(1)), 6)) using (var b = new SolidBrush(A(10, BodyTheme.Ink))) g.FillPath(b, rp);
                // the heading (owner, 23.11.2026: in capitals), wrapped when long; the value and its unit right of it
                float top = y + M(5);
                g.DrawString(L0.Lt, L0.Lf, Brushes(rowHot ? BodyTheme.Ink : BodyTheme.Label), new RectangleF(x0, top, L0.HeadW, L0.HeadH + M(1)), StringFormat.GenericTypographic);
                var vr = new RectangleF(L0.Vx - M(3), top - M(1), L0.Vw + L0.Uw + M(6), valH + M(2));
                if (editing == p) vr = PaintField(g, p, val, L0.Uf, L0.Ut, x1, top - M(2));
                values.Add(Tuple.Create(vr, p));
                if (editing != p)
                {
                    if (p.Changed && !DpsForm.Potato && !DpsForm.EyeComfort)   // a changed value glows as the screen's headings do
                        using (var halo = new SolidBrush(A(28, BodyTheme.Accent)))
                            foreach (var o in new[] { new PointF(-0.6f, 0), new PointF(0.6f, 0), new PointF(0, -0.6f), new PointF(0, 0.6f) })
                                g.DrawString(L0.Vt, val, halo, L0.Vx + o.X * K, top + o.Y * K, StringFormat.GenericTypographic);
                    g.DrawString(L0.Vt, val, Brushes(p.Changed ? BodyTheme.Accent : BodyTheme.Ink), L0.Vx, top, StringFormat.GenericTypographic);
                    if (L0.Ut != "") g.DrawString(L0.Ut, L0.Uf, Brushes(Dim), x1 - L0.Uw, top + Ascent(val) - Ascent(L0.Uf), StringFormat.GenericTypographic);
                }
                // the key and the "new events only" tag on their own line under the heading
                float ky = top + L0.HeadH + M(1.5f), kmid = ky + L0.KeyH / 2;
                g.DrawString(p.Key, mono, Brushes(Faint), x0, kmid - monoH / 2, StringFormat.GenericTypographic);
                if (p.NewEventsOnly)
                {
                    float tx = x0 + g.MeasureString(p.Key, mono, 1000, StringFormat.GenericTypographic).Width + M(5);
                    var ts = g.MeasureString(tag, tgf, 1000, StringFormat.GenericTypographic);
                    var tr = new RectangleF(tx, kmid - tagH / 2, ts.Width + M(7), tagH);
                    using (var path = HRound(tr, 4)) using (var pen = new Pen(A(60, BodyTheme.Ink), 1)) g.DrawPath(pen, path);
                    g.DrawString(tag, tgf, Brushes(Dim), tr.Left + M(3.5f), tr.Top + (tr.Height - tgf.GetHeight(g)) / 2 + M(0.3f), StringFormat.GenericTypographic);
                }
                // the track; the reset key right of it while the value differs
                float resetW = M(13);
                float tcy = ky + L0.KeyH + M(7);
                var track = RectangleF.FromLTRB(x0 + M(3), tcy - M(0.8f), x1 - resetW - M(8), tcy + M(0.8f));
                var hitTrack = RectangleF.FromLTRB(track.Left - U(6), tcy - M(6), track.Right + U(6), tcy + M(6));
                tracks.Add(Tuple.Create(hitTrack, p));
                using (var path = Theme.Round(track, M(0.8f), 1f)) using (var b = new SolidBrush(A(34, BodyTheme.Ink))) g.FillPath(b, path);
                float fx = track.Left + (float)Frac(p, p.V) * track.Width, dx = track.Left + (float)Frac(p, p.Def) * track.Width;
                var fill = RectangleF.FromLTRB(track.Left, track.Top, Math.Max(track.Left + M(1), fx), track.Bottom);
                using (var path = Theme.Round(fill, M(0.8f), 1f)) using (var b = new SolidBrush(p.Changed ? BodyTheme.Accent : A(120, BodyTheme.Ink))) g.FillPath(b, path);
                using (var pen = new Pen(A(170, BodyTheme.Silk), Math.Max(1f, M(0.7f)))) g.DrawLine(pen, dx, tcy - M(3.5f), dx, tcy + M(3.5f));
                bool hotKnob = drag == p || hitTrack.Contains(mouse);
                float kd = M(hotKnob ? 7.5f : 6.5f);
                if (p.Changed) DpsForm.SoftGlow(g, new PointF(fx, tcy), kd / 2 + M(4), BodyTheme.Accent, 140);   // none in Potato or eye comfort
                using (var b = new SolidBrush(p.Changed ? BodyTheme.Accent : BodyTheme.Ink)) g.FillEllipse(b, fx - kd / 2, tcy - kd / 2, kd, kd);
                using (var pen = new Pen(Color.Black, Math.Max(1f, M(1)))) g.DrawEllipse(pen, fx - kd / 2, tcy - kd / 2, kd, kd);
                if (p.Changed)
                {
                    var rr = new RectangleF(x1 - resetW, tcy - resetW / 2, resetW, resetW);
                    bool hot = HotKey(rr);
                    using (var path = HRound(rr, 5))
                    {
                        using (var b = new SolidBrush(hot ? A(26, BodyTheme.Ink) : A(12, BodyTheme.Ink))) g.FillPath(b, path);
                        using (var pen = new Pen(A(50, BodyTheme.Ink), 1f)) g.DrawPath(pen, path);
                    }
                    float ri = M(8);
                    MaterialIcon.Draw(g, MaterialIcon.Refresh, new RectangleF(rr.Left + (rr.Width - ri) / 2, rr.Top + (rr.Height - ri) / 2, ri, ri), Brushes(hot ? BodyTheme.Accent : BodyTheme.Ink));
                    resets.Add(Tuple.Create(rr, p));
                }
                // the limits at the ends, the default under its tick while the value differs (kept clear of the limits)
                float ly = tcy + M(4.5f);
                var mn = Tune.Format(p, p.Min); var mx = Tune.Format(p, p.Max);
                float mnw = g.MeasureString(mn, limF, 1000, StringFormat.GenericTypographic).Width, mxw = g.MeasureString(mx, limF, 1000, StringFormat.GenericTypographic).Width;
                g.DrawString(mn, limF, Brushes(Faint), track.Left, ly, StringFormat.GenericTypographic);
                g.DrawString(mx, limF, Brushes(Faint), track.Right - mxw, ly, StringFormat.GenericTypographic);
                if (p.Changed)
                {
                    var dt = T("default") + " " + Tune.Format(p, p.Def); var df = TextF(dt, M(6.4f));
                    float dw = g.MeasureString(dt, df, 1000, StringFormat.GenericTypographic).Width;
                    float lo = track.Left + mnw + M(6), hi = track.Right - mxw - M(6) - dw;
                    g.DrawString(dt, df, Brushes(Dim), Math.Max(lo, Math.Min(hi, dx - dw / 2)), ly, StringFormat.GenericTypographic);
                }
                // what it does: all of it, wrapped
                float hy = ly + microH + M(2.5f);
                if (L0.Ht != "") g.DrawString(L0.Ht, L0.Hf, Brushes(Dim), new RectangleF(x0, hy, x1 - x0, L0.HelpH + M(2)), Wrap);
                using (var b = new SolidBrush(A(20, BodyTheme.Ink))) g.FillRectangle(b, x0, row.Bottom - 1, x1 - x0, 1);
                y += L0.H;
            }
            g.DrawString(note, nf, Brushes(Faint), new RectangleF(x0, y + M(5), x1 - x0, nf.GetHeight(g) * 3), Wrap);
            mono.Dispose();
        }
        // where a font's baseline sits under the top of its line (GenericTypographic draws from the cell's top)
        static float Ascent(Font f) { var ff = f.FontFamily; return f.Size * ff.GetCellAscent(f.Style) / ff.GetEmHeight(f.Style); }

        void PaintInspector(Graphics g, RectangleF area)
        {
            var now = DemoNow ?? DateTime.UtcNow;
            if (report == null) report = body.InspectReport(now);
            // owner, 23.11.2026: on the app's scale and at its Window size - the module names as the cards' titles (8.8),
            // the lines in Consolas at the same size
            var mono = new Font("Consolas", M(8.8f), GraphicsUnit.Pixel);
            var headF = HeadFont(M(8.8f));
            float lh = M(13.6f), x = area.Left + M(16);
            contentH = M(10);
            foreach (var r in report) contentH += r.Head ? M(25) : lh;
            scroll = Math.Max(0, Math.Min(scroll, Math.Max(0, contentH - area.Height)));
            float y = area.Top + M(8) - scroll;
            foreach (var r in report)
            {
                if (r.Head)
                {
                    y += M(7);
                    g.DrawString(r.Text, headF, Brushes(r.Color), x, y);
                    y += M(18);
                    continue;
                }
                if (y > area.Top - lh && y < area.Bottom)
                    g.DrawString(r.Text, mono, Brushes(r.Color), new RectangleF(x, y, area.Right - x - M(10), lh), OneLine);
                y += lh;
            }
            if (frozen)
            {
                var t = T("FROZEN"); var f = headF;   // one size with the module names
                g.DrawString(t, f, Brushes(BodyTheme.Dread), area.Right - M(13) - g.MeasureString(t, f).Width, area.Top + M(8));
            }
            mono.Dispose();
        }

        // a value's place on its track 0..1: linear, or logarithmic for a range of 50x or more
        static bool LogScale(Tune.P p) { return p.Min > 0 && p.Max / p.Min >= 50; }
        static double Frac(Tune.P p, double v)
        {
            if (LogScale(p)) return (Math.Log(v) - Math.Log(p.Min)) / (Math.Log(p.Max) - Math.Log(p.Min));
            return (v - p.Min) / (p.Max - p.Min);
        }
        static double FromFrac(Tune.P p, double f)
        {
            f = Math.Max(0, Math.Min(1, f));
            double v = LogScale(p) ? Math.Exp(Math.Log(p.Min) + f * (Math.Log(p.Max) - Math.Log(p.Min))) : p.Min + f * (p.Max - p.Min);
            return Math.Round(v / p.Step) * p.Step;
        }

        static readonly StringFormat OneLine = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        static readonly Dictionary<Color, SolidBrush> brushes = new Dictionary<Color, SolidBrush>();
        static Brush Brushes(Color c) { SolidBrush b; if (!brushes.TryGetValue(c, out b)) brushes[c] = b = new SolidBrush(c); return b; }

        // ---------------- input (as the body's) ----------------
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mouse = e.Location;
            if (barGrip != null) { PullBar(e.Y); return; }
            if (drag != null) { SetFromTrack(drag, e.X); Redraw(); return; }
            lightAt = !DpsForm.Potato && (navGlassR.Contains(mouse) || mainGlassR.Contains(mouse)) ? (PointF?)mouse : null;
            // the tip after a short rest on a hull key, not at once
            var id = SpotAt(e.Location);
            if (id != tipFor)
            {
                tipFor = id; tip.Hide(this);
                var kr = id == null ? RectangleF.Empty : keys.Where(x => x.Item2 == id).Select(x => x.Item1).FirstOrDefault();
                tipText = id == "close" ? T("Close") : id == "resetall" ? T("Reset all") : id == "openfile" ? T("Open file") : id == "delete" ? T("Clear and delete custom tuning") : id == "freeze" ? T(frozen ? "Resume" : "Freeze") : null;
                tipAt = new Point((int)kr.Left, (int)(kr.Bottom + M(4)));
                tipDue = Clock + 0.7;
            }
            var hover = HoverState();
            if (hover != shownHover) { shownHover = hover; Redraw(); } else Present();
        }
        // what the frame shows of the mouse: the key, the row, the track under it
        string shownHover;
        string HoverState()
        {
            var r = rows.FirstOrDefault(x => x.Item1.Contains(mouse)); var t = tracks.FirstOrDefault(x => x.Item1.Contains(mouse));
            return (SpotAt(mouse) ?? "") + "|" + (r == null ? "" : r.Item2.Key) + "|" + (t == null ? "" : t.Item2.Key) + "|" + barTrack.Contains(mouse);
        }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); mouse = new Point(-1, -1); lightAt = null; shownHover = null; downId = null; tipFor = null; tipText = null; tip.Hide(this); Redraw(); }

        void SetFromTrack(Tune.P p, int mx)
        {
            var t = tracks.FirstOrDefault(x => x.Item2 == p);
            if (t == null) return;
            var r = t.Item1; float left = r.Left + U(6), right = r.Right - U(6);
            Tune.Set(p, FromFrac(p, (mx - left) / (right - left)));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            tipText = null; tip.Hide(this);   // a click: no tip
            if (e.Button != MouseButtons.Left) return;
            var id = SpotAt(e.Location);
            if (editing != null && id != "field:" + editing.Key) CommitField();   // a click elsewhere keeps the number
            if (barTrack.Contains(e.Location))
            {
                float barY = barView.Top + (barView.Height - barH) * Math.Min(scroll, barMax) / barMax;
                barGrip = e.Y >= barY && e.Y <= barY + barH ? e.Y - barY : barH / 2;
                PullBar(e.Y); return;
            }
            if (id != null) { downId = id; Redraw(); return; }
            var t = tracks.FirstOrDefault(x => x.Item1.Contains(e.Location));
            if (t != null) { drag = t.Item2; Capture = true; SetFromTrack(drag, e.X); Redraw(); }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            bool left = e.Button == MouseButtons.Left;
            drag = null; Capture = false; barGrip = null;
            var id = downId; downId = null;
            if (id != null && left && SpotAt(e.Location) == id) Press(id);   // a key acts on release, over the same key
            Redraw();
        }
        void PullBar(int y)
        {
            float room = Math.Max(1, barView.Height - barH);
            scroll = Math.Max(0, Math.Min(barMax, (y - barGrip.Value - barView.Top) / room * barMax));
            Redraw();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            var row = rows.FirstOrDefault(x => x.Item1.Contains(e.Location));
            if ((Mods & Keys.Control) != 0 && row != null)
            {
                var p = row.Item2; double step = p.Step * ((Mods & Keys.Shift) != 0 ? 10 : 1);
                Tune.Set(p, p.V + Math.Sign(e.Delta) * step);
            }
            else scroll -= Math.Sign(e.Delta) * M(24);   // the settings' step
            Redraw();
        }

        void Press(string id)
        {
            if (id == "close") { Hide(); return; }
            if (id == "resetall") { Tune.ResetAll(); Redraw(); return; }
            if (id == "freeze") { frozen = !frozen; report = null; Redraw(); return; }
            if (id == "delete") { body.AskDeleteTuning(this); return; }
            if (id.StartsWith("reset:")) { var p = Tune.All.First(x => x.Key == id.Substring(6)); Tune.Set(p, p.Def); Redraw(); return; }
            if (id.StartsWith("field:")) { editing = Tune.All.First(x => x.Key == id.Substring(6)); editText = null; Redraw(); return; }
            if (id == "openfile")
            {
                if (!System.IO.File.Exists(Tune.FilePath)) Tune.Save();
                try { System.Diagnostics.Process.Start("notepad.exe", "\"" + Tune.FilePath + "\""); } catch { }
                return;
            }
            if (id.StartsWith("page:")) { ShowPage(id.Substring(5)); Redraw(); }
        }

        // typing a value (Modes.cs): digits and one decimal point (comma too) where the value has decimals; Backspace;
        // Enter keeps it, Tab keeps it and goes to the next value of the page (Shift+Tab back), Escape leaves it as it was
        internal void CommitField()
        {
            var p = editing; var text = editText;
            editing = null; editText = null;
            double v;
            if (p != null && !string.IsNullOrEmpty(text) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) Tune.Set(p, v);
            Redraw();
        }
        internal bool EditKey(Keys keyData)
        {
            if (editing == null) return false;
            var key = keyData & Keys.KeyCode;
            if (key == Keys.Enter) { CommitField(); return true; }
            if (key == Keys.Tab)
            {
                var list = Tune.All.Where(x => x.Group == editing.Group).ToList();
                int i = list.IndexOf(editing);
                CommitField();
                editing = list[(i + ((keyData & Keys.Shift) != 0 ? list.Count - 1 : 1)) % list.Count]; editText = null;
                Redraw(); return true;
            }
            if (key == Keys.Escape) { editing = null; editText = null; Redraw(); return true; }
            if (key == Keys.Back) { editText = string.IsNullOrEmpty(editText) ? "" : editText.Substring(0, editText.Length - 1); Redraw(); return true; }
            return false;
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) { return EditKey(keyData) || base.ProcessCmdKey(ref msg, keyData); }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (editing == null) return;
            e.Handled = true;
            char ch = e.KeyChar == ',' ? '.' : e.KeyChar;
            bool dec = !editing.Int;
            if (!(ch >= '0' && ch <= '9') && !(dec && ch == '.')) return;
            var t = editText ?? "";
            if (ch == '.' && (t.Contains('.') || t.Length == 0)) return;
            var next = t + ch;
            double v;
            if (ch != '.' && (!double.TryParse(next, NumberStyles.Float, CultureInfo.InvariantCulture, out v) || v > editing.Max))
            {
                errP = editing; errAt = Clock; Redraw();   // over the limit: red, one shake
                return;
            }
            editText = next;
            Redraw();
        }

        // --selftest-dps: the window on two groups (one value changed) and the Inspector over the self-test's fleet. Tuning is
        // switched on only in memory: nothing is read from or written to tuning.json, and every value is put back after.
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessage")] static extern IntPtr SendMsg(IntPtr h, int msg, IntPtr w, IntPtr l);
        public static void SelfTest(DpsForm f, string dir, DateTime now, System.Text.StringBuilder log)
        {
            // a double click on a hull (HTCAPTION) does nothing: neither the body nor this window maximizes (owner, 24.11.2026)
            using (var dw = new TuneWindow(f))
                foreach (var form in new Form[] { f, dw })
                {
                    var keep = form.WindowState;
                    SendMsg(form.Handle, 0xA3 /* WM_NCLBUTTONDBLCLK */, (IntPtr)2 /* HTCAPTION */, IntPtr.Zero);
                    if (form.WindowState == FormWindowState.Maximized) throw new Exception("self-test: a double click on the hull maximized " + form.GetType().Name);
                    form.WindowState = keep;
                }
            bool was = Tune.Enabled;
            Tune.Enabled = true;
            Tune.FileOverride = System.IO.Path.Combine(dir, "tuning_selftest.json");   // never the user's file
            try
            {
                Interact(f, now, log);
                Tune.SbCycleGapS.V = 7.5; Tune.HeatFallTau.V = 6; Tune.Version++;
                var t = now.ToLocalTime();
                Inspect.Demo(new[]
                {
                    new Inspect.LogLine { Wall = t.AddSeconds(-3), Pilot = "Pilot-1", Kind = "DMG>", Text = "[ 2026.11.20 18:41:07 ] (combat) 412 to Sleepless Warden - Large Vorton Projector II - Hits" },
                    new Inspect.LogLine { Wall = t.AddSeconds(-3), Pilot = "Pilot-5", Kind = "DMG>", Text = "[ 2026.11.20 18:41:07 ] (combat) 183 to Core Grand Admiral - Caldari Navy Large Graviton Smartbomb - Hits" },
                    new Inspect.LogLine { Wall = t.AddSeconds(-2), Pilot = "Pilot-2", Kind = "ISK ", Text = "[ 2026.11.20 18:41:08 ] (bounty) 29 250 ISK added to next bounty payout" },
                    new Inspect.LogLine { Wall = t.AddSeconds(-2), Pilot = "Pilot-3", Kind = "DMG<", Text = "[ 2026.11.20 18:41:08 ] (combat) 96 from Corelatis High Captain - Hits" },
                    new Inspect.LogLine { Wall = t.AddSeconds(-1), Pilot = "Pilot-4", Kind = "    ", Text = "[ 2026.11.20 18:41:09 ] (notify) Your Large Shield Booster II is repaired" },
                });
                using (var w = new TuneWindow(f) { DemoNow = now })
                {
                    var keepLang = Lang;
                    foreach (var lg in L.Languages.Select(l => l[0]))
                    {
                        Lang = lg;
                        foreach (var pg in new[] { "Smartbombs", "Anomaly timer", "Inspector" })
                        {
                            w.ShowPage(pg);
                            using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_" + (lg == "en" ? "" : lg + "_") + pg.ToLowerInvariant().Replace(" ", "_") + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    langOverride = null;
                    // every page's name fits the side menu at its one size, with the dot of changed values: none runs by
                    Tune.VortonKernelS.V = Tune.VortonKernelS.Def * 2; Tune.Version++;
                    w.ShowPage("Vorton projector");
                    using (w.Render()) { }
                    Tune.VortonKernelS.V = Tune.VortonKernelS.Def; Tune.Version++;
                    if (w.running.Count > 0) throw new Exception("Tuning self-test: a side-menu name does not fit: " + string.Join(", ", w.running.Select(q => q.Text)));
                    // the mouse as the body's: a hull key held, the light on the glass, a value being typed in
                    w.ShowPage("Smartbombs");
                    using (w.Render()) { }
                    var rk = w.keys.First(x => x.Item2 == "resetall").Item1;
                    w.mouse = Point.Round(new PointF(rk.Left + rk.Width / 2, rk.Top + rk.Height / 2)); w.downId = "resetall";
                    using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_key_held.png"), System.Drawing.Imaging.ImageFormat.Png);
                    w.downId = null;
                    var ok = w.keys.First(x => x.Item2 == "openfile").Item1;
                    w.mouse = Point.Round(new PointF(ok.Left + ok.Width / 2, ok.Top + ok.Height / 2));
                    using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_key_hover.png"), System.Drawing.Imaging.ImageFormat.Png);
                    w.mouse = new Point(-1, -1);
                    w.DemoMouse = new PointF(w.mainGlassR.Left + w.mainGlassR.Width * 0.55f, w.mainGlassR.Top + w.mainGlassR.Height * 0.42f);
                    using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_glass_light.png"), System.Drawing.Imaging.ImageFormat.Png);
                    w.DemoMouse = null;
                    w.editing = Tune.SbCycleGapS; w.editText = "8.";
                    using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_field_typing.png"), System.Drawing.Imaging.ImageFormat.Png);
                    w.errP = Tune.SbCycleGapS; w.errAt = Clock - 0.5;   // past the shake, still red
                    using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_field_error.png"), System.Drawing.Imaging.ImageFormat.Png);
                    w.editing = null; w.editText = null; w.errP = null;
                    // the app switched to Potato mode while the window is open: it follows at once (5 fps, plain hull, no
                    // light, no glow), and back
                    w.SyncLook();
                    DpsForm.Potato = true; w.SyncLook();
                    if (w.frameTimer.Interval != 200) throw new Exception("Tuning self-test: Potato did not slow the window to 5 fps");
                    w.DemoMouse = new PointF(w.mainGlassR.Left + w.mainGlassR.Width * 0.55f, w.mainGlassR.Top + w.mainGlassR.Height * 0.42f);
                    using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_potato.png"), System.Drawing.Imaging.ImageFormat.Png);
                    DpsForm.Potato = false; w.SyncLook(); w.DemoMouse = null;
                    if (w.frameTimer.Interval != 40) throw new Exception("Tuning self-test: leaving Potato did not bring back 25 fps");
                    // Settings -> Display -> Window size while the window is open: it takes the new size at once, and back
                    float scale0 = DpsForm.UiScale; var size0 = w.ClientSize;
                    var wa0 = Screen.FromControl(w).WorkingArea;
                    foreach (var sc in new[] { 1f, 1.25f, 1.5f })
                    {
                        DpsForm.UiScale = sc; w.SyncLook();
                        int want = Math.Min((int)Math.Round(HW * K * sc), wa0.Width - 20);
                        if (w.ClientSize.Width != want) throw new Exception("Tuning self-test: window size " + sc + " gave " + w.ClientSize.Width + " px, not " + want);
                        using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_size_" + sc.ToString(CultureInfo.InvariantCulture) + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                        w.ShowPage("Inspector");   // the Inspector follows the size too
                        using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_size_" + sc.ToString(CultureInfo.InvariantCulture) + "_inspector.png"), System.Drawing.Imaging.ImageFormat.Png);
                        w.ShowPage("Smartbombs");
                    }
                    DpsForm.UiScale = scale0; w.SyncLook();
                    if (w.ClientSize != size0) throw new Exception("Tuning self-test: the window did not go back to its size");
                    // Settings -> Display while the window is open: eye comfort, the hull's colour, the power display's animation
                    bool keepEc = DpsForm.EyeComfort; int keepSat = DpsForm.HullSaturation, keepBr = DpsForm.HullBrightness; var keepAn = DpsForm.PowerAnimName;
                    var look0 = w.look;
                    DpsForm.EyeComfort = true; DpsForm.HullSaturation = 85; DpsForm.HullBrightness = 30; DpsForm.PowerAnimName = PowerAnims.Names.First(n => n != keepAn);
                    DpsForm.ApplyTheme(); w.SyncLook();
                    if (w.look == look0) throw new Exception("Tuning self-test: the window did not follow the app's Display settings");
                    using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_theme.png"), System.Drawing.Imaging.ImageFormat.Png);
                    DpsForm.EyeComfort = keepEc; DpsForm.HullSaturation = keepSat; DpsForm.HullBrightness = keepBr; DpsForm.PowerAnimName = keepAn;
                    DpsForm.ApplyTheme(); w.SyncLook();
                    // what a frame costs: the whole window painted again, or only the light on the glass moved
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    for (int i = 0; i < 5; i++) using (w.Render()) { }
                    double full = sw.Elapsed.TotalMilliseconds / 5;
                    using (var fb = new Bitmap(w.ClientSize.Width, w.ClientSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        sw.Restart();
                        for (int i = 0; i < 20; i++) w.Compose(fb, (PointF?)new PointF(w.mainGlassR.Left + 40 + i * 9, w.mainGlassR.Top + 120 + i * 4));
                    }
                    log.AppendLine(string.Format(CultureInfo.InvariantCulture, "tuning frame: full paint {0:0} ms, light only {1:0} ms", full, sw.Elapsed.TotalMilliseconds / 20));
                }
            }
            finally
            {
                foreach (var p in Tune.All) p.V = p.Def;
                Inspect.Demo(new Inspect.LogLine[0]);
                try { System.IO.File.Delete(Tune.FileOverride); } catch { }
                Tune.FileOverride = null;
                Tune.Enabled = was; Tune.Version++;
            }
        }

        // the window's input and the file, as the owner would use them: a drag on a track, the reset key, Ctrl+wheel,
        // a typed value; then tuning.json: saved, read back, a broken file, a value out of its limits
        static void Interact(DpsForm f, DateTime now, System.Text.StringBuilder log)
        {
            Action<bool, string> check = (ok, what) => { if (!ok) throw new Exception("Tuning self-test: " + what); };
            var gap = Tune.SbCycleGapS;
            var keepL = Lang; Lang = "en";
            using (var w = new TuneWindow(f) { DemoNow = now })
            {
                Action<PointF> click = pt => { w.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, (int)pt.X, (int)pt.Y, 0)); w.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, (int)pt.X, (int)pt.Y, 0)); using (w.Render()) { } };
                Func<RectangleF, PointF> mid = r => new PointF(r.Left + r.Width / 2, r.Top + r.Height / 2);
                w.ShowPage("Smartbombs");
                using (w.Render()) { }   // the hit areas
                var tr = w.tracks.First(x => x.Item2 == gap).Item1;
                float left = tr.Left + U(6), right = tr.Right - U(6);
                int mx = (int)(left + (right - left) * Frac(gap, 12));
                int my = (int)((tr.Top + tr.Bottom) / 2);
                w.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, mx, my, 0));
                w.OnMouseMove(new MouseEventArgs(MouseButtons.Left, 0, mx, my, 0));
                w.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, mx, my, 0));
                check(Math.Abs(gap.V - 12) <= gap.Step, "drag to 12 s gave " + gap.V);
                check(System.IO.File.Exists(Tune.FilePath) && System.IO.File.ReadAllText(Tune.FilePath).Contains("sb.cycle_gap_s"), "the drag was not saved");
                using (w.Render()) { }
                var rr = w.resets.First(x => x.Item2 == gap).Item1;
                click(mid(rr));
                check(!gap.Changed, "reset left " + gap.V);
                using (w.Render()) { }
                var row = w.rows.First(x => x.Item2 == Tune.SbMinGcd).Item1;
                var keepMods = ModsOverride; ModsOverride = Keys.Control;
                w.OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, (int)(row.Left + U(40)), (int)(row.Top + U(10)), 120));
                ModsOverride = keepMods;
                check(Tune.SbMinGcd.I == 4, "Ctrl+wheel gave " + Tune.SbMinGcd.V);
                using (w.Render()) { }
                // the value typed in its field: a click on it, the keys, Enter; over the limit refused; Esc leaves it
                click(mid(w.values.First(x => x.Item2 == gap).Item1));
                check(w.editing == gap, "a click on the value did not open its field");
                foreach (var ch in "8,5") w.OnKeyPress(new KeyPressEventArgs(ch));
                w.EditKey(Keys.Enter);
                check(Math.Abs(gap.V - 8.5) < 1e-9 && w.editing == null, "typed 8,5 gave " + gap.V);
                click(mid(w.values.First(x => x.Item2 == gap).Item1));
                foreach (var ch in "99") w.OnKeyPress(new KeyPressEventArgs(ch));
                check(w.editText == "9" && w.errP == gap, "99 s (over " + gap.Max + ") was not refused");
                w.EditKey(Keys.Escape);
                check(Math.Abs(gap.V - 8.5) < 1e-9 && w.editing == null, "Esc changed the value to " + gap.V);
                // a key acts on release over it: pressed and moved off, nothing happens
                using (w.Render()) { }
                var rk = w.keys.First(x => x.Item2 == "resetall").Item1;
                w.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, (int)(rk.Left + rk.Width / 2), (int)(rk.Top + rk.Height / 2), 0));
                w.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, (int)(rk.Left - M(30)), (int)(rk.Top + rk.Height / 2), 0));
                check(gap.Changed, "Reset all acted though released off the key");
                // the language: the app's, followed while the window is open
                langOverride = null;
                var keepCode = L.Code;
                L.Code = "de"; w.SyncLook();
                check(Lang == "de" && T("Reset all") == "Alles zurücksetzen", "the app in German gave " + Lang);
                L.Code = keepCode; w.SyncLook(); Lang = "en";
            }
            // the file: what was saved reads back; a broken file and a value out of its limits
            var saved = System.IO.File.ReadAllText(Tune.FilePath);
            foreach (var p in Tune.All) p.V = p.Def;
            Tune.Load(false);
            check(Math.Abs(gap.V - 8.5) < 1e-9 && Tune.SbMinGcd.I == 4, "read back gave " + gap.V + " / " + Tune.SbMinGcd.V);
            System.IO.File.WriteAllText(Tune.FilePath, "{ \"sb.cycle_gap_s\": 3, \"sb.min_gcd\": ");
            Tune.Load(false);
            check(Math.Abs(gap.V - 8.5) < 1e-9 && Tune.LastError != null, "a broken file changed the values");
            System.IO.File.WriteAllText(Tune.FilePath, "{ \"sb.cycle_gap_s\": 999, \"sb.min_gcd\": 2 }");
            Tune.Load(false);
            check(gap.V == gap.Max && Tune.SbMinGcd.I == 2 && Tune.LastError == null, "limits gave " + gap.V);
            foreach (var p in Tune.All) p.V = p.Def;
            Tune.Version++;
            langOverride = null;
            log.AppendLine("tuning: drag, reset, Ctrl+wheel, typed value (limit, Esc), release off a key, language, save / read back, broken file, limits: ok (" + saved.Length + " bytes saved)");
        }
        public static Keys? ModsOverride;   // selftest: the modifier keys of a simulated wheel
        static Keys Mods { get { return ModsOverride ?? ModifierKeys; } }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // the close key hides it; the app's exit disposes it
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); }
            base.OnFormClosing(e);
        }
    }
}
