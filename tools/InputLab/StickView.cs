using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace InputStitch.Tools.InputLab
{
    internal sealed class StickView : Control
    {
        private double x;
        private double y;

        internal StickView()
        {
            DoubleBuffered = true;
            MinimumSize = new Size(70, 70);
            Size = new Size(100, 100);
            BackColor = Color.White;
        }

        internal void SetValue(double newX, double newY)
        {
            x = Math.Max(-1.0, Math.Min(1.0, newX));
            y = Math.Max(-1.0, Math.Min(1.0, newY));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int size = Math.Min(ClientSize.Width, ClientSize.Height) - 18;
            if (size < 20) return;
            Rectangle circle = new Rectangle((ClientSize.Width - size) / 2, (ClientSize.Height - size) / 2, size, size);
            using (Pen border = new Pen(Color.FromArgb(90, 100, 115), 2f))
            using (Pen axis = new Pen(Color.FromArgb(215, 220, 228), 1f))
            using (Brush fill = new SolidBrush(Color.FromArgb(246, 248, 251)))
            using (Brush dot = new SolidBrush(Color.FromArgb(32, 116, 214)))
            {
                e.Graphics.FillEllipse(fill, circle);
                e.Graphics.DrawEllipse(border, circle);
                float cx = circle.Left + circle.Width / 2f;
                float cy = circle.Top + circle.Height / 2f;
                e.Graphics.DrawLine(axis, circle.Left, cy, circle.Right, cy);
                e.Graphics.DrawLine(axis, cx, circle.Top, cx, circle.Bottom);
                float radius = circle.Width / 2f - 8f;
                float px = cx + (float)x * radius;
                float py = cy - (float)y * radius;
                e.Graphics.FillEllipse(dot, px - 7f, py - 7f, 14f, 14f);
            }
        }
    }
}
