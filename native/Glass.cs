// Glass.cs - the app's own mouse pointer: an electric-blue arrow, and the pulsing ring under it over keys.
// (The 1.0 "liquid glass" module shells that lived here went with the 1.0 look in 1.5.3.)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TheNorns
{
    static class Glass
    {
        static float K { get { return Theme.K; } }
        static readonly Color PointerColor = Color.FromArgb(250, 226, 60);   // the 1.0 yellow arrow (Glass.Pointer)

        // ---------- the yellow pointer ----------

        [StructLayout(LayoutKind.Sequential)] struct ICONINFO { public bool fIcon; public int xHotspot, yHotspot; public IntPtr hbmMask, hbmColor; }
        [DllImport("user32.dll")] static extern IntPtr CreateIconIndirect(ref ICONINFO info);
        [DllImport("gdi32.dll")] static extern IntPtr CreateBitmap(int w, int h, uint planes, uint bpp, IntPtr bits);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr o);

        static Cursor pointer;
        public static Cursor Pointer
        {
            get
            {
                if (pointer != null) return pointer;
                try { pointer = MakePointer(); } catch { pointer = Cursors.Default; }
                return pointer;
            }
        }

        // over keys (owner, 27.10.2026): the app's own arrow with a ring pulsing out from its tip underneath; the ring
        // rests for the first 0.3 of the loop, then spreads and fades. t = 0..1 of one loop
        public const double PulseLoop = 1.2;   // seconds
        const float PulseTip = 10;             // the arrow's tip in the cursor (units), = the hot spot
        public static Bitmap PulseFrame(double t, Color fillColor, Color outlineColor, float s)
        {
            int size = (int)Math.Ceiling(32 * s);
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.Transparent);
                if (t > 0.3)
                {
                    double r = (t - 0.3) / 0.7, e = 1 - (1 - r) * (1 - r);
                    float rad = (2.5f + 6.5f * (float)e) * s;
                    using (var pen = new Pen(Color.FromArgb((int)(200 * (1 - r)), fillColor), 2.8f * s))
                        g.DrawEllipse(pen, PulseTip * s - rad, PulseTip * s - rad, 2 * rad, 2 * rad);
                }
                DrawArrow(g, PulseTip - 3, fillColor, outlineColor, s);
            }
            return bmp;
        }

        // one frame of the pulsing arrow as a cursor, the hot spot on the tip
        public static Cursor MakePulse(double t, Color fillColor, Color outlineColor)
        {
            float s = K;
            using (var bmp = PulseFrame(t, fillColor, outlineColor, s))
            {
                var info = new ICONINFO { fIcon = false, xHotspot = (int)(PulseTip * s), yHotspot = (int)(PulseTip * s), hbmColor = bmp.GetHbitmap(Color.FromArgb(0, 0, 0, 0)), hbmMask = CreateBitmap(bmp.Width, bmp.Height, 1, 1, IntPtr.Zero) };
                var h = CreateIconIndirect(ref info);
                DeleteObject(info.hbmColor); DeleteObject(info.hbmMask);
                if (h == IntPtr.Zero) return Cursors.Default;
                return new Cursor(h);
            }
        }

        // the arrow, moved by `off` units right and down (its tip at 3 + off)
        static void DrawArrow(Graphics g, float off, Color fillColor, Color outlineColor, float s)
        {
            var pts = new[] { new PointF(3, 3), new PointF(21, 9.5f), new PointF(12.2f, 12.2f), new PointF(9.5f, 21) };
            for (int i = 0; i < pts.Length; i++) pts[i] = new PointF((pts[i].X + off) * s, (pts[i].Y + off) * s);
            using (var path = new GraphicsPath())
            {
                path.AddPolygon(pts);
                using (var m = new Matrix()) { m.Translate(0.8f * s, 1.2f * s); var sh = (GraphicsPath)path.Clone(); sh.Transform(m);
                    using (var sp = new Pen(Color.FromArgb(70, 0, 0, 0), 3.2f * s) { LineJoin = LineJoin.Round }) g.DrawPath(sp, sh); sh.Dispose(); }   // soft shadow
                using (var outline = new Pen(outlineColor, 2.6f * s) { LineJoin = LineJoin.Round }) g.DrawPath(outline, path);
                using (var fill = new SolidBrush(fillColor)) using (var round = new Pen(fillColor, 1.6f * s) { LineJoin = LineJoin.Round })
                { g.FillPath(fill, path); g.DrawPath(round, path); }
            }
        }

        // arrow like the reference's collaborator cursors: tip at the top-left, notched tail, rounded corners
        static Cursor MakePointer() { return MakePointer(PointerColor, Color.FromArgb(150, 40, 32, 0)); }

        // the same arrow in another colour (the Norns body: electric blue with a dark navy outline)
        public static Cursor MakePointer(Color fillColor, Color outlineColor)
        {
            float s = K;
            int size = (int)Math.Ceiling(26 * s);
            using (var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.Transparent);
                    DrawArrow(g, 0, fillColor, outlineColor, s);
                }
                var info = new ICONINFO { fIcon = false, xHotspot = (int)(3 * s), yHotspot = (int)(3 * s), hbmColor = bmp.GetHbitmap(Color.FromArgb(0, 0, 0, 0)), hbmMask = CreateBitmap(size, size, 1, 1, IntPtr.Zero) };
                var h = CreateIconIndirect(ref info);
                DeleteObject(info.hbmColor); DeleteObject(info.hbmMask);
                if (h == IntPtr.Zero) return Cursors.Default;
                return new Cursor(h);
            }
        }
    }
}
