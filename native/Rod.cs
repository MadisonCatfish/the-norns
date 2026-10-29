// The Norns - the Lightning rod module (Competition mode, owner 29.10.2026): in place of Smartbombs.
// The number: the damage the Thunderchilds put through the runner per second (its Vorton hits in the last 10 s x one
// shot's damage from Settings -> Modes, / 10).
// The graph is an ECG of the runner, on the middle line (owner, 29.10.2026): a pen at the right end on a damped spring,
// every hit on the runner kicks it - a smooth peak, then a few fading ripples - and the trace runs to the left. Each kick
// also sends a small wave through the whole trace, so the line writhes. The Vorton hits in bright blood red with a glow,
// the NPCs' hits on the runner as a thin white line. Below the number: what the runner takes per second against its
// repair (red when it is losing hit points).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;

namespace TheNorns
{
    partial class DpsForm
    {
        internal static readonly Color RodRed = Color.FromArgb(236, 28, 48);

        public static readonly FormulaText RodFormula = new FormulaText
        {
            Title = "Eff. DPS · Lightning rod",
            Body = "How much damage the Thunderchilds put through the runner, per second.\n" +
                   "The big number counts the Vorton hits on the runner in all the Thunderchilds' combat logs over the last 10 seconds, multiplies them by the damage of one shot (Settings → Modes) and divides by 10. Each shot arcs on to the NPCs with that damage.\n" +
                   "The red line is the runner's ECG: every Vorton hit on it kicks the line, the bigger the hit the higher the peak, and the trace runs to the left.\n" +
                   "The thin white line does the same for the NPCs' hits on the runner, from its own combat log.\n" +
                   "Under the number: what the runner takes per second against its repair. Red means it is losing hit points."
        };

        // one ECG trace: a pen on a damped spring, its past positions running left, and a wave over the whole trace
        class RodTrace
        {
            public const int M = 150;          // samples across the module
            const double Span = 9;             // seconds across the module
            const double W = 2 * Math.PI * 2.1, Zeta = 0.2;   // the pen's spring: ~2 Hz, a few ripples
            public readonly double[] Rec = new double[M], Wave = new double[M];
            readonly double[] waveV = new double[M];
            double pen, penV, acc;
            public void Kick(double size)      // size 0..1 of half the height
            {
                penV -= size * W * 2.1;
                for (int k = 0; k < 6; k++) waveV[M - 1 - k] -= size * 0.9 * Math.Exp(-k * 0.5);
            }
            public void Step(double dt)
            {
                penV += (-W * W * pen - 2 * Zeta * W * penV) * dt; pen += penV * dt;
                // the wave: neighbours pull on each other, each node springs back to the middle line, all of it damped
                for (int i = 1; i < M - 1; i++) waveV[i] += (900 * (Wave[i - 1] - 2 * Wave[i] + Wave[i + 1]) - 6 * Wave[i] - 2.2 * waveV[i]) * dt;
                waveV[M - 1] += (900 * (Wave[M - 2] - Wave[M - 1]) - 6 * Wave[M - 1] - 2.2 * waveV[M - 1]) * dt;
                for (int i = 1; i < M; i++) Wave[i] += waveV[i] * dt;
                acc += dt;
                double every = Span / M;
                while (acc >= every) { acc -= every; Array.Copy(Rec, 1, Rec, 0, M - 1); Rec[M - 1] = pen; }
                Rec[M - 1] = pen;
            }
            public bool Calm { get { return Math.Abs(pen) + Math.Abs(penV) < 1e-3 && Rec.All(v => Math.Abs(v) < 1e-3) && Wave.All(v => Math.Abs(v) < 1e-3); } }
        }

        readonly RodTrace rodVorton = new RodTrace(), rodNpc = new RodTrace();
        readonly HashSet<DmgEvent> rodSeen = new HashSet<DmgEvent>();
        DateTime rodSim = DateTime.MinValue;

        // how high a hit kicks: 65 (a usual Vorton hit on a Jaguar) about 0.7 of half the height
        static double KickSize(double amount) { return 1 - Math.Exp(-amount / 54.0); }

        // runs the traces up to `now`: in self-tests the hits by their log time (from 12 s back, so the picture is full);
        // live, each hit kicks when it arrives (the log lines come a second or two late)
        void SimRod(DateTime now)
        {
            bool demo = DemoNow != null;
            if (rodSim == DateTime.MinValue || (now - rodSim).TotalSeconds > 5 || now < rodSim)
            {
                rodSim = demo ? now.AddSeconds(-12) : now;
                if (!demo) foreach (var h in stats.RodHits.Concat(stats.RunnerNpcHits)) rodSeen.Add(h);   // the past: no kicks
            }
            const double dt = 1 / 60.0;
            var hits = stats.RodHits.ToList(); var npc = stats.RunnerNpcHits.ToList();
            if (!demo)
            {
                foreach (var h in hits) if (rodSeen.Add(h)) rodVorton.Kick(KickSize(h.Amount));
                foreach (var h in npc) if (rodSeen.Add(h)) rodNpc.Kick(KickSize(h.Amount));
                rodSeen.RemoveWhere(h => (now - h.T).TotalSeconds > DpsStats.Seconds + 20);
            }
            int steps = 0;
            while (rodSim < now && steps++ < 1200)
            {
                var next = rodSim.AddSeconds(dt);
                if (demo)
                {
                    foreach (var h in hits) if (h.T > rodSim && h.T <= next) rodVorton.Kick(KickSize(h.Amount));
                    foreach (var h in npc) if (h.T > rodSim && h.T <= next) rodNpc.Kick(KickSize(h.Amount));
                }
                rodVorton.Step(dt); rodNpc.Step(dt);
                rodSim = next;
            }
        }

