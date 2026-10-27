// Controls.cs - the rounded text field (the installer's folder and license fields) and the glass card that hosts
// controls.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TheNorns
{
    // Rounded dark text field (single or multi line) - the one field of the app (UI review 25.10.2026: the Combat settings
    // window had its own); numeric = right-aligned value; Changed = yellow text (differs from the default); Invalid = red frame
    class DarkField : Panel
    {
        public readonly TextBox Box = new TextBox();
        bool invalid, changed;
        public bool Invalid { get { return invalid; } set { if (invalid != value) { invalid = value; Invalidate(); } } }
        public bool Changed { get { return changed; } set { changed = value; Box.ForeColor = value ? Theme.Yellow : Theme.Text; } }
        public DarkField(bool multiline = false, bool numeric = false)
        {
            DoubleBuffered = true; BackColor = Theme.Card; ResizeRedraw = true;
            Box.BorderStyle = BorderStyle.None; Box.BackColor = Theme.CardInner; Box.ForeColor = Theme.Text;
            Box.Font = new Font(multiline ? "Consolas" : "Segoe UI", multiline ? 9f : Theme.FBody);
            if (numeric) Box.TextAlign = HorizontalAlignment.Right;
            Box.Multiline = multiline; if (multiline) { Box.AcceptsReturn = true; Box.WordWrap = false; }
            Box.GotFocus += (s, e) => Invalidate(); Box.LostFocus += (s, e) => Invalidate();
            Controls.Add(Box);
            if (multiline) ThinScroll.Attach(Box);   // thin scroll bar in the app's style instead of the Windows one
        }
        public override string Text { get { return Box.Text; } set { Box.Text = value; } }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e); int p = (int)(10 * Theme.K);
            if (Box.Multiline) Box.SetBounds(p, p, Width - 2 * p - (int)ThinScroll.Gutter, Height - 2 * p);
            else Box.SetBounds(p, (Height - Box.PreferredHeight) / 2, Width - 2 * p, Box.PreferredHeight);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; Theme.Smooth(g); g.Clear(Parent != null ? Parent.BackColor : Theme.Card);
            using (var path = Theme.Round(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), Math.Min(12 * Theme.K, Height * 0.32f)))
            using (var b = new SolidBrush(Theme.CardInner)) using (var pen = new Pen(invalid ? Theme.Red : Box.Focused ? Theme.Blue : Theme.Border))
            { g.FillPath(b, path); g.DrawPath(pen, path); }
        }
    }

    // Glass card panel that hosts real controls (setup steps, settings): "● title" inside the glass, 8 px glass rim,
    // inner panel in Theme.Card (the hosted labels / pills use it as their background)
    class Panel2 : Panel
    {
        public string Title = ""; public Color Dot = Theme.Text;
        readonly Glass.Cache glass = new Glass.Cache { Header = 38 * Theme.K, InnerColor = Theme.Card };
        public Panel2()
        {
            DoubleBuffered = true; BackColor = Theme.Card; ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
            Glass.Track(this);
        }
        protected override void Dispose(bool disposing) { if (disposing) glass.Dispose(); base.Dispose(disposing); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            g.DrawImageUnscaled(glass.Get(this), 0, 0);
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            Theme.Smooth(g);
            Glass.PaintPointerGlow(g, this, glass.Header);
            if (Title != "") Glass.PaintHeader(g, Title, Dot, glass.Header, Width);
        }
    }
}
