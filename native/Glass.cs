// Glass.cs - "liquid glass" module shells (player reference: node UI + CodePen shingidad/jEPxMgW shader) and the
// yellow multiplayer-style mouse pointer whose colour lights up the glass rim near it.
//  * shell: rounded glass frame with the module title inside it (header) and an 8 px rim around the inner panel;
//  * glass (per pixel, cached per size / position): the dotted canvas behind it refracted like a lens - pulled in
//    from further inside near the edge, a little more for blue than red (prism fringes) - softly frosted, lit from
//    the top, with a bright thin ring just inside the edge and a fainter one at the bottom (the shader's rb1/rb2/rb3);
//    the inner panel sits recessed in the glass;
//  * pointer: a bright yellow arrow cursor; within reach its colour glows in the glass, strongest on the edge ring,
//    with orange / mint fringes (redrawn when the mouse moves, ~60 Hz).
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
        public static float Rim { get { return 8 * K; } }          // glass edge around the inner panel
        public static float HeaderH { get { return 42 * K; } }     // title row inside the glass
        public static float OuterR { get { return 24 * K; } }      // cornerKit cards: radius 24, smoothing 0.85
        public static float InnerR { get { return 16 * K; } }      // concentric with the shell: 24 - 8 px rim
        public const float Smooth = 0.85f;
        public static readonly Color PointerColor = Color.FromArgb(250, 226, 60);

        public static RectangleF Inner(Size s) { return Inner(s, HeaderH); }
        public static RectangleF Inner(Size s, float headerH) { return new RectangleF(Rim, headerH, s.Width - 2 * Rim, s.Height - headerH - Rim); }

        public static GraphicsPath ShellPath(Size s) { return Theme.Round(new RectangleF(0, 0, s.Width, s.Height), OuterR, Smooth); }
        public static GraphicsPath InnerPath(Size s, float headerH) { return Theme.Round(Inner(s, headerH), InnerR, Smooth); }

        // ---------- cached glass image ----------

        // Header = title row height (device px), InnerColor = inner panel fill. Behind the glass: the dot grid when the
        // control sits directly on a Canvas, else its parent's plain background.
        public class Cache
        {
            public float Header = HeaderH; public Color InnerColor = Theme.CardInner;
            public Bitmap Get(Control c)
            {
                bool dots = c.Parent is Canvas; var back = dots || c.Parent == null ? Theme.Back : c.Parent.BackColor;
                return Shared(c.Size, c.Location, Header, InnerColor, dots, back);
            }
            // the bitmaps belong to the shared cache (a rebuilt page draws the same cards again)
            public void Dispose() { }
        }

        // the last rendered cards by size, place and colours: a page rebuilt in place (wizard pilot switch, setup steps)
        // reuses them instead of rendering again. Evicted bitmaps are disposed: every caller draws what Get returns at once in
        // its OnPaint, on the one UI thread, so nothing still holds an evicted one (checked in the review of 26.10.2026).
        // A caller that keeps the bitmap (BackgroundImage, another thread) must copy it.
        static readonly Dictionary<string, Bitmap> shared = new Dictionary<string, Bitmap>();
        static readonly LinkedList<string> sharedOrder = new LinkedList<string>();
        static Bitmap Shared(Size size, Point at, float headerH, Color ic, bool dots, Color back)
        {
            var key = string.Join(",", size.Width, size.Height, dots ? at.X : 0, dots ? at.Y : 0, headerH, ic.ToArgb(), dots, back.ToArgb(), K);
            Bitmap b;
            if (shared.TryGetValue(key, out b)) { sharedOrder.Remove(key); sharedOrder.AddFirst(key); return b; }
            b = Render(size, at, headerH, ic, dots, back);
            shared[key] = b; sharedOrder.AddFirst(key);
            while (sharedOrder.Count > 32)
            {
                var kOld = sharedOrder.Last.Value; sharedOrder.RemoveLast();
                Bitmap old;
                if (shared.TryGetValue(kOld, out old)) { shared.Remove(kOld); if (old != null && old != b) old.Dispose(); }
            }
            return b;
        }

        // Signed distance (negative inside) and outward normal for a rectangle with smoothed corners (Theme.Corners):
        // straight parts analytically, corners against the sampled corner curve.
        class Shape
        {
            readonly float x0, y0, x1, y1;
            readonly float[] ext = new float[4];       // how far each corner curve reaches along its edges
            readonly PointF[][] curve = new PointF[4][];

            public Shape(RectangleF r, float rad, float smoothing)
            {
                x0 = r.Left; y0 = r.Top; x1 = r.Right; y1 = r.Bottom;
                var verts = new[] { new PointF(x0, y0), new PointF(x1, y0), new PointF(x1, y1), new PointF(x0, y1) };
                var cs = Theme.Corners(r, rad, smoothing);
                for (int i = 0; i < 4; i++)
                {
                    var c = cs[i];
                    ext[i] = (float)Math.Sqrt((c[0].X - verts[i].X) * (c[0].X - verts[i].X) + (c[0].Y - verts[i].Y) * (c[0].Y - verts[i].Y));
                    const int n = 10;
                    var pts = new List<PointF> { c[0] };
                    for (int seg = 0; seg < 3; seg++)
                        for (int j = 1; j <= n; j++)
                        {
                            float t = j / (float)n, m = 1 - t;
                            PointF A = c[seg * 3], B = c[seg * 3 + 1], C = c[seg * 3 + 2], D = c[seg * 3 + 3];
                            pts.Add(new PointF(m * m * m * A.X + 3 * m * m * t * B.X + 3 * m * t * t * C.X + t * t * t * D.X,
                                               m * m * m * A.Y + 3 * m * m * t * B.Y + 3 * m * t * t * C.Y + t * t * t * D.Y));
                        }
                    curve[i] = pts.ToArray();
                }
            }

            public float Dist(float px, float py, out float nx, out float ny)
            {
                int ci = px < x0 + ext[0] && py < y0 + ext[0] ? 0 : px > x1 - ext[1] && py < y0 + ext[1] ? 1
                       : px > x1 - ext[2] && py > y1 - ext[2] ? 2 : px < x0 + ext[3] && py > y1 - ext[3] ? 3 : -1;
                if (ci < 0)
                {
                    float dl = x0 - px, dr = px - x1, dt = y0 - py, db = py - y1, m = Math.Max(Math.Max(dl, dr), Math.Max(dt, db));
                    nx = m == dl ? -1 : m == dr ? 1 : 0; ny = nx != 0 ? 0 : m == dt ? -1 : 1;
                    return m;
                }
                var pts = curve[ci];
                float best = float.MaxValue, cx = 0, cy = 0, sx = 1, sy = 0;
                for (int i = 0; i + 1 < pts.Length; i++)
                {
                    float ax = pts[i].X, ay = pts[i].Y, ex = pts[i + 1].X - ax, ey = pts[i + 1].Y - ay, l2 = ex * ex + ey * ey;
                    float t = l2 > 0 ? Math.Max(0, Math.Min(1, ((px - ax) * ex + (py - ay) * ey) / l2)) : 0;
                    float qx = ax + ex * t, qy = ay + ey * t, d2 = (px - qx) * (px - qx) + (py - qy) * (py - qy);
                    if (d2 < best) { best = d2; cx = qx; cy = qy; sx = ex; sy = ey; }
                }
                float sl = (float)Math.Sqrt(sx * sx + sy * sy); if (sl > 0) { sx /= sl; sy /= sl; }
                float onx = sy, ony = -sx;                       // outward: the path runs clockwise (y down)
                float dist = (float)Math.Sqrt(best), sign = (px - cx) * onx + (py - cy) * ony >= 0 ? 1 : -1;
                if (dist > 0.3f) { nx = sign * (px - cx) / dist; ny = sign * (py - cy) / dist; } else { nx = onx; ny = ony; }
                return sign * dist;
            }
        }

        // the canvas behind a card: background + dot grid (Canvas.OnPaint), box-filtered with half-width hw
        static float Dots(float x, float y, float hw)
        {
            float step = 22 * K, d = 1.6f;
            float fx = Mod(x - step / 2, step), fy = Mod(y - step / 2, step);
            return Cover(fx, hw, step, d) * Cover(fy, hw, step, d) / (4 * hw * hw);
        }
        static float Mod(float a, float m) { a %= m; return a < 0 ? a + m : a; }
        static float Cover(float f, float hw, float step, float d)
        {
            float c = Math.Max(0, Math.Min(f + hw, d) - Math.Max(f - hw, 0));
            if (f + hw > step) c += Math.Max(0, Math.Min(f + hw, step + d) - step);
            return c;
        }

        // A glass shell for a window of its own (layered, per-pixel alpha): transparent outside the rounded shape.
        public static Bitmap RenderStandalone(Size size, float headerH, Color innerColor)
        { return Render(size, Point.Empty, headerH, innerColor, false, Theme.Back, true); }

        static Bitmap Render(Size size, Point at, float headerH, Color ic, bool dots, Color back, bool clearOutside = false)
        {
            int w = Math.Max(1, size.Width), h = Math.Max(1, size.Height);
            var px = new int[w * h];
            var inner = Inner(size, headerH);
            float k = K, rim = Rim;
            var outer = new Shape(new RectangleF(0, 0, w, h), OuterR, Smooth);
            var innerShape = new Shape(inner, InnerR, Smooth);
            Color dot = dots ? Theme.Dot : back;   // no grid: the "dots" blend to the plain background
            float bevel = rim * 1.7f, pull = 12 * k;
            // 24.10.2026: most pixels are the flat inner panel - fill them without the two distance functions (a 600x400
            // card took ~40 ms and every page switch re-rendered all cards); rows in parallel
            int icArgb = ic.ToArgb();
            float cm = InnerR * 2 + 2;   // beyond the corner curves
            System.Threading.Tasks.Parallel.For(0, h, y =>
            {
                float fy = y + 0.5f;
                bool midRow = fy > inner.Top + cm && fy < inner.Bottom - cm, edgeRow = fy > inner.Top + 2 && fy < inner.Bottom - 2;
                float fillL = midRow ? inner.Left + 2 : inner.Left + cm, fillR = midRow ? inner.Right - 2 : inner.Right - cm;
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f, cx = at.X + fx, cy = at.Y + fy, nx, ny, mx, my;
                    if (edgeRow && fx > fillL && fx < fillR) { px[y * w + x] = icArgb; continue; }
                    float dOut = outer.Dist(fx, fy, out nx, out ny);
                    float dIn = innerShape.Dist(fx, fy, out mx, out my);
                    double r, g, b;
                    if (dIn < -1.5f) { px[y * w + x] = icArgb; continue; }   // inner panel
                    // plain canvas (outside the shell: corners)
                    float plain = Dots(cx, cy, 0.5f);
                    double pr = back.R + (dot.R - back.R) * plain, pg = back.G + (dot.G - back.G) * plain, pb = back.B + (dot.B - back.B) * plain;
                    float cov = Math.Max(0, Math.Min(1, 0.5f - dOut));
                    if (cov <= 0) { px[y * w + x] = clearOutside ? 0 : Pack(pr, pg, pb); continue; }
                    // glass: lens refraction near the edge (sample from further inside), a bit more for blue
                    float e = -dOut, t = Math.Max(0, Math.Min(1, e / bevel)), amt = pull * (1 - t) * (1 - t);
                    float dr = Dots(cx - nx * amt * 0.9f, cy - ny * amt * 0.9f, 1.1f);
                    float dg = Dots(cx - nx * amt, cy - ny * amt, 1.1f);
                    float db = Dots(cx - nx * amt * 1.15f, cy - ny * amt * 1.15f, 1.1f);
                    r = back.R + (dot.R - back.R) * dr * 1.4; g = back.G + (dot.G - back.G) * dg * 1.4; b = back.B + (dot.B - back.B) * db * 1.4;
                    // frost tint
                    r = r * 0.72 + 44 * 0.28; g = g * 0.72 + 44 * 0.28; b = b * 0.72 + 50 * 0.28;
                    // light from the top (shader: rb1 * gradient)
                    double top = 1 - fy / h; r += 5 + 9 * top; g += 5 + 9 * top; b += 6 + 10 * top;
                    // bright thin ring just inside the edge (rb2), stronger at the top; fainter bottom ring (rb3)
                    double ring = Math.Exp(-Math.Pow((e - 1.1 * k) / (0.9 * k), 2));
                    double ring2 = Math.Exp(-Math.Pow((e - 3.2 * k) / (1.4 * k), 2)) * Math.Max(0, ny);
                    double lit = 55 * ring * (0.55 + 0.45 * Math.Max(0, -ny)) + 18 * ring2;
                    r += lit; g += lit; b += lit * 1.05;
                    // the inner panel is recessed: soft shadow around it, light lip on its top edge
                    if (dIn > 0) { double sh = 1 - 0.35 * Math.Exp(-dIn / (2.5 * k)); r *= sh; g *= sh; b *= sh; }
                    float ci = Math.Max(0, Math.Min(1, 0.5f - dIn));
                    if (ci > 0)
                    {
                        double lip = dIn > -1.4f * k && my < 0 ? 14 * -my : 0;
                        r += (ic.R + lip - r) * ci; g += (ic.G + lip - g) * ci; b += (ic.B + lip - b) * ci;
                    }
                    px[y * w + x] = clearOutside ? PackA(r * cov, g * cov, b * cov, cov)   // premultiplied edge
                                                 : Pack(pr + (r - pr) * cov, pg + (g - pg) * cov, pb + (b - pb) * cov);
                }
            });
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            Marshal.Copy(px, 0, data.Scan0, px.Length);
            bmp.UnlockBits(data);
            return bmp;
        }

        static int PackA(double r, double g, double b, double a)
        {
            int A = (int)Math.Max(0, Math.Min(255, a * 255));
            int R = (int)Math.Max(0, Math.Min(A, r)), G = (int)Math.Max(0, Math.Min(A, g)), B = (int)Math.Max(0, Math.Min(A, b));
            return (A << 24) | (R << 16) | (G << 8) | B;
        }

        static int Pack(double r, double g, double b)
        {
            int R = (int)Math.Max(0, Math.Min(255, r)), G = (int)Math.Max(0, Math.Min(255, g)), B = (int)Math.Max(0, Math.Min(255, b));
            return (255 << 24) | (R << 16) | (G << 8) | B;
        }

        // ---------- glass rim over live content (the intro animation) ----------

        // For every pixel of an 8 px rim (and the transparent corners outside it): where to sample the frame for each
        // colour channel (lens + prism, as in Render), the light added on top, the shadow factor, the coverage of the
        // outer edge (alpha) and of the inner edge (blend back to the content), and how close it is to the edge ring.
        public sealed class RimMap
        {
            public int Count;
            public int[] Dst, SR, SG, SB;
            public float[] Cov, Ci, Add, Mul, Band, X, Y;
        }

        // headerH: glass title row at the top (as in the modules); Rim = just the edge
        public static RimMap BuildRimMap(Size size, float outerR, float innerR, float headerH)
        {
            int w = Math.Max(1, size.Width), h = Math.Max(1, size.Height);
            var outer = new Shape(new RectangleF(0, 0, w, h), outerR, Smooth);
            var ins = new Shape(new RectangleF(Rim, headerH, w - 2 * Rim, h - headerH - Rim), innerR, Smooth);
            float k = K, bevel = Rim * 1.7f, pull = 12 * k;
            var dst = new List<int>(); var sr = new List<int>(); var sg = new List<int>(); var sb = new List<int>();
            var cov = new List<float>(); var ci = new List<float>(); var add = new List<float>(); var mul = new List<float>();
            var band = new List<float>(); var xs = new List<float>(); var ys = new List<float>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f, nx, ny, mx, my;
                    float dIn = ins.Dist(fx, fy, out mx, out my);
                    if (dIn < -1.5f) continue;                       // content, untouched
                    float dOut = outer.Dist(fx, fy, out nx, out ny);
                    float c = Math.Max(0, Math.Min(1, 0.5f - dOut));
                    int at = y * w + x;
                    float e = -dOut, t = Math.Max(0, Math.Min(1, e / bevel)), amt = pull * (1 - t) * (1 - t);
                    Func<float, int> lens = sc =>
                    {
                        int sx = (int)Math.Max(0, Math.Min(w - 1, Math.Round(fx - nx * amt * sc - 0.5f)));
                        int sy = (int)Math.Max(0, Math.Min(h - 1, Math.Round(fy - ny * amt * sc - 0.5f)));
                        return sy * w + sx;
                    };
                    double top = 1 - fy / h;
                    double ring = Math.Exp(-Math.Pow((e - 1.1 * k) / (0.9 * k), 2));
                    double ring2 = Math.Exp(-Math.Pow((e - 3.2 * k) / (1.4 * k), 2)) * Math.Max(0, ny);
                    dst.Add(at);
                    sr.Add(c > 0 ? lens(0.9f) : at); sg.Add(c > 0 ? lens(1f) : at); sb.Add(c > 0 ? lens(1.15f) : at);
                    cov.Add(c);
                    ci.Add(Math.Max(0, Math.Min(1, 0.5f - dIn)));
                    add.Add((float)(5 + 9 * top + 55 * ring * (0.55 + 0.45 * Math.Max(0, -ny)) + 18 * ring2));
                    mul.Add(dIn > 0 ? (float)(1 - 0.35 * Math.Exp(-dIn / (2.5 * k))) : 1f);
                    band.Add(e < 3 * k ? 1 - e / (3 * k) : 0);
                    xs.Add(fx); ys.Add(fy);
                }
            return new RimMap
            {
                Count = dst.Count, Dst = dst.ToArray(), SR = sr.ToArray(), SG = sg.ToArray(), SB = sb.ToArray(),
                Cov = cov.ToArray(), Ci = ci.ToArray(), Add = add.ToArray(), Mul = mul.ToArray(), Band = band.ToArray(), X = xs.ToArray(), Y = ys.ToArray(),
            };
        }

        // ---------- header (dot + title inside the glass) ----------

        public static void PaintHeader(Graphics g, string title, Color dot) { PaintHeader(g, title, dot, HeaderH); }
        public static void PaintHeader(Graphics g, string title, Color dot, float headerH, float width = 0)
        {
            float k = K, cy = headerH / 2 + 1 * k;
            using (var halo = new SolidBrush(Color.FromArgb(50, dot))) g.FillEllipse(halo, 18 * k - 6 * k, cy - 6 * k, 12 * k, 12 * k);
            using (var db = new SolidBrush(dot)) g.FillEllipse(db, 18 * k - 3.5f * k, cy - 3.5f * k, 7 * k, 7 * k);
            using (var f = new Font("Segoe UI", Theme.FBody)) using (var tb = new SolidBrush(Theme.Text))
                // a longer translation stays inside the card, cut with "…" (Loc.cs)
                g.DrawString(title, f, tb, new RectangleF(30 * k, 0, width > 0 ? Math.Max(40 * k, width - 46 * k) : 1000 * k, headerH + 2 * k), new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter });
        }

        // ---------- pointer colour in the glass ----------

        public static float Reach { get { return 170 * K; } }

        public static void PaintPointerGlow(Graphics g, Control c) { PaintPointerGlow(g, c, HeaderH); }
        public static void PaintPointerGlow(Graphics g, Control c, float headerH) { PaintPointerGlow(g, c, headerH, Color.Empty); }
        // tint: the pointer light in another colour (UI review 23.10.2026: red over a card whose client is not found),
        // without the prism fringes; Color.Empty = the yellow pointer colour
        public static void PaintPointerGlow(Graphics g, Control c, float headerH, Color tint)
        {
            var pc = tint == Color.Empty ? PointerColor : tint;
            var p = c.PointToClient(Cursor.Position);
            var rect = new RectangleF(0, 0, c.Width, c.Height);
            float R = Reach;
            if (p.X < -R || p.Y < -R || p.X > c.Width + R || p.Y > c.Height + R) return;
            var state = g.Save();
            using (var shell = ShellPath(c.Size)) using (var inner = InnerPath(c.Size, headerH))
            using (var glass = new Region(shell))
            {
                glass.Exclude(inner);
                g.SetClip(glass, CombineMode.Replace);
                Radial(g, p.X, p.Y, R, pc, 90);                                 // light inside the glass
                // edge band: where the shader's bright ring is, the colour is strongest
                var band = new RectangleF(2.6f * K, 2.6f * K, c.Width - 5.2f * K, c.Height - 5.2f * K);
                using (var bp = Theme.Round(band, OuterR - 2.6f * K, Smooth))
                using (var edge = new Region(shell))
                {
                    edge.Exclude(bp);
                    g.SetClip(edge, CombineMode.Replace);
                    Radial(g, p.X, p.Y, R * 0.9f, pc, 255);
                    if (tint == Color.Empty)
                    {
                        Radial(g, p.X + 7 * K, p.Y + 5 * K, R * 0.65f, Color.FromArgb(255, 140, 40), 150);   // prism fringes
                        Radial(g, p.X - 7 * K, p.Y - 5 * K, R * 0.55f, Color.FromArgb(130, 255, 190), 100);
                    }
                }
            }
            g.Restore(state);
        }

        // ---------- state colour in the glass (player 19.10.2026: a card whose client is not found glows red) ----------
        // The same two regions the pointer glow uses: the whole glass rim gets a faint wash, the edge ring a strong one,
        // so the colour reads as light caught in the edge of the glass, not as a painted border.
        public static void PaintEdgeTint(Graphics g, Control c, Color color, int edgeAlpha, int glassAlpha) { PaintEdgeTint(g, c, color, edgeAlpha, glassAlpha, HeaderH); }
        public static void PaintEdgeTint(Graphics g, Control c, Color color, int edgeAlpha, int glassAlpha, float headerH)
        {
            if (color == Color.Empty || c.Width <= 0 || c.Height <= 0) return;
            var state = g.Save();
            using (var shell = ShellPath(c.Size)) using (var inner = InnerPath(c.Size, headerH))
            using (var glass = new Region(shell))
            {
                glass.Exclude(inner);
                g.SetClip(glass, CombineMode.Replace);
                using (var b = new SolidBrush(Color.FromArgb(glassAlpha, color))) g.FillRectangle(b, 0, 0, c.Width, c.Height);
                var band = new RectangleF(2.6f * K, 2.6f * K, c.Width - 5.2f * K, c.Height - 5.2f * K);
                using (var bp = Theme.Round(band, OuterR - 2.6f * K, Smooth))
                using (var edge = new Region(shell))
                {
                    edge.Exclude(bp);
                    g.SetClip(edge, CombineMode.Replace);
                    using (var b = new SolidBrush(Color.FromArgb(edgeAlpha, color))) g.FillRectangle(b, 0, 0, c.Width, c.Height);
                }
            }
            g.Restore(state);
        }

        static void Radial(Graphics g, float x, float y, float r, Color c, int alpha)
        {
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(x - r, y - r, 2 * r, 2 * r);
                using (var b = new PathGradientBrush(path) { CenterColor = Color.FromArgb(alpha, c), SurroundColors = new[] { Color.FromArgb(0, c) } })
                {
                    b.Blend = new Blend { Positions = new[] { 0f, 0.45f, 0.75f, 1f }, Factors = new[] { 0f, 0.18f, 0.55f, 1f } };   // soft falloff
                    g.FillPath(b, path);
                }
            }
        }

        // ---------- repaint glass controls when the pointer moves near them ----------

        static readonly List<Control> tracked = new List<Control>();
        static Timer timer;
        static Point last = new Point(int.MinValue, int.MinValue);

        public static void Track(Control c)
        {
            tracked.Add(c);
            c.Disposed += (s, e) => tracked.Remove(c);
            if (timer != null) return;
            timer = new Timer { Interval = 15 };
            timer.Tick += (s, e) =>
            {
                var p = Cursor.Position;
                if (p == last) return;
                int reach = (int)Reach + 2;
                foreach (var t in tracked.ToArray())
                {
                    if (!t.IsHandleCreated || !t.Visible) continue;
                    var r = t.RectangleToScreen(t.ClientRectangle); r.Inflate(reach, reach);
                    if (r.Contains(p) || r.Contains(last)) t.Invalidate();
                }
                last = p;
            };
            timer.Start();
        }

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
                    var pts = new[] { new PointF(3, 3), new PointF(21, 9.5f), new PointF(12.2f, 12.2f), new PointF(9.5f, 21) };
                    for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(pts[i].X * s, pts[i].Y * s);
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
                var info = new ICONINFO { fIcon = false, xHotspot = (int)(3 * s), yHotspot = (int)(3 * s), hbmColor = bmp.GetHbitmap(Color.FromArgb(0, 0, 0, 0)), hbmMask = CreateBitmap(size, size, 1, 1, IntPtr.Zero) };
                var h = CreateIconIndirect(ref info);
                DeleteObject(info.hbmColor); DeleteObject(info.hbmMask);
                if (h == IntPtr.Zero) return Cursors.Default;
                return new Cursor(h);
            }
        }
    }
}
