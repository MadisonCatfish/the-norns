// SwitchName.cs - the character's name on the power display's animation (owner, 12.11.2026): with Carousel on and the
// live client picture off, a switch to another client blurs the animation and its character's name comes up over it at
// once, in the same size and place as over the live picture; 5 s later the name goes and the animation is sharp again.
// The animation keeps running under the blur, so the display never stands still.
// Timeline (s after the switch): 0..0.18 the blur and the name come in, to 4.55 held, 4.55..5 both go.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace TheNorns
{
    partial class DpsForm
    {
        const double SwitchNameHold = 5, SwitchNameIn = 0.18, SwitchNameOut = 0.45;
        IntPtr switchSeen = IntPtr.Zero;      // the client in front at the last frame
        string switchName; double switchAt = -1;
        public string DemoSwitchName; public double? DemoSwitchAge;   // selftest: the name and the seconds since the switch

        // called each frame while the animation shows under Carousel: a new client in front starts the name
        void WatchSwitch()
        {
            if (fleet == null) return;
            var a = fleet.Clients.ActiveClient;
            var h = a == null ? IntPtr.Zero : a.Hwnd;
            if (h == switchSeen) return;
            bool first = switchSeen == IntPtr.Zero;
            switchSeen = h;
            if (first || a == null || a.Character == null) return;
            // switches one after another (owner, 12.11.2026): the blur is already there, so it is not started again from a
            // sharp picture (that jerked) - it goes on from where it is (full, or partly gone while fading out), the name is
            // the new one and its 5 s start again
            string old; double level = SwitchLevel(out old);
            double t = 1 - Math.Sqrt(Math.Max(0, 1 - level));   // the fade-in's own point at this level
            switchName = a.Character;
            switchAt = Clock - t * SwitchNameIn;
        }

        // 0..1: how far the blur and the name are in
        double SwitchLevel(out string name)
        {
            name = DemoSwitchName ?? switchName;
            double age = DemoSwitchAge ?? (switchAt < 0 ? double.MaxValue : Clock - switchAt);
            if (name == null || age < 0 || age >= SwitchNameHold) return 0;
            if (age < SwitchNameIn) { double t = age / SwitchNameIn; return 1 - (1 - t) * (1 - t); }           // fast in
            if (age > SwitchNameHold - SwitchNameOut) { double t = (SwitchNameHold - age) / SwitchNameOut; return t * t * (3 - 2 * t); }
            return 1;
        }

        // the animation, blurred by `level` (drawn small and stretched back up: GDI+ has no blur), and the name over it
        void PaintAnimWithSwitch(Graphics g, RectangleF face, Action<Graphics, RectangleF> anim)
        {
            string name;
            double level = SwitchLevel(out name);
            if (level <= 0.001) { anim(g, face); return; }
            if (Potato) { anim(g, face); PaintSwitchName(g, face, name, level, 0); return; }   // Potato: no blur, the name plain
            int w = Math.Max(1, (int)Math.Ceiling(face.Width)), h = Math.Max(1, (int)Math.Ceiling(face.Height));
            using (var sharp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (var sg = Graphics.FromImage(sharp))
                {
                    sg.SmoothingMode = SmoothingMode.AntiAlias;
                    anim(sg, new RectangleF(0, 0, face.Width, face.Height));
                }
                // the blur: a 1/7 copy stretched back up, twice (a smoother falloff than one step)
                int sw = Math.Max(2, w / 7), shh = Math.Max(2, h / 7);
                using (var small = new Bitmap(sw, shh, PixelFormat.Format32bppArgb))
                using (var blur = new Bitmap(w, h, PixelFormat.Format32bppArgb))
                {
                    using (var smg = Graphics.FromImage(small)) { smg.InterpolationMode = InterpolationMode.HighQualityBilinear; smg.DrawImage(sharp, new Rectangle(0, 0, sw, shh)); }
                    using (var bg = Graphics.FromImage(blur)) { bg.InterpolationMode = InterpolationMode.HighQualityBilinear; bg.PixelOffsetMode = PixelOffsetMode.Half; bg.DrawImage(small, new Rectangle(0, 0, w, h)); }
                    var dest = new Rectangle((int)Math.Floor(face.X), (int)Math.Floor(face.Y), w, h);
                    // the sharp one fades out as the blurred one (a touch dimmer, so the name reads) fades in
                    DrawAlpha(g, sharp, dest, (float)(1 - level));
                    DrawAlpha(g, blur, dest, (float)(level * 0.75));
                }
            }
            PaintSwitchName(g, face, name, level, 1 - level);
        }

        static void DrawAlpha(Graphics g, Bitmap b, Rectangle dest, float a)
        {
            if (a <= 0.004) return;
            using (var ia = new ImageAttributes())
            {
                ia.SetColorMatrix(new ColorMatrix { Matrix33 = Math.Min(1, a) });
                g.DrawImage(b, dest, 0, 0, b.Width, b.Height, GraphicsUnit.Pixel, ia);
            }
        }

        // the name exactly where and as the live picture shows it (owner, 12.11.2026: its size and place unchanged) - at
        // the glass's foot on a dark fade, running by when too long - only faded in and out with the blur
        void PaintSwitchName(Graphics g, RectangleF face, string name, double alpha, double grow)
        {
            PaintPreviewName(g, face, name, DemoSeconds ?? Clock, alpha);
        }
    }
}
