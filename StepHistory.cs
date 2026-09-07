using System;
using System.Collections.Generic;
using System.Linq;

namespace InputStitch
{
    internal sealed class StepHistory
    {
        private MacroDefinition owner;
        private List<MacroStep> current;
        private readonly List<List<MacroStep>> undo = new List<List<MacroStep>>();
        private readonly List<List<MacroStep>> redo = new List<List<MacroStep>>();
        internal bool CanUndo { get { return undo.Count > 0; } }
        internal bool CanRedo { get { return redo.Count > 0; } }
        internal int UndoCount { get { return undo.Count; } }
        private static List<MacroStep> Copy(IList<MacroStep> steps)
        {
            return steps == null ? new List<MacroStep>() : steps.Select(s => s.Clone()).ToList();
        }
        private static bool Equal(IList<MacroStep> a, IList<MacroStep> b)
        {
            if (a.Count != b.Count) return false;
            // Compare every serialized field, including future additions, without cloning macro identity/name.
            var fields = typeof(MacroStep).GetFields();
            for (int i = 0; i < a.Count; i++)
                foreach (var field in fields)
                    if (!Object.Equals(field.GetValue(a[i]), field.GetValue(b[i]))) return false;
            return true;
        }
        internal void Observe(MacroDefinition macro)
        {
            if (!Object.ReferenceEquals(owner, macro))
            {
                owner = macro;
                current = macro == null ? new List<MacroStep>() : Copy(macro.Steps);
                undo.Clear(); redo.Clear();
                return;
            }
            if (macro == null || Equal(current, macro.Steps)) return;
            undo.Add(current);
            if (undo.Count > 50) undo.RemoveAt(0);
            current = Copy(macro.Steps);
            redo.Clear();
        }
        internal bool Restore(MacroDefinition macro, bool forward)
        {
            Observe(macro);
            var from = forward ? redo : undo;
            var to = forward ? undo : redo;
            if (macro == null || from.Count == 0) return false;
            to.Add(current);
            current = from[from.Count - 1];
            from.RemoveAt(from.Count - 1);
            macro.Steps = Copy(current);
            return true;
        }
    }
}