        // --preview-competition: Competition with made-up hits - 4 Thunderchilds on the runner about every 2 s together,
        // now and then a few NPC hits on it - to watch the ECG move next to a running copy
        DateTime rodDemoNext = DateTime.MaxValue, npcDemoNext = DateTime.MaxValue;
        readonly Random rodRnd = new Random(5);
        public void StartRodDemo() { Competition = true; rodDemoNext = npcDemoNext = DateTime.UtcNow.AddSeconds(1); if (RunnerRepair <= 0) RunnerRepair = 74.7; if (VortonShot[0] + VortonShot[1] <= 0) VortonShot = new double[] { 1801, 1789 }; }
        void FeedRodDemo()
        {
            var t = DateTime.UtcNow; var es = new List<DmgEvent>();
            if (t >= rodDemoNext) { es.Add(new DmgEvent { T = t, Pilot = "Pilot-" + (1 + rodRnd.Next(4)), Out = true, Quality = "Hits", Other = "Pilot-5[DEMO](Jaguar)", Weapon = "Large Vorton Projector II", Amount = rodRnd.NextDouble() < 0.75 ? 65 : 36 + rodRnd.Next(26) }); rodDemoNext = t.AddSeconds(0.8 + rodRnd.NextDouble() * 2.6); }
            if (t >= npcDemoNext) { es.Add(new DmgEvent { T = t, Pilot = "Pilot-5", Out = false, Quality = "Hits", Other = "Core Grand Admiral", Amount = 15 + rodRnd.Next(40) }); npcDemoNext = t.AddSeconds(rodRnd.NextDouble() < 0.7 ? 0.7 : 6 + rodRnd.NextDouble() * 6); }
            if (es.Count > 0) AddDemo(es);
        }

        void PaintLightningRod(Graphics g, RectangleF r, DateTime now)
        {
            if (rodDemoNext != DateTime.MaxValue) FeedRodDemo();
            double shot = VortonShot[0] + VortonShot[1];
            sbFx = FormulaBadge(g, "Eff. DPS · Lightning rod", NornsFonts.Screen(U(9)), r.Left, r.Top);
            var big = NornsFonts.Screen(U(9));
            Glow(g, Num(stats.RodDps(now, shot)), big, BodyTheme.Ink, r.Right, r.Top, true);
            // what the runner takes against its repair, small under the number
            double takes = stats.RunnerVortonTaken(now) + stats.RunnerNpcTaken(now);
            var tl = Num(takes) + " / " + RunnerRepair.ToString("0.#", CultureInfo.InvariantCulture) + " HP/S";
            var tf = NornsFonts.Screen(U(5));
            float tw = g.MeasureString(tl, tf, 1000, StringFormat.GenericTypographic).Width;
            bool losing = RunnerRepair > 0 && takes > RunnerRepair;
            using (var b = new SolidBrush(losing ? RodRed : Color.FromArgb(140, BodyTheme.Ink))) g.DrawString(tl, tf, b, r.Right - tw, r.Top + big.GetHeight(g) + U(1), StringFormat.GenericTypographic);

            SimRod(now);
            var area = RectangleF.FromLTRB(r.Left, r.Top + U(19), r.Right, r.Bottom - U(1));
            float mid = area.Top + area.Height / 2, half = area.Height / 2 - U(1.5f);
            Func<RodTrace, PointF[]> trace = t => Enumerable.Range(0, RodTrace.M).Select(i =>
                new PointF(area.Left + area.Width * i / (RodTrace.M - 1), mid + half * (float)Math.Tanh(t.Rec[i] + t.Wave[i]))).ToArray();

            // the middle line, faint
            using (var b = new SolidBrush(Color.FromArgb(18, BodyTheme.Ink))) g.FillRectangle(b, area.Left, mid, area.Width, Math.Max(1f, U(0.5f)));
            var st = g.Save();
            g.SetClip(RectangleF.Inflate(area, 0, U(2)));
            // the NPCs on the runner: thin white
            using (var pen = new Pen(Color.FromArgb(225, 242, 243, 245), Math.Max(1f, U(0.6f))) { LineJoin = LineJoin.Round })
                g.DrawCurve(pen, trace(rodNpc), 0.45f);
            // the Vorton hits: bright blood red, a wide soft glow under a solid core
            var pts = trace(rodVorton);
            if (!Potato)
            {
                using (var pen = new Pen(Color.FromArgb(40, RodRed), U(7)) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawCurve(pen, pts, 0.45f);
                using (var pen = new Pen(Color.FromArgb(90, RodRed), U(3.4f)) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawCurve(pen, pts, 0.45f);
            }
            using (var pen = new Pen(RodRed, U(1.5f)) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawCurve(pen, pts, 0.45f);
            g.Restore(st);
        }
    }
}
