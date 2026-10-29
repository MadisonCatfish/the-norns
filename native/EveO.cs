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
        // the live client on the power display (owner, 29.10.2026: it can be turned off - the animation stays); on by default
        public static bool FleetPreview = !(AppSetting("fleet_preview") is bool) || (bool)AppSetting("fleet_preview");
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
            if (id == "eveo:minimize")
            {
                Fleet.MinimizeOthers = !Fleet.MinimizeOthers; SaveApp("fleet_minimize", Fleet.MinimizeOthers);
                if (!Fleet.MinimizeOthers) ClientSwitch.TransitionsBack();   // the clients animate as before
                return true;
            }
            if (id.StartsWith("eveo:pin:"))
            {
                var n = id.Substring(9);
                if (!Fleet.KeepOpen.Remove(n)) Fleet.KeepOpen.Add(n);
                SaveApp("fleet_keep_open", Fleet.KeepOpen.ToArray());
                return true;
            }
            if (id == "eveo:preview") { FleetPreview = !FleetPreview; SaveApp("fleet_preview", FleetPreview); return true; }   // the next frame hides or shows it
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

        // the page scrolls with the wheel (owner, 29.10.2026: the clients' table makes it long); a thin bar on the right
        float eveoScroll;
        RectangleF eveoView;
        void PaintEveO(Graphics g, RectangleF c, float top)
        {
            eveoView = RectangleF.FromLTRB(c.Left, top - U(2), c.Right, c.Bottom - U(10));
            var st = g.Save();
            g.SetClip(eveoView);
            int firstSpot = spots.Count;
            var inner = RectangleF.FromLTRB(c.Left, c.Top, c.Right - U(7), c.Bottom);
            float end = PaintEveOContent(g, inner, top - eveoScroll);
            g.Restore(st);
            // the keys scrolled out of view can't be clicked
            for (int i = spots.Count - 1; i >= firstSpot; i--)
            {
                var r = RectangleF.Intersect(spots[i].Item2, eveoView);
                if (r.Width <= 0 || r.Height <= 0) spots.RemoveAt(i); else spots[i] = Tuple.Create(spots[i].Item1, r);
            }
            float max = Math.Max(0, end + eveoScroll - eveoView.Bottom + U(4));
            if (eveoScroll > max) { eveoScroll = max; Invalidate(); }
            if (max > 0)
            {
                // the bar: its length is the part shown, its place where that part is (as on the scrolling formula cards)
                float trackH = eveoView.Height, barH = Math.Max(U(12), trackH * trackH / (trackH + max)), barY = eveoView.Top + (trackH - barH) * eveoScroll / max;
                using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillRectangle(b, c.Right - U(2), eveoView.Top, U(1.5f), trackH);
                using (var p = Theme.Round(new RectangleF(c.Right - U(2.5f), barY, U(2.5f), barH), U(1.25f), 1f))
                using (var b = new SolidBrush(Color.FromArgb(150, BodyTheme.Ink))) g.FillPath(b, p);
            }
        }

        // the wheel over the page (Settings.cs OnMouseWheel); false = not over it
        bool ScrollEveO(Point p, int delta)
        {
            if (!settingsOpen || page != "eveo" || !eveoView.Contains(p)) return false;
            eveoScroll = Math.Max(0, eveoScroll - Math.Sign(delta) * U(24));   // the next frame clamps it
            return true;
        }

        // the page's content from y on; returns where it ends
        float PaintEveOContent(Graphics g, RectangleF c, float y)
        {
            // on / off, and the live client on the power display: lamp rows as on General
            LampRow(g, c, y, "eveo:on", FleetOn, L.T("Client switching"));
            y += U(19) - 3;
            LampRow(g, c, y, "eveo:preview", FleetPreview, L.T("Live client on the power display instead of the animation"));
            y += U(19) - 3;
            LampRow(g, c, y, "eveo:minimize", Fleet.MinimizeOthers, L.T("Minimize the previous client on switching"));
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
            y += hh + U(12);
            return PaintClientTable(g, c, y);
        }

        // ---- the clients' table: the switching order, changed by dragging a row (owner, 29.10.2026) ----
        public List<string> DemoClients;      // selftest: the order shown
        public HashSet<string> DemoOpen;      // selftest: which of them are open
        List<string> openNames = new List<string>();   // the open characters at the last poll while the module is off
        int dragRow = -1; float dragY0, dragDy, rowH;   // the row being dragged, where the drag began, how far it went

        List<string> ClientOrder()
        {
            if (DemoClients != null) return DemoClients;
            if (fleet != null) return fleet.Clients.Order.ToList();
            var order = Fleet.Configured("fleet_order").Where(n => n.Trim() != "").ToList();
            foreach (var n in openNames) if (!order.Contains(n, StringComparer.OrdinalIgnoreCase)) order.Add(n);
            return order;
        }

        bool ClientOpen(string name)
        {
            if (DemoOpen != null) return DemoOpen.Contains(name);
            if (fleet != null) return fleet.Clients.All.Any(w => !w.Login && string.Equals(w.Character, name, StringComparison.OrdinalIgnoreCase));
            return openNames.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        // where the dragged row would land
        int DragTarget(int count) { return Math.Max(0, Math.Min(count - 1, dragRow + (int)Math.Round(dragDy / rowH))); }

        float PaintClientTable(Graphics g, RectangleF c, float y)
        {
            var order = ClientOrder();
            var ct = L.T("Clients");
            var ctf = TitleFont(ct, U(6.5f));
            Glow(g, ct, ctf, Muted, c.Left, y);
            if (order.Count > 0)
            {
                var n = order.Count.ToString();
                var nf = NornsFonts.Screen(U(6));
                using (var b = new SolidBrush(BodyTheme.Accent)) g.DrawString(n, nf, b, c.Left + g.MeasureString(ct.ToUpperInvariant(), ctf, 1000, StringFormat.GenericTypographic).Width + U(5), y + U(0.5f), StringFormat.GenericTypographic);
            }
            y += U(12);
            rowH = U(16);
            var box = new RectangleF(c.Left, y, c.Width, Math.Max(1, order.Count) * rowH + U(6));
            using (var p = Round(box, 6)) using (var b = new SolidBrush(Color.FromArgb(10, BodyTheme.Ink))) g.FillPath(b, p);
            using (var p = Round(box, 6)) using (var pen = new Pen(Color.FromArgb(28, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
            float y0 = box.Top + U(3);
            if (order.Count == 0)
            {
                var t = L.T("No EVE clients found");
                var f = TextFont(t, U(7));
                using (var b = new SolidBrush(Color.FromArgb(120, BodyTheme.Ink))) g.DrawString(t, f, b, box.Left + U(7), y0 + (rowH - f.GetHeight(g)) / 2, StringFormat.GenericTypographic);
                return box.Bottom + U(6);
            }
            if (dragRow >= order.Count) dragRow = -1;
            int target = dragRow >= 0 ? DragTarget(order.Count) : -1;
            for (int i = 0; i < order.Count; i++)
            {
                if (i == dragRow) continue;
                int slot = i;   // where the others stand while a row is dragged: they make room at the target
                if (dragRow >= 0 && dragRow < i && i <= target) slot = i - 1;
                else if (dragRow >= 0 && target <= i && i < dragRow) slot = i + 1;
                ClientRow(g, new RectangleF(box.Left + U(3), y0 + slot * rowH, box.Width - U(6), rowH), order[i], slot, false);
            }
            if (dragRow >= 0)
            {
                // the dragged row: lifted, with the accent rim, under the mouse within the table
                float ry = Math.Max(y0, Math.Min(y0 + (order.Count - 1) * rowH, y0 + dragRow * rowH + dragDy));
                ClientRow(g, new RectangleF(box.Left + U(3), ry, box.Width - U(6), rowH), order[dragRow], target, true);
            }
            y = box.Bottom + U(5);
            var hint = L.T("Drag a row to change the switching order. The pin keeps a client open when the others are minimized.");
            var hf = TextFont(hint, U(6.6f));
            var hh = g.MeasureString(hint, hf, (int)c.Width).Height;
            using (var b = new SolidBrush(Color.FromArgb(150, BodyTheme.Ink))) g.DrawString(hint, hf, b, new RectangleF(c.Left, y, c.Width, hh + U(2)));
            return y + hh + U(4);
        }

        // a row: the drag handle (Material drag_indicator), the place in the order, the character, open or offline
        void ClientRow(Graphics g, RectangleF r, string name, int place, bool lifted)
        {
            string id = "eveo:row:" + name;
            bool hot = hotSpot == id && dragRow < 0, open = ClientOpen(name);
            var rr = RectangleF.Inflate(r, 0, -U(1));
            if (lifted)
            {
                var sh = rr; sh.Offset(0, U(2));
                using (var p = Round(sh, 5)) using (var b = new SolidBrush(Color.FromArgb(120, 0, 0, 0))) g.FillPath(b, p);
                using (var p = Round(rr, 5)) using (var b = new SolidBrush(Color.FromArgb(255, 22, 24, 18))) g.FillPath(b, p);
                using (var p = Round(rr, 5)) using (var pen = new Pen(BodyTheme.Accent, 1f)) g.DrawPath(pen, p);
            }
            else if (hot) using (var p = Round(rr, 5)) using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillPath(b, p);
            float x = rr.Left + U(3), cy = rr.Top + rr.Height / 2, ic = U(8);
            using (var b = new SolidBrush(Color.FromArgb(lifted || hot ? 200 : 90, BodyTheme.Ink))) MaterialIcon.Draw(g, MaterialIcon.DragIndicator, new RectangleF(x, cy - ic / 2, ic, ic), b);
            x += ic + U(4);
            var nf = NornsFonts.Screen(U(6));
            var num = (place + 1).ToString();
            using (var b = new SolidBrush(open ? BodyTheme.Accent : Color.FromArgb(90, BodyTheme.Ink))) g.DrawString(num, nf, b, x, cy - nf.GetHeight(g) / 2, StringFormat.GenericTypographic);
            x += U(10);
            var f = TextFont(name, U(7.2f));
            using (var b = new SolidBrush(open ? BodyTheme.Ink : Color.FromArgb(110, BodyTheme.Ink)))
                g.DrawString(name, f, b, new RectangleF(x, cy - f.GetHeight(g) / 2, rr.Right - x - U(48), f.GetHeight(g) + U(1)), new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            // the pin (Material push_pin): kept open when the others are minimized; lit when pinned
            bool pinned = Fleet.KeepOpen.Contains(name), pinHot = hotSpot == "eveo:pin:" + name;
            var pr = new RectangleF(rr.Right - U(44), cy - U(6), U(12), U(12));
            float pi = U(8);
            using (var b = new SolidBrush(pinned ? BodyTheme.Accent : Color.FromArgb(pinHot ? 200 : 70, BodyTheme.Ink)))
                MaterialIcon.Draw(g, MaterialIcon.PushPin, new RectangleF(pr.Left + (pr.Width - pi) / 2, pr.Top + (pr.Height - pi) / 2, pi, pi), b);
            if (!lifted) spots.Add(Tuple.Create("eveo:pin:" + name, pr));
            // open: a lit lamp; offline: the word
            if (open)
            {
                float d = U(4.5f), lx = rr.Right - U(6) - d;
                SoftGlow(g, new PointF(lx + d / 2, cy), d / 2 + U(3), BodyTheme.Accent, 110);
                using (var b = new SolidBrush(BodyTheme.Accent)) g.FillEllipse(b, lx, cy - d / 2, d, d);
            }
            else
            {
                var t = L.T("offline");
                var tf = TextFont(t, U(6));
                float tw = g.MeasureString(t, tf, 1000, StringFormat.GenericTypographic).Width;
                using (var b = new SolidBrush(Color.FromArgb(100, BodyTheme.Ink))) g.DrawString(t, tf, b, rr.Right - U(5) - tw, cy - tf.GetHeight(g) / 2, StringFormat.GenericTypographic);
            }
            if (!lifted) spots.Add(Tuple.Create(id, r));
        }

        // the mouse on a row (Body.cs): a drag begins at once; true = taken
        bool BeginRowDrag(string spot, int y)
        {
            if (spot == null || !spot.StartsWith("eveo:row:")) return false;
            var order = ClientOrder();
            int i = order.FindIndex(n => string.Equals(n, spot.Substring(9), StringComparison.OrdinalIgnoreCase));
            if (i < 0) return false;
            dragRow = i; dragY0 = y; dragDy = 0;
            return true;
        }

        bool MoveRowDrag(int y) { if (dragRow < 0) return false; dragDy = y - dragY0; Render(); return true; }

        // the row let go: the order with it at its new place, kept in config.json "fleet_order" and taken by the tracker
        bool EndRowDrag()
        {
            if (dragRow < 0) return false;
            var order = ClientOrder().ToList();
            int from = dragRow, to = DragTarget(order.Count);
            dragRow = -1;
            if (to != from)
            {
                var n = order[from]; order.RemoveAt(from); order.Insert(to, n);
                if (DemoClients != null) DemoClients = order;
                else
                {
                    SaveApp("fleet_order", order.ToArray());
                    if (fleet != null) fleet.Clients.SetOrder(order);
                }
            }
            return true;
        }

        void LampRow(Graphics g, RectangleF c, float y, string id, bool on, string text)
        {
            var r = new RectangleF(c.Left, y, c.Width, U(15));
            bool hot = hotSpot == id;
            if (hot) using (var p = Round(r, 6)) using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillPath(b, p);
            float dd = U(5.5f), lx = r.Left + U(5), ly = r.Top + r.Height / 2;
            if (on) SoftGlow(g, new PointF(lx + dd / 2, ly), dd / 2 + U(4), BodyTheme.Accent, 140);
            using (var b = new SolidBrush(on ? BodyTheme.Accent : Color.FromArgb(46, 48, 30))) g.FillEllipse(b, lx, ly - dd / 2, dd, dd);
            var f = TextFont(text, U(7.5f));
            using (var b = new SolidBrush(hot ? BodyTheme.Ink : Color.FromArgb(200, 203, 210))) g.DrawString(text, f, b, lx + dd + U(6), ly - f.GetHeight(g) / 2, StringFormat.GenericTypographic);
            spots.Add(Tuple.Create(id, r));
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
