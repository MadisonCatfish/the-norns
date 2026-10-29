// Settings.cs - the settings, shown on the body's own screen in place of the meter (owner 27.10.2026): a click on ノルン
// opens and closes them. Language (the app's six), the game log keys that used to sit on the strip (open the combat log
// folder, reset the anomaly counter), and a place for the per-session statistics that come later. While they are open
// the screen takes clicks instead of passing them to the game.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

namespace TheNorns
{
    partial class DpsForm
    {
        bool settingsOpen;
        string hotSpot, downSpot;   // "page:<name>", "lang:<code>", "logs", "reset", "back"
        string page = "general";    // general, logs, stats, about (the RIFT-like plan, step 1)
        static readonly string[][] Pages = { new[] { "general", "General" }, new[] { "modes", "Modes" }, new[] { "logs", "Game logs" }, new[] { "storage", "Storage" }, new[] { "stats", "Statistics" }, new[] { "about", "About" } };
        readonly List<Tuple<string, RectangleF>> spots = new List<Tuple<string, RectangleF>>();
        double resetShownUntil = -1;
        bool demoResetShown;
        public bool? DemoLogDirOk;   // selftest: the folder mark
        // the tabs scroll (owner, 28.10.2026: more pages will come): TabsShown at a time, the arrow keys side by side on
        // the left; tabFirst = the first one shown, tabShift = where the row is drawn (glides to tabFirst)
        const int TabsShown = 4;
        int tabFirst; float tabShift; RectangleF tabRow;
        void ShowTab(string p)
        {
            int k = Array.FindIndex(Pages, x => x[0] == p);
            if (k < tabFirst) tabFirst = k; else if (k >= tabFirst + TabsShown) tabFirst = k - TabsShown + 1;
            tabFirst = Math.Max(0, Math.Min(Math.Max(0, Pages.Length - TabsShown), tabFirst));
        }
        void ScrollTabs(int by) { tabFirst = Math.Max(0, Math.Min(Math.Max(0, Pages.Length - TabsShown), tabFirst + by)); }

        void ToggleSettings()
        {
            if (settingsOpen) CommitField();
            settingsOpen = !settingsOpen;
            hotSpot = downSpot = null; spots.Clear();
            if (settingsOpen) { ShowTab(page); tabShift = tabFirst; }   // the open page's tab in view
            if (Compact) Size = new Size(DesiredWidth, DesiredHeight);   // compact: full height while the settings are open
            tip.Hide(this);
        }

        string SpotAt(Point p)
        {
            foreach (var s in spots) if (s.Item2.Contains(p)) return s.Item1;
            return null;
        }

        void Activate(string id)
        {
            if (ActivateMode(id)) return;
            if (id == "back") { ToggleSettings(); return; }
            if (id == "logs") { OpenGameLogs(); return; }
            if (id == "dir:select")
            {
                using (var d = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = GameLogTail.Dir, Description = L.T("Combat log folder (Documents\\EVE\\logs\\Gamelogs)") })
                    if (d.ShowDialog(this) == System.Windows.Forms.DialogResult.OK) SetLogDir(d.SelectedPath);
                return;
            }
            if (id == "dir:detect") { SetLogDir(""); return; }
            if (id == "reset") { stats.Tracker.ResetAll(); resetShownUntil = clock.Elapsed.TotalSeconds + 2.5; return; }
            if (id.StartsWith("lang:")) SetLanguage(id.Substring(5));
            if (id.StartsWith("page:")) { page = id.Substring(5); storage = null; sessions = null; ShowTab(page); }
            if (id == "tabs:prev") ScrollTabs(-1);
            if (id == "tabs:next") ScrollTabs(1);
            if (id == "data") OpenDataFolder();
            if (id.StartsWith("about:")) { aboutView = id.Substring(6); newsScroll = 0; }
            if (id == "update") { if (updateState == 3) OpenReleasePage(); else CheckForUpdates(false); }
            if (id == "updatestart") { UpdateAtStart = !UpdateAtStart; SaveApp("update_check", UpdateAtStart); }
            if (id == "compact")
            {
                Compact = !Compact; SaveApp("compact", Compact);
                Size = new Size(DesiredWidth, DesiredHeight);
                if (shell != null) { shell.Dispose(); shell = null; }
            }
            if (id == "potato")
            {
                Potato = !Potato; SaveApp("potato", Potato);
                timer.Interval = Potato ? PotatoInterval : 40;
                if (shell != null) { shell.Dispose(); shell = null; }   // the hull drawn again, plain or shaded
            }
            if (id.StartsWith("scale:"))
            {
                UiScale = float.Parse(id.Substring(6), CultureInfo.InvariantCulture);
                SaveApp("scale", (double)UiScale);
                var r = Right; Size = new Size(DesiredWidth, DesiredHeight); Left = r - Width;   // grows to the left: the right edge stays
                if (shell != null) { shell.Dispose(); shell = null; }
            }
            if (id == "spawnsound") { SpawnSound = !SpawnSound; SaveApp("spawn_sound", SpawnSound); if (SpawnSound) PlayAlert(); }
            if (id == "playalert") PlayAlert();
            if (id == "remember") { RememberPos = !RememberPos; SaveApp("remember_position", RememberPos); if (RememberPos) SavePos(); }
        }

        // the choice is kept in config.json "app": { "language": ... } (Program reads it on start) and applies at once
        void SetLanguage(string code)
        {
            if (!L.Known(code) || code == L.Code) return;
            L.Init(code);
            if (SelfTesting) return;
            try
            {
                Directory.CreateDirectory(AppInfo.DataDir);
                var root = File.Exists(AppInfo.ConfigPath) ? ConfigStore.Load(AppInfo.ConfigPath) : new Dictionary<string, object>();
                ConfigStore.Obj(root, "app")["language"] = code;
                ConfigStore.Save(AppInfo.ConfigPath, root);
            }
            catch { }
        }

