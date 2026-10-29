// The Norns - Settings -> Modes: Standard / Competition (owner, 29.10.2026).
// Competition: several packs in one system; the fleet anchor is a fast frigate (the runner) that deals no DPS - the
// Thunderchilds' Vorton projectors hit it and the lightning arcs on to the NPCs. The Smartbombs module becomes
// Lightning rod. Its formula needs the runner's resists and repair, and the Vorton's damage caused per shot (EM and
// Kinetic, as the game's tooltip shows it); they are entered here and kept in config.json "app".
using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace TheNorns
{
    partial class DpsForm
    {
        public static bool Competition = Convert.ToString(AppSetting("mode")) == "competition";
        // EM, Thermal, Kinetic, Explosive in %
        public static double[] RunnerResists = ReadNumbers("runner_resists", 4);
        // hit points the runner repairs per second
        public static double RunnerRepair = ReadNumbers("runner_repair", 1)[0];
        // one Vorton shot: EM and Kinetic damage caused
        public static double[] VortonShot = ReadNumbers("vorton_shot", 2);

        static double[] ReadNumbers(string key, int n)
        {
            var r = new double[n];
            object v = AppSetting(key);
            var list = v as IEnumerable;
            if (v is string || list == null) list = v == null ? new object[0] : new[] { v };
            int i = 0;
            foreach (var x in list)
            {
                if (i >= n) break;
                double d; if (double.TryParse(Convert.ToString(x, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out d)) r[i] = d;
                i++;
            }
            return r;
        }

        string focusField;   // the number field being typed in: "res0".."res3", "repair", "shot0", "shot1"
        string editText;     // what was typed in it; null = nothing yet (the first key replaces the number)
        // a key over the field's limit: the field turns red and shakes once (owner, 29.10.2026)
        string errField; double errAt = -10;
        public void DemoFieldError(string id) { errField = id; errAt = clock.Elapsed.TotalSeconds - 0.06; }
        // the "?" keys: what a field means, in the formula window, shorter than the main screen's (owner, 29.10.2026)
        // the "?" key after Runner: one card with the formula and what every field means, by sections; it scrolls
        // (owner, 29.10.2026)
        static readonly FormulaText HelpRunner = new FormulaText { Title = "Lightning rod · formula", Scroll = true, Body =
            "The runner is the fleet's lightning rod: the Thunderchilds shoot it, and every Vorton shot arcs on to the NPCs with its full damage." + "\n" +
            "Damage through the rod: the Thunderchilds' Vorton hits on the runner in the last 10 seconds × the damage of one shot (EM + Kinetic, below) ÷ 10." + "\n" +
            "The runner itself takes far less: a shot × (1 − its resists), cut down again by its small signature. The NPCs' damage on it is added (the thin white line)." + "\n" +
            "Tank: what the runner takes per second against its repair. Above the repair it is losing hit points." + "\n" +
            "# Resists\n" + "The runner's resists in %, as its ship info shows them: EM, Thermal, Kinetic, Explosive; whole numbers up to 99." + "\n" + "Vorton damage is EM and Kinetic, so these two matter most; all four count against the NPCs' damage." + "\n" +
            "# Repair\n" + "The hit points the runner repairs per second (shield or armor), as its fitting shows it: up to 300, one decimal." + "\n" + "Under the Lightning rod number: what the runner takes per second against this repair; red means it is losing hit points." + "\n" +
            "# " + "Vorton · one shot" + "\n" + "The damage of one Vorton shot of one Thunderchild, as the gun's tooltip shows it under Damage caused: EM and Kinetic apart, whole numbers up to 2500." + "\n" + "Ships with other skills or implants hit differently: enter their average." };

        static readonly string[] FieldOrder = { "res0", "res1", "res2", "res3", "repair", "shot0", "shot1" };

        // a click on a mode or a field (Activate); false = not ours
        bool ActivateMode(string id)
        {
            if (focusField != null && id != "field:" + focusField) CommitField();
            if (id.StartsWith("mode:"))
            {
                Competition = id == "mode:competition";
                SaveApp("mode", Competition ? "competition" : "standard");
                return true;
            }
            if (id.StartsWith("field:")) { focusField = id.Substring(6); editText = null; return true; }
            if (id.StartsWith("help:"))
            {
                FormulaWindow.Open(this, HelpRunner);
                return true;
            }
            return false;
        }

        double FieldValue(string id)
        {
            if (id.StartsWith("res")) return RunnerResists[id[3] - '0'];
            if (id == "repair") return RunnerRepair;
            return VortonShot[id[4] - '0'];
        }

        // the limits (owner, 29.10.2026): resists whole % up to 99, repair up to 300 hp/s with a decimal point, a Vorton
        // shot up to 2500 HP whole; only digits (and the repair's point) can be typed
        static double FieldMax(string id) { return id.StartsWith("res") ? 99 : id == "repair" ? 300 : 2500; }
        static bool FieldDecimal(string id) { return id == "repair"; }

        // the typed number into its place and into config.json
        void CommitField()
        {
            var id = focusField; var text = editText;
            focusField = null; editText = null;
            if (id == null || string.IsNullOrEmpty(text)) return;
            double v;
            if (!double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return;
            v = Math.Round(Math.Max(0, Math.Min(FieldMax(id), v)), FieldDecimal(id) ? 1 : 0);
            if (id.StartsWith("res")) { RunnerResists[id[3] - '0'] = v; SaveApp("runner_resists", RunnerResists); }
            else if (id == "repair") { RunnerRepair = v; SaveApp("runner_repair", v); }
            else { VortonShot[id[4] - '0'] = v; SaveApp("vorton_shot", VortonShot); }
        }

        // typing in a field: digits and one decimal point; Backspace; Enter / Tab = keep it and go to the next field
        // (Shift+Tab back); Escape = leave it as it was
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (settingsOpen && focusField != null)
            {
                var key = keyData & Keys.KeyCode;
                if (key == Keys.Tab || key == Keys.Enter)
                {
                    int i = Array.IndexOf(FieldOrder, focusField);
                    CommitField();
                    if (key == Keys.Tab) { focusField = FieldOrder[(i + ((keyData & Keys.Shift) != 0 ? FieldOrder.Length - 1 : 1)) % FieldOrder.Length]; editText = null; }
                    Render(); return true;
                }
                if (key == Keys.Escape) { focusField = null; editText = null; Render(); return true; }
                if (key == Keys.Back) { editText = string.IsNullOrEmpty(editText) ? "" : editText.Substring(0, editText.Length - 1); Render(); return true; }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (!settingsOpen || focusField == null) return;
            e.Handled = true;   // nothing else reaches the form while a field is typed in
            char ch = e.KeyChar == ',' ? '.' : e.KeyChar;
            bool dec = FieldDecimal(focusField);
            if (!(ch >= '0' && ch <= '9') && !(dec && ch == '.')) return;
            var t = editText ?? "";
            if (ch == '.' && (t.Contains('.') || t.Length == 0)) return;
            if (dec && t.Contains('.') && t.Length - t.IndexOf('.') > 1) return;   // one digit after the point
            var next = t + ch;
            double v;
            if (ch != '.' && (!double.TryParse(next, NumberStyles.Float, CultureInfo.InvariantCulture, out v) || v > FieldMax(focusField)))
            {
                errField = focusField; errAt = clock.Elapsed.TotalSeconds; Render();   // over the limit: red, one shake
                return;
            }
            editText = next;
            Render();
        }

        void PaintModes(Graphics g, RectangleF c, float y)
        {
            // the mode: two lamps, as the languages
            var mt = L.T("Mode");
            Glow(g, mt, TitleFont(mt, U(6.5f)), Muted, c.Left, y);
            y += U(11);
            var modes = new[] { new[] { "standard", "Standard" }, new[] { "competition", "Competition" } };
            float colW = c.Width / 3;
            for (int i = 0; i < modes.Length; i++)
            {
                string id = "mode:" + modes[i][0], name = L.T(modes[i][1]);
                bool cur = (modes[i][0] == "competition") == Competition, hot = hotSpot == id;
                var row = new RectangleF(c.Left + i * colW, y, colW - U(4), U(14));
                if (hot) using (var p = Round(row, 6)) using (var b = new SolidBrush(Color.FromArgb(22, BodyTheme.Ink))) g.FillPath(b, p);
                float d = U(5.5f), lx = row.Left + U(5), ly = row.Top + row.Height / 2;
                if (cur) SoftGlow(g, new PointF(lx + d / 2, ly), d / 2 + U(4), BodyTheme.Accent, 140);
                using (var b = new SolidBrush(cur ? BodyTheme.Accent : Color.FromArgb(46, 48, 30))) g.FillEllipse(b, lx, ly - d / 2, d, d);
                var nf = TextFont(name, U(8));
                using (var b = new SolidBrush(cur || hot ? BodyTheme.Ink : Color.FromArgb(165, 168, 176)))
                    g.DrawString(name, nf, b, lx + d + U(6), ly - nf.GetHeight(g) / 2, StringFormat.GenericTypographic);
                spots.Add(Tuple.Create(id, row));
            }
            y += U(18);
            var about = Competition
                ? L.T("Anchor — a frigate runner. DPS — Thunderchilds with Vorton projectors.")
                : L.T("Anchor — a smartbombing battleship. DPS — Thunderchilds with Vorton projectors.");
            var af = TextFont(about, U(7.8f));   // 20 % larger; only the roles (owner, 29.10.2026)
            var ah = g.MeasureString(about, af, (int)c.Width).Height;
            using (var b = new SolidBrush(Color.FromArgb(150, BodyTheme.Ink))) g.DrawString(about, af, b, new RectangleF(c.Left, y, c.Width, ah + U(2)));
            y += ah + U(9);
            if (!Competition) return;

            // the runner's window: a dark box on the screen, its numbers in key-like fields
            var box = RectangleF.FromLTRB(c.Left, y, c.Right, Math.Min(c.Bottom - U(10), y + U(100)));
            using (var p = Round(box, 6)) using (var b = new SolidBrush(Color.FromArgb(10, BodyTheme.Ink))) g.FillPath(b, p);
            using (var p = Round(box, 6)) using (var pen = new Pen(Color.FromArgb(28, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
            var inner = RectangleF.Inflate(box, -U(7), -U(6));
            float yy = inner.Top;
            var rt = L.T("Runner");
            var rtf = TitleFont(rt, U(6.5f));
            Glow(g, rt, rtf, BodyTheme.Ink, inner.Left, yy);
            HelpKey(g, inner.Left + g.MeasureString(rt.ToUpperInvariant(), rtf, 1000, StringFormat.GenericTypographic).Width + U(5), yy + (rtf.GetHeight(g) - U(11)) / 2, "help:runner");
            yy += U(13);
            float labelW = U(58), gap = U(4), fh = U(15);
            float fx = inner.Left + labelW, fw4 = (inner.Right - fx - 3 * gap) / 4;
            RowLabel(g, L.T("Resists"), inner.Left, yy, fh);
            var types = new[] { Dmg.EM, Dmg.Thermal, Dmg.Kinetic, Dmg.Explosive };
            for (int k = 0; k < 4; k++)
                NumberField(g, new RectangleF(fx + k * (fw4 + gap), yy, fw4, fh), "res" + k, RunnerResists[k], "%", DamageIcon(types[k]), DamageColor(types[k]));
            yy += fh + U(6);
            RowLabel(g, L.T("Repair"), inner.Left, yy, fh);
            NumberField(g, new RectangleF(fx, yy, 2 * fw4 + gap, fh), "repair", RunnerRepair, "hp/s", null, Color.Empty);
            yy += fh + U(10);
            var vt = L.T("Vorton · damage caused per ship (average)");
            Glow(g, vt, TitleFont(vt, U(6.5f)), BodyTheme.Ink, inner.Left, yy);
            yy += U(13);
            RowLabel(g, L.T("One shot"), inner.Left, yy, fh);
            NumberField(g, new RectangleF(fx, yy, 2 * fw4 + gap, fh), "shot0", VortonShot[0], "HP", DamageIcon(Dmg.EM), DamageColor(Dmg.EM));
            NumberField(g, new RectangleF(fx + 2 * (fw4 + gap), yy, 2 * fw4 + gap, fh), "shot1", VortonShot[1], "HP", DamageIcon(Dmg.Kinetic), DamageColor(Dmg.Kinetic));
        }

        void RowLabel(Graphics g, string text, float x, float y, float h)
        {
            var f = TextFont(text, U(7));
            using (var b = new SolidBrush(Color.FromArgb(200, 203, 210))) g.DrawString(text, f, b, x, y + (h - f.GetHeight(g)) / 2, StringFormat.GenericTypographic);
        }

        // the "?" key: drawn like the function badge of the module titles (Material question_mark)
        void HelpKey(Graphics g, float x, float y, string id)
        {
            var r = new RectangleF(x, y, U(11), U(11));
            bool hover = hotSpot == id;
            using (var p = Round(r, 3.5f))
            {
                if (hover) using (var b = new SolidBrush(BodyTheme.BadgeFill)) g.FillPath(b, p);
                using (var pen = new Pen(Color.FromArgb(hover ? 160 : 89, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
            }
            float ic = U(7.5f);
            using (var b = new SolidBrush(hover ? BodyTheme.Ink : Muted)) MaterialIcon.Draw(g, MaterialIcon.QuestionMark, new RectangleF(r.Left + (r.Width - ic) / 2, r.Top + (r.Height - ic) / 2, ic, ic), b);
            spots.Add(Tuple.Create(id, RectangleF.Inflate(r, U(2), U(2))));
        }

        // a number field like the size keys: icon (the damage type's, in its colour), the number, its unit; the one being
        // typed in has the accent rim
        void NumberField(Graphics g, RectangleF r, string id, double value, string unit, string icon, Color iconColor)
        {
            bool focus = focusField == id, hot = hotSpot == "field:" + id;
            // the error: red for 0.9 s, one shake in the first 0.35 s
            double et = errField == id ? clock.Elapsed.TotalSeconds - errAt : 10;
            bool err = et < 0.9;
            if (err && et < 0.35 && !Potato) r.Offset((float)(U(2.2f) * Math.Sin(et * 2 * Math.PI * 7) * Math.Exp(-et * 8)), 0);
            using (var p = Round(r, 5))
            {
                using (var b = new SolidBrush(err ? Color.FromArgb(46, AlarmRed) : focus ? Color.FromArgb(40, BodyTheme.Accent) : hot ? Color.FromArgb(26, BodyTheme.Ink) : Color.FromArgb(12, BodyTheme.Ink))) g.FillPath(b, p);
                using (var pen = new Pen(err ? AlarmRed : focus ? BodyTheme.Accent : Color.FromArgb(50, BodyTheme.Ink), 1f)) g.DrawPath(pen, p);
            }
            float x = r.Left + U(4);
            if (icon != null)
            {
                float ic = U(8);
                using (var b = new SolidBrush(iconColor)) MaterialIcon.Draw(g, icon, new RectangleF(x, r.Top + (r.Height - ic) / 2, ic, ic), b);
                x += ic + U(3);
            }
            var vf = NornsFonts.Screen(U(6.5f));
            var txt = focus && editText != null ? editText : value.ToString(value % 1 == 0 ? "0" : "0.0", CultureInfo.InvariantCulture);
            var uf = NornsFonts.Body(U(5.5f));
            float uw = g.MeasureString(unit, uf, 1000, StringFormat.GenericTypographic).Width;
            using (var b = new SolidBrush(BodyTheme.Ink)) g.DrawString(txt, vf, b, x, r.Top + (r.Height - vf.GetHeight(g)) / 2, StringFormat.GenericTypographic);
            // the caret after the number while typing, blinking (still in self-tests and Potato)
            if (focus && (SelfTesting || Potato || clock.Elapsed.TotalSeconds % 1.0 < 0.55))
            {
                float tw = txt.Length == 0 ? 0 : g.MeasureString(txt, vf, 1000, StringFormat.GenericTypographic).Width;
                using (var b = new SolidBrush(BodyTheme.Accent)) g.FillRectangle(b, x + tw + U(1), r.Top + U(3.5f), Math.Max(1f, U(0.7f)), r.Height - U(7));
            }
            using (var b = new SolidBrush(Color.FromArgb(120, BodyTheme.Ink))) g.DrawString(unit, uf, b, r.Right - U(4) - uw, r.Top + (r.Height - uf.GetHeight(g)) / 2 + U(0.5f), StringFormat.GenericTypographic);
            spots.Add(Tuple.Create("field:" + id, r));
        }
    }
}
