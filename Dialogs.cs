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
            ClientSize = new Size(560, Math.Min(580, Math.Max(300, 180 + (macros == null ? 0 : macros.Count) * 28)));
            MinimumSize = new Size(500, 300);
            Font = EditorTheme.BodyFont();
            MaximizeBox = MinimizeBox = false;
            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            list = new EditorCheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false, BorderStyle = BorderStyle.None, HorizontalScrollbar = true };
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

    internal sealed class BatchTimingDialog : Form
    {
        private readonly RadioButton selectedScope, allScope, fixedMode, randomMode;
        private readonly CheckBox holdChangeBox, waitChangeBox;
        private readonly NumericUpDown holdBox, fixedBox, minBox, maxBox;
        private readonly Button applyButton;
        private readonly Label rangeHint;
        private readonly int selectedCount, totalCount, selectedHoldCount, totalHoldCount;
        internal StepTimingEdit Edit { get; private set; }

        public BatchTimingDialog() : this(1, 1, 1, 1, new MacroStep()) { }

        internal BatchTimingDialog(int selected, int total, int selectedHolds, int totalHolds, MacroStep sample)
        {
            selectedCount = selected; totalCount = total;
            selectedHoldCount = selectedHolds; totalHoldCount = totalHolds;
            sample = sample ?? new MacroStep();
            Text = EditorDialogs.T("批量修改时间", "Edit step timing");
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            ClientSize = new Size(600, 390);
            MinimumSize = new Size(560, 410);
            MaximizeBox = MinimizeBox = false;
            Font = EditorTheme.BodyFont();
            TableLayoutPanel body = new TableLayoutPanel { AutoScroll = true, Dock = DockStyle.Fill,
                ColumnCount = 2, Padding = new Padding(16), BackColor = Color.White };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            selectedScope = new RadioButton { Text = EditorDialogs.T("所选步骤", "Selected steps") + " (" + selected + ")",
                AutoSize = true, Enabled = selected > 0, Checked = selected > 0 };
            allScope = new RadioButton { Text = EditorDialogs.T("全部步骤", "All steps") + " (" + total + ")",
                AutoSize = true, Checked = selected == 0 };
            FlowLayoutPanel scope = Row(selectedScope, allScope);
            scope.Margin = new Padding(0, 0, 0, 16);
            body.Controls.Add(scope, 0, 0); body.SetColumnSpan(scope, 2);
            holdChangeBox = new CheckBox { Text = EditorDialogs.T("修改按住时长", "Hold duration"), AutoSize = true };
            waitChangeBox = new CheckBox { Text = EditorDialogs.T("修改等待时长", "Wait after each step"), AutoSize = true, Checked = true };
            holdBox = Number(sample.HoldMs);
            fixedBox = Number(sample.DelayMs);
            minBox = Number(sample.RandomDelayMinMs);
            maxBox = Number(sample.RandomDelayMaxMs);
            body.Controls.Add(holdChangeBox, 0, 1); body.Controls.Add(Row(holdBox, LabelFor("ms")), 1, 1);
            waitChangeBox.Margin = new Padding(0, 13, 0, 6);
            body.Controls.Add(waitChangeBox, 0, 2); body.SetColumnSpan(waitChangeBox, 2);
            fixedMode = new RadioButton { Text = EditorDialogs.T("固定", "Fixed"), AutoSize = true, Checked = !sample.RandomDelay };
            randomMode = new RadioButton { Text = EditorDialogs.T("随机范围", "Random range"), AutoSize = true, Checked = sample.RandomDelay };
            body.Controls.Add(fixedMode, 0, 3); body.Controls.Add(Row(fixedBox, LabelFor("ms")), 1, 3);
            body.Controls.Add(randomMode, 0, 4); body.Controls.Add(Row(minBox, LabelFor("–"), maxBox, LabelFor("ms")), 1, 4);
            rangeHint = new Label { AutoSize = true, Dock = DockStyle.Top, ForeColor = EditorTheme.Muted,
                Margin = new Padding(0, 16, 0, 0), MaximumSize = new Size(530, 0) };
            body.Controls.Add(rangeHint, 0, 5); body.SetColumnSpan(rangeHint, 2);
            applyButton = EditorDialogs.Button(EditorDialogs.T("应用修改", "Apply changes"), DialogResult.None);
            applyButton.Click += delegate
            {
                UpdateEnabled();
                if (!applyButton.Enabled) return;
                Edit = new StepTimingEdit { AllSteps = allScope.Checked, EditHold = holdChangeBox.Checked,
                    HoldMs = (int)holdBox.Value, EditWait = waitChangeBox.Checked, RandomWait = randomMode.Checked,
                    WaitMs = (int)fixedBox.Value, MinWaitMs = (int)minBox.Value, MaxWaitMs = (int)maxBox.Value };
                DialogResult = DialogResult.OK; Close();
            };
            Button cancel = EditorDialogs.Button(Localizer.T("取消"), DialogResult.Cancel);
            AcceptButton = applyButton; CancelButton = cancel;
            foreach (RadioButton option in new[] { selectedScope, allScope, fixedMode, randomMode })
                option.CheckedChanged += delegate { UpdateEnabled(); };
            holdChangeBox.CheckedChanged += delegate { UpdateEnabled(); };
            waitChangeBox.CheckedChanged += delegate { UpdateEnabled(); };
            minBox.ValueChanged += delegate { UpdateEnabled(); };
            maxBox.ValueChanged += delegate { UpdateEnabled(); };
            EditorDialogs.Frame(this, body, EditorDialogs.Actions(applyButton, cancel),
                EditorDialogs.T("选择步骤范围，勾选要修改的时间。1000 毫秒 = 1 秒。",
                    "Choose the steps and the timing fields to change. 1000 ms = 1 second."), null);
            body.Layout += delegate
            {
                int height = body.Padding.Vertical;
                foreach (Control child in body.Controls)
                    if (child.Visible) height = Math.Max(height, child.Bottom - body.AutoScrollPosition.Y + child.Margin.Bottom + body.Padding.Bottom);
                Size extent = new Size(0, height);
                if (body.AutoScrollMinSize != extent) body.AutoScrollMinSize = extent;
            };
            UpdateEnabled();
        }

        private static FlowLayoutPanel Row(params Control[] controls)
        {
            FlowLayoutPanel row = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill,
                Margin = new Padding(0), WrapContents = true };
            row.Controls.AddRange(controls); return row;
        }

        private static Label LabelFor(string value)
        { return new Label { Text = value, AutoSize = true, Margin = new Padding(4, 4, 6, 0) }; }

        private static NumericUpDown Number(int value)
        {
            return new NumericUpDown { Width = 108, Minimum = 0, Maximum = 600000,
                Value = Math.Max(0, Math.Min(600000, value)), Margin = new Padding(0, 0, 5, 4) };
        }

        private void UpdateEnabled()
        {
            int count = allScope.Checked ? totalCount : selectedCount;
            int holds = allScope.Checked ? totalHoldCount : selectedHoldCount;
            holdChangeBox.Enabled = holds > 0;
            if (!holdChangeBox.Enabled) holdChangeBox.Checked = false;
            holdBox.Enabled = holdChangeBox.Checked && holdChangeBox.Enabled;
            fixedMode.Enabled = randomMode.Enabled = waitChangeBox.Checked;
            fixedBox.Enabled = waitChangeBox.Checked && fixedMode.Checked;
            minBox.Enabled = maxBox.Enabled = waitChangeBox.Checked && randomMode.Checked;
            bool validRange = !waitChangeBox.Checked || !randomMode.Checked || minBox.Value <= maxBox.Value;
            applyButton.Enabled = count > 0 && (holdChangeBox.Checked || waitChangeBox.Checked) && validRange;
            if (!validRange)
                rangeHint.Text = EditorDialogs.T("最小等待不能大于最大等待。", "The minimum wait must not exceed the maximum.");
            else if (holds == 0)
                rangeHint.Text = EditorDialogs.T("这些步骤使用单独按下、松开或滚轮操作，可通过等待时长调整节奏。",
                    "These steps use separate down/up events or the wheel. Change their wait times to adjust the timing.");
            else if (holds < count)
                rangeHint.Text = EditorDialogs.T("可修改按住时长的步骤：", "Steps with an editable hold duration: ") + holds + " / " + count;
            else
                rangeHint.Text = EditorDialogs.T("按住时长控制一次按键持续多久；等待时长控制完成后多久执行下一步。",
                    "Hold = time pressed; wait = pause before the next step.");
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
            Font = EditorTheme.BodyFont();

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            text = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font("Consolas", 11F), BorderStyle = BorderStyle.FixedSingle };
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
            Font = EditorTheme.BodyFont();

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            ClientSize = new Size(620, 250);
            MinimumSize = new Size(520, 280);
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
