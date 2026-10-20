using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace InputStitch
{
    // Main window construction (BuildUi) and tray icon construction. Extracted from InputStitch.cs as the
    // fourth step of the incremental MainForm decomposition (architecture-review finding F4); behaviour is
    // unchanged - the members stay on the same partial class.
    public partial class MainForm
    {
        private void BuildUi()
        {
            uiToolTip = new ToolTip();
            uiToolTip.InitialDelay = 450;
            uiToolTip.ReshowDelay = 120;
            uiToolTip.AutoPopDelay = 12000;
            uiToolTip.ShowAlways = true;
            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.FixedPanel = FixedPanel.Panel1;
            split.SplitterWidth = 6;
            split.BackColor = Color.FromArgb(220, 226, 235);
            split.Panel1.BackColor = Color.FromArgb(247, 249, 252);
            split.Panel2.BackColor = Color.FromArgb(244, 247, 251);
            Controls.Add(split);
            split.Panel1MinSize = 220;
            split.Panel2MinSize = 600;
            split.SplitterDistance = 280;

            TableLayoutPanel leftRoot = new TableLayoutPanel();
            leftRoot.Dock = DockStyle.Fill;
            leftRoot.Padding = new Padding(12);
            leftRoot.ColumnCount = 1;
            leftRoot.RowCount = 5;
            leftRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            split.Panel1.Controls.Add(leftRoot);

            TableLayoutPanel leftHeader = new TableLayoutPanel();
            leftHeader.Dock = DockStyle.Fill;
            leftHeader.AutoSize = true;
            leftHeader.ColumnCount = 2;
            leftHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            leftHeader.Margin = new Padding(0, 0, 0, 10);
            Control leftTitle = new EditorWordmark();
            leftTitle.Size = new Size(202, 48);
            leftTitle.Dock = DockStyle.Fill;
            leftTitle.Anchor = AnchorStyles.Left;
            toolsButton = new GearButton();
            toolsButton.Size = new Size(36, 36);
            toolsButton.Margin = new Padding(6, 0, 0, 0);
            toolsButton.AccessibleName = Localizer.T("设置");
            BuildToolsMenu();
            toolsButton.Click += delegate { if (toolsMenu != null) toolsMenu.Show(toolsButton, new Point(0, toolsButton.Height)); };
            leftHeader.Controls.Add(leftTitle, 0, 0);
            leftHeader.Controls.Add(toolsButton, 1, 0);
            leftRoot.Controls.Add(leftHeader, 0, 0);

            Button addMacro = MakeButton("＋ 新建宏", 0, 0, 0);
            newMacroButton = addMacro;
            addMacro.Dock = DockStyle.Fill;
            addMacro.MinimumSize = new Size(0, 44);
            addMacro.Margin = new Padding(0, 4, 0, 18);
            addMacro.Font = new Font(Font, FontStyle.Bold);
            addMacro.Click += ShowNewMacroChooser;
            leftRoot.Controls.Add(addMacro, 0, 1);
            macroCountLabel = new Label { AutoSize = true, Dock = DockStyle.Fill, ForeColor = EditorTheme.Muted, Margin = new Padding(3, 0, 0, 10) };
            leftRoot.Controls.Add(macroCountLabel, 0, 2);

            macroList = new ListBox();
            macroList.Dock = DockStyle.Fill;
            macroList.IntegralHeight = false;
            macroList.BorderStyle = BorderStyle.None;
            macroList.BackColor = Color.White;
            macroList.DrawMode = DrawMode.OwnerDrawFixed;
            macroList.ItemHeight = 76;
            macroList.DrawItem += DrawMacroItem;
            macroList.Margin = new Padding(0, 0, 0, 10);
            macroList.SelectedIndexChanged += MacroList_SelectedIndexChanged;
            leftRoot.Controls.Add(macroList, 0, 3);

            FlowLayoutPanel macroButtons = new FlowLayoutPanel();
            macroButtons.Dock = DockStyle.Fill;
            macroButtons.AutoSize = true;
            macroButtons.WrapContents = true;
            macroButtons.Margin = new Padding(0);
            Button copyMacro = MakeButton("复制", 0, 0, 0);
            Button delMacro = MakeButton("删除", 0, 0, 0);
            Button upMacro = MakeButton("↑", 0, 0, 0);
            Button downMacro = MakeButton("↓", 0, 0, 0);
            foreach (Button b in new Button[] { copyMacro, delMacro }) { b.AutoSize = true; b.AutoSizeMode = AutoSizeMode.GrowAndShrink; b.MinimumSize = new Size(62, 32); }
            upMacro.Size = downMacro.Size = new Size(34, 32);
            copyMacro.Click += CopyMacro_Click;
            delMacro.Click += DeleteMacro_Click;
            upMacro.Click += delegate { MoveSelectedMacro(-1); };
            downMacro.Click += delegate { MoveSelectedMacro(1); };
            Button libraryButton = MakeButton("导入与备份 ▾", 0, 0, 0);
            libraryButton.AutoSize = true;
            libraryButton.MinimumSize = new Size(238, 36);
            libraryButton.Margin = new Padding(0, 0, 0, 8);
            libraryButton.Click += ShowLibraryMenu;
            macroButtons.Controls.Add(libraryButton);
            macroButtons.SetFlowBreak(libraryButton, true);
            macroButtons.Controls.AddRange(new Control[] { copyMacro, delMacro, upMacro, downMacro });
            leftRoot.Controls.Add(macroButtons, 0, 4);

            Panel rightHost = new Panel();
            rightHost.Dock = DockStyle.Fill;
            rightHost.AutoScroll = true;
            rightHost.BackColor = Color.FromArgb(244, 247, 251);
            split.Panel2.Controls.Add(rightHost);

            TableLayoutPanel rightRoot = new TableLayoutPanel();
            rightRoot.Dock = DockStyle.Top;
            rightRoot.AutoSize = true;
            rightRoot.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            rightRoot.Padding = new Padding(14, 12, 14, 12);
            rightRoot.ColumnCount = 1;
            rightRoot.RowCount = 4;
            rightRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rightHost.Controls.Add(rightRoot);
            EventHandler resizeRightContent = delegate
            {
                int width = Math.Max(560, rightHost.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
                int height = Math.Max(0, rightHost.ClientSize.Height);
                rightRoot.MinimumSize = new Size(width, height);
            };
            rightHost.Resize += resizeRightContent;
            Shown += delegate { resizeRightContent(rightHost, EventArgs.Empty); };

            GroupBox detailsGroup = MakeMainGroup("宏信息");
            TableLayoutPanel details = MakeFormGrid();
            detailsGroup.Controls.Add(details);
            Label nameLabel = MakeFieldLabel("名称：");
            nameBox = new TextBox();
            nameBox.Dock = DockStyle.Fill;
            nameBox.TextChanged += NameBox_TextChanged;
            nameBox.Leave += NameBox_Leave;
            enabledBox = new CheckBox();
            enabledBox.Text = "启用触发键";
            enabledBox.AutoSize = true;
            enabledBox.Anchor = AnchorStyles.Left;
            enabledBox.Margin = new Padding(12, 4, 0, 4);
            enabledBox.CheckedChanged += Settings_Changed;
            details.Controls.Add(nameLabel, 0, 0);
            details.Controls.Add(nameBox, 1, 0);
            details.Controls.Add(enabledBox, 2, 0);
            Label descLabel = MakeFieldLabel("备注：");
            descriptionBox = new TextBox();
            descriptionBox.Dock = DockStyle.Fill;
            descriptionBox.TextChanged += DescriptionBox_TextChanged;
            descriptionBox.Leave += delegate { if (!loadingUi) SaveConfig(); };
            details.Controls.Add(descLabel, 0, 1);
            details.Controls.Add(descriptionBox, 1, 1);
            details.SetColumnSpan(descriptionBox, 2);
            workflowGuide = new Label { AutoSize = true, Dock = DockStyle.Fill, ForeColor = EditorTheme.Muted, MaximumSize = new Size(800, 0), Margin = new Padding(0, 8, 0, 0) };
            details.Controls.Add(workflowGuide, 0, 2);
            details.SetColumnSpan(workflowGuide, 3);
            rightRoot.Controls.Add(detailsGroup, 0, 0);

            GroupBox triggerGroup = MakeMainGroup("如何触发");
            ((EditorSection)triggerGroup).Badge = "1";
            TableLayoutPanel triggerLayout = MakeFormGrid();
            triggerGroup.Controls.Add(triggerLayout);

            TableLayoutPanel triggerTop = new TableLayoutPanel();
            triggerTop.Dock = DockStyle.Top;
            triggerTop.AutoSize = true;
            triggerTop.ColumnCount = 3;
            triggerTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108F));
            triggerTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            triggerTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            triggerTop.Margin = new Padding(0, 0, 0, 8);
            triggerBox = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Margin = new Padding(0, 5, 10, 5) };
            captureTriggerButton = MakeButton(EditorDialogs.T("选择触发键…", "Choose shortcut…"), 0, 0, 0);
            captureTriggerButton.AutoSize = true;
            captureTriggerButton.MinimumSize = new Size(128, 32);
            captureTriggerButton.Click += SelectVirtualKeyboardTrigger;
            triggerTop.Controls.Add(MakeFieldLabel("触发键："), 0, 0);
            triggerTop.Controls.Add(triggerBox, 1, 0);
            triggerTop.Controls.Add(VirtualKeyboardDialog.WithDropDown(captureTriggerButton,
                CaptureTriggerButton_Click, SelectVirtualKeyboardTrigger), 2, 0);
            triggerModeBox = new EditorChoice { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 5, 10, 5), DropDownWidth = 330 };
            triggerModeBox.Items.AddRange(TriggerEditor.Names);
            triggerModeBox.SelectedIndexChanged += TriggerModeBox_SelectedIndexChanged;
            triggerTop.Controls.Add(MakeFieldLabel("触发方式："), 0, 1);
            triggerTop.Controls.Add(triggerModeBox, 1, 1);
            activatorConditionButton = MakeButton(Localizer.IsEnglish ? "Timing & conditions…" : "时间与条件…", 0, 0, 0);
            activatorConditionButton.AutoSize = true;
            activatorConditionButton.UseMnemonic = false;
            activatorConditionButton.MinimumSize = new Size(128, 32);
            activatorConditionButton.Click += ActivatorConditionButton_Click;
            triggerTop.Controls.Add(activatorConditionButton, 2, 1);
            triggerBehaviorLabel = new Label { AutoSize = true, Dock = DockStyle.Fill,
                ForeColor = EditorTheme.Muted, Margin = new Padding(0, 3, 0, 6), MaximumSize = new Size(560, 0) };
            triggerTop.Controls.Add(triggerBehaviorLabel, 1, 2);
            triggerTop.SetColumnSpan(triggerBehaviorLabel, 2);
            triggerLayout.Controls.Add(triggerTop, 0, 0);
            triggerLayout.SetColumnSpan(triggerTop, 3);

            Label repeatLabel = MakeFieldLabel("执行次数：");
            repeatBox = new NumericUpDown();
            repeatBox.Minimum = 1;
            repeatBox.Maximum = 100000000;
            repeatBox.Value = 1;
            repeatBox.Width = 110;
            repeatBox.Anchor = AnchorStyles.Left;
            repeatBox.ValueChanged += Settings_Changed;
            infiniteBox = new CheckBox();
            infiniteBox.Text = "无限循环（再次触发或点击停止）";
            infiniteBox.AutoSize = true;
            infiniteBox.Anchor = AnchorStyles.Left;
            infiniteBox.Margin = new Padding(12, 4, 0, 4);
            infiniteBox.CheckedChanged += InfiniteBox_CheckedChanged;
            triggerLayout.Controls.Add(repeatLabel, 0, 1);
            triggerLayout.Controls.Add(repeatBox, 1, 1);
            triggerLayout.Controls.Add(infiniteBox, 2, 1);
            suppressBox = new CheckBox();
            suppressBox.Text = "触发时屏蔽最后一个触发键/鼠标事件";
            suppressBox.AutoSize = true;
            suppressBox.Anchor = AnchorStyles.Left;
            suppressBox.CheckedChanged += Settings_Changed;
            optionalSettingsButton = MakeButton("", 0, 0, 0);
            optionalSettingsButton.AutoSize = true;
            optionalSettingsButton.UseMnemonic = false;
            optionalSettingsButton.Click += ToggleOptionalSettings;
            triggerLayout.Controls.Add(optionalSettingsButton, 0, 3);
            triggerLayout.SetColumnSpan(optionalSettingsButton, 3);
            optionalSettingsPanel = MakeFormGrid();
            optionalSettingsPanel.Visible = false;
            optionalSettingsPanel.Controls.Add(suppressBox, 1, 0);
            optionalSettingsPanel.SetColumnSpan(suppressBox, 2);
            triggerLayout.Controls.Add(optionalSettingsPanel, 0, 4);
            triggerLayout.SetColumnSpan(optionalSettingsPanel, 3);

            Label activeLayerLabel = MakeFieldLabel("当前映射层：");
            activeLayerBox = new EditorChoice();
            activeLayerBox.DropDownStyle = ComboBoxStyle.DropDownList;
            activeLayerBox.Dock = DockStyle.Fill;
            activeLayerBox.SelectedIndexChanged += ActiveLayerBox_SelectedIndexChanged;
            layerManageButton = new Button();
            layerManageButton.AutoSize = true;
            layerManageButton.Text = Localizer.IsEnglish ? "Manage layers..." : "管理映射层…";
            layerManageButton.Click += delegate { ShowLayerManagementMenu(); };
            optionalSettingsPanel.Controls.Add(activeLayerLabel, 0, 1);
            optionalSettingsPanel.Controls.Add(activeLayerBox, 1, 1);
            optionalSettingsPanel.Controls.Add(layerManageButton, 2, 1);

            Label mappingLayerLabel = MakeFieldLabel("此宏属于：");
            mappingLayerBox = new EditorChoice();
            mappingLayerBox.DropDownStyle = ComboBoxStyle.DropDownList;
            mappingLayerBox.Dock = DockStyle.Fill;
            mappingLayerBox.SelectedIndexChanged += MappingLayerBox_SelectedIndexChanged;
            optionalSettingsPanel.Controls.Add(mappingLayerLabel, 0, 2);
            optionalSettingsPanel.Controls.Add(mappingLayerBox, 1, 2);
            optionalSettingsPanel.SetColumnSpan(mappingLayerBox, 2);

            runtimeCapabilityTitleLabel = MakeFieldLabel(Localizer.IsEnglish ? "Status:" : "运行状态：");
            runtimeCapabilityLabel = new Label();
            runtimeCapabilityLabel.AutoSize = true;
            runtimeCapabilityLabel.Dock = DockStyle.Fill;
            runtimeCapabilityLabel.Margin = new Padding(0, 5, 0, 3);
            runtimeCapabilityLabel.Padding = new Padding(0, 2, 0, 2);
            runtimeCapabilityLabel.TextAlign = ContentAlignment.MiddleLeft;
            triggerLayout.Controls.Add(runtimeCapabilityTitleLabel, 0, 2);
            triggerLayout.Controls.Add(runtimeCapabilityLabel, 1, 2);
            triggerLayout.SetColumnSpan(runtimeCapabilityLabel, 2);
            rightRoot.Controls.Add(triggerGroup, 0, 1);

            targetGroup = MakeMainGroup("目标窗口");
            TableLayoutPanel targetLayout = MakeFormGrid();
            targetGroup.Controls.Add(targetLayout);
            Label targetLabel = MakeFieldLabel("目标窗口：");
            targetWindowBox = new TextBox();
            targetWindowBox.Dock = DockStyle.Fill;
            targetWindowBox.ReadOnly = true;
            targetWindowBox.TabStop = false;
            FlowLayoutPanel targetButtons = new FlowLayoutPanel();
            targetButtons.AutoSize = true;
            targetButtons.WrapContents = false;
            targetButtons.Margin = new Padding(8, 0, 0, 0);
            lockTargetButton = MakeButton("锁定刚才的目标窗口", 0, 0, 0);
            lockTargetButton.AutoSize = true;
            lockTargetButton.MinimumSize = new Size(165, 32);
            lockTargetButton.Click += LockTargetButton_Click;
            clearTargetButton = MakeButton("清除目标", 0, 0, 0);
            clearTargetButton.AutoSize = true;
            clearTargetButton.MinimumSize = new Size(96, 32);
            clearTargetButton.Click += ClearTargetButton_Click;
            targetButtons.Controls.Add(lockTargetButton);
            targetButtons.Controls.Add(clearTargetButton);
            targetLayout.Controls.Add(targetLabel, 0, 0);
            targetLayout.Controls.Add(targetWindowBox, 1, 0);
            targetLayout.Controls.Add(targetButtons, 2, 0);
            targetGroup.Visible = false;
            rightRoot.Controls.Add(targetGroup, 0, 2);

            GroupBox stepsGroup = MakeMainGroup("执行步骤");
            ((EditorSection)stepsGroup).Badge = "2";
            stepsGroup.Dock = DockStyle.Fill;
            stepsGroup.AutoSize = false;
            stepsGroup.MinimumSize = new Size(0, 340);
            TableLayoutPanel stepsLayout = new TableLayoutPanel();
            stepsLayout.Dock = DockStyle.Fill;
            stepsLayout.ColumnCount = 1;
            stepsLayout.RowCount = 4;
            stepsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            stepsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stepsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stepsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            // Leave room for the horizontal scrollbar as well as a full-height button.
            stepsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            stepsGroup.Controls.Add(stepsLayout);
            TableLayoutPanel stepsHeader = new TableLayoutPanel();
            stepsHeader.Dock = DockStyle.Fill;
            stepsHeader.AutoSize = true;
            stepsHeader.ColumnCount = 2;
            stepsHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            stepsHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            FlowLayoutPanel primaryActions = new FlowLayoutPanel();
            primaryActions.AutoSize = true;
            primaryActions.WrapContents = false;
            primaryActions.FlowDirection = FlowDirection.RightToLeft;
            primaryActions.Margin = new Padding(0);

            runButton = MakeButton("▶ 执行所选宏", 0, 0, 0);
            runButton.AutoSize = true;
            runButton.MinimumSize = new Size(210, 38);
            runButton.Margin = new Padding(8, 0, 0, 2);
            runButton.Font = new Font(Font, FontStyle.Bold);
            runButton.FlatStyle = FlatStyle.Flat;
            runButton.FlatAppearance.BorderSize = 1;
            runButton.FlatAppearance.BorderColor = SystemColors.ControlDark;
            runButton.FlatAppearance.MouseOverBackColor = SystemColors.ControlLight;
            runButton.FlatAppearance.MouseDownBackColor = SystemColors.ControlLight;
            runButton.BackColor = SystemColors.Control;
            runButton.UseVisualStyleBackColor = false;
            runButton.TabStop = false;
            runButton.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            };
            runButton.Click += RunButton_Click;

            stepRunButton = MakeButton("▷ 单步执行", 0, 0, 0);
            stepRunButton.AutoSize = true;
            stepRunButton.MinimumSize = new Size(112, 38);
            stepRunButton.Margin = new Padding(0, 0, 0, 2);
            stepRunButton.Font = new Font(Font, FontStyle.Bold);
            stepRunButton.FlatStyle = FlatStyle.Flat;
            stepRunButton.FlatAppearance.BorderSize = 1;
            stepRunButton.FlatAppearance.BorderColor = SystemColors.ControlDark;
            stepRunButton.FlatAppearance.MouseOverBackColor = SystemColors.ControlLight;
            stepRunButton.FlatAppearance.MouseDownBackColor = SystemColors.ControlLight;
            stepRunButton.BackColor = SystemColors.Control;
            stepRunButton.UseVisualStyleBackColor = false;
            stepRunButton.Click += StepRunButton_Click;

            recordButton = MakeButton("● 录制宏", 0, 0, 0);
            recordButton.AutoSize = true;
            recordButton.MinimumSize = new Size(142, 38);
            recordButton.Margin = new Padding(0, 0, 0, 2);
            recordButton.Font = new Font(Font, FontStyle.Bold);
            recordButton.FlatStyle = FlatStyle.Flat;
            recordButton.FlatAppearance.BorderSize = 1;
            recordButton.FlatAppearance.BorderColor = SystemColors.ControlDark;
            recordButton.FlatAppearance.MouseOverBackColor = SystemColors.ControlLight;
            recordButton.FlatAppearance.MouseDownBackColor = SystemColors.ControlLight;
            recordButton.BackColor = SystemColors.Control;
            recordButton.UseVisualStyleBackColor = false;
            recordButton.Click += RecordButton_Click;
            primaryActions.Controls.Add(runButton);
            primaryActions.Controls.Add(stepRunButton);
            primaryActions.Controls.Add(recordButton);
            stepsHeader.Controls.Add(primaryActions, 1, 0);
            stepsHintLabel = new Label { AutoSize = true, Dock = DockStyle.Fill, ForeColor = EditorTheme.Muted, Margin = new Padding(0, 0, 0, 10) };
            stepsLayout.Controls.Add(stepsHintLabel, 0, 0);
            stepsLayout.Controls.Add(stepsHeader, 0, 1);
            FlowLayoutPanel historyButtons = new FlowLayoutPanel();
            historyButtons.AutoSize = true;
            historyButtons.WrapContents = false;
            undoStepsButton = MakeButton("撤销", 0, 0, 68);
            redoStepsButton = MakeButton("重做", 0, 0, 68);
            undoStepsButton.AutoSize = redoStepsButton.AutoSize = true;
            undoStepsButton.Click += delegate { RestoreSteps(false); };
            redoStepsButton.Click += delegate { RestoreSteps(true); };
            historyButtons.Controls.AddRange(new Control[] { undoStepsButton, redoStepsButton });
            stepsHeader.Controls.Add(historyButtons, 0, 0);
            RegisterUiSafetyControl(undoStepsButton, "编辑步骤");
            RegisterUiSafetyControl(redoStepsButton, "编辑步骤");
            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.MinimumSize = new Size(0, 130);
            grid.Margin = new Padding(0, 4, 0, 6);
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = true;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.Columns.Add("Index", "#");
            grid.Columns.Add("Action", "操作");
            grid.Columns.Add("Input", "按键 / 鼠标 / 手柄");
            grid.Columns.Add("Hold", "按住 (ms)");
            grid.Columns.Add("Delay", "等待 (ms)");
            grid.Columns[0].Width = 45;
            grid.Columns[0].MinimumWidth = 40;
            grid.Columns[1].Width = 125;
            grid.Columns[1].MinimumWidth = 105;
            grid.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns[2].MinimumWidth = 160;
            grid.Columns[3].Width = 130;
            grid.Columns[3].MinimumWidth = 115;
            grid.Columns[4].Width = 150;
            grid.Columns[4].MinimumWidth = 130;
            grid.CellDoubleClick += delegate { EditSelectedStep(); };
            grid.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Control && (e.KeyCode == Keys.Z || e.KeyCode == Keys.Y))
                {
                    RestoreSteps(e.KeyCode == Keys.Y || e.Shift);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            Panel stepsHost = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 8) };
            stepsHost.Controls.Add(grid);
            emptyStepsPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 250, 253) };
            TableLayoutPanel emptyContent = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 2 };
            emptyContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            emptyContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            emptyContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Label emptyText = new Label { Text = "每个步骤就是一次按键或鼠标操作。\r\n试着添加第一个步骤，设置要按的键和等待时间。", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = EditorTheme.Muted };
            Button firstStep = MakeButton("＋ 添加第一个步骤", 0, 0, 190);
            firstStep.AutoSize = true;
            firstStep.Anchor = AnchorStyles.None;
            firstStep.Click += delegate { AddStep(); };
            emptyContent.Controls.Add(emptyText, 0, 0);
            emptyContent.Controls.Add(firstStep, 0, 1);
            emptyStepsPanel.Controls.Add(emptyContent);
            stepsHost.Controls.Add(emptyStepsPanel);
            emptyStepsPanel.BringToFront();
            RegisterUiSafetyControl(firstStep, "添加宏步骤");
            stepsLayout.Controls.Add(stepsHost, 0, 2);

            FlowLayoutPanel stepFooter = new FlowLayoutPanel();
            stepFooter.Dock = DockStyle.Fill;
            stepFooter.AutoSize = false;
            stepFooter.WrapContents = false;
            stepFooter.AutoScroll = true;
            Button addStep = MakeButton("＋ 添加步骤", 0, 0, 0);
            Button editStep = MakeButton("编辑", 0, 0, 0);
            Button copyStep = MakeButton("复制步骤", 0, 0, 0);
            Button delStep = MakeButton("删除", 0, 0, 0);
            Button upStep = MakeButton("上移", 0, 0, 0);
            Button downStep = MakeButton("下移", 0, 0, 0);
            Button batchDelayStep = MakeButton("批量间隔", 0, 0, 0);
            foreach (Button b in new Button[] { addStep, editStep, copyStep, delStep, upStep, downStep, batchDelayStep })
            {
                b.AutoSize = true;
                b.MinimumSize = new Size(66, 32);
                b.Margin = new Padding(0, 0, 6, 4);
            }
            addStepButton = addStep;
            editStepButton = editStep;
            addStep.Click += delegate { AddStep(); };
            editStep.Click += delegate { EditSelectedStep(); };
            copyStep.Click += delegate { CopySelectedSteps(); };
            delStep.Click += delegate { DeleteSelectedStep(); };
            upStep.Click += delegate { MoveSelectedStep(-1); };
            downStep.Click += delegate { MoveSelectedStep(1); };
            batchDelayStep.Click += delegate { BatchEditSelectedStepDelay(); };
            stepFooter.Controls.AddRange(new Control[] { addStep, editStep, copyStep, delStep, upStep, downStep, batchDelayStep });
            stepsLayout.Controls.Add(stepFooter, 0, 3);
            rightRoot.Controls.Add(stepsGroup, 0, 3);

            SetTip(libraryButton, "导入宏、分享宏，或备份与载入整套方案。");
            SetTip(toolsButton, "打开安全、诊断、托盘和关于选项。" );
            SetTip(upMacro, "将当前宏在列表中上移一位；多个已启用宏共用触发键时，列表靠上的宏优先触发。" );
            SetTip(downMacro, "将当前宏在列表中下移一位；多个已启用宏共用触发键时，列表靠上的宏优先触发。" );

            SetTip(enabledBox, "控制这个宏是否响应全局触发键。关闭后仍可用 UI 按钮执行。" );
            SetTip(descriptionBox, "给宏添加简短用途说明。备注会随宏包一起导出。" );
            SetTip(infiniteBox, "持续重复整个宏，直到再次触发、松开按住型触发键、点击停止或使用紧急停止键。" );
            SetTip(repeatBox, "宏完整执行的次数。勾选无限循环后忽略此数值。" );
            SetTip(triggerModeBox, "选择按一次切换启动/停止，或按住触发键运行、松开立即停止。" );
            SetTip(activatorConditionButton, Localizer.IsEnglish ? "Adjust press timing or limit when this macro can run." : "调整短按、长按、双击的时间，或限制这个宏在哪些情况下生效。" );
            SetTip(captureTriggerButton, "点击后按下希望使用的键盘键、鼠标按钮或滚轮方向来设置触发方式。" );
            SetTip(suppressBox, "触发宏时阻止最后一个实际按键或鼠标事件继续传给当前程序；其他输入不受影响。" );
            SetTip(targetWindowBox, "当前锁定的目标窗口。用于 UI 自动切换和保存方案时的进程绑定。" );
            SetTip(lockTargetButton, "先切到目标程序，再切回 InputStitch 后点击这里；程序会锁定最近有效的外部前台窗口。" );
            SetTip(clearTargetButton, "清除当前目标窗口，不删除任何宏。" );
            SetTip(recordButton, "录制物理键盘、鼠标按钮和滚轮输入，自动换算为宏步骤；不记录鼠标移动。" );
            SetTip(copyStep, "复制当前选中的一个或多个步骤，并插入到所选步骤之后。" );
            SetTip(batchDelayStep, "批量设置所选步骤的固定间隔或简单随机间隔范围。" );
            SetTip(runButton, "开始执行所选宏；所选宏正在运行时用于停止所选宏。" );

            RegisterUiSafetyControl(libraryButton, "导入与备份");
            RegisterUiSafetyControl(toolsButton, "打开工具菜单");
            RegisterUiSafetyControl(macroList, "选择宏");
            RegisterUiSafetyControl(addMacro, "新增宏");
            RegisterUiSafetyControl(copyMacro, "复制宏");
            RegisterUiSafetyControl(delMacro, "删除宏");
            RegisterUiSafetyControl(upMacro, "调整宏列表顺序");
            RegisterUiSafetyControl(downMacro, "调整宏列表顺序");
            RegisterUiSafetyControl(nameBox, "编辑名称");
            RegisterUiSafetyControl(descriptionBox, "编辑备注");
            RegisterUiSafetyControl(enabledBox, "修改全局触发设置");
            RegisterUiSafetyControl(triggerBox, "触发键设置");
            RegisterUiSafetyControl(captureTriggerButton, "录制触发键");
            RegisterUiSafetyControl(triggerModeBox, "修改触发方式");
            RegisterUiSafetyControl(activatorConditionButton, "修改时间与条件");
            RegisterUiSafetyControl(suppressBox, "修改触发屏蔽设置");
            RegisterUiSafetyControl(repeatBox, "修改执行次数");
            RegisterUiSafetyControl(infiniteBox, "修改循环设置");
            RegisterUiSafetyControl(lockTargetButton, "锁定目标窗口");
            RegisterUiSafetyControl(clearTargetButton, "清除目标窗口");
            RegisterUiSafetyControl(grid, "编辑宏步骤");
            RegisterUiSafetyControl(addStep, "添加宏步骤");
            RegisterUiSafetyControl(editStep, "编辑宏步骤");
            RegisterUiSafetyControl(copyStep, "复制宏步骤");
            RegisterUiSafetyControl(delStep, "删除宏步骤");
            RegisterUiSafetyControl(upStep, "调整宏步骤顺序");
            RegisterUiSafetyControl(downStep, "调整宏步骤顺序");
            RegisterUiSafetyControl(batchDelayStep, "批量修改步骤间隔");

            inputSink = new InputSinkControl();
            inputSink.Size = new Size(1, 1);
            split.Panel2.Controls.Add(inputSink);

            MouseEventHandler safeBackgroundClick = delegate
            {
                try { inputSink.Focus(); } catch { }
                UpdateUiSafetyPauseState();
            };

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 62;
            bottom.BackColor = Color.White;
            Controls.Add(bottom);
            bottom.SendToBack();

            TableLayoutPanel statusLayout = new TableLayoutPanel();
            statusLayout.Dock = DockStyle.Fill;
            statusLayout.Padding = new Padding(14, 7, 14, 6);
            statusLayout.ColumnCount = 3;
            statusLayout.RowCount = 2;
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            bottom.Controls.Add(statusLayout);

            statusLabel = new Label();
            statusLabel.Text = "状态：空闲";
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.AutoEllipsis = true;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.Font = new Font(Font, FontStyle.Bold);
            statusLabel.TextChanged += StatusLabel_TextChanged;
            statusLayout.Controls.Add(statusLabel, 0, 0);

            panicHintLabel = new Label();
            panicHintLabel.Dock = DockStyle.Fill;
            panicHintLabel.AutoEllipsis = true;
            panicHintLabel.TextAlign = ContentAlignment.MiddleLeft;
            panicHintLabel.ForeColor = SystemColors.GrayText;
            statusLayout.Controls.Add(panicHintLabel, 0, 1);
            // Running and global stop remain reachable while the editor scrolls.
            if (runButton.Parent != null) runButton.Parent.Controls.Remove(runButton);
            runButton.MinimumSize = new Size(180, 38);
            runButton.Anchor = AnchorStyles.Right;
            statusLayout.Controls.Add(runButton, 1, 0);
            statusLayout.SetRowSpan(runButton, 2);
            Button emergencyStopButton = MakeButton("全部停止", 0, 0, 112);
            emergencyStopButton.Anchor = AnchorStyles.Right;
            emergencyStopButton.Margin = new Padding(12, 0, 0, 0);
            emergencyStopButton.Click += delegate { EmergencyStop("ui-stop-all"); };
            statusLayout.Controls.Add(emergencyStopButton, 2, 0);
            statusLayout.SetRowSpan(emergencyStopButton, 2);
            saveWarning = new LinkLabel();
            saveWarning.Dock = DockStyle.Top;
            saveWarning.Height = 34;
            saveWarning.Padding = new Padding(14, 4, 14, 4);
            saveWarning.BackColor = Color.MistyRose;
            saveWarning.LinkColor = Color.Firebrick;
            saveWarning.AutoEllipsis = true;
            saveWarning.Visible = false;
            saveWarning.LinkClicked += delegate
            {
                if (!SaveConfig()) MessageBox.Show(this, saveFailure + "\r\n\r\n" + configPath,
                    VirtualKeyboardDialog.TextFor("未保存", "Not saved"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            Controls.Add(saveWarning);
            saveWarning.SendToBack();
            RegisterSafeBackgroundClicks(this, safeBackgroundClick);
            RefreshPanicUi();
            UpdateTargetSectionVisibility();
            CaptureLocalizableControlTexts(this);
            ApplyLanguageToMainUi(false);
            EditorTheme.Apply(this);
            EditorTheme.Primary(newMacroButton, false);
            RefreshEditorGuidance();
            UpdateRunButton();
            ApplyStatusVisuals();
        }

        private void CaptureLocalizableControlTexts(Control root)
        {
            if (root == null) return;
            foreach (Control child in root.Controls)
            {
                bool isStaticTextControl = child is Label || child is Button || child is CheckBox || child is RadioButton || child is GroupBox;
                bool isDynamic = child == workflowGuide || child == macroCountLabel || child == stepsHintLabel || child == statusLabel || child == panicHintLabel || child == runButton || child == recordButton || child == infiniteBox || child == captureTriggerButton || child == activatorConditionButton;
                if (isStaticTextControl && !isDynamic && !localizableControlTexts.ContainsKey(child))
                    localizableControlTexts.Add(child, child.Text ?? "");
                CaptureLocalizableControlTexts(child);
            }
        }

        private void ApplyLanguageToMainUi(bool userInitiated)
        {
            Text = AppInfo.ProductName + " " + AppInfo.Version + " - " + Localizer.T("键鼠与手柄宏工具");
            if (toolsButton != null) toolsButton.AccessibleName = Localizer.T("设置");

            foreach (KeyValuePair<Control, string> pair in localizableControlTexts)
            {
                try { if (pair.Key != null && !pair.Key.IsDisposed) pair.Key.Text = Localizer.T(pair.Value); }
                catch { }
            }

            int triggerModeIndex = triggerModeBox == null ? -1 : triggerModeBox.SelectedIndex;
            if (triggerModeBox != null)
            {
                loadingUi = true;
                triggerModeBox.Items.Clear();
                triggerModeBox.Items.AddRange(TriggerEditor.Names);
                if (triggerModeIndex >= 0 && triggerModeIndex < triggerModeBox.Items.Count) triggerModeBox.SelectedIndex = triggerModeIndex;
                loadingUi = false;
            }

            if (grid != null && grid.Columns.Count >= 5)
            {
                grid.Columns[0].HeaderText = "#";
                grid.Columns[1].HeaderText = Localizer.T("操作");
                grid.Columns[2].HeaderText = Localizer.T("按键 / 鼠标 / 手柄");
                grid.Columns[3].HeaderText = Localizer.IsEnglish ? "Hold (ms)" : "按住 (ms)";
                grid.Columns[4].HeaderText = Localizer.IsEnglish ? "Wait (ms)" : "等待 (ms)";
                grid.Columns[3].ToolTipText = Localizer.IsEnglish ? "How long to hold the input before releasing it. 1000 ms = 1 second." : "按住按键后，经过多久松开。1000 毫秒 = 1 秒。";
                grid.Columns[4].ToolTipText = Localizer.IsEnglish ? "How long to wait after this action before the next step." : "这个操作完成后，等待多久再执行下一步。";
            }

            RefreshMenuLanguage();
            RefreshToolTipsLanguage();
            RefreshTargetWindowUi();
            UpdateTriggerModeUiText();
            RefreshActivatorConditionUi();
            if (captureTriggerButton != null)
                captureTriggerButton.Text = captureMode == CaptureMode.Trigger ? Localizer.T("请按触发键…") : EditorDialogs.T("选择触发键…", "Choose shortcut…");
            RefreshSteps();
            RefreshPanicUi();
            UpdateRunButton();
            UpdateRecordButton();

            int selected = macroList == null ? -1 : macroList.SelectedIndex;
            if (macroList != null && config != null && config.Macros != null) RefreshMacroList(selected);
            RefreshStatusForLanguage();

            if (userInitiated)
            {
                config.Language = Localizer.Language;
                SaveConfig();
            }
        }

        private void RefreshStatusForLanguage()
        {
            if (statusLabel == null) return;
            if (recordingActive)
            {
                statusLabel.Text = Localizer.Dynamic("录制中：切到目标程序进行操作；回到 InputStitch 后点击“停止录制”。");
                return;
            }
            int runCount = ActiveMacroRunCount();
            int heldCount = ActiveParallelHeldMappingCount();
            if (runCount > 0 || heldCount > 0)
            {
                if (runCount > 0 && heldCount > 0)
                    statusLabel.Text = Localizer.IsEnglish
                        ? "Active runtime: " + runCount.ToString() + " macro run(s), " + heldCount.ToString() + " Held Mapping(s)"
                        : "活动运行：" + runCount.ToString() + " 个宏运行实例，" + heldCount.ToString() + " 个按住映射";
                else if (runCount > 0)
                    statusLabel.Text = runCount == 1
                        ? Localizer.Dynamic("正在执行：1 个宏")
                        : (Localizer.IsEnglish ? "Running macros: " + runCount.ToString() : "正在执行的宏：" + runCount.ToString() + " 个");
                else
                    statusLabel.Text = Localizer.IsEnglish ? "Held mappings active: " + heldCount.ToString() : "按住映射活动：" + heldCount.ToString();
            }
            else if (manualTriggerSuspend)
                statusLabel.Text = Localizer.Dynamic("状态：全局宏触发已暂停；紧急停止键仍有效。");
            else
                statusLabel.Text = Localizer.Dynamic("状态：空闲");
        }

        private void ChangeLanguage(string language)
        {
            string normalized = string.Equals(language, Localizer.English, StringComparison.OrdinalIgnoreCase) ? Localizer.English : Localizer.Chinese;
            if (string.Equals(Localizer.Language, normalized, StringComparison.OrdinalIgnoreCase)) return;
            Localizer.SetLanguage(normalized);
            if (config != null) config.Language = normalized;
            ApplyLanguageToMainUi(true);
        }

        private void RefreshMenuLanguage()
        {
            if (settingsMenuItem != null) settingsMenuItem.Text = Localizer.T("设置...");
            if (suspendTriggersMenuItem != null) suspendTriggersMenuItem.Text = Localizer.T("暂停全局宏触发");
            if (minimizeToTrayMenuItem != null) minimizeToTrayMenuItem.Text = Localizer.T("最小化到系统托盘");
            if (languageMenuItem != null) languageMenuItem.Text = Localizer.T("语言 / Language");
            if (chineseLanguageMenuItem != null)
            {
                chineseLanguageMenuItem.Text = Localizer.IsEnglish ? "Simplified Chinese" : "简体中文";
                chineseLanguageMenuItem.Checked = !Localizer.IsEnglish;
            }
            if (englishLanguageMenuItem != null)
            {
                englishLanguageMenuItem.Text = "English";
                englishLanguageMenuItem.Checked = Localizer.IsEnglish;
            }
            if (runtimeObservationMenuItem != null) runtimeObservationMenuItem.Text = Localizer.IsEnglish ? "Runtime observation..." : "运行观察...";
            if (deviceManagerMenuItem != null) deviceManagerMenuItem.Text = Localizer.IsEnglish ? "Device Manager..." : "设备管理器...";
            if (controllerTakeoverMenuItem != null) controllerTakeoverMenuItem.Text = Localizer.IsEnglish ? "Controller takeover (Experimental)..." : "手柄接管（实验）...";
            if (layerManageButton != null) layerManageButton.Text = Localizer.IsEnglish ? "Manage layers..." : "管理映射层…";
            if (diagnosticsMenuItem != null) diagnosticsMenuItem.Text = Localizer.T("诊断信息...");
            if (openConfigFolderMenuItem != null) openConfigFolderMenuItem.Text = Localizer.T("打开配置文件夹");
            if (openLogMenuItem != null) openLogMenuItem.Text = Localizer.T("打开日志文件夹");
            if (githubMenuItem != null) githubMenuItem.Text = Localizer.T("打开项目 GitHub");
            if (aboutMenuItem != null) aboutMenuItem.Text = Localizer.Dynamic("关于 " + AppInfo.ProductName + "...");
            if (trayShowMenuItem != null) trayShowMenuItem.Text = Localizer.Dynamic("显示 " + AppInfo.ProductName);
            if (trayPanicMenuItem != null) trayPanicMenuItem.Text = Localizer.T("紧急停止");
            if (traySuspendMenuItem != null) traySuspendMenuItem.Text = Localizer.T("暂停全局宏触发");
            if (trayExitMenuItem != null) trayExitMenuItem.Text = Localizer.T("退出");
            if (panicMenuItem != null && captureMode != CaptureMode.PanicTrigger)
            {
                string panicText = InputNames.FormatTrigger(config == null ? null : config.PanicTrigger);
                panicMenuItem.Text = Localizer.IsEnglish
                    ? "Set Emergency Stop hotkey... (current: " + panicText + ")"
                    : "设置紧急停止键...（当前：" + panicText + "）";
            }
        }

        private void RefreshToolTipsLanguage()
        {
            if (uiToolTip == null) return;
            foreach (KeyValuePair<Control, string> pair in toolTipSourceTexts)
            {
                try
                {
                    if (pair.Key != null && !pair.Key.IsDisposed)
                        uiToolTip.SetToolTip(pair.Key, WrapToolTip(Localizer.T(pair.Value), Localizer.IsEnglish ? 440 : 360));
                }
                catch { }
            }
        }

        private void StatusLabel_TextChanged(object sender, EventArgs e)
        {
            if (statusLabel == null) return;
            if (!translatingStatusText && Localizer.IsEnglish)
            {
                string translated = Localizer.Dynamic(statusLabel.Text ?? "");
                if (!string.Equals(translated, statusLabel.Text, StringComparison.Ordinal))
                {
                    translatingStatusText = true;
                    statusLabel.Text = translated;
                    translatingStatusText = false;
                }
            }
            ApplyStatusVisuals();
        }

        private void BuildToolsMenu()
        {
            toolsMenu = new ContextMenuStrip();
            settingsMenuItem = new ToolStripMenuItem();
            settingsMenuItem.Click += delegate { ShowSettingsDialog(); };
            toolsMenu.Items.Add(settingsMenuItem);
            toolsMenu.Items.Add(new ToolStripSeparator());

            panicMenuItem = new ToolStripMenuItem();
            panicMenuItem.Click += delegate { BeginPanicTriggerCapture(); };
            toolsMenu.Items.Add(panicMenuItem);

            suspendTriggersMenuItem = new ToolStripMenuItem();
            suspendTriggersMenuItem.CheckOnClick = true;
            suspendTriggersMenuItem.CheckedChanged += delegate
            {
                manualTriggerSuspend = suspendTriggersMenuItem.Checked;
                UpdateTrayMenuChecks();
                statusLabel.Text = manualTriggerSuspend ? "状态：全局宏触发已暂停；紧急停止键仍有效。" : "状态：全局宏触发已恢复。";
            };
            toolsMenu.Items.Add(suspendTriggersMenuItem);

            minimizeToTrayMenuItem = new ToolStripMenuItem();
            minimizeToTrayMenuItem.CheckOnClick = true;
            minimizeToTrayMenuItem.Checked = config.MinimizeToTray;
            minimizeToTrayMenuItem.CheckedChanged += delegate
            {
                if (loadingUi) return;
                config.MinimizeToTray = minimizeToTrayMenuItem.Checked;
                SaveConfig();
            };
            toolsMenu.Items.Add(minimizeToTrayMenuItem);

            languageMenuItem = new ToolStripMenuItem();
            chineseLanguageMenuItem = new ToolStripMenuItem("简体中文");
            englishLanguageMenuItem = new ToolStripMenuItem("English");
            chineseLanguageMenuItem.Click += delegate { ChangeLanguage(Localizer.Chinese); };
            englishLanguageMenuItem.Click += delegate { ChangeLanguage(Localizer.English); };
            languageMenuItem.DropDownItems.Add(chineseLanguageMenuItem);
            languageMenuItem.DropDownItems.Add(englishLanguageMenuItem);
            toolsMenu.Items.Add(languageMenuItem);

            toolsMenu.Items.Add(new ToolStripSeparator());

            runtimeObservationMenuItem = new ToolStripMenuItem();
            runtimeObservationMenuItem.Click += delegate
            {
                if (runtimeObservationForm != null && !runtimeObservationForm.IsDisposed)
                {
                    runtimeObservationForm.BringToFront();
                    runtimeObservationForm.Activate();
                    return;
                }
                runtimeObservationForm = new DiagnosticsForm(BuildRuntimeObservationText);
                runtimeObservationForm.Text = Localizer.IsEnglish ? AppInfo.ProductName + " - Runtime observation" : AppInfo.ProductName + " - 运行观察";
                runtimeObservationForm.FormClosed += delegate { runtimeObservationForm = null; };
                runtimeObservationForm.Show(this);
            };
            toolsMenu.Items.Add(runtimeObservationMenuItem);

            deviceManagerMenuItem = new ToolStripMenuItem();
            deviceManagerMenuItem.Click += delegate { ShowDeviceManagerDialog(); };
            toolsMenu.Items.Add(deviceManagerMenuItem);

            controllerTakeoverMenuItem = new ToolStripMenuItem();
            controllerTakeoverMenuItem.Click += delegate { ShowControllerTakeoverDialog(); };
            toolsMenu.Items.Add(controllerTakeoverMenuItem);

            diagnosticsMenuItem = new ToolStripMenuItem();
            diagnosticsMenuItem.Click += delegate
            {
                uiSafetyModalDepth++;
                UpdateUiSafetyPauseState();
                try
                {
                    using (DiagnosticsForm f = new DiagnosticsForm(BuildDiagnosticsText)) f.ShowDialog(this);
                }
                finally
                {
                    uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                    UpdateUiSafetyPauseState();
                }
            };
            toolsMenu.Items.Add(diagnosticsMenuItem);

            openConfigFolderMenuItem = new ToolStripMenuItem();
            openConfigFolderMenuItem.Click += OpenConfigFolderButton_Click;
            toolsMenu.Items.Add(openConfigFolderMenuItem);

            openLogMenuItem = new ToolStripMenuItem();
            openLogMenuItem.Click += delegate
            {
                try
                {
                    Directory.CreateDirectory(AppPaths.Logs);
                    Process.Start("explorer.exe", AppPaths.Logs);
                }
                catch (Exception ex) { AppLog.Write("Open log folder failed", ex); }
            };
            toolsMenu.Items.Add(openLogMenuItem);

            githubMenuItem = new ToolStripMenuItem();
            githubMenuItem.Click += delegate
            {
                try { Process.Start(new ProcessStartInfo(AppInfo.ProjectUrl) { UseShellExecute = true }); }
                catch (Exception ex)
                {
                    AppLog.Write("Open project GitHub failed", ex);
                    LocalizedMessageBox.Show(this, AppInfo.ProjectUrl, AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            aboutMenuItem = new ToolStripMenuItem();
            aboutMenuItem.Click += delegate
            {
                uiSafetyModalDepth++;
                UpdateUiSafetyPauseState();
                try { using (AboutForm f = new AboutForm()) f.ShowDialog(this); }
                finally
                {
                    uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                    UpdateUiSafetyPauseState();
                }
            };
            toolsMenu.Items.Add(new ToolStripSeparator());
            toolsMenu.Items.Add(githubMenuItem);
            toolsMenu.Items.Add(aboutMenuItem);
            toolsMenu.Opening += delegate { RefreshPanicUi(); RefreshMenuLanguage(); };
            RefreshMenuLanguage();
        }

        internal static void DeferContextMenuDispose(Control dispatcher, ContextMenuStrip menu)
        {
            if (menu == null || menu.IsDisposed) return;
            if (dispatcher == null || dispatcher.IsDisposed || dispatcher.Disposing || !dispatcher.IsHandleCreated) return;
            try
            {
                dispatcher.BeginInvoke((MethodInvoker)delegate
                {
                    if (!menu.IsDisposed) menu.Dispose();
                });
            }
            catch (InvalidOperationException) { }
        }

        private void ShowDeviceManagerDialog()
        {
            if (deviceIdentityService == null)
            {
                LocalizedMessageBox.Show(this,
                    Localizer.IsEnglish ? "Device Manager is unavailable in this host." : "当前环境不能使用设备管理器。",
                    AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            uiSafetyModalDepth++;
            UpdateUiSafetyPauseState();
            try
            {
                using (DeviceManagerDialog dialog = new DeviceManagerDialog(delegate { return deviceIdentityService.Refresh(); }))
                    dialog.ShowDialog(this);
            }
            finally
            {
                uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                UpdateUiSafetyPauseState();
            }
        }

        private void QueueDeviceIdentityRefresh()
        {
            if (deviceIdentityService == null) return;
            if (Interlocked.CompareExchange(ref deviceIdentityRefreshQueued, 1, 0) != 0) return;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try { deviceIdentityService.Refresh(); }
                catch (Exception ex) { AppLog.Write("Device identity refresh failed", ex); }
                finally { Interlocked.Exchange(ref deviceIdentityRefreshQueued, 0); }
            });
        }


        private void ShowSettingsDialog()
        {
            uiSafetyModalDepth++;
            UpdateUiSafetyPauseState();
            try
            {
                using (SettingsDialog dialog = new SettingsDialog(config, GetRecentIdleTargetCandidate,
                    delegate { return deviceIdentityService == null ? null : deviceIdentityService.Refresh(); }))
                {
                    dialog.CheckForUpdatesRequested += delegate { CheckForUpdatesAsync(dialog, false); };
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                    {
                        GamepadOutput.Configure(config.GamepadDeviceType);
                        if (GamepadOutput.IsConnected && !string.Equals(GamepadOutput.ConnectedType, config.GamepadDeviceType, StringComparison.OrdinalIgnoreCase))
                        {
                            try { GamepadOutput.EnsureConnected(config.GamepadDeviceType); } catch { }
                        }
                        return;
                    }
                    bool autoProfilesWasEnabled = config.AutoSwitchProfiles;
                    bool gamepadWasConnected = GamepadOutput.IsConnected;
                    bool routerWasEnabled = config.GamepadRouterEnabled;
                    bool virtualOutputWasDisabled = VirtualGamepadTypes.IsDisabled(config.GamepadDeviceType);
                    bool requestedRouterEnabled = dialog.SelectedGamepadRouterEnabled;
                    RouterSourcePolicyConfig requestedRouterSourcePolicy = dialog.SelectedGamepadRouterSourcePolicy;
                    RouterSourcePolicyConfig.Normalize(requestedRouterSourcePolicy);
                    if (controlledReplacement != null && controlledReplacement.Active &&
                        !string.Equals(requestedRouterSourcePolicy.Mode, RouterSourceModes.AllVisible, StringComparison.OrdinalIgnoreCase))
                    {
                        LocalizedMessageBox.Show(this,
                            Localizer.IsEnglish
                                ? "Experimental Controller Takeover currently requires Router source policy 'all visible'. Stop takeover before selecting individual routed devices."
                                : "实验性的“手柄接管”目前要求汇总来源保持为“所有可见来源”。请先停止手柄接管，再选择单独的汇总设备。",
                            AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    string requestedGamepadType = requestedRouterEnabled ? VirtualGamepadTypes.Xbox360 : dialog.SelectedGamepadType;
                    bool virtualOutputWillBeDisabled = VirtualGamepadTypes.IsDisabled(requestedGamepadType);
                    if (!virtualOutputWasDisabled && virtualOutputWillBeDisabled && !StopRuntimeForConfigChange()) return;

                    config.UseScanCodeInput = dialog.UseScanCodeInput;
                    config.PauseMacroInRiskyUi = dialog.PauseMacroInRiskyUi;
                    config.MinimizeToTray = dialog.MinimizeToTray;
                    config.KeepWindowTopMost = dialog.KeepWindowTopMost;
                    config.ActivateTargetWindowOnUiRun = dialog.ActivateTargetWindowOnUiRun;
                    config.UiRunStartDelayMs = dialog.UiRunStartDelayMs;
                    config.AutoSwitchProfiles = dialog.AutoSwitchProfiles;
                    config.UpdateMode = dialog.SelectedUpdateMode;
                    if (!requestedRouterEnabled && controlledReplacement != null && controlledReplacement.Active)
                    {
                        ControlledReplacementResult stopReplacement = StopControlledReplacement();
                        if (stopReplacement.State == ControlledReplacementState.Failed)
                        {
                            requestedRouterEnabled = true;
                            LocalizedMessageBox.Show(this,
                                Localizer.IsEnglish
                                    ? "Controller takeover could not be fully restored, so controller merging was kept enabled. " + stopReplacement.Message
                                    : "手柄接管未能完整恢复原设备，因此暂时保持“手柄汇总”开启。" + stopReplacement.Message,
                                AppInfo.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    config.GamepadRouterEnabled = requestedRouterEnabled;
                    config.GamepadRouterSourcePolicy = requestedRouterSourcePolicy;
                    config.GamepadDeviceType = config.GamepadRouterEnabled ? VirtualGamepadTypes.Xbox360 : dialog.SelectedGamepadType;
                    bool sdl3WasEnabled = config.Sdl3InputEnabled;
                    config.Sdl3InputEnabled = dialog.Sdl3InputEnabled;
                    IdleGamepadOptions requestedIdleOptions = dialog.SelectedIdleOptions;
                    bool idleDisabledByVirtualOutput = VirtualGamepadTypes.IsDisabled(config.GamepadDeviceType) &&
                        requestedIdleOptions != null && requestedIdleOptions.Enabled;
                    if (idleDisabledByVirtualOutput) requestedIdleOptions.Enabled = false;
                    config.IdleGamepad = requestedIdleOptions;
                    idleTargetWindowHandle = IntPtr.Zero;
                    ResolveIdleTargetWindowFromConfig();
                    idleKeyboardMouseActivityInScope = IsIdleKeyboardMouseActivityInScope(NativeWindowFocus.ForegroundWindow());
                    InputSender.UseScanCodeInput = config.UseScanCodeInput;
                    GamepadOutput.Configure(config.GamepadDeviceType);
                    if (idleGamepad != null) idleGamepad.Configure(config.IdleGamepad, true);
                    if (routerSourcePolicyEvaluator != null) routerSourcePolicyEvaluator.Update(config.GamepadRouterSourcePolicy);
                    if (gamepadRouter != null) gamepadRouter.Configure(config.GamepadRouterEnabled);
                    if (VirtualGamepadTypes.IsDisabled(config.GamepadDeviceType))
                    {
                        if (gamepadRouter != null) gamepadRouter.Stop("virtual-output-disabled");
                        GamepadOutput.NeutralizeAll();
                        GamepadOutput.Disconnect();
                    }
                    else
                    {
                        if (config.GamepadRouterEnabled)
                            EnsureGamepadRouterReady(false);
                        else if (routerWasEnabled && gamepadRouter != null)
                            gamepadRouter.Stop("disabled-by-user");
                        if (!config.GamepadRouterEnabled && gamepadWasConnected && !string.Equals(GamepadOutput.ConnectedType, config.GamepadDeviceType, StringComparison.OrdinalIgnoreCase))
                            EnsureGamepadReady(null, false);
                    }
                    TopMost = config.KeepWindowTopMost;
                    if (autoProfilesWasEnabled && !config.AutoSwitchProfiles) lastAutoProfileProcess = "";
                    UpdateTargetSectionVisibility();
                    if (minimizeToTrayMenuItem != null)
                    {
                        loadingUi = true;
                        minimizeToTrayMenuItem.Checked = config.MinimizeToTray;
                        loadingUi = false;
                    }
                    if (!string.Equals(config.Language, dialog.SelectedLanguage, StringComparison.OrdinalIgnoreCase))
                    {
                        config.Language = dialog.SelectedLanguage;
                        Localizer.SetLanguage(config.Language);
                        ApplyLanguageToMainUi(false);
                        EditorTheme.Apply(this);
                        EditorTheme.Primary(newMacroButton, false);
                        RefreshEditorGuidance();
                        UpdateRunButton();
                    }
                    UpdateUiSafetyPauseState();
                    SaveConfig();
                    if (sdl3WasEnabled != config.Sdl3InputEnabled)
                    {
                        SetStatusSafe(Localizer.IsEnglish
                            ? "SDL3 input lane setting saved; it takes effect after the next start."
                            : "SDL3 输入通道设置已保存；将在下次启动后生效。");
                    }
                    else if (VirtualGamepadTypes.IsDisabled(config.GamepadDeviceType))
                    {
                        SetStatusSafe(idleDisabledByVirtualOutput
                            ? (Localizer.IsEnglish
                                ? "Virtual controller creation is disabled; Idle gamepad input was also turned off."
                                : "已设置为不创建虚拟手柄；“闲置自动手柄输入”也已关闭。")
                            : (Localizer.IsEnglish
                                ? "Virtual controller creation is disabled. Keyboard/mouse features remain available."
                                : "已设置为不创建虚拟手柄；键盘/鼠标功能仍可正常使用。"));
                    }
                }
            }
            finally
            {
                uiSafetyModalDepth = Math.Max(0, uiSafetyModalDepth - 1);
                UpdateUiSafetyPauseState();
            }
        }

        private void CheckForUpdatesAsync(IWin32Window owner, bool automatic)
        {
            if (updateUiCoordinator == null)
                updateUiCoordinator = new UpdateUiCoordinator(this, statusLabel, EmergencyStop, delegate { SaveConfig(); });
            updateUiCoordinator.CheckForUpdatesAsync(owner, automatic);
        }

        private void UpdateTargetSectionVisibility()
        {
            if (targetGroup == null || config == null) return;
            bool shouldShow = optionalSettingsExpanded && (config.ActivateTargetWindowOnUiRun || config.AutoSwitchProfiles);
            // Assign unconditionally: before the form is shown, Control.Visible reports false
            // when an ancestor is hidden even if the control's own visibility flag is still true.
            targetGroup.Visible = shouldShow;
            TableLayoutPanel parent = targetGroup.Parent as TableLayoutPanel;
            if (parent != null)
            {
                int row = parent.GetRow(targetGroup);
                parent.RowStyles[row].SizeType = shouldShow ? SizeType.AutoSize : SizeType.Absolute;
                parent.RowStyles[row].Height = 0;
            }
        }

        private void BuildTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayShowMenuItem = new ToolStripMenuItem();
            trayShowMenuItem.Click += delegate { RestoreFromTray(); };
            trayMenu.Items.Add(trayShowMenuItem);

            trayPanicMenuItem = new ToolStripMenuItem();
            trayPanicMenuItem.Click += delegate { EmergencyStop("托盘紧急停止"); };
            trayMenu.Items.Add(trayPanicMenuItem);

            traySuspendMenuItem = new ToolStripMenuItem();
            traySuspendMenuItem.CheckOnClick = true;
            traySuspendMenuItem.Tag = "suspend";
            traySuspendMenuItem.CheckedChanged += delegate
            {
                if (traySuspendMenuItem.Checked == manualTriggerSuspend) return;
                manualTriggerSuspend = traySuspendMenuItem.Checked;
                if (suspendTriggersMenuItem != null) suspendTriggersMenuItem.Checked = manualTriggerSuspend;
            };
            trayMenu.Items.Add(traySuspendMenuItem);
            trayMenu.Items.Add(new ToolStripSeparator());

            trayExitMenuItem = new ToolStripMenuItem();
            trayExitMenuItem.Click += delegate { Close(); };
            trayMenu.Items.Add(trayExitMenuItem);

            trayIcon = new NotifyIcon();
            try
            {
                trayIcon.Icon = Icon != null ? Icon : Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
            trayIcon.Text = AppInfo.ProductName + " " + AppInfo.Version;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate { RestoreFromTray(); };
            trayMenu.Opening += delegate { UpdateTrayMenuChecks(); RefreshMenuLanguage(); };
            RefreshMenuLanguage();
        }

        private void UpdateTrayMenuChecks()
        {
            if (trayMenu == null) return;
            foreach (ToolStripItem item in trayMenu.Items)
            {
                ToolStripMenuItem mi = item as ToolStripMenuItem;
                if (mi != null && object.Equals(mi.Tag, "suspend")) mi.Checked = manualTriggerSuspend;
            }
            if (suspendTriggersMenuItem != null && suspendTriggersMenuItem.Checked != manualTriggerSuspend)
                suspendTriggersMenuItem.Checked = manualTriggerSuspend;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized && config != null && config.MinimizeToTray)
            {
                BeginInvoke((MethodInvoker)delegate { Hide(); });
            }
        }

        private void RestoreFromTray()
        {
            try
            {
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
            }
            catch { }
        }

        private void BeginPanicTriggerCapture()
        {
            if (captureMode == CaptureMode.PanicTrigger)
            {
                CancelCapture();
                return;
            }
            captureMode = CaptureMode.PanicTrigger;
            if (panicMenuItem != null) panicMenuItem.Text = Localizer.T("请按新的紧急停止键…");
            statusLabel.Text = "状态：正在录制紧急停止键（按 Esc 取消）";
            UpdateUiSafetyPauseState();
        }

    }
}
