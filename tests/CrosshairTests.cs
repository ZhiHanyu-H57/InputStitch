using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using InputStitch;

internal static class CrosshairTests
{
    private static int checks;
    private static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static string root;
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr value);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point value);
    [DllImport("user32.dll", EntryPoint = "GetWindowLong")] private static extern int GetWindowLong(IntPtr value, int index);
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr value, int command);
    [DllImport("user32.dll")] private static extern int GetGuiResources(IntPtr process, int flags);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sx, int sy, uint operation);
    private static Bitmap Desktop(Rectangle area)
    {
        Bitmap bitmap = new Bitmap(area.Width, area.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            IntPtr source = GetDC(IntPtr.Zero), destination = g.GetHdc();
            try { if (!BitBlt(destination, 0, 0, area.Width, area.Height, source, area.Left, area.Top, 0x40cc0020)) throw new Exception("Desktop capture failed."); }
            finally { g.ReleaseHdc(destination); ReleaseDC(IntPtr.Zero, source); }
        }
        return bitmap;
    }
    private static Color Pixel(Point point) { using (Bitmap bitmap = Desktop(new Rectangle(point, new Size(1, 1)))) return bitmap.GetPixel(0, 0); }
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    private static T Field<T>(object value, string name) { return (T)value.GetType().GetField(name, Hidden).GetValue(value); }
    private static object Call(object value, string name, params object[] args)
    { return value.GetType().GetMethod(name, Hidden).Invoke(value, args); }
    private static void Pump(int ms) { Stopwatch timer = Stopwatch.StartNew(); while (timer.ElapsedMilliseconds < ms) { Application.DoEvents(); Thread.Sleep(2); } }
    private static System.Collections.Generic.IEnumerable<Control> All(Control value)
    { foreach (Control child in value.Controls) { yield return child; foreach (Control nested in All(child)) yield return nested; } }
    private static void Render(Form form, string name)
    {
        form.PerformAutoScale();
        typeof(Control).GetMethod("SetState", Hidden).Invoke(form, new object[] { 2, true }); form.CreateControl(); form.PerformLayout();
        using (Bitmap bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(Path.Combine(root, name + ".png")); }
    }
    [STAThread] private static int Main(string[] args)
    {
        // Match the packaged PerMonitorV2 manifest before creating any controls or reading Screen bounds.
        SetProcessDpiAwarenessContext(new IntPtr(-4));
        Application.EnableVisualStyles(); EmbeddedDependencyLoader.Register();
        root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crosshair"); Directory.CreateDirectory(root);
        try
        {
            if (args.Length == 1 && args[0] == "--view")
            {
                Localizer.SetLanguage("zh-CN");
                using (CrosshairSettingsDialog dialog = new CrosshairSettingsDialog(new CrosshairOptions(), null, null))
                using (System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer())
                {
                    timer.Interval = 250;
                    timer.Tick += delegate
                    {
                        timer.Stop();
                        using (Bitmap bitmap = new Bitmap(dialog.Width, dialog.Height)) using (Graphics g = Graphics.FromImage(bitmap))
                        { g.CopyFromScreen(dialog.Location, Point.Empty, bitmap.Size); bitmap.Save(Path.Combine(root, "settings-shown.png")); }
                        dialog.Close();
                    };
                    timer.Start(); dialog.ShowDialog();
                }
                return 0;
            }
            if (args.Length == 1 && args[0] == "--native") NativeOverlay();
            else if (args.Length == 2 && args[0] == "--soak-minutes") Soak(int.Parse(args[1]));
            else { Persistence(); Geometry(); Dialogs(); Workflow(); NativeOverlay(); }
            Console.WriteLine("PASS crosshair: " + checks + " checks; isolated files, rendering, actual layered window and editor workflow."); return 0;
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(root, "failure.txt"), ex.ToString()); Console.Error.WriteLine(ex); return 1; }
    }
    private static CrosshairOptions Example()
    { return new CrosshairOptions { Enabled = true, Visible = false, Style = CrosshairStyle.RingDot, Size = 61, LineWidth = 3, Gap = 7,
        DotSize = 5, Outline = false, OutlineWidth = 4, Color = "#EF7512", OutlineColor = "#152637", Opacity = 67,
        OffsetX = -12, OffsetY = 31, MonitorDevice = "TEST-DISPLAY", Hotkey = new TriggerSpec { Ctrl = true, Shift = true, VirtualKey = (int)Keys.F9 } }; }
    private static void Persistence()
    {
        string directory = Path.Combine(root, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        string file = Path.Combine(directory, "config.xml");
        File.WriteAllText(file, "<MacroConfig><Language>en-US</Language></MacroConfig>");
        MacroConfig old = ConfigPackageSerializer.DeserializeConfigFromFile(file);
        Check(!old.Crosshair.Enabled && old.Crosshair.Visible && old.Crosshair.OffsetX == 0 && old.Crosshair.OffsetY == 0, "old XML defaults crosshair off and centered");
        MacroConfig config = new MacroConfig { Crosshair = Example() };
        new ConfigStore().Save(file, config);
        CrosshairOptions result = ConfigPackageSerializer.DeserializeConfigFromFile(file).Crosshair;
        Check(result.Enabled && !result.Visible && result.Style == CrosshairStyle.RingDot && result.Size == 61 && result.LineWidth == 3 && result.Gap == 7 && result.DotSize == 5, "shape and visibility survive disk restart");
        Check(!result.Outline && result.OutlineWidth == 4 && result.Color == "#EF7512" && result.OutlineColor == "#152637" && result.Opacity == 67, "outline, both colors and opacity survive disk restart");
        Check(result.OffsetX == -12 && result.OffsetY == 31 && result.MonitorDevice == "TEST-DISPLAY" && result.Hotkey.Ctrl && result.Hotkey.Shift && result.Hotkey.VirtualKey == (int)Keys.F9, "position, display and shortcut survive restart");
        CrosshairOptions clone = ConfigPackageSerializer.CloneConfig(config).Crosshair; clone.Hotkey.VirtualKey = (int)Keys.F11;
        Check(config.Crosshair.Hotkey.VirtualKey == (int)Keys.F9, "cloning does not share editable shortcut state");
        config.Crosshair.Hotkey = null; new ConfigStore().Save(file, config);
        Check(ConfigPackageSerializer.DeserializeConfigFromFile(file).Crosshair.Hotkey == null, "cleared shortcut survives restart");
        CrosshairOptions invalid = new CrosshairOptions { Size = int.MinValue, LineWidth = 1000, Gap = -1, DotSize = 0, OutlineWidth = 0, Opacity = 200,
            OffsetX = int.MaxValue, OffsetY = int.MinValue, Color = "bad", OutlineColor = null, MonitorDevice = null, Style = (CrosshairStyle)500 };
        invalid.Normalize();
        Check(invalid.Size == 1 && invalid.LineWidth == 30 && invalid.Gap == 0 && invalid.DotSize == 1 && invalid.OutlineWidth == 1 && invalid.Opacity == 100, "imported ranges are bounded");
        Check(invalid.Color == "#00FF65" && invalid.OutlineColor == "#000000" && invalid.MonitorDevice == "" && invalid.Style == CrosshairStyle.CrossDot, "invalid imported colors and style use defaults");
    }
    private static void Geometry()
    {
        CrosshairOptions o = new CrosshairOptions();
        Check(CrosshairRenderer.Center(new Rectangle(0, 0, 1920, 1080), o) == new Point(960, 540), "full display center includes taskbar area");
        o.OffsetX = -9; o.OffsetY = 7;
        Check(CrosshairRenderer.Center(new Rectangle(-2560, -100, 2560, 1440), o) == new Point(-1289, 627), "negative-origin secondary monitor offsets are exact pixels");
        o.OffsetX = 16384; o.OffsetY = -16384;
        Check(CrosshairRenderer.Center(new Rectangle(-1000, 200, 999, 801), o) == new Point(-2, 200), "unreachable positions clamp to nearest connected screen edge");
        foreach (CrosshairStyle style in Enum.GetValues(typeof(CrosshairStyle)))
        foreach (bool outlined in new[] { false, true })
        {
            o = new CrosshairOptions { Style = style, Outline = outlined, Color = "#FF0000", OutlineColor = "#0000FF", Size = 32, Gap = 10 };
            using (Bitmap bitmap = CrosshairRenderer.Render(o))
            {
                int red = 0, blue = 0, transparent = 0;
                for (int y = 0; y < bitmap.Height; y++) for (int x = 0; x < bitmap.Width; x++)
                { Color c = bitmap.GetPixel(x, y); if (c.A == 0) transparent++; else { if (c.R > c.B) red++; if (c.B > c.R) blue++; } }
                Check(red > 0 && transparent > bitmap.Width, "every style has colored pixels and transparent surroundings: " + style);
                Check(outlined ? blue > 0 : blue == 0, "outline enable/color affects rendered output: " + style);
                Check(bitmap.GetPixel(0, 0).A == 0 && bitmap.GetPixel(bitmap.Width - 1, bitmap.Height - 1).A == 0, "drawing stays inside alpha canvas");
                bool hasCenter = style == CrosshairStyle.Dot || style == CrosshairStyle.Square || style == CrosshairStyle.CrossDot || style == CrosshairStyle.RingDot;
                Check((bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2).A > 0) == hasCenter, "center gap/dot geometry: " + style);
                bitmap.Save(Path.Combine(root, "style-" + style + "-" + outlined + ".png"));
            }
        }
        foreach (CrosshairStyle style in Enum.GetValues(typeof(CrosshairStyle)))
        foreach (int size in new[] { 1, 200 })
        using (Bitmap bitmap = CrosshairRenderer.Render(new CrosshairOptions { Style = style, Size = size, LineWidth = 30, Gap = 100, DotSize = 50, OutlineWidth = 12 }))
        {
            Check(bitmap.GetPixel(0, 0).A == 0 && bitmap.Width <= 233, "extreme valid values stay within the alpha canvas: " + style);
            if (style == CrosshairStyle.RingDot) Check(bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2).A > 0, "large center dot remains filled when it overlaps the ring");
        }
    }
    private static void Dialogs()
    {
        using (CrosshairSettingsDialog dialog = new CrosshairSettingsDialog(new CrosshairOptions { Enabled = true }, null, o => o.Enabled ? "Conflict example" : ""))
        {
            Check(!((Button)dialog.AcceptButton).Enabled && Field<Label>(dialog, "feedback").Text == "Conflict example", "shortcut conflicts prevent Save and explain correction inline");
            Field<CheckBox>(dialog, "enabled").Checked = false;
            Check(((Button)dialog.AcceptButton).Enabled, "disabling crosshair resolves shortcut occupation");
            Field<CrosshairOptions>(dialog, "draft").Hotkey = new TriggerSpec { VirtualKey = (int)Keys.LControlKey };
            Call(dialog, "Changed");
            Check(!((Button)dialog.AcceptButton).Enabled, "modifier-only shortcut is explained instead of silently cleared on Save");
        }
        foreach (string language in new[] { "zh-CN", "en-US" })
        foreach (float scale in new[] { 1F, 1.25F, 1.5F, 2F })
        {
            Localizer.SetLanguage(language);
            CrosshairOptions source = Example();
            using (CrosshairSettingsDialog dialog = new CrosshairSettingsDialog(source, null, null))
            {
                dialog.PerformAutoScale();
                float dpiScale;
                using (Graphics g = dialog.CreateGraphics()) dpiScale = g.DpiX / 96F;
                if (scale == 1) Render(dialog, "settings-full-" + language);
                dialog.Scale(new SizeF(scale, scale)); dialog.ClientSize = new Size((int)(820 * dpiScale * scale), (int)(620 * dpiScale * scale));
                TabControl tabs = All(dialog).OfType<TabControl>().Single();
                for (int tab = 0; tab < 2; tab++)
                {
                    tabs.SelectedIndex = tab; Render(dialog, "settings-" + language + "-" + tab + "-" + (int)(scale * 100));
                    Button save = (Button)dialog.AcceptButton; Rectangle bounds = dialog.RectangleToClient(save.RectangleToScreen(save.ClientRectangle));
                    Check(bounds.Bottom <= dialog.ClientSize.Height && bounds.Right <= dialog.ClientSize.Width && bounds.Top > 0, "larger-font footer reachable at " + language + " / " + scale);
                }
                Field<NumericUpDown>(dialog, "x").Value = 17; Field<NumericUpDown>(dialog, "opacity").Value = 38;
                Check(source.OffsetX == -12 && source.Opacity == 67, "draft edits never alter source before Save");
                All(dialog).OfType<Button>().Single(b => b.Name == "crosshairCenter").PerformClick();
                Check(Field<NumericUpDown>(dialog, "x").Value == 0 && Field<NumericUpDown>(dialog, "y").Value == 0, "center action resets both offsets");
                ((Button)dialog.AcceptButton).PerformClick();
                Check(dialog.Result.Opacity == 38 && dialog.Result.OffsetX == 0 && dialog.Result.OffsetY == 0 && !dialog.Result.Visible, "Save captures staged values without overwriting visibility");
            }
        }
    }
    private static void Workflow()
    {
        Localizer.SetLanguage("zh-CN");
        MacroConfig config = new MacroConfig { Crosshair = new CrosshairOptions(), GamepadDeviceType = VirtualGamepadTypes.None };
        string directory = Path.Combine(root, "workflow-" + Guid.NewGuid().ToString("N"));
        using (MainForm main = new MainForm(config, directory))
        {
            Render(main, "main-entry");
            foreach (bool save in new[] { false, true })
            using (System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer())
            {
                bool handled = false;
                timer.Interval = 40;
                timer.Tick += delegate
                {
                    CrosshairSettingsDialog dialog = Application.OpenForms.OfType<CrosshairSettingsDialog>().FirstOrDefault();
                    if (dialog == null) return;
                    timer.Stop(); handled = true;
                    Check(Field<int>(main, "uiSafetyModalDepth") > 0, "actual crosshair modal preserves macro editor safety");
                    Field<NumericUpDown>(dialog, "size").Value = 47; Field<NumericUpDown>(dialog, "x").Value = 9;
                    // Keep overlay disabled here; actual native overlay behavior has a separate target window.
                    if (save) ((Button)dialog.AcceptButton).PerformClick(); else { dialog.DialogResult = DialogResult.Cancel; dialog.Close(); }
                };
                timer.Start(); Field<Button>(main, "crosshairButton").PerformClick();
                Check(handled && Field<int>(main, "uiSafetyModalDepth") == 0, "actual main action opens and unwinds crosshair editor");
                Check(config.Crosshair.Size == (save ? 47 : 24), "cancel restores / Save commits crosshair options");
            }
            Check(ConfigPackageSerializer.DeserializeConfigFromFile(Path.Combine(directory, "config.xml")).Crosshair.OffsetX == 9, "actual Save writes isolated restart configuration");
            config.Crosshair.Enabled = true;
            Call(main, "ApplyProfileConfig", new MacroConfig { Crosshair = Example() }, "test-profile", false);
            Check(Field<MacroConfig>(main, "config").Crosshair.Size == 47, "loading macro profiles preserves global crosshair settings");
            MacroConfig current = Field<MacroConfig>(main, "config");
            InputEventInfo key = new InputEventInfo { Input = new InputSpec { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F10 }, Ctrl = true, Alt = true };
            Check((bool)Call(main, "HandleTerminalInput", key), "physical hotkey consumes its terminal key"); Pump(30);
            Check(!current.Crosshair.Visible, "shortcut hides overlay");
            Check(!ConfigPackageSerializer.DeserializeConfigFromFile(Path.Combine(directory, "config.xml")).Crosshair.Visible, "shortcut visibility is saved for restart");
            Call(main, "HandleTerminalInput", key); Pump(30); Check(current.Crosshair.Visible, "same shortcut shows overlay again");
            current.PanicTrigger = current.Crosshair.Hotkey.Clone();
            Call(main, "HandleTerminalInput", key); Pump(30); Check(current.Crosshair.Visible, "Emergency Stop takes priority over identical crosshair hotkey");
            Check(((string)Call(main, "CrosshairShortcutConflict", current.Crosshair)).Length > 0, "shortcut conflict is explained before saving");
            current.PanicTrigger = new MacroConfig().PanicTrigger;
            using (System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer())
            {
                timer.Interval = 40;
                timer.Tick += delegate
                {
                    CrosshairSettingsDialog dialog = Application.OpenForms.OfType<CrosshairSettingsDialog>().FirstOrDefault();
                    if (dialog == null) return;
                    timer.Stop(); Field<NumericUpDown>(dialog, "x").Value = 37;
                    Point center = CrosshairRenderer.Center(Screen.PrimaryScreen.Bounds, new CrosshairOptions { OffsetX = 37 });
                    CrosshairController controller = Field<CrosshairController>(main, "crosshairController");
                    Check(controller.Window.Left + controller.Window.Width / 2 == center.X, "actual editor live preview immediately changes screen placement");
                    dialog.DialogResult = DialogResult.Cancel; dialog.Close();
                };
                timer.Start(); Field<Button>(main, "crosshairButton").PerformClick();
            }
            CrosshairController restored = Field<CrosshairController>(main, "crosshairController");
            Check(restored.Window.Left + restored.Window.Width / 2 == CrosshairRenderer.Center(Screen.PrimaryScreen.Bounds, current.Crosshair).X && current.Crosshair.OffsetX == 9,
                "Cancel restores the real screen overlay as well as saved settings");
            Field<CrosshairController>(main, "crosshairController").Apply(new CrosshairOptions());
        }
    }
    private static void NativeOverlay()
    {
        NativeOverlay(false);
        NativeOverlay(true);
    }
    private static void NativeOverlay(bool borderless)
    {
        IntPtr foreground = GetForegroundWindow();
        Rectangle display = Screen.PrimaryScreen.Bounds;
        using (Form target = new Form { Text = "InputStitch crosshair acceptance target", BackColor = Color.White, TopMost = true, StartPosition = FormStartPosition.Manual,
            FormBorderStyle = borderless ? FormBorderStyle.None : FormBorderStyle.Sizable,
            Bounds = borderless ? display : new Rectangle(display.Left + display.Width / 2 - 250, display.Top + display.Height / 2 - 150, 500, 300) })
        using (CrosshairController controller = new CrosshairController(target))
        {
            try
            {
                target.Show(); SetForegroundWindow(target.Handle); Pump(70);
                IntPtr before = GetForegroundWindow();
                CrosshairOptions options = new CrosshairOptions { Enabled = true, Style = CrosshairStyle.Square, Size = 20, Outline = false, Color = "#FF0000" };
                controller.Apply(options); Pump(500);
                Check(GetForegroundWindow() == before, "showing overlay does not take foreground focus");
                int flags = GetWindowLong(controller.Window.Handle, -20);
                Check((flags & (0x80000 | 0x20 | 0x80 | 0x08000000)) == (0x80000 | 0x20 | 0x80 | 0x08000000), "actual layered, click-through, tool-window, no-activation flags");
                Point center = CrosshairRenderer.Center(Screen.PrimaryScreen.Bounds, options);
                Check(WindowFromPoint(center) == target.Handle, "native point hit-testing passes through colored crosshair pixels to the target: hit=" + WindowFromPoint(center)
                    + "; target=" + target.Handle + "; overlay=" + controller.Window.Handle + "; targetBounds=" + target.Bounds + "; overlayBounds=" + controller.Window.Bounds);
                Check(controller.Window.Left + controller.Window.Width / 2 == center.X && controller.Window.Top + controller.Window.Height / 2 == center.Y, "actual native window is centered in screen pixels");
                {
                    Color solid = Pixel(center);
                    using (Bitmap desktop = Desktop(new Rectangle(center.X - 80, center.Y - 80, 160, 160))) desktop.Save(Path.Combine(root, borderless ? "native-overlay-borderless.png" : "native-overlay.png"));
                    Check(solid.R > 230 && solid.G < 30 && solid.B < 30, "desktop compositor displays selected red fill: " + solid + "; window=" + controller.Window.Bounds
                        + "; visible=" + controller.Visible + "; flags=" + flags.ToString("X") + "; prev=" + GetWindow(controller.Window.Handle, 3) + "; target=" + target.Handle);
                    options.Opacity = 50; controller.Apply(options); Pump(70); Color half = Pixel(center);
                    Check(half.R > 230 && half.G > 100 && half.G < 160 && half.B > 100 && half.B < 160, "per-pixel alpha and global opacity composite correctly");
                }
                options.OffsetX = 13; options.OffsetY = -11; controller.Apply(options);
                Pump(30);
                Check(controller.Window.Left + controller.Window.Width / 2 == center.X + 13 && controller.Window.Top + controller.Window.Height / 2 == center.Y - 11,
                    "native one-pixel placement uses exact offsets: " + controller.Window.Bounds + "; center=" + center);
                options.Visible = false; controller.Apply(options); Check(!controller.Visible, "hide keeps configuration without a visible overlay");
                options.Visible = true; options.MonitorDevice = "UNPLUGGED-DISPLAY"; controller.Apply(options);
                Check(controller.Visible && controller.Window.Left + controller.Window.Width / 2 == center.X + 13, "disconnected monitor falls back to primary");
                options.Enabled = false; controller.Apply(options); Check(!controller.Visible, "disabled setting removes overlay");
            }
            finally { target.Close(); if (foreground != IntPtr.Zero) SetForegroundWindow(foreground); }
        }
    }
    private static void Soak(int minutes)
    {
        // Actual layered windows, transparent bitmap/GDI resources, repeated changes and display placement.
        Stopwatch elapsed = Stopwatch.StartNew(); int cycles = 0; long maxDispatch = 0; int sampleSecond = -1;
        using (Form dispatcher = new Form())
        using (CrosshairController controller = new CrosshairController(dispatcher))
        using (Process process = Process.GetCurrentProcess())
        {
            IntPtr handle = dispatcher.Handle;
            CrosshairOptions options = new CrosshairOptions { Enabled = true, Opacity = 50, OffsetX = -16384, OffsetY = -16384 };
            controller.Apply(options); Pump(50); process.Refresh();
            long baselineBytes = process.PrivateMemorySize64; int baselineHandles = process.HandleCount, baselineGdi = GetGuiResources(process.Handle, 0);
            int baselineUser = GetGuiResources(process.Handle, 1); bool warmed = false;
            string samples = Path.Combine(root, "soak-samples.csv"); File.WriteAllText(samples, "seconds,handles,gdi,user,privateBytes\r\n");
            while (elapsed.Elapsed.TotalMinutes < minutes)
            {
                options.Style = (CrosshairStyle)(cycles % 8); options.Visible = cycles % 11 != 0;
                options.Opacity = cycles % 101;
                Stopwatch tick = Stopwatch.StartNew(); controller.Apply(options); Application.DoEvents(); maxDispatch = Math.Max(maxDispatch, tick.ElapsedMilliseconds);
                cycles++; Pump(20);
                int second = (int)elapsed.Elapsed.TotalSeconds;
                if (second / 10 > sampleSecond / 10 || sampleSecond < 0)
                {
                    sampleSecond = second; process.Refresh();
                    int handles = process.HandleCount, gdi = GetGuiResources(process.Handle, 0), user = GetGuiResources(process.Handle, 1);
                    long bytes = process.PrivateMemorySize64;
                    if (!warmed && second >= 30) { warmed = true; baselineBytes = bytes; baselineHandles = handles; baselineGdi = gdi; baselineUser = user; }
                    File.AppendAllText(samples, second + "," + handles + "," + gdi + "," + user + "," + bytes + "\r\n");
                    Console.WriteLine("RESOURCE " + second + "s handles=" + handles + " gdi=" + gdi + " user=" + user + " bytes=" + bytes);
                }
            }
            CrosshairOverlay closingWindow = controller.Window;
            controller.Apply(new CrosshairOptions()); controller.Dispose(); Pump(50); process.Refresh();
            int finalGdi = GetGuiResources(process.Handle, 0), finalHandles = process.HandleCount; long finalBytes = process.PrivateMemorySize64;
            int finalUser = GetGuiResources(process.Handle, 1);
            bool passed = warmed && finalGdi <= baselineGdi + 10 && finalHandles <= baselineHandles + 25 && finalUser <= baselineUser + 5
                && closingWindow.Handle == IntPtr.Zero && finalBytes < baselineBytes + 32L * 1024 * 1024;
            string report = "{\"status\":\"" + (passed ? "PASS" : "FAIL") + "\",\"version\":\"" + ReleaseInfo.Version + "\",\"backend\":\"actual layered window\",\"durationSeconds\":" + elapsed.Elapsed.TotalSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
                + ",\"cycles\":" + cycles + ",\"baselineGdi\":" + baselineGdi + ",\"finalGdi\":" + finalGdi + ",\"baselineHandles\":" + baselineHandles + ",\"finalHandles\":" + finalHandles
                + ",\"warmupSeconds\":30,\"baselineUserObjects\":" + baselineUser + ",\"finalUserObjects\":" + finalUser
                + ",\"baselinePrivateBytes\":" + baselineBytes + ",\"finalPrivateBytes\":" + finalBytes + ",\"maxDispatchMs\":" + maxDispatch + "}";
            File.WriteAllText(Path.Combine(root, "soak-report.json"), report); Console.WriteLine(report);
            Check(finalGdi <= baselineGdi + 10, "sustained redraws do not leak GDI objects");
            Check(finalHandles <= baselineHandles + 25, "sustained overlay does not leak process handles: " + baselineHandles + " to " + finalHandles);
            Check(warmed && finalUser <= baselineUser + 5 && closingWindow.Handle == IntPtr.Zero, "sustained overlay releases its native window and USER resources");
            Check(finalBytes < baselineBytes + 32L * 1024 * 1024, "sustained overlay private-memory growth bounded");
        }
    }
}
