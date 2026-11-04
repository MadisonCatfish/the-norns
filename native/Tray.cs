// Tray.cs - the body's icon in the notification area (roadmap item 20, owner 04.11.2026): the minimize key hides the
// body to it, a click on the icon brings it back; the menu: Show / Hide, Exit. The logs are read all the time.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TheNorns
{
    partial class DpsForm
    {
        NotifyIcon tray;

        // the icon is there while the app runs, so a hidden body is always one click away
        void StartTray()
        {
            if (!Standalone || SelfTesting || tray != null) return;
            var menu = new ContextMenuStrip { ShowImageMargin = false, Renderer = new TrayMenuRenderer() };
            // the first item names what it does now: Show when the body is hidden, Hide when it is out
            menu.Items.Add(new ToolStripMenuItem(L.T("Show"), null, (s, e) => ToggleTray()));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(L.T("Exit"), null, (s, e) => { ShowFromTray(); SavePos(); Close(); }));
            menu.Opening += (s, e) => menu.Items[0].Text = Visible ? L.T("Hide") : L.T("Show");
            tray = new NotifyIcon { Icon = AppInfo.Icon, Text = AppInfo.Name, ContextMenuStrip = menu, Visible = true };
            tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) ToggleTray(); };
        }

        void ToggleTray() { if (Visible) HideToTray(); else ShowFromTray(); }

        void HideToTray()
        {
            if (tray == null) { WindowState = FormWindowState.Minimized; return; }   // no icon: the taskbar
            tip.Hide(this);
            Hide();
        }

        void ShowFromTray()
        {
            if (!Visible) Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
            Render();
        }

        void StopTray()
        {
            if (tray == null) return;
            tray.Visible = false;   // else the icon stays in the notification area until the mouse passes over it
            if (tray.ContextMenuStrip != null) tray.ContextMenuStrip.Dispose();
            tray.Dispose();
            tray = null;
        }

        // the menu in the body's colours: dark, the text in the ink, the item under the mouse a little lighter
        class TrayMenuRenderer : ToolStripProfessionalRenderer
        {
            static readonly Color Back = Color.FromArgb(28, 30, 34), Hot = Color.FromArgb(48, 51, 57), Line = Color.FromArgb(60, 64, 72);
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) { using (var b = new SolidBrush(Back)) e.Graphics.FillRectangle(b, e.AffectedBounds); }
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { using (var p = new Pen(Line)) e.Graphics.DrawRectangle(p, 0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1); }
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.Selected) return;
                using (var b = new SolidBrush(Hot)) e.Graphics.FillRectangle(b, new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height));
            }
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) { e.TextColor = Color.FromArgb(225, 229, 236); base.OnRenderItemText(e); }
            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) { using (var p = new Pen(Line)) e.Graphics.DrawLine(p, 4, e.Item.Height / 2, e.Item.Width - 4, e.Item.Height / 2); }
        }
    }
}
