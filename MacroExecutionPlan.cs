using System;
using System.Collections.Generic;

namespace InputStitch
{
    // One plan belongs to one run. Its cloned definitions and cached inputs are
    // read-only to the executor; output ownership makes its own copies as before.
    // Preparing these values once avoids input allocation/key formatting each
    // time an immutable step is repeated. No UI, scheduler or output is owned here.
    internal sealed class PreparedMacroStep
    {
        internal readonly MacroStep Definition;
        internal readonly InputSpec Input;
        internal readonly string Key;
        internal readonly bool Holdable;

        internal PreparedMacroStep(MacroStep source)
        {
            if (source == null) throw new ArgumentNullException("source");
            Definition = source.Clone();
            Input = MacroExecutionPlan.ConvertInput(Definition);
            Key = MacroExecutionPlan.KeyForInput(Input);
            Holdable = InputSender.IsHoldable(Input);
        }
    }

    internal sealed class MacroExecutionPlan
    {
        private readonly PreparedMacroStep[] steps;
        internal readonly bool HasNaturalDelay;
        internal int Count { get { return steps.Length; } }
        internal PreparedMacroStep this[int index] { get { return steps[index]; } }

        internal MacroExecutionPlan(IList<MacroStep> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            steps = new PreparedMacroStep[source.Count];
            for (int i = 0; i < steps.Length; i++)
            {
                PreparedMacroStep prepared = new PreparedMacroStep(source[i]);
                steps[i] = prepared;
                MacroStep definition = prepared.Definition;
                int possibleDelay = definition.RandomDelay
                    ? Math.Max(definition.RandomDelayMinMs, definition.RandomDelayMaxMs) : definition.DelayMs;
                // Wheel edges do not execute HoldMs. Counting that unused value
                // would bypass the worker's zero-delay infinite-loop throttle.
                if (possibleDelay > 0 || (prepared.Holdable && definition.Action == MacroAction.Press && definition.HoldMs > 0))
                    HasNaturalDelay = true;
            }
        }

        // Shared with recording/held-mapping callers through MainForm's thin
        // wrappers, so step/input identity has one conversion implementation.
        internal static InputSpec ConvertInput(MacroStep step)
        {
            if (step == null) return null;
            return new InputSpec
            {
                Kind = step.Kind, VirtualKey = step.VirtualKey, ScanCode = step.ScanCode, Extended = step.Extended,
                GamepadControl = step.GamepadControl, GamepadX = step.GamepadX, GamepadY = step.GamepadY,
                GamepadValue = step.GamepadValue
            };
        }

        internal static string KeyForInput(InputSpec input)
        {
            if (input != null && input.Kind == InputKind.Gamepad)
                return ((int)input.Kind).ToString() + ":" + ((int)input.GamepadControl).ToString();
            return ((int)input.Kind).ToString() + ":" + input.VirtualKey.ToString() + ":" + input.ScanCode.ToString() + ":" + input.Extended.ToString();
        }
    }
}
