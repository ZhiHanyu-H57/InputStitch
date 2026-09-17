using System;
using System.Collections.Generic;

namespace InputStitch
{
    public static class ActivatorModes
    {
        public const string Legacy = "Legacy";
        public const string Press = "Press";
        public const string Release = "Release";
        public const string WhileHeld = "WhileHeld";
        public const string LongPress = "LongPress";
        public const string DoublePress = "DoublePress";

        public static string Normalize(string value)
        {
            if (string.Equals(value, Press, StringComparison.OrdinalIgnoreCase)) return Press;
            if (string.Equals(value, Release, StringComparison.OrdinalIgnoreCase)) return Release;
            if (string.Equals(value, WhileHeld, StringComparison.OrdinalIgnoreCase)) return WhileHeld;
            if (string.Equals(value, LongPress, StringComparison.OrdinalIgnoreCase)) return LongPress;
            if (string.Equals(value, DoublePress, StringComparison.OrdinalIgnoreCase)) return DoublePress;
            return Legacy;
        }

        public static string Effective(MacroDefinition macro)
        {
            if (macro == null || macro.Activator == null) return Legacy;
            string mode = Normalize(macro.Activator.Mode);
            if (!string.Equals(mode, Legacy, StringComparison.OrdinalIgnoreCase)) return mode;
            return macro.RunMode == TriggerRunMode.Hold ? WhileHeld : Legacy;
        }

