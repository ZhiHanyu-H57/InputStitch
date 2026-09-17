using System;
using System.Drawing;
using System.Windows.Forms;

namespace InputStitch
{
    internal sealed class AnalogTransformDialog : Form
    {
        private readonly AnalogTransformProfile working;
        private readonly StickEditor leftStick;
        private readonly StickEditor rightStick;
        private readonly TriggerEditor leftTrigger;
        private readonly TriggerEditor rightTrigger;

        internal AnalogTransformDialog(AnalogTransformProfile current)
        {
            working = current == null ? new AnalogTransformProfile() : current.Clone();
            Text = Localizer.IsEnglish ? "Analog Transform" : "模拟量处理";
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(650, 560);
            ClientSize = new Size(720, 620);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(14);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            Label intro = new Label();
            intro.AutoSize = true;
            intro.MaximumSize = new Size(670, 0);
            intro.Text = Localizer.IsEnglish
                ? "Transforms are applied to each routed controller before Output Ownership merges sources. Order is fixed: deadzone remap → response curve → scale → axis inversion → max-output clamp. 100% curve = linear. Values above 100% give finer control near center; values below 100% respond faster near center."
                : "模拟量处理会先作用于每个被汇总的手柄，再交给 Output Ownership 合并。顺序固定为：死区重映射 → 响应曲线 → 缩放 → 轴反转 → 最大输出限幅。曲线 100% 为线性；高于 100% 时中心附近更细腻，低于 100% 时中心附近响应更快。";
            intro.Margin = new Padding(0, 0, 0, 10);
            root.Controls.Add(intro, 0, 0);

            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            leftStick = new StickEditor(working.LeftStick);
            rightStick = new StickEditor(working.RightStick);
            leftTrigger = new TriggerEditor(working.LeftTrigger);
            rightTrigger = new TriggerEditor(working.RightTrigger);
            AddTab(tabs, Localizer.IsEnglish ? "Left stick" : "左摇杆", leftStick);
            AddTab(tabs, Localizer.IsEnglish ? "Right stick" : "右摇杆", rightStick);
            AddTab(tabs, Localizer.IsEnglish ? "Left trigger" : "左扳机", leftTrigger);
            AddTab(tabs, Localizer.IsEnglish ? "Right trigger" : "右扳机", rightTrigger);
            root.Controls.Add(tabs, 0, 1);

            Label merge = new Label();
            merge.AutoSize = true;
            merge.MaximumSize = new Size(670, 0);
            merge.Margin = new Padding(0, 8, 0, 8);
            merge.Text = Localizer.IsEnglish
                ? "Cross-source merge semantics are unchanged in this stage: sticks use normalized sum, triggers use maximum, and digital ownership is reference-counted."
                : "这一阶段不会改变多来源合并规则：摇杆仍使用归一化求和，扳机仍取最大值，数字按键仍按来源所有权合并。";
            root.Controls.Add(merge, 0, 2);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            Button cancel = new Button();
            cancel.Text = Localizer.IsEnglish ? "Cancel" : "取消";
            cancel.AutoSize = true;
            cancel.DialogResult = DialogResult.Cancel;
            Button ok = new Button();
            ok.Text = Localizer.IsEnglish ? "OK" : "确定";
            ok.AutoSize = true;
            ok.DialogResult = DialogResult.OK;
            ok.Click += delegate { CaptureProfile(); };
            Button reset = new Button();
            reset.Text = Localizer.IsEnglish ? "Reset all" : "全部恢复默认";
            reset.AutoSize = true;
            reset.Click += delegate
            {
                leftStick.LoadValues(new AnalogStickTransformConfig());
                rightStick.LoadValues(new AnalogStickTransformConfig());
                leftTrigger.LoadValues(new AnalogTriggerTransformConfig());
                rightTrigger.LoadValues(new AnalogTriggerTransformConfig());
            };
            actions.Controls.Add(cancel);
            actions.Controls.Add(ok);
            actions.Controls.Add(reset);
            root.Controls.Add(actions, 0, 3);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        internal AnalogTransformProfile SelectedProfile
        {
            get { return working.Clone(); }
        }

        private void CaptureProfile()
        {
            working.LeftStick = leftStick.Read();
            working.RightStick = rightStick.Read();
            working.LeftTrigger = leftTrigger.Read();
            working.RightTrigger = rightTrigger.Read();
            AnalogTransformProfile.Normalize(working);
        }

        private static void AddTab(TabControl tabs, string title, Control control)
        {
            TabPage page = new TabPage(title);
            page.Padding = new Padding(10);
            control.Dock = DockStyle.Fill;
            page.Controls.Add(control);
            tabs.TabPages.Add(page);
        }

        private sealed class StickEditor : UserControl
        {
            private readonly NumericUpDown inner = Number(0, 99, 0);
            private readonly NumericUpDown outer = Number(0, 99, 0);
            private readonly NumericUpDown curve = Number(25, 400, 100);
            private readonly NumericUpDown scale = Number(0, 300, 100);
            private readonly NumericUpDown max = Number(0, 100, 100);
            private readonly CheckBox invertX = new CheckBox();
            private readonly CheckBox invertY = new CheckBox();
            private readonly Label preview = new Label();
            private bool loading;

            internal StickEditor(AnalogStickTransformConfig value)
            {
                AutoScroll = true;
                TableLayoutPanel table = BaseTable();
                Controls.Add(table);
                AddNumberRow(table, 0, Localizer.IsEnglish ? "Inner deadzone" : "内死区", inner, "%",
                    Localizer.IsEnglish ? "Input inside this radial magnitude becomes zero." : "摇杆径向幅度位于该范围内时输出为 0。");
                AddNumberRow(table, 1, Localizer.IsEnglish ? "Outer deadzone" : "外死区", outer, "%",
                    Localizer.IsEnglish ? "The outer portion is remapped to full-scale output." : "最外侧这部分会提前映射到满幅输出。");
                AddNumberRow(table, 2, Localizer.IsEnglish ? "Response curve" : "响应曲线", curve, "%",
                    Localizer.IsEnglish ? "100 = linear; 200 = squared; 50 = square-root-like response." : "100 = 线性；200 ≈ 平方曲线；50 ≈ 平方根曲线。");
                AddNumberRow(table, 3, Localizer.IsEnglish ? "Scale / sensitivity" : "缩放 / 灵敏度", scale, "%",
                    Localizer.IsEnglish ? "Applied after the response curve; values above 100 can saturate earlier." : "在响应曲线之后应用；高于 100% 会更早达到限幅。");
                AddNumberRow(table, 4, Localizer.IsEnglish ? "Max output" : "最大输出", max, "%",
                    Localizer.IsEnglish ? "Final radial output clamp." : "最终径向输出上限。");

                FlowLayoutPanel invertRow = new FlowLayoutPanel();
                invertRow.AutoSize = true;
                invertRow.Dock = DockStyle.Top;
                invertX.AutoSize = true;
                invertY.AutoSize = true;
                invertX.Text = Localizer.IsEnglish ? "Invert X" : "反转 X 轴";
                invertY.Text = Localizer.IsEnglish ? "Invert Y" : "反转 Y 轴";
                invertRow.Controls.Add(invertX);
                invertRow.Controls.Add(invertY);
                table.Controls.Add(invertRow, 1, 5);
                table.Controls.Add(LabelText(Localizer.IsEnglish ? "Axis inversion" : "轴反转"), 0, 5);

                preview.AutoSize = true;
                preview.MaximumSize = new Size(560, 0);
                preview.Margin = new Padding(0, 10, 0, 0);
                table.Controls.Add(preview, 0, 6);
                table.SetColumnSpan(preview, 2);

                EventHandler changed = delegate { OnChanged(); };
                inner.ValueChanged += changed;
                outer.ValueChanged += changed;
                curve.ValueChanged += changed;
                scale.ValueChanged += changed;
                max.ValueChanged += changed;
                invertX.CheckedChanged += changed;
                invertY.CheckedChanged += changed;
                LoadValues(value);
            }

            internal void LoadValues(AnalogStickTransformConfig value)
            {
                AnalogStickTransformConfig x = value == null ? new AnalogStickTransformConfig() : value.Clone();
                loading = true;
                inner.Value = x.InnerDeadzonePercent;
                outer.Value = x.OuterDeadzonePercent;
                curve.Value = x.CurveExponentPercent;
                scale.Value = x.ScalePercent;
                max.Value = x.MaxOutputPercent;
                invertX.Checked = x.InvertX;
                invertY.Checked = x.InvertY;
                loading = false;
                OnChanged();
            }

            internal AnalogStickTransformConfig Read()
            {
                AnalogStickTransformConfig result = new AnalogStickTransformConfig();
                result.InnerDeadzonePercent = (int)inner.Value;
                result.OuterDeadzonePercent = (int)outer.Value;
                result.CurveExponentPercent = (int)curve.Value;
                result.ScalePercent = (int)scale.Value;
                result.MaxOutputPercent = (int)max.Value;
                result.InvertX = invertX.Checked;
                result.InvertY = invertY.Checked;
                AnalogStickTransformConfig.Normalize(result);
                return result;
            }

            private void OnChanged()
            {
                if (loading) return;
                EnforceDeadzonePair(inner, outer);
                AnalogStickTransformConfig value = Read();
                int x25 = 25, y = 0;
                AnalogTransformEngine.TransformStick(ref x25, ref y, value);
                int x50 = 50; y = 0;
                AnalogTransformEngine.TransformStick(ref x50, ref y, value);
                int x75 = 75; y = 0;
                AnalogTransformEngine.TransformStick(ref x75, ref y, value);
                int x100 = 100; y = 0;
                AnalogTransformEngine.TransformStick(ref x100, ref y, value);
                preview.Text = (Localizer.IsEnglish ? "Magnitude preview: " : "幅度预览：") +
                    "25→" + Math.Abs(x25).ToString() + "%   50→" + Math.Abs(x50).ToString() +
                    "%   75→" + Math.Abs(x75).ToString() + "%   100→" + Math.Abs(x100).ToString() + "%";
            }
        }

        private sealed class TriggerEditor : UserControl
        {
            private readonly NumericUpDown inner = Number(0, 99, 0);
            private readonly NumericUpDown outer = Number(0, 99, 0);
            private readonly NumericUpDown curve = Number(25, 400, 100);
            private readonly NumericUpDown scale = Number(0, 300, 100);
            private readonly NumericUpDown max = Number(0, 100, 100);
            private readonly Label preview = new Label();
            private bool loading;

            internal TriggerEditor(AnalogTriggerTransformConfig value)
            {
                AutoScroll = true;
                TableLayoutPanel table = BaseTable();
                Controls.Add(table);
                AddNumberRow(table, 0, Localizer.IsEnglish ? "Inner deadzone" : "内死区", inner, "%",
                    Localizer.IsEnglish ? "Small trigger travel inside this range becomes zero." : "小于该范围的扳机行程输出为 0。");
                AddNumberRow(table, 1, Localizer.IsEnglish ? "Outer deadzone" : "外死区", outer, "%",
                    Localizer.IsEnglish ? "The outer portion is remapped to full-scale output." : "最末端这部分会提前映射到满幅输出。");
                AddNumberRow(table, 2, Localizer.IsEnglish ? "Response curve" : "响应曲线", curve, "%",
                    Localizer.IsEnglish ? "100 = linear; above 100 gives more precision near the start." : "100 = 线性；高于 100% 时起始区域更细腻。");
                AddNumberRow(table, 3, Localizer.IsEnglish ? "Scale / sensitivity" : "缩放 / 灵敏度", scale, "%",
                    Localizer.IsEnglish ? "Applied after the response curve." : "在响应曲线之后应用。");
                AddNumberRow(table, 4, Localizer.IsEnglish ? "Max output" : "最大输出", max, "%",
                    Localizer.IsEnglish ? "Final trigger-output clamp." : "最终扳机输出上限。");
                preview.AutoSize = true;
                preview.MaximumSize = new Size(560, 0);
                preview.Margin = new Padding(0, 10, 0, 0);
                table.Controls.Add(preview, 0, 5);
                table.SetColumnSpan(preview, 2);

                EventHandler changed = delegate { OnChanged(); };
                inner.ValueChanged += changed;
                outer.ValueChanged += changed;
                curve.ValueChanged += changed;
                scale.ValueChanged += changed;
                max.ValueChanged += changed;
                LoadValues(value);
            }

            internal void LoadValues(AnalogTriggerTransformConfig value)
            {
                AnalogTriggerTransformConfig x = value == null ? new AnalogTriggerTransformConfig() : value.Clone();
                loading = true;
                inner.Value = x.InnerDeadzonePercent;
                outer.Value = x.OuterDeadzonePercent;
                curve.Value = x.CurveExponentPercent;
                scale.Value = x.ScalePercent;
                max.Value = x.MaxOutputPercent;
                loading = false;
                OnChanged();
            }

            internal AnalogTriggerTransformConfig Read()
            {
                AnalogTriggerTransformConfig result = new AnalogTriggerTransformConfig();
                result.InnerDeadzonePercent = (int)inner.Value;
                result.OuterDeadzonePercent = (int)outer.Value;
                result.CurveExponentPercent = (int)curve.Value;
                result.ScalePercent = (int)scale.Value;
                result.MaxOutputPercent = (int)max.Value;
                AnalogTriggerTransformConfig.Normalize(result);
                return result;
            }

            private void OnChanged()
            {
                if (loading) return;
                EnforceDeadzonePair(inner, outer);
                AnalogTriggerTransformConfig value = Read();
                preview.Text = (Localizer.IsEnglish ? "Output preview: " : "输出预览：") +
                    "25→" + AnalogTransformEngine.TransformTrigger(25, value).ToString() +
                    "%   50→" + AnalogTransformEngine.TransformTrigger(50, value).ToString() +
                    "%   75→" + AnalogTransformEngine.TransformTrigger(75, value).ToString() +
                    "%   100→" + AnalogTransformEngine.TransformTrigger(100, value).ToString() + "%";
            }
        }

        private static TableLayoutPanel BaseTable()
        {
            TableLayoutPanel table = new TableLayoutPanel();
            table.AutoSize = true;
            table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            table.Dock = DockStyle.Top;
            table.ColumnCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return table;
        }

        private static void AddNumberRow(TableLayoutPanel table, int row, string title, NumericUpDown number, string suffix, string hint)
        {
            table.RowCount = Math.Max(table.RowCount, row + 1);
            table.Controls.Add(LabelText(title), 0, row);
            FlowLayoutPanel valueRow = new FlowLayoutPanel();
            valueRow.AutoSize = true;
            valueRow.Dock = DockStyle.Top;
            number.Width = 90;
            valueRow.Controls.Add(number);
            Label unit = LabelText(suffix);
            unit.Margin = new Padding(2, 6, 0, 0);
            valueRow.Controls.Add(unit);
            Label help = LabelText(hint);
            help.MaximumSize = new Size(330, 0);
            help.Margin = new Padding(12, 5, 0, 0);
            valueRow.Controls.Add(help);
            table.Controls.Add(valueRow, 1, row);
        }

        private static NumericUpDown Number(int min, int max, int value)
        {
            NumericUpDown box = new NumericUpDown();
            box.Minimum = min;
            box.Maximum = max;
            box.Value = Math.Max(min, Math.Min(max, value));
            box.Increment = 1;
            return box;
        }

        private static Label LabelText(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Margin = new Padding(0, 6, 8, 6);
            return label;
        }

        private static void EnforceDeadzonePair(NumericUpDown inner, NumericUpDown outer)
        {
            int maxOuter = Math.Max(0, 99 - (int)inner.Value);
            if (outer.Maximum != maxOuter) outer.Maximum = maxOuter;
            if (outer.Value > maxOuter) outer.Value = maxOuter;
        }
    }
}
