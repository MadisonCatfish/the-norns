// The Norns - the client squares right of the body (owner, 29.10.2026): one per open client, at most 8, in the order of
// Settings -> Carousel's table; the active one in the accent; a click on another one brings that client to the front.
// Shown only while the client module is on (Fleet.cs).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace TheNorns
{
    // the window the squares are drawn into: layered, never active (a click must not take the front from EVE)
    sealed class ClientDock : Form
    {
        public ClientDock()
        {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None; TopMost = true; Cursor = Glass.Pointer;
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80 /* TOOLWINDOW */ | 0x08000000 /* NOACTIVATE */ | 0x80000 /* LAYERED */;
                return cp;
            }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x21 /* WM_MOUSEACTIVATE */) { m.Result = (IntPtr)3 /* MA_NOACTIVATE */; return; }
            base.WndProc(ref m);
        }
    }

    partial class DpsForm
    {
        public const int DockMax = 8;
        ClientDock dock;
        List<ClientWindow> dockClients = new List<ClientWindow>();
        int dockHot = -1;
        string dockShown;   // what the dock shows now (clients, active, hover, place): drawn again only when it changes
        public List<string> DemoDock; public int DemoDockActive;   // selftest: the squares' characters, the active one

        float DockSquare { get { return U(12); } }
        float DockSquareH { get { return U(16); } }   // owner, 29.10.2026: 4 px longer down than wide
        float DockGap { get { return U(4); } }
        float DockMargin { get { return U(2); } }
        // the side of the body the squares are on (owner, 29.10.2026; config.json "app": { "fleet_dock": "left" / "top" /
        // "right" }): a column at the left or the right, a row at the top
        public static readonly string[] DockSides = { "left", "top", "right" };
        public static string DockSide = DockSides.Contains(AppSetting("fleet_dock") as string) ? (string)AppSetting("fleet_dock") : "right";
        bool DockRow { get { return DockSide == "top"; } }
        RectangleF DockCell(int i)
        {
            float m = DockMargin;
            return DockRow ? new RectangleF(m + i * (DockSquare + DockGap), m, DockSquare, DockSquareH)
                           : new RectangleF(m, m + i * (DockSquareH + DockGap), DockSquare, DockSquareH);
        }
        Size DockSize(int n) { var r = DockCell(n - 1); return new Size((int)Math.Ceiling(r.Right + DockMargin), (int)Math.Ceiling(r.Bottom + DockMargin)); }
        // where the dock lies, from the body's top left corner
        Point DockOffset(Size d)
        {
            int m = (int)DockMargin, away = (int)U(5), midX = (Width - d.Width) / 2, midY = (Height - d.Height) / 2;   // centred on the side
            if (DockSide == "left") return new Point(-away - d.Width + m, midY);
            if (DockRow) return new Point(midX, -away - d.Height + m);
            return new Point(Width + away - m, midY);
        }

        // every body frame: the dock follows the body, the clients and the one in front
        void SyncDock()
        {
            if (fleet == null || !Visible) { HideDock(); return; }
            dockClients = fleet.Clients.Ordered(DockMax);
            if (dockClients.Count == 0) { HideDock(); return; }
            var active = fleet.Clients.Active;
            int on = dockClients.FindIndex(w => w.Hwnd == active);
            var off = DockOffset(DockSize(dockClients.Count));
            var at = new Point(Left + off.X, Top + off.Y);
            string key = string.Join("|", dockClients.Select(w => w.Hwnd.ToInt64() + ":" + w.Character)) + "/" + on + "/" + dockHot + "/" + at;
            if (dock == null)
            {
                dock = new ClientDock();
                dock.MouseMove += (s, e) => { int i = DockIndex(e.Location); if (i != dockHot) { dockHot = i; SyncDock(); } };
                dock.MouseLeave += (s, e) => { if (dockHot != -1) { dockHot = -1; SyncDock(); } };
                dock.MouseDown += (s, e) => DockClick(DockIndex(e.Location));
            }
            if (!dock.Visible) { dock.Location = at; dock.Show(this); dockShown = null; }
            if (key == dockShown) return;
            dockShown = key;
            dock.Location = at;
            using (var b = DrawDock(dockClients.Count, on, dockHot)) { dock.Size = b.Size; AppChrome.PresentLayered(dock, b); }
        }

        void HideDock()
        {
            if (dock != null && dock.Visible) dock.Hide();
            dockHot = -1; dockShown = null;
        }

        void DisposeDock() { if (dock != null) { dock.Dispose(); dock = null; } dockShown = null; }

        int DockIndex(Point p)
        {
            for (int i = 0; i < dockClients.Count; i++) if (DockCell(i).Contains(p)) return i;
            return -1;
        }

        void DockClick(int i)
        {
            if (fleet == null || i < 0 || i >= dockClients.Count) return;
            var w = dockClients[i];
            if (w.Hwnd == fleet.Clients.Active) return;
            fleet.SwitchTo(w, "square " + (i + 1));
            Render();
        }

        // n squares in a column (or a row at the top), the body's steel with its satin light and edge; on = the one in front (the
        // accent), hot = under the mouse
        public Bitmap DrawDock(int n, int on, int hot)
        {
            var sz = DockSize(n);
            var bmp = new Bitmap(sz.Width, sz.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                for (int i = 0; i < n; i++)
                {
                    var r = DockCell(i);
                    // the accent as shipped: the squares sit beside the game, outside the body, and stay as they are with eye comfort
                    var fill = i == on ? Palette.All[0].Accent : i == hot ? Mix(BodyTheme.Hull, Color.White, 0.18) : BodyTheme.Hull;
                    using (var p = Round(r, 6))
                    {
                        using (var b = new SolidBrush(fill)) g.FillPath(b, p);
                        if (!Potato)
                            using (var lg = new LinearGradientBrush(r, Color.White, Color.Black, 80f))
                            {
                                lg.InterpolationColors = new ColorBlend { Colors = new[] { Color.FromArgb(36, 255, 255, 255), Color.FromArgb(5, 255, 255, 255), Color.FromArgb(66, 0, 0, 0) }, Positions = new[] { 0f, 0.45f, 1f } };
                                g.FillPath(lg, p);
                            }
                    }
                    var edge = r; edge.Inflate(-0.5f, -0.5f);
                    using (var ep = Round(edge, 6))
                    using (var lg = new LinearGradientBrush(r, Color.FromArgb(70, 255, 255, 255), Color.FromArgb(115, 0, 0, 0), 90f))
                    using (var pen = new Pen(lg, 1f))
                        g.DrawPath(pen, ep);
                }
            }
            return bmp;
        }

        // selftest: the body with its squares beside it, as on the screen
        public Bitmap DrawWithDock()
        {
            var body = Draw();
            if (DemoDock == null || DemoDock.Count == 0) return body;
            using (body)
            using (var d = DrawDock(Math.Min(DockMax, DemoDock.Count), DemoDockActive, -1))
            {
                var o = DockOffset(d.Size);
                int bx = Math.Max(0, -o.X), by = Math.Max(0, -o.Y);   // the body moved over when the dock is left of it or above
                var all = new Bitmap(Math.Max(bx + Width, bx + o.X + d.Width), Math.Max(by + Height, by + o.Y + d.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(all)) { g.DrawImageUnscaled(body, bx, by); g.DrawImageUnscaled(d, bx + o.X, by + o.Y); }
                return all;
            }
        }
    }
}
