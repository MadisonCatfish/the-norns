// Volume.cs - the app's sound level (owner, 12.11.2026): Settings -> General -> Alerts, 0..100 % (100 = the sounds as made).
// waveOut plays the samples as they are, so each sound's samples are scaled once when it is loaded (and again when the
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
        // the sounds at the current level, each a wav in unmanaged memory (it never moves while it plays), played on one
        // waveOut device of our own, opened once and kept open. Owner, 24.11.2026: a play key must cut the sound that plays
        // and start its own at once - System.Media.SoundPlayer (winmm PlaySound, the device opened for each sound) often
        // lost it and a key had to be pressed again. waveOutReset stops the sound now, then the new one is written
        sealed class Wav { public IntPtr Mem, Fmt, Data; public int DataLen; }
        static readonly Dictionary<string, Wav> sounds = new Dictionary<string, Wav>();
        static int soundsAt = -1;
        static IntPtr waveOut, waveHdr;
        static byte[] waveFmt;   // the format the device is open in: all the app's sounds share one, so it stays open
        static bool hdrPrepared;
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct WAVEHDR { public IntPtr lpData; public uint dwBufferLength, dwBytesRecorded; public IntPtr dwUser; public uint dwFlags, dwLoops; public IntPtr lpNext, reserved; }
        static readonly int HdrSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(WAVEHDR));
        [System.Runtime.InteropServices.DllImport("winmm.dll")] static extern int waveOutOpen(out IntPtr hwo, uint device, IntPtr fmt, IntPtr callback, IntPtr instance, uint flags);
        [System.Runtime.InteropServices.DllImport("winmm.dll")] static extern int waveOutPrepareHeader(IntPtr hwo, IntPtr hdr, int size);
        [System.Runtime.InteropServices.DllImport("winmm.dll")] static extern int waveOutUnprepareHeader(IntPtr hwo, IntPtr hdr, int size);
        [System.Runtime.InteropServices.DllImport("winmm.dll")] static extern int waveOutWrite(IntPtr hwo, IntPtr hdr, int size);
        [System.Runtime.InteropServices.DllImport("winmm.dll")] static extern int waveOutReset(IntPtr hwo);
        [System.Runtime.InteropServices.DllImport("winmm.dll")] static extern int waveOutClose(IntPtr hwo);
        const uint WAVE_MAPPER = 0xFFFFFFFF;

        // the sound that plays: stopped now (the device stays open: opening it again each time lost the sound's start
        // while the output woke up - owner, 24.11.2026)
        static void StopSound()
        {
            if (waveOut == IntPtr.Zero) return;
            waveOutReset(waveOut);
            if (hdrPrepared) { waveOutUnprepareHeader(waveOut, waveHdr, HdrSize); hdrPrepared = false; }
        }

        // the embedded wav "TheNorns.<name>.wav" at the current level
        internal static void PlaySound(string name)
        {
            try
            {
                if (SoundVolume <= 0 || SelfTesting) return;
                if (soundsAt != SoundVolume)
                {
                    StopSound();   // its buffer is freed now
                    foreach (var w in sounds.Values) System.Runtime.InteropServices.Marshal.FreeHGlobal(w.Mem);
                    sounds.Clear(); soundsAt = SoundVolume;
                }
                Wav snd;
                if (!sounds.TryGetValue(name, out snd))
                {
                    byte[] wav;
                    using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("TheNorns." + name + ".wav"))
                    using (var m = new MemoryStream()) { s.CopyTo(m); wav = m.ToArray(); }
                    ScaleWav(wav, Math.Pow(SoundVolume / 100.0, 2));
                    snd = new Wav { Mem = System.Runtime.InteropServices.Marshal.AllocHGlobal(wav.Length) };
                    System.Runtime.InteropServices.Marshal.Copy(wav, 0, snd.Mem, wav.Length);
                    // the "fmt " chunk (the format, as waveOutOpen takes it) and the "data" chunk (the samples)
                    for (int i = 12; i + 8 <= wav.Length; )
                    {
                        string id = System.Text.Encoding.ASCII.GetString(wav, i, 4);
                        int size = BitConverter.ToInt32(wav, i + 4);
                        if (id == "fmt ") snd.Fmt = snd.Mem + i + 8;
                        if (id == "data") { snd.Data = snd.Mem + i + 8; snd.DataLen = Math.Min(size, wav.Length - i - 8); }
                        i += 8 + size + (size & 1);
                    }
                    if (snd.Fmt == IntPtr.Zero || snd.Data == IntPtr.Zero) { System.Runtime.InteropServices.Marshal.FreeHGlobal(snd.Mem); return; }
                    sounds[name] = snd;
                }
                StopSound();   // a sound that plays stops at once
                if (waveHdr == IntPtr.Zero) waveHdr = System.Runtime.InteropServices.Marshal.AllocHGlobal(HdrSize);
                var fmt = new byte[16]; System.Runtime.InteropServices.Marshal.Copy(snd.Fmt, fmt, 0, 16);
                if (waveOut != IntPtr.Zero && (waveFmt == null || !System.Linq.Enumerable.SequenceEqual(fmt, waveFmt))) { waveOutClose(waveOut); waveOut = IntPtr.Zero; }
                if (waveOut == IntPtr.Zero)
                {
                    if (waveOutOpen(out waveOut, WAVE_MAPPER, snd.Fmt, IntPtr.Zero, IntPtr.Zero, 0) != 0) { waveOut = IntPtr.Zero; return; }
                    waveFmt = fmt;
                }
                System.Runtime.InteropServices.Marshal.StructureToPtr(new WAVEHDR { lpData = snd.Data, dwBufferLength = (uint)snd.DataLen }, waveHdr, false);
                waveOutPrepareHeader(waveOut, waveHdr, HdrSize); hdrPrepared = true;
                waveOutWrite(waveOut, waveHdr, HdrSize);
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
