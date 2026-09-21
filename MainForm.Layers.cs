using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace InputStitch
{
    // Layer subsystem: display/management logic, the layer runtime state machine (toggle, momentary holds)
    // and context conditions. Extracted from InputStitch.cs as the first step of the incremental MainForm
    // decomposition (architecture-review finding F4); behaviour is unchanged.
    public partial class MainForm
    {
        private sealed class LayerChoice
        {
            public string Id;
            public string Text;
            public LayerChoice(string id, string text) { Id = id ?? ""; Text = text ?? ""; }
            public override string ToString() { return Text; }
        }

        // One physically held momentary layer switch. List order is the activation order; the newest
        // still-held entry is the effective layer. ReleaseProbeSince supports the physical-release
        // fallback when a hook or controller release edge is missed.
        private sealed class MomentaryLayerHold
        {
            public string LayerId;
            public long ReleaseProbeSince;
        }

        // Empty means only Base is eligible. This is intentionally runtime-only in Layer v1.
        private string activeMappingLayerId = "";
        // Underlying click-to-switch / UI selection. Momentary holds temporarily override this value.
        private string persistentMappingLayerId = "";
        // Momentary layer holds in activation order; the last still-valid entry is the effective layer.
        private readonly List<MomentaryLayerHold> momentaryLayerHolds = new List<MomentaryLayerHold>();
        // Test seam for Layer context evaluation; null means the real condition evaluator is used.
        private Func<MacroConditionConfig, bool> layerContextEvaluator = null;

        private string LayerDisplayName(MappingLayerDefinition layer)
        {
            if (layer == null) return "";
            string modeSuffix = layer.MomentarySwitch && layer.SwitchTrigger != null
                ? (Localizer.IsEnglish ? " (hold)" : "（按住）")
                : "";
            string contextSuffix = layer.ContextCondition == null || layer.ContextCondition.IsEmpty
                ? ""
                : (Localizer.IsEnglish ? " (context)" : "（上下文）");
            if (string.Equals(layer.Id, MappingLayerIds.Base, StringComparison.OrdinalIgnoreCase))
            {
                string baseName = Localizer.IsEnglish ? "Base layer" : "基础层（始终启用）";
                if (layer.SwitchTrigger != null) baseName += "  [" + InputNames.FormatTrigger(layer.SwitchTrigger) + "]" + modeSuffix;
                return baseName + contextSuffix;
            }
            string name = string.IsNullOrWhiteSpace(layer.Name) ? layer.Id : layer.Name;
            if (layer.SwitchTrigger != null) name += "  [" + InputNames.FormatTrigger(layer.SwitchTrigger) + "]" + modeSuffix;
            return name + contextSuffix;
        }

        private void RefreshLayerChoiceLists()
        {
            if (config == null || mappingLayerBox == null || activeLayerBox == null) return;
            NormalizeMappingLayers(config);
            bool oldLoading = loadingUi;
            loadingUi = true;
            try
            {
                mappingLayerBox.Items.Clear();
                activeLayerBox.Items.Clear();
                activeLayerBox.Items.Add(new LayerChoice("", Localizer.IsEnglish ? "Base only" : "仅基础层"));
                foreach (MappingLayerDefinition layer in config.MappingLayers)
                {
                    if (layer == null) continue;
                    LayerChoice choice = new LayerChoice(layer.Id, LayerDisplayName(layer));
                    mappingLayerBox.Items.Add(choice);
                    if (!string.Equals(layer.Id, MappingLayerIds.Base, StringComparison.OrdinalIgnoreCase))
                        activeLayerBox.Items.Add(new LayerChoice(layer.Id, LayerDisplayName(layer)));
                }
                int activeIndex = 0;
                for (int i = 0; i < activeLayerBox.Items.Count; i++)
                {
                    LayerChoice choice = activeLayerBox.Items[i] as LayerChoice;
                    if (choice != null && string.Equals(choice.Id, activeMappingLayerId, StringComparison.OrdinalIgnoreCase)) { activeIndex = i; break; }
                }
                activeLayerBox.SelectedIndex = activeIndex;
            }
            finally { loadingUi = oldLoading; }
        }

        private MappingLayerDefinition FindMappingLayer(string id)
        {
            if (config == null || config.MappingLayers == null) return null;
            string wanted = string.IsNullOrWhiteSpace(id) ? MappingLayerIds.Base : id;
            foreach (MappingLayerDefinition layer in config.MappingLayers)
                if (layer != null && string.Equals(layer.Id, wanted, StringComparison.OrdinalIgnoreCase)) return layer;
            return null;
        }

        private MappingLayerDefinition CurrentLayerManagementTarget()
        {
            LayerChoice choice = activeLayerBox == null ? null : activeLayerBox.SelectedItem as LayerChoice;
            return FindMappingLayer(choice == null ? activeMappingLayerId : choice.Id);
        }

        private string PromptLayerName(string title, string initialValue)
        {
            using (Form dialog = new Form())
            {
                dialog.Text = title;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ClientSize = new Size(420, 118);
                dialog.Font = Font;
                Label label = new Label { Left = 14, Top = 14, Width = 390, Text = Localizer.IsEnglish ? "Layer name:" : "映射层名称：" };
                TextBox input = new TextBox { Left = 14, Top = 38, Width = 390, Text = initialValue ?? "" };
                Button ok = new Button { Text = Localizer.IsEnglish ? "OK" : "确定", DialogResult = DialogResult.OK, Left = 248, Top = 76, Width = 76 };
                Button cancel = new Button { Text = Localizer.IsEnglish ? "Cancel" : "取消", DialogResult = DialogResult.Cancel, Left = 328, Top = 76, Width = 76 };
                dialog.Controls.Add(label); dialog.Controls.Add(input); dialog.Controls.Add(ok); dialog.Controls.Add(cancel);
                dialog.AcceptButton = ok; dialog.CancelButton = cancel;
                dialog.Shown += delegate { input.Focus(); input.SelectAll(); };
                if (dialog.ShowDialog(this) != DialogResult.OK) return null;
                string value = (input.Text ?? "").Trim();
                return value.Length == 0 ? null : value;
            }
        }

        private void AddMappingLayer()
        {
            int number = config == null || config.MappingLayers == null ? 1 : Math.Max(1, config.MappingLayers.Count);
            string defaultName = (Localizer.IsEnglish ? "Layer " : "映射层 ") + number.ToString();
            string name = PromptLayerName(Localizer.IsEnglish ? "Add mapping layer" : "新增映射层", defaultName);
            if (string.IsNullOrWhiteSpace(name)) return;
            NormalizeMappingLayers(config);
            config.MappingLayers.Add(new MappingLayerDefinition { Id = "layer-" + Guid.NewGuid().ToString("N"), Name = name, IsBase = false });
            SaveConfig();
            RefreshLayerChoiceLists();
            SetStatusSafe((Localizer.IsEnglish ? "Added mapping layer: " : "已新增映射层：") + name);
        }

        private void RenameMappingLayer(MappingLayerDefinition layer)
        {
            if (layer == null || layer.IsBase) return;
            string name = PromptLayerName(Localizer.IsEnglish ? "Rename mapping layer" : "重命名映射层", layer.Name);
            if (string.IsNullOrWhiteSpace(name)) return;
            layer.Name = name;
            SaveConfig();
            RefreshLayerChoiceLists();
            RefreshMacroList(macroList == null ? 0 : macroList.SelectedIndex);
        }

        private void DeleteMappingLayer(MappingLayerDefinition layer)
        {
            if (layer == null || layer.IsBase || config == null || config.MappingLayers == null) return;
            DialogResult confirm = MessageBox.Show(this,
                Localizer.IsEnglish
                    ? "Delete layer '" + layer.Name + "'? Macros assigned to it will move back to Base."
                    : "删除映射层“" + layer.Name + "”？属于该层的宏会自动移回基础层。",
                AppInfo.ProductName, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (confirm != DialogResult.OK) return;
            RemoveMappingLayerCore(layer);
            SaveConfig();
            RefreshMacroList(macroList == null ? 0 : macroList.SelectedIndex);
            SetStatusSafe(Localizer.IsEnglish ? "Mapping layer deleted; its macros moved to Base." : "映射层已删除；其中的宏已移回基础层。");
        }

        private void RemoveMappingLayerCore(MappingLayerDefinition layer)
        {
            if (layer == null || layer.IsBase || config == null || config.MappingLayers == null) return;
            lock (runLock)
            {
                momentaryLayerHolds.RemoveAll(delegate(MomentaryLayerHold hold)
                {
                    return hold == null || string.Equals(hold.LayerId, layer.Id, StringComparison.OrdinalIgnoreCase);
                });
                if (string.Equals(persistentMappingLayerId, layer.Id, StringComparison.OrdinalIgnoreCase)) persistentMappingLayerId = "";
            }
            if (string.Equals(activeMappingLayerId, layer.Id, StringComparison.OrdinalIgnoreCase)) ApplyEffectiveMappingLayer();
            foreach (MacroDefinition macro in config.Macros)
            {
                if (macro == null || !string.Equals(MacroLayerId(macro), layer.Id, StringComparison.OrdinalIgnoreCase)) continue;
                StopMacroForDefinitionChange(macro, "mapping-layer-delete");
                macro.MappingLayerId = MappingLayerIds.Base;
            }
            config.MappingLayers.Remove(layer);
        }

        private void BeginLayerSwitchCapture(MappingLayerDefinition layer)
        {
            if (layer == null) return;
            captureMode = CaptureMode.LayerSwitch;
            pendingModifierCapture = null;
            stepCaptureCallback = null;
            layerSwitchCaptureTarget = layer;
            SetStatusSafe((Localizer.IsEnglish ? "Press the shortcut for layer '" : "请按下映射层“") +
                (layer.IsBase ? (Localizer.IsEnglish ? "Base" : "基础层") : layer.Name) +
                (Localizer.IsEnglish ? "' (Esc cancels)." : "”的切换快捷键（Esc 取消）。"));
            UpdateUiSafetyPauseState();
        }

        private void ShowLayerManagementMenu()
        {
            MappingLayerDefinition target = CurrentLayerManagementTarget();
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem add = new ToolStripMenuItem(Localizer.IsEnglish ? "Add layer..." : "新增映射层…");
            add.Click += delegate { AddMappingLayer(); };
            menu.Items.Add(add);
            ToolStripMenuItem rename = new ToolStripMenuItem(Localizer.IsEnglish ? "Rename current layer..." : "重命名当前层…");
            rename.Enabled = target != null && !target.IsBase;
            rename.Click += delegate { RenameMappingLayer(target); };
            menu.Items.Add(rename);
            ToolStripMenuItem remove = new ToolStripMenuItem(Localizer.IsEnglish ? "Delete current layer" : "删除当前层");
            remove.Enabled = target != null && !target.IsBase;
            remove.Click += delegate { DeleteMappingLayer(target); };
            menu.Items.Add(remove);
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem shortcut = new ToolStripMenuItem(Localizer.IsEnglish ? "Set switch shortcut..." : "设置当前层切换快捷键…");
            shortcut.Enabled = target != null;
            shortcut.Click += delegate { BeginLayerSwitchCapture(target); };
            menu.Items.Add(shortcut);
            ToolStripMenuItem momentaryMode = new ToolStripMenuItem(Localizer.IsEnglish ? "Hold to activate (release returns)" : "按住生效（松开返回）");
            momentaryMode.CheckOnClick = false;
            momentaryMode.Checked = target != null && target.MomentarySwitch;
            momentaryMode.Enabled = target != null && target.SwitchTrigger != null;
            momentaryMode.Click += delegate
            {
                if (target == null) return;
                target.MomentarySwitch = !target.MomentarySwitch;
                SaveConfig();
                ApplyEffectiveMappingLayer();
                RefreshLayerChoiceLists();
                SetStatusSafe(target.MomentarySwitch
                    ? (Localizer.IsEnglish
                        ? "Layer '" + target.Name + "' now activates while its switch shortcut is held."
                        : "映射层“" + target.Name + "”已改为按住生效：松开切换键后返回上一层。")
                    : (Localizer.IsEnglish
                        ? "Layer '" + target.Name + "' switched back to click-to-switch."
                        : "映射层“" + target.Name + "”已改回按一下切换。"));
            };
            menu.Items.Add(momentaryMode);
            ToolStripMenuItem clearShortcut = new ToolStripMenuItem(Localizer.IsEnglish ? "Clear switch shortcut" : "清除当前层切换快捷键");
            clearShortcut.Enabled = target != null && target.SwitchTrigger != null;
            clearShortcut.Click += delegate
            {
                if (target == null) return;
                target.SwitchTrigger = null;
                target.MomentarySwitch = false;
                SaveConfig();
                ApplyEffectiveMappingLayer();
                RefreshLayerChoiceLists();
            };
            menu.Items.Add(clearShortcut);
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem contextCondition = new ToolStripMenuItem(Localizer.IsEnglish ? "Set context condition..." : "设置上下文条件…");
            contextCondition.Enabled = target != null;
            contextCondition.Click += delegate { EditLayerContextCondition(target); };
            menu.Items.Add(contextCondition);
            ToolStripMenuItem clearContextCondition = new ToolStripMenuItem(Localizer.IsEnglish ? "Clear context condition" : "清除上下文条件");
            clearContextCondition.Enabled = target != null && target.ContextCondition != null && !target.ContextCondition.IsEmpty;
            clearContextCondition.Click += delegate
            {
                if (target == null) return;
                target.ContextCondition = null;
                SaveConfig();
                ApplyEffectiveMappingLayer();
                RefreshLayerChoiceLists();
                SetStatusSafe(Localizer.IsEnglish ? "Layer context condition cleared." : "已清除该层的上下文条件。");
            };
            menu.Items.Add(clearContextCondition);
            menu.Closed += delegate { DeferContextMenuDispose(this, menu); };
            menu.Show(layerManageButton, new Point(0, layerManageButton.Height));
        }

        private static string MacroLayerId(MacroDefinition macro)
        {
            return macro == null || string.IsNullOrWhiteSpace(macro.MappingLayerId) ? MappingLayerIds.Base : macro.MappingLayerId;
        }

        private static bool IsMacroLayerEligibleFor(MacroDefinition macro, string activeLayerId)
        {
            if (macro == null) return false;
            string layerId = MacroLayerId(macro);
            if (string.Equals(layerId, MappingLayerIds.Base, StringComparison.OrdinalIgnoreCase)) return true;
            activeLayerId = activeLayerId ?? "";
            return activeLayerId.Length != 0 && string.Equals(layerId, activeLayerId, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsMacroLayerEligible(MacroDefinition macro)
        {
            return IsMacroLayerEligibleFor(macro, activeMappingLayerId);
        }

        private void SwitchActiveMappingLayer(string newLayerId)
        {
            newLayerId = newLayerId ?? "";
            if (string.Equals(newLayerId, activeMappingLayerId, StringComparison.OrdinalIgnoreCase)) return;
            string previousLayerId = activeMappingLayerId;

            // Every macro type participates in Layer eligibility. Stop anything that belonged to
            // the old active layer before publishing the new eligibility state. Worker-backed
            // sources are additionally suspended immediately so they cannot reassert output while
            // their thread is observing the stop request.
            List<MacroDefinition> toStop = new List<MacroDefinition>();
            Dictionary<MacroDefinition, string> workerSources = new Dictionary<MacroDefinition, string>();
            lock (runLock)
            {
                foreach (MacroDefinition macro in config.Macros)
                {
                    if (macro == null) continue;
                    bool wasEligible = IsMacroLayerEligibleFor(macro, previousLayerId);
                    bool willBeEligible = IsMacroLayerEligibleFor(macro, newLayerId);
                    if (!wasEligible || willBeEligible) continue;
                    toStop.Add(macro);
                    MacroRunRuntime runtime = GetMacroRun_NoLock(macro);
                    if (runtime != null && !string.IsNullOrWhiteSpace(runtime.SourceId))
                        workerSources[macro] = runtime.SourceId;
                }
            }
            foreach (MacroDefinition macro in toStop)
            {
                StopMacroForDefinitionChange(macro, "layer-switch");
                string sourceId;
                if (workerSources.TryGetValue(macro, out sourceId))
                {
                    try { outputOwnership.SuspendSource(sourceId); }
                    catch (Exception ex) { HandleOutputOwnershipFailure(ex, "layer-switch-suspend"); }
                }
            }

            lock (runLock)
            {
                layerBlockedUntilRelease.Clear();
                foreach (MacroDefinition macro in config.Macros)
                {
                    if (macro == null) continue;
                    bool wasEligible = IsMacroLayerEligibleFor(macro, previousLayerId);
                    bool willBeEligible = IsMacroLayerEligibleFor(macro, newLayerId);
                    // A key/button already held while a layer becomes eligible must not create an
                    // activation through key-repeat or a later poll. Require a real release first.
                    if (!wasEligible && willBeEligible && IsPhysicalHoldTriggerSatisfied(macro.Trigger))
                        layerBlockedUntilRelease.Add(macro);
                }
                activeMappingLayerId = newLayerId;
            }
            runtimeTrace.Add("layer-switch", newLayerId.Length == 0 ? "base-only" : newLayerId);
            RefreshRuntimeCapabilityUi();
            MappingLayerDefinition activeLayerDefinition = FindMappingLayer(newLayerId);
            string activeLayerName = activeLayerDefinition == null ? newLayerId : (string.IsNullOrWhiteSpace(activeLayerDefinition.Name) ? activeLayerDefinition.Id : activeLayerDefinition.Name);
            SetStatusSafe(newLayerId.Length == 0
                ? (Localizer.IsEnglish ? "Layer: Base only." : "映射层：当前仅启用基础层。")
                : (Localizer.IsEnglish ? "Active layer: " : "当前映射层：") + activeLayerName + (Localizer.IsEnglish ? "." : "。"));
        }

        private MappingLayerDefinition FindMomentaryLayer(string layerId)
        {
            MappingLayerDefinition layer = FindMappingLayer(layerId);
            return layer != null && layer.MomentarySwitch ? layer : null;
        }

        // True when the effective layer id currently comes from a momentary hold. An empty effective
        // id is the Base-only state, which a momentary Base-layer shortcut can also hold temporarily.
        private bool IsMomentaryLayerHoldActive(string effectiveLayerId)
        {
            effectiveLayerId = effectiveLayerId ?? "";
            lock (runLock)
            {
                foreach (MomentaryLayerHold hold in momentaryLayerHolds)
                {
                    if (hold == null) continue;
                    MappingLayerDefinition layer = FindMappingLayer(hold.LayerId);
                    if (layer == null || !layer.MomentarySwitch) continue;
                    if (string.Equals(layer.IsBase ? "" : layer.Id, effectiveLayerId, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }

        private int MomentaryLayerHoldCount()
        {
            lock (runLock) return momentaryLayerHolds.Count;
        }

        private bool IsLayerContextSatisfied(MappingLayerDefinition layer)
        {
            if (layer == null) return false;
            MacroConditionConfig condition = layer.ContextCondition;
            if (condition == null || condition.IsEmpty) return false;
            Func<MacroConditionConfig, bool> evaluator = layerContextEvaluator;
            if (evaluator != null) return evaluator(condition);
            return AreConditionsSatisfied(condition, -1);
        }

        // The first matching context layer in configured list order wins; the choice is deterministic and
        // visible in diagnostics. Base may be a context target, which means "temporarily Base-only".
        private MappingLayerDefinition FindMatchingContextLayer_NoLock()
        {
            if (config == null || config.MappingLayers == null) return null;
            foreach (MappingLayerDefinition layer in config.MappingLayers)
                if (IsLayerContextSatisfied(layer)) return layer;
            return null;
        }

        private static string DescribeLayerContext(MappingLayerDefinition layer)
        {
            MacroConditionConfig condition = layer == null ? null : layer.ContextCondition;
            if (condition == null || condition.IsEmpty) return "none";
            StringBuilder text = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(condition.ForegroundProcessName))
                text.Append("process=").Append(condition.ForegroundProcessName.Trim());
            if (!string.IsNullOrWhiteSpace(condition.ForegroundTitleContains))
            {
                if (text.Length != 0) text.Append('+');
                text.Append("title~").Append(condition.ForegroundTitleContains.Trim());
            }
            if (!string.IsNullOrWhiteSpace(condition.DeviceKey))
            {
                if (text.Length != 0) text.Append('+');
                text.Append("device=").Append(condition.DeviceKey.Trim());
            }
            if (condition.Analog != null && condition.Analog.Enabled)
            {
                if (text.Length != 0) text.Append('+');
                text.Append("analog");
            }
            return text.Length == 0 ? "none" : text.ToString();
        }

        // The effective layer is the newest still-valid momentary hold, otherwise the persistent
        // click-to-switch / UI selection. Callers must hold runLock.
        private string ComputeEffectiveMappingLayerId_NoLock()
        {
            for (int i = momentaryLayerHolds.Count - 1; i >= 0; i--)
            {
                MomentaryLayerHold hold = momentaryLayerHolds[i];
                if (hold == null)
                {
                    momentaryLayerHolds.RemoveAt(i);
                    continue;
                }
                MappingLayerDefinition layer = FindMappingLayer(hold.LayerId);
                if (layer == null || !layer.MomentarySwitch || layer.SwitchTrigger == null)
                {
                    // The layer was deleted, or converted back to click-to-switch, while held.
                    momentaryLayerHolds.RemoveAt(i);
                    runtimeTrace.Add("momentary-layer-drop", hold.LayerId ?? "null");
                    continue;
                }
                return layer.IsBase ? "" : layer.Id;
            }
            MappingLayerDefinition contextLayer = FindMatchingContextLayer_NoLock();
            if (contextLayer != null) return contextLayer.IsBase ? "" : contextLayer.Id;
            return persistentMappingLayerId ?? "";
        }

        private void ApplyEffectiveMappingLayer()
        {
            string desired;
            lock (runLock) { desired = ComputeEffectiveMappingLayerId_NoLock(); }
            SwitchActiveMappingLayer(desired);
        }

        // Periodic context re-evaluation (foreground timer). It only applies a transition when the effective
        // layer actually changes, so the 20 Hz foreground tick stays cheap.
        private void EvaluateContextLayers()
        {
            string desired;
            lock (runLock) { desired = ComputeEffectiveMappingLayerId_NoLock(); }
            if (string.Equals(desired, activeMappingLayerId, StringComparison.OrdinalIgnoreCase)) return;
            ApplyEffectiveMappingLayer();
            RefreshLayerChoiceLists();
        }

        // True when the effective layer currently comes from a matching context condition (and not from a
        // momentary hold). Used by Runtime Observation.
        private bool IsActiveLayerContextDriven()
        {
            lock (runLock)
            {
                if (IsMomentaryLayerHoldActive(activeMappingLayerId)) return false;
                MappingLayerDefinition contextLayer = FindMatchingContextLayer_NoLock();
                if (contextLayer == null) return false;
                return string.Equals(contextLayer.IsBase ? "" : contextLayer.Id, activeMappingLayerId, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Physical edges arrive on hook/poll threads while layer transitions touch UI controls, so the
        // apply step is marshalled to the UI thread whenever a window handle exists.
        private void RequestApplyEffectiveMappingLayer()
        {
            if (IsDisposed) return;
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke((MethodInvoker)ApplyEffectiveMappingLayer);
                    return;
                }
            }
            catch { return; }
            ApplyEffectiveMappingLayer();
        }

        private void SetPersistentMappingLayer(string layerId)
        {
            layerId = layerId ?? "";
            lock (runLock) { persistentMappingLayerId = layerId; }
            ApplyEffectiveMappingLayer();
        }

        private bool BeginMomentaryLayerHold(MappingLayerDefinition layer)
        {
            if (layer == null || !layer.MomentarySwitch || layer.SwitchTrigger == null) return false;
            lock (runLock)
            {
                foreach (MomentaryLayerHold existing in momentaryLayerHolds)
                    if (existing != null && string.Equals(existing.LayerId, layer.Id, StringComparison.OrdinalIgnoreCase)) return false;
                momentaryLayerHolds.Add(new MomentaryLayerHold { LayerId = layer.Id });
            }
            runtimeTrace.Add("momentary-layer-hold", layer.Id);
            RequestApplyEffectiveMappingLayer();
            return true;
        }

        private bool EndMomentaryLayerHoldsForRelease(InputEventInfo releaseEvent)
        {
            if (releaseEvent == null || releaseEvent.Input == null) return false;
            bool changed = false;
            lock (runLock)
            {
                for (int i = momentaryLayerHolds.Count - 1; i >= 0; i--)
                {
                    MomentaryLayerHold hold = momentaryLayerHolds[i];
                    if (hold == null)
                    {
                        momentaryLayerHolds.RemoveAt(i);
                        changed = true;
                        continue;
                    }
                    MappingLayerDefinition layer = FindMappingLayer(hold.LayerId);
                    if (layer == null || !layer.MomentarySwitch || layer.SwitchTrigger == null ||
                        ModifierSafetyPolicy.HoldTriggerReleasedByEvent(layer.SwitchTrigger, releaseEvent))
                    {
                        momentaryLayerHolds.RemoveAt(i);
                        changed = true;
                        runtimeTrace.Add("momentary-layer-release", hold.LayerId ?? "null");
                    }
                }
            }
            if (changed) RequestApplyEffectiveMappingLayer();
            return changed;
        }

        private void ActiveLayerBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loadingUi || activeLayerBox == null) return;
            LayerChoice choice = activeLayerBox.SelectedItem as LayerChoice;
            if (choice != null) SetPersistentMappingLayer(choice.Id);
        }

        private void MappingLayerBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loadingUi || mappingLayerBox == null) return;
            MacroDefinition macro = SelectedMacro;
            LayerChoice choice = mappingLayerBox.SelectedItem as LayerChoice;
            if (macro == null || choice == null) return;
            if (string.Equals(MacroLayerId(macro), choice.Id, StringComparison.OrdinalIgnoreCase)) return;
            StopMacroForDefinitionChange(macro, "mapping-layer-change");
            macro.MappingLayerId = choice.Id;
            SaveConfig();
            RefreshRuntimeCapabilityUi();
            statusLabel.Text = Localizer.IsEnglish ? "Mapping layer updated." : "状态：已修改这个宏所属的映射层。";
        }

    }
}
