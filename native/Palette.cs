// Palette.cs - the app's colours (roadmap 8, owner 07..12.11.2026): Settings -> Display -> Colours.
// Eye comfort (owner, 12.11.2026, instead of a choice of accents): the app sits beside the game, in the player's peripheral
// vision, where brightness, saturation and glow pull the eye away from the game. With it on, the app keeps every number
// and every meaning but takes the colour load down, after the usual low-strain practice for secondary displays:
// - the screen's white is a soft grey (no pure white on black: less halation, contrast still ~11 : 1);
// - the accent family (the lamps, the ISK grid, the plate, the power display) loses most of its saturation and some light;
// - the second colour (the smartbomb bars) becomes a muted indigo, the pilots' lines follow the softer white;
// - no glow: no halos round the text and the lamps (glow reads as light to the eye and draws it);
// - the meanings keep their full colour - the titan red, the dread orange, the faction violet - so the eye is pulled
//   only when something happens (salience kept for the events, taken from the steady state).
// Each family: the accent, the power display's lamp, a lamp off, the LOG lamp's gradient, the ISK grid's heat stops, the
// second colour with its light and dark ends. Entry 0 is the shipped acid yellow / ultramarine, exactly.
// The hull: its saturation 0..100 % (50 = the shipped #4E5768), and its brightness 0..100 % (50 = shipped): from an almost
// black graphite to a light steel.
// config.json "app": { "eye_comfort": true|false, "hull_saturation": 0..100, "hull_brightness": 0..100 }
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace TheNorns
{
    static class Palette
    {
        public sealed class Entry
        {
            public readonly string Name;
            public readonly Color Accent, Lamp, LampOff, LampCoreOff, LampCoreOn, LampEdgeOff, LampEdgeOn, Second, SecondHi, SecondLo;
            public readonly Color[] Heat;
            public Entry(string name, Color accent, Color lamp, Color lampOff, Color coreOff, Color coreOn, Color edgeOff, Color edgeOn,
                Color h1, Color h2, Color h3, Color h4, Color second, Color secondHi, Color secondLo)
            {
                Name = name; Accent = accent; Lamp = lamp; LampOff = lampOff; LampCoreOff = coreOff; LampCoreOn = coreOn; LampEdgeOff = edgeOff; LampEdgeOn = edgeOn;
                Heat = new[] { h1, h2, h3, h4, accent }; Second = second; SecondHi = secondHi; SecondLo = secondLo;
            }
        }
        static Color C(int r, int g, int b) { return Color.FromArgb(r, g, b); }

        public static readonly Entry[] All =
        {
            new Entry("Acid", C(247, 255, 20), C(225, 255, 0), C(46, 48, 30), C(70, 72, 20), C(255, 255, 200), C(40, 42, 12), C(225, 240, 0), C(14, 15, 8), C(52, 56, 10), C(128, 134, 14), C(200, 208, 18), C(61, 43, 255), C(175, 166, 255), C(28, 18, 150)),
            new Entry("Comfort", C(190, 196, 132), C(184, 198, 133), C(39, 40, 33), C(59, 60, 43), C(191, 191, 171), C(33, 34, 24), C(188, 196, 135), C(11, 11, 8), C(44, 46, 31), C(109, 113, 78), C(190, 195, 141), C(65, 79, 153), C(153, 151, 187), C(32, 42, 87)),
        };

        public static int IndexOf(string name)
        {
            for (int i = 0; i < All.Length; i++) if (string.Equals(All[i].Name, name, StringComparison.OrdinalIgnoreCase)) return i;
            return 0;
        }

        // ---- OKLab (Björn Ottosson) for the hull's chroma ----
        static double Lin(int c) { double v = c / 255.0; return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4); }
        static int Gam(double v) { v = Math.Max(0, Math.Min(1, v)); return (int)Math.Round(255 * (v <= 0.0031308 ? 12.92 * v : 1.055 * Math.Pow(v, 1 / 2.4) - 0.055)); }
        public static void ToOklch(Color c, out double L, out double C, out double h)
        {
            double r = Lin(c.R), g = Lin(c.G), b = Lin(c.B);
            double l = Math.Pow(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b, 1 / 3.0);
            double m = Math.Pow(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b, 1 / 3.0);
            double s = Math.Pow(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b, 1 / 3.0);
            L = 0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s;
            double A = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s, B = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;
            C = Math.Sqrt(A * A + B * B); h = Math.Atan2(B, A);
        }
        public static Color FromOklch(double L, double C, double h)
        {
            double A = C * Math.Cos(h), B = C * Math.Sin(h);
            double l = Math.Pow(L + 0.3963377774 * A + 0.2158037573 * B, 3), m = Math.Pow(L - 0.1055613458 * A - 0.0638541728 * B, 3), s = Math.Pow(L - 0.0894841775 * A - 1.2914855480 * B, 3);
            return Color.FromArgb(Gam(4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s), Gam(-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s), Gam(-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s));
        }

        public static readonly Color ShippedHull = Color.FromArgb(78, 87, 104);
        // the hull's lightness (OKLCH L; shipped 0.455): down to an almost black graphite (owner, 12.11.2026), its colour
        // fading as it darkens; up to 0.58, where the white print (the name, NRN-01) still keeps 3 : 1
        const double DarkL = 0.19, LightL = 0.58;
        // saturation: 0 = graphite, 50 = shipped, 100 = twice its chroma; brightness: 0 = DarkL, 50 = shipped, 100 = LightL
        public static Color Hull(int saturation, int brightness)
        {
            if (saturation == 50 && brightness == 50) return ShippedHull;
            double L, C, h; ToOklch(ShippedHull, out L, out C, out h);
            double t = (Math.Max(0, Math.Min(100, brightness)) - 50) / 50.0;
            double l2 = t < 0 ? L + t * (L - DarkL) : L + t * (LightL - L);
            double c2 = C * Math.Max(0, Math.Min(100, saturation)) / 50.0 * Math.Min(1, l2 / L);
            return FromOklch(l2, c2, h);
        }
    }

    partial class DpsForm
    {
        public static bool EyeComfort = AppSetting("eye_comfort") is bool && (bool)AppSetting("eye_comfort");
        public static int AccentIndex { get { return EyeComfort ? 1 : 0; } }
        public static readonly Color ShippedInk = Color.FromArgb(242, 243, 245), ComfortInk = Color.FromArgb(198, 202, 208);
        public static int HullSaturation = ReadPercent("hull_saturation"), HullBrightness = ReadPercent("hull_brightness");
        static int ReadPercent(string key, int def = 50)
        {
            object v = AppSetting(key);
            double d; return v != null && double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? (int)Math.Max(0, Math.Min(100, Math.Round(d))) : def;
        }

        // the tokens from the choice; called once at start (Program) and on every change
        public static void ApplyTheme()
        {
            var e = Palette.All[Math.Max(0, Math.Min(Palette.All.Length - 1, AccentIndex))];
            BodyTheme.Accent = e.Accent; BodyTheme.Lamp = e.Lamp; BodyTheme.LampOff = e.LampOff;
            BodyTheme.LampCoreOff = e.LampCoreOff; BodyTheme.LampCoreOn = e.LampCoreOn; BodyTheme.LampEdgeOff = e.LampEdgeOff; BodyTheme.LampEdgeOn = e.LampEdgeOn;
            BodyTheme.HeatStops = e.Heat; BodyTheme.Accent2 = e.Second; BodyTheme.Accent2Hi = e.SecondHi; BodyTheme.Accent2Lo = e.SecondLo;
            BodyTheme.Hull = Palette.Hull(HullSaturation, HullBrightness);
            BodyTheme.Ink = EyeComfort ? ComfortInk : ShippedInk;
        }
        void ThemeChanged()
        {
            ApplyTheme();
            if (shell != null) { shell.Dispose(); shell = null; }   // the hull is in the kept shell
        }

        // Settings -> Display: the colours' section (y: its top; returns the next y)
        float PaintColours(Graphics g, RectangleF c, float y)
        {
            var ct = L.T("Colours");
            Glow(g, ct, TitleFont(ct, U(7.2f)), Muted, c.Left, y);
            y += U(11);
            // eye comfort: one switch (the colour load down, the data kept)
            LampRow(g, c, y, "eyecomfort", EyeComfort, L.T("Eye comfort: muted colours, no glow"));
            y += RowStep;
            // the hull: its saturation and its brightness, two sliders
            y = HullSlider(g, c, y, "hull:sat", L.T("Hull saturation"), HullSaturation, v => Palette.Hull(v, HullBrightness));
            y = HullSlider(g, c, y, "hull:bright", L.T("Hull brightness"), HullBrightness, v => Palette.Hull(HullSaturation, v));
            return y;
        }

        // a slider: the label, a track showing what it does (its 0 -> shipped -> 100 colours, the shipped one marked), a knob
        // in the hull's colour, the value on the right
        float HullSlider(Graphics g, RectangleF c, float y, string id, string label, int value, Func<int, Color> at)
        {
            var hf = TextFont(label, U(7.5f));
            using (var b = new SolidBrush(BodyTheme.Label)) g.DrawString(label, hf, b, c.Left, y + (U(15) - hf.GetHeight(g)) / 2, StringFormat.GenericTypographic);
            // the value round the shipped colour: 0 in the middle, -50 .. +50 (owner, 12.11.2026); stored 0..100
            int shown = value - 50;
            var vt = shown > 0 ? "+" + shown : shown < 0 ? "\u2212" + (-shown) : "0";
            var vf = NornsFonts.Screen(U(6.5f));
            float vw = g.MeasureString("+50", vf, 1000, StringFormat.GenericTypographic).Width;
            using (var b = new SolidBrush(shown == 0 ? BodyTheme.Label : BodyTheme.Accent)) g.DrawString(vt, vf, b, c.Right - g.MeasureString(vt, vf, 1000, StringFormat.GenericTypographic).Width, y + (U(15) - vf.GetHeight(g)) / 2, StringFormat.GenericTypographic);
            float tx0 = c.Left + U(92), tx1 = c.Right - vw - U(10), ty = y + U(7.5f);
            var track = new RectangleF(tx0, ty - U(6.5f), tx1 - tx0, U(13));
            sliderTracks[id] = track;
            var bar = new RectangleF(tx0, ty - U(1.5f), tx1 - tx0, U(3));
            using (var lg = new System.Drawing.Drawing2D.LinearGradientBrush(bar, at(0), at(100), 0f))
            {
                lg.InterpolationColors = new System.Drawing.Drawing2D.ColorBlend { Colors = new[] { at(0), at(50), at(100) }, Positions = new[] { 0f, 0.5f, 1f } };
                using (var p = Round(bar, 1.5f)) g.FillPath(lg, p);
            }
            // the 0: a tick through the track in the middle (the shipped colour)
            using (var b = new SolidBrush(Color.FromArgb(150, BodyTheme.Ink))) g.FillRectangle(b, tx0 + (tx1 - tx0) * 0.5f - 0.5f, ty - U(4), 1, U(8));
            float kx = tx0 + (tx1 - tx0) * value / 100f, kr = U(4.2f);
            bool khot = hotSpot == id || sliderDrag == id;
            using (var b = new SolidBrush(BodyTheme.Hull)) g.FillEllipse(b, kx - kr, ty - kr, 2 * kr, 2 * kr);
            using (var pen = new Pen(khot ? BodyTheme.Accent : BodyTheme.Ink, Math.Max(1f, U(0.8f)))) g.DrawEllipse(pen, kx - kr, ty - kr, 2 * kr, 2 * kr);
            AddKey(id, track);
            return y + U(17);
        }

        readonly System.Collections.Generic.Dictionary<string, RectangleF> sliderTracks = new System.Collections.Generic.Dictionary<string, RectangleF>();
        string sliderDrag;
        // a press starts a drag; a double click puts it back on 0, the shipped colour (owner, 12.11.2026; the volume to 100 %)
        bool BeginHullDrag(string spot, Point p, int clicks)
        {
            if (spot == null || !sliderTracks.ContainsKey(spot)) return false;
            if (clicks >= 2)
            {
                if (spot == "hull:sat") { HullSaturation = 50; ThemeChanged(); SaveApp("hull_saturation", 50); }
                else if (spot == "hull:bright") { HullBrightness = 50; ThemeChanged(); SaveApp("hull_brightness", 50); }
                else if (spot == "volume") { SoundVolume = 100; SaveApp("sound_volume", 100); }
                Render(); return true;
            }
            sliderDrag = spot; MoveHullDrag(p.X); return true;
        }
        bool MoveHullDrag(int x)
        {
            if (sliderDrag == null) return false;
            var t = sliderTracks[sliderDrag];
            int v = (int)Math.Round(Math.Max(0, Math.Min(1, (x - t.Left) / t.Width)) * 20) * 5;   // 5 % steps
            if (sliderDrag == "hull:sat" && v != HullSaturation) { HullSaturation = v; ThemeChanged(); }
            if (sliderDrag == "hull:bright" && v != HullBrightness) { HullBrightness = v; ThemeChanged(); }
            if (sliderDrag == "volume") SoundVolume = v;   // Volume.cs
            Render(); return true;
        }
        bool EndHullDrag()
        {
            if (sliderDrag == null) return false;
            if (sliderDrag == "volume") { SaveApp("sound_volume", SoundVolume); PlayFaction(); }   // let it be heard
            else SaveApp(sliderDrag == "hull:sat" ? "hull_saturation" : "hull_brightness", sliderDrag == "hull:sat" ? HullSaturation : HullBrightness);
            sliderDrag = null; return true;
        }

        bool ActivateColour(string id)
        {
            if (id != "eyecomfort") return false;
            EyeComfort = !EyeComfort; SaveApp("eye_comfort", EyeComfort); ThemeChanged();
            return true;
        }
    }
}
