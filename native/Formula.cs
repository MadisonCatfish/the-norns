// Formula.cs - the formula window: a click on a title's function badge opens a small OLED display in a thin rim of the
// hull. It slides in from below with its perspective easing from tilted back to flat, then floats over the body's
// bottom right corner, covering 85 % of it, with a soft shadow under it; the text says in plain words how the number
// and the graph are computed. Its Close key is the body's; Esc closes it too (owner, 27.10.2026).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace TheNorns
{
    class FormulaText
    {
        public string Title, Body;   // Body: English, paragraphs split by "\n" (translated through L.T)
    }

    class FormulaWindow : Form
    {
        public static readonly FormulaText Vorton = new FormulaText
        {
            Title = "Eff. DPS · Vorton Projector",
            Body = "How much damage your Vorton Projectors actually put on the rats, per second.\n" +
                   "The big number adds up every Vorton hit on NPCs in all your pilots' combat logs over the last 10 seconds and divides it by 10.\n" +
                   "Each line is one pilot: their Vorton damage over the last minute, smoothed a little so a single volley does not spike the curve.\n" +
                   "Effective means what really landed after resists and misses, not the paper DPS of the fit.\n" +
                   "Arcs that jump onto your own fleet, the anchor included, do not count."
        };
        public static readonly FormulaText Smartbombs = new FormulaText
        {
            Title = "Eff. DPS · Smartbombs",
            Body = "How hard each of your smartbombs hits in this site.\n" +
                   "All smartbombs go off together, the log fills with hits, then it goes quiet until the next cycle. A pause longer than 6 seconds ends a cycle.\n" +
                   "The number of modules comes from the log itself: every rat in range takes one hit from each smartbomb, so the hits on one rat show how many fired. After a fleet warp two full cycles have to agree before the columns appear.\n" +
                   "Each column is one module: this cycle's damage against that module's best cycle in this site. A full column is as good as it gets here; a short one means fewer rats in range or rats that died mid-cycle.\n" +
                   "The number on the right is the damage of the last cycle divided by the cycle time, usually about 9 seconds.\n" +
                   "The columns glide from one cycle to the next instead of jumping."
        };

        static FormulaWindow current;
        public static void Open(Form main, FormulaText text)
        {
            if (current != null && !current.IsDisposed) { current.closing = false; current.Close(); }
            current = new FormulaWindow(main, text);
            current.Show(main);
        }

        // ---- geometry: the card is 85 % of the main window and floats out past its bottom right corner ----
        static float U(float v) { return v * Theme.K * DpsForm.UiScale; }
        static float Margin { get { return U(26); } }   // room for the shadow and the perspective
        static SizeF CardSize(Size main) { return new SizeF(main.Width * 0.85f, main.Height * 0.85f); }
        static PointF CardOffset(Size main) { var c = CardSize(main); return new PointF(main.Width - c.Width + U(12), main.Height - c.Height + U(12)); }
        static RectangleF CloseIn(SizeF card) { return new RectangleF(card.Width - U(3.5f) - U(9) - U(14.2f), U(3.5f) + U(9), U(14.2f), U(28)); }

        readonly FormulaText text;
        readonly Size cardPx;
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly Timer timer = new Timer { Interval = 15 };
        Bitmap card;
        bool hoverClose, downClose, closing = true;   // closing: a Close() call runs the closing animation first
        double closeStart = -1;
        const double OpenSecs = 0.42, CloseSecs = 0.22;

        FormulaWindow(Form main, FormulaText text)
        {
            this.text = text;
            var cs = CardSize(main.Size); cardPx = new Size((int)cs.Width, (int)cs.Height);
            var off = CardOffset(main.Size);
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; TopMost = true;
            AutoScaleMode = AutoScaleMode.None; KeyPreview = true; Text = L.T(text.Title);
            Size = new Size(cardPx.Width + 2 * (int)Margin, cardPx.Height + 2 * (int)Margin);
            Location = new Point(main.Left + (int)(off.X - Margin), main.Top + (int)(off.Y - Margin));
            timer.Tick += (s, e) => Render();
            timer.Start();
        }

        protected override CreateParams CreateParams { get { var cp = base.CreateParams; cp.ExStyle |= 0x80000 /* WS_EX_LAYERED */ | 0x80 /* TOOLWINDOW */; return cp; } }
        protected override void OnShown(EventArgs e) { base.OnShown(e); Render(); }
        protected override void Dispose(bool disposing) { if (disposing) { timer.Dispose(); if (card != null) card.Dispose(); } base.Dispose(disposing); }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (closing && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                if (closeStart < 0) { closeStart = clock.Elapsed.TotalSeconds; timer.Start(); }
                return;
            }
            if (current == this) current = null;
            base.OnFormClosing(e);
        }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Escape) Close(); }

        RectangleF CardRect { get { return new RectangleF(Margin, Margin, cardPx.Width, cardPx.Height); } }
        RectangleF CloseRect { get { var c = CloseIn(cardPx); c.Offset(Margin, Margin); return c; } }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84 /* WM_NCHITTEST */)
            {
                var p = PointToClient(new Point((short)(m.LParam.ToInt64() & 0xFFFF), (short)((m.LParam.ToInt64() >> 16) & 0xFFFF)));
                m.Result = (IntPtr)(CardRect.Contains(p) ? 1 /* HTCLIENT */ : -1 /* HTTRANSPARENT */);
                return;
            }
            if (m.Msg == 0x20 /* WM_SETCURSOR */ && (short)(m.LParam.ToInt64() & 0xFFFF) == 1)
            {
                Cursor.Current = DpsForm.Pointer; m.Result = (IntPtr)1; return;
            }
            base.WndProc(ref m);
        }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); bool h = CloseRect.Contains(e.Location); if (h != hoverClose) { hoverClose = h; Redraw(); } }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); if (hoverClose || downClose) { hoverClose = downClose = false; Redraw(); } }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left && CloseRect.Contains(e.Location)) { downClose = true; Redraw(); } }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            bool was = downClose; downClose = false; Redraw();
            if (was && e.Button == MouseButtons.Left && CloseRect.Contains(e.Location)) Close();
        }
        void Redraw() { if (card != null) { card.Dispose(); card = null; } Render(); }

        void Render()
        {
            double t = clock.Elapsed.TotalSeconds, p;
            if (DpsForm.Potato) t += 10;   // Potato mode: no slide in or out, the card just shows and goes
            if (closeStart >= 0)
            {
                p = 1 - Math.Min(1, (t - closeStart) / CloseSecs);
                if (p <= 0) { timer.Stop(); closing = false; Close(); return; }
            }
            else
            {
                p = Math.Min(1, t / OpenSecs);
                if (p >= 1) timer.Stop();
            }
            if (card == null) card = DrawCard(cardPx, text, hoverClose, downClose);
            using (var bmp = Frame(card, Size, Ease(p))) AppChrome.PresentLayered(this, bmp);
        }

        static double Ease(double p) { p = Math.Max(0, Math.Min(1, p)); return 1 - Math.Pow(1 - p, 3); }

        // the whole layered window at animation state e (0 = below and tilted back, 1 = in place): the shadow under the card,
        // then the card drawn row by row, each row a little narrower towards the top (a perspective GDI+ can't do in one call)
        static Bitmap Frame(Bitmap card, Size window, double e)
        {
            var bmp = new Bitmap(window.Width, window.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float m = Margin, alpha = (float)Math.Min(1, e * 1.6);
                float dy = (float)((1 - e) * U(70));
                // the shadow: lies under the floating card, wider and softer the higher the card floats
                var sr = new RectangleF(m + U(4), m + U(10) + dy * 0.4f, card.Width - U(8), card.Height - U(4));
                for (int i = 0; i < 6; i++)
                {
                    var r = RectangleF.Inflate(sr, U(2.5f) * i, U(2.5f) * i);
                    using (var path = Theme.Round(r, U(12) + U(2.5f) * i, 0.6f))
                    using (var b = new SolidBrush(Color.FromArgb((int)(alpha * (34 - i * 5)), 0, 0, 0))) g.FillPath(b, path);
                }
                if (e >= 0.999) { g.DrawImageUnscaled(card, (int)m, (int)m); return bmp; }
                float top = (float)(0.72 + 0.28 * e), sy = (float)(0.88 + 0.12 * e);
                float h = card.Height * sy, cx = m + card.Width / 2f, y0 = m + card.Height - h + dy;
                g.InterpolationMode = InterpolationMode.HighQualityBilinear; g.PixelOffsetMode = PixelOffsetMode.Half;
                using (var ia = new ImageAttributes())
                {
                    ia.SetColorMatrix(new ColorMatrix { Matrix33 = alpha });
                    ia.SetWrapMode(WrapMode.TileFlipXY);
                    for (int r = 0; r < (int)Math.Ceiling(h); r++)
                    {
                        float f = top + (1 - top) * (r / h), w = card.Width * f;
                        int src = Math.Min(card.Height - 1, (int)(r / sy));
                        g.DrawImage(card, new Rectangle((int)(cx - w / 2), (int)(y0 + r), (int)Math.Round(w), 1), 0, src, card.Width, 1, GraphicsUnit.Pixel, ia);
                    }
                }
            }
            return bmp;
        }

        // the card: a thin rim of the hull around a black OLED glass; the title in Orbitron caps, the text in Space Grotesk
        // (Segoe UI in Ukrainian - the embedded fonts have no Cyrillic), made smaller until it fits
        static Bitmap DrawCard(Size size, FormulaText text, bool hoverClose, bool downClose)
        {
            var bmp = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                var all = new RectangleF(0, 0, size.Width, size.Height);
                using (var body = Theme.Round(all, U(20 * 0.6f), 0.6f))
                {
                    using (var b = new SolidBrush(BodyTheme.Hull)) g.FillPath(b, body);
                    using (var lg = new LinearGradientBrush(all, Color.White, Color.Black, 80f))
                    {
                        lg.InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(40, 255, 255, 255), Color.FromArgb(5, 255, 255, 255), Color.FromArgb(70, 0, 0, 0) }, Positions = new[] { 0f, 0.45f, 1f } };
                        g.FillPath(lg, body);
                    }
                    var edge = all; edge.Inflate(-0.5f, -0.5f);
                    using (var ep = Theme.Round(edge, U(20 * 0.6f), 0.6f))
                    using (var lg = new LinearGradientBrush(all, Color.FromArgb(80, 255, 255, 255), Color.FromArgb(120, 0, 0, 0), 90f))
                    using (var pen = new Pen(lg, 1f)) g.DrawPath(pen, ep);
                }
                var glass = RectangleF.Inflate(all, -U(3.5f), -U(3.5f));
                using (var gp = Theme.Round(glass, U(17 * 0.6f), 0.6f))
                {
                    using (var b = new SolidBrush(Color.Black)) g.FillPath(b, gp);
                    using (var pen = new Pen(Color.FromArgb(60, 0, 0, 0), U(1))) g.DrawPath(pen, gp);
                }

                var close = CloseIn(size);
                DpsForm.CloseKey(g, close, hoverClose, downClose, U(1));

                // title: the function symbol in the accent, then the section's name
                float pad = U(12), x = glass.Left + pad, ty = close.Top + close.Height / 2;
                var tf = NornsFonts.Screen(U(9.5f));
                var ir = new RectangleF(x, ty - U(5.5f), U(11), U(11));
                using (var ab = new SolidBrush(BodyTheme.Accent)) MaterialIcon.DrawSymbol(g, MaterialIcon.Function, ir, ab);
                using (var tb = new SolidBrush(BodyTheme.Ink))
                    g.DrawString(text.Title.ToUpperInvariant(), tf, tb, new RectangleF(ir.Right + U(6), ty - tf.GetHeight(g) / 2, close.Left - ir.Right - U(12), tf.GetHeight(g) + U(2)),
                        new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                float lineY = close.Bottom + U(8);
                using (var pen = new Pen(Color.FromArgb(34, BodyTheme.Ink), 1f)) g.DrawLine(pen, x, lineY, glass.Right - pad, lineY);

                // the text: the first paragraph brighter and larger (what it is), the rest as plain paragraphs
                var paras = L.T(text.Body).Split('\n');
                var area = RectangleF.FromLTRB(x, lineY + U(9), glass.Right - pad, glass.Bottom - pad);
                bool latin = L.Code != "uk";
                for (float scale = 1f; scale > 0.6f; scale -= 0.04f)
                {
                    Font lead = latin ? NornsFonts.Body(U(8.6f) * scale) : new Font("Segoe UI", U(8.2f) * scale, GraphicsUnit.Pixel);
                    Font body = latin ? NornsFonts.Body(U(7.6f) * scale) : new Font("Segoe UI", U(7.3f) * scale, GraphicsUnit.Pixel);
                    float y = area.Top, gap = U(5.5f) * scale;
                    var heights = new List<float>();
                    for (int i = 0; i < paras.Length; i++) { float hh = g.MeasureString(paras[i], i == 0 ? lead : body, (int)area.Width).Height; heights.Add(hh); y += hh + gap; }
                    if (y - gap <= area.Bottom || scale <= 0.64f)
                    {
                        y = area.Top;
                        using (var lb = new SolidBrush(BodyTheme.Ink)) using (var bb = new SolidBrush(Color.FromArgb(186, 189, 196)))
                            for (int i = 0; i < paras.Length; i++)
                            {
                                g.DrawString(paras[i], i == 0 ? lead : body, i == 0 ? lb : bb, new RectangleF(area.Left, y, area.Width, heights[i] + U(2)));
                                y += heights[i] + gap;
                            }
                        if (!latin) { lead.Dispose(); body.Dispose(); }
                        break;
                    }
                    if (!latin) { lead.Dispose(); body.Dispose(); }
                }
            }
            return bmp;
        }

        // selftest: the main window's image with the formula window over it at animation state p, on a dark backdrop
        public static void SaveDemo(Bitmap main, FormulaText text, double p, string path)
        {
            var cs = CardSize(main.Size); var size = new Size((int)cs.Width, (int)cs.Height);
            var off = CardOffset(main.Size);
            float m = Margin, o = U(30);
            var win = new Size(size.Width + 2 * (int)m, size.Height + 2 * (int)m);
            int W = (int)(o + Math.Max(main.Width, off.X - m + win.Width) + o / 2), H = (int)(o + Math.Max(main.Height, off.Y - m + win.Height) + o / 2);
            using (var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            using (var card = DrawCard(size, text, false, false))
            using (var frame = Frame(card, win, Ease(p)))
            {
                g.Clear(Color.FromArgb(38, 40, 46));
                g.DrawImageUnscaled(main, (int)o, (int)o);
                g.DrawImageUnscaled(frame, (int)(o + off.X - m), (int)(o + off.Y - m));
                bmp.Save(path, ImageFormat.Png);
            }
        }
    }
}
