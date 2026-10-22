using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace InputStitch
{
    // Accessible labels and hit regions over a shared, lazily loaded product image. No input injection.
    internal sealed class MouseInputDiagram : Panel
    {
        private readonly Dictionary<Button, RectangleF> positions = new Dictionary<Button, RectangleF>();
        internal event Action<InputKind> InputSelected;
        internal InputKind? SelectedKind;
        private readonly HashSet<InputKind> selectedKinds = new HashSet<InputKind>();
        internal static bool IsMouse(InputKind kind)
        { return kind >= InputKind.MouseLeft && kind <= InputKind.WheelDown; }

        internal MouseInputDiagram()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            MinimumSize = new Size(300, 270);
            Add(InputKind.MouseLeft, "左键", "Left", 0, 40, 96, 32);
            Add(InputKind.MouseRight, "右键", "Right", 260, 40, 98, 32);
            Add(InputKind.MouseMiddle, "中键", "Middle", 260, 100, 98, 32);
            Add(InputKind.WheelUp, "滚轮向上", "Wheel ↑", 260, 142, 98, 32);
            Add(InputKind.WheelDown, "滚轮向下", "Wheel ↓", 260, 184, 98, 32);
            Add(InputKind.MouseX1, "后退侧键 X1", "Back X1", 0, 186, 112, 32);
            Add(InputKind.MouseX2, "前进侧键 X2", "Forward X2", 0, 142, 112, 32);
            AccessibleName = VirtualKeyboardDialog.TextFor("鼠标按键图", "Mouse button diagram");
        }

        private void Add(InputKind kind, string zh, string en, float x, float y, float w, float h)
        {
            Button button = new EditorButton { CompactText = true, Text = VirtualKeyboardDialog.TextFor(zh, en),
                Tag = kind, Margin = Padding.Empty, TabStop = true };
            button.AccessibleName = InputNames.FormatInput(new InputSpec { Kind = kind });
            button.Click += delegate
            {
                Action<InputKind> handler = InputSelected;
                if (handler != null) handler(kind);
            };
            positions.Add(button, new RectangleF(x, y, w, h));
            Controls.Add(button);
        }

        internal void SetSelection(InputKind? kind)
        {
            SelectedKind = kind;
            selectedKinds.Clear();
            if (kind.HasValue) selectedKinds.Add(kind.Value);
            RefreshSelection();
        }

        internal void SetSelections(IList<InputSpec> inputs)
        {
            SelectedKind = null; selectedKinds.Clear();
            foreach (InputSpec input in inputs) if (IsMouse(input.Kind))
            { selectedKinds.Add(input.Kind); if (!SelectedKind.HasValue) SelectedKind = input.Kind; }
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            foreach (Button button in positions.Keys)
            {
                bool selected = selectedKinds.Contains((InputKind)button.Tag);
                button.BackColor = selected ? EditorTheme.Selection : Color.White;
                button.ForeColor = selected ? EditorTheme.Accent : EditorTheme.Ink;
                button.AccessibleDescription = VirtualKeyboardDialog.TextFor(selected ? "已选中" : "点击选择", selected ? "Selected" : "Click to select");
            }
            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (positions == null) return;
            float sx = ClientSize.Width / 360F, sy = Math.Min(ClientSize.Height / 300F, sx);
            foreach (KeyValuePair<Button, RectangleF> pair in positions)
            {
                RectangleF r = pair.Value;
                pair.Key.Bounds = Rectangle.Round(new RectangleF(r.X * sx, r.Y * sy, r.Width * sx, Math.Max(r.Height * sy, pair.Key.Font.Height + 8)));
            }
        }

        private static readonly Dictionary<InputKind, PointF> anchors = new Dictionary<InputKind, PointF>
        {
            { InputKind.MouseLeft, new PointF(151, 73) }, { InputKind.MouseRight, new PointF(226, 59) },
            { InputKind.MouseMiddle, new PointF(186, 72) }, { InputKind.WheelUp, new PointF(182, 57) },
            { InputKind.WheelDown, new PointF(190, 88) }, { InputKind.MouseX2, new PointF(138, 122) },
            { InputKind.MouseX1, new PointF(151, 159) }
        };
        private static readonly Lazy<Bitmap> mousePhoto = new Lazy<Bitmap>(LoadPhoto);

        private static Bitmap LoadPhoto()
        {
            System.Reflection.Assembly assembly = typeof(MouseInputDiagram).Assembly;
            using (System.IO.Stream colorStream = assembly.GetManifestResourceStream("InputStitch.Assets.mouse-color.jpg"))
            using (System.IO.Stream alphaStream = assembly.GetManifestResourceStream("InputStitch.Assets.mouse-alpha.png"))
            {
                if (colorStream == null || alphaStream == null) throw new InvalidOperationException("Mouse artwork was not embedded.");
                using (Bitmap color = new Bitmap(colorStream))
                using (Bitmap alpha = new Bitmap(alphaStream))
                {
                    Bitmap result = new Bitmap(color.Width, color.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (Graphics graphics = Graphics.FromImage(result))
                        graphics.DrawImage(color, new Rectangle(0, 0, color.Width, color.Height), 0, 0, color.Width, color.Height, GraphicsUnit.Pixel);
                    System.Drawing.Imaging.BitmapData data = result.LockBits(new Rectangle(0, 0, result.Width, result.Height),
                        System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    try
                    {
                        byte[] row = new byte[result.Width * 4];
                        for (int y = 0; y < result.Height; y++)
                        {
                            IntPtr address = IntPtr.Add(data.Scan0, y * data.Stride);
                            System.Runtime.InteropServices.Marshal.Copy(address, row, 0, row.Length);
                            for (int x = 0; x < result.Width; x++) row[x * 4 + 3] = alpha.GetPixel(x, y).R;
                            System.Runtime.InteropServices.Marshal.Copy(row, 0, address, row.Length);
                        }
                    }
                    finally { result.UnlockBits(data); }
                    return result;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            Bitmap photo = mousePhoto.Value;
            g.DrawImage(photo, GetPhotoBounds(), new RectangleF(0, 0, photo.Width, photo.Height), GraphicsUnit.Pixel);
            Callout(g, InputKind.MouseLeft);
            Callout(g, InputKind.MouseRight);
            if (selectedKinds.Contains(InputKind.WheelUp)) Callout(g, InputKind.WheelUp);
            else if (selectedKinds.Contains(InputKind.WheelDown)) Callout(g, InputKind.WheelDown);
            else Callout(g, InputKind.MouseMiddle);
            Callout(g, InputKind.MouseX2);
            Callout(g, InputKind.MouseX1);
        }

        private RectangleF GetPhotoBounds()
        {
            float sx = ClientSize.Width / 360F, sy = Math.Min(ClientSize.Height / 300F, sx);
            float scale = Math.Min(sx, sy);
            return new RectangleF(100 * sx + 85 * (sx - scale), 20 * sy, 170 * scale, 255 * scale);
        }

        private PointF GetCalloutTarget(InputKind kind)
        {
            RectangleF photo = GetPhotoBounds();
            PointF target = anchors[kind];
            return new PointF(photo.X + (target.X - 100) / 170 * photo.Width, photo.Y + (target.Y - 20) / 255 * photo.Height);
        }

        private void Callout(Graphics graphics, InputKind kind)
        {
            RectangleF photo = GetPhotoBounds();
            PointF target = GetCalloutTarget(kind);
            foreach (Button button in positions.Keys) if ((InputKind)button.Tag == kind)
            {
                PointF label = new PointF(button.Left < photo.Left ? button.Right : button.Left, button.Top + button.Height / 2F);
                bool selected = selectedKinds.Contains(kind);
                using (Pen line = new Pen(selected ? EditorTheme.Accent : Color.FromArgb(173, 185, 203), selected ? 1.5F : .8F))
                {
                    graphics.DrawLine(line, label, target);
                    if (selected) graphics.DrawEllipse(line, target.X - 5, target.Y - 5, 10, 10);
                }
                return;
            }
        }

        private InputKind? Hit(Point point)
        {
            RectangleF photo = GetPhotoBounds();
            if (photo.Width <= 0 || photo.Height <= 0) return null;
            PointF p = new PointF(100 + (point.X - photo.Left) / photo.Width * 170, 20 + (point.Y - photo.Top) / photo.Height * 255);
            if (new RectangleF(128, 104, 25, 36).Contains(p)) return InputKind.MouseX2;
            if (new RectangleF(140, 142, 24, 40).Contains(p)) return InputKind.MouseX1;
            if (new RectangleF(172, 49, 26, 53).Contains(p)) return InputKind.MouseMiddle;
            if (new RectangleF(130, 45, 45, 90).Contains(p)) return InputKind.MouseLeft;
            if (new RectangleF(202, 45, 52, 105).Contains(p)) return InputKind.MouseRight;
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        { base.OnMouseMove(e); Cursor = Hit(e.Location).HasValue ? Cursors.Hand : Cursors.Default; }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            InputKind? kind = Hit(e.Location);
            if (e.Button == MouseButtons.Left && kind.HasValue && InputSelected != null) InputSelected(kind.Value);
        }
    }
}
