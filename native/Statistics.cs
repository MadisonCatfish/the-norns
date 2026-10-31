// Statistics.cs - the session statistics (owner 27.10.2026): every run of the app is a session; each site done in it
// (its first bounty to the warp out) and its ISK are kept in %LOCALAPPDATA%\The Norns\sessions\<start>.json, written
// after every site and on close. Only this PC; nothing leaves it. The Statistics page lists this session and the earlier ones.
// The folder is the user's own, not the app's: an update or a reinstall keeps it, and every version reads the files of the
// ones before (a field a file does not have counts as 0); only the uninstaller's "remove my data" deletes it.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace TheNorns
{
    class SessionInfo
    {
        public DateTime Start, End;
        public int Sites;
        public long Isk;
        public int Spawns;
        public int Titans, Dreads;   // from 1.5.0 (owner, 31.10.2026); 0 in the files before
        public List<long[]> SiteList = new List<long[]>();   // start ticks, end ticks, ISK
    }

    partial class DpsForm
    {
        readonly SessionInfo session = new SessionInfo { Start = DateTime.Now, End = DateTime.Now };
        List<SessionInfo> sessions;   // the earlier ones, newest first (read when the page opens)
        static string SessionDir { get { return Path.Combine(AppInfo.DataDir, "sessions"); } }
        string SessionFile { get { return Path.Combine(SessionDir, session.Start.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".json"); } }

        void StartSessionStats()
        {
            stats.Tracker.SiteClosed += (start, end, isk) =>
            {
                session.Sites++; session.Isk += isk; session.End = DateTime.Now;
                session.SiteList.Add(new[] { start.Ticks, end.Ticks, isk });
                SaveSession();
            };
            FormClosed += (s, e) => SaveSession();
        }

        SessionInfo CurrentSession()
        {
            // the site under way counts too
            var tr = stats.Tracker;
            return new SessionInfo { Start = session.Start, End = DateTime.Now, Sites = session.Sites + (tr.CurrentAnomalyIsk > 0 ? 1 : 0), Isk = session.Isk + tr.CurrentAnomalyIsk, Spawns = stats.Spawns.Count, Titans = Titans, Dreads = Dreads };
        }
        // the titans and pirate dreadnoughts of this session: each spawn once (a name seen again within 10 minutes is the same)
        int Titans { get { return stats.Sightings.Count(x => x.Kind == 2); } }
        int Dreads { get { return stats.Sightings.Count(x => x.Kind == 3); } }

        // the file's layout; a later version adds fields, never renames or drops one
        const int SessionFormat = 2;

        void SaveSession()
        {
            if (SelfTesting || !Standalone || session.Sites == 0) return;
            try
            {
                Directory.CreateDirectory(SessionDir);
                var o = new Dictionary<string, object>
                {
                    { "start", session.Start.ToString("o", CultureInfo.InvariantCulture) },
                    { "end", DateTime.Now.ToString("o", CultureInfo.InvariantCulture) },
                    { "format", SessionFormat },
                    { "sites", session.Sites }, { "isk", session.Isk }, { "spawns", stats.Spawns.Count }, { "titans", Titans }, { "dreads", Dreads },
                    { "site_list", session.SiteList.Select(x => new Dictionary<string, object> { { "start", new DateTime(x[0]).ToString("o", CultureInfo.InvariantCulture) }, { "end", new DateTime(x[1]).ToString("o", CultureInfo.InvariantCulture) }, { "isk", x[2] } }).ToList() },
                };
                File.WriteAllText(SessionFile, new JavaScriptSerializer().Serialize(o));
            }
            catch { }
        }

        public List<SessionInfo> DemoSessions;   // selftest: earlier sessions shown instead of the PC's own

        List<SessionInfo> ReadSessions()
        {
            if (DemoSessions != null) return DemoSessions;
            var res = new List<SessionInfo>();
            if (SelfTesting) return res;
            try
            {
                if (!Directory.Exists(SessionDir)) return res;
                var js = new JavaScriptSerializer();
                foreach (var f in Directory.GetFiles(SessionDir, "*.json").OrderByDescending(x => x))
                {
                    if (string.Equals(f, SessionFile, StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        var o = js.DeserializeObject(File.ReadAllText(f)) as Dictionary<string, object>;
                        if (o == null) continue;
                        res.Add(new SessionInfo
                        {
                            Start = DateTime.Parse((string)o["start"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                            End = DateTime.Parse((string)o["end"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                            Sites = Convert.ToInt32(o["sites"], CultureInfo.InvariantCulture),
                            Isk = Convert.ToInt64(o["isk"], CultureInfo.InvariantCulture),
                            Spawns = o.ContainsKey("spawns") ? Convert.ToInt32(o["spawns"], CultureInfo.InvariantCulture) : 0,   // before 1.2: none kept
                            Titans = o.ContainsKey("titans") ? Convert.ToInt32(o["titans"], CultureInfo.InvariantCulture) : 0,   // before 1.5: none kept
                            Dreads = o.ContainsKey("dreads") ? Convert.ToInt32(o["dreads"], CultureInfo.InvariantCulture) : 0,
                        });
                    }
                    catch { }
                }
            }
            catch { }
            return res;
        }

        // ---- the Statistics page (owner, 31.10.2026): the sessions in a period picked on the calendar card, or all of them;
        // the list scrolls with the wheel; the rare spawns per session: ★ faction NPCs and commanders, then titans and pirate
        // dreadnoughts, each with the red skull (Material Symbols skull) ----
        DateTime? statFrom, statTo;   // the period, whole days; null: all sessions
        float statsScroll;
        RectangleF statsView;
        public DateTime? DemoStatFrom, DemoStatTo;   // selftest

        bool InPeriod(SessionInfo x) { return statFrom == null || x.Start.Date >= statFrom.Value && x.Start.Date <= statTo.Value; }

        string PeriodText()
        {
            if (statFrom == null) return L.T("All sessions");
            // short, to leave the totals room: the year only when it is not this one
            Func<DateTime, string> d = x => x.ToString(x.Year == DateTime.Today.Year ? "dd.MM" : "dd.MM.yy", CultureInfo.InvariantCulture);
            string a = d(statFrom.Value), b = d(statTo.Value);
            return a == b ? a : a + " – " + b;
        }

        void PaintStats(Graphics g, RectangleF c, float y, Action<float> line)
        {
            if (DemoStatFrom != null) { statFrom = DemoStatFrom; statTo = DemoStatTo; }
            if (sessions == null) sessions = ReadSessions();
            var cur = CurrentSession();
            var all = new List<SessionInfo> { cur };
            all.AddRange(sessions);
            all = all.Where(InPeriod).ToList();

            // the period on the left, the calendar key on the right (owner, 31.10.2026)
            var ck = new RectangleF(c.Right - U(15), y - U(3.5f), U(15), U(15));
            SquareKey(g, ck, MaterialIcon.Calendar, false, "stats:calendar", statFrom != null);
            var tt = PeriodText();
            Glow(g, tt, TitleFont(tt, U(7.2f)), statFrom != null ? BodyTheme.Accent : Muted, c.Left, y);
            y += U(14);
            line(y); y += U(5);

            // the totals at the bottom, on the right: ★, titans, dreadnoughts, sites and ISK
            var vf = NornsFonts.Screen(U(6.5f));
            float fy = c.Bottom - U(9);
            line(fy - U(5));
            var tv = all.Sum(x => x.Sites) + " " + L.T("anom") + "  ·  " + FormatIsk(all.Sum(x => x.Isk));
            float tvx = c.Right - g.MeasureString(tv.ToUpperInvariant(), vf, 1000, StringFormat.GenericTypographic).Width;
            using (var b = new SolidBrush(BodyTheme.Accent)) g.DrawString(tv.ToUpperInvariant(), vf, b, tvx, fy, StringFormat.GenericTypographic);
            int titans = all.Sum(x => x.Titans), dreads = all.Sum(x => x.Dreads);
            float dx = IconCount(g, dreads, MaterialIcon.Skull, true, vf, dreads > 0 ? AlarmRed : Color.FromArgb(90, AlarmRed), tvx - U(8), fy);
            spots.Add(Tuple.Create("stats:dreads", new RectangleF(dx, fy - U(2), tvx - U(8) - dx, U(11))));
            float tx = IconCount(g, titans, MaterialIcon.Skull, true, vf, titans > 0 ? AlarmRed : Color.FromArgb(90, AlarmRed), dx - U(6), fy);
            spots.Add(Tuple.Create("stats:titans", new RectangleF(tx, fy - U(2), dx - U(6) - tx, U(11))));
            IconCount(g, all.Sum(x => x.Spawns), MaterialIcon.Star, false, vf, BodyTheme.Accent, tx - U(6), fy);

            // the rows between, clipped to the view and scrolled; a thin bar on the right when they do not fit
            statsView = RectangleF.FromLTRB(c.Left, y, c.Right, fy - U(9));
            float rowH = U(15), max = Math.Max(0, all.Count * rowH - statsView.Height);
            statsScroll = Math.Max(0, Math.Min(max, statsScroll));
            float right = max > 0 ? c.Right - U(8) : c.Right;
            var st = g.Save();
            g.SetClip(statsView);
            float ry = y - statsScroll;
            foreach (var x in all)
            {
                if (ry + rowH < statsView.Top || ry > statsView.Bottom) { ry += rowH; continue; }
                bool now = ReferenceEquals(x, cur);
                var when = now ? L.T("This session") : x.Start.ToString("dd.MM HH:mm", CultureInfo.InvariantCulture);
                var dur = x.End - x.Start;
                var mid = (int)dur.TotalHours + ":" + dur.Minutes.ToString("00") + "  ·  " + x.Sites + " " + L.T("anom");
                using (var b = new SolidBrush(now ? BodyTheme.Ink : Color.FromArgb(190, 193, 200)))
                {
                    g.DrawString(when, TextFont(when, U(7)), b, c.Left, ry, StringFormat.GenericTypographic);
                    g.DrawString(mid, TextFont(mid, U(7)), b, c.Left + c.Width * 0.25f, ry, StringFormat.GenericTypographic);
                }
                var iv = FormatIsk(x.Isk).ToUpperInvariant();
                float ivx = right - g.MeasureString(iv, vf, 1000, StringFormat.GenericTypographic).Width;
                using (var b = new SolidBrush(now ? BodyTheme.Accent : BodyTheme.Ink)) g.DrawString(iv, vf, b, ivx, ry + U(0.5f), StringFormat.GenericTypographic);
                var dim = Color.FromArgb(90, BodyTheme.Ink);
                IconCount(g, x.Dreads, MaterialIcon.Skull, true, vf, x.Dreads > 0 ? AlarmRed : dim, right - U(58), ry + U(0.5f), 2);
                IconCount(g, x.Titans, MaterialIcon.Skull, true, vf, x.Titans > 0 ? AlarmRed : dim, right - U(76), ry + U(0.5f), 2);
                IconCount(g, x.Spawns, MaterialIcon.Star, false, vf, x.Spawns > 0 ? BodyTheme.Ink : dim, right - U(94), ry + U(0.5f), 2);
                ry += rowH;
            }
            g.Restore(st);
            if (max > 0)
            {
                float trackH = statsView.Height, barH = Math.Max(U(12), trackH * trackH / (trackH + max)), barY = statsView.Top + (trackH - barH) * statsScroll / max;
                using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillRectangle(b, c.Right - U(2), statsView.Top, U(1.5f), trackH);
                using (var p = Theme.Round(new RectangleF(c.Right - U(2.5f), barY, U(2.5f), barH), U(1.25f), 1f))
                using (var b = new SolidBrush(Color.FromArgb(150, BodyTheme.Ink))) g.FillPath(b, p);
            }
            string empty = statFrom != null && all.Count == 0 ? L.T("No sessions in this period.")
                : statFrom == null && all.Count == 1 && all[0].Sites == 0 ? L.T("No sites done yet. Every session is kept here from now on.") : null;
            if (empty != null)
                using (var b = new SolidBrush(Color.FromArgb(110, BodyTheme.Ink))) g.DrawString(empty, TextFont(empty, U(7.2f)), b, new RectangleF(c.Left, y + (statFrom == null ? rowH : 0), c.Width, U(30)));
        }

        // the wheel over the list (Settings.cs OnMouseWheel); false = not over it
        bool ScrollStats(Point p, int delta)
        {
            if (!settingsOpen || page != "stats" || !statsView.Contains(p)) return false;
            statsScroll = Math.Max(0, statsScroll - Math.Sign(delta) * U(24));   // the next frame clamps it
            return true;
        }

        // an icon and a count after it, ending at `right`; returns where it starts. digits > 0: the icon stays where that many
        // digits put it, the count left-aligned after it, so a column's icons line up whatever the count (owner, 31.10.2026)
        float IconCount(Graphics g, int n, string icon, bool symbol, Font f, Color c, float right, float y, int digits = 0)
        {
            float h = f.GetHeight(g), ic = U(7.5f);
            var t = n.ToString(CultureInfo.InvariantCulture);
            float x = right - ic - U(2) - g.MeasureString(digits > 0 ? new string('0', digits) : t, f, 1000, StringFormat.GenericTypographic).Width;
            using (var b = new SolidBrush(c))
            {
                var ir = new RectangleF(x, y + (h - ic) / 2 - U(0.5f), ic, ic);
                if (symbol) MaterialIcon.DrawSymbol(g, icon, ir, b); else MaterialIcon.Draw(g, icon, ir, b);
                g.DrawString(t, f, b, x + ic + U(2), y, StringFormat.GenericTypographic);
            }
            return x;
        }

        // the calendar card: the days with a session have an accent dot; a day, or two days for a period, filter the list
        FormulaText CalendarCard()
        {
            if (sessions == null) sessions = ReadSessions();
            var cur = CurrentSession();
            var days = new HashSet<DateTime>(sessions.Select(x => x.Start.Date));
            if (cur.Sites > 0) days.Add(cur.Start.Date);
            var cal = new SessionCalendar { Days = days, From = statFrom, To = statTo, Month = new DateTime((statTo ?? DateTime.Today).Year, (statTo ?? DateTime.Today).Month, 1) };
            cal.Changed = () => { statFrom = cal.From; statTo = cal.To; statsScroll = 0; Render(); };
            return new FormulaText { Title = "Calendar", Icon = MaterialIcon.Calendar, Content = cal };
        }
        void OpenCalendar() { FormulaWindow.Open(this, CalendarCard()); }
    }


    // the month on the calendar card (Formula.cs draws the card): the month's name between two arrow keys, the weekdays from
    // Monday, six weeks of days; a dot in the accent under a day with a session; the period as an accent band, its ends lit.
    // The first click picks a day, the second the other end of the period; "All sessions" clears it
    class SessionCalendar : ICardContent
    {
        public DateTime Month;
        public DateTime? From, To;
        public HashSet<DateTime> Days = new HashSet<DateTime>();
        public Action Changed;
        bool picking;
        readonly List<Tuple<string, RectangleF>> spots = new List<Tuple<string, RectangleF>>();
        static float U(float v) { return v * Theme.K * DpsForm.UiScale; }

        static CultureInfo Culture()
        {
            try { return new CultureInfo(L.Code == "en" ? "en-GB" : L.Code == "uk" ? "uk-UA" : L.Code == "zh" ? "zh-CN" : L.Code); }
            catch { return CultureInfo.InvariantCulture; }
        }

        public void Paint(Graphics g, RectangleF a, PointF? hover)
        {
            spots.Clear();
            var ci = Culture();
            Func<string, bool> hot = id => hover != null && spots.Any(s => s.Item1 == id && s.Item2.Contains(hover.Value));
            Func<string, RectangleF, bool> spot = (id, r) => { spots.Add(Tuple.Create(id, r)); return hover != null && r.Contains(hover.Value); };

            // the month and its arrows
            float y = a.Top, kh = U(16);
            for (int k = 0; k < 2; k++)
            {
                var kr = new RectangleF(k == 0 ? a.Left : a.Right - U(18), y, U(18), kh);
                bool h = spot(k == 0 ? "prev" : "next", kr);
                using (var p = Theme.Round(kr, U(3), 0.6f))
                {
                    using (var b = new SolidBrush(Color.FromArgb(h ? 26 : 12, BodyTheme.Ink))) g.FillPath(b, p);
                    using (var pen = new Pen(Color.FromArgb(50, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
                }
                float ic = U(11);
                using (var b = new SolidBrush(BodyTheme.Ink)) MaterialIcon.Draw(g, k == 0 ? MaterialIcon.ArrowLeft : MaterialIcon.ArrowRight, new RectangleF(kr.Left + (kr.Width - ic) / 2, kr.Top + (kr.Height - ic) / 2, ic, ic), b);
            }
            var mt = (ci.DateTimeFormat.GetMonthName(Month.Month) + " " + Month.Year).ToUpper(ci);
            var mf = DpsForm.TitleFont(mt, U(8));
            var ms = g.MeasureString(mt, mf, 1000, StringFormat.GenericTypographic);
            using (var b = new SolidBrush(BodyTheme.Ink)) g.DrawString(mt, mf, b, a.Left + (a.Width - ms.Width) / 2, y + (kh - mf.GetHeight(g)) / 2, StringFormat.GenericTypographic);
            y += kh + U(9);

            // the weekdays
            float cw = a.Width / 7;
            for (int d = 0; d < 7; d++)
            {
                var dn = ci.DateTimeFormat.ShortestDayNames[(d + 1) % 7];
                var df = DpsForm.TextFont(dn, U(6.6f));
                float w = g.MeasureString(dn, df, 1000, StringFormat.GenericTypographic).Width;
                using (var b = new SolidBrush(Color.FromArgb(d >= 5 ? 110 : 150, BodyTheme.Ink))) g.DrawString(dn, df, b, a.Left + d * cw + (cw - w) / 2, y, StringFormat.GenericTypographic);
            }
            y += U(12);

            // six weeks of days
            float keyH = U(19), noteH = U(14);
            float ch = Math.Min(U(26), (a.Bottom - keyH - noteH - U(8) - y) / 6);
            var first = Month.AddDays(-(((int)Month.DayOfWeek + 6) % 7));
            DateTime? lo = From, hi = picking ? From : To;
            var nf = NornsFonts.Screen(U(7));
            for (int i = 0; i < 42; i++)
            {
                var day = first.AddDays(i);
                var cell = new RectangleF(a.Left + (i % 7) * cw, y + (i / 7) * ch, cw, ch);
                bool inMonth = day.Month == Month.Month, h = spot("day:" + day.ToString("yyyyMMdd", CultureInfo.InvariantCulture), cell);
                bool inside = lo != null && day >= lo.Value && day <= hi.Value, end = lo != null && (day == lo.Value || day == hi.Value);
                var pill = RectangleF.Inflate(cell, -U(1.5f), -U(1.5f));
                if (inside && !end)
                    using (var b = new SolidBrush(Color.FromArgb(40, BodyTheme.Accent))) g.FillRectangle(b, cell.Left, pill.Top, cell.Width, pill.Height);
                if (end)
                {
                    using (var p = Theme.Round(pill, U(3), 0.6f)) using (var b = new SolidBrush(BodyTheme.Accent)) g.FillPath(b, p);
                }
                else if (h)
                    using (var p = Theme.Round(pill, U(3), 0.6f)) using (var b = new SolidBrush(Color.FromArgb(30, BodyTheme.Ink))) g.FillPath(b, p);
                if (day == DateTime.Today && !end)
                    using (var p = Theme.Round(pill, U(3), 0.6f)) using (var pen = new Pen(Color.FromArgb(150, BodyTheme.Accent), 1f)) g.DrawPath(pen, p);
                var dt = day.Day.ToString(CultureInfo.InvariantCulture);
                var ds = g.MeasureString(dt, nf, 1000, StringFormat.GenericTypographic);
                var ink = end ? Color.Black : inMonth ? BodyTheme.Ink : Color.FromArgb(70, BodyTheme.Ink);
                using (var b = new SolidBrush(ink)) g.DrawString(dt, nf, b, cell.Left + (cell.Width - ds.Width) / 2, cell.Top + cell.Height / 2 - ds.Height / 2 - U(2), StringFormat.GenericTypographic);
                if (Days.Contains(day))
                {
                    float r = U(1.5f), cy = cell.Top + cell.Height / 2 + U(5.5f);
                    using (var b = new SolidBrush(end ? Color.Black : inMonth ? BodyTheme.Accent : Color.FromArgb(110, BodyTheme.Accent))) g.FillEllipse(b, cell.Left + cell.Width / 2 - r, cy - r, 2 * r, 2 * r);
                }
            }
            y += 6 * ch + U(4);

            // what is shown, and the All sessions key
            var note = picking ? L.T("Now the other end of the period.") : L.T("Click a day, or two days for a period.");
            using (var b = new SolidBrush(Color.FromArgb(130, BodyTheme.Ink))) g.DrawString(note, DpsForm.TextFont(note, U(7.2f)), b, a.Left, y, StringFormat.GenericTypographic);
            var ak = new RectangleF(a.Left, a.Bottom - keyH, a.Width, keyH);
            bool ah = spot("all", ak);
            DpsForm.WideKey(g, ak, MaterialIcon.Calendar, L.T("All sessions"), From == null, ah, false);
        }

        public bool Click(PointF p)
        {
            var hit = spots.FirstOrDefault(s => s.Item2.Contains(p));
            if (hit == null) return false;
            var id = hit.Item1;
            if (id == "prev") Month = Month.AddMonths(-1);
            else if (id == "next") Month = Month.AddMonths(1);
            else if (id == "all") { From = To = null; picking = false; if (Changed != null) Changed(); }
            else if (id.StartsWith("day:"))
            {
                var day = DateTime.ParseExact(id.Substring(4), "yyyyMMdd", CultureInfo.InvariantCulture);
                if (!picking || From == null) { From = To = day; picking = true; }
                else { if (day < From.Value) { To = From; From = day; } else To = day; picking = false; }
                if (day.Month != Month.Month) Month = new DateTime(day.Year, day.Month, 1);
                if (Changed != null) Changed();
            }
            return true;
        }
    }
}
