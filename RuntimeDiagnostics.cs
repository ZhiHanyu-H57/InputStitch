using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace InputStitch
{
    internal static class UiSafetyPolicy
    {
        internal static bool IsEditor(Control control)
        {
            TextBoxBase text = control as TextBoxBase;
            if (text != null) return !text.ReadOnly;
            DataGridView grid = control as DataGridView;
            if (grid != null) return grid.IsCurrentCellInEditMode;
            return control is NumericUpDown || control is ComboBox;
        }

        internal static bool ShouldPauseControl(bool focused, bool hovered, bool editor, bool running, bool mouseOutput)
        {
            // Hovering cannot start editing. Pointer protection is only necessary
            // when a running macro can click/scroll on the pointed-at control.
            return (focused && (editor || running)) || (hovered && running && mouseOutput);
        }

        internal static bool HasMouseOutput(MacroDefinition macro)
        {
            if (macro == null || macro.Steps == null) return false;
            foreach (MacroStep step in macro.Steps)
                if (step != null && step.Kind != InputKind.Keyboard && step.Kind != InputKind.Gamepad) return true;
            return false;
        }

        internal static string ProtectionHint(string reason)
        {
            return Localizer.IsEnglish
                ? "Hotkeys paused: " + Localizer.Dynamic(reason) + ". Click an empty area to finish editing."
                : "热键暂不可用：" + reason + "。点击空白处结束编辑。";
        }
    }

    // Bounded, memory-only lifecycle evidence, not a keyboard log. Callers only
    // record matched macro decisions and run transitions; never arbitrary keys.
    internal sealed class RuntimeTrace
    {
        internal const int Capacity = 64;
        private readonly object sync = new object();
        private readonly Queue<string> events = new Queue<string>();
        private long sequence;
        internal void Add(string kind, string detail)
        {
            lock (sync)
            {
                string safe = (detail ?? "").Replace('\r', ' ').Replace('\n', ' ');
                if (safe.Length > 160) safe = safe.Substring(0, 160);
                events.Enqueue((++sequence).ToString() + " " + DateTime.UtcNow.ToString("HH:mm:ss.fff") + " UTC " + kind + " " + safe);
                while (events.Count > Capacity) events.Dequeue();
            }
        }
        internal string Snapshot()
        {
            lock (sync)
            {
                StringBuilder text = new StringBuilder();
                text.AppendLine("RecentRuntimeEvents (memory only, last 64; timestamps UTC):");
                foreach (string item in events) text.AppendLine(item);
                return text.ToString();
            }
        }
    }
}
