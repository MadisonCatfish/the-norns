// The Norns - Settings -> Eve-O (owner, 29.10.2026): the client preview & switching module (Fleet.cs), off by default.
// A lamp switches it on; its hotkeys are set here: a click on a field, then the keys (Backspace clears it, Esc leaves it
// as it was). The keys are kept in config.json "app": { "fleet": true, "fleet_next": "Control+2", "fleet_prev": ... }.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TheNorns
{
    // the EVE client's default keys (EVE University wiki, "Keyboard controls", as of Havoc 21.06 - not read from the
    // game), written as EVE-O writes keys; a hotkey set here on one of them gets a warning next to its field.
    // Modifier-only clicks (Ctrl, Alt, Shift) and mouse buttons are left out.
    static class EveKeys
    {
        static readonly string[] Defaults =
        {
            // windows
            "Alt+V|Activity Tracker", "Alt+A|Character Sheet", "Alt+E|Contacts", "Alt+D|Directional Scanner", "F12|EVE Help",
            "Alt+F|Fitting", "Alt+S|Industry", "Alt+C|Inventory", "Alt+I|Mail", "F11|Map Browser", "F10|Map", "Alt+4|New Eden Store",
            "Alt+G|Open Item Hangar", "Control+F|Open FPS Monitor", "Alt+N|Open Ship Hangar", "Alt+J|Opportunities",
            "Alt+T|Personal Assets", "Alt+P|Probe Scanner", "Alt+Y|Redeem Items", "Alt+R|Regional Market", "Space|Set Channel Focus",
            "Alt+Space|Set Overview Focus", "Alt+X|Skills", "F9|Solar System Map", "Alt+M|The Agency", "Alt+W|Wallet", "L|Locations",
            "Control+W|Close Active Window", "Control+Alt+W|Close All Windows", "Oem5|Neocom Menu", "Shift+Back|Force fade from black",
            // combat
            "Shift+1|Activate Defense Mode", "Shift+2|Activate Sharpshooter Mode", "Shift+3|Activate Propulsion Mode", "A|Align to",
            "Q|Approach", "V|Directional Scan", "D|Dock / Jump / Activate gate", "E|Keep at Range", "W|Orbit", "B|Refresh Scan Probe",
            "T|Show info", "1|Tag item as: 1", "2|Tag item as: 2", "3|Tag item as: 3", "4|Tag item as: A", "5|Tag item as: B",
            "6|Tag item as: C", "7|Tag item from sequence: 1, 2 or 3", "8|Tag item from sequence: A, B or C",
            "9|Tag item from sequence: X, Y or Z", "Shift+C|Toggle Auto Tracking", "C|Track", "S|Warp to",
            // general
            "Control+Shift+Tab|Browse Window Menu Upwards", "Control+Tab|Browse Window Menu", "Escape|Close modal windows / config menu",
            "Enter|Confirm", "Control+C|Copy", "Control+X|Cut", "Control+V|Paste", "Control+A|Select All", "Alt+F9|Hide Mouse Cursor",
            "Control+Shift+PageDown|Next stacked window", "Control+Shift+PageUp|Previous stacked window", "Control+PageDown|Next Tab",
            "Control+PageUp|Previous Tab", "Control+Alt+Shift+M|Open Monitor", "Alt+Shift+Q|Quit Game", "Control+Alt+Enter|Reset Monitor",
            "Shift+S|Set Search Bar Focus", "Tab|Tab to next edit field", "Shift+Tab|Tab to previous edit field",
            "Control+Shift+F9|Toggle 3D view", "Control+Alt+Shift+F12|Toggle Audio", "Control+Alt+Shift+E|Toggle effects",
            "Control+Alt+Shift+T|Toggle turret effects", "Control+F9|Toggle User Interface Visibility", "Alt+Enter|Toggle Windowed",
            "Control+Shift+A|Zoom In", "Control+Shift+Z|Zoom Out",
            // navigation
            "Add|Accelerate", "Subtract|Decelerate", "Control+S|Autopilot", "Control+Space|Stop Ship", "Control+Alt+Space|Set Full Speed",
            "Left|Turn Left", "Right|Turn Right", "Up|Pitch Up", "Down|Pitch Down", "Alt+Left|Select previous target",
            "Alt+Right|Select next target", "Control+B|Save Location", "Control+O|Sensor Overlay", "Control+D|Tactical Overlay",
            "Alt+1|Tactical Camera", "Alt+2|Orbit Camera", "Alt+3|First Person Camera", "Alt+Z|Toggle show all brackets",
            "Alt+Shift+Z|Toggle show no brackets", "Alt+Shift+X|Toggle Special Bracket Display", "Z|Broadcast: Spotted the Enemy",
            "X|Broadcast: Target (Selected)",
            // modules
            "Control+R|Reload Ammo", "Control+3|Toggle Overload on High Power Rack", "Control+1|Toggle Overload on Low Power Rack",
            "Control+2|Toggle Overload on Medium Power Rack",
            // drones
            "F|All Drones: Engage", "Alt+Shift+R|All Drones: Return and Orbit", "Shift+R|All Drones: Return to Drone Bay", "Shift+F|Launch Drones",
        };

        static Dictionary<uint, string> byKey;

        static uint Code(uint mods, uint vk) { return mods << 16 | vk; }

        // the client's action on that key, or null
        public static string Action(string key)
        {
            if (byKey == null)
            {
                byKey = new Dictionary<uint, string>();
                Action<string, string> add = (k, a) => { uint m, v; if (Hotkeys.Parse(k, out m, out v) && !byKey.ContainsKey(Code(m, v))) byKey[Code(m, v)] = a; };
                foreach (var d in Defaults) { var p = d.Split('|'); add(p[0], p[1]); }
                // the module slots: F1-F8 high, Alt mid, Ctrl low; with Shift they overload
                for (int i = 1; i <= 8; i++)
                {
                    add("F" + i, "Activate High Power Slot " + i); add("Alt+F" + i, "Activate Medium Power Slot " + i); add("Control+F" + i, "Activate Low Power Slot " + i);
                    add("Shift+F" + i, "Toggle Overload on High Power Slot " + i); add("Alt+Shift+F" + i, "Toggle Overload on Medium Power Slot " + i);
                    add("Control+Shift+F" + i, "Toggle Overload on Low Power Slot " + i);
                }
            }
            uint mods, vk; string action;
            return Hotkeys.Parse(key, out mods, out vk) && byKey.TryGetValue(Code(mods, vk), out action) ? action : null;
        }
    }

    partial class DpsForm
    {
        public static bool FleetOn = Fleet.Enabled;
        string keyCapture;   // the hotkey being set: "fleet_next" or "fleet_prev"; null while none
        public Dictionary<string, string[]> DemoKeys;   // selftest: the keys instead of config.json
        public Dictionary<string, string> DemoKeyState;   // selftest: "taken" for a key another program holds

        void StartFleet()
        {
            if (fleet != null) return;
            fleet = new Fleet();
            LocationChanged += FleetFollow;
            VisibleChanged += FleetVisible;
        }

        void StopFleet()
        {
            if (fleet == null) return;
            LocationChanged -= FleetFollow;
            VisibleChanged -= FleetVisible;
            fleet.Dispose();
            fleet = null;
        }

        void FleetFollow(object s, EventArgs e) { if (fleet == null) return; var a = PreviewArea; a.Offset(Location); fleet.Preview.Follow(a); }
        void FleetVisible(object s, EventArgs e) { if (fleet != null && !Visible) fleet.Preview.Hide(); }

        // a click on the page (Activate); false = not ours
        bool ActivateEveO(string id)
        {
            if (keyCapture != null && id != "eveo:key:" + keyCapture) EndCapture();
            if (id == "eveo:on")
            {
                FleetOn = !FleetOn; SaveApp("fleet", FleetOn);
                if (SelfTesting) return true;
                if (FleetOn) StartFleet(); else StopFleet();
                return true;
            }
            if (id.StartsWith("eveo:key:"))
            {
                keyCapture = id.Substring(9);
                if (fleet != null) fleet.SuspendKeys();   // the keys pressed now are for the field, not for switching
                return true;
            }
            return false;
        }

        void EndCapture()
        {
            keyCapture = null;
            if (fleet != null) fleet.ReloadKeys();
        }

        // the keys for the field being set (ProcessCmdKey): a modifier alone waits for its key
        bool CaptureKey(Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu || key == Keys.LWin || key == Keys.RWin) return true;
            var setting = keyCapture;
            if (key == Keys.Back || key == Keys.Delete) SaveApp(setting, new string[0]);
            else if (key != Keys.Escape) SaveApp(setting, KeyName(keyData));
            EndCapture();
            Render();
            return true;
        }

        // as EVE-O writes keys: "Control+Shift+F16", a digit alone as itself
        static string KeyName(Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            string k = key >= Keys.D0 && key <= Keys.D9 ? ((char)('0' + (key - Keys.D0))).ToString() : key.ToString();
            return ((keyData & Keys.Control) != 0 ? "Control+" : "") + ((keyData & Keys.Shift) != 0 ? "Shift+" : "") + ((keyData & Keys.Alt) != 0 ? "Alt+" : "") + k;
        }

        // on the key: "CTRL + 2"
        static string KeyLabel(string key)
        {
            return string.Join(" + ", key.Split('+').Select(p => p.Trim()).Select(p => p.Equals("Control", StringComparison.OrdinalIgnoreCase) ? "Ctrl" : p).Select(p => p.ToUpperInvariant()));
        }

        IList<string> KeysOf(string setting)
        {
            string[] k;
            if (DemoKeys != null) return DemoKeys.TryGetValue(setting, out k) ? k : new string[0];
            return Fleet.Configured(setting).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }

        void PaintEveO(Graphics g, RectangleF c, float y)
        {
            // on / off: a lamp row as on General
            var ol = L.T("Live client on the power display, client switching");
            var orr = new RectangleF(c.Left, y, c.Width, U(15));
            bool oh = hotSpot == "eveo:on";
            if (oh) using (var p = Round(orr, 6)) using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillPath(b, p);
            float dd = U(5.5f), lx = orr.Left + U(5), ly = orr.Top + orr.Height / 2;
            if (FleetOn) SoftGlow(g, new PointF(lx + dd / 2, ly), dd / 2 + U(4), BodyTheme.Accent, 140);
            using (var b = new SolidBrush(FleetOn ? BodyTheme.Accent : Color.FromArgb(46, 48, 30))) g.FillEllipse(b, lx, ly - dd / 2, dd, dd);
            var of = TextFont(ol, U(7.5f));
            using (var b = new SolidBrush(oh ? BodyTheme.Ink : Color.FromArgb(200, 203, 210))) g.DrawString(ol, of, b, lx + dd + U(6), ly - of.GetHeight(g) / 2, StringFormat.GenericTypographic);
            spots.Add(Tuple.Create("eveo:on", orr));
            y += U(23);

            // the hotkeys: a label and a key-like field each
            var ht = L.T("Hotkeys");
            Glow(g, ht, TitleFont(ht, U(6.5f)), Muted, c.Left, y);
            y += U(11);
            float labelW = U(70), fh = U(15);
            foreach (var row in new[] { new[] { "fleet_next", "Next client" }, new[] { "fleet_prev", "Previous client" } })
            {
                RowLabel(g, L.T(row[1]), c.Left, y, fh);
                KeyField(g, RectangleF.FromLTRB(c.Left + labelW, y, c.Right - fh - U(4), y + fh), row[0]);
                // a key the EVE client has by default: the band's warning triangle in a yellow square key, its action as a tip
                if (keyCapture != row[0] && KeysOf(row[0]).Any(k => EveKeys.Action(k) != null))
                    WarnBadge(g, new RectangleF(c.Right - fh, y, fh, fh), "eveo:warn:" + row[0]);
                y += fh + U(6);
            }
            y += U(3);
            var hint = L.T("Click a field and press the keys. Backspace clears it, Esc leaves it as it was. A key another program already holds can't be used.");
            var hf = TextFont(hint, U(6.6f));
            var hh = g.MeasureString(hint, hf, (int)c.Width).Height;
            using (var b = new SolidBrush(Color.FromArgb(150, BodyTheme.Ink))) g.DrawString(hint, hf, b, new RectangleF(c.Left, y, c.Width, hh + U(2)));
        }

        void WarnBadge(Graphics g, RectangleF r, string id)
        {
            bool hot = hotSpot == id;
            using (var p = Round(r, 5))
            {
                using (var b = new SolidBrush(Color.FromArgb(hot ? 60 : 34, BodyTheme.Accent))) g.FillPath(b, p);
                using (var pen = new Pen(BodyTheme.Accent, 1f)) g.DrawPath(pen, p);
            }
            WarnTriangle(g, r.Left + r.Width / 2, r.Top + r.Height / 2 + U(0.3f), U(1.45f), BodyTheme.Accent);
            spots.Add(Tuple.Create(id, r));
        }

        // the tip over the warning: what the EVE client does on those keys
        string WarnTip(string spot)
        {
            if (spot == null || !spot.StartsWith("eveo:warn:")) return null;
            var lines = KeysOf(spot.Substring(10)).Select(k => new { k, a = EveKeys.Action(k) }).Where(x => x.a != null).Select(x => KeyLabel(x.k) + " — " + x.a);
            return L.T("The EVE client uses this key by default:") + "\n" + string.Join("\n", lines);
        }

        // a hotkey field like the number fields: the keys, "Press keys…" while being set, red when another program holds them
        void KeyField(Graphics g, RectangleF r, string setting)
        {
            string id = "eveo:key:" + setting;
            bool cap = keyCapture == setting, hot = hotSpot == id;
            string state = null;
            if (DemoKeyState != null) DemoKeyState.TryGetValue(setting, out state);
            else if (fleet != null) state = fleet.KeyState(setting);
            bool bad = !cap && (state == "taken" || state == "not a key");
            using (var p = Round(r, 5))
            {
                using (var b = new SolidBrush(bad ? Color.FromArgb(46, AlarmRed) : cap ? Color.FromArgb(40, BodyTheme.Accent) : hot ? Color.FromArgb(26, BodyTheme.Ink) : Color.FromArgb(12, BodyTheme.Ink))) g.FillPath(b, p);
                using (var pen = new Pen(bad ? AlarmRed : cap ? BodyTheme.Accent : Color.FromArgb(50, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
            }
            var keys = KeysOf(setting);
            float x = r.Left + U(5);
            if (cap || keys.Count == 0)
            {
                var t = cap ? L.T("Press keys…") : L.T("none");
                var f = TextFont(t, U(7));
                using (var b = new SolidBrush(cap ? BodyTheme.Ink : Color.FromArgb(120, BodyTheme.Ink))) g.DrawString(t, f, b, x, r.Top + (r.Height - f.GetHeight(g)) / 2, StringFormat.GenericTypographic);
                if (cap && (SelfTesting || Potato || clock.Elapsed.TotalSeconds % 1.0 < 0.55))
                {
                    float tw = g.MeasureString(t, f, 1000, StringFormat.GenericTypographic).Width;
                    using (var b = new SolidBrush(BodyTheme.Accent)) g.FillRectangle(b, x + tw + U(1.5f), r.Top + U(3.5f), Math.Max(1f, U(0.7f)), r.Height - U(7));
                }
            }
            else
            {
                var t = string.Join(",  ", keys.Select(KeyLabel));
                var f = NornsFonts.Screen(U(6.5f));
                using (var b = new SolidBrush(BodyTheme.Ink)) g.DrawString(t, f, b, x, r.Top + (r.Height - f.GetHeight(g)) / 2, StringFormat.GenericTypographic);
            }
            if (bad)
            {
                var t = L.T("taken");
                var f = TextFont(t, U(6));
                float tw = g.MeasureString(t, f, 1000, StringFormat.GenericTypographic).Width;
                using (var b = new SolidBrush(AlarmRed)) g.DrawString(t, f, b, r.Right - U(5) - tw, r.Top + (r.Height - f.GetHeight(g)) / 2, StringFormat.GenericTypographic);
            }
            spots.Add(Tuple.Create(id, r));
        }
    }
}
