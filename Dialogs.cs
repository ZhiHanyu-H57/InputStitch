using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace InputStitch
{
    // Extracted from InputStitch.cs during the incremental MainForm decomposition (architecture-review
    // finding F4); members moved verbatim, behaviour is unchanged.
    internal sealed class MacroExportSelectionForm : Form
    {
        private CheckedListBox list;
        public List<int> SelectedIndices = new List<int>();

        public MacroExportSelectionForm(List<MacroDefinition> macros, int currentIndex)
        {
            Text = EditorDialogs.T("选择要导出的宏", "Choose macros to export");
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            ClientSize = new Size(560, 580);
            MinimumSize = new Size(500, 480);
            Font = new Font("Microsoft YaHei UI", 10F);
            MaximizeBox = MinimizeBox = false;
            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            list = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false, BorderStyle = BorderStyle.None };
            if (macros != null) for (int i = 0; i < macros.Count; i++) list.Items.Add(macros[i] == null ? Localizer.T("（空宏）") : macros[i].Name, i == currentIndex);
            Button all = EditorDialogs.Button(EditorDialogs.T("全选", "Select all"), DialogResult.None);
            Button none = EditorDialogs.Button(EditorDialogs.T("全不选", "Clear selection"), DialogResult.None);
            all.Click += delegate { for (int i = 0; i < list.Items.Count; i++) list.SetItemChecked(i, true); };
            none.Click += delegate { for (int i = 0; i < list.Items.Count; i++) list.SetItemChecked(i, false); };
            FlowLayoutPanel utility = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            utility.Controls.AddRange(new Control[] { all, none });
            body.Controls.Add(utility, 0, 0);
            body.Controls.Add(list, 0, 1);
            Button ok = EditorDialogs.Button(EditorDialogs.T("导出所选宏", "Export selected"), DialogResult.None);
            ok.Enabled = list.CheckedItems.Count > 0;
            list.ItemCheck += delegate(object sender, ItemCheckEventArgs e)
            { ok.Enabled = list.CheckedItems.Count + (e.NewValue == CheckState.Checked ? 1 : 0) - (e.CurrentValue == CheckState.Checked ? 1 : 0) > 0; };
            ok.Click += delegate
            {
                SelectedIndices.Clear();
                for (int i = 0; i < list.Items.Count; i++) if (list.GetItemChecked(i)) SelectedIndices.Add(i);
                if (SelectedIndices.Count == 0) return;
                DialogResult = DialogResult.OK;
                Close();
            };
            Button cancel = EditorDialogs.Button(Localizer.T("取消"), DialogResult.Cancel);
            AcceptButton = ok; CancelButton = cancel;
            EditorDialogs.Frame(this, body, EditorDialogs.Actions(ok, cancel), EditorDialogs.T("勾选要分享或备份的宏。导出的宏包保留宏设置，不会改变程序全局设置。", "Select macros to share or back up. The package keeps their settings without changing the application's global setup."), null);
        }
    }

    internal sealed class BatchDelayDialog : Form
    {
        private RadioButton fixedMode;
        private RadioButton randomMode;
        private NumericUpDown fixedBox;
        private NumericUpDown minBox;
        private NumericUpDown maxBox;
        public bool UseRandom;
        public int FixedMs;
        public int MinMs;
        public int MaxMs;

        private Button applyButton;
        private Label rangeHint;

        public BatchDelayDialog()
        {
            Text = EditorDialogs.T("批量设置步骤间隔", "Set step wait times");
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            ClientSize = new Size(600, 390);
            MinimumSize = new Size(580, 410);
            MaximizeBox = MinimizeBox = false;
            Font = new Font("Microsoft YaHei UI", 10F);
            TableLayoutPanel body = new TableLayoutPanel { AutoScroll = true, Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16), BackColor = Color.White };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fixedMode = new RadioButton { Text = Localizer.T("固定间隔"), AutoSize = true, Checked = true };
            randomMode = new RadioButton { Text = Localizer.T("随机间隔"), AutoSize = true };
            fixedBox = MakeNumber(0, 0, 50);
            minBox = MakeNumber(0, 0, 45);
            maxBox = MakeNumber(0, 0, 60);
            FlowLayoutPanel fixedRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            fixedRow.Controls.Add(fixedBox); fixedRow.Controls.Add(MakeLabel("ms", 0, 0));
            FlowLayoutPanel randomRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            randomRow.Controls.AddRange(new Control[] { minBox, MakeLabel("–", 0, 0), maxBox, MakeLabel("ms", 0, 0) });
            body.Controls.Add(fixedMode, 0, 0); body.Controls.Add(fixedRow, 1, 0);
            body.Controls.Add(randomMode, 0, 1); body.Controls.Add(randomRow, 1, 1);
            rangeHint = new Label { AutoSize = true, Dock = DockStyle.Top, ForeColor = EditorTheme.Muted, Margin = new Padding(0, 16, 0, 0), MaximumSize = new Size(530, 0) };
            body.Controls.Add(rangeHint, 0, 2); body.SetColumnSpan(rangeHint, 2);
            Button ok = EditorDialogs.Button(EditorDialogs.T("应用到所选步骤", "Apply to selection"), DialogResult.None);
            applyButton = ok;
            ok.Click += delegate
            {
                if (randomMode.Checked && minBox.Value > maxBox.Value) return;
                UseRandom = randomMode.Checked; FixedMs = (int)fixedBox.Value; MinMs = (int)minBox.Value; MaxMs = (int)maxBox.Value;
                DialogResult = DialogResult.OK; Close();
            };
            Button cancel = EditorDialogs.Button(Localizer.T("取消"), DialogResult.Cancel);
            AcceptButton = ok; CancelButton = cancel;
            fixedMode.CheckedChanged += delegate { UpdateEnabled(); };
            randomMode.CheckedChanged += delegate { UpdateEnabled(); };
            minBox.ValueChanged += delegate { UpdateEnabled(); };
            maxBox.ValueChanged += delegate { UpdateEnabled(); };
            EditorDialogs.Frame(this, body, EditorDialogs.Actions(ok, cancel), EditorDialogs.T("设置每个操作完成后，多久再执行下一步。仅修改选中的步骤，不改变按住时长。", "Choose how long to wait after each action. Only selected steps change; hold durations stay the same."), null);
            UpdateEnabled();
        }

        private NumericUpDown MakeNumber(int x, int y, int value)
        {
            NumericUpDown n = new NumericUpDown();
            n.Location = new Point(x, y);
            n.Size = new Size(110, 26);
            n.Minimum = 0;
            n.Maximum = 600000;
            n.Value = value;
            return n;
        }

        private Label MakeLabel(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Location = new Point(x, y);
            l.AutoSize = true;
            return l;
        }

        private void UpdateEnabled()
        {
            fixedBox.Enabled = fixedMode.Checked;
            minBox.Enabled = randomMode.Checked;
            maxBox.Enabled = randomMode.Checked;
            bool valid = !randomMode.Checked || minBox.Value <= maxBox.Value;
            if (applyButton != null) applyButton.Enabled = valid;
            if (rangeHint != null) rangeHint.Text = valid
                ? EditorDialogs.T("1000 毫秒 = 1 秒。随机间隔会在最小值和最大值之间选择等待时间。", "1000 ms = 1 second. A random wait is chosen between your minimum and maximum.")
                : EditorDialogs.T("最小间隔不能大于最大间隔，请调整后再应用。", "The minimum wait must not exceed the maximum. Adjust the range to apply.");
        }
    }

    internal sealed class DiagnosticsForm : Form
    {
        private TextBox text;
        private Func<string> provider;
        private System.Windows.Forms.Timer timer;

        public DiagnosticsForm(Func<string> diagnosticsProvider)
        {
            provider = diagnosticsProvider;
            Text = AppInfo.ProductName + " · " + EditorDialogs.T("诊断信息", "Diagnostics");
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(650, 520);
            MinimumSize = new Size(520, 380);
            Font = new Font("Microsoft YaHei UI", 10F);

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            text = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font("Consolas", 9F), BorderStyle = BorderStyle.FixedSingle };
            Button copy = EditorDialogs.Button(Localizer.T("复制"), DialogResult.None);
            copy.Click += delegate { try { Clipboard.SetText(text.Text ?? ""); } catch { } };
            Button close = EditorDialogs.Button(Localizer.T("关闭"), DialogResult.Cancel);
            close.Click += delegate { Close(); };
            CancelButton = close;
            EditorDialogs.Frame(this, text, EditorDialogs.Actions(close, copy), EditorDialogs.T("查看运行状态。需要排查问题时，可复制这些信息。", "View runtime details. Copy this information when diagnosing a problem."), null);
            Localizer.ApplyStaticControls(this);
            RefreshText();
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 500;
            timer.Tick += delegate { RefreshText(); };
            timer.Start();
        }

        private void RefreshText()
        {
            try
            {
                int sel = text.SelectionStart;
                string value = Localizer.Dynamic(provider == null ? "" : provider());
                if (text.Text != value) text.Text = value;
                if (sel <= text.TextLength) text.SelectionStart = sel;
            }
            catch { }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (timer != null) { timer.Stop(); timer.Dispose(); timer = null; }
            base.OnFormClosed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && timer != null) { timer.Stop(); timer.Dispose(); timer = null; }
            base.Dispose(disposing);
        }
    }

    internal sealed class AboutForm : Form
    {
        public AboutForm()
        {
            Text = EditorDialogs.T("关于 ", "About ") + AppInfo.ProductName;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 310);
            Font = new Font("Microsoft YaHei UI", 10F);

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            ClientSize = new Size(620, 380);
            Label body = new Label { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = Color.White, Text = EditorDialogs.T("设置宏，让键盘、鼠标或虚拟手柄自动完成重复操作。\r\n\r\n配置目录：\r\n", "Set up macros to automate repeated keyboard, mouse or virtual controller actions.\r\n\r\nConfiguration folder:\r\n") + AppPaths.Root };
            Button open = EditorDialogs.Button(EditorDialogs.T("打开配置目录", "Open config folder"), DialogResult.None);
            open.Click += delegate { try { Process.Start("explorer.exe", AppPaths.Root); } catch { } };
            Button close = EditorDialogs.Button(Localizer.T("关闭"), DialogResult.Cancel);
            close.Click += delegate { Close(); };
            CancelButton = close;
            EditorDialogs.Frame(this, body, EditorDialogs.Actions(close, open), AppInfo.ProductName + " " + AppInfo.Version, null);
            Localizer.ApplyStaticControls(this);
        }
    }
}