        public static bool UsesAdvancedPolicy(MacroDefinition macro)
        {
            return macro != null && macro.Activator != null &&
                !string.Equals(Normalize(macro.Activator.Mode), Legacy, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsHoldLifecycle(MacroDefinition macro)
        {
            if (macro == null) return false;
            string configured = macro.Activator == null ? Legacy : Normalize(macro.Activator.Mode);
            if (!string.Equals(configured, Legacy, StringComparison.OrdinalIgnoreCase))
                return string.Equals(configured, WhileHeld, StringComparison.OrdinalIgnoreCase);
            return macro.RunMode == TriggerRunMode.Hold;
        }
    }

    [Serializable]
    public sealed class ActivatorConfig
    {
        public string Mode = ActivatorModes.Legacy;
        public int LongPressMs = 500;
        public int DoublePressWindowMs = 300;

        public ActivatorConfig Clone()
        {
            ActivatorConfig copy = new ActivatorConfig();
            copy.Mode = ActivatorModes.Normalize(Mode);
            copy.LongPressMs = LongPressMs;
            copy.DoublePressWindowMs = DoublePressWindowMs;
            Normalize(copy);
            return copy;
        }

        public static void Normalize(ActivatorConfig value)
        {
            if (value == null) return;
            value.Mode = ActivatorModes.Normalize(value.Mode);
            value.LongPressMs = Clamp(value.LongPressMs, 100, 10000);
            value.DoublePressWindowMs = Clamp(value.DoublePressWindowMs, 100, 2000);
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }

    public enum AnalogConditionDirection
    {
        Magnitude = 0,
        PositiveX = 1,
        NegativeX = 2,
        PositiveY = 3,
        NegativeY = 4
    }

    [Serializable]
    public sealed class AnalogZoneCondition
    {
        public bool Enabled;
        public GamepadControl Control = GamepadControl.LeftTrigger;
        public AnalogConditionDirection Direction = AnalogConditionDirection.Magnitude;
        public int MinimumPercent = 80;
        public int MaximumPercent = 100;

        public AnalogZoneCondition Clone()
        {
            AnalogZoneCondition copy = new AnalogZoneCondition();
            copy.Enabled = Enabled;
            copy.Control = Control;
            copy.Direction = Direction;
            copy.MinimumPercent = MinimumPercent;
            copy.MaximumPercent = MaximumPercent;
            Normalize(copy);
            return copy;
        }

        public static void Normalize(AnalogZoneCondition value)
        {
            if (value == null) return;
            if (value.Control != GamepadControl.LeftStick && value.Control != GamepadControl.RightStick &&
                value.Control != GamepadControl.LeftTrigger && value.Control != GamepadControl.RightTrigger)
                value.Control = GamepadControl.LeftTrigger;
            if (!Enum.IsDefined(typeof(AnalogConditionDirection), value.Direction)) value.Direction = AnalogConditionDirection.Magnitude;
            value.MinimumPercent = Clamp(value.MinimumPercent, 0, 100);
            value.MaximumPercent = Clamp(value.MaximumPercent, 0, 100);
            if (value.MaximumPercent < value.MinimumPercent)
            {
                int swap = value.MinimumPercent;
                value.MinimumPercent = value.MaximumPercent;
                value.MaximumPercent = swap;
            }
            if ((value.Control == GamepadControl.LeftTrigger || value.Control == GamepadControl.RightTrigger) &&
                value.Direction != AnalogConditionDirection.Magnitude)
                value.Direction = AnalogConditionDirection.Magnitude;
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }

    [Serializable]
    public sealed class MacroConditionConfig
    {
        // MappingLayerId remains the authoritative Layer condition. These are additional AND gates.
        public string ForegroundProcessName = "";
        public string ForegroundTitleContains = "";
        public string DeviceKey = "";
        public AnalogZoneCondition Analog = new AnalogZoneCondition();

        public MacroConditionConfig Clone()
        {
            MacroConditionConfig copy = new MacroConditionConfig();
            copy.ForegroundProcessName = ForegroundProcessName ?? "";
            copy.ForegroundTitleContains = ForegroundTitleContains ?? "";
            copy.DeviceKey = DeviceKey ?? "";
            copy.Analog = Analog == null ? new AnalogZoneCondition() : Analog.Clone();
            Normalize(copy);
            return copy;
        }

        public static void Normalize(MacroConditionConfig value)
        {
            if (value == null) return;
            value.ForegroundProcessName = (value.ForegroundProcessName ?? "").Trim();
            value.ForegroundTitleContains = (value.ForegroundTitleContains ?? "").Trim();
            value.DeviceKey = (value.DeviceKey ?? "").Trim();
            if (value.Analog == null) value.Analog = new AnalogZoneCondition();
            AnalogZoneCondition.Normalize(value.Analog);
        }

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(ForegroundProcessName) &&
                    string.IsNullOrWhiteSpace(ForegroundTitleContains) &&
                    string.IsNullOrWhiteSpace(DeviceKey) &&
                    (Analog == null || !Analog.Enabled);
            }
        }
    }

    internal enum ActivatorDecisionKind
    {
        None = 0,
        StartOnce = 1,
        StartHeld = 2,
        StopHeld = 3
    }

    internal sealed class ActivatorDecision
    {
        public MacroDefinition Macro;
        public ActivatorDecisionKind Kind;
        public InputEventInfo TriggerEvent;
        public string Reason = "";
    }

    // Timing/edge state only. Macro execution still goes through the existing concurrent runtime
    // and Output Ownership. Legacy Toggle/Hold remains a compatibility adapter outside this state
    // machine until the advanced rule is explicitly enabled on a macro.
    internal sealed class ActivatorRuntimeEngine
    {
        private sealed class State
        {
            public bool Down;
            public bool EligibleAtDown;
            public bool LongFired;
            public bool HeldStarted;
            public long DownAt;
            public long LastPressAt = -1;
            public int DeviceIndex = -1;
            public TriggerSpec EffectiveTrigger;
            public InputEventInfo DownEvent;
        }

        private readonly object sync = new object();
        private readonly Dictionary<MacroDefinition, State> states = new Dictionary<MacroDefinition, State>();

        internal ActivatorDecision OnDown(MacroDefinition macro, InputEventInfo inputEvent, TriggerSpec effectiveTrigger,
            long nowMs, bool conditionsSatisfied)
        {
            if (macro == null || inputEvent == null || inputEvent.Input == null || !ActivatorModes.UsesAdvancedPolicy(macro)) return null;
            string mode = ActivatorModes.Normalize(macro.Activator.Mode);
            bool edgeOnly = effectiveTrigger != null &&
                (effectiveTrigger.Kind == InputKind.WheelUp || effectiveTrigger.Kind == InputKind.WheelDown);
            lock (sync)
            {
                State state = GetStateLocked(macro);
                if (state.Down && !edgeOnly) return null;
                state.Down = !edgeOnly;
                state.EligibleAtDown = conditionsSatisfied;
                state.LongFired = false;
                state.HeldStarted = false;
                state.DownAt = nowMs;
                state.DeviceIndex = inputEvent.DeviceIndex;
                state.EffectiveTrigger = effectiveTrigger == null ? null : effectiveTrigger.Clone();
                state.DownEvent = CloneEvent(inputEvent);

                // Wheel directions are edge-only inputs. Press and DoublePress have meaningful
                // edge semantics; Release/WhileHeld/LongPress require a persistent down state and
                // deliberately do not pretend the wheel can provide one.
                if (edgeOnly && !string.Equals(mode, ActivatorModes.Press, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(mode, ActivatorModes.DoublePress, StringComparison.OrdinalIgnoreCase))
                {
                    state.EligibleAtDown = false;
                    state.EffectiveTrigger = null;
                    state.DownEvent = null;
                    return null;
                }

                if (!conditionsSatisfied)
                {
                    if (string.Equals(mode, ActivatorModes.DoublePress, StringComparison.OrdinalIgnoreCase)) state.LastPressAt = -1;
                    return null;
                }
                if (string.Equals(mode, ActivatorModes.Press, StringComparison.OrdinalIgnoreCase))
                    return Decision(macro, ActivatorDecisionKind.StartOnce, state.DownEvent, "press");
                if (string.Equals(mode, ActivatorModes.WhileHeld, StringComparison.OrdinalIgnoreCase))
                {
                    state.HeldStarted = true;
                    return Decision(macro, ActivatorDecisionKind.StartHeld, state.DownEvent, "while-held-down");
                }
                if (string.Equals(mode, ActivatorModes.DoublePress, StringComparison.OrdinalIgnoreCase))
                {
                    int window = macro.Activator == null ? 300 : macro.Activator.DoublePressWindowMs;
                    if (state.LastPressAt >= 0 && nowMs >= state.LastPressAt && nowMs - state.LastPressAt <= window)
                    {
                        state.LastPressAt = -1;
                        return Decision(macro, ActivatorDecisionKind.StartOnce, state.DownEvent, "double-press");
                    }
                    state.LastPressAt = nowMs;
                }
                return null;
            }
        }

        internal List<ActivatorDecision> OnUp(InputEventInfo inputEvent, long nowMs,
            Func<MacroDefinition, int, bool> conditionsSatisfied)
        {
            List<ActivatorDecision> result = new List<ActivatorDecision>();
            if (inputEvent == null || inputEvent.Input == null) return result;
            lock (sync)
            {
                List<MacroDefinition> keys = new List<MacroDefinition>(states.Keys);
                foreach (MacroDefinition macro in keys)
                {
                    State state;
                    if (!states.TryGetValue(macro, out state) || state == null || !state.Down || state.EffectiveTrigger == null) continue;
                    if (!ModifierSafetyPolicy.HoldTriggerReleasedByEvent(state.EffectiveTrigger, inputEvent)) continue;
                    string mode = macro.Activator == null ? ActivatorModes.Legacy : ActivatorModes.Normalize(macro.Activator.Mode);
                    bool currentConditions = conditionsSatisfied == null || conditionsSatisfied(macro, state.DeviceIndex);
                    state.Down = false;
                    if (string.Equals(mode, ActivatorModes.Release, StringComparison.OrdinalIgnoreCase) &&
                        state.EligibleAtDown && currentConditions)
                        result.Add(Decision(macro, ActivatorDecisionKind.StartOnce, CloneEvent(inputEvent), "release"));
                    // Existing hold-runtime release tracking remains authoritative for physical
                    // WhileHeld release; only condition loss below needs an explicit StopHeld.
                    state.EligibleAtDown = false;
                    state.LongFired = false;
                    state.HeldStarted = false;
                    state.EffectiveTrigger = null;
                    state.DownEvent = null;
                }
            }
            return result;
        }

        internal List<ActivatorDecision> Tick(long nowMs, Func<MacroDefinition, int, bool> conditionsSatisfied)
        {
            List<ActivatorDecision> result = new List<ActivatorDecision>();
            lock (sync)
            {
                foreach (KeyValuePair<MacroDefinition, State> pair in states)
                {
                    MacroDefinition macro = pair.Key;
                    State state = pair.Value;
                    if (macro == null || state == null || !state.Down || macro.Activator == null) continue;
                    bool currentConditions = conditionsSatisfied == null || conditionsSatisfied(macro, state.DeviceIndex);
                    string mode = ActivatorModes.Normalize(macro.Activator.Mode);
                    if (!currentConditions)
                    {
                        state.EligibleAtDown = false;
                        if (state.HeldStarted)
                        {
                            state.HeldStarted = false;
                            result.Add(Decision(macro, ActivatorDecisionKind.StopHeld, state.DownEvent, "condition-failed"));
                        }
                        continue;
                    }
                    if (string.Equals(mode, ActivatorModes.LongPress, StringComparison.OrdinalIgnoreCase) &&
                        state.EligibleAtDown && !state.LongFired && nowMs >= state.DownAt &&
                        nowMs - state.DownAt >= macro.Activator.LongPressMs)
                    {
                        state.LongFired = true;
                        result.Add(Decision(macro, ActivatorDecisionKind.StartOnce, state.DownEvent, "long-press"));
                    }
                }
            }
            return result;
        }

        internal void Reset(MacroDefinition macro)
        {
            if (macro == null) return;
            lock (sync) states.Remove(macro);
        }

        internal void Clear()
        {
            lock (sync) states.Clear();
        }

        internal int TrackedCount
        {
            get { lock (sync) return states.Count; }
        }

        private State GetStateLocked(MacroDefinition macro)
        {
            State state;
            if (!states.TryGetValue(macro, out state) || state == null)
            {
                state = new State();
                states[macro] = state;
            }
            return state;
        }

        private static ActivatorDecision Decision(MacroDefinition macro, ActivatorDecisionKind kind, InputEventInfo inputEvent, string reason)
        {
            return new ActivatorDecision
            {
                Macro = macro,
                Kind = kind,
                TriggerEvent = inputEvent == null ? null : CloneEvent(inputEvent),
                Reason = reason ?? ""
            };
        }

        private static InputEventInfo CloneEvent(InputEventInfo inputEvent)
        {
            if (inputEvent == null) return null;
            return new InputEventInfo
            {
                Input = inputEvent.Input == null ? null : inputEvent.Input.Clone(),
                DeviceIndex = inputEvent.DeviceIndex,
                Ctrl = inputEvent.Ctrl,
                Shift = inputEvent.Shift,
                Alt = inputEvent.Alt,
                Win = inputEvent.Win
            };
        }
    }
}
