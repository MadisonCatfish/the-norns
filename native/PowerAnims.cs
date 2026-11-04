// PowerAnims.cs - the power display's animation, chosen in Settings -> Display (owner, 04.11.2026): the loadersz sphere
// (PowerAnim in Body.cs, the default) or one of five made for the body after the owner's picks from the prototypes
// (build\power-anims): Cube wave, ノルン, Flow field. All are dots in one colour; near dots larger and
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
        public static readonly string[] Names = { "working", "cubewave", "norns", "flow" };
        public static readonly string[] Titles = { "Working", "Cube wave", "ノルン", "Flow field" };
        public static bool Known(string n) { return Names.Contains(n); }

        static double Hash(double e) { double t = Math.Sin(e * 12.9898 + 78.233) * 43758.5453; return t - Math.Floor(t); }
        static double Ease(double x) { return x < 0.5 ? 4 * x * x * x : 1 - Math.Pow(-2 * x + 2, 3) / 2; }
        static double Clamp(double x, double a, double b) { return Math.Max(a, Math.Min(b, x)); }
        static double[] RotY(double x, double y, double z, double a) { double c = Math.Cos(a), s = Math.Sin(a); return new[] { x * c - z * s, y, x * s + z * c }; }
        static double[] RotX(double[] p, double a) { double c = Math.Cos(a), s = Math.Sin(a); return new[] { p[0], p[1] * c - p[2] * s, p[1] * s + p[2] * c }; }

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
                case "flow": Flow(slot, g, face, secs, color, k); break;
                default: PowerAnim.Draw(g, new PointF(face.Left + face.Width / 2, face.Top + face.Height / 2), face.Height * 1.89f, secs, color, (float)k); break;
            }
            g.Restore(st);
        }

        // ---- Cube wave (after Bees & Bombs): columns of dots rising and sinking in a wave from the centre; drawn large,
        // its corners may leave the glass (owner, 04.11.2026) ----
        static void CubeWave(Graphics g, RectangleF f, double t, Color c, double k)
        {
            const int n = 11;
            var pts = new List<double[]>(n * n * 6);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    double x = (i - (n - 1) / 2.0) / (n / 2.0), z = (j - (n - 1) / 2.0) / (n / 2.0), d = Math.Sqrt(x * x + z * z);
                    double hh = 0.25 + 0.55 * (0.5 + 0.5 * Math.Sin(t * 2.2 - d * 4.5));
                    for (int s = 0; s <= 5; s++)
                    {
                        var q = RotX(RotY(x, -0.6 + hh * s / 5, z, Math.PI / 4), 0.6);
                        pts.Add(new[] { q[0], q[1], q[2], s == 5 ? 1 : 0, hh });
                    }
                }
            foreach (var q in pts.OrderBy(v => v[2]))
            {
                bool top = q[3] > 0;
                Dot(g, f.Left + f.Width / 2 + q[0] * f.Height * 0.84, f.Top + f.Height * 0.58 - q[1] * f.Height * 0.84, q[2], (top ? 0.42 : 0.19) * k, top ? 0.5 + q[4] * 0.5 : 0.3, c);
            }
        }

        // ---- Flow field: particles drifting through a turning field of swirls, each with a short fading tail ----
        class FlowState { public double[][] P; public double[][][] Tail; public double Last = double.NaN; }
        static void Flow(string slot, Graphics g, RectangleF f, double t, Color c, double k)
        {
            const int n = 500, tail = 3;
            object o; FlowState s;
            if (!states.TryGetValue("flow:" + slot, out o)) { s = new FlowState { P = new double[n][], Tail = new double[n][][] }; for (int i = 0; i < n; i++) { s.P[i] = new[] { Hash(i) * 2 - 1, Hash(i * 5.7) * 2 - 1, Hash(i * 9.3), Hash(i * 3.1) * 4 }; s.Tail[i] = new double[tail][]; } states["flow:" + slot] = s; }
            else s = (FlowState)o;
            double dt = double.IsNaN(s.Last) ? 0 : Clamp(t - s.Last, 0, 0.1); s.Last = t;
            for (int i = 0; i < n; i++)
            {
                var q = s.P[i];
                Array.Copy(s.Tail[i], 0, s.Tail[i], 1, tail - 1); s.Tail[i][0] = new[] { q[0], q[1] };
                double x = q[0] * 2.2, y = q[1] * 2.2;
                // the curl of a sum of waves: a field without sources, so the particles swirl instead of piling up
                double vx = Math.Cos(y * 1.3 + t * 0.4) + 0.6 * Math.Cos(y * 2.9 - x * 1.1 + t * 0.7);
                double vy = -Math.Sin(x * 1.5 - t * 0.3) - 0.6 * Math.Sin(x * 2.7 + y * 1.3 - t * 0.5);
                q[0] += vx * dt * 0.22; q[1] += vy * dt * 0.22; q[3] += dt;
                if (Math.Abs(q[0]) > 1.1 || Math.Abs(q[1]) > 1.1 || q[3] > 4 + q[2] * 4)
                {
                    q[0] = Hash(i + t) * 2 - 1; q[1] = Hash(i * 1.3 + t) * 2 - 1; q[3] = 0;
                    for (int j = 0; j < tail; j++) s.Tail[i][j] = null;
                }
            }
            double cx = f.Left + f.Width / 2, cy = f.Top + f.Height / 2;
            for (int i = 0; i < n; i++)
            {
                var q = s.P[i];
                double life = Math.Min(1, q[3] * 2) * Math.Min(1, (4 + q[2] * 4 - q[3]) * 2), z = q[2] * 2 - 1;
                for (int j = tail - 1; j >= 0; j--)
                {
                    var p = s.Tail[i][j]; if (p == null) continue;
                    Dot(g, cx + p[0] * f.Width * 0.5, cy + p[1] * f.Height * 0.5, z, 0.2 * k, 0.75 * life * (1 - (j + 1) / (double)(tail + 1)) * 0.5, c);
                }
                Dot(g, cx + q[0] * f.Width * 0.5, cy + q[1] * f.Height * 0.5, z, 0.24 * k, 0.75 * life, c);
            }
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
                        var b = path.GetBounds(); double half = b.Width / 2;
                        for (float y = b.Top; y <= b.Bottom; y += 2.8f)
                            for (float x = b.Left; x <= b.Right; x += 2.8f)
                                if (path.IsVisible(x, y)) list.Add(new[] { (x - b.Left - half) / half * 0.8, (y - b.Top - b.Height / 2) / half * 0.8 });
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
