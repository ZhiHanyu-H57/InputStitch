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
            Text = "选择要导出的宏";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(460, 520);
            MinimumSize = new Size(400, 380);
            Font = new Font("Microsoft YaHei UI", 9F);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;

            Label tip = new Label();
            tip.Text = "勾选一个或多个宏。导出的宏包只包含宏本身，不会改变程序全局设置。";
            tip.Location = new Point(14, 14);
            tip.Size = new Size(410, 42);
            Controls.Add(tip);

            list = new CheckedListBox();
            list.CheckOnClick = true;
            list.Location = new Point(14, 58);
            list.Size = new Size(410, 350);
            list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            if (macros != null)
            {
                for (int i = 0; i < macros.Count; i++)
                {
                    MacroDefinition m = macros[i];
                    list.Items.Add(m == null ? Localizer.T("（空宏）") : m.Name, i == currentIndex);
                }
            }
            Controls.Add(list);

            Button all = new Button();
            all.Text = "全选";
            all.Location = new Point(14, 420);
            all.Size = new Size(70, 30);
            all.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            all.Click += delegate
            {
                for (int i = 0; i < list.Items.Count; i++) list.SetItemChecked(i, true);
            };
            Controls.Add(all);

            Button none = new Button();
            none.Text = "全不选";
            none.Location = new Point(90, 420);
            none.Size = new Size(70, 30);
            none.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            none.Click += delegate
            {
                for (int i = 0; i < list.Items.Count; i++) list.SetItemChecked(i, false);
            };
            Controls.Add(none);

            Button ok = new Button();
            ok.Text = "导出";
            ok.Location = new Point(276, 420);
            ok.Size = new Size(70, 30);
            ok.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            ok.Click += delegate
            {
                SelectedIndices.Clear();
                for (int i = 0; i < list.Items.Count; i++)
                    if (list.GetItemChecked(i)) SelectedIndices.Add(i);
                if (SelectedIndices.Count == 0)
                {
                    LocalizedMessageBox.Show(this, "请至少选择一个宏。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.Location = new Point(354, 420);
            cancel.Size = new Size(70, 30);
            cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);

            AcceptButton = ok;
            CancelButton = cancel;
            Localizer.ApplyStaticControls(this);
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

        public BatchDelayDialog()
        {
            Text = "批量设置步骤间隔";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 210);
            Font = new Font("Microsoft YaHei UI", 9F);

            fixedMode = new RadioButton();
            fixedMode.Text = "固定间隔";
            fixedMode.Location = new Point(20, 24);
            fixedMode.AutoSize = true;
            fixedMode.Checked = true;
            fixedMode.CheckedChanged += delegate { UpdateEnabled(); };
            Controls.Add(fixedMode);

            fixedBox = MakeNumber(135, 20, 50);
            Controls.Add(fixedBox);
            Controls.Add(MakeLabel("ms", 250, 24));

            randomMode = new RadioButton();
            randomMode.Text = "随机间隔";
            randomMode.Location = new Point(20, 72);
            randomMode.AutoSize = true;
            randomMode.CheckedChanged += delegate { UpdateEnabled(); };
            Controls.Add(randomMode);

            minBox = MakeNumber(135, 68, 45);
            Controls.Add(minBox);
            Controls.Add(MakeLabel("～", 250, 72));
            maxBox = MakeNumber(275, 68, 60);
            Controls.Add(maxBox);
            Controls.Add(MakeLabel("ms", 390, 72));

            Label tip = new Label();
            tip.Text = "仅修改当前选中的步骤，不改变按住时长。";
            tip.Location = new Point(20, 115);
            tip.AutoSize = true;
            Controls.Add(tip);

            Button ok = new Button();
            ok.Text = "确定";
            ok.Location = new Point(230, 155);
            ok.Size = new Size(80, 32);
            ok.Click += delegate
            {
                if (randomMode.Checked && minBox.Value > maxBox.Value)
                {
                    LocalizedMessageBox.Show(this, "随机间隔的最小值不能大于最大值。", AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                UseRandom = randomMode.Checked;
                FixedMs = (int)fixedBox.Value;
                MinMs = (int)minBox.Value;
                MaxMs = (int)maxBox.Value;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.Location = new Point(320, 155);
            cancel.Size = new Size(80, 32);
            cancel.DialogResult = DialogResult.Cancel;
            Controls.Add(cancel);
            CancelButton = cancel;
            UpdateEnabled();
            Localizer.ApplyStaticControls(this);
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
            Text = AppInfo.ProductName + " - 诊断信息";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(650, 520);
            MinimumSize = new Size(520, 380);
            Font = new Font("Microsoft YaHei UI", 9F);

            text = new TextBox();
            text.Multiline = true;
            text.ReadOnly = true;
            text.ScrollBars = ScrollBars.Both;
            text.WordWrap = false;
            text.Font = new Font("Consolas", 9F);
            text.Location = new Point(12, 12);
            text.Size = new Size(610, 410);
            text.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(text);

            Button copy = new Button();
            copy.Text = "复制";
            copy.Location = new Point(452, 435);
            copy.Size = new Size(80, 32);
            copy.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            copy.Click += delegate
            {
                try { Clipboard.SetText(text.Text ?? ""); } catch { }
            };
            Controls.Add(copy);

            Button close = new Button();
            close.Text = "关闭";
            close.Location = new Point(542, 435);
            close.Size = new Size(80, 32);
            close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            close.Click += delegate { Close(); };
            Controls.Add(close);

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
    }

    internal sealed class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "关于 " + AppInfo.ProductName;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 310);
            Font = new Font("Microsoft YaHei UI", 9F);

            Label title = new Label();
            title.Text = AppInfo.ProductName + " " + AppInfo.Version;
            title.Font = new Font(Font.FontFamily, 16F, FontStyle.Bold);
            title.Location = new Point(24, 22);
            title.AutoSize = true;
            Controls.Add(title);

            Label body = new Label();
            body.Text = "轻量级 Windows 可视化键鼠与虚拟手柄宏工具\r\n专注精确时序、游戏场景下的可靠控制与安全停止。\r\n\r\n支持键盘、鼠标、Xbox 360 / PS4 虚拟手柄、宏录制、宏包与配置方案。\r\n\r\n配置目录：\r\n" + AppPaths.Root;
            body.Location = new Point(26, 72);
            body.Size = new Size(465, 170);
            Controls.Add(body);

            Button open = new Button();
            open.Text = "打开配置目录";
            open.Location = new Point(270, 255);
            open.Size = new Size(120, 32);
            open.Click += delegate
            {
                try { Process.Start("explorer.exe", AppPaths.Root); } catch { }
            };
            Controls.Add(open);

            Button close = new Button();
            close.Text = "关闭";
            close.Location = new Point(400, 255);
            close.Size = new Size(90, 32);
            close.Click += delegate { Close(); };
            Controls.Add(close);
            Localizer.ApplyStaticControls(this);
        }
    }
}
