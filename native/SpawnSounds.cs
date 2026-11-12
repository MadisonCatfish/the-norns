// SpawnSounds.cs - two optional sounds in Settings -> General -> Alerts, both off by default (owner, 12.11.2026):
// - a faction NPC or a commander spawns (a ★): "Toggle" - a toggle switch snapping over (two tight plastic clicks, a short
//   spring ring) and a rising digital triplet (res\spawn_faction_toggle.wav);
// - a re-warp with no damage (the red count-down on the Vorton screen: the site was taken): "Breaker" - a breaker thrown,
//   a metal latch striking a plastic housing, the module's buzz, a beep (res\rewarp_breaker.wav).
// Synthesized by a script, no samples, after a physical control on a sci-fi ship's console. Earcon practice: under 1 s,
// no click at the start, energy mostly 150..5000 Hz so small speakers carry it. All three sounds (these two and the
// titan / dreadnought alert) at one loudness (owner, 12.11.2026). Played at the app's volume (Volume.cs).
// config.json "app": { "faction_sound": true|false, "rewarp_sound": true|false }
using System;
using System.Drawing;

namespace TheNorns
{
    partial class DpsForm
    {
        public static bool FactionSound = AppSetting("faction_sound") is bool && (bool)AppSetting("faction_sound");
        public static bool RewarpSound = AppSetting("rewarp_sound") is bool && (bool)AppSetting("rewarp_sound");
        int factionHeard = -1;
        DateTime rewarpHeard = DateTime.MinValue;

        public static void PlayFaction() { PlaySound("spawn_faction_toggle"); }
        public static void PlayRewarp() { PlaySound("rewarp_breaker"); }

        // a new ★: the sound, unless it is an old one read back from the log at start, or a titan / dread alert sounds now
        void SoundFactionSpawns(DateTime now)
        {
            int n = stats.Spawns.Count;
            if (factionHeard < 0 || n < factionHeard) { factionHeard = n; return; }
            if (n == factionHeard) return;
            factionHeard = n;
            bool loudNow = SpawnSound && (now - stats.LastLoudSpawn).TotalSeconds < 3;
            if (FactionSound && !loudNow && (now - stats.Spawns[n - 1].Item1).TotalSeconds < 60) PlayFaction();
        }

        // the red count-down of a re-warp with no damage has come up (PaintAnomalyTimer): once per warp, live only
        void SoundRewarp(DateTime warp)
        {
            if (warp == rewarpHeard) return;
            bool first = rewarpHeard == DateTime.MinValue && (DateTime.UtcNow - warp).TotalSeconds > 60;   // an old one read back at start
            rewarpHeard = warp;
            if (RewarpSound && !first && !SelfTesting && DemoNow == null) PlayRewarp();
        }

        // Settings -> General -> Alerts: the two switches, each with a play key
        float PaintAlertSounds(Graphics g, RectangleF c, float y)
        {
            y = SoundRow(g, c, y, "factionsound", FactionSound, L.T("Sound on a faction spawn"), "playfaction");
            y = SoundRow(g, c, y, "rewarpsound", RewarpSound, L.T("Sound on a re-warp with no damage"), "playrewarp");
            return y;
        }
        float SoundRow(Graphics g, RectangleF c, float y, string id, bool on, string label, string playId)
        {
            var pk = new RectangleF(c.Right - U(29), y, U(29), U(15));
            var sr = new RectangleF(c.Left, y, pk.Left - U(4) - c.Left, U(15));
            bool sh = hotSpot == id;
            if (sh) using (var p = Round(sr, 6)) using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillPath(b, p);
            float sd = U(5.5f), sx = sr.Left + U(5), sy = sr.Top + sr.Height / 2;
            Lamp(g, sx, sy, sd, on);
            var lf = TextFont(label, U(7.5f));
            using (var b = new SolidBrush(sh ? BodyTheme.Ink : BodyTheme.Label)) g.DrawString(label, lf, b, sx + sd + U(6), sy - lf.GetHeight(g) / 2, StringFormat.GenericTypographic);
            spots.Add(Tuple.Create(id, sr));
            bool ph = hotSpot == playId;
            using (var p = Round(pk, 5))
            {
                using (var b = new SolidBrush(ph ? Color.FromArgb(26, BodyTheme.Ink) : Color.FromArgb(12, BodyTheme.Ink))) g.FillPath(b, p);
                using (var pen = new Pen(Color.FromArgb(50, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
            }
            float ic = U(9);
            using (var b = new SolidBrush(BodyTheme.Ink)) MaterialIcon.Draw(g, MaterialIcon.Play, new RectangleF(pk.Left + (pk.Width - ic) / 2, pk.Top + (pk.Height - ic) / 2, ic, ic), b);
            AddKey(playId, pk);
            return y + RowStep;
        }

        bool ActivateFactionSound(string id)
        {
            if (id == "factionsound") { FactionSound = !FactionSound; SaveApp("faction_sound", FactionSound); if (FactionSound) PlayFaction(); return true; }
            if (id == "playfaction") { PlayFaction(); return true; }
            if (id == "rewarpsound") { RewarpSound = !RewarpSound; SaveApp("rewarp_sound", RewarpSound); if (RewarpSound) PlayRewarp(); return true; }
            if (id == "playrewarp") { PlayRewarp(); return true; }
            return false;
        }
    }
}
