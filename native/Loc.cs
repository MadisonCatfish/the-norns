// Loc.cs - the app's languages (player 25.10.2026): English, Ukrainian, Spanish, German, Italian, French.
// UI text goes through L.T("English text"); the translations are native\lang\<code>.json embedded in the exe
// (English text -> translation), a missing entry shows the English. Game terms stay as players say them with the
// English client the app works with (Siege, lock, overview, fleet warp, cap ...). The combat log stays English.
// Layout never changes with the language: controls measure the English text (L.En) and draw the translation trimmed
// with "…"; the full text is then a tooltip (L.SetTip). WinForms labels get the same through L.Fit.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TheNorns
{
    static class L
    {
        // code, name in its own language
        public static readonly string[][] Languages =
        {
            new[] { "en", "English" }, new[] { "uk", "Українська" }, new[] { "es", "Español" },
            new[] { "de", "Deutsch" }, new[] { "it", "Italiano" }, new[] { "fr", "Français" },
        };
        public static string Code = "en";
        static Dictionary<string, string> map = new Dictionary<string, string>();
        static Dictionary<string, string> back = new Dictionary<string, string>();

        public static bool Known(string code) { return Languages.Any(l => l[0] == code); }

        // the Windows display language when the app has it, else English (installer, first start)
        public static string SystemDefault()
        {
            var c = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return Known(c) ? c : "en";
        }

        // config.json "app": { "language": "de" }; not set -> the Windows language
        public static string FromConfig(Dictionary<string, object> raw)
        {
            object a, v;
            if (raw != null && raw.TryGetValue("app", out a) && a is Dictionary<string, object> && ((Dictionary<string, object>)a).TryGetValue("language", out v) && v != null && Known(v.ToString()))
                return v.ToString();
            return SystemDefault();
        }

        public static void Init(string code)
        {
            Code = Known(code) ? code : "en";
            map = Load(Code);
            back = new Dictionary<string, string>();
            foreach (var kv in map) if (!back.ContainsKey(kv.Value)) back[kv.Value] = kv.Key;
        }

        public static Dictionary<string, string> Load(string code)
        {
            var d = new Dictionary<string, string>();
            if (code == "en") return d;
            using (var s = typeof(L).Assembly.GetManifestResourceStream("TheNorns.lang." + code + ".json"))
            {
                if (s == null) return d;
                using (var r = new StreamReader(s, System.Text.Encoding.UTF8))
                {
                    var o = new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = int.MaxValue }.DeserializeObject(r.ReadToEnd()) as Dictionary<string, object>;
                    if (o != null) foreach (var kv in o) if (kv.Value is string && !kv.Key.StartsWith("_")) d[kv.Key] = (string)kv.Value;
                }
            }
            return d;
        }

        // the text in the current language
        public static string T(string en)
        {
            if (en == null) return null;
            string t; return map.TryGetValue(en, out t) && !string.IsNullOrEmpty(t) ? t : en;
        }
        // a formatted text; its English version is remembered too, so a layout can be measured with it
        public static string F(string en, params object[] args)
        {
            var t = string.Format(CultureInfo.InvariantCulture, T(en), args);
            if (Code != "en")
                lock (composed)
                {
                    if (composed.Count > 4000) composed.Clear();   // status lines change every second: keep it bounded
                    composed[t] = string.Format(CultureInfo.InvariantCulture, en, args);
                }
            return t;
        }
        static readonly Dictionary<string, string> composed = new Dictionary<string, string>();
        // the English text behind a translated one (what the layout is measured with)
        public static string En(string shown)
        {
            if (shown == null) return null;
            string e; if (back.TryGetValue(shown, out e)) return e;
            lock (composed) if (composed.TryGetValue(shown, out e)) return e;
            return shown;
        }

        // a row of buttons / switches laid out at their English widths: the space left up to `right` goes to the ones whose
        // translation is cut, each up to its full width (player 25.10.2026: "stretch them, there is room")
        public static void SpreadRow(IList<Control> row, int right, int gap)
        {
            if (Code == "en" || row.Count == 0) return;
            int spare = right - row[row.Count - 1].Right;
            List<int> need;
            using (var g = row[0].CreateGraphics()) using (var f = new Font("Segoe UI", Theme.FControl))
            {
                Func<Control, int> full = c =>
                {
                    var pb = c as PillButton; if (pb == null) return c.Width;
                    int pad = (int)((c is TogglePill ? 68 : 34) * Theme.K);
                    return (int)Math.Ceiling(g.MeasureString((pb.Icon != "" ? pb.Icon + "  " : "") + pb.Text, f).Width) + pad;
                };
                need = row.Select(c => Math.Max(0, full(c) - c.Width)).ToList();
            }
            int total = need.Sum();
            if (spare <= 0 || total == 0) return;
            int x = row[0].Left;
            for (int i = 0; i < row.Count; i++)
            {
                int add = (int)Math.Min(need[i], (long)spare * need[i] / total);
                row[i].SetBounds(x, row[i].Top, row[i].Width + add, row[i].Height);
                x = row[i].Right + gap;
            }
        }

        // ---- tooltip with the full text of a trimmed control ----
        // Shown by hand on hover (player 25.10.2026: WinForms' automatic tooltips often never came - they need the window
        // active and get reset by repaints): 0.45 s after the pointer rests on the control, until it leaves or clicks.
        static ToolTip tip;
        static readonly Dictionary<Control, string> tips = new Dictionary<Control, string>();
        static readonly HashSet<Control> hooked = new HashSet<Control>();
        static Timer hoverTimer; static Control hoverCtl; static string shown;
        public static void SetTip(Control c, string full)
        {
            string cur; tips.TryGetValue(c, out cur);
            if (cur == full) return;
            if (full == null) tips.Remove(c); else tips[c] = full;
            if (!hooked.Contains(c))
            {
                hooked.Add(c);
                c.MouseEnter += (s, e) => Hover(c);
                c.MouseMove += (s, e) => { if (hoverCtl != c) Hover(c); };
                c.MouseLeave += (s, e) => Unhover(c);
                c.MouseDown += (s, e) => Unhover(c);
                c.Disposed += (s, e) => { Unhover(c); tips.Remove(c); hooked.Remove(c); };
            }
            if (hoverCtl == c) { if (shown != null && shown != full) HideTip(); Hover(c); }   // text changed under the pointer
        }
        static void Hover(Control c)
        {
            hoverCtl = c;
            if (hoverTimer == null)
            {
                hoverTimer = new Timer { Interval = 450 };
                hoverTimer.Tick += (s, e) =>
                {
                    hoverTimer.Stop();
                    string t; var h = hoverCtl;
                    if (h == null || h.IsDisposed || !h.IsHandleCreated || !tips.TryGetValue(h, out t) || t == null || t == shown) return;
                    if (tip == null) tip = new ToolTip { ShowAlways = true, UseAnimation = false, UseFading = false };
                    var p = h.PointToClient(Cursor.Position);
                    tip.Show(t, h, p.X + 14, p.Y + 22, 15000);
                    shown = t;
                };
            }
            hoverTimer.Stop(); hoverTimer.Start();
        }
        static void Unhover(Control c)
        {
            if (hoverCtl != c) return;
            hoverCtl = null; if (hoverTimer != null) hoverTimer.Stop();
            if (tip != null && !c.IsDisposed) try { tip.Hide(c); } catch { }
            shown = null;
        }
        static void HideTip() { if (tip != null && hoverCtl != null) try { tip.Hide(hoverCtl); } catch { } shown = null; }

        // a string drawn into a rectangle: trimmed with "…" when it doesn't fit; true = it was trimmed
        public static bool Trimmed(Graphics g, string text, Font f, float width)
        {
            return !string.IsNullOrEmpty(text) && g.MeasureString(text, f).Width > width + 0.5f;
        }

        // every open window gets Fit while the app is idle (pages are rebuilt as the user goes through steps), at most 4x/s
        static DateTime lastFit;
        public static void AutoFit()
        {
            if (Code == "en") return;
            Application.Idle += (s, e) =>
            {
                if ((DateTime.Now - lastFit).TotalMilliseconds < 250) return;
                lastFit = DateTime.Now;
                foreach (Form f in Application.OpenForms.Cast<Form>().ToList()) Fit(f);
            };
        }

        // WinForms labels: a translated label is never wider than its English text - longer text is cut with "…" and shown
        // whole as a tooltip (Label.AutoEllipsis does both). Call once a form or page is built.
        static readonly HashSet<Label> fitted = new HashSet<Label>();   // auto-size labels Fit holds at the English width
        public static void Fit(Control root)
        {
            if (Code == "en" || root == null) return;
            foreach (Control c in root.Controls)
            {
                var lab = c as Label;
                if (lab != null && !(lab is LinkLabel) && !string.IsNullOrEmpty(lab.Text))
                {
                    bool mine = fitted.Contains(lab);
                    if ((lab.AutoSize && lab.MaximumSize.Width == 0) || mine)
                    {
                        var en = En(lab.Text);
                        if (en == lab.Text) { if (mine) { fitted.Remove(lab); lab.AutoSize = true; } }   // not a translation (any more)
                        else
                        {
                            var ew = TextRenderer.MeasureText(en, lab.Font).Width + lab.Padding.Horizontal + 2;
                            if (mine || lab.PreferredWidth > ew)
                            {
                                if (!mine) { fitted.Add(lab); lab.Disposed += (s, e) => fitted.Remove(lab); }
                                lab.AutoSize = false; lab.AutoEllipsis = true;
                                var sz = new Size(ew, TextRenderer.MeasureText(en, lab.Font).Height + lab.Padding.Vertical);
                                if (lab.Size != sz) lab.Size = sz;
                            }
                        }
                    }
                    else if (!lab.AutoEllipsis) lab.AutoEllipsis = true;
                    // cut text -> our hover tooltip (the label's own one is unreliable)
                    if (lab.AutoEllipsis)
                    {
                        bool cut = lab.Text.IndexOf('\n') >= 0 || lab.Height > TextRenderer.MeasureText("Ag", lab.Font).Height * 1.6
                            ? lab.GetPreferredSize(new Size(Math.Max(1, lab.Width), 0)).Height > lab.Height + 1
                            : TextRenderer.MeasureText(lab.Text, lab.Font).Width > lab.Width - lab.Padding.Horizontal;
                        SetTip(lab, cut ? lab.Text : null);
                    }
                }
                if (c.HasChildren) Fit(c);
            }
        }
    }
}
