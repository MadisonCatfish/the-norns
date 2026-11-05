// Tray.cs - the body's icon in the notification area (roadmap item 20, owner 04.11.2026). There is no taskbar button: the
// minimize key hides the body to the icon, a click on the icon brings it back. The menu: Show / Hide, Settings,
// Statistics, Compact and Potato mode, Check for updates, Exit. A dot on the icon tells the state while the body is
// hidden: the accent while a log is followed, red with no EVE client, blue for a newer version; a titan or a dreadnought
// on grid makes the icon flash red / yellow. The tip: the fleet's DPS and the anomaly's ISK. Settings -> General: "Close
// to tray" (the close key hides instead of quitting) and "Start in the tray", both off by default.
// The logs are read all the time; while hidden the body is not drawn.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TheNorns
{
    partial class DpsForm
    {
        NotifyIcon tray;
        public static bool CloseToTray = AppSetting("close_to_tray") is bool && (bool)AppSetting("close_to_tray");
        public static bool StartInTray = AppSetting("start_in_tray") is bool && (bool)AppSetting("start_in_tray");
        ToolStripMenuItem trayShow, trayCompact, trayPotato;
        string trayIconKey, trayText;

        // the icon is there while the app runs, so a hidden body is always one click away
        void StartTray()
        {
            if (!Standalone || SelfTesting || tray != null) return;
            tray = new NotifyIcon { Icon = AppInfo.Icon, Text = AppInfo.Name, ContextMenuStrip = TrayMenu(), Visible = true };
            tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) ToggleTray(); };
            // start in the tray: the body is not shown at all (the logs are followed from the start all the same)
            if (StartInTray) base.Shown += (s, e) => BeginInvoke((Action)HideToTray);
        }

        ContextMenuStrip TrayMenu()
        {
            var menu = new ContextMenuStrip { ShowImageMargin = false, ShowCheckMargin = true, Renderer = new TrayMenuRenderer() };
            trayShow = new ToolStripMenuItem(L.T("Show"), null, (s, e) => ToggleTray());
            trayCompact = new ToolStripMenuItem(L.T("Compact mode"), null, (s, e) => { ShowFromTray(); Activate("compact"); });
            trayPotato = new ToolStripMenuItem(L.T("Potato mode"), null, (s, e) => { ShowFromTray(); Activate("potato"); });
            menu.Items.Add(trayShow);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(L.T("Settings"), null, (s, e) => OpenPageFromTray("general")));
            menu.Items.Add(new ToolStripMenuItem(L.T("Statistics"), null, (s, e) => OpenPageFromTray("stats")));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(trayCompact);
            menu.Items.Add(trayPotato);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(L.T("Check for updates"), null, (s, e) => { OpenPageFromTray("about"); CheckForUpdates(false); }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(L.T("Exit"), null, (s, e) => { ShowFromTray(); SavePos(); exiting = true; Close(); }));
            // the texts and checks as they are now (the language may have changed since)
            menu.Opening += (s, e) =>
            {
                trayShow.Text = Visible ? L.T("Hide") : L.T("Show");
                trayCompact.Text = L.T("Compact mode"); trayCompact.Checked = Compact;
                trayPotato.Text = L.T("Potato mode"); trayPotato.Checked = Potato;
                menu.Items[2].Text = L.T("Settings"); menu.Items[3].Text = L.T("Statistics");
                menu.Items[8].Text = L.T("Check for updates"); menu.Items[10].Text = L.T("Exit");
            };
            return menu;
        }

        bool exiting;   // the menu's Exit: the close goes through even with "Close to tray" on

        void ToggleTray() { if (Visible) HideToTray(); else ShowFromTray(); }

        void HideToTray()
        {
            if (tray == null) { WindowState = FormWindowState.Minimized; return; }   // no icon: the taskbar
            tip.Hide(this);
            Hide();
            UpdateTray();
        }

        void ShowFromTray()
        {
            if (!Visible) Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
            Render();
            UpdateTray();
        }

        // the settings (or Statistics, About) opened on the body, brought back if hidden
        void OpenPageFromTray(string p)
        {
            ShowFromTray();
            if (!settingsOpen) ToggleSettings();
            page = p; sessions = null; ShowTab(page);
            Render();
        }

        // once a second (the log poll) and when the body hides or shows: the icon's dot and the tip
        void UpdateTray()
        {
            if (tray == null) return;
            var now = DateTime.UtcNow;
            bool titan = (now - stats.LastTitan).TotalSeconds < 20, dread = (now - stats.LastDread).TotalSeconds < 20;
            string key;
            if (titan || dread) key = (now.Second % 2 == 0) ? (titan ? "alarm-red" : "alarm-yellow") : "plain";   // flashes
            else if (updateState == 3) key = "update";
            else if (clients == 0) key = "noclient";
            else if (tail.Following) key = "follow";
            else key = "plain";
            if (key != trayIconKey) { trayIconKey = key; tray.Icon = TrayIcon(key); }
            // the tip: at most 63 characters (NotifyIcon)
            var text = AppInfo.Name;
            if (tail.Following)
            {
                double dps = stats.Now(null, true, now);
                text += "\n" + Num(dps) + " DPS · " + FormatIsk(stats.Tracker.CurrentAnomalyIsk);
            }
            else if (clients == 0) text += "\n" + L.T("No EVE clients found");
            if (updateState == 3 && updateLatest != null) text += "\n" + L.F("Update to {0}", updateLatest);
            if (text.Length > 63) text = text.Substring(0, 63);
            if (text != trayText) { trayText = text; tray.Text = text; }
        }

        // the app's icon at the tray's size with a status dot at its lower right (made once per state)
        static readonly System.Collections.Generic.Dictionary<string, Icon> trayIcons = new System.Collections.Generic.Dictionary<string, Icon>();
        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);
        internal static Icon TrayIcon(string key)
        {
            Icon ic;
            if (trayIcons.TryGetValue(key, out ic)) return ic;
            int s = SystemInformation.SmallIconSize.Width;
            using (var bmp = new Bitmap(s, s, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    using (var app = new Icon(AppInfo.Icon, s, s)) g.DrawIcon(app, new Rectangle(0, 0, s, s));
                    Color? dot = key == "follow" ? BodyTheme.Accent : key == "noclient" ? AlarmRed : key == "update" ? BodyTheme.Info
                        : key == "alarm-red" ? AlarmRed : key == "alarm-yellow" ? BodyTheme.Dread : (Color?)null;
                    if (dot != null && !key.StartsWith("alarm"))
                    {
                        float d = s * 0.46f, x = s - d - 0.5f, y = s - d - 0.5f;
                        using (var b = new SolidBrush(Color.FromArgb(230, 10, 10, 12))) g.FillEllipse(b, x - 1.2f, y - 1.2f, d + 2.4f, d + 2.4f);
                        using (var b = new SolidBrush(dot.Value)) g.FillEllipse(b, x, y, d, d);
                    }
                }
                // the alarm: the icon itself tinted, pixel by pixel, keeping its own alpha (its round corners stay clear)
                if (key.StartsWith("alarm"))
                {
                    var tint = key == "alarm-red" ? AlarmRed : BodyTheme.Dread;
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            var px = bmp.GetPixel(x, y);
                            if (px.A == 0) continue;
                            bmp.SetPixel(x, y, Color.FromArgb(px.A, (px.R * 2 + tint.R * 3) / 5, (px.G * 2 + tint.G * 3) / 5, (px.B * 2 + tint.B * 3) / 5));
                        }
                }
                var h = bmp.GetHicon();
                ic = (Icon)Icon.FromHandle(h).Clone();
                DestroyIcon(h);
            }
            trayIcons[key] = ic;
            return ic;
        }

        void StopTray()
        {
            if (tray == null) return;
            tray.Visible = false;   // else the icon stays in the notification area until the mouse passes over it
            if (tray.ContextMenuStrip != null) tray.ContextMenuStrip.Dispose();
            tray.Dispose();
            tray = null;
        }

        // selftest: the menu as it opens, on a bitmap
        public Bitmap DemoTrayMenu()
        {
            var menu = TrayMenu();
            trayShow.Text = L.T("Hide"); trayCompact.Checked = Compact; trayPotato.Checked = Potato;
            menu.CreateControl();
            var size = menu.GetPreferredSize(Size.Empty);
            menu.Size = size;
            var bmp = new Bitmap(size.Width, size.Height);
            menu.DrawToBitmap(bmp, new Rectangle(Point.Empty, size));
            menu.Dispose();
            return bmp;
        }

        // the menu in the body's colours: dark, the text in the ink, the item under the mouse a little lighter, the checks in
        // the accent
        class TrayMenuRenderer : ToolStripProfessionalRenderer
        {
            static readonly Color Back = Color.FromArgb(28, 30, 34), Hot = Color.FromArgb(48, 51, 57), Line = Color.FromArgb(60, 64, 72);
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) { using (var b = new SolidBrush(Back)) e.Graphics.FillRectangle(b, e.AffectedBounds); }
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { using (var p = new Pen(Line)) e.Graphics.DrawRectangle(p, 0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1); }
            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.Selected) return;
                using (var b = new SolidBrush(Hot)) e.Graphics.FillRectangle(b, new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height));
            }
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) { e.TextColor = Color.FromArgb(225, 229, 236); base.OnRenderItemText(e); }
            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                var r = e.ImageRectangle; r.Inflate(1, 1);
                using (var b = new SolidBrush(BodyTheme.Accent)) MaterialIcon.Draw(e.Graphics, MaterialIcon.Check, r, b);
            }
            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) { using (var p = new Pen(Line)) e.Graphics.DrawLine(p, 4, e.Item.Height / 2, e.Item.Width - 4, e.Item.Height / 2); }
        }
    }
}
