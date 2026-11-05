// Anim.cs - --demo-anim <dir> [scene]: short recordings of the window for the site (owner, 02.11.2026). Every frame is
// drawn as the selftest draws the window, with a made-up fleet (Pilot-1..5) and a clock that moves 1/25 s a frame, so
// the lines, numbers and columns ease as they do live; the mouse is played through the window's own handlers. Nothing
// is read from the PC (no logs, no sessions, no config), nothing is saved, nothing goes to the network.
// <dir>\<scene>\0000.png ... at 25 fps; tools\encode_anim.py makes the videos.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TheNorns
{
    partial class DpsForm
    {
        const int AnimFps = 25;   // the live timer's rate: the easing steps are per frame at 25 fps

        static readonly string[] AnimScenes = { "live", "glass", "spawns", "timer", "competition", "settings", "language", "stats", "formula", "update", "noclient", "compact", "rewarp" };

        public static void Anim(string dir, string only)
        {
            SelfTesting = true;
            L.Init("en");
            UiScale = 1.25f; Compact = false; Potato = false; SpawnSound = false; FleetOn = false; FleetPreview = false;
            Competition = false; UpdateAtStart = false;
            foreach (var s in AnimScenes)
            {
                if (only != null && only != s) continue;
                var sd = Path.Combine(dir, s);
                if (Directory.Exists(sd)) foreach (var old in Directory.GetFiles(sd, "*.png")) File.Delete(old);
                Directory.CreateDirectory(sd);
                L.Init("en");
                var rec = new AnimRec(sd);
                try { Scene(s, rec); }
                finally { rec.Dispose(); FormulaWindow.DemoOpen = null; Compact = false; }
                Console.WriteLine(s + ": " + rec.N + " frames");
            }
        }

        static readonly string[] Five = { "Pilot-1", "Pilot-2", "Pilot-3", "Pilot-4", "Pilot-5" };

        static void Scene(string name, AnimRec r)
        {
            var f = r.F;
            switch (name)
            {
                case "live":
                    // a site: the Vorton lines, the smartbomb cycles, the bounty ticks; a commander spawns at 5 s
                    r.Site(Five, 14, 30);
                    r.Spawn(5.2, "Shadow Serpentis Vice Admiral", 4);
                    r.Wait(10);
                    break;
                case "glass":
                    r.Site(Five, 10, 60);
                    r.Mouse = r.MainAt(f.GlassRect, 0.2f, 0.25f);
                    var g = f.GlassRect;
                    r.Path(7, t => r.MainAt(g, 0.5f + 0.36f * (float)Math.Sin(t * 0.9 + 0.4), 0.45f + 0.32f * (float)Math.Sin(t * 1.7)));
                    break;
                case "spawns":
                    // a pirate dreadnought lands (the hull heats yellow), then a titan (red)
                    r.Site(Five, 13, 80);
                    r.Spawn(1.5, "Shadow Serpentis Dreadnought", 9);
                    r.Spawn(6.5, "Shadow Serpentis Titan", 6);
                    r.Wait(12);
                    break;
                case "timer":
                    // the fleet warps out: the lines sink, the clear time counts up; then the next site's first hit
                    r.Site(Five, 3.2, 70, 3.0);
                    f.stats.Tracker.PrevWarpTime = r.T0.AddSeconds(-402);
                    r.At(3.4, () => { f.stats.Tracker.LastWarpTime = r.Now; });
                    r.Wait(8);
                    r.Fight(Five, r.T, r.T + 4, 5, false);
                    r.Wait(3.5);
                    break;
                case "competition":
                    Competition = true; RunnerResists = new double[] { 87, 60, 87, 50 }; RunnerRepair = 74.7; VortonShot = new double[] { 1801, 1789 };
                    try
                    {
                        var tc = new[] { "Pilot-1", "Pilot-2", "Pilot-3", "Pilot-4" };
                        r.Fight(tc, -70, 10, 3, false);
                        r.Rod(tc, "Pilot-5", -20, 10);
                        r.Ticks(-60, 10, 40);
                        r.Warm(3);
                        r.Wait(9);
                    }
                    finally { Competition = false; }
                    break;
                case "settings":
                    // the name opens the settings; the pages one after another; Statistics and About; back
                    r.Site(Five, 20, 70);
                    f.DemoLogDirOk = true; f.aboutView = "credits";   // About: the credits, not the change notes
                    r.Mouse = r.MainAt(f.GlassRect, 0.55f, 0.5f);
                    r.Wait(0.6);
                    r.MoveTo(r.MainAt(f.NameRect, 0.5f, 0.5f), 0.8); r.Click(); r.Wait(1.2);
                    foreach (var id in new[] { "page:display", "page:modes", "page:data", "page:stats", "page:about" }) { r.MoveToSpot(id, 0.6); r.Click(); r.Wait(1.3); }
                    r.MoveToSpot("page:general", 0.6); r.Click(); r.Wait(1.0);
                    r.MoveToSpot("back", 0.6); r.Click(); r.Wait(1.0);
                    break;
                case "language":
                    // Settings -> General: every language in turn
                    r.Site(Five, 18, 70);
                    f.ToggleSettings();
                    r.Mouse = r.MainAt(f.GlassRect, 0.6f, 0.75f);
                    r.Wait(0.8);
                    foreach (var code in new[] { "de", "fr", "es", "it", "uk", "zh", "en" }) { r.MoveToSpot("lang:" + code, 0.5); r.Click(); r.Wait(1.3); }
                    break;
                case "stats":
                    // the ★ chip opens Statistics; the list scrolls; the calendar: a period, a quarter, a year, all
                    f.DemoSessions = AnimSessions(r.T0);
                    r.Site(Five, 24, 70);
                    r.Spawn(-30, "Shadow Serpentis Vice Admiral", 3);
                    r.CardCanvas = f.CalendarCard();
                    r.Mouse = r.MainAt(f.GlassRect, 0.5f, 0.45f);
                    r.Wait(0.8);
                    r.MoveTo(r.MainAt(f.spawnChip, 0.5f, 0.5f), 0.7); r.Click(); r.Wait(0.9);
                    r.MoveTo(r.MainAt(f.statsView, 0.45f, 0.5f), 0.5);
                    r.Ease(1.6, p => f.statsScroll = (float)p * f.U(160)); r.Wait(0.4);
                    r.Ease(0.9, p => f.statsScroll = (float)(1 - p) * f.U(160)); r.Wait(0.3);
                    r.MoveToSpot("stats:calendar", 0.6); r.Click(); r.Wait(0.8);
                    var today = DateTime.Today;
                    r.MoveToCardSpot("prev", 0.6); r.Click(); r.Wait(0.7);
                    r.MoveToDay(today.AddDays(-24), 0.6); r.Click(); r.Wait(0.5);
                    r.MoveToDay(today.AddDays(-9), 0.6); r.Click(); r.Wait(1.2);
                    foreach (var k in new[] { "quarter", "year", "all" }) { r.MoveToCardSpot(k, 0.5); r.Click(); r.Wait(1.1); }
                    r.MoveToClose(0.6); r.Click(); r.Wait(1.2);
                    break;
                case "formula":
                    // the fx key: the formula card slides in over the body, the glass under the mouse, out again
                    r.Site(Five, 11, 70);
                    r.CardCanvas = FormulaWindow.Vorton;
                    r.Mouse = r.MainAt(f.GlassRect, 0.5f, 0.6f);
                    r.Wait(0.6);
                    r.MoveTo(r.MainAt(f.vortonFx, 0.5f, 0.5f), 0.7); r.Wait(0.3); r.Click(); r.Wait(0.6);
                    r.MoveToCard(0.55f, 0.55f, 0.8); r.MoveToCard(0.3f, 0.75f, 1.6); r.Wait(0.4);
                    r.MoveToClose(0.7); r.Click(); r.Wait(1.0);
                    break;
                case "update":
                    // a newer version found at start: the plate blinks; a click opens About with the download key
                    r.Site(Five, 9, 70);
                    f.DemoUpdate("1.6.0"); f.aboutView = "legal";
                    r.Mouse = r.MainAt(f.GlassRect, 0.5f, 0.55f);
                    r.Wait(2.2);
                    r.MoveTo(r.MainAt(f.updateMark, 0.5f, 0.5f), 0.9); r.Wait(0.5); r.Click(); r.Wait(0.8);
                    r.MoveToSpot("update", 0.7); r.Wait(1.8);
                    break;
                case "compact":
                    // Compact mode: the smaller body keeps the key numbers (no power display, narrow damage display)
                    Compact = true; f.Size = new Size(f.DesiredWidth, f.DesiredHeight);
                    if (f.shell != null) { f.shell.Dispose(); f.shell = null; }
                    r.Site(Five, 16, 40, 16);
                    r.Spawn(3.5, "Shadow Serpentis Vice Admiral", 3);
                    r.Wait(9);
                    break;
                case "rewarp":
                    // a site, the warp out (the clear time), then two false re-warps: a random time rushing down to 00:00 each
                    r.Site(Five, 2.0, 60, 2.0);
                    f.stats.Tracker.PrevWarpTime = r.T0.AddSeconds(-402);
                    r.At(2.3, () => { f.stats.Tracker.LastWarpTime = r.Now; });
                    r.At(6.0, () => { var tr = f.stats.Tracker; tr.PrevWarpTime = tr.LastWarpTime; tr.LastWarpTime = r.Now; });
                    r.At(9.5, () => { var tr = f.stats.Tracker; tr.PrevWarpTime = tr.LastWarpTime; tr.LastWarpTime = r.Now; });
                    r.Wait(12.5);
                    break;
                case "noclient":
                    // no EVE client: the lamp and the power display fail now and then
                    f.DemoNoClients = true; f.DemoFollowing = false;
                    r.Wait(8);
                    break;
            }
        }

        // the earlier sessions of the statistics clip: two months of evenings, some with a commander, a titan, a dreadnought
        static List<SessionInfo> AnimSessions(DateTime now)
        {
            var list = new List<SessionInfo>();
            var rnd = new Random(5);
            var today = DateTime.Today;
            for (int d = 1; d < 75; d++)
            {
                if (rnd.NextDouble() < 0.45) continue;
                var start = today.AddDays(-d).AddHours(18 + rnd.Next(4)).AddMinutes(rnd.Next(60));
                int sites = 3 + rnd.Next(12);
                list.Add(new SessionInfo { Start = start, End = start.AddMinutes(25 * sites + rnd.Next(40)), Sites = sites, Isk = (long)(sites * (95e6 + rnd.Next(40) * 1e6)), Spawns = rnd.NextDouble() < 0.3 ? 1 + rnd.Next(2) : 0, Titans = rnd.NextDouble() < 0.06 ? 1 : 0, Dreads = rnd.NextDouble() < 0.1 ? 1 : 0 });
            }
            return list;
        }

        // one recording: the window, its clock, the fleet's log lines and bounty ticks fed as the clock passes them, the
        // mouse, and the card over the window
        class AnimRec : IDisposable
        {
            public readonly DpsForm F;
            readonly string dir;
            public int N;
            public double T;                       // seconds since the start
            public readonly DateTime T0;           // the clip's "now" at T = 0: today 20:00
            public DateTime Now { get { return T0.AddSeconds(T); } }
            readonly List<DmgEvent> pending = new List<DmgEvent>();
            readonly List<Tuple<DateTime, long>> ticks = new List<Tuple<DateTime, long>>();
            readonly List<Tuple<double, Action>> cues = new List<Tuple<double, Action>>();
            public PointF? Mouse;                  // in the picture's px
            bool down;
            public FormulaText CardCanvas;         // set: every frame on the card's larger picture (the card closed: p = 0)
            FormulaText card; double cardP; int cardDir;
            Point cardAt;
            readonly Random rnd = new Random(7);

            public AnimRec(string dir)
            {
                this.dir = dir;
                T0 = DateTime.Today.AddHours(20).ToUniversalTime();
                F = new DpsForm(Five, () => null, true) { DemoNow = T0, DemoLive = true, DemoFollowing = true, DemoClock = 0 };
                F.Stats.DetectedFaction = "Serpentis";
                FormulaWindow.DemoOpen = t => { card = t; cardP = 0; cardDir = 1; };
            }
            public void Dispose() { F.Dispose(); }

            int Inset { get { return CardCanvas != null ? FormulaWindow.DemoInset : 0; } }
            public PointF MainAt(RectangleF r, float fx, float fy) { return new PointF(Inset + r.Left + r.Width * fx, Inset + r.Top + r.Height * fy); }

            // ---- the fleet ----
            // a site from `from` s (before the start: the graph is full at once) for `secs`; `ticks0` bounty ticks before the
            // start, more every ~4 s; the hits stop at `stop` s when given
            public void Site(string[] pls, double secs, int ticks0, double stop = double.NaN)
            {
                F.Stats.Tracker.AnomalyCount = 3; F.Stats.Tracker.TotalSessionIsk = 612450000;
                double end = double.IsNaN(stop) ? secs : stop;
                Fight(pls, -70, end, 1, true);
                Ticks(-4 * ticks0, end, ticks0);
                Warm(3);
            }

            public void Fight(string[] pls, double from, double to, int seed, bool smartbombs)
            {
                var q = new[] { "Hits", "Hits", "Smashes", "Penetrates", "Glances Off" };
                var npcs = new[] { "Core Grand Admiral", "Corelatis High Captain", "Corelatis Captain Sentry", "Core Lord Admiral" };
                var rn = new Random(seed);
                for (double t = Math.Ceiling(from); t < to; t += 1)
                    for (int i = 0; i < pls.Length; i++)
                    {
                        if (rn.NextDouble() > 0.82) continue;
                        double wave = 0.72 + 0.28 * Math.Sin(t * 2 * Math.PI / (14 + 3 * i) + i * 1.3);
                        var kq = q[rn.Next(q.Length)];
                        pending.Add(new DmgEvent { T = T0.AddSeconds(t + rn.NextDouble() * 0.9), Pilot = pls[i], Out = true, Quality = kq, Other = npcs[rn.Next(npcs.Length)], Weapon = "Large Vorton Projector II", Amount = (2300 + 700 * (i % 3)) * wave * (0.75 + rn.NextDouble() * 0.5) });
                    }
                if (smartbombs)
                {
                    // the anchor's 7 smartbombs every 9 s on the NPCs in range
                    string anchor = pls[pls.Length - 1];
                    for (double t = Math.Ceiling(from / 9) * 9; t < to; t += 9)
                    {
                        int n = 3 + rn.Next(4);
                        for (int m = 0; m < 7; m++)
                            for (int k = 0; k < n; k++)
                                pending.Add(new DmgEvent { T = T0.AddSeconds(t + m * 0.05), Pilot = anchor, Out = true, Amount = k % 2 == 0 ? 263 : 195, Other = npcs[k % npcs.Length], Quality = "Hits", Weapon = "Caldari Navy Large Graviton Smartbomb" });
                    }
                }
                pending.Sort((a, b) => a.T.CompareTo(b.T));
            }

            // Competition: the Thunderchilds' Vortons on the runner about every 8 s each, the NPCs on the runner now and then
            public void Rod(string[] tc, string runner, double from, double to)
            {
                var rn = new Random(11);
                for (int i = 0; i < tc.Length; i++)
                    for (double t = from + i * 2.1; t < to; t += 7.6 + rn.NextDouble() * 0.8)
                        pending.Add(new DmgEvent { T = T0.AddSeconds(t), Pilot = tc[i], Out = true, Quality = "Hits", Other = runner + "[DEMO](Jaguar)", Weapon = "Large Vorton Projector II", Amount = rn.NextDouble() < 0.75 ? 65 : 61 });
                for (double t = from; t < to; t += 2.2 + rn.NextDouble() * 1.5)
                    pending.Add(new DmgEvent { T = T0.AddSeconds(t), Pilot = runner, Out = false, Quality = "Hits", Other = "Core Grand Admiral", Amount = 20 + rn.Next(25) });
                pending.Sort((a, b) => a.T.CompareTo(b.T));
            }

            static readonly long[] TickValues = { 27000, 27750, 29250, 240000, 260000, 5175, 5400, 5625, 260000, 4050, 4275, 42750, 45000, 47250, 260000, 27750, 30750, 240000 };
            public void Ticks(double from, double to, int count)
            {
                int i = 0;
                for (double t = from; t < to; t += 3.2 + rnd.NextDouble() * 1.6, i++)
                    ticks.Add(Tuple.Create(T0.AddSeconds(t), TickValues[i % TickValues.Length]));
            }

            // a rare NPC on the grid at `at` s: a few hits on it, then its bounty
            public void Spawn(double at, string npc, int hits)
            {
                for (int k = 0; k < hits; k++)
                    pending.Add(new DmgEvent { T = T0.AddSeconds(at + k * 0.8), Pilot = Five[k % 5], Out = true, Quality = "Hits", Other = npc, Weapon = "Large Vorton Projector II", Amount = 2800 });
                pending.Sort((a, b) => a.T.CompareTo(b.T));
            }

            public void At(double at, Action a) { cues.Add(Tuple.Create(at, a)); }

            // frames before the clip, not saved: the eased lines, columns and grid settle first
            public void Warm(double secs) { T = -secs; while (T < -0.5 / AnimFps) Frame(); T = 0; }

            // ---- time ----
            public void Wait(double secs) { int n = (int)Math.Round(secs * AnimFps); for (int i = 0; i < n; i++) Frame(); }
            public void Ease(double secs, Action<double> step)
            {
                int n = Math.Max(1, (int)Math.Round(secs * AnimFps));
                for (int i = 1; i <= n; i++) { double p = (double)i / n; step(p < 0.5 ? 4 * p * p * p : 1 - Math.Pow(-2 * p + 2, 3) / 2); Frame(); }
            }
            public void Path(double secs, Func<double, PointF> at)
            {
                int n = (int)Math.Round(secs * AnimFps);
                for (int i = 0; i < n; i++) { Mouse = at((double)i / AnimFps); Frame(); }
            }
            public void MoveTo(PointF to, double secs)
            {
                var from = Mouse ?? to;
                Ease(secs, p => Mouse = new PointF(from.X + (to.X - from.X) * (float)p, from.Y + (to.Y - from.Y) * (float)p));
            }
            public void MoveToSpot(string id, double secs)
            {
                var s = F.spots.FirstOrDefault(x => x.Item1 == id);
                if (s == null) throw new Exception("no spot " + id + " on page " + F.page);
                MoveTo(MainAt(s.Item2, 0.5f, 0.5f), secs);
            }
            SessionCalendar Cal { get { return card == null ? null : card.Content as SessionCalendar; } }
            public void MoveToCardSpot(string id, double secs)
            {
                var r = Cal == null ? null : Cal.Spot(id);
                if (r == null) throw new Exception("no card spot " + id);
                MoveTo(new PointF(cardAt.X + r.Value.Left + r.Value.Width / 2, cardAt.Y + r.Value.Top + r.Value.Height / 2), secs);
            }
            public void MoveToDay(DateTime d, double secs) { MoveToCardSpot("day:" + d.ToString("yyyyMMdd", CultureInfo.InvariantCulture), secs); }
            public void MoveToCard(float fx, float fy, double secs)
            {
                var cs = FormulaWindow.DemoCardSize(F.Size, card);
                MoveTo(new PointF(cardAt.X + cs.Width * fx, cardAt.Y + cs.Height * fy), secs);
            }
            public void MoveToClose(double secs)
            {
                var c = FormulaWindow.DemoCloseRect(F.Size, card);
                MoveTo(new PointF(cardAt.X + c.Left + c.Width / 2, cardAt.Y + c.Top + c.Height / 2), secs);
            }

            // a click where the mouse is: on the card (its content, its close key) or on the window
            public void Click()
            {
                var cp = CardPoint();
                if (cp != null)
                {
                    down = true; Wait(0.12); down = false;
                    if (FormulaWindow.DemoCloseRect(F.Size, card).Contains(cp.Value)) cardDir = -1;
                    else if (card.Content != null) card.Content.Click(cp.Value);
                    Frame();
                    return;
                }
                var mp = MainPoint();
                if (mp == null) return;
                var at = Point.Round(mp.Value);
                F.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, at.X, at.Y, 0));
                down = true; Wait(0.12); down = false;
                F.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, at.X, at.Y, 0));
                Frame();
            }

            PointF? CardPoint()
            {
                if (Mouse == null || card == null || cardP < 0.9) return null;
                var cs = FormulaWindow.DemoCardSize(F.Size, card);
                var p = new PointF(Mouse.Value.X - cardAt.X, Mouse.Value.Y - cardAt.Y);
                return p.X >= 0 && p.Y >= 0 && p.X < cs.Width && p.Y < cs.Height ? (PointF?)p : null;
            }
            PointF? MainPoint()
            {
                if (Mouse == null) return null;
                var p = new PointF(Mouse.Value.X - Inset, Mouse.Value.Y - Inset);
                return p.X >= 0 && p.Y >= 0 && p.X < F.Width && p.Y < F.Height ? (PointF?)p : null;
            }

            // ---- one frame ----
            void Frame()
            {
                var now = Now;
                F.DemoClock = T; F.DemoNow = now;
                foreach (var c in cues.Where(c => c.Item1 <= T).ToList()) { c.Item2(); cues.Remove(c); }
                int k = 0;
                while (k < pending.Count && pending[k].T <= now) k++;
                if (k > 0) { F.AddDemo(pending.GetRange(0, k)); pending.RemoveRange(0, k); }
                var tr = F.Stats.Tracker;
                while (ticks.Count > 0 && ticks[0].Item1 <= now)
                {
                    tr.Bounties.Add(ticks[0].Item2); tr.BountyTimes.Add(ticks[0].Item1);
                    tr.CurrentAnomalyIsk += ticks[0].Item2; tr.TotalSessionIsk += ticks[0].Item2;
                    ticks.RemoveAt(0);
                }
                // the card: in 0.42 s, out in 0.22 s (FormulaWindow)
                if (card != null)
                {
                    cardP += cardDir > 0 ? 1.0 / AnimFps / 0.42 : -1.0 / AnimFps / 0.22;
                    if (cardP >= 1) cardP = 1;
                    if (cardP <= 0) { card = null; cardP = 0; }
                }
                // the mouse: over the card, over the window's glass, its keys
                var cp = CardPoint();
                var mp = cp == null ? MainPoint() : null;
                if (mp != null) { var at = Point.Round(mp.Value); F.OnMouseMove(new MouseEventArgs(down ? MouseButtons.Left : MouseButtons.None, 0, at.X, at.Y, 0)); }
                else F.OnMouseLeave(EventArgs.Empty);
                F.DemoMouse = mp != null && F.GlassRect.Contains(mp.Value) ? (Point?)Point.Round(mp.Value) : null;
                bool pulse = mp != null && F.OnPulse(Point.Round(mp.Value));
                if (cp != null) pulse = FormulaWindow.DemoCloseRect(F.Size, card).Contains(cp.Value) || Cal != null && Cal.Keys.Any(r => r.Contains(cp.Value)) || Cal != null && Cal.OnDay(cp.Value);

                using (var main = F.Draw())
                {
                    Bitmap pic;
                    if (CardCanvas != null) pic = FormulaWindow.Demo(main, card ?? CardCanvas, card == null ? 0 : cardP, 0, cp, cp, Color.Transparent, out cardAt);
                    else pic = new Bitmap(main);
                    using (pic)
                    {
                        if (Mouse != null)
                            using (var g = Graphics.FromImage(pic))
                            using (var arrow = Glass.PulseFrame(pulse ? T % Glass.PulseLoop / Glass.PulseLoop : 0, BodyTheme.Pointer, BodyTheme.PointerOutline, Theme.K))
                            {
                                float tip = 10 * Theme.K;
                                g.DrawImageUnscaled(arrow, (int)Math.Round(Mouse.Value.X - tip), (int)Math.Round(Mouse.Value.Y - tip));
                            }
                        if (T > -0.5 / AnimFps) { pic.Save(System.IO.Path.Combine(dir, N.ToString("0000") + ".png"), ImageFormat.Png); N++; }
                    }
                }
                T += 1.0 / AnimFps;
            }
        }
    }
}
