using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using InputStitch;

internal static class SecondaryLayoutTests
{
    private static readonly List<string> failures = new List<string>();
    private static int checks, windows;
    private static bool native;
    private static string root, context;
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr parameter, IntPtr value);
    private static void Check(bool value, string message)
    { checks++; if (!value) failures.Add(context + " | " + message); }
    private static IEnumerable<Control> All(Control rootControl)
    { foreach (Control child in rootControl.Controls) { yield return child; foreach (Control nested in All(child)) yield return nested; } }
    private static T Field<T>(object target, string name) { return (T)target.GetType().GetField(name, Private).GetValue(target); }
    private static void Pump(int ms)
    { Stopwatch time = Stopwatch.StartNew(); while (time.ElapsedMilliseconds < ms) { Application.DoEvents(); Thread.Sleep(1); } }
    private static DeviceInventorySnapshot Inventory()
    {
        DeviceInventorySnapshot snapshot = new DeviceInventorySnapshot { StableMetadataAvailable = true, RegistryWritable = true,
            DiscoveryStatus = "12 connected controllers", RegistryStatus = "Ready" };
        for (int i = 0; i < 24; i++) snapshot.Items.Add(new DeviceInventoryItem { DeviceKey = "device-" + i.ToString("D2"),
            DisplayName = "Controller / 手柄 " + (i+1), Present = i < 12, Persistent = true,
            Role = DeviceInventoryRole.PersistentGamingDevice, IdentityBasis = "ContainerId", Provider = "Test", Vid = "1234", Pid = "5678",
            XInputSlot = i < 4 ? i : -1, FirstSeenUtc = DateTime.UtcNow, LastSeenUtc = DateTime.UtcNow });
        return snapshot;
    }
    private static IEnumerable<Func<Form>> Forms()
    {
        yield return () => new SettingsDialog(new MacroConfig());
        foreach (string mode in new[] { ActivatorModes.Legacy, ActivatorModes.ShortPress, ActivatorModes.LongPress, ActivatorModes.DoublePress })
        { string selected = mode; yield return () => new ActivatorConditionDialog(new MacroDefinition { Activator = new ActivatorConfig { Mode = selected } }, Inventory, "Base / 基础"); }
        yield return () => new StepEditDialog(null, new MacroStep { Kind = InputKind.Keyboard, VirtualKey = (int)Keys.F8, HoldMs = 30, DelayMs = 100 });
        yield return () => new StepEditDialog(null, new MacroStep { Kind = InputKind.Gamepad, GamepadControl = GamepadControl.LeftStick, GamepadX = 50, GamepadY = 50 });
        yield return () => new VirtualKeyboardDialog(true, null);
        yield return () => new VirtualKeyboardDialog(false, null);
        yield return () => new BatchDelayDialog();
        yield return () => new MacroExportSelectionForm(Enumerable.Range(0,24).Select(i => new MacroDefinition { Name = "Example macro / 示例宏 " + (i+1) }).ToList(),0);
        yield return () => new MacroExportSelectionForm(new List<MacroDefinition> { new MacroDefinition { Name = "One macro / 一个宏" } },0);
        yield return () => new RouterSourceDialog(new RouterSourcePolicyConfig(), Inventory);
        yield return () => new AnalogTransformDialog(new AnalogTransformProfile());
        yield return () => new DeviceManagerDialog(Inventory);
        yield return () => new ControlledReplacementDialog(null, () => -1, () => false, () => VirtualGamepadTypes.None, () => false, () => false, null, null);
        yield return () => new AboutForm();
        yield return () => new DiagnosticsForm(() => string.Join(Environment.NewLine, Enumerable.Range(0,40).Select(i => "Runtime / 运行状态 " + i + ": Ready")));
        yield return () => new CrosshairSettingsDialog(new CrosshairOptions(), null, null);
        yield return () => new NewMacroDialog();
        foreach (QuickTemplate template in Enum.GetValues(typeof(QuickTemplate)))
        { QuickTemplate selected = template; yield return () => new QuickCreateDialog(null,selected); }
    }
    private static void VisibleBounds(Control control, Form form)
    {
        bool scrollY = false, scrollX = false;
        for (Control parent = control.Parent; parent != null; parent = parent.Parent)
        {
            ScrollableControl viewport = parent as ScrollableControl;
            if (viewport != null && viewport.AutoScroll)
            { scrollY |= viewport.VerticalScroll.Visible; scrollX |= viewport.HorizontalScroll.Visible; }
            Rectangle visible = parent.RectangleToClient(control.RectangleToScreen(control.ClientRectangle));
            Check((scrollX || visible.Left >= 0 && visible.Right <= parent.ClientSize.Width + 1) &&
                (scrollY || visible.Top >= 0 && visible.Bottom <= parent.ClientSize.Height + 1),
                "clipped " + control.GetType().Name + " '" + control.Text.Replace("\r\n", " ") + "' by " + parent.GetType().Name + ": " + visible + " / " + parent.ClientSize);
            if (parent == form) break;
        }
    }
    private static void Audit(Form form)
    {
        if (form is VirtualKeyboardDialog && !context.EndsWith(" compact",StringComparison.Ordinal) && (native || context.EndsWith("-100",StringComparison.Ordinal)))
            foreach (ScrollableControl viewport in All(form).OfType<ScrollableControl>().Where(x => x.AutoScroll))
                Check(!viewport.HorizontalScroll.Visible && !viewport.VerticalScroll.Visible,"default keyboard is fully visible without scrolling");
        foreach (EditorIntro intro in All(form).OfType<EditorIntro>().Where(x => x.Visible))
            Check(intro.Height >= intro.GetPreferredSize(new Size(intro.Width,0)).Height, "intro description fits: " + intro.Size);
        foreach (TabControl tabs in All(form).OfType<TabControl>().Where(x => x.Visible))
            for (int i=0; i<tabs.TabCount; i++)
            {
                Rectangle tab = tabs.GetTabRect(i);
                Check(tab.Width >= TextRenderer.MeasureText(tabs.TabPages[i].Text, tabs.Font).Width + 6 && tab.Height >= tabs.Font.Height + 4,
                    "tab text fits: " + tabs.TabPages[i].Text + " " + tab.Size);
            }
        foreach (Control control in All(form).Where(x => x.Visible && (x is Label || x is Button || x is ComboBox || x is NumericUpDown || x is CheckBox || x is RadioButton || x is ListBox || x is DataGridView)))
        {
            VisibleBounds(control,form);
            Button button = control as Button;
            if (button != null && !string.IsNullOrEmpty(button.Text))
            {
                EditorButton editor = button as EditorButton;
                bool compact = editor != null && editor.CompactText;
                Size text = TextRenderer.MeasureText(button.Text, button.Font, Size.Empty, TextFormatFlags.NoPrefix | (compact ? TextFormatFlags.NoPadding : 0));
                Check(text.Width <= button.ClientSize.Width - (compact ? 2 : 10) && text.Height <= button.ClientSize.Height - 4,
                    "button text fits: " + button.Text + " text=" + text + " button=" + button.ClientSize);
            }
            Label label = control as Label;
            if (label != null && label.Text.Length > 0 && !label.AutoEllipsis)
            {
                Size text = label.GetPreferredSize(new Size(label.Width,0));
                Check(text.Height <= label.ClientSize.Height + 1, "label text fits: " + label.Text.Replace("\r\n"," ") + " preferred=" + text + " label=" + label.ClientSize);
            }
            ListBox list = control as ListBox;
            ComboBox choice = control as ComboBox;
            if (choice != null && choice.DropDownStyle == ComboBoxStyle.DropDownList && choice.SelectedIndex >= 0)
                Check(TextRenderer.MeasureText(choice.Text,choice.Font).Width + SystemInformation.VerticalScrollBarWidth + 6 <= choice.ClientSize.Width,
                    "selected option fits: " + choice.Text + " " + choice.ClientSize);
            NumericUpDown number = control as NumericUpDown;
            if (number != null)
                Check(TextRenderer.MeasureText(number.Value.ToString(),number.Font).Width + SystemInformation.VerticalScrollBarWidth + 4 <= number.ClientSize.Width,
                    "numeric value and spinner fit: " + number.Value + " " + number.ClientSize);
            if (list != null && list.Items.Count > 0)
            {
                int height = list.GetItemRectangle(0).Height;
                Check(height >= list.Font.Height + 6, "list row has readable spacing: " + height + " font=" + list.Font.Height);
                if (list.Items.Count >= 20)
                {
                    int rows = context.EndsWith(" compact",StringComparison.Ordinal) ? 3 : 5;
                    Check(list.ClientSize.Height / height >= rows, "list retains useful visible capacity: " + list.ClientSize.Height / height + " rows");
                }
            }
            DataGridView grid = control as DataGridView;
            if (grid != null)
            {
                Check(grid.Rows.Cast<DataGridViewRow>().All(x => x.Height >= grid.Font.Height + grid.DefaultCellStyle.Padding.Vertical), "grid rows fit text and padding");
                Check(!All(grid).OfType<HScrollBar>().Any(x => x.Visible),"device columns fit without horizontal scrolling");
            }
        }
        foreach (ScrollableControl viewport in All(form).OfType<ScrollableControl>().Where(x => x.Visible && x.AutoScroll))
            if (!(form is VirtualKeyboardDialog)) Check(!viewport.HorizontalScroll.Visible,"no horizontal page scrolling: " + viewport.GetType().Name);
    }
    private static void Capture(Form form,string name)
    {
        using (Bitmap bitmap = new Bitmap(form.Width,form.Height))
        { form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size)); bitmap.Save(Path.Combine(root,name+".png")); }
    }
    private static void ExerciseChoice(ComboBox choice)
    {
        if (!choice.Enabled || choice.Items.Count==0) return;
        Check(choice.ItemHeight>=choice.Font.Height+6,"opened choice rows fit the scaled font: "+choice.Text);
        int widest=choice.Items.Cast<object>().Max(x=>TextRenderer.MeasureText(choice.GetItemText(x),choice.Font).Width);

        if(choice is EditorChoice && ((EditorChoice)choice).CompactWidth && choice.DropDownStyle==ComboBoxStyle.DropDownList)
        {
            float scale=Math.Max(choice.Font.GetHeight()/choice.Font.GetHeight(96F),choice.Font.Size/EditorTheme.BodyFontSize);
            Check(choice.Width<=Math.Max(112*scale,widest+SystemInformation.VerticalScrollBarWidth+14*scale)+2,"short choice has no excessive blank width");
        }
        choice.DroppedDown=true; Pump(8);
        Check(choice.DroppedDown,"native choice actually opens");
        Check(choice.DropDownWidth>=widest+SystemInformation.VerticalScrollBarWidth+6,"expanded choice width fits every option");
        Check(SendMessage(choice.Handle,0x0154,IntPtr.Zero,IntPtr.Zero).ToInt32()>=choice.Font.Height+6,"native dropdown row spacing matches the scaled font");
        choice.DroppedDown=false;
    }

    private static void ExerciseList(CheckedListBox list)
    {
        if (list.Items.Count < 3) return;
        bool old = list.GetItemChecked(2);
        list.SelectedIndex = 2;
        SendMessage(list.Handle,0x0100,new IntPtr((int)Keys.Space),IntPtr.Zero);
        SendMessage(list.Handle,0x0102,new IntPtr((int)' '),IntPtr.Zero);
        SendMessage(list.Handle,0x0101,new IntPtr((int)Keys.Space),IntPtr.Zero);
        Check(list.GetItemChecked(2) != old,"spaced list retains native keyboard checking");
        list.SetItemChecked(2,old);
        Point center = new Point(8,list.GetItemRectangle(1).Top+list.GetItemRectangle(1).Height/2);
        bool second = list.GetItemChecked(1);
        IntPtr point = new IntPtr(center.X | (center.Y << 16));
        SendMessage(list.Handle,0x0201,new IntPtr(1),point); SendMessage(list.Handle,0x0202,IntPtr.Zero,point);
        Check(list.GetItemChecked(1) != second && list.GetItemChecked(2) == old,"spaced list click toggles the intended row only");
        list.SetItemChecked(1,second);
    }
    private static void InlineGroupName()
    {
        foreach (string language in new[] { "zh-CN","en-US" })
        using (MainForm host = new MainForm(new MacroConfig { Language=language,GamepadDeviceType=VirtualGamepadTypes.None },Path.Combine(root,"inline-"+Guid.NewGuid().ToString("N"))))
        using (System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer())
        {
            context = "inline group name " + language;
            IntPtr handle = host.Handle;
            bool seen = false;
            timer.Interval=20;
            timer.Tick += delegate
            {
                Form dialog=Application.OpenForms.Cast<Form>().FirstOrDefault(x => x.GetType()==typeof(Form) && x.Text=="Group / 分组");
                if (dialog==null) return;
                timer.Stop(); seen=true; windows++; Audit(dialog);
                Capture(dialog,"inline-group-"+language);
                dialog.DialogResult=DialogResult.Cancel; dialog.Close();
            };
            timer.Start();
            object result=typeof(MainForm).GetMethod("PromptLayerName",Private).Invoke(host,new object[] { "Group / 分组","Example / 示例" });
            Check(seen && result==null,"cancelled group-name editor preserves the existing value");
        }
    }
    [STAThread]
    private static int Main(string[] args)
    {
        native = args.Contains("--native");
        if (native) SetProcessDpiAwarenessContext(new IntPtr(-4));
        Application.EnableVisualStyles(); EmbeddedDependencyLoader.Register();
        root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"secondary-layout"+(native?"-native":args.Contains("--scaled-fonts")?"-scaled-fonts":"")); Directory.CreateDirectory(root);
        using (Graphics screen = Graphics.FromHwnd(IntPtr.Zero)) Console.WriteLine("SECONDARY DPI="+screen.DpiX+" native="+native);
        try
        {
            if (!args.Contains("--settings-only")) InlineGroupName();
            if (!args.Contains("--settings-only"))
                foreach (Func<Form> factory in new Func<Form>[] { () => new SettingsDialog(new MacroConfig()), () => new VirtualKeyboardDialog(true,null) })
                    using (Form form = factory())
                    {
                        context = "first-show working area " + form.GetType().Name;
                        form.StartPosition = FormStartPosition.CenterScreen; form.ShowInTaskbar = false;
                        form.Show(); Pump(20); windows++;
                        Check(Screen.FromControl(form).WorkingArea.Contains(form.Bounds),"default dialog remains inside the working area");
                        Audit(form); form.Close();
                    }
            foreach (string language in new[] { "zh-CN","en-US" })
            foreach (float scale in native ? new[] { 1F } : new[] { 1F,1.25F,1.5F,2F })
            {
                int index=0;
                foreach (Func<Form> factory in Forms())
                {
                    Localizer.SetLanguage(language);
                    using (Form form = factory())
                    {
                        if (args.Contains("--settings-only") && !(form is SettingsDialog)) continue;
                        string focus=args.FirstOrDefault(x=>x.StartsWith("--focus=",StringComparison.Ordinal));
                        if(focus!=null && !focus.Substring(8).Split(',').Contains(form.GetType().Name)) continue;
                        string name = (++index).ToString("D2")+"-"+form.GetType().Name+"-"+language+"-"+(int)(scale*100);
                        context = name;
                        form.StartPosition=FormStartPosition.Manual; form.Location=new Point(-20000,-20000); form.ShowInTaskbar=false;
                        Size requested = form.ClientSize;
                        if (scale != 1F)
                        {
                            form.Scale(new SizeF(scale,scale));
                            if(args.Contains("--scaled-fonts"))
                            {
                                Control[] controls=new[]{(Control)form}.Concat(All(form)).ToArray();
                                Font[] fonts=controls.Select(x=>new Font(x.Font.FontFamily,x.Font.Size*scale,x.Font.Style)).ToArray();
                                for(int n=0;n<controls.Length;n++) controls[n].Font=fonts[n];
                                form.Disposed+=delegate { foreach(Font font in fonts) font.Dispose(); };
                            }
                            requested=new Size((int)(requested.Width*scale),(int)(requested.Height*scale)); form.ClientSize=requested;
                        }
                        form.Show(); Pump(20); windows++;
                        Audit(form);
                        foreach(ComboBox choice in All(form).OfType<ComboBox>().Where(x=>x.Visible).ToArray()) ExerciseChoice(choice);
                        if (native || scale==1F) Capture(form,name+"-default");
                        foreach (CheckedListBox list in All(form).OfType<CheckedListBox>().Where(x => x.Visible && x.Enabled)) ExerciseList(list);
                        foreach (TabControl tabs in All(form).OfType<TabControl>().ToArray())
                            for (int tab=0;tab<tabs.TabPages.Count;tab++)
                            {
                                tabs.SelectedIndex=tab; Pump(5); context=name+" tab="+tab; Audit(form);
                                foreach(ComboBox choice in All(tabs.SelectedTab).OfType<ComboBox>().Where(x=>x.Visible).ToArray()) ExerciseChoice(choice);
                                if (form is SettingsDialog && (native || scale==1F))
                                {
                                    ComboBox choice=All(tabs.SelectedTab).OfType<ComboBox>().FirstOrDefault(x=>x.Visible && x.Enabled);
                                    if(choice!=null) { choice.DroppedDown=true; Pump(8); Capture(form,name+"-choices-tab"+tab); choice.DroppedDown=false; }
                                }
                                if (native || scale==1F) Capture(form,name+"-tab"+tab);
                            }
                        if (form is ActivatorConditionDialog)
                        { Field<CheckBox>(form,"analogEnabled").Checked=true; Pump(5); context=name+" analog limits"; Audit(form); }
                        // Resize the same live window down and back; controls must not retain an earlier layout's clipped size.
                        Size original=form.Size;
                        bool clamped = scale > 1F && (form.ClientSize.Width < requested.Width || form.ClientSize.Height < requested.Height);
                        if (!clamped && form.MinimumSize.Width <= original.Width && form.MinimumSize.Height <= original.Height)
                        {
                            form.Size=new Size(Math.Max(form.MinimumSize.Width,(int)(original.Width*.85)),Math.Max(form.MinimumSize.Height,(int)(original.Height*.85)));
                            Pump(12); context=name+" compact"; Audit(form);
                            if (native || scale==1F) Capture(form,name+"-compact");
                            form.Size=original; Pump(8); context=name+" restored"; Audit(form);
                        }
                        else Console.WriteLine("CLAMPED compact resize unavailable: "+name+" minimum="+form.MinimumSize+" actual="+original);
                        Console.WriteLine("WINDOW "+name+" "+form.Size+" font="+form.Font.Height);
                        form.Close();
                    }
                }
            }
        }
        catch(Exception ex) { failures.Add(context+" | "+ex); }
        File.WriteAllLines(Path.Combine(root,"issues.txt"),failures,Encoding.UTF8);
        string summary=(failures.Count==0?"PASS":"FAIL")+" secondary layouts: "+checks+" checks; "+windows+" windows; issues="+failures.Count+"; native="+native;
        File.WriteAllText(Path.Combine(root,"summary.txt"),summary,Encoding.UTF8);
        foreach(string failure in failures.Take(45)) Console.Error.WriteLine(failure);
        Console.WriteLine(summary);
        return failures.Count==0?0:1;
    }
}
