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
        string page = "general";    // general, display, modes, eveo, data; stats and about from the header keys
        // five tabs (roadmap 17, 30.10.2026: General / Display / Modes / Carousel / Data); Statistics and About are keys in the
        // header beside the close key, not settings
        static readonly string[][] Pages = { new[] { "general", "General" }, new[] { "display", "Display" }, new[] { "modes", "Modes" }, new[] { "eveo", "Carousel" }, new[] { "data", "Data" } };
        readonly List<Tuple<string, RectangleF>> spots = new List<Tuple<string, RectangleF>>();
        double resetShownUntil = -1;
        bool demoResetShown;
        public bool? DemoLogDirOk;   // selftest: the folder mark
        // the tabs scroll (owner, 28.10.2026: more pages will come), the arrow keys side by side on the left; tabFirst =
        // the first one shown, tabShift = where the row is drawn (glides to tabFirst). Each tab is as wide as its label
        // plus the gap (owner, 29.10.2026: the gap 40% less than in the four equal cells before); tabX = where each one
        // starts, from the last frame (null before it: four to a row)
        const int TabsShown = 5;
        int tabFirst; float tabShift; RectangleF tabRow;
        float[] tabX; float tabGap;
        // the last first tab that still leaves no room at the right
        int TabsMaxFirst()
        {
            if (tabX == null) return Math.Max(0, Pages.Length - TabsShown);
            int f = 0;
            while (f < Pages.Length - 1 && tabX[Pages.Length] - tabGap - tabX[f] > tabRow.Width) f++;
            return f;
        }
        void ShowTab(string p)
        {
            int k = Array.FindIndex(Pages, x => x[0] == p);
            if (k < tabFirst) tabFirst = k;
            else if (tabX == null) { if (k >= tabFirst + TabsShown) tabFirst = k - TabsShown + 1; }
            else while (tabFirst < k && tabX[k + 1] - tabGap - tabX[tabFirst] > tabRow.Width) tabFirst++;
            tabFirst = Math.Max(0, Math.Min(TabsMaxFirst(), tabFirst));
        }
        void ScrollTabs(int by) { tabFirst = Math.Max(0, Math.Min(TabsMaxFirst(), tabFirst + by)); }
        // the row's offset at a (gliding) first tab
        float TabOffset(float s)
        {
            int i = Math.Min((int)s, Pages.Length - 1);
            return tabX[i] + (tabX[Math.Min(i + 1, Pages.Length)] - tabX[i]) * (s - i);
        }

        void ToggleSettings()
        {
            if (settingsOpen) CommitField();
            if (keyCapture != null) EndCapture();
            settingsOpen = !settingsOpen;
            hotSpot = downSpot = null; spots.Clear();
            if (settingsOpen) { ShowTab(page); tabShift = tabFirst; }   // the open page's tab in view
            if (Compact) Size = new Size(DesiredWidth, DesiredHeight);   // compact: full height while the settings are open
            tip.Hide(this);
        }

        // a key on the screen: clickable, and the glass dirt the cursor lights stays off it (owner, 31.10.2026). keyFaces
        // also holds keys that cannot be clicked now (a dim arrow); a key spot clipped away by a scrolled page goes with it
        readonly HashSet<string> keyIds = new HashSet<string>();
        readonly List<RectangleF> keyFaces = new List<RectangleF>();
        void AddKey(string id, RectangleF r) { spots.Add(Tuple.Create(id, r)); keyIds.Add(id); }
        IEnumerable<RectangleF> KeyRects() { return spots.Where(s => keyIds.Contains(s.Item1)).Select(s => s.Item2).Concat(keyFaces); }

        // a page's scroll bar as drawn in the last frame, so the mouse can take it and pull it (owner, 31.10.2026); a press
        // on the track beside the bar moves the bar's middle there
        sealed class ScrollBar { public RectangleF Track; public float BarH, Max; public Func<float> Get; public Action<float> Set; }
        readonly List<ScrollBar> bars = new List<ScrollBar>();
        ScrollBar dragBar; float dragGrip;
        void AddBar(RectangleF view, float right, float barH, float max, Func<float> get, Action<float> set)
        {
            bars.Add(new ScrollBar { Track = RectangleF.FromLTRB(right - U(7), view.Top, right + U(2), view.Bottom), BarH = barH, Max = max, Get = get, Set = set });
        }
        bool BeginBarDrag(Point p)
        {
            dragBar = bars.LastOrDefault(b => b.Track.Contains(p));
            if (dragBar == null) return false;
            float barY = dragBar.Track.Top + (dragBar.Track.Height - dragBar.BarH) * dragBar.Get() / dragBar.Max;
            dragGrip = p.Y >= barY && p.Y <= barY + dragBar.BarH ? p.Y - barY : dragBar.BarH / 2;
            MoveBarDrag(p.Y);
            return true;
        }
        bool MoveBarDrag(int y)
        {
            if (dragBar == null) return false;
            float room = Math.Max(1, dragBar.Track.Height - dragBar.BarH);
            dragBar.Set(Math.Max(0, Math.Min(dragBar.Max, (y - dragGrip - dragBar.Track.Top) / room * dragBar.Max)));
            Render();
            return true;
        }
        bool EndBarDrag() { if (dragBar == null) return false; dragBar = null; return true; }

        string SpotAt(Point p)
        {
            foreach (var s in spots) if (s.Item2.Contains(p)) return s.Item1;
            return null;
        }

        void Activate(string id)
        {
            if (ActivateMode(id)) return;
            if (ActivateEveO(id)) return;
            if (id == "back") { ToggleSettings(); return; }
            if (id == "stats:calendar") { OpenCalendar(); return; }
            if (id == "stats:readlogs") { ReadLogsIntoSessions(); return; }
            if (id == "logs") { OpenGameLogs(); return; }
            if (id == "dir:select")
            {
                using (var d = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = GameLogTail.Dir, Description = L.T("Combat log folder (Documents\\EVE\\logs\\Gamelogs)") })
                    if (d.ShowDialog(this) == System.Windows.Forms.DialogResult.OK) SetLogDir(d.SelectedPath);
                return;
            }
            if (id == "dir:detect") { SetLogDir(""); return; }
            if (id == "reset") { stats.Tracker.ResetAll(); resetShownUntil = Clock + 2.5; return; }
            if (id.StartsWith("lang:")) SetLanguage(id.Substring(5));
            if (id.StartsWith("page:")) { page = id.Substring(5); storage = null; sessions = null; ShowTab(page); }
            if (id == "tabs:prev") ScrollTabs(-1);
            if (id == "tabs:next") ScrollTabs(1);
            if (id == "data") OpenDataFolder();
            if (id.StartsWith("about:")) { aboutView = id.Substring(6); newsScroll = 0; }
            // a newer version: download its installer and run it (Update.cs); after a failed download, the release page
            if (id == "update") { if (updateState == 3) StartUpdate(); else if (updateState == 7) OpenReleasePage(); else if (updateState != 6) CheckForUpdates(false); }
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
            if (id == "closetray") { CloseToTray = !CloseToTray; SaveApp("close_to_tray", CloseToTray); }
            if (id == "starttray") { StartInTray = !StartInTray; SaveApp("start_in_tray", StartInTray); }
            if (id.StartsWith("anim:") && PowerAnims.Known(id.Substring(5))) { PowerAnimName = id.Substring(5); SaveApp("power_anim", PowerAnimName); }
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
        int updateState;             // 0 not checked, 1 checking, 2 up to date, 3 newer found, 4 failed, 5 no release page yet,
                                     // 6 downloading the installer, 7 download failed (Update.cs)
        string updateLatest, updatePage;
        // the update check at start (owner, 27.10.2026: on by default, a switch in General; before, only on the key)
        public static bool UpdateAtStart = !(AppSetting("update_check") is bool) || (bool)AppSetting("update_check");
        public void DemoUpdate(string version) { updateLatest = version; updatePage = ReleasesPage; updateState = 3; }
        public void DemoUpdateState(int state, int percent = 0) { updateState = state; updatePercent = percent; }   // selftest
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
                case 3: return L.F("Update to {0}", updateLatest);
                case 6: return L.F("Downloading… {0} %", updatePercent);
                case 7: return L.T("Download failed");
                case 4: return L.T("Check failed");
                case 5: return L.T("No release page yet");
                default: return L.T("Check for updates");
            }
        }

        // quiet (the check at start): nothing shows unless a newer version is found
        void CheckForUpdates(bool quiet)
        {
            if (updateState == 1 || updateState == 3 || updateState == 6) return;
            if (ReleasesApi == "") { if (!quiet) updateState = 5; return; }
            if (!quiet) updateState = 1;
            new System.Threading.Thread(() =>
            {
                int st; string latest = null, html = null;
                try
                {
                    System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
                    // the release page, not the API: github.com/.../releases/latest answers with a redirect to
                    // .../releases/tag/v<version>. The API allows 60 requests an hour per IP without a token and then
                    // answers 403, so behind a shared or busy address the check failed (owner, 04.11.2026)
                    var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(ReleasesPage);
                    req.Method = "HEAD"; req.AllowAutoRedirect = false; req.UserAgent = "TheNorns"; req.Timeout = 15000;
                    using (var resp = (System.Net.HttpWebResponse)req.GetResponse())
                    {
                        var loc = resp.Headers["Location"];
                        int at = loc == null ? -1 : loc.LastIndexOf("/tag/", StringComparison.Ordinal);
                        if (at >= 0) { latest = Uri.UnescapeDataString(loc.Substring(at + 5)).TrimStart('v'); html = loc; }
                    }
                    // the API as a fallback when the page did not redirect
                    if (latest == null)
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
            var hf = NornsFonts.Screen(U(7)); var bf = NornsFonts.Body(U(7.2f));
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
                    var f = TextFont(t, U(7.2f)) ?? bf;
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
            if (fleet != null && !Compact && PowerRect.Contains(e.Location)) { fleet.Wheel(e.Delta); return; }   // the client module: the wheel flips clients
            if (ScrollEveO(e.Location, e.Delta)) { Render(); return; }   // Settings -> Carousel scrolls (EveO.cs)
            if (ScrollStats(e.Location, e.Delta)) { Render(); return; }  // the Statistics list (Statistics.cs)
            if (settingsOpen && tabRow.Contains(e.Location)) { ScrollTabs(-Math.Sign(e.Delta)); Render(); return; }   // the wheel over the tabs scrolls them
            if (!settingsOpen || page != "about" || aboutView != "news") return;
            newsScroll = Math.Max(0, Math.Min(Math.Max(0, newsHeight - newsBox), newsScroll - Math.Sign(e.Delta) * U(24)));
            Render();
        }

        static string CreditsText()
        {
            return L.T("Fonts") + ": Orbitron, Noto Sans, Noto Sans SC (SIL OFL 1.1); Zarame Gothic by _mokuzai_ (SIL OFL 1.1) — ノルン.\n" +
                   L.T("Animation") + ": loadersz \"working\" by lumberjacque (MIT).\n" +
                   L.T("Glass texture") + ": Light Scratches by Poliigon (poliigon.com).\n" +
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
            var f = TextFont(label, U(7));
            var sz = g.MeasureString(label, f, 1000, StringFormat.GenericTypographic);
            using (var b = new SolidBrush(on ? BodyTheme.Accent : BodyTheme.Ink))
                g.DrawString(label, f, b, new RectangleF(r.Left + Math.Max(U(3), (r.Width - sz.Width) / 2), r.Top + (r.Height - sz.Height) / 2, r.Width - U(6), sz.Height + U(1)),
                    new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            AddKey(id, r);
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

        // Ukrainian titles: Orbitron has no Cyrillic (Loc.cs)
        static readonly Dictionary<string, Font> sysFonts = new Dictionary<string, Font>();
        static Font Sys(string family, float px, FontStyle style = FontStyle.Regular)
        {
            var key = family + "|" + px + "|" + style; Font f;
            if (!sysFonts.TryGetValue(key, out f)) sysFonts[key] = f = new Font(family, px, style, GraphicsUnit.Pixel);
            return f;
        }
        internal static Font TitleFont(string text, float px)
        {
            return L.Cjk(text) ? NornsFonts.Sc(px * NornsFonts.MatchSc(FontStyle.Bold, true), FontStyle.Bold)
                : L.Cyrillic(text) ? Sys("Segoe UI Semibold", px * NornsFonts.Match("Segoe UI Semibold", FontStyle.Regular, true)) : NornsFonts.Screen(px);
        }
        internal static Font TextFont(string text, float px)
        {
            return L.Cjk(text) ? NornsFonts.Sc(px * NornsFonts.MatchSc(FontStyle.Regular, false))
                : NornsFonts.Body(px);   // Noto Sans has Cyrillic
        }

        void PaintSettings(Graphics g, RectangleF c)
        {
            spots.Clear(); keyIds.Clear(); keyFaces.Clear(); bars.Clear();
            var line = new Action<float>(ly => { using (var b = new SolidBrush(Color.FromArgb(26, BodyTheme.Ink))) g.FillRectangle(b, c.Left, ly, c.Width, Math.Max(1f, U(0.5f))); });

            // header: the settings symbol, the title, the back key
            var title = L.T("Settings");
            var tf = TitleFont(title, U(9.28f));
            float th = tf.GetHeight(g);
            var ir = new RectangleF(c.Left, c.Top + (th - U(10)) / 2, U(10), U(10));
            using (var ab = new SolidBrush(BodyTheme.Accent)) MaterialIcon.Draw(g, MaterialIcon.Settings, ir, ab);
            Glow(g, title, tf, BodyTheme.Ink, ir.Right + U(5), c.Top);
            var back = new RectangleF(c.Right - U(11), c.Top - U(2), U(11), U(15));
            CloseKey(g, back, hotSpot == "back", downSpot == "back", U(1) * 0.8f);
            AddKey("back", RectangleF.Inflate(back, U(2), U(2)));
            // Statistics and About: square keys left of the close key, lit while their page is open (Material bar_chart, info)
            for (int hk = 0; hk < 2; hk++)
            {
                string hp = hk == 0 ? "about" : "stats", hid = "page:" + hp;
                // 2 px further from the close key (owner, 31.10.2026): 6.6 u at the 125 % size
                var kr = new RectangleF(back.Left - U(6.6f) - (hk + 1) * U(15) - hk * U(3), c.Top - U(2), U(15), U(15));
                SquareKey(g, kr, hk == 0 ? MaterialIcon.Info : MaterialIcon.BarChart, false, hid, page == hp);
            }
            float y = c.Top + Math.Max(th, U(13)) + U(6);
            line(y); y += U(7);

            // the pages: the two arrow keys side by side on the left (Material arrow_left / arrow_right, dim when there is
            // nothing more that way), then the tabs, the open one's label in the accent (owner, 28.10.2026); the one cut at
            // the right edge fades into the black, so more can be seen to be there (owner, 29.10.2026)
            float ak = U(14), ah = U(13), ay = y - U(1.5f);
            var ups = new string[Pages.Length]; var fonts = new Font[Pages.Length]; var lw = new float[Pages.Length];
            for (int k = 0; k < Pages.Length; k++)
            {
                var label = L.T(Pages[k][1]);
                fonts[k] = TitleFont(label, U(6.8f));
                ups[k] = label.ToUpperInvariant();
                lw[k] = g.MeasureString(ups[k], fonts[k], 1000, StringFormat.GenericTypographic).Width;
            }
            // all the tabs fit the row: no arrow keys, the row from the left edge
            bool arrows = lw.Sum() + (Pages.Length - 1) * U(5) > c.Width;
            float rowLeft = arrows ? c.Left + 2 * ak + U(3) + U(4) : c.Left;
            tabRow = RectangleF.FromLTRB(rowLeft, y - U(2), c.Right, y + U(12));
            bool firstRow = tabX == null;
            tabGap = Math.Max(U(5), 0.6f * (tabRow.Width / TabsShown - lw.Average()));
            tabX = new float[Pages.Length + 1];
            for (int k = 0; k < Pages.Length; k++) tabX[k + 1] = tabX[k] + lw[k] + tabGap;
            if (firstRow) { ShowTab(page); tabShift = tabFirst; }
            tabFirst = Math.Min(tabFirst, TabsMaxFirst());
            for (int a = 0; arrows && a < 2; a++)
            {
                string id = a == 0 ? "tabs:prev" : "tabs:next";
                bool can = a == 0 ? tabFirst > 0 : tabFirst < TabsMaxFirst(), hot = can && hotSpot == id;
                var kr = new RectangleF(c.Left + a * (ak + U(3)), ay, ak, ah);
                using (var p = Round(kr, 5))
                {
                    using (var b = new SolidBrush(hot ? Color.FromArgb(26, BodyTheme.Ink) : Color.FromArgb(12, BodyTheme.Ink))) g.FillPath(b, p);
                    using (var pen = new Pen(Color.FromArgb(can ? 50 : 22, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
                }
                float ic = U(10);
                using (var b = new SolidBrush(can ? BodyTheme.Ink : Color.FromArgb(60, BodyTheme.Ink)))
                    MaterialIcon.Draw(g, a == 0 ? MaterialIcon.ArrowLeft : MaterialIcon.ArrowRight, new RectangleF(kr.Left + (kr.Width - ic) / 2, kr.Top + (kr.Height - ic) / 2, ic, ic), b);
                if (can) AddKey(id, kr); else keyFaces.Add(kr);
            }
            tabShift = Potato || SelfTesting && !DemoLive ? tabFirst : tabShift + (tabFirst - tabShift) * 0.35f;
            if (Math.Abs(tabFirst - tabShift) < 0.01f) tabShift = tabFirst; else Invalidate();
            var tabClip = g.Save();
            g.SetClip(tabRow);
            for (int k = 0; k < Pages.Length; k++)
            {
                string id = "page:" + Pages[k][0], up = ups[k];
                bool on = page == Pages[k][0], hot = hotSpot == id;
                var f = fonts[k];
                var tr = new RectangleF(rowLeft + tabX[k] - TabOffset(tabShift), y - U(2), lw[k] + tabGap - U(3), U(14));
                if (tr.Right <= tabRow.Left || tr.Left >= tabRow.Right) continue;
                if (hot && !on) using (var p = Round(tr, 5)) using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillPath(b, p);
                float w = Math.Min(tabRow.Width - U(4), lw[k]);
                var ink = on ? BodyTheme.Accent : hot ? Color.FromArgb(200, 203, 210) : Color.FromArgb(130, 133, 140);
                var lr = new RectangleF(tr.Left + (tr.Width - w) / 2, tr.Top + U(2.5f), w + U(1), f.GetHeight(g) + U(1));
                // cut at the right edge: the label from its colour to the black at the edge, the fade 10% longer than the part shown
                Brush lb = lr.Right > tabRow.Right && lr.Left < tabRow.Right - U(2)
                    ? (Brush)new LinearGradientBrush(new PointF(tabRow.Right - 1.1f * (tabRow.Right - lr.Left) - 1, 0), new PointF(tabRow.Right, 0), ink, Color.FromArgb(0, ink))
                    : new SolidBrush(ink);
                using (lb)
                    g.DrawString(up, f, lb, lr,
                        new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                var hit = RectangleF.Intersect(tr, tabRow);
                if (hit.Width > tr.Width / 2) spots.Add(Tuple.Create(id, hit));
            }
            g.Restore(tabClip);
            y += U(16);
            line(y); y += U(8);

            if (page == "general")
            {
                // language: the seven, four columns of two (owner, 28.10.2026: three columns; 29.10.2026: four, for
                // Chinese), a lamp lit at the one in use
                var lt = L.T("Language");
                Glow(g, lt, TitleFont(lt, U(7.2f)), Muted, c.Left, y);
                y += U(11);
                float colW = c.Width / 4, rowH = U(16);
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
                    var nf = TextFont(name, U(7.5f));
                    using (var b = new SolidBrush(cur || hot ? BodyTheme.Ink : Color.FromArgb(165, 168, 176)))
                        g.DrawString(name, nf, b, lx + d + U(6), ly - nf.GetHeight(g) / 2, StringFormat.GenericTypographic);
                    spots.Add(Tuple.Create(id, row));
                }
                y += 2 * rowH + U(6);

                var updatesT = L.T("Updates");
                Glow(g, updatesT, TitleFont(updatesT, U(7.2f)), Muted, c.Left, y);
                y += U(11);
                LampRow(g, c, y, "updatestart", UpdateAtStart, L.T("Check for updates at start"));
                y += U(19) - 3;
                y += U(6);
                var alertsT = L.T("Alerts");
                Glow(g, alertsT, TitleFont(alertsT, U(7.2f)), Muted, c.Left, y);
                y += U(11);
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
                AddKey("playalert", pk);
                y += U(19) - 3;
                y += U(6);
                // the tray icon (Tray.cs; owner, 04.11.2026): the close key hides to it; the app starts hidden in it
                var trayT = L.T("Tray");
                Glow(g, trayT, TitleFont(trayT, U(7.2f)), Muted, c.Left, y);
                y += U(11);
                LampRow(g, c, y, "closetray", CloseToTray, L.T("Close to tray"));
                y += U(19) - 3;
                LampRow(g, c, y, "starttray", StartInTray, L.T("Start in the tray"));
                y += U(19) - 3;
            }
            else if (page == "display")
            {
                // window: the body's size and whether it comes back where it was
                var wt = L.T("Window");
                Glow(g, wt, TitleFont(wt, U(7.2f)), Muted, c.Left, y);
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
                    var lf = NornsFonts.Screen(U(6.5f));
                    var ls = g.MeasureString(lab, lf, 1000, StringFormat.GenericTypographic);
                    using (var b = new SolidBrush(cur ? BodyTheme.Accent : BodyTheme.Ink)) g.DrawString(lab, lf, b, br.Left + (br.Width - ls.Width) / 2, br.Top + (br.Height - ls.Height) / 2, StringFormat.GenericTypographic);
                    AddKey(id, br);
                }
                y += U(20);
                LampRow(g, c, y, "remember", RememberPos, L.T("Remember window position"));
                y += U(19) - 3;
                LampRow(g, c, y, "compact", Compact, L.T("Compact mode"));
                y += U(19) - 3;
                LampRow(g, c, y, "potato", Potato, L.T("Potato mode: low CPU, no effects"));
                y += U(19) - 3;
                y += U(4);
                // the power display's animation: a live preview of each in one row, about the display's own size; the chosen one
                // framed in the accent (owner, 04.11.2026)
                var at = L.T("Power display");
                Glow(g, at, TitleFont(at, U(7.2f)), Muted, c.Left, y);
                y += U(11);
                int cols = PowerAnims.Names.Length; float gap = U(5), tileW = (c.Width - (cols - 1) * gap) / cols, tileH = tileW * 28 / 53, lh = U(10);
                for (int i = 0; i < PowerAnims.Names.Length; i++)
                {
                    string an = PowerAnims.Names[i], id = "anim:" + an;
                    var tile = new RectangleF(c.Left + (i % cols) * (tileW + gap), y + (i / cols) * (tileH + lh + U(4)), tileW, tileH);
                    bool cur = PowerAnimName == an, hot = hotSpot == id;
                    using (var p = Round(tile, 5))
                    {
                        using (var b = new SolidBrush(Color.FromArgb(4, 4, 5))) g.FillPath(b, p);
                        var st2 = g.Save(); g.SetClip(p);
                        PowerAnims.Draw(an, "tile", g, tile, Potato ? 0 : Clock, BodyTheme.Accent);
                        g.Restore(st2);
                        using (var pen = new Pen(cur ? BodyTheme.Accent : hot ? Color.FromArgb(120, BodyTheme.Ink) : Color.FromArgb(40, BodyTheme.Ink), cur ? 1.5f : 1f)) g.DrawPath(pen, p);
                    }
                    var name = L.T(PowerAnims.Titles[i]);
                    var nf2 = TextFont(name, U(6.6f));
                    var ns = g.MeasureString(name, nf2, 1000, StringFormat.GenericTypographic);
                    using (var b = new SolidBrush(cur ? BodyTheme.Accent : hot ? BodyTheme.Ink : Color.FromArgb(165, 168, 176)))
                        g.DrawString(name, nf2, b, tile.Left + (tile.Width - ns.Width) / 2, tile.Bottom + U(2), StringFormat.GenericTypographic);
                    AddKey(id, RectangleF.FromLTRB(tile.Left, tile.Top, tile.Right, tile.Bottom + lh));
                }
                y += tileH + lh + U(4);
            }
            else if (page == "modes") PaintModes(g, c, y);
            else if (page == "eveo") PaintEveO(g, c, y);
            else if (page == "data")
            {
                // the combat log folder: its path (from Documents on: no user name on screen), a mark whether logs are
                // there, Select / Detect / Open; then the app's own folder with its size, Open, and the anomaly reset
                bool resetShown = demoResetShown || Clock < resetShownUntil;
                var lt = L.T("Combat log folder");
                Glow(g, lt, TitleFont(lt, U(7.2f)), Muted, c.Left, y);
                y += U(11);
                var dir = GameLogTail.Dir; var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).TrimEnd('\\');
                bool ok = false;
                try { ok = Directory.Exists(dir) && Directory.EnumerateFiles(dir, "*.txt").Any(); } catch { }
                if (DemoLogDirOk != null) ok = DemoLogDirOk.Value;
                if (dir.StartsWith(docs + "\\", StringComparison.OrdinalIgnoreCase)) dir = "Documents" + dir.Substring(docs.Length);
                var field = new RectangleF(c.Left, y, c.Width - U(16), U(17));
                using (var p = Round(field, 5)) using (var b = new SolidBrush(Color.FromArgb(16, 17, 20))) using (var pen = new Pen(Color.FromArgb(40, BodyTheme.Ink), 1f)) { g.FillPath(b, p); g.DrawPath(pen, p); }
                var df = TextFont(dir, U(7.2f));   // a user folder may be named in Cyrillic or Chinese
                using (var b = new SolidBrush(Color.FromArgb(200, 203, 210)))
                    g.DrawString(dir, df, b, new RectangleF(field.Left + U(5), field.Top + (field.Height - df.GetHeight(g)) / 2, field.Width - U(10), df.GetHeight(g) + U(1)),
                        new StringFormat { Trimming = StringTrimming.EllipsisPath, FormatFlags = StringFormatFlags.NoWrap });
                var mr = new RectangleF(field.Right + U(4), field.Top + (field.Height - U(11)) / 2, U(11), U(11));
                using (var b = new SolidBrush(ok ? BodyTheme.Accent : AlarmRed)) MaterialIcon.Draw(g, ok ? MaterialIcon.Check : MaterialIcon.Close, mr, b);
                y += U(21);
                float tw3 = (c.Width - 2 * U(4)) / 3;
                PaintWideKey(g, new RectangleF(c.Left, y, tw3, U(19)), MaterialIcon.Folder, L.T("Select"), "dir:select", false);
                PaintWideKey(g, new RectangleF(c.Left + tw3 + U(4), y, tw3, U(19)), MaterialIcon.Search, L.T("Detect"), "dir:detect", false);
                PaintWideKey(g, new RectangleF(c.Left + 2 * (tw3 + U(4)), y, tw3, U(19)), MaterialIcon.Description, L.T("Open"), "logs", false);
                y += U(23);
                var note = ok ? L.T("Combat logs found.") : L.T("No combat logs here yet. They appear once a pilot is in space; or pick the folder.");
                using (var b = new SolidBrush(ok ? Color.FromArgb(120, BodyTheme.Ink) : Color.FromArgb(220, 255, 120, 110)))
                    g.DrawString(note, TextFont(note, U(7.2f)), b, new RectangleF(c.Left, y, c.Width, U(20)));
                y += U(18);
                if (storage == null) storage = ReadStorage();
                var dt = L.T("App data");
                Glow(g, dt, TitleFont(dt, U(7.2f)), Muted, c.Left, y);
                var total = storage.FirstOrDefault(r => r.Item3);
                if (total != null)
                {
                    var nf3 = NornsFonts.Screen(U(6.5f));
                    using (var b = new SolidBrush(BodyTheme.Accent)) g.DrawString(total.Item2, nf3, b, c.Right - g.MeasureString(total.Item2, nf3, 1000, StringFormat.GenericTypographic).Width, y, StringFormat.GenericTypographic);
                }
                y += U(11);
                var pf2 = TextFont(AppInfo.DataDir, U(7.2f));
                using (var b = new SolidBrush(Color.FromArgb(150, BodyTheme.Ink)))
                    g.DrawString(Shown(AppInfo.DataDir), pf2, b, new RectangleF(c.Left, y, c.Width, pf2.GetHeight(g) + U(1)), new StringFormat { Trimming = StringTrimming.EllipsisPath, FormatFlags = StringFormatFlags.NoWrap });
                y += U(13);
                PaintWideKey(g, new RectangleF(c.Left, y, c.Width, U(19)), MaterialIcon.Folder, L.T("Open app data folder"), "data", false);
                y += U(23);
                PaintWideKey(g, new RectangleF(c.Left, y, c.Width, U(19)), MaterialIcon.Refresh, resetShown ? L.T("Anomaly counter reset") : L.T("Reset anomaly counter"), "reset", resetShown);
            }
            else if (page == "stats") PaintStats(g, c, y, line);
            else
            {
                // about: name and version, the update check, then one of: what's new / credits / legal (keys at the bottom)
                var nm = AppInfo.Name.ToUpperInvariant();
                Glow(g, nm, NornsFonts.Screen(U(9.6f)), BodyTheme.Ink, c.Left, y);
                var vt = AppInfo.VersionText.ToUpperInvariant() + "  ·  MIT";
                using (var b = new SolidBrush(BodyTheme.Accent)) g.DrawString(vt, NornsFonts.Body(U(7.2f)), b, c.Left, y + U(14), StringFormat.GenericTypographic);
                // the update check: only when pressed (nothing goes to the network otherwise)
                var uk = new RectangleF(c.Right - U(118), y, U(118), U(19));
                PaintWideKey(g, uk, updateState == 3 || updateState == 6 ? MaterialIcon.Download : MaterialIcon.Refresh, UpdateLabel(), "update", updateState == 2 || updateState == 3 || updateState == 6);
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
                    var f = TextFont(text, U(7.2f));
                    using (var b = new SolidBrush(Color.FromArgb(200, 203, 210))) g.DrawString(text, f, b, box);
                }
                g.Restore(st2);
            }

        }

        // the Material star and a count after it (the spawns of a session), ending at `right`

        // a full-width key on the screen: a dark key face, the icon and the label; lit = the action just happened
        void PaintWideKey(Graphics g, RectangleF r, string icon, string label, string id, bool lit)
        {
            WideKey(g, r, icon, label, lit, hotSpot == id, downSpot == id);
            AddKey(id, r);
        }
        // the same key drawn anywhere (the calendar card)
        internal static void WideKey(Graphics g, RectangleF r, string icon, string label, bool lit, bool hot, bool down)
        {
            Func<float, float> U = v => v * Theme.K * UiScale;
            using (var rim = Theme.Round(r, U(5 * 0.6f), 0.6f)) using (var b = new SolidBrush(BodyTheme.KeyRim)) g.FillPath(b, rim);
            var face = RectangleF.Inflate(r, -U(1), -U(1));
            if (down) face.Offset(0, U(0.5f));
            using (var fp = Theme.Round(face, U(4.5f * 0.6f), 0.6f))
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
            if (icon != null) using (var ib = new SolidBrush(col)) MaterialIcon.Draw(g, icon, ir, ib);
            var f = TextFont(label, U(7.5f));
            float tl = icon != null ? ir.Right + U(6) : face.Left + U(4), tw = icon != null ? face.Right - ir.Right - U(12) : face.Width - U(8);
            using (var tb = new SolidBrush(lit ? BodyTheme.Accent : hot ? BodyTheme.Ink : Color.FromArgb(200, 203, 210)))
                g.DrawString(label, f, tb, new RectangleF(tl, face.Top + (face.Height - f.GetHeight(g)) / 2, tw, f.GetHeight(g) + U(1)),
                    new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, Alignment = icon != null ? StringAlignment.Near : StringAlignment.Center });
        }

        // a square key with an icon (the settings' header, the calendar on Statistics); on: lit in the accent
        void SquareKey(Graphics g, RectangleF kr, string icon, bool symbol, string id, bool on)
        {
            bool hot = hotSpot == id;
            using (var p = Round(kr, 5))
            {
                using (var b = new SolidBrush(on ? Color.FromArgb(40, BodyTheme.Accent) : hot ? Color.FromArgb(26, BodyTheme.Ink) : Color.FromArgb(12, BodyTheme.Ink))) g.FillPath(b, p);
                using (var pen = new Pen(on ? BodyTheme.Accent : Color.FromArgb(50, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
            }
            float hic = U(9);
            var ir = new RectangleF(kr.Left + (kr.Width - hic) / 2, kr.Top + (kr.Height - hic) / 2, hic, hic);
            using (var b = new SolidBrush(on ? BodyTheme.Accent : BodyTheme.Ink)) { if (symbol) MaterialIcon.DrawSymbol(g, icon, ir, b); else MaterialIcon.Draw(g, icon, ir, b); }
            AddKey(id, kr);
        }
    }
}
