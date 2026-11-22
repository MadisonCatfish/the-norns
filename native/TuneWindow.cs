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
        internal TextBox edit; Tune.P editing;
        List<DpsForm.InspectRow> report;
        Point mouse = new Point(-1, -1);
        // hit areas of the last frame
        readonly List<Tuple<RectangleF, string>> keys = new List<Tuple<RectangleF, string>>();
        readonly List<Tuple<RectangleF, Tune.P>> tracks = new List<Tuple<RectangleF, Tune.P>>(), values = new List<Tuple<RectangleF, Tune.P>>(), rows = new List<Tuple<RectangleF, Tune.P>>(), resets = new List<Tuple<RectangleF, Tune.P>>();
        float contentH;

        // the window's language (owner, 21.11.2026; all the app's languages 22.11.2026): at first the app's; the header key
        // opens a list of L.Languages (owner, 22.11.2026: a dropdown). The texts are native\lang\tune.<code>.json (English -> that language), kept apart from
        // the app's own <code>.json; a missing entry shows the English.
        public static string Lang = L.Known(L.Code) ? L.Code : "en";
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
        const float Wd = 840, Ht = 640, Head = 46, Nav = 180, Foot = 30, RowH = 94;
        public DateTime? DemoNow;    // selftest: a fixed clock

        public TuneWindow(DpsForm body)
        {
            this.body = body;
            Text = AppInfo.Name + " — Tuning";
            Icon = AppInfo.Icon;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            var wa = Screen.PrimaryScreen.WorkingArea;   // never larger than the screen (a large Windows scale)
            ClientSize = new Size(Math.Min((int)U(Wd), wa.Width - 20), Math.Min((int)U(Ht), wa.Height - 20));
            TopMost = true;
            KeyPreview = true;
            BackColor = Color.Black;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            page = Tune.Groups.First();
            timer.Tick += (s, e) => { if (page == "Inspector" && !frozen) { report = null; Invalidate(); } else if (Tune.Version != shownVersion) Invalidate(); };
            timer.Start();
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape && edit == null) { if (langOpen) { langOpen = false; Invalidate(); } else Hide(); } };
        }
        int shownVersion = -1;

        protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }

        // the header drags the window (not its keys)
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x84 /* WM_NCHITTEST */ && (int)m.Result == 1)
            {
                var p = PointToClient(new Point((short)(m.LParam.ToInt64() & 0xFFFF), (short)((m.LParam.ToInt64() >> 16) & 0xFFFF)));
                if (p.Y < U(Head) && !keys.Any(k => k.Item1.Contains(p))) m.Result = (IntPtr)2;   // HTCAPTION
            }
        }

        // ---------------- painting ----------------
        protected override void OnPaint(PaintEventArgs e) { Paint(e.Graphics, ClientSize); }

        public Bitmap Render()
        {
            var b = new Bitmap(ClientSize.Width, ClientSize.Height);
            using (var g = Graphics.FromImage(b)) Paint(g, ClientSize);
            return b;
        }
        public void ShowPage(string p) { page = p; scroll = 0; report = null; }

        static readonly Color Hull = Color.FromArgb(24, 26, 30), Panel = Color.FromArgb(14, 15, 17), Line = Color.FromArgb(44, 47, 54),
                              Dim = Color.FromArgb(150, 154, 162), Faint = Color.FromArgb(96, 100, 108);

        void Paint(Graphics g, Size size)
        {
            shownVersion = Tune.Version;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            keys.Clear(); tracks.Clear(); values.Clear(); rows.Clear(); resets.Clear();
            var all = new RectangleF(0, 0, size.Width, size.Height);
            using (var b = new SolidBrush(Hull)) g.FillRectangle(b, all);
            using (var p = new Pen(Line, 1)) g.DrawRectangle(p, 0, 0, size.Width - 1, size.Height - 1);

            // header
            var title = HeadFont(U(13));
            var small = BodyFont(U(9.5f));
            var tt = T("TUNING");
            g.DrawString(tt, title, Brushes(BodyTheme.Ink), U(18), U(12));
            float tw = g.MeasureString(tt, title).Width;
            g.DrawString(AppInfo.Name + " " + AppInfo.VersionText + (DpsForm.TestLabel != null ? " · " + DpsForm.TestLabel : ""), small, Brushes(Dim), U(18) + tw + U(6), U(17));
            float kx = size.Width - U(14);
            kx = Key(g, "✕", "close", kx, U(10), true);
            kx = Key(g, T("Reset all"), "resetall", kx - U(6), U(10), Tune.AnyChanged);
            kx = Key(g, T("Open file"), "openfile", kx - U(6), U(10), true);
            kx = Key(g, T("Language") + ": " + Lang.ToUpperInvariant(), "lang", kx - U(6), U(10), true, true);   // the window's language: opens the list
            if (page == "Inspector") Key(g, T(frozen ? "Resume" : "Freeze"), "freeze", kx - U(6), U(10), true);
            using (var p = new Pen(Line, 1)) g.DrawLine(p, 0, U(Head), size.Width, U(Head));

            // navigation
            var nav = RectangleF.FromLTRB(0, U(Head), U(Nav), size.Height - U(Foot));
            using (var b = new SolidBrush(Panel)) g.FillRectangle(b, nav);
            float y = nav.Top + U(10);
            var navFont = BodyFont(U(11.5f));
            foreach (var grp in Tune.Groups.Concat(new[] { "", "Inspector" }))
            {
                if (grp == "") { y += U(8); using (var p = new Pen(Line, 1)) g.DrawLine(p, U(14), y, nav.Right - U(14), y); y += U(8); continue; }
                var r = new RectangleF(0, y, nav.Width, U(32));
                bool on = grp == page, hot = r.Contains(mouse);
                if (on || hot) using (var b = new SolidBrush(on ? Color.FromArgb(30, 33, 38) : Color.FromArgb(22, 24, 28))) g.FillRectangle(b, r);
                if (on) using (var b = new SolidBrush(BodyTheme.Accent)) g.FillRectangle(b, 0, r.Top + U(6), U(3), r.Height - U(12));
                g.DrawString(T(grp), navFont, Brushes(on ? BodyTheme.Ink : Dim), U(18), r.Top + (r.Height - navFont.GetHeight(g)) / 2);
                int n = Tune.All.Count(p => p.Group == grp && p.Changed);
                if (n > 0)
                {
                    var t = n.ToString(CultureInfo.InvariantCulture); var f = NornsFonts.Screen(U(8.5f));
                    var sz = g.MeasureString(t, f);
                    var pill = new RectangleF(nav.Right - U(14) - sz.Width - U(8), r.Top + (r.Height - U(16)) / 2, sz.Width + U(8), U(16));
                    using (var path = Theme.Round(pill, U(8), 0.85f)) using (var b = new SolidBrush(BodyTheme.Accent)) g.FillPath(b, path);
                    g.DrawString(t, f, Brushes(Color.Black), pill.Left + U(4), pill.Top + (pill.Height - f.GetHeight(g)) / 2);
                }
                keys.Add(Tuple.Create(r, "page:" + grp));
                y += r.Height;
            }

            // content
            var content = RectangleF.FromLTRB(nav.Right, U(Head), size.Width, size.Height - U(Foot));
            var state = g.Save();
            g.SetClip(content);
            if (page == "Inspector") PaintInspector(g, content); else PaintGroup(g, content);
            g.Restore(state);
            if (contentH > content.Height + 1)
            {
                float th = content.Height * content.Height / contentH, ty = content.Top + (content.Height - th) * scroll / (contentH - content.Height);
                using (var path = Theme.Round(new RectangleF(content.Right - U(7), ty + U(3), U(3), th - U(6)), U(1.5f), 0.85f)) using (var b = new SolidBrush(Color.FromArgb(110, 116, 126))) g.FillPath(b, path);
            }

            // footer
            var foot = RectangleF.FromLTRB(0, size.Height - U(Foot), size.Width, size.Height);
            using (var b = new SolidBrush(Panel)) g.FillRectangle(b, foot);
            using (var p = new Pen(Line, 1)) g.DrawLine(p, 0, foot.Top, size.Width, foot.Top);
            int changed = Tune.All.Count(p => p.Changed);
            string status = Tune.LastError ?? (changed == 0 ? T("All values are the defaults.") : string.Format(CultureInfo.InvariantCulture, T(changed == 1 ? "{0} value differs from the defaults · kept in {1}" : "{0} values differ from the defaults · kept in {1}"), changed, Tune.FilePath));
            g.DrawString(status, small, Brushes(Tune.LastError != null ? BodyTheme.Alarm : changed > 0 ? BodyTheme.Accent : Dim), U(14), foot.Top + (foot.Height - small.GetHeight(g)) / 2);
            string note = T("Formula cards show the default numbers.");
            g.DrawString(note, small, Brushes(Faint), size.Width - U(14) - g.MeasureString(note, small).Width, foot.Top + (foot.Height - small.GetHeight(g)) / 2);
            if (langOpen) PaintLangMenu(g);
        }

        // ---------------- the language list ----------------
        // every language of the app by its own name, the one in use with a check; a click on a name takes it, a click
        // anywhere else or Esc closes the list
        const string DropDown = "M7 10l5 5 5-5z", DropUp = "M7 14l5-5 5 5z";   // Material arrow_drop_down / arrow_drop_up
        bool langOpen;
        RectangleF langKey, langBox;
        readonly List<Tuple<RectangleF, string>> langRows = new List<Tuple<RectangleF, string>>();
        bool LangMenuOver(PointF p) { return langOpen && langBox.Contains(p); }
        static Font NameFont(string name, float px)
        {
            return L.Cjk(name) ? NornsFonts.Sc(px * NornsFonts.MatchSc(FontStyle.Regular, false)) : NornsFonts.Body(px);
        }
        void PaintLangMenu(Graphics g)
        {
            langRows.Clear();
            float rowH = U(30), pad = U(5), w = langKey.Width;
            foreach (var l in L.Languages) w = Math.Max(w, U(44) + g.MeasureString(l[1], NameFont(l[1], U(11))).Width + U(16));
            langBox = new RectangleF(langKey.Right - w, langKey.Bottom + U(4), w, L.Languages.Length * rowH + 2 * pad);
            var shadow = langBox; shadow.Offset(0, U(3));
            using (var path = Theme.Round(shadow, U(9), 0.85f)) using (var b = new SolidBrush(Color.FromArgb(110, 0, 0, 0))) g.FillPath(b, path);
            using (var path = Theme.Round(langBox, U(9), 0.85f))
            {
                using (var b = new SolidBrush(Color.FromArgb(30, 33, 38))) g.FillPath(b, path);
                using (var p = new Pen(BodyTheme.KeyRim, 1)) g.DrawPath(p, path);
            }
            float y = langBox.Top + pad;
            foreach (var l in L.Languages)
            {
                var r = new RectangleF(langBox.Left + pad, y, langBox.Width - 2 * pad, rowH);
                bool cur = l[0] == Lang, hot = r.Contains(mouse);
                if (hot) using (var path = Theme.Round(r, U(6), 0.85f)) using (var b = new SolidBrush(Color.FromArgb(48, 51, 57))) g.FillPath(b, path);
                if (cur) MaterialIcon.Draw(g, MaterialIcon.Check, new RectangleF(r.Left + U(8), r.Top + (rowH - U(16)) / 2, U(16), U(16)), Brushes(BodyTheme.Accent));
                var f = NameFont(l[1], U(11));
                g.DrawString(l[1], f, Brushes(cur || hot ? BodyTheme.Ink : Dim), r.Left + U(32), r.Top + (rowH - f.GetHeight(g)) / 2);
                langRows.Add(Tuple.Create(r, l[0]));
                y += rowH;
            }
        }
        void SetLang(string code) { Lang = code; langOpen = false; report = null; Invalidate(); }

        // a key of the header, right-aligned at x; returns its left edge
        float Key(Graphics g, string text, string id, float right, float top, bool enabled, bool menu = false)
        {
            var f = BodyFont(U(10.5f));
            var sz = g.MeasureString(text, f);
            float arrow = menu ? U(16) : 0;
            var r = new RectangleF(right - sz.Width - U(20) - arrow, top, sz.Width + U(20) + arrow, U(26));
            bool hot = enabled && (r.Contains(mouse) && !LangMenuOver(mouse) || menu && langOpen);
            using (var path = Theme.Round(r, U(8), 0.85f))
            {
                using (var b = new LinearGradientBrush(r, hot ? Color.FromArgb(60, 63, 70) : BodyTheme.KeyTop, BodyTheme.KeyBottom, 90f)) g.FillPath(b, path);
                using (var p = new Pen(BodyTheme.KeyRim, 1)) g.DrawPath(p, path);
            }
            g.DrawString(text, f, Brushes(!enabled ? Faint : id == "close" && hot ? BodyTheme.Alarm : BodyTheme.Ink), r.Left + U(10), r.Top + (r.Height - f.GetHeight(g)) / 2);
            if (menu) MaterialIcon.Draw(g, langOpen ? DropUp : DropDown, new RectangleF(r.Right - U(26), r.Top + U(3), U(20), U(20)), Brushes(Dim));
            if (enabled) keys.Add(Tuple.Create(r, id));
            if (menu) langKey = r;
            return r.Left;
        }

        void PaintGroup(Graphics g, RectangleF area)
        {
            var list = Tune.All.Where(p => p.Group == page).ToList();
            var label = BodyFont(U(12), FontStyle.Bold);
            var help = BodyFont(U(9.5f));
            var mono = new Font("Consolas", U(8.5f), GraphicsUnit.Pixel);
            var val = NornsFonts.Screen(U(12));
            var unitF = BodyFont(U(9.5f));
            float x0 = area.Left + U(22), x1 = area.Right - U(22);
            contentH = U(14) + list.Count * U(RowH) + U(10);
            scroll = Math.Max(0, Math.Min(scroll, Math.Max(0, contentH - area.Height)));
            float y = area.Top + U(14) - scroll;
            foreach (var p in list)
            {
                var row = new RectangleF(area.Left, y, area.Width, U(RowH));
                rows.Add(Tuple.Create(row, p));
                if (row.Contains(mouse)) using (var b = new SolidBrush(Color.FromArgb(28, 31, 36))) g.FillRectangle(b, row);
                // label, its key, and the "new events" tag
                g.DrawString(T(p.Label), label, Brushes(BodyTheme.Ink), x0, y + U(8));
                float lx = x0 + g.MeasureString(T(p.Label), label).Width + U(4);
                g.DrawString(p.Key, mono, Brushes(Faint), lx, y + U(11));
                if (p.NewEventsOnly)
                {
                    float kx = lx + g.MeasureString(p.Key, mono).Width + U(6);
                    var tag = T("new events only"); var tf = BodyFont(U(8.5f)); var ts = g.MeasureString(tag, tf);
                    var tr = new RectangleF(kx, y + U(9), ts.Width + U(8), U(15));
                    using (var path = Theme.Round(tr, U(7), 0.85f)) using (var pen = new Pen(Faint, 1)) g.DrawPath(pen, path);
                    g.DrawString(tag, tf, Brushes(Dim), tr.Left + U(4), tr.Top + (tr.Height - tf.GetHeight(g)) / 2);
                }
                // the value and its unit, right-aligned; the default under it when changed
                string vt = Tune.Format(p, p.V), ut = T(p.Unit);
                float uw = ut == "" ? 0 : g.MeasureString(ut, unitF).Width;
                float vw = g.MeasureString(vt, val).Width;
                float vx = x1 - uw - vw - (ut == "" ? 0 : U(2));
                var vr = new RectangleF(vx - U(4), y + U(5), vw + uw + U(8), U(22));
                values.Add(Tuple.Create(vr, p));
                if (editing != p)
                {
                    g.DrawString(vt, val, Brushes(p.Changed ? BodyTheme.Accent : BodyTheme.Ink), vx, y + U(7));
                    if (ut != "") g.DrawString(ut, unitF, Brushes(Dim), x1 - uw, y + U(10));
                }
                // the track: the default as a tick, the value as a knob
                float resetW = U(30);
                var track = RectangleF.FromLTRB(x0, y + U(36), x1 - resetW - U(8), y + U(40));
                var hitTrack = RectangleF.FromLTRB(track.Left - U(6), track.Top - U(8), track.Right + U(6), track.Bottom + U(8));
                tracks.Add(Tuple.Create(hitTrack, p));
                using (var path = Theme.Round(track, U(2), 0.85f)) using (var b = new SolidBrush(Color.FromArgb(46, 49, 56))) g.FillPath(b, path);
                float fx = track.Left + (float)Frac(p, p.V) * track.Width, dx = track.Left + (float)Frac(p, p.Def) * track.Width;
                var fill = RectangleF.FromLTRB(track.Left, track.Top, Math.Max(track.Left + U(2), fx), track.Bottom);
                using (var path = Theme.Round(fill, U(2), 0.85f)) using (var b = new SolidBrush(p.Changed ? BodyTheme.Accent : Color.FromArgb(120, 124, 132))) g.FillPath(b, path);
                using (var pen = new Pen(BodyTheme.Silk, Math.Max(1f, U(1.2f)))) g.DrawLine(pen, dx, track.Top - U(4), dx, track.Bottom + U(4));
                bool hotKnob = drag == p || hitTrack.Contains(mouse);
                float kr = U(hotKnob ? 7 : 6);
                using (var b = new SolidBrush(p.Changed ? BodyTheme.Accent : BodyTheme.Ink)) g.FillEllipse(b, fx - kr, (track.Top + track.Bottom) / 2 - kr, kr * 2, kr * 2);
                using (var pen = new Pen(Color.Black, Math.Max(1f, U(1.5f)))) g.DrawEllipse(pen, fx - kr, (track.Top + track.Bottom) / 2 - kr, kr * 2, kr * 2);
                // min / max under the track ends
                var lim = BodyFont(U(8.5f));
                g.DrawString(Tune.Format(p, p.Min), lim, Brushes(Faint), track.Left, track.Bottom + U(5));
                var mx = Tune.Format(p, p.Max); g.DrawString(mx, lim, Brushes(Faint), track.Right - g.MeasureString(mx, lim).Width, track.Bottom + U(5));
                if (p.Changed)
                {
                    var dt = T("default") + " " + Tune.Format(p, p.Def);
                    g.DrawString(dt, lim, Brushes(Dim), (track.Left + track.Right - g.MeasureString(dt, lim).Width) / 2, track.Bottom + U(5));
                    var rr = new RectangleF(x1 - resetW, track.Top - U(11), resetW, U(26));
                    bool hot = rr.Contains(mouse);
                    using (var path = Theme.Round(rr, U(8), 0.85f))
                    {
                        using (var b = new LinearGradientBrush(rr, hot ? Color.FromArgb(60, 63, 70) : BodyTheme.KeyTop, BodyTheme.KeyBottom, 90f)) g.FillPath(b, path);
                        using (var pen = new Pen(BodyTheme.KeyRim, 1)) g.DrawPath(pen, path);
                    }
                    var rf = BodyFont(U(12)); var rs = g.MeasureString("↺", rf);
                    g.DrawString("↺", rf, Brushes(hot ? BodyTheme.Accent : BodyTheme.Ink), rr.Left + (rr.Width - rs.Width) / 2, rr.Top + (rr.Height - rf.GetHeight(g)) / 2);
                    resets.Add(Tuple.Create(rr, p));
                }
                // what it does
                g.DrawString(T(p.Help), help, Brushes(Dim), new RectangleF(x0, y + U(57), x1 - x0, U(34)), TwoLines);
                using (var pen = new Pen(Line, 1)) g.DrawLine(pen, x0, row.Bottom - 1, x1, row.Bottom - 1);
                y += U(RowH);
            }
            mono.Dispose();
        }

        void PaintInspector(Graphics g, RectangleF area)
        {
            var now = DemoNow ?? DateTime.UtcNow;
            if (report == null) report = body.InspectReport(now);
            var mono = new Font("Consolas", U(10), GraphicsUnit.Pixel);
            var headF = HeadFont(U(9.5f));
            float lh = U(15.5f), x = area.Left + U(20);
            contentH = U(12);
            foreach (var r in report) contentH += r.Head ? U(28) : lh;
            scroll = Math.Max(0, Math.Min(scroll, Math.Max(0, contentH - area.Height)));
            float y = area.Top + U(10) - scroll;
            foreach (var r in report)
            {
                if (r.Head)
                {
                    y += U(8);
                    g.DrawString(r.Text, headF, Brushes(r.Color), x, y);
                    y += U(20);
                    continue;
                }
                if (y > area.Top - lh && y < area.Bottom)
                    g.DrawString(r.Text, mono, Brushes(r.Color), new RectangleF(x, y, area.Right - x - U(12), lh), OneLine);
                y += lh;
            }
            if (frozen)
            {
                var t = T("FROZEN"); var f = HeadFont(U(9));
                g.DrawString(t, f, Brushes(BodyTheme.Dread), area.Right - U(16) - g.MeasureString(t, f).Width, area.Top + U(10));
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
        static readonly StringFormat TwoLines = new StringFormat { Trimming = StringTrimming.EllipsisWord };
        static readonly Dictionary<Color, SolidBrush> brushes = new Dictionary<Color, SolidBrush>();
        static Brush Brushes(Color c) { SolidBrush b; if (!brushes.TryGetValue(c, out b)) brushes[c] = b = new SolidBrush(c); return b; }

        // ---------------- input ----------------
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mouse = e.Location;
            if (drag != null) SetFromTrack(drag, e.X);
            Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); mouse = new Point(-1, -1); Invalidate(); }

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
            if (e.Button != MouseButtons.Left) return;
            CommitEdit();
            if (langOpen)
            {
                var lr = langRows.FirstOrDefault(x => x.Item1.Contains(e.Location));
                if (lr != null) { SetLang(lr.Item2); return; }
                langOpen = false; Invalidate(); return;   // a click outside the names only closes the list
            }
            var k = keys.FirstOrDefault(x => x.Item1.Contains(e.Location));
            if (k != null) { Press(k.Item2); return; }
            var rs = resets.FirstOrDefault(x => x.Item1.Contains(e.Location));
            if (rs != null) { Tune.Set(rs.Item2, rs.Item2.Def); Invalidate(); return; }
            var t = tracks.FirstOrDefault(x => x.Item1.Contains(e.Location));
            if (t != null) { drag = t.Item2; Capture = true; SetFromTrack(drag, e.X); Invalidate(); }
        }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); drag = null; Capture = false; Invalidate(); }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            var v = values.FirstOrDefault(x => x.Item1.Contains(e.Location));
            if (v != null) StartEdit(v.Item2, v.Item1);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (langOpen) { langOpen = false; Invalidate(); return; }
            var row = rows.FirstOrDefault(x => x.Item1.Contains(e.Location));
            if ((Mods & Keys.Control) != 0 && row != null)
            {
                var p = row.Item2; double step = p.Step * ((Mods & Keys.Shift) != 0 ? 10 : 1);
                Tune.Set(p, p.V + Math.Sign(e.Delta) * step);
            }
            else scroll -= e.Delta / 120f * U(48);
            Invalidate();
        }

        void Press(string id)
        {
            if (id == "close") { Hide(); return; }
            if (id == "resetall") { Tune.ResetAll(); Invalidate(); return; }
            if (id == "freeze") { frozen = !frozen; report = null; Invalidate(); return; }
            if (id == "lang") { langOpen = !langOpen; Invalidate(); return; }
            if (id == "openfile")
            {
                if (!System.IO.File.Exists(Tune.FilePath)) Tune.Save();
                try { System.Diagnostics.Process.Start("notepad.exe", "\"" + Tune.FilePath + "\""); } catch { }
                return;
            }
            if (id.StartsWith("page:")) { ShowPage(id.Substring(5)); Invalidate(); }
        }

        internal void StartEdit(Tune.P p, RectangleF r)
        {
            CommitEdit();
            editing = p;
            edit = new TextBox
            {
                Text = Tune.Format(p, p.V).Replace(" ", ""), BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(30, 33, 38), ForeColor = BodyTheme.Accent,
                Font = BodyFont(U(12)), TextAlign = HorizontalAlignment.Right,
                Bounds = new Rectangle((int)(r.Right - U(110)), (int)(r.Top + U(2)), (int)U(106), (int)U(20)),
            };
            edit.KeyDown += (s, e) =>
            {
                // the box is removed after its own key event, not inside it
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BeginInvoke((Action)CommitEdit); }
                else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; BeginInvoke((Action)CancelEdit); }
            };
            edit.LostFocus += (s, e) => CommitEdit();
            Controls.Add(edit);
            edit.Focus(); edit.SelectAll();
            Invalidate();
        }
        internal void CommitEdit()
        {
            if (edit == null) return;
            double v;
            var text = edit.Text.Replace(" ", "").Replace(",", ".");
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) Tune.Set(editing, v);
            CancelEdit();
        }
        void CancelEdit()
        {
            if (edit == null) return;
            var e = edit; edit = null; editing = null;
            Controls.Remove(e); e.Dispose();
            Invalidate();
        }

        // --selftest-dps: the window on two groups (one value changed) and the Inspector over the self-test's fleet. Tuning is
        // switched on only in memory: nothing is read from or written to tuning.json, and every value is put back after.
        public static void SelfTest(DpsForm f, string dir, DateTime now, System.Text.StringBuilder log)
        {
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
                    Lang = keepLang;
                    w.ShowPage("Smartbombs"); w.langOpen = true;
                    using (w.Render()) { }   // the key's place
                    using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_language_menu.png"), System.Drawing.Imaging.ImageFormat.Png);
                    w.langOpen = false;
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
                w.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, (int)(rr.Left + rr.Width / 2), (int)(rr.Top + rr.Height / 2), 0));
                check(!gap.Changed, "reset left " + gap.V);
                using (w.Render()) { }
                var row = w.rows.First(x => x.Item2 == Tune.SbMinGcd).Item1;
                var keepMods = ModsOverride; ModsOverride = Keys.Control;
                w.OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, (int)(row.Left + U(40)), (int)(row.Top + U(10)), 120));
                ModsOverride = keepMods;
                check(Tune.SbMinGcd.I == 4, "Ctrl+wheel gave " + Tune.SbMinGcd.V);
                using (w.Render()) { }
                var vr = w.values.First(x => x.Item2 == gap).Item1;
                w.StartEdit(gap, vr); w.edit.Text = "8,5"; w.CommitEdit();
                check(Math.Abs(gap.V - 8.5) < 1e-9, "typed 8,5 gave " + gap.V);
                // the language list: the key opens it, a name takes that language, a click outside closes it
                using (w.Render()) { }
                Action<PointF> click = pt => { w.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, (int)pt.X, (int)pt.Y, 0)); w.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, (int)pt.X, (int)pt.Y, 0)); using (w.Render()) { } };
                Func<RectangleF, PointF> mid = r => new PointF(r.Left + r.Width / 2, r.Top + r.Height / 2);
                click(mid(w.langKey));
                check(w.langOpen && w.langRows.Count == L.Languages.Length, "the Language key did not open the list");
                click(mid(w.langRows.First(x => x.Item2 == "de").Item1));
                check(Lang == "de" && !w.langOpen && T("Reset all") == "Alles zurücksetzen", "picking Deutsch gave " + Lang);
                click(mid(w.langKey));
                var gapRow = w.tracks.First(x => x.Item2 == gap).Item1;
                double before = gap.V;
                click(new PointF(gapRow.Left + U(4), gapRow.Top + gapRow.Height / 2));   // outside the list, on a track
                check(!w.langOpen && gap.V == before && Lang == "de", "a click outside the list did more than close it");
                Lang = "en";
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
            Lang = keepL;
            log.AppendLine("tuning: drag, reset, Ctrl+wheel, typed value, language list, save / read back, broken file, limits: ok (" + saved.Length + " bytes saved)");
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
