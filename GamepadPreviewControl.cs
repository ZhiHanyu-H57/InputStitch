using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace InputStitch
{
    // Extracted from InputStitch.cs during the incremental MainForm decomposition (architecture-review
    // finding F4); members moved verbatim, behaviour is unchanged.
    internal sealed class GamepadPreviewControl : Control
    {
        private bool active;
        private GamepadControl selectedControl;
        private int stickAngle;
        private int stickStrength;
        private int triggerStrength;

        public GamepadPreviewControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            MinimumSize = new Size(230, 210);
            TabStop = false;
            AccessibleRole = AccessibleRole.Graphic;
        }

        public void SetState(bool isActive, GamepadControl control, int angle, int strength, int trigger)
        {
            active = isActive;
            selectedControl = control;
            stickAngle = Math.Max(-180, Math.Min(180, angle));
            stickStrength = Math.Max(0, Math.Min(100, strength));
            triggerStrength = Math.Max(0, Math.Min(100, trigger));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            int diagramHeight = Math.Max(1, ClientSize.Height - Font.Height * 2 - 12);
            float scale = Math.Min(ClientSize.Width / 300F, diagramHeight / 210F);
            if (scale <= 0F) return;
            float contentTop = (ClientSize.Height - (210F * scale + Font.Height * 2 + 12)) / 2F;
            int captionTop = (int)Math.Ceiling(contentTop + 210F * scale + 4);
            GraphicsState saved = g.Save();
            try
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.TranslateTransform((ClientSize.Width - 300F * scale) / 2F,
                    contentTop);
                g.ScaleTransform(scale, scale);
                bool ps4 = string.Equals(GamepadOutput.PreferredType, VirtualGamepadTypes.DualShock4,
                    StringComparison.OrdinalIgnoreCase);

                DrawTrigger(g, new RectangleF(60, 5, 44, 18), GamepadControl.LeftTrigger, ps4 ? "L2" : "LT");
                DrawTrigger(g, new RectangleF(196, 5, 44, 18), GamepadControl.RightTrigger, ps4 ? "R2" : "RT");
                DrawPill(g, new RectangleF(44, 30, 73, 17), Selected(GamepadControl.LeftShoulder), ps4 ? "L1" : "LB");
                DrawPill(g, new RectangleF(183, 30, 73, 17), Selected(GamepadControl.RightShoulder), ps4 ? "R1" : "RB");
                DrawShell(g, ps4);

                if (ps4)
                {
                    using (GraphicsPath touch = RoundRect(new RectangleF(121, 66, 58, 41), 7))
                    using (Brush fill = new SolidBrush(Color.FromArgb(64, 72, 82)))
                    using (Pen border = new Pen(Color.FromArgb(42, 50, 60), 1F))
                    {
                        g.FillPath(fill, touch);
                        g.DrawPath(border, touch);
                    }
                    using (Brush dots = new SolidBrush(Color.FromArgb(88, 97, 107)))
                        for (int y = 73; y < 102; y += 5)
                            for (int x = 128; x < 174; x += 5) g.FillEllipse(dots, x, y, 1F, 1F);
                    DrawUtility(g, 106, 80, GamepadControl.Back, false, true);
                    DrawUtility(g, 194, 80, GamepadControl.Start, true, true);
                    DrawGuide(g, 150, 132);
                    DrawDPad(g, 65, 97);
                    DrawStick(g, 106, 148, GamepadControl.LeftStick, GamepadControl.LeftThumb);
                    DrawStick(g, 194, 148, GamepadControl.RightStick, GamepadControl.RightThumb);
                }
                else
                {
                    DrawGuide(g, 150, 77);
                    DrawUtility(g, 125, 104, GamepadControl.Back, false, false);
                    DrawUtility(g, 175, 104, GamepadControl.Start, true, false);
                    DrawStick(g, 66, 96, GamepadControl.LeftStick, GamepadControl.LeftThumb);
                    DrawDPad(g, 110, 148);
                    DrawStick(g, 194, 148, GamepadControl.RightStick, GamepadControl.RightThumb);
                }
                DrawFaceButton(g, 236, 76, GamepadControl.North, ps4);
                DrawFaceButton(g, 257, 97, GamepadControl.East, ps4);
                DrawFaceButton(g, 215, 97, GamepadControl.West, ps4);
                DrawFaceButton(g, 236, 118, GamepadControl.South, ps4);

                string detail = active ? InputNames.FormatGamepadControl(selectedControl) : Localizer.T("虚拟手柄");
                if (active && (selectedControl == GamepadControl.LeftStick || selectedControl == GamepadControl.RightStick))
                    detail += "  ·  " + stickAngle + "° / " + stickStrength + "%";
                else if (active && (selectedControl == GamepadControl.LeftTrigger || selectedControl == GamepadControl.RightTrigger))
                    detail += "  ·  " + triggerStrength + "%";
                AccessibleName = detail;
            }
            finally { g.Restore(saved); }
            // Captions use the interface font, independent of the controller drawing scale.
            TextRenderer.DrawText(g, string.Equals(GamepadOutput.PreferredType, VirtualGamepadTypes.DualShock4, StringComparison.OrdinalIgnoreCase) ? "DUALSHOCK 4" : "XBOX 360",
                Font, new Rectangle(0, captionTop, Width, Font.Height), EditorTheme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            using (Font detailFont = new Font(Font, FontStyle.Bold))
                TextRenderer.DrawText(g, AccessibleName, detailFont, new Rectangle(0, captionTop + Font.Height + 4, Width, Font.Height), active ? Accent : Ink,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static readonly Color Accent = Color.FromArgb(38, 114, 211);
        private static readonly Color Ink = Color.FromArgb(65, 77, 94);
        private static readonly Color ControlTop = Color.FromArgb(85, 95, 111);
        private static readonly Color ControlBottom = Color.FromArgb(49, 59, 75);

        private bool Selected(GamepadControl control)
        {
            return active && selectedControl == control;
        }

        private static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            float d = Math.Min(radius * 2F, Math.Min(r.Width, r.Height));
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void DrawShell(Graphics g, bool ps4)
        {
            using (GraphicsPath shell = new GraphicsPath())
            {
                shell.StartFigure();
                shell.AddBezier(62, 47, 87, 44, 111, ps4 ? 52 : 61, 150, ps4 ? 52 : 61);
                shell.AddBezier(150, ps4 ? 52 : 61, 189, ps4 ? 52 : 61, 213, 44, 238, 47);
                shell.AddBezier(238, 47, 267, 50, 275, 72, 280, 99);
                shell.AddBezier(280, 99, 286, 129, 292, 175, 277, 194);
                shell.AddBezier(277, 194, 264, 210, 249, 200, 235, 181);
                shell.AddBezier(235, 181, 218, 159, 207, 174, 184, 176);
                shell.AddBezier(184, 176, 160, 177, 140, 177, 116, 176);
                shell.AddBezier(116, 176, 93, 174, 82, 159, 65, 181);
                shell.AddBezier(65, 181, 51, 200, 36, 210, 23, 194);
                shell.AddBezier(23, 194, 8, 175, 14, 129, 20, 99);
                shell.AddBezier(20, 99, 25, 72, 33, 50, 62, 47);
                shell.CloseFigure();
                GraphicsState shadowState = g.Save();
                g.TranslateTransform(0, 4);
                using (Pen shadow = new Pen(Color.FromArgb(13, 40, 57, 79), 7F)) g.DrawPath(shadow, shell);
                using (Brush shadow = new SolidBrush(Color.FromArgb(18, 40, 57, 79))) g.FillPath(shadow, shell);
                g.Restore(shadowState);
                using (Brush fill = new LinearGradientBrush(new RectangleF(12, 45, 276, 158),
                    Color.FromArgb(250, 251, 253), Color.FromArgb(217, 224, 234), LinearGradientMode.Vertical))
                using (Pen border = new Pen(Color.FromArgb(174, 185, 200), 1F))
                {
                    g.FillPath(fill, shell);
                    g.DrawPath(border, shell);
                }
                using (Pen seam = new Pen(Color.FromArgb(196, 205, 217), 1F))
                {
                    g.DrawBezier(seam, 29, 132, 32, 161, 34, 185, 44, 189);
                    g.DrawBezier(seam, 271, 132, 268, 161, 266, 185, 256, 189);
                }
            }
        }

        private void DrawStick(Graphics g, float cx, float cy, GamepadControl vectorControl, GamepadControl clickControl)
        {
            bool vector = Selected(vectorControl);
            bool selected = vector || Selected(clickControl);
            using (Brush well = new SolidBrush(Color.FromArgb(186, 198, 215))) g.FillEllipse(well, cx - 25, cy - 25, 50, 50);
            using (Brush well = new SolidBrush(Color.FromArgb(55, 67, 85))) g.FillEllipse(well, cx - 22, cy - 22, 44, 44);
            using (Pen ring = new Pen(selected ? Color.FromArgb(124, 180, 245) : Color.FromArgb(233, 238, 245), selected ? 2F : 1F))
                g.DrawEllipse(ring, cx - 24, cy - 24, 48, 48);
            float dx = 0, dy = 0;
            if (vector)
            {
                double radians = stickAngle * Math.PI / 180.0;
                dx = (float)Math.Sin(radians) * 10F * stickStrength / 100F;
                dy = -(float)Math.Cos(radians) * 10F * stickStrength / 100F;
                if (stickStrength > 0)
                    using (Pen direction = new Pen(Color.FromArgb(143, 199, 255), 3F))
                    {
                        direction.StartCap = LineCap.Round;
                        direction.EndCap = LineCap.Round;
                        g.DrawLine(direction, cx, cy, cx + dx * 1.9F, cy + dy * 1.9F);
                    }
            }
            using (Brush shadow = new SolidBrush(Color.FromArgb(58, 12, 24, 42))) g.FillEllipse(shadow, cx + dx - 15, cy + dy - 12, 30, 30);
            DrawDisc(g, cx + dx, cy + dy, 15, selected);
            using (Pen edge = new Pen(selected ? Color.FromArgb(119, 182, 255) : Color.FromArgb(128, 141, 158), 0.8F))
                g.DrawEllipse(edge, cx + dx - 11, cy + dy - 11, 22, 22);
            DrawText(g, vectorControl == GamepadControl.LeftStick ? "L" : "R",
                new RectangleF(cx + dx - 8, cy + dy - 7, 16, 14), 9F, Color.FromArgb(232, 239, 248), true);
        }

        private void DrawDPad(Graphics g, float cx, float cy)
        {
            using (Brush well = new SolidBrush(Color.FromArgb(201, 212, 226))) g.FillEllipse(well, cx - 24, cy - 24, 48, 48);
            using (GraphicsPath cross = new GraphicsPath())
            using (Brush fill = new LinearGradientBrush(new RectangleF(cx - 21, cy - 21, 42, 42), ControlTop, ControlBottom, LinearGradientMode.Vertical))
            {
                cross.AddPolygon(new PointF[] {
                    new PointF(cx-7,cy-21), new PointF(cx+7,cy-21), new PointF(cx+7,cy-7), new PointF(cx+21,cy-7),
                    new PointF(cx+21,cy+7), new PointF(cx+7,cy+7), new PointF(cx+7,cy+21), new PointF(cx-7,cy+21),
                    new PointF(cx-7,cy+7), new PointF(cx-21,cy+7), new PointF(cx-21,cy-7), new PointF(cx-7,cy-7) });
                g.FillPath(fill, cross);
            }
            DrawPadArrow(g, cx, cy, 0, GamepadControl.DPadUp);
            DrawPadArrow(g, cx, cy, 90, GamepadControl.DPadRight);
            DrawPadArrow(g, cx, cy, 180, GamepadControl.DPadDown);
            DrawPadArrow(g, cx, cy, 270, GamepadControl.DPadLeft);
        }

        private void DrawPadArrow(Graphics g, float cx, float cy, float angle, GamepadControl control)
        {
            GraphicsState saved = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(angle);
            if (Selected(control))
                using (Brush fill = new SolidBrush(Accent)) g.FillRectangle(fill, -7, -21, 14, 16);
            using (Brush arrow = new SolidBrush(Selected(control) ? Color.White : Color.FromArgb(176, 190, 210)))
                g.FillPolygon(arrow, new PointF[] { new PointF(0,-17), new PointF(-3,-12), new PointF(3,-12) });
            g.Restore(saved);
        }

        private static void DrawDisc(Graphics g, float cx, float cy, float radius, bool selected)
        {
            RectangleF rect = new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2);
            using (Brush fill = new LinearGradientBrush(rect,
                selected ? Color.FromArgb(74, 150, 239) : ControlTop,
                selected ? Accent : ControlBottom, LinearGradientMode.Vertical))
            using (Pen edge = new Pen(selected ? Color.FromArgb(29, 96, 186) : Color.FromArgb(38, 48, 65), 0.8F))
            {
                g.FillEllipse(fill, rect);
                g.DrawEllipse(edge, rect);
            }
        }

        private void DrawFaceButton(Graphics g, float cx, float cy, GamepadControl control, bool ps4)
        {
            bool selected = Selected(control);
            using (Brush seat = new SolidBrush(Color.FromArgb(187, 199, 215))) g.FillEllipse(seat, cx-12, cy-11, 24, 24);
            DrawDisc(g, cx, cy, 10.5F, selected);
            Color symbol = selected ? Color.White :
                control == GamepadControl.South ? (ps4 ? Color.FromArgb(121, 190, 251) : Color.FromArgb(138, 219, 145)) :
                control == GamepadControl.East ? Color.FromArgb(248, 150, 157) :
                control == GamepadControl.West ? (ps4 ? Color.FromArgb(239, 160, 217) : Color.FromArgb(120, 185, 255)) :
                (ps4 ? Color.FromArgb(123, 222, 197) : Color.FromArgb(252, 218, 115));
            if (!ps4)
            {
                string label = control == GamepadControl.South ? "A" : control == GamepadControl.East ? "B" : control == GamepadControl.West ? "X" : "Y";
                DrawText(g, label, new RectangleF(cx - 10, cy - 8, 20, 16), 11F, symbol, true);
                return;
            }
            using (Pen pen = new Pen(symbol, 1.4F))
            {
                pen.LineJoin = LineJoin.Round;
                if (control == GamepadControl.North)
                    g.DrawPolygon(pen, new PointF[] { new PointF(cx,cy-5), new PointF(cx-5,cy+4), new PointF(cx+5,cy+4) });
                else if (control == GamepadControl.East) g.DrawEllipse(pen, cx-4.5F, cy-4.5F, 9, 9);
                else if (control == GamepadControl.West) g.DrawRectangle(pen, cx-4, cy-4, 8, 8);
                else { g.DrawLine(pen, cx-4, cy-4, cx+4, cy+4); g.DrawLine(pen, cx-4, cy+4, cx+4, cy-4); }
            }
        }

        private static void DrawPill(Graphics g, RectangleF rect, bool selected, string text)
        {
            using (GraphicsPath pill = RoundRect(rect, 6))
            using (Brush fill = new LinearGradientBrush(rect, selected ? Color.FromArgb(74, 150, 239) : ControlTop,
                selected ? Accent : ControlBottom, LinearGradientMode.Vertical))
                g.FillPath(fill, pill);
            DrawText(g, text, rect, 10F, Color.White, true);
        }

        private void DrawTrigger(Graphics g, RectangleF rect, GamepadControl control, string text)
        {
            bool selected = Selected(control);
            DrawPill(g, rect, selected, text);
            RectangleF track = new RectangleF(rect.X + 4, rect.Bottom + 1, rect.Width - 8, 2);
            using (Brush empty = new SolidBrush(Color.FromArgb(221, 229, 239))) g.FillRectangle(empty, track);
            if (selected && triggerStrength > 0)
                using (Brush fill = new SolidBrush(Accent))
                    g.FillRectangle(fill, track.X, track.Y, track.Width * triggerStrength / 100F, track.Height);
        }

        private void DrawUtility(Graphics g, float cx, float cy, GamepadControl control, bool menu, bool ps4)
        {
            DrawDisc(g, cx, cy, ps4 ? 7F : 8.5F, Selected(control));
            using (Pen pen = new Pen(Color.FromArgb(229, 237, 247), 1F))
            {
                if (menu)
                    for (int i = -1; i <= 1; i++) g.DrawLine(pen, cx - 3, cy + i * 2.5F, cx + 3, cy + i * 2.5F);
                else
                {
                    g.DrawRectangle(pen, cx - 3, cy - 3, 4.5F, 4.5F);
                    g.DrawLines(pen, new PointF[] { new PointF(cx+3,cy-1), new PointF(cx+3,cy+3), new PointF(cx-1,cy+3) });
                }
            }
        }

        private void DrawGuide(Graphics g, float cx, float cy)
        {
            DrawDisc(g, cx, cy, 9, Selected(GamepadControl.Guide));
            using (Pen pen = new Pen(Color.FromArgb(226, 236, 249), 1.3F))
            {
                g.DrawArc(pen, cx-4, cy-4, 8, 8, -55, 290);
                g.DrawLine(pen, cx, cy-5, cx, cy);
            }
        }

        private static void DrawText(Graphics g, string text, RectangleF rect, float size, Color color, bool bold)
        {
            using (Font font = new Font("Microsoft YaHei UI", size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel))
            using (Brush brush = new SolidBrush(color))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.FormatFlags = StringFormatFlags.NoWrap;
                format.Trimming = StringTrimming.EllipsisCharacter;
                g.DrawString(text, font, brush, rect, format);
            }
        }
    }

    internal sealed class InputSinkControl : Control
    {
        public InputSinkControl()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = false;
            Size = new Size(1, 1);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            e.Handled = true;
        }
    }
}