        // "" = found by itself (GameLogTail.Detect); kept in config.json "logs": { "dir": ... }; the next scan reads it
        void SetLogDir(string dir)
        {
            GameLogTail.Chosen = dir;
            if (SelfTesting) return;
            try
            {
                Directory.CreateDirectory(AppInfo.DataDir);
                var root = File.Exists(AppInfo.ConfigPath) ? ConfigStore.Load(AppInfo.ConfigPath) : new Dictionary<string, object>();
                ConfigStore.Obj(root, "logs")["dir"] = dir;
                ConfigStore.Save(AppInfo.ConfigPath, root);
            }
            catch { }
        }

        void SaveApp(string key, object value)
        {
            if (SelfTesting) return;
            try
            {
                Directory.CreateDirectory(AppInfo.DataDir);
                var root = File.Exists(AppInfo.ConfigPath) ? ConfigStore.Load(AppInfo.ConfigPath) : new Dictionary<string, object>();
                ConfigStore.Obj(root, "app")[key] = value;
                ConfigStore.Save(AppInfo.ConfigPath, root);
            }
            catch { }
        }

        // ---- About ----
        string aboutView = "news";   // news, credits, legal
        float newsScroll;            // What's new: lines scrolled with the wheel
        int updateState;             // 0 not checked, 1 checking, 2 up to date, 3 newer found, 4 failed, 5 no release page yet
        string updateLatest, updatePage;
        // the update check at start (owner, 27.10.2026: on by default, a switch in General; before, only on the key)
        public static bool UpdateAtStart = !(AppSetting("update_check") is bool) || (bool)AppSetting("update_check");
        public void DemoUpdate(string version) { updateLatest = version; updatePage = ReleasesPage; updateState = 3; }
        const string ReleasesPage = "https://github.com/MadisonCatfish/the-norns/releases/latest";
        public string DemoAboutView { set { aboutView = value; } }

        // where releases are published (GitHub Releases API); empty until the repository is public
        const string ReleasesApi = "https://api.github.com/repos/MadisonCatfish/the-norns/releases/latest";

        string UpdateLabel()
        {
            switch (updateState)
            {
                case 1: return L.T("Checking…");
                case 2: return L.T("Up to date");
                case 3: return L.F("Download {0}", updateLatest);
                case 4: return L.T("Check failed");
                case 5: return L.T("No release page yet");
                default: return L.T("Check for updates");
            }
        }

        // quiet (the check at start): nothing shows unless a newer version is found
        void CheckForUpdates(bool quiet)
        {
            if (updateState == 1 || updateState == 3) return;
            if (ReleasesApi == "") { if (!quiet) updateState = 5; return; }
            if (!quiet) updateState = 1;
            new System.Threading.Thread(() =>
            {
                int st; string latest = null, html = null;
                try
                {
                    System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
                    using (var wc = new System.Net.WebClient())
                    {
                        wc.Headers.Add("User-Agent", "TheNorns");
                        var json = wc.DownloadString(ReleasesApi);
                        var o = new System.Web.Script.Serialization.JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
                        latest = o != null && o.ContainsKey("tag_name") ? Convert.ToString(o["tag_name"]).TrimStart('v') : null;
                        html = o != null && o.ContainsKey("html_url") ? Convert.ToString(o["html_url"]) : null;
                    }
                    Version a, b;
                    bool newer = latest != null && Version.TryParse(latest.Split('-')[0], out a) && Version.TryParse(AppInfo.Version, out b) && a > b;
                    st = latest == null ? 4 : newer ? 3 : 2;
                }
                catch { st = 4; }
                if (quiet && st != 3) st = 0;   // at start: up to date or no network says nothing
                try { BeginInvoke((Action)(() => { updateLatest = latest; updatePage = html ?? ReleasesPage; updateState = st; })); } catch { }
            }) { IsBackground = true }.Start();
        }

        // the release page in the default browser: the download is the user's, nothing installs by itself
        void OpenReleasePage()
        {
            try { System.Diagnostics.Process.Start(updatePage ?? ReleasesPage); } catch { }
        }

        static string changelog;
        static string Changelog
        {
            get
            {
                if (changelog == null)
                    try { using (var s = typeof(DpsForm).Assembly.GetManifestResourceStream("TheNorns.CHANGELOG.md")) changelog = s == null ? "" : new StreamReader(s, System.Text.Encoding.UTF8).ReadToEnd(); }
                    catch { changelog = ""; }
                return changelog;
            }
        }

        // What's new: the changelog's versions as headings and their points, scrolled with the mouse wheel
        void PaintNews(Graphics g, RectangleF box)
        {
            float y = box.Top - newsScroll;
            var hf = NornsFonts.Screen(U(7)); var bf = NornsFonts.Body(U(6.6f));
            using (var hb = new SolidBrush(BodyTheme.Accent)) using (var bb = new SolidBrush(Color.FromArgb(200, 203, 210)))
                foreach (var raw in Changelog.Replace("\r", "").Split('\n'))
                {
                    var l = raw.TrimEnd();
                    if (l.StartsWith("# ") || l == "") continue;
                    if (l.StartsWith("## "))
                    {
                        y += U(3);
                        g.DrawString(l.Substring(3).ToUpperInvariant(), hf, hb, box.Left, y, StringFormat.GenericTypographic);
                        y += hf.GetHeight(g) + U(3);
                        continue;
                    }
                    var t = l.TrimStart(' ', '-').Replace("`", "").Replace("**", "");
                    float indent = l.StartsWith("  ") ? U(8) : 0;
                    var f = TextFont(t, U(6.6f)) ?? bf;
                    var h = g.MeasureString("• " + t, f, (int)(box.Width - indent)).Height;
                    if (y + h > box.Top - U(20) && y < box.Bottom + U(5)) g.DrawString("• " + t, f, bb, new RectangleF(box.Left + indent, y, box.Width - indent, h + U(1)));
                    y += h + U(2);
                }
            newsHeight = y + newsScroll - box.Top; newsBox = box.Height;
        }
        float newsHeight, newsBox;

