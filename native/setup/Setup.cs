// Setup.cs - TheNorns-Setup.exe: per-user installer / uninstaller, drawn as a piece of the app's hardware (the body of
// Body.cs: steel-blue hull, ノルン and the lamp on the top strip, the pages on a recessed OLED screen, keys at the bottom).
// Built by build.ps1 -Setup from the app's sources plus this file (/main:TheNorns.SetupProgram); the files to install
// are an embedded zip (TheNorns.payload.zip: TheNorns.exe, LICENSE, README.md, CHANGELOG.md, licenses\).
//   * install: %LOCALAPPDATA%\Programs\The Norns by default, no administrator rights; desktop / Start menu shortcuts;
//     an "Apps & features" entry (HKCU ...\Uninstall\The Norns); Uninstall.exe = a copy of this exe; install.txt = the
//     files it put there (the uninstaller removes exactly those). An install of the app under its earlier name is
//     removed (its files, shortcuts and entry; its settings move to the new data folder on the app's first start).
//   * uninstall (Uninstall.exe /uninstall): runs from a temp copy, removes the files, shortcuts and the entry; the
//     user's data in %LOCALAPPDATA%\The Norns only when asked.
// The user's settings are never touched by an update.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TheNorns
{
    static class SetupProgram
    {
        public const string Exe = "TheNorns.exe";
        const string Uninstalls = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\";
        public static string UninstallKey = Uninstalls + AppInfo.Name;
        public static string DefaultDir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", AppInfo.Name); } }

        [DllImport("shcore.dll")] static extern int SetProcessDpiAwareness(int value);

        [STAThread]
        static void Main(string[] args)
        {
            try { SetProcessDpiAwareness(2); } catch { }
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            Application.EnableVisualStyles();
            // the installer speaks the Windows language when the app has it (Loc.cs); --lang <code> for the selftest
            int lg = Array.IndexOf(args, "--lang");
            L.Init(lg >= 0 && args.Length > lg + 1 ? args[lg + 1] : L.SystemDefault()); L.AutoFit();
            // --selftest <dir>: every page to PNG, then a real install + uninstall into <dir>\install (no shortcuts, a
            // separate Apps entry, an earlier install left alone) and a report - the release check
            int st = Array.IndexOf(args, "--selftest");
            if (st >= 0 && args.Length > st + 1) { using (var f = new SetupForm(false, null)) f.SelfTest(args[st + 1]); return; }
            int u = Array.FindIndex(args, a => a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase));
            // Uninstall.exe started by hand (no arguments): its name alone means uninstall
            bool asUninstaller = Path.GetFileName(Application.ExecutablePath).Equals("Uninstall.exe", StringComparison.OrdinalIgnoreCase);
            if (u < 0 && asUninstaller) { args = new[] { "/uninstall" }; u = 0; }
            if (u >= 0)
            {
                var dir = args.Length > u + 1 ? args[u + 1] : Path.GetDirectoryName(Application.ExecutablePath);
                // an uninstaller can't delete itself: the copy inside the install folder hands over to a temp copy
                if (Path.GetFullPath(Path.GetDirectoryName(Application.ExecutablePath)).TrimEnd('\\').Equals(Path.GetFullPath(dir).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    var tmp = Path.Combine(Path.GetTempPath(), "TheNorns-Uninstall-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".exe");
                    File.Copy(Application.ExecutablePath, tmp, true);
                    // working folder outside the install: a process whose current folder is in it keeps the folder from being deleted
                    Process.Start(new ProcessStartInfo(tmp, "/uninstall \"" + dir + "\"") { UseShellExecute = false, WorkingDirectory = Path.GetTempPath() });
                    return;
                }
                try { Environment.CurrentDirectory = Path.GetTempPath(); } catch { }
                Application.Run(new SetupForm(true, dir));
                return;
            }
            Application.Run(new SetupForm(false, null));
        }

        public static ZipArchive Payload()
        {
            var s = typeof(SetupProgram).Assembly.GetManifestResourceStream("TheNorns.payload.zip");
            if (s == null) throw new InvalidOperationException("this setup has no files inside (built without the payload)");
            return new ZipArchive(s, ZipArchiveMode.Read);
        }

        public static string PayloadText(string name)
        {
            try { using (var z = Payload()) { var e = z.GetEntry(name); if (e == null) return ""; using (var r = new StreamReader(e.Open())) return r.ReadToEnd(); } }
            catch { return ""; }
        }

        // a .lnk via WScript.Shell
        public static void Shortcut(string lnk, string exe)
        {
            var t = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(t);
            try
            {
                object l = t.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { lnk });
                var lt = l.GetType();
                Action<string, object> set = (n, v) => lt.InvokeMember(n, System.Reflection.BindingFlags.SetProperty, null, l, new[] { v });
                set("TargetPath", exe);
                set("WorkingDirectory", Path.GetDirectoryName(exe));
                set("IconLocation", exe + ",0");
                set("Description", AppInfo.Title);
                lt.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, l, null);
                Marshal.FinalReleaseComObject(l);
            }
            finally { Marshal.FinalReleaseComObject(shell); }
        }
        public static string ShortcutTarget(string lnk)
        {
            var t = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(t);
            try
            {
                object l = t.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { lnk });
                var target = (string)l.GetType().InvokeMember("TargetPath", System.Reflection.BindingFlags.GetProperty, null, l, null);
                Marshal.FinalReleaseComObject(l);
                return target ?? "";
            }
            finally { Marshal.FinalReleaseComObject(shell); }
        }
        static string Lnk(Environment.SpecialFolder where, string name) { return Path.Combine(Environment.GetFolderPath(where), name + ".lnk"); }
        public static string DesktopLnk { get { return Lnk(Environment.SpecialFolder.DesktopDirectory, AppInfo.Name); } }
        public static string StartLnk { get { return Lnk(Environment.SpecialFolder.Programs, AppInfo.Name); } }

        // instances of the app (exe) started from the given folder
        public static List<Process> Running(string dir, string exe = Exe)
        {
            var res = new List<Process>();
            foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe)))
                try
                {
                    if (Path.GetDirectoryName(p.MainModule.FileName).TrimEnd('\\').Equals(Path.GetFullPath(dir).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) res.Add(p);
                    else p.Dispose();
                }
                catch { p.Dispose(); }
            return res;
        }

        // removes an install: exactly the files on its install.txt (else the exe and its uninstaller), the folders left
        // empty, the desktop / Start menu shortcuts named `name` that point into it, and its Apps entry
        public static void RemoveInstall(string dir, string exe, string name, string key, Action<string, float> report)
        {
            var listFile = Path.Combine(dir, "install.txt");
            var files = File.Exists(listFile) ? File.ReadAllLines(listFile).Where(l => l.Trim() != "").ToList() : new List<string>();
            if (files.Count == 0 && File.Exists(Path.Combine(dir, exe))) files = new List<string> { exe, "Uninstall.exe" };
            for (int i = 0; i < files.Count; i++)
            {
                report(files[i], 0.8f * i / Math.Max(1, files.Count));
                var f = Path.Combine(dir, files[i]);
                try { if (File.Exists(f)) File.Delete(f); } catch { }
                Thread.Sleep(15);
            }
            foreach (var d in Directory.Exists(dir) ? Directory.GetDirectories(dir, "*", SearchOption.AllDirectories).OrderByDescending(x => x.Length) : Enumerable.Empty<string>())
                try { if (!Directory.EnumerateFileSystemEntries(d).Any()) Directory.Delete(d); } catch { }
            try { if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any()) Directory.Delete(dir); } catch { }
            report(L.T("Shortcuts…"), 0.85f);
            // only shortcuts that point into this install
            foreach (var l in new[] { Lnk(Environment.SpecialFolder.DesktopDirectory, name), Lnk(Environment.SpecialFolder.Programs, name) })
                try
                {
                    if (File.Exists(l) && ShortcutTarget(l).StartsWith(Path.GetFullPath(dir).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
                        File.Delete(l);
                }
                catch { }
            report(L.T("Windows Settings → Apps…"), 0.9f);
            try { Registry.CurrentUser.DeleteSubKeyTree(key, false); } catch { }
        }
    }

    // ---------------- the installer's hardware: a key on the hull, a lamp switch on the screen ----------------

    // a key in a dark rim (the body's LOG / RST key) with its label printed on the face; Primary = an accent LED left of
    // the label; Close = the body's close key (pale grey, red under the mouse) with no label
    class KeyButton : Control
    {
        public bool Primary, CloseKey;
        bool hover, down;
        public KeyButton(string text)
        {
            Text = text; DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            Cursor = SetupForm.BluePointer;
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnEnabledChanged(EventArgs e) { hover = false; down = false; Invalidate(); base.OnEnabledChanged(e); }

        float K { get { return Theme.K; } }
        public int PreferredWidth()
        {
            using (var g = CreateGraphics())
            {
                // English width (Loc.cs); a short English label still leaves room for a translation
                float w = g.MeasureString(L.En(Text).ToUpperInvariant(), SetupForm.KeyFont, 1000, StringFormat.GenericTypographic).Width;
                if (L.Code != "en" && L.En(Text) != Text) w = Math.Max(w, g.MeasureString(Text.ToUpperInvariant(), SetupForm.KeyFont, 1000, StringFormat.GenericTypographic).Width);
                return Math.Max((int)(96 * K), (int)(w + (Primary ? 52 : 36) * K));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            SetupForm.PaintBehind(this, g);
            g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            var r = new RectangleF(1 * K, 1 * K, Width - 2 * K, Height - 2.5f * K);
            if (CloseKey) { PaintClose(g, r); return; }
            using (var rim = SetupForm.Round(r, 5)) using (var b = new SolidBrush(BodyTheme.KeyRim)) g.FillPath(b, rim);
            var face = RectangleF.Inflate(r, -1 * K, -1 * K);
            if (down) face.Offset(0, 0.6f * K);
            bool hv = hover && Enabled;
            using (var fp = SetupForm.Round(face, 4.5f))
            {
                Color top = hv ? Color.FromArgb(54, 56, 61) : BodyTheme.KeyTop, bottom = hv ? Color.FromArgb(32, 34, 38) : BodyTheme.KeyBottom;
                if (down) { top = Color.FromArgb(24, 25, 28); bottom = Color.FromArgb(34, 35, 39); }
                using (var lg = new LinearGradientBrush(face, top, bottom, 90f)) g.FillPath(lg, fp);
                if (!down)
                    using (var lg = new LinearGradientBrush(face, Color.FromArgb(34, 255, 255, 255), Color.FromArgb(115, 0, 0, 0), 90f))
                    using (var pen = new Pen(lg, 1f)) g.DrawPath(pen, fp);
            }
            var f = SetupForm.KeyFont;
            var label = Text.ToUpperInvariant();
            float tw = g.MeasureString(label, f, 1000, StringFormat.GenericTypographic).Width, th = f.GetHeight(g);
            float led = Primary ? 10 * K : 0;
            float x = face.Left + (face.Width - tw - led) / 2 + led, y = face.Top + (face.Height - th) / 2;
            if (Primary)
            {
                // the accent LED: lit while the key can be pressed
                var lr = new RectangleF(x - led, face.Top + face.Height / 2 - 2.5f * K, 5 * K, 5 * K);
                var on = Enabled;
                if (on) DpsForm.SoftGlow(g, new PointF(lr.X + lr.Width / 2, lr.Y + lr.Height / 2), 6 * K, BodyTheme.Accent, 120);
                using (var b = new SolidBrush(on ? BodyTheme.Accent : Color.FromArgb(58, 60, 30))) g.FillEllipse(b, lr);
            }
            var c = !Enabled ? Color.FromArgb(78, 80, 86) : hv ? BodyTheme.Ink : Primary ? Color.FromArgb(222, 224, 228) : Color.FromArgb(176, 179, 186);
            using (var b = new SolidBrush(c)) g.DrawString(label, f, b, x, y, StringFormat.GenericTypographic);
        }

        void PaintClose(Graphics g, RectangleF r)
        {
            using (var cp = SetupForm.Round(r, 5))
            using (var lg = new LinearGradientBrush(r, Color.FromArgb(44, 46, 49), Color.FromArgb(23, 24, 26), 90f))
            {
                g.FillPath(lg, cp);
                using (var pen = new Pen(Color.FromArgb(30, 255, 255, 255), 1f)) g.DrawPath(pen, cp);
            }
            var face = RectangleF.Inflate(r, -2 * K, -2 * K);
            if (down) face.Offset(0, 0.5f * K);
            var c = down ? Color.FromArgb(160, 14, 10) : hover && Enabled ? Color.FromArgb(232, 22, 14) : Enabled ? Color.FromArgb(150, 154, 162) : Color.FromArgb(90, 93, 100);
            using (var fp = SetupForm.Round(face, 3.5f))
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
    }

    // a switch on the screen: a small dark key with a lamp (accent when on) and its label in white
    class LampSwitch : Control
    {
        bool on, hover; public event EventHandler CheckedChanged;
        public bool Checked { get { return on; } set { if (on == value) return; on = value; Invalidate(); if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty); } }
        public LampSwitch(string text)
        {
            Text = text; DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            BackColor = Color.Black; Cursor = SetupForm.BluePointer;
        }
        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        public int PreferredWidth()
        {
            using (var g = CreateGraphics()) return (int)(g.MeasureString(Text, SetupForm.TextFont).Width + 40 * Theme.K);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; float k = Theme.K;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            var key = new RectangleF(1 * k, Height / 2f - 9 * k, 18 * k, 18 * k);
            using (var rp = SetupForm.Round(key, 4)) using (var b = new SolidBrush(BodyTheme.KeyRim)) g.FillPath(b, rp);
            var face = RectangleF.Inflate(key, -1 * k, -1 * k);
            using (var fp = SetupForm.Round(face, 3.5f))
            {
                using (var lg = new LinearGradientBrush(face, hover ? Color.FromArgb(54, 56, 61) : BodyTheme.KeyTop, hover ? Color.FromArgb(32, 34, 38) : BodyTheme.KeyBottom, 90f)) g.FillPath(lg, fp);
                using (var lg = new LinearGradientBrush(face, Color.FromArgb(34, 255, 255, 255), Color.FromArgb(115, 0, 0, 0), 90f)) using (var pen = new Pen(lg, 1f)) g.DrawPath(pen, fp);
            }
            var c = new PointF(face.Left + face.Width / 2, face.Top + face.Height / 2); float d = 6.5f * k;
            if (on) DpsForm.SoftGlow(g, c, d / 2 + 5 * k, BodyTheme.Accent, 130);
            using (var b = new SolidBrush(on ? BodyTheme.Accent : Color.FromArgb(52, 54, 34))) g.FillEllipse(b, c.X - d / 2, c.Y - d / 2, d, d);
            var f = SetupForm.TextFont;
            float tx = key.Right + 10 * k;
            L.SetTip(this, L.Trimmed(g, Text, f, Width - tx) ? Text : null);
            using (var tb = new SolidBrush(hover ? BodyTheme.Ink : Color.FromArgb(214, 216, 221)))
                g.DrawString(Text, f, tb, new RectangleF(tx, 0, Width - tx, Height), new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
        }
    }

    // text on the screen, drawn with GDI+ (a WinForms Label falls back to a system font for the embedded ones);
    // lines split by "\n" are paragraphs with a small gap between them
    class ScreenText : Control
    {
        public float ParaGap;
        public Font Face;   // the embedded fonts are kept here: WinForms may swap a Control.Font of a private family for a system one
        public ScreenText() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true); BackColor = Color.Black; }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            float y = 0;
            using (var b = new SolidBrush(ForeColor))
                foreach (var para in (Text ?? "").Split('\n'))
                {
                    var f = Face ?? Font;
                    var sz = g.MeasureString(para, f, Width);
                    g.DrawString(para, f, b, new RectangleF(0, y, Width, Height - y));
                    y += sz.Height + ParaGap;
                }
        }
    }

    // the progress page: the percentage, the bar in the accent and the current file (no animation, owner 27.10.2026)
    class ProgressPaint : Control
    {
        readonly Func<float> value; readonly Func<string> text;
        public ProgressPaint(Func<float> value, Func<string> text)
        {
            this.value = value; this.text = text;
            DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); BackColor = Color.Black;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; float k = Theme.K;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            float mid = Height / 2f, w = Width;
            float v = Math.Max(0, Math.Min(1, value()));
            var pf = SetupForm.HeadFont(22);
            using (var b = new SolidBrush(BodyTheme.Ink)) g.DrawString(((int)Math.Round(v * 100)).ToString() + " %", pf, b, 0, mid - 40 * k, StringFormat.GenericTypographic);
            var track = new RectangleF(0, mid - 3 * k, w, 6 * k);
            using (var p = Theme.Round(track, 3 * k, 0)) using (var b = new SolidBrush(Color.FromArgb(28, 29, 33))) g.FillPath(b, p);
            if (v > 0) using (var p = Theme.Round(new RectangleF(track.X, track.Y, Math.Max(6 * k, track.Width * v), track.Height), 3 * k, 0)) using (var b = new SolidBrush(BodyTheme.Accent)) g.FillPath(b, p);
            using (var b = new SolidBrush(Color.FromArgb(140, 143, 150)))
                g.DrawString(text(), SetupForm.SmallFont, b, new RectangleF(0, mid + 14 * k, w, 20 * k), new StringFormat { Trimming = StringTrimming.EllipsisPath, FormatFlags = StringFormatFlags.NoWrap });
        }
    }

    class SetupForm : Form
    {
        readonly bool uninstall; readonly string uninstallDir;
        readonly ScreenText heading = new ScreenText(), hint = new ScreenText();
        readonly Panel page = new Panel { BackColor = Color.Black };
        readonly KeyButton back = new KeyButton(L.T("Back")), next = new KeyButton(L.T("Next")) { Primary = true }, cancel = new KeyButton(L.T("Cancel")),
                           close = new KeyButton("") { CloseKey = true };
        readonly LampSwitch accept = new LampSwitch(L.T("I accept the license")), desktop = new LampSwitch(L.T("Desktop shortcut")) { Checked = true },
                            start = new LampSwitch(L.T("Start menu shortcut")) { Checked = true },
                            // the update check at start (Settings -> General in the app): set here too, with the privacy
                            // note beside it (SignPath's rule for software that sends anything: shown during installation,
                            // with a way to turn it off); an update keeps the choice made before
                            updates = new LampSwitch(L.T("Check for updates at start")) { Checked = UpdateCheckBefore() }, launch = new LampSwitch(L.F("Launch {0}", AppInfo.Name)) { Checked = true },
                            wipe = new LampSwitch(L.T("Also delete my settings"));
        readonly DarkField folder = new DarkField();
        int step; volatile bool busy; float progress; string status = ""; bool done, failed;
        readonly System.Windows.Forms.Timer anim = new System.Windows.Forms.Timer { Interval = 40 };
        readonly Stopwatch clock = Stopwatch.StartNew();
        double animSecs = 1.3;
        // the bar follows the real progress at most 1 / BarSecs per second (owner 27.10.2026: an instant bar looked like a
        // bug); the done page comes once it has reached the end
        const double BarSecs = 1.3;
        float shown; volatile bool finished; double lastTick = -1;
        Bitmap shell;
        float nameRight, nameMid;

        static float K { get { return Theme.K; } }
        static int P(float v) { return (int)(v * K); }
        // corners as on the body (Body.cs Round: the sketch's radius x 0.6, smoothing 0.6)
        public static GraphicsPath Round(RectangleF r, float logicalRadius) { return Theme.Round(r, logicalRadius * 0.6f * 1.25f * K, 0.6f); }

        // type: Orbitron caps on the screen's headings, Space Grotesk for the rest; Segoe UI where those have no glyphs
        // (Ukrainian - neither font has Cyrillic)
        static bool Latin { get { return L.Code != "uk"; } }
        public static Font HeadFont(float px) { return Latin ? NornsFonts.Screen(px * K) : Cached("Segoe UI Semibold", px * 0.92f * K); }
        public static Font TextFont { get { return Latin ? NornsFonts.Body(13f * K) : Cached("Segoe UI", 12.5f * K); } }
        public static Font SmallFont { get { return Latin ? NornsFonts.Body(11.5f * K) : Cached("Segoe UI", 11f * K); } }
        public static Font KeyFont { get { return Latin ? NornsFonts.Body(11.5f * K) : Cached("Segoe UI Semibold", 10.5f * K); } }
        static readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>();
        static Font Cached(string family, float px)
        {
            var key = family + "|" + px; Font f;
            if (!fonts.TryGetValue(key, out f)) fonts[key] = f = new Font(family, px, GraphicsUnit.Pixel);
            return f;
        }

        static Cursor pointer;
        public static Cursor BluePointer { get { if (pointer == null) try { pointer = Glass.MakePointer(BodyTheme.Pointer, BodyTheme.PointerOutline); } catch { pointer = Cursors.Default; } return pointer; } }

        // layout (logical px x K): the top strip, the screen (cavity, bezel, glass), the key row, the printed band
        const float W0 = 660, H0 = 524, Strip = 46, Side = 16, KeysH = 36, Band = 22;
        RectangleF CavityRect { get { return new RectangleF(P(Side), P(Strip), ClientSize.Width - 2 * P(Side), ClientSize.Height - P(Strip) - P(KeysH) - P(Band) - P(22)); } }
        RectangleF BezelRect { get { var r = CavityRect; r.Inflate(-P(3), -P(3)); return r; } }
        RectangleF GlassRect { get { var r = CavityRect; r.Inflate(-P(7), -P(7)); return r; } }
        Rectangle ScreenArea { get { var r = GlassRect; return Rectangle.Round(new RectangleF(r.X + P(22), r.Y + P(18), r.Width - P(44), r.Height - P(32))); } }
        int KeysTop { get { return (int)CavityRect.Bottom + P(11); } }
        RectangleF BandRect { get { return new RectangleF(P(Side + 8), ClientSize.Height - P(Band) - P(4), ClientSize.Width - 2 * P(Side + 8), P(Band)); } }

        public SetupForm(bool uninstall, string dir)
        {
            this.uninstall = uninstall; uninstallDir = dir;
            Icon = AppInfo.Icon; Text = AppInfo.Name + " — " + (uninstall ? L.T("uninstall") : L.T("setup"));
            FormBorderStyle = FormBorderStyle.None; BackColor = BodyTheme.Hull; ForeColor = BodyTheme.Ink;
            Cursor = BluePointer; AutoScaleMode = AutoScaleMode.None; StartPosition = FormStartPosition.CenterScreen; MaximizeBox = false;
            DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            ClientSize = new Size(P(W0), P(H0));
            using (var rp = Round(new RectangleF(0, 0, ClientSize.Width, ClientSize.Height), 24)) Region = new Region(rp);
            heading.Face = HeadFont(19); heading.ForeColor = BodyTheme.Ink;
            hint.Face = TextFont; hint.ForeColor = Color.FromArgb(150, 153, 160);
            Controls.AddRange(new Control[] { heading, hint, page, back, next, cancel, close });
            folder.Text = SetupProgram.DefaultDir;
            back.Click += (s, e) => Go(step - 1);
            next.Click += (s, e) => Next();
            cancel.Click += (s, e) => Close();
            close.Click += (s, e) => Close();
            accept.CheckedChanged += (s, e) => Frame();
            anim.Tick += (s, e) =>
            {
                double t = clock.Elapsed.TotalSeconds, dt = lastTick < 0 ? 0 : t - lastTick;
                lastTick = t; animSecs = 1.3 + t;
                shown = Math.Min(progress, shown + (float)(dt / BarSecs));
                if (finished && shown >= progress) { finished = false; busy = false; done = true; anim.Stop(); Go(4); }
                page.Invalidate(true); Invalidate();
            };
            Go(0);
        }

        protected override void OnFormClosing(FormClosingEventArgs e) { if (busy) { e.Cancel = true; return; } base.OnFormClosing(e); }

        // the body is dragged by any part that is not a control
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x84 /* WM_NCHITTEST */ && (int)m.Result == 1 /* HTCLIENT */) m.Result = (IntPtr)2 /* HTCAPTION */;
        }

        // a control on the body or the screen paints the body behind it first (its corners show the hull)
        public static void PaintBehind(Control c, Graphics g)
        {
            var f = c.FindForm() as SetupForm;
            if (f == null || f.shell == null) { g.Clear(BodyTheme.Hull); return; }
            var at = f.PointToClient(c.PointToScreen(Point.Empty));
            g.DrawImageUnscaled(f.shell, -at.X, -at.Y);
        }

        // steps - install: 0 welcome, 1 license, 2 folder, 3 installing, 4 done; uninstall: 0 confirm, 3 removing, 4 done
        void Go(int s)
        {
            step = Math.Max(0, s);
            foreach (var c in page.Controls.Cast<Control>().ToList()) { page.Controls.Remove(c); if (c != folder && c != accept && c != desktop && c != start && c != wipe && c != launch && c != updates) c.Dispose(); }
            Frame();
            if (uninstall) { if (step == 0) BuildUninstall(); else BuildProgress(); }
            else if (step == 0) BuildWelcome(); else if (step == 1) BuildLicense(); else if (step == 2) BuildFolder(); else BuildProgress();
            heading.Text = heading.Text.ToUpperInvariant();
        }

        void Frame()
        {
            var a = ScreenArea;
            heading.SetBounds(a.X, a.Y, a.Width, P(30));
            hint.SetBounds(a.X, a.Y + P(34), a.Width, P(40));
            page.SetBounds(a.X, a.Y + P(82), a.Width, a.Bottom - a.Y - P(82));
            close.SetBounds(ClientSize.Width - P(Side + 8) - P(24), P(11), P(24), P(24));
            next.Text = uninstall ? (step == 0 ? L.T("Uninstall") : L.T("Close")) : step == 2 ? L.T("Install") : step >= 4 ? L.T("Finish") : L.T("Next");
            bool showBack = !uninstall && (step == 1 || step == 2), showCancel = step < 3, showNext = step != 3;
            back.Visible = showBack; cancel.Visible = showCancel; next.Visible = showNext;
            close.Enabled = !busy;
            int x = ClientSize.Width - P(Side + 8);
            foreach (var b in new[] { next, back, cancel })
            {
                if ((b == back && !showBack) || (b == cancel && !showCancel) || (b == next && !showNext)) continue;
                int w = b.PreferredWidth();
                b.SetBounds(x - w, KeysTop, w, P(KeysH)); x = b.Left - P(10);
            }
            next.Enabled = !(step == 1 && !accept.Checked);
            Invalidate(); foreach (Control c in new Control[] { back, next, cancel, close }) c.Invalidate();
        }

        ScreenText Text_(string t, Color c, int x, int y, int w, int h, Font f = null)
        {
            return new ScreenText { Text = t, ForeColor = c, Face = f ?? TextFont, Location = new Point(x, y), Size = new Size(w, h), ParaGap = P(5) };
        }
        // %LOCALAPPDATA%\... instead of the user's own folder on the pages
        static string Shown(string path)
        {
            var la = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).TrimEnd('\\');
            return path.StartsWith(la + "\\", StringComparison.OrdinalIgnoreCase) ? "%LOCALAPPDATA%" + path.Substring(la.Length) : path;
        }
        // a small section mark on the screen: an accent dot and the title in Orbitron caps
        ScreenText Section(string t, int y)
        {
            var l = Text_("●  " + t.ToUpperInvariant(), BodyTheme.Accent, 0, y, page.Width, P(18), HeadFont(10.5f));
            page.Controls.Add(l);
            return l;
        }

        void BuildWelcome()
        {
            heading.Text = L.F("Install {0}", AppInfo.Title);
            hint.Text = L.T("A DPS meter for EVE Online from the game's own combat log. Runs fully on this PC: no AI, no cloud.");
            Section(L.T("Before you install"), 0);
            page.Controls.Add(Text_(
                L.F("• {0} reads the combat logs EVE writes on this PC and shows your fleet's damage, hits and bounties.", AppInfo.Name) + "\n" +
                L.T("• It never sends anything to the game: no key presses, no clicks, no screen reading.") + "\n" +
                L.T("• It does not read game memory, network traffic or cache files, and does not change the EVE client or its settings.") + "\n" +
                L.T("• It is provided as is, without warranty. Use it at your own risk.") + "\n" +
                L.F("• No administrator rights are needed. Your settings are kept in {0}.", Shown(AppInfo.DataDir)),
                Color.FromArgb(214, 216, 221), 0, P(26), page.Width, page.Height - P(26) - P(52)));
            page.Controls.Add(Text_(Disclaimer, Color.FromArgb(118, 121, 128), 0, page.Height - P(50), page.Width, P(50), SmallFont));
        }

        public static string Disclaimer
        {
            get { return L.F("{0} is a free, independent, fan-made tool. It is not affiliated with, endorsed or sponsored by CCP hf. EVE, EVE Online and CCP are trademarks or registered trademarks of CCP hf.", AppInfo.Name); }
        }

        void BuildLicense()
        {
            heading.Text = L.T("License");
            hint.Text = L.F("{0} is open source under the MIT license.", AppInfo.Name);
            var box = new DarkField(true); box.Box.ReadOnly = true; box.Box.WordWrap = true;
            var lic = SetupProgram.PayloadText("LICENSE");
            box.Text = (lic == "" ? "MIT License" : lic).Replace("\r\n", "\n").Replace("\n", "\r\n");
            box.SetBounds(0, 0, page.Width, page.Height - P(44));
            accept.SetBounds(0, page.Height - P(30), accept.PreferredWidth(), P(30));
            page.Controls.Add(box); page.Controls.Add(accept);
        }

        void BuildFolder()
        {
            heading.Text = L.T("Where to install");
            hint.Text = L.T("A folder of your own user — no administrator rights. An update keeps your settings.");
            Section(L.T("Install folder"), 0);
            var browse = new KeyButton(L.T("Browse…"));
            int bw = browse.PreferredWidth();
            folder.SetBounds(0, P(28), page.Width - bw - P(10), P(36));
            browse.SetBounds(page.Width - bw, P(28), bw, P(36));
            browse.Click += (s, e) =>
            {
                using (var d = new FolderBrowserDialog { SelectedPath = folder.Text, Description = L.F("Folder for {0}", AppInfo.Name) })
                    if (d.ShowDialog(this) == DialogResult.OK)
                    {
                        var leaf = Path.GetFileName(d.SelectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                        folder.Text = string.Equals(leaf, AppInfo.Name, StringComparison.OrdinalIgnoreCase) ? d.SelectedPath : Path.Combine(d.SelectedPath, AppInfo.Name);
                    }
            };
            desktop.SetBounds(0, P(84), desktop.PreferredWidth(), P(30));
            start.SetBounds(desktop.Right + P(24), P(84), start.PreferredWidth(), P(30));
            page.Controls.AddRange(new Control[] { folder, browse, desktop, start });
            page.Controls.Add(Text_(L.T("Space needed: about 1 MB.  Uninstall later from Windows Settings → Apps."), Color.FromArgb(118, 121, 128), 0, P(124), page.Width, P(24), SmallFont));
            Section(L.T("Privacy"), P(156));
            page.Controls.Add(Text_(L.T("No account, no telemetry. The only network request is the update check (the GitHub Releases API); it sends nothing about you or your game."),
                Color.FromArgb(214, 216, 221), 0, P(182), page.Width, P(42)));
            updates.SetBounds(0, P(226), updates.PreferredWidth(), P(30));
            page.Controls.Add(updates);
        }

        // config.json "app": { "update_check" } as the app keeps it (Settings.cs); on unless it was turned off
        static bool UpdateCheckBefore()
        {
            try { var v = ConfigStore.Load(AppInfo.ConfigPath); object a; if (v.TryGetValue("app", out a) && a is Dictionary<string, object> && ((Dictionary<string, object>)a).ContainsKey("update_check")) return !(((Dictionary<string, object>)a)["update_check"] is bool) || (bool)((Dictionary<string, object>)a)["update_check"]; }
            catch { }
            return true;
        }

        static void SaveUpdateCheck(bool on)
        {
            try
            {
                Directory.CreateDirectory(AppInfo.DataDir);
                var root = File.Exists(AppInfo.ConfigPath) ? ConfigStore.Load(AppInfo.ConfigPath) : new Dictionary<string, object>();
                ConfigStore.Obj(root, "app")["update_check"] = on;
                ConfigStore.Save(AppInfo.ConfigPath, root);
            }
            catch { }
        }

        void BuildUninstall()
        {
            heading.Text = L.F("Uninstall {0}", AppInfo.Name);
            hint.Text = L.F("Removes the app from {0}, its shortcuts and its entry in Windows Settings → Apps.", Shown(uninstallDir));
            Section(L.T("Your data"), 0);
            page.Controls.Add(Text_(L.F("Your settings stay in {0} — unless you switch this on:", Shown(AppInfo.DataDir)), Color.FromArgb(214, 216, 221), 0, P(28), page.Width, P(44)));
            wipe.SetBounds(0, P(78), wipe.PreferredWidth(), P(30));
            page.Controls.Add(wipe);
        }

        // progress page: the working animation (it runs while the work goes on), the bar and the current file
        void BuildProgress()
        {
            heading.Text = done ? (failed ? L.T("Something went wrong") : uninstall ? L.F("{0} is uninstalled", AppInfo.Name) : L.F("{0} is installed", AppInfo.Name))
                                : uninstall ? L.T("Uninstalling…") : L.T("Installing…");
            hint.Text = done ? (failed ? status : uninstall ? L.F("Thanks for flying with {0}.", AppInfo.Name) : L.T("Start it from the shortcut, or right here.")) : "";
            Section(done ? L.T("Done") : L.T("Working"), 0);
            var bar = new ProgressPaint(() => done ? 1f : shown, () => done ? "" : status);
            bar.SetBounds(0, P(34), page.Width, P(120));
            page.Controls.Add(bar);
            if (done && !failed && !uninstall) { launch.SetBounds(0, P(170), launch.PreferredWidth(), P(30)); page.Controls.Add(launch); }
        }

        void Next()
        {
            if (busy) return;
            if (step >= 4 || (uninstall && step == 3 && done)) { Finish(); return; }
            if (uninstall) { Work(Uninstall); return; }
            if (step < 2) { Go(step + 1); return; }
            Work(Install);
        }

        void Finish()
        {
            if (!uninstall && !failed && launch.Checked)
                try { Process.Start(new ProcessStartInfo(Path.Combine(folder.Text, SetupProgram.Exe)) { WorkingDirectory = folder.Text }); }
                catch { }
            if (uninstall) SelfDelete();
            Close();
        }

        void Work(Action<string> job)
        {
            var target = uninstall ? uninstallDir : folder.Text.Trim();
            // a running copy of the app from that folder must close first
            var run = SetupProgram.Running(target);
            try
            {
                if (run.Count > 0)
                {
                    if (MessageBox.Show(this, L.F("{0} is running. Close it now?", AppInfo.Name), Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
                    foreach (var p in run) { try { p.CloseMainWindow(); p.WaitForExit(8000); } catch { } }
                    var run2 = SetupProgram.Running(target);
                    bool stillRunning = run2.Count > 0;
                    foreach (var p in run2) p.Dispose();
                    if (stillRunning) { MessageBox.Show(this, L.F("Close {0} and press {1} again.", AppInfo.Name, next.Text), Text); return; }
                }
            }
            finally { foreach (var p in run) p.Dispose(); }
            busy = true; done = failed = finished = false; progress = 0; shown = 0; lastTick = -1; status = "";
            Go(3); anim.Start();
            var t = new Thread(() =>
            {
                try { job(target); }
                catch (Exception ex) { failed = true; status = ex.Message; }
                BeginInvoke((Action)(() =>
                {
                    if (failed) { busy = false; done = true; anim.Stop(); Go(4); }   // an error shows at once
                    else { progress = 1; finished = true; }
                }));
            }) { IsBackground = true };
            t.Start();
        }

        void Install(string dir)
        {
            Directory.CreateDirectory(dir);
            var list = new List<string>();
            using (var z = SetupProgram.Payload())
            {
                var files = z.Entries.Where(e => e.Name != "").ToList();
                for (int i = 0; i < files.Count; i++)
                {
                    var e = files[i]; status = e.FullName; progress = 0.85f * i / files.Count;
                    var dst = Path.Combine(dir, e.FullName.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(dst));
                    e.ExtractToFile(dst, true);
                    list.Add(e.FullName.Replace('/', '\\'));
                    Thread.Sleep(15);   // the animation gets to run a little
                }
            }
            status = L.T("Uninstaller…");
            var un = Path.Combine(dir, "Uninstall.exe");
            File.Copy(Application.ExecutablePath, un, true); list.Add("Uninstall.exe");
            list.Add("install.txt");
            // an update keeps the files of the earlier version on the list, so the uninstaller still removes them
            var listFile = Path.Combine(dir, "install.txt");
            if (File.Exists(listFile)) list = File.ReadAllLines(listFile).Where(l => l.Trim() != "").Concat(list).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            File.WriteAllLines(listFile, list);
            progress = 0.9f; status = L.T("Shortcuts…");
            var exe = Path.Combine(dir, SetupProgram.Exe);
            if (desktop.Checked) SetupProgram.Shortcut(SetupProgram.DesktopLnk, exe);
            if (start.Checked) SetupProgram.Shortcut(SetupProgram.StartLnk, exe);
            if (!selfTesting) SaveUpdateCheck(updates.Checked);
            status = L.T("Windows Settings → Apps…");
            long kb = list.Select(f => Path.Combine(dir, f)).Where(File.Exists).Sum(f => new FileInfo(f).Length) / 1024;
            using (var k = Registry.CurrentUser.CreateSubKey(SetupProgram.UninstallKey))
            {
                k.SetValue("DisplayName", AppInfo.Name);
                k.SetValue("DisplayVersion", AppInfo.Version);
                k.SetValue("Publisher", AppInfo.Name + " contributors");
                k.SetValue("InstallLocation", dir);
                k.SetValue("DisplayIcon", exe + ",0");
                k.SetValue("UninstallString", "\"" + un + "\" /uninstall \"" + dir + "\"");
                k.SetValue("NoModify", 1, RegistryValueKind.DWord); k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                k.SetValue("EstimatedSize", (int)kb, RegistryValueKind.DWord);
            }
            progress = 1; status = "";
        }

        void Uninstall(string dir)
        {
            SetupProgram.RemoveInstall(dir, SetupProgram.Exe, AppInfo.Name, SetupProgram.UninstallKey, (s, p) => { status = s; progress = p; });
            if (wipe.Checked) { status = L.T("Your data…"); try { if (Directory.Exists(AppInfo.DataDir)) Directory.Delete(AppInfo.DataDir, true); } catch { } }
            progress = 1; status = "";
        }

        // ---------------- the body ----------------
        protected override void OnResize(EventArgs e) { base.OnResize(e); if (shell != null) { shell.Dispose(); shell = null; } }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (shell == null || shell.Size != ClientSize) { if (shell != null) shell.Dispose(); shell = DrawShell(); }
            e.Graphics.DrawImageUnscaled(shell, 0, 0);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            // the lamp next to the name: lit while the installer works or is done, a steady glow otherwise
            float d = P(8.5f), x = nameRight + P(8), y = nameMid - d / 2;
            double level = busy ? 0.55 + 0.45 * Math.Max(0, Math.Sin(animSecs * Math.PI / 0.6)) : done && failed ? 0.3 : 1;
            DpsForm.SoftGlow(g, new PointF(x + d / 2, y + d / 2), d / 2 + P(6), BodyTheme.Accent, (int)(level * 150));
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(x, y, d, d);
                using (var pb = new PathGradientBrush(path))
                {
                    pb.CenterColor = DpsForm.Mix(Color.FromArgb(70, 72, 20), Color.FromArgb(255, 255, 200), level);
                    pb.SurroundColors = new[] { DpsForm.Mix(Color.FromArgb(40, 42, 12), Color.FromArgb(225, 240, 0), level) };
                    g.FillPath(pb, path);
                }
            }
        }

        Bitmap DrawShell()
        {
            int Wd = Math.Max(1, ClientSize.Width), Hd = Math.Max(1, ClientSize.Height);
            var bmp = new Bitmap(Wd, Hd, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(BodyTheme.Hull);
                var all = new RectangleF(0, 0, Wd, Hd);
                using (var body = Round(all, 24))
                {
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

                // ノルン and what this is: SETUP / UNINSTALL printed after the lamp
                float x0 = P(Side + 10), mid = P(Strip) / 2f + P(1);
                var name = DpsForm.NamePath(P(14));
                if (name != null)
                    using (name)
                    {
                        var nb0 = name.GetBounds();
                        using (var m = new Matrix()) { m.Translate(x0 - nb0.X, mid - nb0.Height / 2 - nb0.Y); name.Transform(m); }
                        using (var nb = new SolidBrush(Color.FromArgb(244, 245, 247))) g.FillPath(nb, name);
                        nameRight = x0 + nb0.Width;
                    }
                else
                {
                    using (var nb = new SolidBrush(Color.FromArgb(244, 245, 247))) g.DrawString(AppInfo.Name, HeadFont(13), nb, x0, mid - P(8), StringFormat.GenericTypographic);
                    nameRight = x0 + g.MeasureString(AppInfo.Name, HeadFont(13), 1000, StringFormat.GenericTypographic).Width;
                }
                nameMid = mid;
                var f = NornsFonts.Body(P(9.5f));
                using (var silk = new SolidBrush(Color.FromArgb(215, BodyTheme.Silk)))
                    DpsForm.Spaced(g, (uninstall ? "UNINSTALL" : "SETUP") + " · " + AppInfo.VersionText.ToUpperInvariant(), f, silk, nameRight + P(26), mid - f.GetHeight(g) / 2, P(1.1f));

                // the recessed screen: cavity, bezel, black glass
                var cav = CavityRect;
                using (var cp = Round(cav, 17))
                {
                    using (var b = new SolidBrush(BodyTheme.Hull)) g.FillPath(b, cp);
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
                    var sh = bz; sh.Offset(0, P(1.5f));
                    using (var sp = Round(sh, 15.5f)) using (var sb = new SolidBrush(Color.FromArgb(110, 0, 0, 0))) g.FillPath(sb, sp);
                    g.FillPath(lg, bp);
                    using (var pen = new Pen(Color.FromArgb(26, 255, 255, 255), 1f)) g.DrawPath(pen, bp);
                }
                using (var gp = Round(GlassRect, 13)) using (var b = new SolidBrush(Color.Black)) g.FillPath(b, gp);

                // printed markings on the bottom edge
                var r = BandRect;
                var bf = NornsFonts.Body(P(8.5f));
                float cy = r.Top + r.Height / 2, ty = cy - bf.GetHeight(g) / 2, ls = P(0.9f);
                using (var silk = new SolidBrush(Color.FromArgb(215, BodyTheme.Silk)))
                {
                    float x = r.Left;
                    foreach (var ch in "NRN-01")
                    {
                        DpsForm.Weighted(g, ch.ToString(), bf, BodyTheme.Name, x, ty, P(0.35f));
                        x += g.MeasureString(ch.ToString(), bf, 1000, StringFormat.GenericTypographic).Width + ls;
                    }
                    x += P(7) - ls;
                    using (var b = new SolidBrush(Color.FromArgb(191, 236, 235, 228))) g.FillRectangle(b, x, cy - P(2.5f), P(3), P(5));
                    x += P(3) + P(7);
                    DpsForm.Spaced(g, "PER-USER · NO ADMIN · READ ONLY", bf, silk, x, ty, ls);
                    var mod = "MOD " + AppInfo.Version;
                    float w = DpsForm.SpacedWidth(g, mod, bf, ls);
                    DpsForm.Spaced(g, mod, bf, silk, r.Right - w, ty, ls);
                    float rx = r.Right - w - P(8);
                    for (int i = 2; i >= 0; i--) g.FillRectangle(silk, rx - P(1) - i * P(2.2f), cy - P(2.75f), P(1), P(5.5f));
                }
            }
            return bmp;
        }

        bool selfTesting;
        public void SelfTest(string outDir)
        {
            Directory.CreateDirectory(outDir);
            SetupProgram.UninstallKey += "-selftest"; selfTesting = true;   // the user's config.json left as it is
            Opacity = 0; ShowInTaskbar = false; Show();
            Action<string> shot = name => { Application.DoEvents(); using (var b = new Bitmap(ClientSize.Width, ClientSize.Height)) { DrawToBitmap(b, new Rectangle(Point.Empty, ClientSize)); b.Save(Path.Combine(outDir, name)); } };
            shot("setup_1_welcome.png");
            Go(1); shot("setup_2_license.png"); accept.Checked = true; Frame(); shot("setup_2_license_accepted.png");
            var dir = Path.Combine(outDir, "install"); folder.Text = dir; desktop.Checked = false; start.Checked = false;
            Go(2); shot("setup_3_folder.png");
            var log = new List<string>();
            busy = true; Go(3); progress = shown = 0.42f; status = SetupProgram.Exe; shot("setup_4_installing.png");
            try { Install(dir); log.Add("install: ok, " + Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length + " files"); }
            catch (Exception ex) { log.Add("install FAILED: " + ex.Message); }
            busy = false; done = true; Go(4); shot("setup_5_done.png");
            using (var k = Registry.CurrentUser.OpenSubKey(SetupProgram.UninstallKey)) log.Add("apps entry: " + (k != null ? k.GetValue("DisplayName") + " " + k.GetValue("DisplayVersion") + " | " + k.GetValue("UninstallString") : "MISSING"));
            try { log.Add("files: " + string.Join(", ", File.ReadAllLines(Path.Combine(dir, "install.txt")))); } catch { }
            try { Uninstall(dir); log.Add("uninstall: folder " + (Directory.Exists(dir) ? "LEFT: " + string.Join(", ", Directory.GetFileSystemEntries(dir, "*", SearchOption.AllDirectories)) : "removed")); }
            catch (Exception ex) { log.Add("uninstall FAILED: " + ex.Message); }
            using (var k = Registry.CurrentUser.OpenSubKey(SetupProgram.UninstallKey)) log.Add("apps entry after uninstall: " + (k == null ? "removed" : "LEFT"));
            File.WriteAllLines(Path.Combine(outDir, "setup_selftest.txt"), log);
            var un = new SetupForm(true, dir) { Opacity = 0, ShowInTaskbar = false }; un.Show(); Application.DoEvents();
            using (var b = new Bitmap(un.ClientSize.Width, un.ClientSize.Height)) { un.DrawToBitmap(b, new Rectangle(Point.Empty, un.ClientSize)); b.Save(Path.Combine(outDir, "setup_6_uninstall.png")); }
            un.Close(); Close();
        }

        // the temp copy of the uninstaller removes itself after it has closed
        void SelfDelete()
        {
            try
            {
                var me = Application.ExecutablePath;
                if (!me.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase)) return;
                Process.Start(new ProcessStartInfo("cmd.exe", "/c ping -n 3 127.0.0.1 >nul & del /q \"" + me + "\"") { CreateNoWindow = true, UseShellExecute = false });
            }
            catch { }
        }
    }
}
