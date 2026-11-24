// TuneWarn.cs - the warning before Tuning (owner, 23.11.2026): the Settings header's Tuning key is always there; while
// tuning is off it opens this small window first. The hull is heated yellow, as the body's when a dreadnought is on grid
// (a danger, not a critical one); the black glass says what Tuning does and asks to read each value's text first.
// Open Tuning switches tuning on (Tune.Init: tuning.json is read and followed), keeps it on after a restart (config.json
// "app": { "tuning" }) and opens the Tuning window;
// Cancel, the close key or Esc only close this window. It follows the app's settings live, as the Tuning window.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace TheNorns
{
    sealed class TuneWarnWindow : Form
    {
        // Open: before Tuning; Delete: before the custom tuning is cleared and deleted (the hull heated red: it cannot be undone)
        internal enum Kind { Open, Delete }
        readonly Form body;
        readonly Kind kind;
        const float WW = 300, HPad = 7, HStrip = 30, HGap = 6;
        static float M(float v) { return v * Theme.K * DpsForm.UiScale; }
        static string T(string en) { return TuneWindow.T(en); }
        const string WarnIcon = "M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z";   // Material warning
        internal const string DeleteIcon = "M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z";   // Material delete
        internal const string Science = "M19.8 18.4L14 10.67V6.5l1.35-1.69c.26-.33.03-.81-.39-.81H9.04c-.42 0-.65.48-.39.81L10 6.5v4.17L4.2 18.4c-.49.66-.02 1.6.8 1.6h14c.82 0 1.29-.94.8-1.6z";   // Material science

        string Head { get { return kind == Kind.Delete ? T("DELETE CUSTOM TUNING") : T("CHANGE WITH CARE"); } }
        string Msg { get { return kind == Kind.Delete ? T("This clears all your custom values, deletes tuning.json and switches Tuning off. Every value goes back to its default. You cannot undo this.") : T("Tuning changes the values The Norns uses to read the logs and to count. A wrong value can make the program show wrong numbers or work incorrectly. Read the description of each value carefully before you change it."); } }

        RectangleF closeR, okR, cancelR;
        Point mouse = new Point(-1, -1);
        string downId;
        string look;
        readonly Timer timer = new Timer { Interval = 200 };
        public bool Accepted;

        public TuneWarnWindow(Form body, Kind kind = Kind.Open)
        {
            this.body = body; this.kind = kind;
            Text = AppInfo.Name + " — Tuning";
            Icon = AppInfo.Icon;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true; MaximizeBox = false;
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.None;
            SyncLook();
            timer.Tick += (s, e) => SyncLook();
            timer.Start();
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); if (e.KeyCode == Keys.Enter) Accept(); };
        }
        protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
        protected override CreateParams CreateParams { get { var cp = base.CreateParams; cp.ExStyle |= 0x80000 /* WS_EX_LAYERED */; return cp; } }
        protected override void OnShown(EventArgs e) { base.OnShown(e); Redraw(); }

        // the app's settings while the window is open: Potato, eye comfort, colours, the size, the language
        void SyncLook()
        {
            var now = string.Join("|", DpsForm.Potato, DpsForm.EyeComfort, DpsForm.UiScale, TuneWindow.Lang, BodyTheme.Hull.ToArgb(), BodyTheme.Accent.ToArgb(), BodyTheme.Dread.ToArgb());
            if (now == look) return;
            look = now;
            var size = new Size((int)Math.Round(M(WW)), (int)Math.Round(Measure()));
            if (ClientSize != size)
            {
                ClientSize = size;
                // Open: up and to the right of the body, over its top right corner (owner, 23.11.2026); Delete: over the
                // Tuning window's middle. On the body's screen
                var wa = Screen.FromControl(body).WorkingArea;
                int x = body.Left + (body.Width - Width) / 2, y = body.Top + (body.Height - Height) / 2;
                if (kind == Kind.Open) { x = body.Left + (int)Math.Round(M(59)); y = body.Top - (int)Math.Round(M(21)); }
                Location = new Point(Math.Max(wa.Left, Math.Min(x, wa.Right - Width)), Math.Max(wa.Top, Math.Min(y, wa.Bottom - Height)));
            }
            Redraw();
        }

        // the sizes on the app's scale: the heading as the cards' titles (8.8), the text as the cards' text (9.6)
        static Font HeadF(string t) { return TuneWindow.TitleF(t, M(8.8f)); }   // the Tuning window's faces
        static Font TextF(string t) { return TuneWindow.TextF(t, M(9.6f)); }
        static readonly StringFormat Wrap = new StringFormat();
        float TextW { get { return M(WW - 2 * HPad - 8 - 2 * 12); } }
        float Measure()
        {
            using (var bmp = new Bitmap(1, 1)) using (var g = Graphics.FromImage(bmp))
            {
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                float hh = g.MeasureString(Head, HeadF(Head), new SizeF(TextW, 10000), StringFormat.GenericTypographic).Height;
                float th = g.MeasureString(Msg, TextF(Msg), new SizeF(TextW, 10000), Wrap).Height;
                return M(HPad + HStrip + HGap + 4 + 11) + hh + M(7) + th + M(12) + M(19) + M(11 + 4 + HPad + 4);
            }
        }

        void Redraw()
        {
            if (!IsHandleCreated || !Visible) return;
            using (var b = Render()) AppChrome.PresentLayered(this, b);
        }
        public Bitmap Render()
        {
            var b = new Bitmap(ClientSize.Width, ClientSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(b)) Paint(g, ClientSize);
            return b;
        }

        static GraphicsPath HRound(RectangleF r, float lr) { return Theme.Round(r, M(lr * 0.6f), 0.6f); }
        void Paint(Graphics g, Size size)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            float W = size.Width, H = size.Height;
            var all = new RectangleF(0, 0, W, H);
            // the hull, heated yellow as the body's on a dreadnought (DpsForm.PaintHot, without the flicker)
            var heat = kind == Kind.Delete ? DpsForm.HotRed : DpsForm.HotYellow;
            using (var hull = HRound(all, 24))
            {
                using (var b = new SolidBrush(BodyTheme.Hull)) g.FillPath(b, hull);
                if (!DpsForm.Potato)
                    using (var lg = new LinearGradientBrush(all, Color.White, Color.Black, 80f))
                    {
                        lg.InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(36, 255, 255, 255), Color.FromArgb(5, 255, 255, 255), Color.FromArgb(66, 0, 0, 0) }, Positions = new[] { 0f, 0.45f, 1f } };
                        g.FillPath(lg, hull);
                    }
                var st = g.Save();
                g.SetClip(hull);
                using (var lg = new LinearGradientBrush(all, Color.Black, Color.Black, 90f))
                {
                    lg.InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(125, heat[0]), Color.FromArgb(150, heat[1]), Color.FromArgb(170, heat[2]) }, Positions = new[] { 0f, 0.55f, 1f } };
                    g.FillRectangle(lg, all);
                }
                if (!DpsForm.Potato) DpsForm.SoftGlow(g, new PointF(W / 2f, H), W * 0.7f, heat[3], 70);
                g.Restore(st);
                var edge = all; edge.Inflate(-0.5f, -0.5f);
                using (var ep = HRound(edge, 24))
                using (var lg = new LinearGradientBrush(all, Color.FromArgb(70, 255, 255, 255), Color.FromArgb(115, 0, 0, 0), 90f))
                using (var pen = new Pen(lg, 1f)) g.DrawPath(pen, ep);
            }

            // the strip: the warning sign and TUNING printed, the close key
            float mid = M(HPad + HStrip / 2), x0 = M(HPad + 7);
            var silk = Color.FromArgb(235, BodyTheme.Silk);
            float ic = M(11);
            using (var b = new SolidBrush(silk)) MaterialIcon.Draw(g, WarnIcon, new RectangleF(x0, mid - ic / 2, ic, ic), b);
            var tt = T("TUNING").ToUpperInvariant();
            var tf = TuneWindow.TitleF(tt, M(7.2f));   // Caption, as the Tuning window's
            using (var b = new SolidBrush(silk)) DpsForm.Spaced(g, tt, tf, b, x0 + ic + M(6), mid - tf.GetHeight(g) / 2, M(1));
            float kw = M(14.2f), kh = M(24);
            closeR = new RectangleF(W - M(HPad + 4) - kw, mid - kh / 2, kw, kh);
            DpsForm.CloseKey(g, closeR, closeR.Contains(mouse), downId == "close" && closeR.Contains(mouse), M(1));

            // the black glass in its recess: the heading in the dreadnought's yellow, the text, the two keys
            var glass = PaintRecess(g, RectangleF.FromLTRB(M(HPad), M(HPad + HStrip + HGap), W - M(HPad), H - M(HPad + 4)));
            float x = glass.Left + M(12), y = glass.Top + M(11), tw = glass.Width - M(24);
            var hf = HeadF(Head);
            float hh = g.MeasureString(Head, hf, new SizeF(tw, 10000), StringFormat.GenericTypographic).Height;
            using (var b = new SolidBrush(kind == Kind.Delete ? BodyTheme.Alarm : BodyTheme.Dread)) g.DrawString(Head, hf, b, new RectangleF(x, y, tw, hh + M(1)), StringFormat.GenericTypographic);
            y += hh + M(7);
            var xf = TextF(Msg);
            float th = g.MeasureString(Msg, xf, new SizeF(tw, 10000), Wrap).Height;
            using (var b = new SolidBrush(BodyTheme.Label)) g.DrawString(Msg, xf, b, new RectangleF(x, y, tw, th + M(2)), Wrap);
            y += th + M(12);
            float kgap = M(6), kw2 = (tw - kgap) / 2;
            cancelR = new RectangleF(x, y, kw2, M(19));
            okR = new RectangleF(x + kw2 + kgap, y, kw2, M(19));
            DpsForm.WideKey(g, cancelR, MaterialIcon.Close, T("Cancel"), false, cancelR.Contains(mouse), downId == "cancel" && cancelR.Contains(mouse));
            if (kind == Kind.Delete) DpsForm.WideKey(g, okR, DeleteIcon, T("Clear and delete"), false, okR.Contains(mouse), downId == "ok" && okR.Contains(mouse));
            else DpsForm.WideKey(g, okR, Science, T("Open Tuning"), true, okR.Contains(mouse), downId == "ok" && okR.Contains(mouse));
        }

        // a screen in its recess, as the Tuning window's (TuneWindow.PaintRecess); returns the glass
        static RectangleF PaintRecess(Graphics g, RectangleF cav)
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

        // ---------------- input ----------------
        string At(Point p) { return closeR.Contains(p) ? "close" : okR.Contains(p) ? "ok" : cancelR.Contains(p) ? "cancel" : null; }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0xA3 /* WM_NCLBUTTONDBLCLK */) { m.Result = IntPtr.Zero; return; }   // a double click on the hull does nothing (owner, 24.11.2026: it maximized the window)
            if (m.Msg == 0x84 /* WM_NCHITTEST */)
            {
                var p = PointToClient(new Point((short)(m.LParam.ToInt64() & 0xFFFF), (short)((m.LParam.ToInt64() >> 16) & 0xFFFF)));
                bool onBody;
                using (var hull = HRound(new RectangleF(0, 0, Width, Height), 24)) onBody = hull.IsVisible(p);
                m.Result = (IntPtr)(!onBody ? -1 /* HTTRANSPARENT */ : At(p) != null ? 1 /* HTCLIENT */ : 2 /* HTCAPTION */);
                return;
            }
            if (m.Msg == 0x20 /* WM_SETCURSOR */)
            {
                int hit = (short)(m.LParam.ToInt64() & 0xFFFF);
                if (hit == 1 || hit == 2) { Cursor.Current = At(PointToClient(Cursor.Position)) != null ? DpsForm.PulsePointer : DpsForm.Pointer; m.Result = (IntPtr)1; return; }
            }
            base.WndProc(ref m);
        }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); var was = At(mouse); mouse = e.Location; if (At(mouse) != was) Redraw(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); mouse = new Point(-1, -1); Redraw(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { downId = At(e.Location); Redraw(); } }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            var id = At(e.Location); var was = downId; downId = null;
            if (id == null || id != was) { Redraw(); return; }
            if (id == "ok") Accept(); else Close();
        }
        void Accept() { Accepted = true; Close(); }

        // selftest: the window as it opens, and with the mouse on Open Tuning
        public static void SelfTest(DpsForm f, string dir)
        {
            using (var w = new TuneWarnWindow(f))
            {
                foreach (var lg in new[] { "en", "uk", "zh" })
                {
                    TuneWindow.Lang = lg; w.SyncLook();
                    using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_warning" + (lg == "en" ? "" : "_" + lg) + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                TuneWindow.Lang = null; w.SyncLook();
                w.mouse = Point.Round(new PointF(w.okR.Left + w.okR.Width / 2, w.okR.Top + w.okR.Height / 2));
                using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_warning_hover.png"), System.Drawing.Imaging.ImageFormat.Png);
                bool keep = DpsForm.Potato; DpsForm.Potato = true; w.SyncLook();
                using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_warning_potato.png"), System.Drawing.Imaging.ImageFormat.Png);
                DpsForm.Potato = keep; w.SyncLook();
            }
            using (var w = new TuneWarnWindow(f, Kind.Delete))
                using (var b = w.Render()) b.Save(System.IO.Path.Combine(dir, "tune_delete_warning.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
    }

    partial class DpsForm
    {
        TuneWarnWindow tuneWarn;
        // the Settings header's Tuning key: the window at once while tuning is on, else the warning first
        internal void TuningKey()
        {
            if (Tune.Enabled) { OpenTuning(); return; }
            if (tuneWarn != null && !tuneWarn.IsDisposed) { tuneWarn.Activate(); return; }
            tuneWarn = new TuneWarnWindow(this);
            tuneWarn.FormClosed += (s, e) =>
            {
                bool ok = tuneWarn.Accepted; tuneWarn = null;
                if (ok) { Tune.Init(true); Tune.Version++; if (!SelfTesting) SaveApp("tuning", true); OpenTuning(); }   // kept: on after a restart too
            };
            tuneWarn.Show();
            tuneWarn.Activate();
        }

        // the Tuning window's Delete key: after the red warning, every value back to its default, tuning.json deleted,
        // tuning off (also after a restart) and the Tuning window closed
        internal void AskDeleteTuning(Form over)
        {
            if (tuneWarn != null && !tuneWarn.IsDisposed) { tuneWarn.Activate(); return; }
            tuneWarn = new TuneWarnWindow(over, TuneWarnWindow.Kind.Delete);
            tuneWarn.FormClosed += (s, e) =>
            {
                bool ok = tuneWarn.Accepted; tuneWarn = null;
                if (!ok) return;
                Tune.Delete();
                if (!SelfTesting) SaveApp("tuning", false);
                if (tuneWin != null && !tuneWin.IsDisposed) tuneWin.Hide();
            };
            tuneWarn.Show();
            tuneWarn.Activate();
        }
    }
}