        protected override void OnMouseWheel(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (settingsOpen && tabRow.Contains(e.Location)) { ScrollTabs(-Math.Sign(e.Delta)); Render(); return; }   // the wheel over the tabs scrolls them
            if (!settingsOpen || page != "about" || aboutView != "news") return;
            newsScroll = Math.Max(0, Math.Min(Math.Max(0, newsHeight - newsBox), newsScroll - Math.Sign(e.Delta) * U(24)));
            Render();
        }

        static string CreditsText()
        {
            return L.T("Fonts") + ": Orbitron, Space Grotesk (SIL OFL 1.1); Zarame Gothic by _mokuzai_ (SIL OFL 1.1) — ノルン.\n" +
                   L.T("Animation") + ": loadersz \"working\" by lumberjacque (MIT).\n" +
                   L.T("Icons") + ": Google Material Icons / Symbols (Apache 2.0).\n" +
                   L.T("NPC damage types") + ": EVE University wiki.\n" +
                   L.T("Licenses are in the licenses folder next to the app.");
        }

        static string LegalText()
        {
            return L.F("{0} is a free, independent, fan-made tool. It is not affiliated with, endorsed or sponsored by CCP hf. EVE, EVE Online and CCP are trademarks or registered trademarks of CCP hf.", AppInfo.Name) + "\n\n" +
                   L.T("• It never sends anything to the game: no key presses, no clicks, no screen reading.") + "\n" +
                   L.T("• It does not read game memory, network traffic or cache files, and does not change the EVE client or its settings.") + "\n" +
                   L.F("• It collects no data. The only network request is the update check: at start (it can be turned off in {0}) and when you press it.", L.T("General"));
        }

