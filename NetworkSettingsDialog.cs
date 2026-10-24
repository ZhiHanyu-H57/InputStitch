using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace InputStitch
{
    internal sealed class NetworkSettingsDialog : Form
    {
        private readonly NetworkControlOptions draft;
        private readonly CheckBox enabled, blockSound, restoreSound;
        private readonly ComboBox mode;
        private readonly TextBox path;
        private readonly Button save, browse;
        private readonly Label feedback, shortcutHint, count, audioFeedback;
        private readonly ListBox applications;
        private readonly Panel applicationPanel;
        private readonly TableLayoutPanel scopeFields;
        private readonly int applicationsRow;
        private readonly NumericUpDown blockVolume, restoreVolume;
        private readonly Func<NetworkControlOptions, string> validate;
        private bool loading;
        internal NetworkControlOptions Result { get; private set; }
        private static string T(string zh, string en) { return ToolFields.T(zh, en); }
        internal NetworkSettingsDialog(NetworkControlOptions options, Func<NetworkControlOptions, string> validator)
        {
            draft = (options ?? new NetworkControlOptions()).Clone(); draft.Normalize(); validate = validator;
            TriggerSpec oldBlock = draft.BlockShortcut, oldRestore = draft.RestoreShortcut;
            draft.BlockHotkey = oldBlock == null ? null : oldBlock.Clone(); draft.RestoreHotkey = oldRestore == null ? null : oldRestore.Clone(); draft.Hotkey = null;
            Font = EditorTheme.BodyFont(); AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
            Text = T("断网与恢复", "Network control"); ClientSize = new Size(750, 660); MinimumSize = new Size(650, 520);
            StartPosition = FormStartPosition.CenterParent; MinimizeBox = MaximizeBox = false;
            EditorTabs tabs = new EditorTabs { Dock = DockStyle.Fill, Name = "networkTabs" };
            TabPage scope = new TabPage(T("范围与快捷键", "Scope & hotkeys")), sound = new TabPage(T("音效", "Sounds")); tabs.TabPages.AddRange(new[] { scope, sound });
            TableLayoutPanel fields = ToolFields.Page(scope), sounds = ToolFields.Page(sound); scopeFields = fields;
            enabled = ToolFields.Check(T("启用网络控制快捷键", "Enable network hotkeys")); enabled.Checked = draft.Enabled; ToolFields.Row(fields, "", enabled);
            mode = new EditorChoice { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left, Width = 220, Name = "networkMode" };
            mode.Items.AddRange(new object[] { T("指定程序", "Selected applications"), T("整台电脑", "Whole computer") }); mode.SelectedIndex = draft.Mode == NetworkBlockMode.EntireComputer ? 1 : 0;
            ToolFields.Row(fields, T("断网范围", "Block scope"), mode);
            applicationPanel = new Panel { Dock = DockStyle.Top, AutoSize = true };
            TableLayoutPanel app = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2 };
            app.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); app.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            applications = new ListBox { Dock = DockStyle.Top, Height = 96, HorizontalScrollbar = true, IntegralHeight = false, DrawMode = DrawMode.OwnerDrawFixed, Name = "networkApplications" };
            applications.FontChanged += delegate { LayoutApplications(); };
            applications.DrawItem += delegate(object sender, DrawItemEventArgs e)
            {
                e.DrawBackground();
                if (e.Index >= 0 && e.Index < applications.Items.Count) TextRenderer.DrawText(e.Graphics, (string)applications.Items[e.Index], e.Font, new Rectangle(e.Bounds.X + 4, e.Bounds.Y, Math.Max(1, e.Bounds.Width - 8), e.Bounds.Height), e.ForeColor, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                e.DrawFocusRectangle();
            };
            foreach (string item in draft.Targets()) applications.Items.Add(item ?? ""); app.Controls.Add(applications, 0, 0); app.SetColumnSpan(applications, 2);
            path = new TextBox { Dock = DockStyle.Top, Name = "networkExecutable" };
            browse = ToolFields.Button(T("替换…", "Replace…")); browse.Click += delegate { ChooseApplications(false); };
            app.Controls.Add(path, 0, 1); app.Controls.Add(browse, 1, 1);
            Button add = ToolFields.Button(T("添加程序…", "Add apps…")), remove = ToolFields.Button(T("移除", "Remove"));
            add.Name = "networkAddApplications"; remove.Name = "networkRemoveApplication";
            add.Click += delegate { ChooseApplications(true); };
            remove.Click += delegate
            {
                int selected = applications.SelectedIndex; if (selected < 0) return;
                loading = true; applications.Items.RemoveAt(selected); path.Text = ""; loading = false;
                if (applications.Items.Count > 0) applications.SelectedIndex = Math.Min(selected, applications.Items.Count - 1); Changed();
            };
            count = ToolFields.Help(""); FlowLayoutPanel commands = ToolFields.Flow(add, remove, count);
            app.Controls.Add(commands, 0, 2); app.SetColumnSpan(commands, 2); applicationPanel.Controls.Add(app);
            applicationsRow = fields.RowCount; ToolFields.Row(fields, T("程序列表", "Applications"), applicationPanel);
            ToolFields.Row(fields, T("断开网络", "Block traffic"), ToolFields.Shortcut(this, "networkBlockHotkey", () => draft.BlockHotkey, v => draft.BlockHotkey = v, Changed));
            ToolFields.Row(fields, T("恢复网络", "Restore traffic"), ToolFields.Shortcut(this, "networkRestoreHotkey", () => draft.RestoreHotkey, v => draft.RestoreHotkey = v, Changed));
            shortcutHint = ToolFields.Help(""); ToolFields.Row(fields, "", shortcutHint);
            ToolFields.Row(fields, "", ToolFields.Help(T("首次断网需管理员授权。紧急停止、设置中的“恢复网络”和退出程序均可恢复连接。", "First use needs administrator approval. Emergency Stop or exiting restores traffic.")));
            foreach (Control field in fields.Controls) field.Margin = new Padding(field.Margin.Left, 1, field.Margin.Right, 1);
            blockSound = ToolFields.Check(T("启用断开音效", "Enable block cue")); blockSound.Name = "networkBlockSound"; blockSound.Checked = draft.BlockSound;
            restoreSound = ToolFields.Check(T("启用恢复音效", "Enable restore cue")); restoreSound.Name = "networkRestoreSound"; restoreSound.Checked = draft.RestoreSound;
            blockVolume = ToolFields.Number(0, 100, draft.BlockSoundVolume, 0, "networkBlockVolume"); blockVolume.Width = 72;
            restoreVolume = ToolFields.Number(0, 100, draft.RestoreSoundVolume, 0, "networkRestoreVolume"); restoreVolume.Width = 72;
            ToolFields.Row(sounds, T("断开网络", "Block traffic"), SoundEditor(NetworkSoundKind.Blocked, blockSound, blockVolume));
            ToolFields.Row(sounds, T("恢复网络", "Restore traffic"), SoundEditor(NetworkSoundKind.Restored, restoreSound, restoreVolume));
            ToolFields.Row(sounds, "", ToolFields.Help(T("支持 8/16 位 PCM WAV（单声道/立体声）。清除文件后使用内置音效。", "8/16-bit PCM WAV (mono/stereo). Clear a file to use the built-in cue.")));
            audioFeedback = ToolFields.Help(""); audioFeedback.Name = "networkAudioFeedback"; ToolFields.Row(sounds, "", audioFeedback);
            feedback = ToolFields.Help(""); feedback.Name = "networkFeedback";
            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); body.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); body.RowStyles.Add(new RowStyle(SizeType.AutoSize)); body.Controls.Add(tabs, 0, 0); body.Controls.Add(feedback, 0, 1);
            save = EditorDialogs.Button(T("保存", "Save"), DialogResult.None); save.Name = "networkSave";
            save.Click += delegate { Changed(); if (!save.Enabled) return; Result = CaptureOptions(); DialogResult = DialogResult.OK; Close(); };
            Button cancel = EditorDialogs.Button(Localizer.T("取消"), DialogResult.Cancel); AcceptButton = save; CancelButton = cancel;
            EditorDialogs.Frame(this, body, EditorDialogs.Actions(save, cancel), T("选择程序、快捷键和音效", "Choose applications, hotkeys and cues"), null);
            applications.SelectedIndexChanged += delegate { if (loading) return; loading = true; path.Text = applications.SelectedIndex < 0 ? "" : (string)applications.SelectedItem; loading = false; Changed(); };
            path.TextChanged += delegate
            {
                if (loading) return;
                int selected = applications.SelectedIndex;
                if (selected < 0 && applications.Items.Count == 0 && !string.IsNullOrWhiteSpace(path.Text))
                { loading = true; applications.Items.Add(path.Text); applications.SelectedIndex = selected = 0; loading = false; }
                if (selected >= 0 && (string)applications.Items[selected] != path.Text)
                { loading = true; applications.Items[selected] = path.Text; applications.SelectedIndex = selected; loading = false; }
                Changed();
            };
            enabled.CheckedChanged += delegate { Changed(); }; mode.SelectedIndexChanged += delegate { Changed(); };
            blockSound.CheckedChanged += delegate { Changed(); }; restoreSound.CheckedChanged += delegate { Changed(); };
            blockVolume.ValueChanged += delegate { Changed(); }; restoreVolume.ValueChanged += delegate { Changed(); };
            if (applications.Items.Count > 0) applications.SelectedIndex = 0; Changed();
        }
        private Control SoundEditor(NetworkSoundKind kind, CheckBox enabledCue, NumericUpDown volume)
        {
            bool block = kind == NetworkSoundKind.Blocked;
            TableLayoutPanel panel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 1, Margin = new Padding(0, 0, 0, 14) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); panel.Controls.Add(enabledCue);
            TextBox file = new TextBox { ReadOnly = true, Dock = DockStyle.Top, Name = block ? "networkBlockSoundFile" : "networkRestoreSoundFile" };
            Action refresh = delegate { string value = block ? draft.BlockSoundPath : draft.RestoreSoundPath; file.Text = string.IsNullOrEmpty(value) ? T("内置音效", "Built-in cue") : value; };
            refresh(); panel.Controls.Add(file);
            Button import = ToolFields.Button(T("导入…", "Import…")), clear = ToolFields.Button(T("清除文件", "Clear file")), preview = ToolFields.Button(T("试听", "Preview"));
            import.Name = block ? "networkBlockSoundImport" : "networkRestoreSoundImport"; clear.Name = block ? "networkBlockSoundClear" : "networkRestoreSoundClear";
            preview.Name = block ? "networkBlockSoundPreview" : "networkRestoreSoundPreview";
            import.Click += delegate
            {
                using (OpenFileDialog picker = new OpenFileDialog { Filter = T("PCM WAV 音效 (*.wav)|*.wav", "PCM WAV files (*.wav)|*.wav"), CheckFileExists = true })
                    if (picker.ShowDialog(this) == DialogResult.OK)
                    {
                        try
                        {
                            SetSoundFile(kind, picker.FileName);
                            audioFeedback.Text = ""; refresh(); Changed();
                        }
                        catch (Exception) { audioFeedback.Text = T("请选择可播放的 8/16 位 PCM WAV 音效。", "Choose a playable 8/16-bit PCM WAV cue."); audioFeedback.ForeColor = Color.Firebrick; }
                    }
            };
            clear.Click += delegate { if (block) draft.BlockSoundPath = ""; else draft.RestoreSoundPath = ""; refresh(); Changed(); };
            preview.Click += delegate
            {
                NetworkSounds.Preview(NetworkSoundCue.From(kind, CaptureOptions()), delegate(string error)
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    try { BeginInvoke((Action)delegate { if (!IsDisposed) { audioFeedback.Text = error.Length == 0 ? "" : T("音效无法播放，请检查文件是否存在且为 8/16 位 PCM WAV。", "Could not play cue. Check that the file exists and uses 8/16-bit PCM WAV."); audioFeedback.ForeColor = Color.Firebrick; } }); } catch (InvalidOperationException) { }
                });
            };
            panel.Controls.Add(ToolFields.Flow(import, clear, preview));
            TrackBar slider = new TrackBar { Minimum = 0, Maximum = 100, Value = (int)volume.Value, TickStyle = TickStyle.None, Width = 180, Height = 30, SmallChange = 1, LargeChange = 10, Name = block ? "networkBlockVolumeSlider" : "networkRestoreVolumeSlider" };
            slider.ValueChanged += delegate { volume.Value = slider.Value; }; volume.ValueChanged += delegate { slider.Value = (int)volume.Value; };
            panel.Controls.Add(ToolFields.Flow(new Label { Text = T("音量", "Volume"), AutoSize = true, Margin = new Padding(3, 8, 6, 0) }, slider, volume, new Label { Text = "%", AutoSize = true, Margin = new Padding(0, 8, 0, 0) }));
            return panel;
        }
        private void LayoutApplications()
        {
            applications.ItemHeight = Math.Max(24, applications.Font.Height + 6); int width = 0;
            foreach (string item in applications.Items) width = Math.Max(width, TextRenderer.MeasureText(item, applications.Font).Width + 8);
            applications.HorizontalExtent = width;
        }
        private void SetSoundFile(NetworkSoundKind kind, string file)
        {
            NetworkCuePlayer.ValidateFile(file);
            NativeNetworkAudio parser = new NativeNetworkAudio(); string opened = parser.Open(file); parser.Close(opened);
            if (kind == NetworkSoundKind.Blocked) draft.BlockSoundPath = file; else draft.RestoreSoundPath = file;
        }
        private void ChooseApplications(bool append)
        {
            using (OpenFileDialog picker = new OpenFileDialog { Filter = T("应用程序 (*.exe)|*.exe", "Applications (*.exe)|*.exe"), CheckFileExists = true, Multiselect = append })
            {
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                loading = true;
                if (append)
                {
                    if (applications.SelectedIndex >= 0) applications.Items[applications.SelectedIndex] = path.Text.Trim();
                    else if (applications.Items.Count == 0 && path.Text.Trim().Length > 0) applications.Items.Add(path.Text.Trim());
                    foreach (string file in picker.FileNames)
                    {
                        bool duplicate = false; foreach (string present in applications.Items) if (string.Equals(present, file, StringComparison.OrdinalIgnoreCase)) duplicate = true;
                        if (!duplicate && applications.Items.Count < NetworkPrivilegeHelper.MaximumApplications) applications.Items.Add(file);
                    }
                    applications.SelectedIndex = applications.Items.Count - 1;
                }
                else if (applications.SelectedIndex >= 0) applications.Items[applications.SelectedIndex] = picker.FileName;
                else { applications.Items.Add(picker.FileName); applications.SelectedIndex = 0; }
                path.Text = applications.SelectedIndex < 0 ? "" : (string)applications.SelectedItem; loading = false; Changed();
            }
        }
        private NetworkControlOptions CaptureOptions()
        {
            NetworkControlOptions o = draft.Clone(); o.Enabled = enabled.Checked; o.Mode = mode.SelectedIndex == 1 ? NetworkBlockMode.EntireComputer : NetworkBlockMode.Executable;
            o.ExecutablePaths = new List<string>(); foreach (string item in applications.Items) o.ExecutablePaths.Add(item);
            if (applications.SelectedIndex >= 0) o.ExecutablePaths[applications.SelectedIndex] = path.Text.Trim();
            else if (o.ExecutablePaths.Count == 0 && path.Text.Trim().Length > 0) o.ExecutablePaths.Add(path.Text.Trim());
            o.ExecutablePath = o.ExecutablePaths.Count == 0 ? "" : o.ExecutablePaths[0];
            o.BlockSound = blockSound.Checked; o.RestoreSound = restoreSound.Checked; o.BlockSoundVolume = (int)blockVolume.Value; o.RestoreSoundVolume = (int)restoreVolume.Value; o.Normalize(); return o;
        }
        private void Changed()
        {
            if (save == null || loading) return; LayoutApplications();
            NetworkControlOptions o = CaptureOptions(); applicationPanel.Enabled = mode.SelectedIndex == 0; EditorDialogs.RowVisible(scopeFields, applicationsRow, mode.SelectedIndex == 0);
            string error = "";
            if (o.Enabled && o.Mode == NetworkBlockMode.Executable) try { NetworkPrivilegeHelper.ValidateExecutables(o.Mode, o.Targets()); } catch (Exception) { error = T("请选择 1–16 个存在的程序文件。", "Choose 1–16 existing application files."); }
            if (error.Length == 0 && (!ToolFields.ValidShortcut(draft.BlockHotkey) || !ToolFields.ValidShortcut(draft.RestoreHotkey))) error = T("请选择包含普通键的键盘组合键。", "Choose a keyboard shortcut that includes a regular key.");
            if (error.Length == 0 && validate != null) error = validate(o);
            feedback.Text = error; feedback.ForeColor = Color.Firebrick; save.Enabled = error.Length == 0; count.Text = o.ExecutablePaths.Count + " / 16";
            bool same = o.BlockShortcut != null && o.RestoreShortcut != null && InputNames.FormatTrigger(o.BlockShortcut) == InputNames.FormatTrigger(o.RestoreShortcut);
            shortcutHint.Text = same ? T("两个快捷键相同：按一次断开，再按一次恢复。", "Same shortcut: press once to block, again to restore.") : T("断开与恢复分别使用上面的快捷键。", "Use the separate shortcuts above to block and restore traffic.");
        }
    }
}
