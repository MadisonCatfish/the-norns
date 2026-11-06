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
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern uint GetDpiForSystem();
        // UI scale: WinForms on .NET 4.8 reports DeviceDpi 96 unless PerMonitorV2 is configured, so ask Windows directly
        public static readonly float K = SystemDpi() / 96f;
        static float SystemDpi() { try { return GetDpiForSystem(); } catch { return 96; } }
        public static readonly Color Card = Color.FromArgb(30, 30, 33);
        public static readonly Color CardInner = Color.FromArgb(38, 38, 42);
        public static readonly Color Border = Color.FromArgb(52, 52, 58);
        public static readonly Color Text = Color.FromArgb(232, 232, 236);
        public static readonly Color Yellow = Color.FromArgb(240, 222, 70);
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

        public const float FBody = 9.5f;   // body / hints (pt): the installer's text fields

        public static void Smooth(Graphics g) { g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit; }
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
    // a layered window shown from a bitmap with per-pixel alpha (the body, the dock, the formula window)
    static class AppChrome
    {
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

}
