// Volume.cs - the app's sound level (owner, 12.11.2026): Settings -> General -> Alerts, 0..100 % (100 = the sounds as made).
// System.Media.SoundPlayer has no volume, so each sound's samples are scaled once when it is loaded (and again when the
// level changes); the level follows the ear (a square law: 50 % on the slider is about half as loud, not -6 dB).
// config.json "app": { "sound_volume": 0..100 }
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace TheNorns
{
    partial class DpsForm
    {
        public static int SoundVolume = ReadPercent("sound_volume", 100);
        static readonly Dictionary<string, System.Media.SoundPlayer> players = new Dictionary<string, System.Media.SoundPlayer>();
        static int playersAt = -1;

        // the embedded wav "TheNorns.<name>.wav" at the current level
        internal static void PlaySound(string name)
        {
            try
            {
                if (SoundVolume <= 0 || SelfTesting) return;
                if (playersAt != SoundVolume) { foreach (var p in players.Values) p.Dispose(); players.Clear(); playersAt = SoundVolume; }
                System.Media.SoundPlayer pl;
                if (!players.TryGetValue(name, out pl))
                {
                    byte[] wav;
                    using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("TheNorns." + name + ".wav"))
                    using (var m = new MemoryStream()) { s.CopyTo(m); wav = m.ToArray(); }
                    ScaleWav(wav, Math.Pow(SoundVolume / 100.0, 2));
                    pl = new System.Media.SoundPlayer(new MemoryStream(wav)); pl.Load(); players[name] = pl;
                }
                pl.Play();   // async
            }
            catch { }
        }

        // 16-bit PCM: every sample of the "data" chunk times the gain
        static void ScaleWav(byte[] wav, double gain)
        {
            if (gain >= 0.999) return;
            int i = 12;
            while (i + 8 <= wav.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(wav, i, 4);
                int size = BitConverter.ToInt32(wav, i + 4);
                if (id == "data")
                {
                    int end = Math.Min(wav.Length, i + 8 + size);
                    for (int k = i + 8; k + 1 < end; k += 2)
                    {
                        int v = (int)Math.Round(BitConverter.ToInt16(wav, k) * gain);
                        wav[k] = (byte)(v & 0xFF); wav[k + 1] = (byte)((v >> 8) & 0xFF);
                    }
                    return;
                }
                i += 8 + size + (size & 1);
            }
        }

        // Settings -> General -> Alerts: the volume slider on the section title's row (the track from silent to the accent)
        float PaintVolume(Graphics g, RectangleF c, float y)
        {
            var label = L.T("Volume");
            var hf = TextFont(label, U(7.5f));
            float lw = g.MeasureString(label, hf, 1000, StringFormat.GenericTypographic).Width;
            var vt = SoundVolume + "%";
            var vf = NornsFonts.Screen(U(6.5f));
            float vw = g.MeasureString("100%", vf, 1000, StringFormat.GenericTypographic).Width;
            using (var b = new SolidBrush(BodyTheme.Accent)) g.DrawString(vt, vf, b, c.Right - g.MeasureString(vt, vf, 1000, StringFormat.GenericTypographic).Width, y + (U(11) - vf.GetHeight(g)) / 2, StringFormat.GenericTypographic);
            float tx1 = c.Right - vw - U(8), tx0 = tx1 - U(80), ty = y + U(5.5f);
            using (var b = new SolidBrush(BodyTheme.Label)) g.DrawString(label, hf, b, tx0 - U(8) - lw, y + (U(11) - hf.GetHeight(g)) / 2, StringFormat.GenericTypographic);
            var track = new RectangleF(tx0, ty - U(5.5f), tx1 - tx0, U(11));
            sliderTracks["volume"] = track;
            var bar = new RectangleF(tx0, ty - U(1.5f), tx1 - tx0, U(3));
            using (var p = Round(bar, 1.5f)) using (var b = new SolidBrush(Color.FromArgb(40, BodyTheme.Ink))) g.FillPath(b, p);
            float kx = tx0 + (tx1 - tx0) * SoundVolume / 100f, kr = U(4.2f);
            if (kx > tx0) using (var p = Round(RectangleF.FromLTRB(tx0, bar.Top, kx, bar.Bottom), 1.5f)) using (var b = new SolidBrush(Color.FromArgb(170, BodyTheme.Accent))) g.FillPath(b, p);
            bool khot = hotSpot == "volume" || sliderDrag == "volume";
            using (var b = new SolidBrush(Color.FromArgb(30, 32, 36))) g.FillEllipse(b, kx - kr, ty - kr, 2 * kr, 2 * kr);
            using (var pen = new Pen(khot ? BodyTheme.Accent : BodyTheme.Ink, Math.Max(1f, U(0.8f)))) g.DrawEllipse(pen, kx - kr, ty - kr, 2 * kr, 2 * kr);
            AddKey("volume", track);
            return y + U(14);
        }
    }
}