        // a small key in a row of three: lit = the one shown
        void PaintTabKey(Graphics g, RectangleF r, string label, string id, bool on)
        {
            bool hot = hotSpot == id;
            using (var p = Round(r, 5))
            {
                using (var b = new SolidBrush(on ? Color.FromArgb(40, BodyTheme.Accent) : hot ? Color.FromArgb(26, BodyTheme.Ink) : Color.FromArgb(12, BodyTheme.Ink))) g.FillPath(b, p);
                using (var pen = new Pen(on ? BodyTheme.Accent : Color.FromArgb(50, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
            }
            var f = TextFont(label, U(6.8f));
            var sz = g.MeasureString(label, f, 1000, StringFormat.GenericTypographic);
            using (var b = new SolidBrush(on ? BodyTheme.Accent : BodyTheme.Ink))
                g.DrawString(label, f, b, new RectangleF(r.Left + Math.Max(U(3), (r.Width - sz.Width) / 2), r.Top + (r.Height - sz.Height) / 2, r.Width - U(6), sz.Height + U(1)),
                    new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            spots.Add(Tuple.Create(id, r));
        }

        // ---- Storage ----
        List<Tuple<string, string, bool>> storage;   // label, size, total row

        static string Shown(string path)
        {
            var la = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).TrimEnd('\\');
            return path.StartsWith(la + "\\", StringComparison.OrdinalIgnoreCase) ? "%LOCALAPPDATA%" + path.Substring(la.Length) : path;
        }

        static string Bytes(long n)
        {
            if (n >= 1 << 20) return (n / 1048576.0).ToString("0.00", CultureInfo.InvariantCulture) + " MB";
            if (n >= 1 << 10) return (n / 1024.0).ToString("0.0", CultureInfo.InvariantCulture) + " KB";
            return n + " B";
        }

        static List<Tuple<string, string, bool>> ReadStorage()
        {
            long settings = 0, window = 0, crash = 0, other = 0, all = 0; int crashes = 0;
            try
            {
                if (Directory.Exists(AppInfo.DataDir))
                    foreach (var f in new DirectoryInfo(AppInfo.DataDir).GetFiles("*", SearchOption.AllDirectories))
                    {
                        all += f.Length;
                        var n = f.Name.ToLowerInvariant();
                        if (n.StartsWith("config.json")) settings += f.Length;
                        else if (n == "dps_pos.txt") window += f.Length;
                        else if (n.StartsWith("crash_")) { crash += f.Length; crashes++; }
                        else other += f.Length;
                    }
            }
            catch { }
            return new List<Tuple<string, string, bool>>
            {
                Tuple.Create(L.T("All data"), Bytes(all), true),
                Tuple.Create(L.T("Settings"), Bytes(settings), false),
                Tuple.Create(L.T("Window position"), Bytes(window), false),
                Tuple.Create(L.T("Crash reports"), (crashes > 0 ? crashes + " · " : "") + Bytes(crash), false),
                Tuple.Create(L.T("Other"), Bytes(other), false),
            };
        }

        void OpenDataFolder()
        {
            try { Directory.CreateDirectory(AppInfo.DataDir); System.Diagnostics.Process.Start("explorer.exe", AppInfo.DataDir); }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show(this, ex.Message, AppInfo.Name); }
        }

        // selftest: the settings open, a spot under the mouse, the reset note shown
        public void DemoSettings(bool open, string hot, bool resetShown, string showPage = "general")
        {
            settingsOpen = open; page = showPage; hotSpot = hot; demoResetShown = resetShown; spots.Clear();
            tabFirst = 0; ShowTab(page); tabShift = tabFirst;
            if (Size.Height != DesiredHeight) Size = new Size(DesiredWidth, DesiredHeight);
        }

        // ノルン as a key: under the mouse the name itself turns accent (owner, 27.10.2026: no bar under it)
        void PaintNameKey(Graphics g)
        {
            bool on = hoverName || ShowHoverName || settingsOpen;
            if (!on) return;
            var c = settingsOpen && !(hoverName || ShowHoverName) ? Color.FromArgb(150, BodyTheme.Accent) : BodyTheme.Accent;
            var s = StripRect;
            var name = NamePath(U(9.5f));
            if (name == null) return;
            using (name)
            {
                var nb0 = name.GetBounds();
                using (var m = new Matrix()) { m.Translate(U(Pad + 7) - nb0.X, s.Top + (s.Height - nb0.Height) / 2 - nb0.Y); name.Transform(m); }
                using (var b = new SolidBrush(c)) g.FillPath(b, name);
            }
        }

        // Ukrainian: the embedded fonts have no Cyrillic
        static readonly Dictionary<string, Font> sysFonts = new Dictionary<string, Font>();
        static Font Sys(string family, float px)
        {
            var key = family + "|" + px; Font f;
            if (!sysFonts.TryGetValue(key, out f)) sysFonts[key] = f = new Font(family, px, GraphicsUnit.Pixel);
            return f;
        }
        static bool Cyrillic(string s) { return s.Any(ch => ch >= 'Ѐ' && ch <= 'ӿ'); }
        Font TitleFont(string text, float px) { return Cyrillic(text) ? Sys("Segoe UI Semibold", px * 0.95f) : NornsFonts.Screen(px); }
        Font TextFont(string text, float px) { return Cyrillic(text) ? Sys("Segoe UI", px * 0.95f) : NornsFonts.Body(px); }

        void PaintSettings(Graphics g, RectangleF c)
        {
            spots.Clear();
            var line = new Action<float>(ly => { using (var b = new SolidBrush(Color.FromArgb(26, BodyTheme.Ink))) g.FillRectangle(b, c.Left, ly, c.Width, Math.Max(1f, U(0.5f))); });

            // header: the settings symbol, the title, the back key
            var title = L.T("Settings");
            var tf = TitleFont(title, U(9));
            float th = tf.GetHeight(g);
            var ir = new RectangleF(c.Left, c.Top + (th - U(10)) / 2, U(10), U(10));
            using (var ab = new SolidBrush(BodyTheme.Accent)) MaterialIcon.Draw(g, MaterialIcon.Settings, ir, ab);
            Glow(g, title, tf, BodyTheme.Ink, ir.Right + U(5), c.Top);
            var back = new RectangleF(c.Right - U(11), c.Top - U(2), U(11), U(15));
            CloseKey(g, back, hotSpot == "back", downSpot == "back", U(1) * 0.8f);
            spots.Add(Tuple.Create("back", RectangleF.Inflate(back, U(2), U(2))));
            float y = c.Top + Math.Max(th, U(13)) + U(6);
            line(y); y += U(7);

            // the pages: the two arrow keys side by side on the left (Material arrow_left / arrow_right, dim when there is
            // nothing more that way), then TabsShown tabs, the open one's label in the accent (owner, 28.10.2026)
            float ak = U(14), ah = U(13), ay = y - U(1.5f);
            for (int a = 0; a < 2; a++)
            {
                string id = a == 0 ? "tabs:prev" : "tabs:next";
                bool can = a == 0 ? tabFirst > 0 : tabFirst + TabsShown < Pages.Length, hot = can && hotSpot == id;
                var kr = new RectangleF(c.Left + a * (ak + U(3)), ay, ak, ah);
                using (var p = Round(kr, 5))
                {
                    using (var b = new SolidBrush(hot ? Color.FromArgb(26, BodyTheme.Ink) : Color.FromArgb(12, BodyTheme.Ink))) g.FillPath(b, p);
                    using (var pen = new Pen(Color.FromArgb(can ? 50 : 22, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
                }
                float ic = U(10);
                using (var b = new SolidBrush(can ? BodyTheme.Ink : Color.FromArgb(60, BodyTheme.Ink)))
                    MaterialIcon.Draw(g, a == 0 ? MaterialIcon.ArrowLeft : MaterialIcon.ArrowRight, new RectangleF(kr.Left + (kr.Width - ic) / 2, kr.Top + (kr.Height - ic) / 2, ic, ic), b);
                if (can) spots.Add(Tuple.Create(id, kr));
            }
            float rowLeft = c.Left + 2 * ak + U(3) + U(4);
            tabRow = RectangleF.FromLTRB(rowLeft, y - U(2), c.Right, y + U(12));
            float tw = tabRow.Width / TabsShown;
            tabShift = Potato || SelfTesting ? tabFirst : tabShift + (tabFirst - tabShift) * 0.35f;
            if (Math.Abs(tabFirst - tabShift) < 0.01f) tabShift = tabFirst; else Invalidate();
            var tabClip = g.Save();
            g.SetClip(tabRow);
            for (int k = 0; k < Pages.Length; k++)
            {
                string id = "page:" + Pages[k][0], label = L.T(Pages[k][1]);
                bool on = page == Pages[k][0], hot = hotSpot == id;
                var tr = new RectangleF(rowLeft + (k - tabShift) * tw, y - U(2), tw - U(3), U(14));
                if (tr.Right <= tabRow.Left || tr.Left >= tabRow.Right) continue;
                if (hot && !on) using (var p = Round(tr, 5)) using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillPath(b, p);
                var f = TitleFont(label, U(6));
                var up = label.ToUpperInvariant();
                float w = Math.Min(tr.Width - U(4), g.MeasureString(up, f, 1000, StringFormat.GenericTypographic).Width);
                using (var b = new SolidBrush(on ? BodyTheme.Accent : hot ? Color.FromArgb(200, 203, 210) : Color.FromArgb(130, 133, 140)))
                    g.DrawString(up, f, b, new RectangleF(tr.Left + (tr.Width - w) / 2, tr.Top + U(2.5f), w + U(1), f.GetHeight(g) + U(1)),
                        new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                var hit = RectangleF.Intersect(tr, tabRow);
                if (hit.Width > tw / 2) spots.Add(Tuple.Create(id, hit));
            }
            g.Restore(tabClip);
            y += U(16);
            line(y); y += U(8);

            if (page == "general")
            {
                // language: the six, three columns (owner, 28.10.2026), a lamp lit at the one in use
                var lt = L.T("Language");
                Glow(g, lt, TitleFont(lt, U(6.5f)), Muted, c.Left, y);
                y += U(11);
                float colW = c.Width / 3, rowH = U(16);
                for (int i = 0; i < L.Languages.Length; i++)
                {
                    string code = L.Languages[i][0], name = L.Languages[i][1];
                    var row = new RectangleF(c.Left + (i / 2) * colW, y + (i % 2) * rowH, colW - U(4), rowH - U(2));
                    string id = "lang:" + code;
                    bool hot = hotSpot == id, cur = L.Code == code;
                    if (hot) using (var p = Round(row, 6)) using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillPath(b, p);
                    float d = U(5.5f), lx = row.Left + U(5), ly = row.Top + row.Height / 2;
                    if (cur) SoftGlow(g, new PointF(lx + d / 2, ly), d / 2 + U(4), BodyTheme.Accent, 140);
                    using (var b = new SolidBrush(cur ? BodyTheme.Accent : Color.FromArgb(46, 48, 30))) g.FillEllipse(b, lx, ly - d / 2, d, d);
                    var nf = TextFont(name, U(8));
                    using (var b = new SolidBrush(cur || hot ? BodyTheme.Ink : Color.FromArgb(165, 168, 176)))
                        g.DrawString(name, nf, b, lx + d + U(6), ly - nf.GetHeight(g) / 2, StringFormat.GenericTypographic);
                    spots.Add(Tuple.Create(id, row));
                }
                y += 2 * rowH + U(6);

                // window: the body's size and whether it comes back where it was
                var wt = L.T("Window");
                Glow(g, wt, TitleFont(wt, U(6.5f)), Muted, c.Left, y);
                y += U(11);
                var sl = L.T("Size");
                var sf2 = TextFont(sl, U(7.5f));
                using (var b = new SolidBrush(Color.FromArgb(200, 203, 210))) g.DrawString(sl, sf2, b, c.Left, y + (U(15) - sf2.GetHeight(g)) / 2, StringFormat.GenericTypographic);
                float bx = c.Right - 3 * U(32);
                foreach (var sc in new[] { 1f, 1.25f, 1.5f })
                {
                    string id = "scale:" + sc.ToString(CultureInfo.InvariantCulture);
                    var br = new RectangleF(bx, y, U(29), U(15)); bx += U(32);
                    bool cur = Math.Abs(UiScale - sc) < 0.01f, hot = hotSpot == id;
                    using (var p = Round(br, 5))
                    {
                        using (var b = new SolidBrush(cur ? Color.FromArgb(40, BodyTheme.Accent) : hot ? Color.FromArgb(26, BodyTheme.Ink) : Color.FromArgb(12, BodyTheme.Ink))) g.FillPath(b, p);
                        using (var pen = new Pen(cur ? BodyTheme.Accent : Color.FromArgb(50, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
                    }
                    var lab = (int)Math.Round(sc * 100) + "%";
                    var lf = NornsFonts.Screen(U(6));
                    var ls = g.MeasureString(lab, lf, 1000, StringFormat.GenericTypographic);
                    using (var b = new SolidBrush(cur ? BodyTheme.Accent : BodyTheme.Ink)) g.DrawString(lab, lf, b, br.Left + (br.Width - ls.Width) / 2, br.Top + (br.Height - ls.Height) / 2, StringFormat.GenericTypographic);
                    spots.Add(Tuple.Create(id, br));
                }
                y += U(20);
                var rl = L.T("Remember window position");
                var rr = new RectangleF(c.Left, y, c.Width, U(15));
                bool rh = hotSpot == "remember";
                if (rh) using (var p = Round(rr, 6)) using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillPath(b, p);
                float dd = U(5.5f), lx2 = rr.Left + U(5), ly2 = rr.Top + rr.Height / 2;
                if (RememberPos) SoftGlow(g, new PointF(lx2 + dd / 2, ly2), dd / 2 + U(4), BodyTheme.Accent, 140);
                using (var b = new SolidBrush(RememberPos ? BodyTheme.Accent : Color.FromArgb(46, 48, 30))) g.FillEllipse(b, lx2, ly2 - dd / 2, dd, dd);
                var rf = TextFont(rl, U(7.5f));
                using (var b = new SolidBrush(rh ? BodyTheme.Ink : Color.FromArgb(200, 203, 210))) g.DrawString(rl, rf, b, lx2 + dd + U(6), ly2 - rf.GetHeight(g) / 2, StringFormat.GenericTypographic);
                spots.Add(Tuple.Create("remember", rr));
                y += U(19) - 3;   // 3 px tighter (owner, 28.10.2026)
                var ul = L.T("Check for updates at start");
                var ur = new RectangleF(c.Left, y, c.Width, U(15));
                bool uh = hotSpot == "updatestart";
                if (uh) using (var p = Round(ur, 6)) using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillPath(b, p);
                float ly3 = ur.Top + ur.Height / 2;
                if (UpdateAtStart) SoftGlow(g, new PointF(lx2 + dd / 2, ly3), dd / 2 + U(4), BodyTheme.Accent, 140);
                using (var b = new SolidBrush(UpdateAtStart ? BodyTheme.Accent : Color.FromArgb(46, 48, 30))) g.FillEllipse(b, lx2, ly3 - dd / 2, dd, dd);
                var uf = TextFont(ul, U(7.5f));
                using (var b = new SolidBrush(uh ? BodyTheme.Ink : Color.FromArgb(200, 203, 210))) g.DrawString(ul, uf, b, lx2 + dd + U(6), ly3 - uf.GetHeight(g) / 2, StringFormat.GenericTypographic);
                spots.Add(Tuple.Create("updatestart", ur));
                y += U(19) - 3;
                var pl = L.T("Potato mode: low CPU, no effects");
                var prr = new RectangleF(c.Left, y, c.Width, U(15));
                bool ph = hotSpot == "potato";
                if (ph) using (var p = Round(prr, 6)) using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillPath(b, p);
                float ly4 = prr.Top + prr.Height / 2;
                if (Potato) SoftGlow(g, new PointF(lx2 + dd / 2, ly4), dd / 2 + U(4), BodyTheme.Accent, 140);
                using (var b = new SolidBrush(Potato ? BodyTheme.Accent : Color.FromArgb(46, 48, 30))) g.FillEllipse(b, lx2, ly4 - dd / 2, dd, dd);
                var pf = TextFont(pl, U(7.5f));
                using (var b = new SolidBrush(ph ? BodyTheme.Ink : Color.FromArgb(200, 203, 210))) g.DrawString(pl, pf, b, lx2 + dd + U(6), ly4 - pf.GetHeight(g) / 2, StringFormat.GenericTypographic);
                spots.Add(Tuple.Create("potato", prr));
                y += U(19) - 3;
                var cl = L.T("Compact mode");
                var crr = new RectangleF(c.Left, y, c.Width, U(15));
                bool ch = hotSpot == "compact";
                if (ch) using (var p = Round(crr, 6)) using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillPath(b, p);
                float ly5 = crr.Top + crr.Height / 2;
                if (Compact) SoftGlow(g, new PointF(lx2 + dd / 2, ly5), dd / 2 + U(4), BodyTheme.Accent, 140);
                using (var b = new SolidBrush(Compact ? BodyTheme.Accent : Color.FromArgb(46, 48, 30))) g.FillEllipse(b, lx2, ly5 - dd / 2, dd, dd);
                var cf = TextFont(cl, U(7.5f));
                using (var b = new SolidBrush(ch ? BodyTheme.Ink : Color.FromArgb(200, 203, 210))) g.DrawString(cl, cf, b, lx2 + dd + U(6), ly5 - cf.GetHeight(g) / 2, StringFormat.GenericTypographic);
                spots.Add(Tuple.Create("compact", crr));
                y += U(19) - 3;
                // the sound when a titan or a dreadnought spawns (off by default), and a key to hear it
                var pk = new RectangleF(c.Right - U(29), y, U(29), U(15));
                var sr = new RectangleF(c.Left, y, pk.Left - U(4) - c.Left, U(15));
                bool sh = hotSpot == "spawnsound";
                if (sh) using (var p = Round(sr, 6)) using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillPath(b, p);
                float sd = U(5.5f), sx = sr.Left + U(5), sy = sr.Top + sr.Height / 2;
                if (SpawnSound) SoftGlow(g, new PointF(sx + sd / 2, sy), sd / 2 + U(4), BodyTheme.Accent, 140);
                using (var b = new SolidBrush(SpawnSound ? BodyTheme.Accent : Color.FromArgb(46, 48, 30))) g.FillEllipse(b, sx, sy - sd / 2, sd, sd);
                var sl2 = L.T("Sound on a titan or dreadnought");
                var sf3 = TextFont(sl2, U(7.5f));
                using (var b = new SolidBrush(sh ? BodyTheme.Ink : Color.FromArgb(200, 203, 210))) g.DrawString(sl2, sf3, b, sx + sd + U(6), sy - sf3.GetHeight(g) / 2, StringFormat.GenericTypographic);
                spots.Add(Tuple.Create("spawnsound", sr));
                // the play key, like the size keys (Material play_arrow)
                bool ph2 = hotSpot == "playalert";
                using (var p = Round(pk, 5))
                {
                    using (var b = new SolidBrush(ph2 ? Color.FromArgb(26, BodyTheme.Ink) : Color.FromArgb(12, BodyTheme.Ink))) g.FillPath(b, p);
                    using (var pen = new Pen(Color.FromArgb(50, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
                }
                float ic2 = U(9);
                using (var b = new SolidBrush(BodyTheme.Ink)) MaterialIcon.Draw(g, MaterialIcon.Play, new RectangleF(pk.Left + (pk.Width - ic2) / 2, pk.Top + (pk.Height - ic2) / 2, ic2, ic2), b);
                spots.Add(Tuple.Create("playalert", pk));
            }
            else if (page == "modes") PaintModes(g, c, y);
            else if (page == "logs")
            {
                // the keys that were on the strip, and where the combat logs are read from
                bool resetShown = demoResetShown || clock.Elapsed.TotalSeconds < resetShownUntil;
                PaintWideKey(g, new RectangleF(c.Left, y, c.Width, U(19)), MaterialIcon.Description, L.T("Open game logs folder"), "logs", false);
                y += U(23);
                PaintWideKey(g, new RectangleF(c.Left, y, c.Width, U(19)), MaterialIcon.Refresh, resetShown ? L.T("Anomaly counter reset") : L.T("Reset anomaly counter"), "reset", resetShown);
                y += U(28);
                // the folder: its path (from Documents on: no user name on screen), a mark whether logs are there,
                // Select / Detect
                var lt = L.T("Combat log folder");
                Glow(g, lt, TitleFont(lt, U(6.5f)), Muted, c.Left, y);
                y += U(11);
                var dir = GameLogTail.Dir; var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).TrimEnd('\\');
                bool ok = false;
                try { ok = Directory.Exists(dir) && Directory.EnumerateFiles(dir, "*.txt").Any(); } catch { }
                if (DemoLogDirOk != null) ok = DemoLogDirOk.Value;
                if (dir.StartsWith(docs + "\\", StringComparison.OrdinalIgnoreCase)) dir = "Documents" + dir.Substring(docs.Length);
                var field = new RectangleF(c.Left, y, c.Width - U(16), U(17));
                using (var p = Round(field, 5)) using (var b = new SolidBrush(Color.FromArgb(16, 17, 20))) using (var pen = new Pen(Color.FromArgb(40, BodyTheme.Ink), 1f)) { g.FillPath(b, p); g.DrawPath(pen, p); }
                var df = NornsFonts.Body(U(6.5f));
                using (var b = new SolidBrush(Color.FromArgb(200, 203, 210)))
                    g.DrawString(dir, df, b, new RectangleF(field.Left + U(5), field.Top + (field.Height - df.GetHeight(g)) / 2, field.Width - U(10), df.GetHeight(g) + U(1)),
                        new StringFormat { Trimming = StringTrimming.EllipsisPath, FormatFlags = StringFormatFlags.NoWrap });
                var mr = new RectangleF(field.Right + U(4), field.Top + (field.Height - U(11)) / 2, U(11), U(11));
                using (var b = new SolidBrush(ok ? BodyTheme.Accent : AlarmRed)) MaterialIcon.Draw(g, ok ? MaterialIcon.Check : MaterialIcon.Close, mr, b);
                y += U(21);
                float hw = (c.Width - U(4)) / 2;
                PaintWideKey(g, new RectangleF(c.Left, y, hw, U(19)), MaterialIcon.Folder, L.T("Select"), "dir:select", false);
                PaintWideKey(g, new RectangleF(c.Left + hw + U(4), y, hw, U(19)), MaterialIcon.Search, L.T("Detect"), "dir:detect", false);
                y += U(24);
                var note = ok ? L.T("Combat logs found.") : L.T("No combat logs here yet. They appear once a pilot is in space; or pick the folder.");
                using (var b = new SolidBrush(ok ? Color.FromArgb(120, BodyTheme.Ink) : Color.FromArgb(220, 255, 120, 110)))
                    g.DrawString(note, TextFont(note, U(6.5f)), b, new RectangleF(c.Left, y, c.Width, U(30)));
            }
            else if (page == "storage")
            {
                // the app's own folder: where it is, what is in it, how much
                if (storage == null) storage = ReadStorage();
                var dt = L.T("App data");
                Glow(g, dt, TitleFont(dt, U(6.5f)), Muted, c.Left, y);
                y += U(11);
                var df = NornsFonts.Body(U(6.5f));
                using (var b = new SolidBrush(Color.FromArgb(150, BodyTheme.Ink)))
                    g.DrawString(Shown(AppInfo.DataDir), df, b, new RectangleF(c.Left, y, c.Width, df.GetHeight(g) + U(1)), new StringFormat { Trimming = StringTrimming.EllipsisPath, FormatFlags = StringFormatFlags.NoWrap });
                y += U(13);
                var nf2 = NornsFonts.Screen(U(6.5f));
                foreach (var row in storage)
                {
                    using (var b = new SolidBrush(row.Item3 ? BodyTheme.Ink : Color.FromArgb(200, 203, 210))) g.DrawString(row.Item1, TextFont(row.Item1, U(7.2f)), b, c.Left, y, StringFormat.GenericTypographic);
                    var v = row.Item2;
                    using (var b = new SolidBrush(row.Item3 ? BodyTheme.Accent : BodyTheme.Ink)) g.DrawString(v, nf2, b, c.Right - g.MeasureString(v, nf2, 1000, StringFormat.GenericTypographic).Width, y + U(0.5f), StringFormat.GenericTypographic);
                    y += U(row.Item3 ? 15 : 12);
                    if (row.Item3) line(y - U(5));
                }
                y += U(4);
                PaintWideKey(g, new RectangleF(c.Left, y, c.Width, U(19)), MaterialIcon.Folder, L.T("Open app data folder"), "data", false);
            }
            else if (page == "stats")
            {
                // this session and the ones before: when, how long, sites done, ISK
                if (sessions == null) sessions = ReadSessions();
                var all = new List<SessionInfo> { CurrentSession() };
                all.AddRange(sessions);
                long isk = all.Sum(x => x.Isk); int sites = all.Sum(x => x.Sites), spawns = all.Sum(x => x.Spawns);
                var tt = L.T("All sessions");
                Glow(g, tt, TitleFont(tt, U(6.5f)), Muted, c.Left, y);
                var tv = sites + " " + L.T("anom") + "  ·  " + FormatIsk(isk);
                var vf2 = NornsFonts.Screen(U(6.5f));
                float tvx = c.Right - g.MeasureString(tv.ToUpperInvariant(), vf2, 1000, StringFormat.GenericTypographic).Width;
                using (var b = new SolidBrush(BodyTheme.Accent)) g.DrawString(tv.ToUpperInvariant(), vf2, b, tvx, y, StringFormat.GenericTypographic);
                StarCount(g, spawns, vf2, BodyTheme.Accent, tvx - U(8), y);
                y += U(12);
                line(y); y += U(5);
                var rf2 = NornsFonts.Body(U(6.8f));
                float rowH = U(15);
                for (int k = 0; k < all.Count && y + rowH < c.Bottom - U(10); k++)
                {
                    var x = all[k];
                    bool now = k == 0;
                    var when = now ? L.T("This session") : x.Start.ToString("dd.MM HH:mm", CultureInfo.InvariantCulture);
                    var dur = x.End - x.Start;
                    var mid = (int)dur.TotalHours + ":" + dur.Minutes.ToString("00") + "  ·  " + x.Sites + " " + L.T("anom");
                    using (var b = new SolidBrush(now ? BodyTheme.Ink : Color.FromArgb(190, 193, 200)))
                    {
                        g.DrawString(when, TextFont(when, U(6.8f)), b, c.Left, y, StringFormat.GenericTypographic);
                        g.DrawString(mid, TextFont(mid, U(6.8f)), b, c.Left + c.Width * 0.36f, y, StringFormat.GenericTypographic);
                    }
                    var iv = FormatIsk(x.Isk).ToUpperInvariant();
                    float ivx = c.Right - g.MeasureString(iv, vf2, 1000, StringFormat.GenericTypographic).Width;
                    using (var b = new SolidBrush(now ? BodyTheme.Accent : BodyTheme.Ink)) g.DrawString(iv, vf2, b, ivx, y + U(0.5f), StringFormat.GenericTypographic);
                    StarCount(g, x.Spawns, vf2, x.Spawns > 0 ? BodyTheme.Ink : Color.FromArgb(90, BodyTheme.Ink), c.Right - U(62), y + U(0.5f));
                    y += rowH;
                }
                if (all.Count == 1 && all[0].Sites == 0)
                    using (var b = new SolidBrush(Color.FromArgb(110, BodyTheme.Ink))) { var e2 = L.T("No sites done yet. Every session is kept here from now on."); g.DrawString(e2, TextFont(e2, U(6.8f)), b, new RectangleF(c.Left, y, c.Width, U(30))); }
            }
            else
            {
                // about: name and version, the update check, then one of: what's new / credits / legal (keys at the bottom)
                var nm = AppInfo.Name.ToUpperInvariant();
                Glow(g, nm, NornsFonts.Screen(U(10)), BodyTheme.Ink, c.Left, y);
                var vt = AppInfo.VersionText.ToUpperInvariant() + "  ·  MIT";
                using (var b = new SolidBrush(BodyTheme.Accent)) g.DrawString(vt, NornsFonts.Body(U(6.5f)), b, c.Left, y + U(14), StringFormat.GenericTypographic);
                // the update check: only when pressed (nothing goes to the network otherwise)
                var uk = new RectangleF(c.Right - U(118), y, U(118), U(19));
                PaintWideKey(g, uk, updateState == 3 ? MaterialIcon.Download : MaterialIcon.Refresh, UpdateLabel(), "update", updateState == 2 || updateState == 3);
                y += U(27);
                float kh = U(17), kw = (c.Width - 2 * U(4)) / 3, ky = c.Bottom - U(12) - kh;
                var subs = new[] { new[] { "news", "What's new" }, new[] { "credits", "Credits" }, new[] { "legal", "Legal & info" } };
                for (int k = 0; k < 3; k++)
                    PaintTabKey(g, new RectangleF(c.Left + k * (kw + U(4)), ky, kw, kh), L.T(subs[k][1]), "about:" + subs[k][0], aboutView == subs[k][0]);
                var box = RectangleF.FromLTRB(c.Left, y, c.Right, ky - U(6));
                using (var p = Round(box, 6)) using (var b = new SolidBrush(Color.FromArgb(10, BodyTheme.Ink))) g.FillPath(b, p);
                box.Inflate(-U(6), -U(5));
                var st2 = g.Save();
                g.SetClip(box);
                if (aboutView == "news") PaintNews(g, box);
                else
                {
                    var text = aboutView == "credits" ? CreditsText() : LegalText();
                    var f = TextFont(text, U(6.8f));
                    using (var b = new SolidBrush(Color.FromArgb(200, 203, 210))) g.DrawString(text, f, b, box);
                }
                g.Restore(st2);
            }

            // the version, bottom right
            var ver = (AppInfo.Name + " " + AppInfo.VersionText).ToUpperInvariant();
            var vf = NornsFonts.Body(U(5.5f));
            using (var b = new SolidBrush(Color.FromArgb(80, BodyTheme.Ink)))
                g.DrawString(ver, vf, b, c.Right - g.MeasureString(ver, vf, 1000, StringFormat.GenericTypographic).Width, c.Bottom - vf.GetHeight(g), StringFormat.GenericTypographic);
        }

        // the Material star and a count after it (the spawns of a session), ending at `right`
        void StarCount(Graphics g, int n, Font f, Color c, float right, float y)
        {
            float h = f.GetHeight(g), ic = U(7.5f);
            float x = right - ic - U(2) - g.MeasureString(n.ToString(CultureInfo.InvariantCulture), f, 1000, StringFormat.GenericTypographic).Width;
            using (var b = new SolidBrush(c))
            {
                MaterialIcon.Draw(g, MaterialIcon.Star, new RectangleF(x, y + (h - ic) / 2 - U(0.5f), ic, ic), b);
                g.DrawString(n.ToString(CultureInfo.InvariantCulture), f, b, x + ic + U(2), y, StringFormat.GenericTypographic);
            }
        }

        // a full-width key on the screen: a dark key face, the icon and the label; lit = the action just happened
        void PaintWideKey(Graphics g, RectangleF r, string icon, string label, string id, bool lit)
        {
            bool hot = hotSpot == id, down = downSpot == id;
            using (var rim = Round(r, 5)) using (var b = new SolidBrush(BodyTheme.KeyRim)) g.FillPath(b, rim);
            var face = RectangleF.Inflate(r, -U(1), -U(1));
            if (down) face.Offset(0, U(0.5f));
            using (var fp = Round(face, 4.5f))
            {
                Color top = hot ? Color.FromArgb(54, 56, 61) : BodyTheme.KeyTop, bottom = hot ? Color.FromArgb(32, 34, 38) : BodyTheme.KeyBottom;
                if (down) { top = Color.FromArgb(24, 25, 28); bottom = Color.FromArgb(34, 35, 39); }
                using (var lg = new LinearGradientBrush(face, top, bottom, 90f)) g.FillPath(lg, fp);
                if (!down)
                    using (var lg = new LinearGradientBrush(face, Color.FromArgb(34, 255, 255, 255), Color.FromArgb(115, 0, 0, 0), 90f))
                    using (var pen = new Pen(lg, 1f)) g.DrawPath(pen, fp);
            }
            float ic = U(10);
            var ir = new RectangleF(face.Left + U(6), face.Top + (face.Height - ic) / 2, ic, ic);
            var col = lit ? BodyTheme.Accent : hot ? BodyTheme.IconHover : BodyTheme.Icon;
            using (var ib = new SolidBrush(col)) MaterialIcon.Draw(g, icon, ir, ib);
            var f = TextFont(label, U(7.5f));
            using (var tb = new SolidBrush(lit ? BodyTheme.Accent : hot ? BodyTheme.Ink : Color.FromArgb(200, 203, 210)))
                g.DrawString(label, f, tb, new RectangleF(ir.Right + U(6), face.Top + (face.Height - f.GetHeight(g)) / 2, face.Right - ir.Right - U(12), f.GetHeight(g) + U(1)),
                    new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            spots.Add(Tuple.Create(id, r));
        }
    }
}
