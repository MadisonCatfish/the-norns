// PowerAnims.cs - the power display's animation, chosen in Settings -> Display (owner, 04.11.2026): the loadersz sphere
// (PowerAnim in Body.cs, the default) or one of five made for the body after the owner's picks from the prototypes
// (build\power-anims): Cube wave, ノルン, Warp (Flow field until 24.11.2026). All are dots in one colour; near dots larger and
// brighter, far ones smaller and dim. Sizes are in "display units": the display is 28 of them high.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace TheNorns
{
    static class PowerAnims
    {
        public static readonly string[] Names = { "working", "cubewave", "norns", "warp" };
        public static readonly string[] Titles = { "Working", "Cube wave", "ノルン", "Warp" };
        public static bool Known(string n) { return Names.Contains(n); }
        // a saved choice of an animation that was replaced (owner, 24.11.2026: Flow field -> Warp)
        public static string Upgrade(string n) { return n == "flow" ? "warp" : n; }

        static double Hash(double e) { double t = Math.Sin(e * 12.9898 + 78.233) * 43758.5453; return t - Math.Floor(t); }
        static double Ease(double x) { return x < 0.5 ? 4 * x * x * x : 1 - Math.Pow(-2 * x + 2, 3) / 2; }
        static double Clamp(double x, double a, double b) { return Math.Max(a, Math.Min(b, x)); }

        // brushes by colour and alpha (32 steps): a frame draws up to ~1000 dots
        static readonly Dictionary<long, SolidBrush> brushes = new Dictionary<long, SolidBrush>();
        static SolidBrush Brush(Color c, double a)
        {
            int step = (int)Math.Round(Clamp(a, 0, 1) * 31);
            long key = ((long)(uint)c.ToArgb() << 5) | (long)step;
            SolidBrush b;
            if (!brushes.TryGetValue(key, out b)) brushes[key] = b = new SolidBrush(Color.FromArgb(step * 255 / 31, c));
            return b;
        }
        // a dot with depth: z -1 far .. 1 near
        static void Dot(Graphics g, double x, double y, double z, double r, double a, Color c)
        {
            double depth = (Clamp(z, -1, 1) + 1) / 2, rr = r * (0.55 + 0.8 * depth), al = a * (0.25 + 0.75 * depth);
            if (al < 0.02) return;
            float d = (float)Math.Max(0.6, rr * 2);
            g.FillEllipse(Brush(c, al), (float)(x - d / 2), (float)(y - d / 2), d, d);
        }

        // what the stateful ones keep, per place it is drawn (the body, each preview in the settings)
        static readonly Dictionary<string, object> states = new Dictionary<string, object>();

        // the display's face; secs: the animation clock (it slows down in a warp, stands still with no log)
        public static void Draw(string name, string slot, Graphics g, RectangleF face, double secs, Color color)
        {
            double k = face.Height / 28.0;
            var st = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            switch (name)
            {
                case "cubewave": CubeWave(g, face, secs, color, k); break;
                case "norns": Norns(slot, g, face, secs, color, k); break;
                case "warp": Warp(slot, g, face, secs, color, k); break;
                default: PowerAnim.Draw(g, new PointF(face.Left + face.Width / 2, face.Top + face.Height / 2), face.Height * 1.89f, secs, color, (float)k); break;
            }
            g.Restore(st);
        }

        // ---- Cube wave (after Bees & Bombs): columns of dots rising and sinking in a wave from the centre; drawn large,
        // its corners may leave the glass (owner, 04.11.2026) ----
        struct CubeDot { public double X, Y, Z, H; public bool Top; public int Order; }
        static readonly CubeDot[] cubePts = new CubeDot[11 * 11 * 6];
        static readonly IComparer<CubeDot> CubeOrder = Comparer<CubeDot>.Create((a, b) => { int c = a.Z.CompareTo(b.Z); return c != 0 ? c : a.Order.CompareTo(b.Order); });
        static void CubeWave(Graphics g, RectangleF f, double t, Color c, double k)
        {
            const int n = 11;
            var pts = cubePts; int count = 0;
            double ry = Math.PI / 4, cyr = Math.Cos(ry), syr = Math.Sin(ry), cxr = Math.Cos(0.6), sxr = Math.Sin(0.6);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    double x = (i - (n - 1) / 2.0) / (n / 2.0), z = (j - (n - 1) / 2.0) / (n / 2.0), d = Math.Sqrt(x * x + z * z);
                    double hh = 0.25 + 0.55 * (0.5 + 0.5 * Math.Sin(t * 2.2 - d * 4.5));
                    for (int s = 0; s <= 5; s++)
                    {
                        // RotY by pi/4, then RotX by 0.6 (as RotY / RotX, without their arrays)
                        double y = -0.6 + hh * s / 5, x1 = x * cyr - z * syr, z1 = x * syr + z * cyr;
                        pts[count] = new CubeDot { X = x1, Y = y * cxr - z1 * sxr, Z = y * sxr + z1 * cxr, Top = s == 5, H = hh, Order = count };
                        count++;
                    }
                }
            // far to near; equal depths keep their order (as OrderBy did), so the frame is the same pixel for pixel
            Array.Sort(pts, 0, count, CubeOrder);
            for (int i = 0; i < count; i++)
            {
                var q = pts[i];
                Dot(g, f.Left + f.Width / 2 + q.X * f.Height * 0.84, f.Top + f.Height * 0.58 - q.Y * f.Height * 0.84, q.Z, (q.Top ? 0.42 : 0.19) * k, q.Top ? 0.5 + q.H * 0.5 : 0.3, c);
            }
        }

        // ---- Warp (owner, 24.11.2026; in place of Flow field): stars flying at the viewer; in a warp they stretch into
        // streaks and the view rolls round the centre. After the prototype build\power-anims\warp.html
        // with the owner's settings: cruise 55, streaks 15 %, roll 80 %; no rim glow. In the body the warp follows the
        // fleet's warp (FleetWarp); elsewhere (the settings' tiles, Tuning) a warp comes every 9 s ----
        public static bool? FleetWarp;   // the body sets it before each frame: true while the fleet is in warp
        class WarpState { public double[] X, Y, Z, Tw; public double Last = double.NaN, Level, Roll, Since; public bool Rising; }
        const int WarpN = 420;
        const double WarpFar = 1400, WarpSpread = 260, WarpCruise = 55, WarpStreak = 0.15, WarpRoll = 0.8, WarpEvery = 9;
        static void WarpPlace(WarpState s, int i, bool anywhere, double seed)
        {
            double x, y; int n = 0;
            do { x = (Hash(seed + i * 1.7 + n) * 2 - 1) * WarpSpread; y = (Hash(seed * 1.3 + i * 2.9 + n) * 2 - 1) * WarpSpread; n++; } while (Math.Sqrt(x * x + y * y) < 14 && n < 8);
            s.X[i] = x; s.Y[i] = y; s.Z[i] = anywhere ? -Hash(seed + i * 3.7) * WarpFar : -WarpFar + Hash(seed + i * 4.1) * 60; s.Tw[i] = Hash(i * 7.3) * 6.28;
        }
        static void Warp(string slot, Graphics g, RectangleF f, double t, Color c, double k)
        {
            object o; WarpState s;
            if (!states.TryGetValue("warp:" + slot, out o))
            {
                s = new WarpState { X = new double[WarpN], Y = new double[WarpN], Z = new double[WarpN], Tw = new double[WarpN] };
                for (int i = 0; i < WarpN; i++) WarpPlace(s, i, true, 0.5);
                states["warp:" + slot] = s;
            }
            else s = (WarpState)o;
            double dt = double.IsNaN(s.Last) ? 0 : Clamp(t - s.Last, 0, 0.1); s.Last = t;
            // the warp's level: 0.8 s to spool up (eased in), held while in warp, 1.2 s back to cruise (eased out)
            bool inWarp;
            if (slot == "body" && FleetWarp != null) inWarp = FleetWarp.Value;
            else { s.Since += dt; double ph = s.Since % (WarpEvery + 3.6); inWarp = ph >= WarpEvery && ph < WarpEvery + 2.4; }
            if (inWarp) { if (!s.Rising) s.Rising = true; s.Level = Math.Min(1, s.Level + dt / 0.8); }
            else { if (s.Rising) s.Rising = false; s.Level = Math.Max(0, s.Level - dt / 1.2); }
            double lv = s.Level, wf = s.Rising ? lv * lv * lv : lv * lv * (3 - 2 * lv);
            double speed = WarpCruise + wf * 1400, streakK = WarpStreak * (0.06 + wf * 0.22);
            s.Roll += WarpRoll * 0.5 * wf * dt;
            double fov = (70 + wf * 14) * Math.PI / 180, proj = f.Height / 2 / Math.Tan(fov / 2);
            double cx = f.Left + f.Width / 2, cy = f.Top + f.Height / 2, cr = Math.Cos(s.Roll), sr = Math.Sin(s.Roll);
            double shx = Math.Sin(t * 37) * 0.25 * wf, shy = Math.Cos(t * 41) * 0.25 * wf;

            // ahead, a faint haze where the ship flies to
            float hr = f.Height * 0.9f;
            using (var hp = new GraphicsPath())
            {
                hp.AddEllipse((float)cx - hr, (float)cy - hr, hr * 2, hr * 2);
                using (var hb = new PathGradientBrush(hp) { CenterColor = Color.FromArgb((int)(255 * 0.13 * (0.6 + 0.4 * wf)), c), SurroundColors = new[] { Color.FromArgb(0, c) } }) g.FillPath(hb, hp);
            }
            var pt = new PointF[2];
            for (int i = 0; i < WarpN; i++)
            {
                s.Z[i] += speed * dt;
                if (s.Z[i] > -2) WarpPlace(s, i, false, t + i * 0.01);
                double x = s.X[i] - shx, y = s.Y[i] - shy, z = s.Z[i];
                double near = 1 - (-z / WarpFar), side = Math.Min(1, Math.Sqrt(s.X[i] * s.X[i] + s.Y[i] * s.Y[i]) / WarpSpread);
                double b = Math.Min(1, near * near * (0.55 + 0.45 * side) * 1.6) * (0.82 + 0.18 * Math.Sin(t * 3 + s.Tw[i]) * (1 - wf));
                if (b < 0.02) continue;
                double len = speed * streakK * (0.6 + side);
                // the head and the tail on the glass (rolled round the centre)
                double hx = x / -z * proj, hy = y / -z * proj, zt = z - len, tx = x / -zt * proj, ty = y / -zt * proj;
                float sx = (float)(cx + hx * cr - hy * sr), sy = (float)(cy + hx * sr + hy * cr);
                if (sx < f.Left - 4 || sx > f.Right + 4 || sy < f.Top - 4 || sy > f.Bottom + 4) continue;
                double lb = b * Math.Min(1, 0.25 + wf * 1.2 + streakK * 2);
                if (len > 0.5 && lb > 0.03)
                {
                    float ex = (float)(cx + tx * cr - ty * sr), ey = (float)(cy + tx * sr + ty * cr);
                    if (Math.Abs(ex - sx) + Math.Abs(ey - sy) > 0.6)
                    {
                        // the streak fades to nothing at its tail: a bright half at the head, a dim half behind
                        float mx = (sx + ex) / 2, my = (sy + ey) / 2, w = (float)Math.Max(0.5, 0.35 * k);
                        using (var pen = new Pen(Color.FromArgb((int)(255 * Clamp(lb * 0.35, 0, 1)), c), w)) g.DrawLine(pen, mx, my, ex, ey);
                        using (var pen = new Pen(Color.FromArgb((int)(255 * Clamp(lb * 0.85, 0, 1)), c), w)) g.DrawLine(pen, sx, sy, mx, my);
                    }
                }
                // the star: larger when near, as the prototype's points (size 4.2, attenuated)
                double r = Clamp(4.2 / -z * proj * 0.5 * 0.6, 0.25 * k, 1.6 * k);
                float d = (float)Math.Max(0.6, r * 2);
                g.FillEllipse(Brush(c, b), sx - d / 2, sy - d / 2, d, d);
            }
            // no rim glow (owner, 24.11.2026: "Rim glow ... to 0")
        }

        // ---- ノルン: the dots gather into the app's name, hold, burst into a swirl and spring back ----
        class NornsState { public double[][] P; public double Last = double.NaN; public long Burst = -1; }
        static double[][] nornsTargets;
        static void Norns(string slot, Graphics g, RectangleF f, double t, Color c, double k)
        {
            if (nornsTargets == null)
            {
                var list = new List<double[]>();
                using (var path = DpsForm.NamePath(100))
                {
                    if (path != null)
                    {
                        // which grid points fall inside the name: the path flattened once into a region (its hit test is
                        // ~100x faster than the path's own, which flattened the curves at every point: the Display tab
                        // froze for ~0.45 s on its first frame)
                        var b = path.GetBounds(); double half = b.Width / 2;
                        using (var inside = new Region(path))
                            for (float y = b.Top; y <= b.Bottom; y += 2.8f)
                                for (float x = b.Left; x <= b.Right; x += 2.8f)
                                    if (inside.IsVisible(x, y)) list.Add(new[] { (x - b.Left - half) / half * 0.8, (y - b.Top - b.Height / 2) / half * 0.8 });
                    }
                }
                nornsTargets = list.ToArray();
            }
            object o; NornsState s;
            if (!states.TryGetValue("norns:" + slot, out o))
            {
                s = new NornsState { P = nornsTargets.Select((p, i) => new[] { p[0], p[1], 0, 0, Hash(i) }).ToArray() };
                states["norns:" + slot] = s;
            }
            else s = (NornsState)o;
            double dt = double.IsNaN(s.Last) ? 0 : Clamp(t - s.Last, 0, 0.1); s.Last = t;
            long cycle = (long)Math.Floor(t / 6); double ph = t - cycle * 6.0;
            bool burst = ph > 3.6 && s.Burst != cycle;
            if (burst) s.Burst = cycle;
            for (int i = 0; i < s.P.Length; i++)
            {
                var q = s.P[i];
                if (burst) { double a = Math.Atan2(q[1], q[0]) + 0.8; q[2] += Math.Cos(a) * (0.9 + q[4]); q[3] += Math.Sin(a) * (0.9 + q[4]); }
                double pull = ph > 3.6 && ph < 4.6 ? 0.4 : 9 * (0.6 + q[4] * 0.6);
                q[2] += ((nornsTargets[i][0] - q[0]) * pull - q[2] * 3.2) * dt; q[3] += ((nornsTargets[i][1] - q[1]) * pull - q[3] * 3.2) * dt;
                q[0] += q[2] * dt; q[1] += q[3] * dt;
            }
            double sc = Math.Min(f.Width * 0.46, f.Height * 1.2);
            foreach (var q in s.P)
            {
                double sp = Math.Sqrt(q[2] * q[2] + q[3] * q[3]);
                Dot(g, f.Left + f.Width / 2 + q[0] * sc, f.Top + f.Height / 2 + q[1] * sc, Clamp(1 - sp, -1, 1), 0.2 * k, 0.6 + Math.Min(0.4, sp), c);
            }
        }
    }
}
