// Ui.cs - dark "node editor" theme of the app (reference: node-based image-generation UI shown by the player).
// Owner-drawn WinForms controls: dotted canvas with bezier links, rounded node cards, pill buttons, toggle pills,
// status chips and a dark combo box. Sizes are logical pixels scaled by the window DPI (Ui.S).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace TheNorns
{
    static class Theme
    {
        // a page rebuilt in place: Controls.Clear() only detaches - the old cards kept their window handles, glass bitmaps,
        // timers and their place in Glass.Track (24.10.2026: switching pilots in the wizard got slower and slower)
        // keep = controls the form owns and puts on its pages again (detached first, never disposed)
        public static void ClearPage(Control page, params Control[] keep)
        {
            foreach (var k in keep) if (k != null && k.Parent != null && page.Contains(k)) k.Parent.Controls.Remove(k);
            var old = new List<Control>(); foreach (Control c in page.Controls) old.Add(c);
            page.Controls.Clear();
            foreach (var c in old) c.Dispose();
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern uint GetDpiForSystem();
        // UI scale: WinForms on .NET 4.8 reports DeviceDpi 96 unless PerMonitorV2 is configured, so ask Windows directly
        public static readonly float K = SystemDpi() / 96f;
        static float SystemDpi() { try { return GetDpiForSystem(); } catch { return 96; } }
        public static readonly Color Back = Color.FromArgb(18, 18, 20);
        public static readonly Color Dot = Color.FromArgb(40, 40, 44);
        public static readonly Color Card = Color.FromArgb(30, 30, 33);
        public static readonly Color CardInner = Color.FromArgb(38, 38, 42);
        public static readonly Color Border = Color.FromArgb(52, 52, 58);
        public static readonly Color Pill = Color.FromArgb(36, 36, 40);
        public static readonly Color PillHover = Color.FromArgb(50, 50, 56);
        public static readonly Color Text = Color.FromArgb(232, 232, 236);
        public static readonly Color Muted = Color.FromArgb(140, 140, 150);
        public static readonly Color Link = Color.FromArgb(78, 78, 86);
        public static readonly Color Yellow = Color.FromArgb(240, 222, 70);
        public static readonly Color Green = Color.FromArgb(84, 214, 120);
        public static readonly Color Red = Color.FromArgb(240, 86, 86);
        public static readonly Color Blue = Color.FromArgb(96, 156, 250);
        public static readonly Color Pink = Color.FromArgb(236, 92, 190);
        public static readonly Color Mint = Color.FromArgb(110, 230, 150);

        // Rounded rectangle with smoothed ("squircle") corners - Figma corner smoothing, as in cornerKit
        // (bejarcode.github.io/cornerKit): the curvature grows gradually out of the straight edge instead of a circular
        // arc starting abruptly. smoothing 0 = plain circular corner, 0.6 = iOS, 0.8..0.9 = cornerKit's cards / pills.
        public const float Smoothing = 0.8f;
        public static GraphicsPath Round(RectangleF r, float rad) { return Round(r, rad, Smoothing); }
        public static GraphicsPath Round(RectangleF r, float rad, float smoothing)
        {
            var p = new GraphicsPath();
            if (rad <= 0 || r.Width <= 0 || r.Height <= 0) { p.AddRectangle(r); return p; }
            foreach (var c in Corners(r, rad, smoothing))
            {
                p.AddBezier(c[0], c[1], c[2], c[3]);
                p.AddBezier(c[3], c[4], c[5], c[6]);
                p.AddBezier(c[6], c[7], c[8], c[9]);
            }
            p.CloseFigure();
            return p;
        }

        // Control points of the four corners (top-left, top-right, bottom-right, bottom-left; clockwise), each as three
        // cubic segments: edge -> arc start, the circular arc (as a bezier), arc end -> next edge.
        // Corner math from figma-squircle (Figma's "corner smoothing" write-up).
        public static List<PointF[]> Corners(RectangleF r, float rad, float smoothing)
        {
            float budget = Math.Min(r.Width, r.Height) / 2;
            float R = Math.Min(rad, budget), s = Math.Max(0, Math.Min(1, smoothing));
            if (R * (1 + s) > budget) s = Math.Max(0, budget / R - 1);   // no room to smooth: shrink smoothing first
            double P = Math.Min((1 + s) * R, budget);
            double arcDeg = 90 * (1 - s), arcLen = Math.Sin(arcDeg / 2 * Math.PI / 180) * R * Math.Sqrt(2);
            double alpha = (90 - arcDeg) / 2, p34 = R * Math.Tan(alpha / 2 * Math.PI / 180);
            double beta = 45 * s * Math.PI / 180, c = p34 * Math.Cos(beta), d = c * Math.Tan(beta);
            double b = (P - arcLen - c - d) / 3, a = 2 * b;
            double kArc = 4.0 / 3 * Math.Tan(arcDeg / 4 * Math.PI / 180) * R;   // bezier handle for a circular arc

            var res = new List<PointF[]>();
            // vertex, direction along the edge into the corner (u) and along the next edge (v)
            var corners = new[]
            {
                new[] { r.Left, r.Top, 0f, -1f, 1f, 0f }, new[] { r.Right, r.Top, 1f, 0f, 0f, 1f },
                new[] { r.Right, r.Bottom, 0f, 1f, -1f, 0f }, new[] { r.Left, r.Bottom, -1f, 0f, 0f, -1f },
            };
            foreach (var q in corners)
            {
                double vx = q[0], vy = q[1], ux = q[2], uy = q[3], wx = q[4], wy = q[5];
                Func<double, double, PointF> pt = (alongU, alongV) => new PointF((float)(vx + ux * alongU + wx * alongV), (float)(vy + uy * alongU + wy * alongV));
                // local coords: alongU measured towards the vertex (start at -P), alongV into the next edge
                var p0 = pt(-P, 0); var p1 = pt(-P + a, 0); var p2 = pt(-P + a + b, 0); var p3 = pt(-P + a + b + c, d);
                var p4 = pt(-P + a + b + c + arcLen, d + arcLen);
                var q1 = pt(-P + a + b + c + arcLen + d, d + arcLen + c); var q2 = pt(-P + a + b + c + arcLen + d, d + arcLen + b + c); var q3 = pt(0, P);
                // arc p3 -> p4: tangents continue the neighbouring beziers
                double t1x = p3.X - p2.X, t1y = p3.Y - p2.Y, l1 = Math.Sqrt(t1x * t1x + t1y * t1y);
                double t2x = q1.X - p4.X, t2y = q1.Y - p4.Y, l2 = Math.Sqrt(t2x * t2x + t2y * t2y);
                if (l1 < 1e-6) { t1x = ux; t1y = uy; l1 = 1; }
                if (l2 < 1e-6) { t2x = wx; t2y = wy; l2 = 1; }
                var a1 = new PointF((float)(p3.X + t1x / l1 * kArc), (float)(p3.Y + t1y / l1 * kArc));
                var a2 = new PointF((float)(p4.X - t2x / l2 * kArc), (float)(p4.Y - t2y / l2 * kArc));
                res.Add(new[] { p0, p1, p2, p3, a1, a2, p4, q1, q2, q3 });
            }
            return res;
        }

        // ---- one set of sizes and type for every window (UI review 25.10.2026: the same kind of control had 4-5 sizes) ----
        // heights in logical px (x K): dialog footer buttons (and what sits in that row), toolbar / action buttons and
        // switches, text fields (and a button right next to one), dense rows (lists, settings tables)
        public const int FooterH = 38, FooterMinW = 110, BarH = 34, FieldH = 36, RowH = 30;
        public const int Gap = 8, Pad = 16, Margin = 24;
        // type scale (pt): caption, secondary text, control text, body / hints, page heading
        public const float FCaption = 8f, FSmall = 9f, FControl = 9.25f, FBody = 9.5f, FH1 = 16f;
        public static int Px(float v) { return (int)(v * K); }

        // the buttons at the bottom of a dialog, laid right to left from `right`: one height, at least FooterMinW wide.
        // The caller passes only the buttons it shows (1.4.1: Control.Visible is false for every control until the form is
        // shown - the installer laid out its footer before that and placed no button at all)
        public static void Footer(int right, int top, params PillButton[] rightToLeft)
        {
            int x = right;
            foreach (var b in rightToLeft)
            {
                if (b == null) continue;
                int w = Math.Max(Px(FooterMinW), b.PreferredWidth());
                b.SetBounds(x - w, top, w, Px(FooterH)); x = b.Left - Px(Gap);
            }
        }

        // a wizard / dialog page head: step chips (optional) at the top, the heading, the hint under it; returns where the
        // page content starts (Setup, calibration, installer, Combat logs, Database)
        public static int Head(Label heading, Label hint, IList<Chip> steps, int m, int width)
        {
            int y = Px(18);
            if (steps != null && steps.Count > 0)
            {
                int x = m;
                foreach (var c in steps) { c.SetBounds(x, y, c.PreferredWidth(), Px(RowH)); x = c.Right + Px(Gap); }
                y += Px(RowH) + Px(Margin);
            }
            heading.Location = new Point(m - Px(2), y);
            y += Px(38);
            if (hint != null)
            {
                if (hint.AutoSize) hint.Location = new Point(m, y); else hint.SetBounds(m, y, width - 2 * m, Px(40));
                y += (hint.AutoSize ? hint.Height : Px(40)) + Px(Pad);
            }
            return y;
        }

        // the on / off switch of TogglePill and of the switchable rows in lists
        public static void PaintSwitch(Graphics g, RectangleF sw, bool on)
        {
            float s = K;
            using (var p = Round(sw, sw.Height / 2)) using (var b = new SolidBrush(on ? Mint : Border)) g.FillPath(b, p);
            using (var kb = new SolidBrush(on ? Color.FromArgb(20, 20, 22) : Muted)) g.FillEllipse(kb, on ? sw.Right - sw.Height + 2 * s : sw.X + 2 * s, sw.Y + 2 * s, sw.Height - 4 * s, sw.Height - 4 * s);
        }

        // summary tiles (label / value / note) in one row across `width` - Combat logs and Database; returns the height
        public static float PaintTiles(Graphics g, IList<Tuple<string, string, string, Color>> tiles, float y, float width)
        {
            float k = K, gap = Gap * k, tw = (width - gap * (tiles.Count - 1)) / tiles.Count, th = 62 * k;
            var nowrap = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            using (var fNum = new Font("Segoe UI Semibold", 12f)) using (var fLong = new Font("Segoe UI Semibold", FSmall)) using (var fLab = new Font("Segoe UI", FCaption))
            using (var mb = new SolidBrush(Muted))
                for (int i = 0; i < tiles.Count; i++)
                {
                    var r = new RectangleF(i * (tw + gap), y, tw, th);
                    using (var p = Round(r, 12 * k)) using (var b = new SolidBrush(CardInner)) using (var pen = new Pen(Border)) { g.FillPath(b, p); g.DrawPath(pen, p); }
                    g.DrawString(tiles[i].Item1, fLab, mb, new RectangleF(r.X + 12 * k, r.Y + 7 * k, r.Width - 16 * k, 16 * k), nowrap);
                    using (var vb = new SolidBrush(tiles[i].Item4))
                        g.DrawString(tiles[i].Item2, tiles[i].Item2.Length > 14 ? fLong : fNum, vb, new RectangleF(r.X + 11 * k, r.Y + 22 * k, r.Width - 14 * k, 22 * k), nowrap);
                    g.DrawString(tiles[i].Item3, fLab, mb, new RectangleF(r.X + 12 * k, r.Y + 42 * k, r.Width - 16 * k, 16 * k), nowrap);
                }
            return th;
        }

        public static void Smooth(Graphics g) { g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit; }
    }

    // Dotted background; draws bezier "links" between registered anchors (screen-independent: anchors are callbacks)
    class Canvas : Panel
    {
        public List<Func<Tuple<PointF, PointF>>> Links = new List<Func<Tuple<PointF, PointF>>>();
        public Canvas() { DoubleBuffered = true; BackColor = Theme.Back; ResizeRedraw = true; }
        // the window's resize border lies over the canvas: let the form answer there (AppChrome)
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84 /* WM_NCHITTEST */ && AppChrome.InResizeEdge(FindForm(), Cursor.Position)) { m.Result = (IntPtr)(-1) /* HTTRANSPARENT */; return; }
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Theme.Smooth(g);
            float step = 22 * Theme.K;
            using (var b = new SolidBrush(Theme.Dot))
                for (float y = step / 2; y < Height; y += step)
                    for (float x = step / 2; x < Width; x += step) g.FillRectangle(b, x, y, 1.6f, 1.6f);
            using (var pen = new Pen(Theme.Link, 1.4f * Theme.K))
                foreach (var l in Links)
                {
                    var ab = l(); if (ab == null) continue;
                    var a = ab.Item1; var c = ab.Item2; float dy = (c.Y - a.Y) * 0.5f;
                    g.DrawBezier(pen, a, new PointF(a.X, a.Y + dy), new PointF(c.X, c.Y - dy), c);
                }
        }
    }

    // Module card in a liquid-glass shell (Glass.cs): "● title" inside the glass, an 8 px glass rim around the inner
    // panel with rows of "label .... value ●"
    class NodeCard : Control
    {
        public string Title = ""; public Color TitleDot = Theme.Text;
        public List<Tuple<string, string, Color>> Rows = new List<Tuple<string, string, Color>>();
        public string Footer = ""; public Color FooterColor = Theme.Muted;
        // colour caught in the glass edge (Color.Empty = none): red while the card's EVE client is not found
        Color edgeTint = Color.Empty;
        public Color EdgeTint { get { return edgeTint; } set { if (edgeTint == value) return; edgeTint = value; Invalidate(); } }
        // colour of the pointer light in the glass (Color.Empty = yellow): red while the card's EVE client is not found
        Color pointerTint = Color.Empty;
        public Color PointerTint { get { return pointerTint; } set { if (pointerTint == value) return; pointerTint = value; Invalidate(); } }
        // height that fits the title and every row with no empty space below (UI review 23.10.2026)
        public int FitHeight(int rows) { return (int)Math.Ceiling(Glass.HeaderH + (12 + rows * 23 + 10) * S + Glass.Rim); }
        readonly Glass.Cache glass = new Glass.Cache();
        public NodeCard() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true); BackColor = Theme.Back; Glass.Track(this); }
        float S { get { return Theme.K; } }
        public PointF PortBottom { get { return new PointF(Left + Width / 2f, Bottom); } }
        protected override void Dispose(bool disposing) { if (disposing) glass.Dispose(); base.Dispose(disposing); }

        public void SetRow(string label, string value, Color dot)
        {
            int i = Rows.FindIndex(r => r.Item1 == label);
            var t = Tuple.Create(label, value, dot);
            if (i < 0) Rows.Add(t); else if (!Rows[i].Equals(t)) Rows[i] = t; else return;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; float s = S;
            g.CompositingMode = CompositingMode.SourceCopy;
            g.DrawImageUnscaled(glass.Get(this), 0, 0);
            g.CompositingMode = CompositingMode.SourceOver;
            Theme.Smooth(g);
            if (edgeTint != Color.Empty) Glass.PaintEdgeTint(g, this, edgeTint, 165, 40);
            Glass.PaintPointerGlow(g, this, Glass.HeaderH, pointerTint);
            Glass.PaintHeader(g, Title, TitleDot, Glass.HeaderH, Width);
            var inner = Glass.Inner(Size);
            if (Footer != "") inner.Height -= 30 * s;
            float rowH = 23 * s, y = inner.Y + 12 * s;
            using (var lf = new Font("Segoe UI", Theme.FSmall)) using (var mb = new SolidBrush(Theme.Muted)) using (var vb = new SolidBrush(Theme.Text))
            {
                var left = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                var right = new StringFormat { Alignment = StringAlignment.Far, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                foreach (var r in Rows)
                {
                    if (y + rowH > inner.Bottom) break;
                    g.DrawString(r.Item1, lf, mb, new RectangleF(inner.X + 12 * s, y, 76 * s, rowH), left);
                    g.DrawString(r.Item2, lf, vb, new RectangleF(inner.X + 90 * s, y, inner.Width - 120 * s, rowH), right);
                    using (var db = new SolidBrush(r.Item3)) g.FillEllipse(db, inner.Right - 20 * s, y + 5 * s, 7 * s, 7 * s);
                    y += rowH;
                }
                if (Footer != "")
                    using (var fb = new SolidBrush(FooterColor))
                        g.DrawString(Footer, lf, fb, new RectangleF(inner.X + 4 * s, inner.Bottom + 7 * s, inner.Width, 20 * s), new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            }
        }
    }

    // Rounded pill button. Primary = white with dark text (the reference's "Share"); Accent = coloured (the "Generate")
    class PillButton : Control
    {
        public bool Primary; public Color Accent = Color.Empty; public string Icon = "";
        bool hover, down;
        public PillButton(string text) { Text = text; DoubleBuffered = true; SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw, true); BackColor = Color.Transparent; Cursor = Glass.Pointer; }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnEnabledChanged(EventArgs e) { hover = false; down = false; Invalidate(); base.OnEnabledChanged(e); }

        public int PreferredWidth()
        {
            using (var g = CreateGraphics()) using (var f = new Font("Segoe UI", Theme.FControl))
            {
                // English width (Loc.cs); a very short English label still leaves room for a translation (72 px of text)
                float w = g.MeasureString((Icon != "" ? Icon + "  " : "") + L.En(Text), f).Width;
                if (L.Code != "en" && L.En(Text) != Text) w = Math.Max(w, 72 * Theme.K);
                return (int)(w + 34 * Theme.K);
            }
        }

        protected virtual Color Fill()
        {
            // disabled (no EVE client found): a flat dim pill, no hover, muted text - the control already refuses clicks
            // UI review 23.10.2026: every disabled button looks the same - an accent colour no longer reads as "available"
            if (!Enabled) return Theme.Pill;
            if (Primary) return down ? Color.FromArgb(210, 210, 214) : hover ? Color.FromArgb(240, 240, 244) : Color.White;
            if (Accent != Color.Empty) return down ? ControlPaint.Dark(Accent, 0.1f) : hover ? ControlPaint.Light(Accent, 0.2f) : Accent;
            return down ? Theme.Border : hover ? Theme.PillHover : Theme.Pill;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Theme.Smooth(g); float s = Theme.K;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            using (var p = Theme.Round(r, 12 * s)) using (var b = new SolidBrush(Fill())) { g.FillPath(b, p); if (!Enabled || (!Primary && Accent == Color.Empty)) using (var pen = new Pen(Theme.Border)) g.DrawPath(pen, p); }
            var tc = Primary || Accent != Color.Empty ? Color.FromArgb(20, 20, 22) : Theme.Text;
            var label = (Icon != "" ? Icon + "  " : "") + Text;
            var tr = new RectangleF(r.X + 8 * s, r.Y, r.Width - 16 * s, r.Height);
            // a longer translation: a slightly smaller font first, then cut with "…" and shown whole as a tooltip (Loc.cs)
            using (var f = new Font("Segoe UI", Theme.FControl)) using (var tb = new SolidBrush(Enabled ? tc : Theme.Muted))
            {
                L.SetTip(this, L.Trimmed(g, label, f, tr.Width) ? Text : null);
                g.DrawString(label, f, tb, tr, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            }
        }
    }

    // Pill with an on/off switch; keeps CheckBox-like Checked/CheckedChanged so the form logic stays the same
    class TogglePill : PillButton
    {
        bool on; public event EventHandler CheckedChanged;
        public bool Checked { get { return on; } set { if (on == value) return; on = value; Invalidate(); if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty); } }
        public TogglePill(string text) : base(text) { }
        protected override void OnClick(EventArgs e) { Checked = !Checked; base.OnClick(e); }
        public new int PreferredWidth() { return base.PreferredWidth() + (int)(34 * Theme.K); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Theme.Smooth(g); float s = Theme.K;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            using (var p = Theme.Round(r, 12 * s)) using (var b = new SolidBrush(Fill())) using (var pen = new Pen(Theme.Border)) { g.FillPath(b, p); g.DrawPath(pen, p); }
            var sw = new RectangleF(12 * s, Height / 2f - 7 * s, 26 * s, 14 * s);
            Theme.PaintSwitch(g, sw, on);
            var tr = new RectangleF(sw.Right + 8 * s, 0, Width - sw.Right - 14 * s, Height);
            using (var f = new Font("Segoe UI", Theme.FControl)) using (var tb = new SolidBrush(Theme.Text))
            {
                L.SetTip(this, L.Trimmed(g, Text, f, tr.Width) ? Text : null);
                g.DrawString(Text, f, tb, tr, new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            }
        }
    }

    // Segmented control: one rounded track, the chosen segment filled mint (a mutually exclusive choice, UI review 23.10.2026)
    class Segmented : Control
    {
        readonly string[] items; int sel, hover = -1;
        public event EventHandler Changed;
        public int Selected { get { return sel; } set { if (sel == value) return; sel = value; Invalidate(); if (Changed != null) Changed(this, EventArgs.Empty); } }
        public void SetSilently(int i) { if (sel != i) { sel = i; Invalidate(); } }
        public Segmented(params string[] items) { this.items = items; DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); Cursor = Glass.Pointer; }
        float SegW(Graphics g, Font f, int i) { return g.MeasureString(L.En(items[i]), f).Width + 28 * Theme.K; }   // English width (Loc.cs)
        public int PreferredWidth() { using (var g = CreateGraphics()) using (var f = new Font("Segoe UI", Theme.FControl)) return (int)(Enumerable.Range(0, items.Length).Sum(i => SegW(g, f, i)) + 8 * Theme.K); }
        // the segments as laid out: a control wider than its English width fills it (player 25.10.2026) - by the shown
        // texts when they all fit, else in the English proportions (a longer text then ends in "…" with a tooltip)
        float[] Segs(Graphics g, Font f)
        {
            float avail = Width - 8 * Theme.K;
            var shown = items.Select(t => g.MeasureString(t, f).Width + 28 * Theme.K).ToArray();
            var w = shown.Sum() <= avail ? shown : items.Select((t, i) => SegW(g, f, i)).ToArray();
            float sum = w.Sum();
            if (sum > 0 && avail > 0 && Math.Abs(avail - sum) > 0.5f) for (int i = 0; i < w.Length; i++) w[i] *= avail / sum;
            return w;
        }
        int At(int x) { using (var g = CreateGraphics()) using (var f = new Font("Segoe UI", Theme.FControl)) { var ws = Segs(g, f); float sx = 4 * Theme.K; for (int i = 0; i < items.Length; i++) { if (x >= sx && x < sx + ws[i]) return i; sx += ws[i]; } } return -1; }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            int h = At(e.X); if (h != hover) { hover = h; Invalidate(); }
            bool cut = false;
            if (h >= 0) using (var g = CreateGraphics()) using (var f = new Font("Segoe UI", Theme.FControl)) cut = L.Trimmed(g, items[h], f, Segs(g, f)[h] - 12 * Theme.K);
            L.SetTip(this, cut ? items[h] : null);
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { int i = At(e.X); if (i >= 0) Selected = i; base.OnMouseDown(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Theme.Smooth(g); float s = Theme.K;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            using (var p = Theme.Round(r, 12 * s)) using (var b = new SolidBrush(Theme.Pill)) using (var pen = new Pen(Theme.Border)) { g.FillPath(b, p); g.DrawPath(pen, p); }
            using (var f = new Font("Segoe UI", Theme.FControl))
            {
                float x = 4 * s; var ws = Segs(g, f);
                for (int i = 0; i < items.Length; i++)
                {
                    float w = ws[i];
                    var sr = new RectangleF(x, 4 * s, w, Height - 8 * s);
                    if (i == sel || i == hover)
                        using (var p = Theme.Round(sr, 9 * s)) using (var b = new SolidBrush(i == sel ? Theme.Mint : Theme.PillHover)) g.FillPath(b, p);
                    using (var tb = new SolidBrush(i == sel ? Color.FromArgb(20, 20, 22) : Theme.Text)) using (var ff = new Font("Segoe UI", Theme.FControl))
                        g.DrawString(items[i], ff, tb, new RectangleF(sr.X + 6 * s, sr.Y, sr.Width - 12 * s, sr.Height), new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                    x += w;
                }
            }
        }
    }

    // Text tab with an underline (second-level tabs inside a step, e.g. the guide pages): a different look from the step
    // pills above it, so the two levels don't read as one row (UI review 23.10.2026)
    class UnderlineTab : Control
    {
        public bool Selected; bool hover;
        public UnderlineTab(string text) { Text = text; DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); Cursor = Glass.Pointer; }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        public int PreferredWidth()
        {
            using (var g = CreateGraphics()) using (var f = new Font("Segoe UI", Theme.FBody))
            {
                float w = g.MeasureString(L.En(Text), f).Width;
                if (L.Code != "en" && L.En(Text) != Text) w = Math.Max(w, 90 * Theme.K);   // room for a translation (Loc.cs)
                return (int)(w + 20 * Theme.K);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Theme.Smooth(g); float s = Theme.K;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            using (var f = new Font("Segoe UI", Theme.FBody)) using (var tb = new SolidBrush(Selected ? Theme.Text : hover ? Color.FromArgb(200, 200, 208) : Theme.Muted))
            {
                L.SetTip(this, L.Trimmed(g, Text, f, Width - 4 * s) ? Text : null);
                g.DrawString(Text, f, tb, new RectangleF(2 * s, 0, Width - 4 * s, Height - 4 * s), new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            }
            if (Selected || hover)
                using (var p = Theme.Round(new RectangleF(8 * s, Height - 4 * s, Width - 16 * s, 3 * s), 1.5f * s)) using (var b = new SolidBrush(Selected ? Theme.Mint : Theme.Border)) g.FillPath(b, p);
        }
    }

    // Small rounded chip with a coloured dot (status, bookmark queue); ForeColor = dot colour
    class Chip : Control
    {
        public Chip() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); ForeColor = Theme.Muted; }
        // Flat = plain "● text" without the pill (information, not a control - UI review 23.10.2026)
        public bool Flat;
        // Current = the step you are on: a filled mint pill with dark text (setup steps, UI review 23.10.2026)
        bool current;
        public bool Current { get { return current; } set { if (current == value) return; current = value; Invalidate(); } }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnForeColorChanged(EventArgs e) { Invalidate(); base.OnForeColorChanged(e); }
        public int PreferredWidth() { using (var g = CreateGraphics()) using (var f = new Font("Segoe UI", Theme.FSmall)) return (int)(g.MeasureString(L.En(Text), f).Width + 44 * Theme.K); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Theme.Smooth(g); float s = Theme.K;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            if (!Flat) using (var p = Theme.Round(r, Height / 2f)) using (var b = new SolidBrush(current ? Theme.Mint : Theme.Card)) using (var pen = new Pen(current ? Theme.Mint : Theme.Border)) { g.FillPath(b, p); g.DrawPath(pen, p); }
            using (var db = new SolidBrush(current ? Color.FromArgb(20, 20, 22) : ForeColor)) g.FillEllipse(db, 12 * s, Height / 2f - 4 * s, 8 * s, 8 * s);
            using (var f = new Font("Segoe UI", Theme.FSmall)) using (var tb = new SolidBrush(current ? Color.FromArgb(20, 20, 22) : Theme.Text))
            {
                L.SetTip(this, L.Trimmed(g, Text, f, Width - 30 * s) ? Text : null);
                g.DrawString(Text, f, tb, new RectangleF(26 * s, 0, Width - 30 * s, Height), new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            }
        }
    }

    // Dark drop-down in the app's style (site selection, ship type) - same API as ComboBox. The closed face is a pill
    // like PillButton (rounded, dark fill, thin border, chevron). The open list is our own layered popup (DarkComboPopup)
    // with anti-aliased rounded corners and a soft shadow - the Windows list can't round its corners on Windows 10.
    class DarkCombo : ComboBox
    {
        bool hover; DarkComboPopup popup;
        public bool IsOpen { get { return popup != null && !popup.IsDisposed; } }
        // optional (UI review 23.10.2026): a group header per run of items (e.g. "C5" / "C6"), the text shown in the list
        // (without the group prefix) and a coloured dot (e.g. the site type)
        public Func<int, string> GroupOf, TextOf;
        public Func<int, Color> DotOf;
        public string Placeholder;   // shown muted while nothing is selected

        public DarkCombo()
        {
            DrawMode = DrawMode.OwnerDrawFixed; DropDownStyle = ComboBoxStyle.DropDownList; FlatStyle = FlatStyle.Flat;
            BackColor = Theme.Pill; ForeColor = Theme.Text; Cursor = Glass.Pointer;
            ItemHeight = (int)(26 * Theme.K);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnSelectedIndexChanged(EventArgs e) { Invalidate(); base.OnSelectedIndexChanged(e); }
        protected override void OnHandleDestroyed(EventArgs e) { ClosePopup(); base.OnHandleDestroyed(e); }

        public void ShowPopup()
        {
            if (IsOpen || Items.Count == 0) return;
            if (!Focused) Focus();
            popup = new DarkComboPopup(this);
            popup.Disposed += (s, e) => { popup = null; Invalidate(); };
            popup.Open();
            Invalidate();
        }
        public void ClosePopup() { if (IsOpen) popup.Dismiss(); }
        internal void Pick(int i) { ClosePopup(); if (i >= 0 && i < Items.Count && i != SelectedIndex) SelectedIndex = i; }

        const int WM_LBUTTONDOWN = 0x201, WM_LBUTTONDBLCLK = 0x203, WM_KEYDOWN = 0x100, WM_SYSKEYDOWN = 0x104, WM_MOUSEWHEEL = 0x20A, WM_KILLFOCUS = 0x8;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONDBLCLK)
            {
                if (IsOpen) ClosePopup(); else ShowPopup();
                return;   // never the Windows list
            }
            if (m.Msg == WM_KEYDOWN || m.Msg == WM_SYSKEYDOWN)
            {
                var key = (Keys)(int)m.WParam;
                if (key == Keys.F4 || (m.Msg == WM_SYSKEYDOWN && (key == Keys.Down || key == Keys.Up))) { if (IsOpen) ClosePopup(); else ShowPopup(); return; }
                if (IsOpen)
                {
                    if (key == Keys.Escape) { ClosePopup(); return; }
                    if (key == Keys.Enter || key == Keys.Space) { if (popup.HoverItem >= 0) Pick(popup.HoverItem); return; }
                    if (key == Keys.Down || key == Keys.Up || key == Keys.PageDown || key == Keys.PageUp || key == Keys.Home || key == Keys.End) { popup.Key(key); return; }
                }
            }
            if (m.Msg == WM_MOUSEWHEEL && IsOpen) { popup.Wheel((short)((long)m.WParam >> 16)); return; }
            if (m.Msg == WM_KILLFOCUS && IsOpen) popup.CloseSoon();
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Theme.Smooth(g); float k = Theme.K; bool open = IsOpen;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            using (var p = Theme.Round(r, Math.Min(12 * k, Height / 2f))) using (var b = new SolidBrush(open ? Theme.Border : hover ? Theme.PillHover : Theme.Pill))
            using (var pen = new Pen(open ? Color.FromArgb(150, Theme.Mint) : Theme.Border, open ? 1.2f * k : 1f)) { g.FillPath(b, p); g.DrawPath(pen, p); }
            var txt = SelectedItem != null ? SelectedItem.ToString() : !string.IsNullOrEmpty(Placeholder) ? Placeholder : Text;
            using (var tb = new SolidBrush(Enabled && SelectedItem != null ? Theme.Text : Theme.Muted))
                g.DrawString(txt, Font, tb, new RectangleF(14 * k, 0, Width - 44 * k, Height),
                    new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            float cx = Width - 20 * k, cy = Height / 2f + (open ? -1.5f : 1.5f) * k, d = 4 * k;
            using (var pen = new Pen(hover || open ? Theme.Text : Theme.Muted, 1.6f * k) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                g.DrawLines(pen, open ? new[] { new PointF(cx - d, cy + d / 2), new PointF(cx, cy - d / 2), new PointF(cx + d, cy + d / 2) }
                                      : new[] { new PointF(cx - d, cy - d / 2), new PointF(cx, cy + d / 2), new PointF(cx + d, cy - d / 2) });
        }
        protected override void OnDrawItem(DrawItemEventArgs e) { }   // everything is drawn by OnPaint / the popup
    }

    // The open list of a DarkCombo: borderless layered window (per-pixel alpha, UpdateLayeredWindow) that never takes
    // the focus. Rounded body (radius 12, cornerKit smoothing) with a soft shadow, rows with a rounded hover pill and a
    // mint dot on the current item, up to 10 rows then wheel / keys scroll with a thin thumb. Closes on a pick, Esc,
    // a click anywhere else, or when the window it belongs to loses focus / moves.
    class DarkComboPopup : Form, IMessageFilter
    {
        readonly DarkCombo combo; readonly Form owner;
        public int Hover; int top; readonly int rows, rowH, pad, shadow;   // Hover / top: positions in the entry list
        // the rows: an item index (>= 0) or a group header (-1 - header number)
        readonly List<int> entry = new List<int>(); readonly List<string> heads = new List<string>();
        public int HoverItem { get { return Hover >= 0 && Hover < entry.Count ? entry[Hover] : -1; } }
        static float K { get { return Theme.K; } }

        public DarkComboPopup(DarkCombo c)
        {
            combo = c; owner = c.FindForm();
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual;
            rowH = c.ItemHeight + (int)(4 * K); pad = (int)(5 * K); shadow = (int)(10 * K);
            string last = null;
            for (int i = 0; i < c.Items.Count; i++)
            {
                var g = c.GroupOf != null ? c.GroupOf(i) : null;
                if (g != null && g != last) { heads.Add(g); entry.Add(-heads.Count); last = g; }
                entry.Add(i);
            }
            rows = Math.Min(10, entry.Count);
            Hover = Math.Max(0, entry.IndexOf(Math.Max(0, c.SelectedIndex)));
            top = Math.Max(0, Math.Min(Hover - rows / 2, entry.Count - rows));
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        const int WS_EX_LAYERED = 0x80000, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x8000000, WS_EX_TOPMOST = 0x8;
        protected override CreateParams CreateParams { get { var cp = base.CreateParams; cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST; return cp; } }

        Rectangle Body { get { return new Rectangle(shadow, shadow, Width - 2 * shadow, Height - 2 * shadow); } }

        public void Open()
        {
            Place();
            // shown with Win32 calls only: WinForms' Show(owner) activates the window despite WS_EX_NOACTIVATE, the main
            // window then deactivates and the list closed the moment it appeared (18.10.2026, "the menu just blinks")
            var h = Handle;
            if (owner != null) SetWindowLongPtr(h, -8 /* GWLP_HWNDPARENT: owner */, owner.Handle);
            Render();
            SetWindowPos(h, (IntPtr)(-1) /* HWND_TOPMOST */, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 | 0x0040 /* NOSIZE NOMOVE NOACTIVATE SHOWWINDOW */);
            Application.AddMessageFilter(this);
            if (owner != null) { owner.Deactivate += OwnerGone; owner.Move += OwnerGone; owner.Resize += OwnerGone; }
        }

        bool gone;
        public void Dismiss()
        {
            if (gone) return; gone = true;
            Application.RemoveMessageFilter(this);
            if (owner != null) { owner.Deactivate -= OwnerGone; owner.Move -= OwnerGone; owner.Resize -= OwnerGone; }
            Dispose();
        }
        void DismissLater() { if (!gone && IsHandleCreated) BeginInvoke((Action)Dismiss); }

        // screen bounds under the combo face (or above it when the screen ends)
        public void Place()
        {
            int w = combo.Width, h = rows * rowH + 2 * pad;
            var at = combo.PointToScreen(new Point(0, combo.Height + (int)(4 * K)));
            var wa = Screen.FromControl(combo).WorkingArea;
            if (at.Y + h > wa.Bottom) at = combo.PointToScreen(new Point(0, -h - (int)(4 * K)));   // no room below: open upwards
            Bounds = new Rectangle(at.X - shadow, at.Y - shadow, w + 2 * shadow, h + 2 * shadow);
        }
        void OwnerGone(object s, EventArgs e) { DismissLater(); }
        // the combo lost the keyboard focus: close unless it got it back right away
        public void CloseSoon() { if (!gone && IsHandleCreated) BeginInvoke((Action)(() => { if (!gone && !combo.Focused) Dismiss(); })); }

        // a click outside the list and outside the combo face closes it (the face toggles it itself)
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x201 || m.Msg == 0x204 || m.Msg == 0x207 || m.Msg == 0xA1 || m.Msg == 0xA4)
            {
                var pt = Cursor.Position;
                bool inList = RectangleToScreen(Body).Contains(pt), inFace = combo.RectangleToScreen(combo.ClientRectangle).Contains(pt);
                if (!inList && !inFace) DismissLater();
            }
            return false;
        }

        int RowAt(Point p) { var b = Body; if (!b.Contains(p)) return -1; int i = (p.Y - b.Top - pad) / rowH; return i >= 0 && i < rows && top + i < entry.Count ? top + i : -1; }
        protected override void OnMouseMove(MouseEventArgs e) { int i = RowAt(e.Location); if (i >= 0 && i != Hover && entry[i] >= 0) { Hover = i; Render(); } base.OnMouseMove(e); }
        protected override void OnMouseDown(MouseEventArgs e) { int i = RowAt(e.Location); if (i >= 0) { if (entry[i] >= 0) combo.Pick(entry[i]); } else Dismiss(); }
        protected override void OnMouseWheel(MouseEventArgs e) { Wheel(e.Delta); }
        const int WM_MOUSEACTIVATE = 0x21, MA_NOACTIVATE = 3;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE) { m.Result = (IntPtr)MA_NOACTIVATE; return; }   // clicks never activate the list
            base.WndProc(ref m);
        }
        public void Wheel(int delta) { int n = entry.Count; top = Math.Max(0, Math.Min(n - rows, top - Math.Sign(delta) * 3)); Render(); }
        public void Key(Keys k)
        {
            int n = entry.Count, dir = k == Keys.Up || k == Keys.PageUp || k == Keys.Home ? -1 : 1;
            Hover = k == Keys.Down ? Hover + 1 : k == Keys.Up ? Hover - 1 : k == Keys.PageDown ? Hover + rows : k == Keys.PageUp ? Hover - rows : k == Keys.Home ? 0 : n - 1;
            Hover = Math.Max(0, Math.Min(n - 1, Hover));
            while (entry[Hover] < 0 && Hover + dir >= 0 && Hover + dir < n) Hover += dir;   // headers are not picked
            if (entry[Hover] < 0) Hover = entry.FindIndex(x => x >= 0);
            if (Hover < top) top = Hover; else if (Hover >= top + rows) top = Hover - rows + 1;
            Render();
        }

        public Bitmap Draw()
        {
            float k = K; var bmp = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                Theme.Smooth(g); g.Clear(Color.Transparent);
                var body = new RectangleF(shadow + 0.5f, shadow + 0.5f, Width - 2 * shadow - 1, Height - 2 * shadow - 1);
                // soft shadow: a few widening rounded outlines, fading out
                for (int i = shadow; i >= 1; i--)
                {
                    var r = RectangleF.Inflate(body, i, i); r.Offset(0, 2 * k);
                    using (var p = Theme.Round(r, 12 * k + i, Glass.Smooth)) using (var b = new SolidBrush(Color.FromArgb((int)(5 + 22 * (1 - i / (float)shadow) * (1 - i / (float)shadow)), 0, 0, 0))) g.FillPath(b, p);
                }
                using (var p = Theme.Round(body, 12 * k, Glass.Smooth)) using (var b = new SolidBrush(Theme.Card)) using (var pen = new Pen(Theme.Border, 1f)) { g.FillPath(b, p); g.DrawPath(pen, p); }
                var font = combo.Font;
                int n = entry.Count;
                for (int i = 0; i < rows; i++)
                {
                    int pos = top + i; if (pos >= n) break;
                    var rr = new RectangleF(body.X + pad, body.Y + pad + i * rowH, body.Width - 2 * pad - (n > rows ? 10 * k : 0), rowH);
                    int idx = entry[pos];
                    if (idx < 0)
                    {
                        // group header: the class, muted, with a hairline
                        using (var hf = new Font("Segoe UI Semibold", Theme.FSmall)) using (var hb = new SolidBrush(Theme.Muted))
                            g.DrawString(heads[-idx - 1], hf, hb, new RectangleF(rr.X + 10 * k, rr.Y, rr.Width, rr.Height), new StringFormat { LineAlignment = StringAlignment.Center });
                        using (var pen = new Pen(Theme.Border)) g.DrawLine(pen, rr.X + 40 * k, rr.Y + rr.Height / 2, rr.Right - 6 * k, rr.Y + rr.Height / 2);
                        continue;
                    }
                    if (pos == Hover) using (var p = Theme.Round(RectangleF.Inflate(rr, 0, -1.5f * k), 8 * k)) using (var b = new SolidBrush(Theme.PillHover)) g.FillPath(b, p);
                    bool current = idx == combo.SelectedIndex;
                    var dot = combo.DotOf != null ? combo.DotOf(idx) : Theme.Mint;
                    if (current || combo.DotOf != null) using (var db = new SolidBrush(dot)) g.FillEllipse(db, rr.X + 9 * k, rr.Y + rowH / 2f - 3 * k, 6 * k, 6 * k);
                    var text = combo.TextOf != null ? combo.TextOf(idx) : combo.Items[idx].ToString();
                    using (var tf = current && combo.DotOf != null ? new Font(font, FontStyle.Bold) : new Font(font, FontStyle.Regular))
                    using (var tb = new SolidBrush(current || pos == Hover ? Theme.Text : Color.FromArgb(200, 200, 208)))
                        g.DrawString(text, tf, tb, new RectangleF(rr.X + 22 * k, rr.Y, rr.Width - 46 * k, rr.Height),
                            new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                    if (current && combo.DotOf != null)   // the chosen one: a tick on the right (the dot is the type now)
                        using (var pen = new Pen(Theme.Mint, 1.8f * k) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                        {
                            float cx = rr.Right - 14 * k, cy = rr.Y + rowH / 2f;
                            g.DrawLines(pen, new[] { new PointF(cx - 5 * k, cy), new PointF(cx - 1.5f * k, cy + 3.5f * k), new PointF(cx + 5 * k, cy - 4 * k) });
                        }
                }
                // more rows than fit: a visible thumb on a faint track, and a fade over the last row while more is below
                if (n > rows)
                {
                    float trackH = body.Height - 2 * pad - 8 * k, th = Math.Max(24 * k, trackH * rows / n), ty = body.Y + pad + 4 * k + (trackH - th) * top / (float)(n - rows);
                    using (var p = Theme.Round(new RectangleF(body.Right - pad - 6 * k, body.Y + pad + 4 * k, 5 * k, trackH), 2.5f * k)) using (var b = new SolidBrush(Color.FromArgb(40, 255, 255, 255))) g.FillPath(b, p);
                    using (var p = Theme.Round(new RectangleF(body.Right - pad - 6 * k, ty, 5 * k, th), 2.5f * k)) using (var b = new SolidBrush(Color.FromArgb(150, 150, 160))) g.FillPath(b, p);
                    if (top + rows < n)
                    {
                        var fr = new RectangleF(body.X + 2, body.Bottom - pad - rowH, body.Width - 4 - 14 * k, rowH);
                        using (var lg = new LinearGradientBrush(fr, Color.FromArgb(0, Theme.Card), Color.FromArgb(230, Theme.Card), LinearGradientMode.Vertical)) g.FillRectangle(lg, fr);
                    }
                }
            }
            return bmp;
        }

        void Render()
        {
            if (!IsHandleCreated) return;
            using (var bmp = Draw()) AppChrome.PresentLayered(this, bmp);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SetWindowLongPtr(IntPtr h, int i, IntPtr v);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, int flags);
    }

    // Thin scroll bar in the app's style for a multiline TextBox (log, fits): the box runs without its Windows scroll
    // bar, this control sits in the gutter right of it and follows the box's first visible line (EM_GETFIRSTVISIBLELINE);
    // drag the thumb or click the track to scroll (EM_LINESCROLL). The wheel scrolls the box natively.
    class ThinScroll : Control
    {
        readonly TextBox box; readonly Timer poll = new Timer { Interval = 120 };
        int first, total, visible; bool hover, drag; float dragY0; int dragFirst0;
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        const int EM_GETFIRSTVISIBLELINE = 0xCE, EM_GETLINECOUNT = 0xBA, EM_LINESCROLL = 0xB6;
        public static float Gutter { get { return 12 * Theme.K; } }   // room to leave right of the box

        public static ThinScroll Attach(TextBox tb)
        {
            tb.ScrollBars = ScrollBars.None;
            var s = new ThinScroll(tb);
            EventHandler place = (o, e) =>
            {
                if (tb.Parent == null) return;
                if (s.Parent != tb.Parent) { tb.Parent.Controls.Add(s); s.BringToFront(); }
                s.SetBounds(tb.Right + (int)(3 * Theme.K), tb.Top, (int)(8 * Theme.K), tb.Height);
            };
            tb.ParentChanged += place; tb.Resize += place; tb.Move += place; place(null, null);
            // the wheel over the text itself: an edit control without its own scroll bar ignores it (24.10.2026: the
            // installer's license didn't scroll with the wheel)
            tb.MouseWheel += (o, e) => { s.Sync(); s.ScrollToLine(s.first - e.Delta / 120 * 3); var h = e as HandledMouseEventArgs; if (h != null) h.Handled = true; };
            return s;
        }

        ThinScroll(TextBox tb)
        {
            box = tb; DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); Cursor = Glass.Pointer;
            BackColor = tb.BackColor;
            poll.Tick += (o, e) => Sync(); poll.Start();
            tb.TextChanged += (o, e) => Sync();
        }
        protected override void Dispose(bool disposing) { if (disposing) poll.Dispose(); base.Dispose(disposing); }

        internal void Sync()
        {
            if (!box.IsHandleCreated || !Visible) return;
            int f = (int)SendMessage(box.Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
            int t = (int)SendMessage(box.Handle, EM_GETLINECOUNT, IntPtr.Zero, IntPtr.Zero);
            int v = Math.Max(1, box.ClientSize.Height / Math.Max(1, box.Font.Height));
            if (f != first || t != total || v != visible) { first = f; total = t; visible = v; Invalidate(); }
        }
        int MaxFirst { get { return Math.Max(0, total - visible); } }
        RectangleF Thumb()
        {
            if (total <= visible) return RectangleF.Empty;
            float h = Math.Max(24 * Theme.K, Height * (float)visible / total), y = (Height - h) * Math.Min(1f, first / (float)Math.Max(1, MaxFirst));
            float w = (hover || drag ? 6 : 4) * Theme.K;
            return new RectangleF((Width - w) / 2, y, w, h);
        }
        internal void ScrollToLine(int line)
        {
            line = Math.Max(0, Math.Min(MaxFirst, line));
            SendMessage(box.Handle, EM_LINESCROLL, IntPtr.Zero, (IntPtr)(line - first));
            Sync();
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            var t = Thumb(); if (t.IsEmpty) return;
            if (e.Y >= t.Top && e.Y <= t.Bottom) { drag = true; dragY0 = e.Y; dragFirst0 = first; Capture = true; }
            else ScrollToLine(first + (e.Y < t.Top ? -visible : visible));
            Invalidate(); base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (drag) { var t = Thumb(); float span = Height - t.Height; if (span > 0) ScrollToLine(dragFirst0 + (int)Math.Round((e.Y - dragY0) / span * MaxFirst)); }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e) { drag = false; Capture = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnMouseWheel(MouseEventArgs e) { ScrollToLine(first - e.Delta / 120 * 3); base.OnMouseWheel(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Theme.Smooth(g);
            var t = Thumb(); if (t.IsEmpty) return;
            using (var p = Theme.Round(t, t.Width / 2)) using (var b = new SolidBrush(drag ? Color.FromArgb(150, 150, 160) : hover ? Color.FromArgb(120, 120, 130) : Color.FromArgb(80, 80, 90))) g.FillPath(b, p);
        }
    }
}

namespace TheNorns
{
    // ---------- window chrome in the app's style (UI review 23.10.2026: no Windows title bars) ----------
    // AppChrome.Apply(form, resizable, minimize): the form loses the Windows frame and gets a TitleBar docked on top
    // (icon, title, minimize / maximize / close drawn like the rest of the app), a 1 px outline, the taskbar's minimize /
    // restore (WS_SYSMENU / WS_MINIMIZEBOX styles), resizing by its edges and a maximize that leaves the taskbar visible.
    static class AppChrome
    {
        public static float TitleH { get { return 38 * Theme.K; } }
        public static readonly Color Stroke = Color.FromArgb(48, 48, 55);    // window outline, barely visible
        public static readonly Color TitleBack = Color.FromArgb(27, 27, 31); // title strip: a tone lighter than the canvas
        static float Edge { get { return 6 * Theme.K; } }
        static readonly Dictionary<Form, bool> forms = new Dictionary<Form, bool>();   // form -> resizable

        public static TitleBar Apply(Form f, bool resizable, bool minimize = true)
        {
            var cs = f.ClientSize;
            f.FormBorderStyle = FormBorderStyle.None;
            // a 1 px stroke on every side: the form's own colour shows through a 1 px padding around the title and canvas
            f.Padding = new Padding(1); f.BackColor = Stroke;
            f.ClientSize = new Size(cs.Width + 2, cs.Height + (int)TitleH + 2);
            var tb = new TitleBar(f, resizable, minimize);
            f.Controls.Add(tb); tb.SendToBack();   // docked first: the top strip; the canvas fills the rest
            forms[f] = resizable;
            f.Disposed += (s, e) => forms.Remove(f);
            f.HandleCreated += (s, e) => Init(f, resizable, minimize);
            if (f.IsHandleCreated) Init(f, resizable, minimize);
            return tb;
        }

        public static bool InResizeEdge(Form f, Point screen)
        {
            bool r;
            if (f == null || !forms.TryGetValue(f, out r) || !r || f.WindowState != FormWindowState.Normal) return false;
            var p = f.PointToClient(screen);
            return p.X < Edge || p.Y < Edge || p.X >= f.ClientSize.Width - Edge || p.Y >= f.ClientSize.Height - Edge;
        }


        static void Init(Form f, bool resizable, bool minimize)
        {
            var h = f.Handle;
            const int GWL_STYLE = -16, WS_SYSMENU = 0x80000, WS_MINIMIZEBOX = 0x20000, WS_MAXIMIZEBOX = 0x10000;
            int st = GetWindowLong(h, GWL_STYLE) | WS_SYSMENU | (minimize ? WS_MINIMIZEBOX : 0) | (resizable ? WS_MAXIMIZEBOX : 0);
            SetWindowLong(h, GWL_STYLE, st);
            // no DWM frame extension: on Windows 10 its 1 px strip is drawn in the system frame colour (a white line on one
            // side); the window's outline is our own 1 px stroke instead (UI review 23.10.2026)
            new Hook(f).AssignHandle(h);
        }

        // the form's own hit test: the edges resize it (the canvas / title bar pass those points through)
        class Hook : NativeWindow
        {
            readonly Form f;
            public Hook(Form f) { this.f = f; f.HandleDestroyed += (s, e) => ReleaseHandle(); }
            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if (m.Msg == 0x24 /* WM_GETMINMAXINFO */)
                {   // maximized = the monitor's working area (a borderless window would cover the taskbar)
                    var sc = Screen.FromHandle(f.Handle);
                    var mmi = (MINMAXINFO)System.Runtime.InteropServices.Marshal.PtrToStructure(m.LParam, typeof(MINMAXINFO));
                    mmi.maxPosX = sc.WorkingArea.X - sc.Bounds.X; mmi.maxPosY = sc.WorkingArea.Y - sc.Bounds.Y;
                    mmi.maxSizeX = sc.WorkingArea.Width; mmi.maxSizeY = sc.WorkingArea.Height;
                    System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, m.LParam, false);
                    return;
                }
                if (m.Msg != 0x84) return;
                bool r; if (!forms.TryGetValue(f, out r) || !r || f.WindowState != FormWindowState.Normal) return;
                var p = f.PointToClient(new Point((short)(m.LParam.ToInt64() & 0xFFFF), (short)((m.LParam.ToInt64() >> 16) & 0xFFFF)));
                float e = Edge; int w = f.ClientSize.Width, hh = f.ClientSize.Height;
                bool L = p.X < e, R = p.X >= w - e, T = p.Y < e, B = p.Y >= hh - e;
                int ht = T && L ? 13 : T && R ? 14 : B && L ? 16 : B && R ? 17 : L ? 10 : R ? 11 : T ? 12 : B ? 15 : 0;
                if (ht != 0) m.Result = (IntPtr)ht;
            }
        }

        struct MINMAXINFO { public int resX, resY, maxSizeX, maxSizeY, maxPosX, maxPosY, minTrackX, minTrackY, maxTrackX, maxTrackY; }
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int i);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h, int i, int v);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] struct POINT { public int x, y; }
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] struct SIZE { public int cx, cy; }
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)] struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr dst, ref POINT pt, ref SIZE size, IntPtr src, ref POINT srcPt, int key, ref BLENDFUNCTION blend, int flags);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern int ReleaseDC(IntPtr h, IntPtr dc);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr o);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr o);

        public static void PresentLayered(Form f, Bitmap bmp, byte alpha = 255)
        {
            if (!f.IsHandleCreated) return;
            IntPtr screen = GetDC(IntPtr.Zero), mem = CreateCompatibleDC(screen), hbm = bmp.GetHbitmap(Color.FromArgb(0)), old = SelectObject(mem, hbm);
            try
            {
                var size = new SIZE { cx = bmp.Width, cy = bmp.Height }; var src = new POINT(); var pos = new POINT { x = f.Left, y = f.Top };
                var blend = new BLENDFUNCTION { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = alpha, AlphaFormat = 1 };
                UpdateLayeredWindow(f.Handle, screen, ref pos, ref size, mem, ref src, 0, ref blend, 2);
            }
            finally { SelectObject(mem, old); DeleteObject(hbm); DeleteDC(mem); ReleaseDC(IntPtr.Zero, screen); }
        }
    }

    // the title strip: icon, title (dimmed while the window is inactive), minimize / maximize-restore / close
    class TitleBar : Control
    {
        readonly Form f; readonly bool resizable, minimize;
        int hover = -1, down = -1;   // 0 minimize, 1 maximize, 2 close
        float K { get { return Theme.K; } }
        public TitleBar(Form f, bool resizable, bool minimize)
        {
            this.f = f; this.resizable = resizable; this.minimize = minimize;
            DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); BackColor = AppChrome.TitleBack; Cursor = Glass.Pointer;
            Dock = DockStyle.Top; Height = (int)AppChrome.TitleH;
            f.TextChanged += (s, e) => Invalidate(); f.Activated += (s, e) => Invalidate(); f.Deactivate += (s, e) => Invalidate();
            f.Resize += (s, e) => Invalidate();
        }
        float BtnW { get { return 46 * K; } }
        bool Has(int i) { return i == 2 || (i == 1 && resizable) || (i == 0 && minimize); }
        RectangleF Btn(int i)
        {   // packed to the right: close, then maximize (if any), then minimize (if any)
            int slot = 0; for (int j = 2; j > i; j--) if (Has(j)) slot++;
            return new RectangleF(Width - BtnW * (slot + 1), 0, BtnW, Height);
        }
        int HitBtn(Point p) { for (int i = 0; i < 3; i++) if (Has(i) && Btn(i).Contains(p)) return i; return -1; }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84 && AppChrome.InResizeEdge(f, Cursor.Position) && HitBtn(PointToClient(Cursor.Position)) < 0) { m.Result = (IntPtr)(-1); return; }
            base.WndProc(ref m);
        }
        protected override void OnMouseMove(MouseEventArgs e) { int h = HitBtn(e.Location); if (h != hover) { hover = h; Invalidate(); } base.OnMouseMove(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            int b = HitBtn(e.Location);
            if (b >= 0) { down = b; Invalidate(); return; }
            if (e.Clicks == 2 && resizable) { ToggleMax(); return; }
            ReleaseCapture(); SendMessage(f.Handle, 0xA1 /* WM_NCLBUTTONDOWN */, (IntPtr)2 /* HTCAPTION */, IntPtr.Zero);   // drag the window
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            int b = HitBtn(e.Location), d = down; down = -1; Invalidate();
            if (d < 0 || b != d) return;
            if (d == 0) f.WindowState = FormWindowState.Minimized;
            else if (d == 1) ToggleMax();
            else f.Close();
        }
        void ToggleMax() { f.WindowState = f.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Theme.Smooth(g); float k = K;
            g.Clear(BackColor);
            using (var pen = new Pen(AppChrome.Stroke)) g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            bool active = Form.ActiveForm == f;
            float x = 14 * k;
            if (f.Icon != null) { g.DrawIcon(f.Icon, new Rectangle((int)x, (int)((Height - 16 * k) / 2), (int)(16 * k), (int)(16 * k))); x += 26 * k; }
            using (var fnt = new Font("Segoe UI", Theme.FSmall)) using (var tb = new SolidBrush(active ? Color.FromArgb(200, 200, 208) : Theme.Muted))
                g.DrawString(f.Text, fnt, tb, new RectangleF(x, 0, Width - x - BtnW * 3 - 8 * k, Height), new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            for (int i = 0; i < 3; i++)
            {
                if (!Has(i)) continue;
                var r = Btn(i);
                if (hover == i || down == i)
                    using (var b = new SolidBrush(i == 2 ? Color.FromArgb(down == i ? 200 : 235, Theme.Red) : (down == i ? Theme.Border : Theme.PillHover))) g.FillRectangle(b, r);
                float cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2, d = 5 * k;
                using (var pen = new Pen(hover == i && i == 2 ? Color.White : active ? Theme.Text : Theme.Muted, 1.2f * k))
                {
                    if (i == 0) g.DrawLine(pen, cx - d, cy, cx + d, cy);
                    else if (i == 1)
                    {
                        if (f.WindowState == FormWindowState.Maximized)
                        {   // restore: two overlapping squares
                            g.DrawRectangle(pen, cx - d, cy - d + 2 * k, 2 * d - 2 * k, 2 * d - 2 * k);
                            g.DrawLines(pen, new[] { new PointF(cx - d + 2 * k, cy - d), new PointF(cx + d, cy - d), new PointF(cx + d, cy + d - 2 * k) });
                        }
                        else g.DrawRectangle(pen, cx - d, cy - d, 2 * d, 2 * d);
                    }
                    else { g.DrawLine(pen, cx - d, cy - d, cx + d, cy + d); g.DrawLine(pen, cx - d, cy + d, cx + d, cy - d); }
                }
            }
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
    }
}
