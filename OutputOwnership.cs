using System;
using System.Collections.Generic;

namespace InputStitch
{
    internal interface IOutputBackend
    {
        void SendDown(InputSpec input);
        void SendUp(InputSpec input);
        void NeutralizeGamepad();
    }

    internal sealed class DirectOutputBackend : IOutputBackend
    {
        public void SendDown(InputSpec input) { InputSender.SendDown(input); }
        public void SendUp(InputSpec input) { InputSender.SendUp(input); }
        public void NeutralizeGamepad() { GamepadOutput.NeutralizeAll(); }
    }

    internal sealed class OutputOwnershipException : Exception
    {
        public OutputOwnershipException(string message, Exception inner) : base(message, inner) { }
    }

    internal sealed class OutputSourceSnapshot
    {
        public string SourceId = "";
        public bool Suspended;
        public List<InputSpec> Contributions = new List<InputSpec>();
    }

    internal sealed class OutputOwnershipSnapshot
    {
        public long Version;
        public string LastReason = "";
        public List<OutputSourceSnapshot> Sources = new List<OutputSourceSnapshot>();
        public List<InputSpec> Merged = new List<InputSpec>();
    }

    // Central ownership/merge layer for persistent output state.
    //
    // Each logical runtime source owns its own contribution. Releasing or clearing one source
    // recomputes the final state without disturbing other sources. Digital outputs use reference
    // ownership, triggers use max(), and stick vectors are summed then normalized to the circular
    // range. Wheel events are instantaneous and therefore intentionally bypass ownership state.
    internal sealed class OutputOwnershipManager
    {
        private sealed class SourceState
        {
            public readonly Dictionary<string, InputSpec> Contributions = new Dictionary<string, InputSpec>(StringComparer.Ordinal);
            public bool Suspended;
        }

        private readonly object sync = new object();
        private readonly IOutputBackend backend;
        private readonly Dictionary<string, SourceState> sources = new Dictionary<string, SourceState>(StringComparer.Ordinal);
        private Dictionary<string, InputSpec> merged = new Dictionary<string, InputSpec>(StringComparer.Ordinal);
        private long version;
        private string lastReason = "init";

        public OutputOwnershipManager(IOutputBackend outputBackend)
        {
            if (outputBackend == null) throw new ArgumentNullException("outputBackend");
            backend = outputBackend;
        }

        public int ActiveSourceCount
        {
            get
            {
                lock (sync)
                {
                    int count = 0;
                    foreach (KeyValuePair<string, SourceState> pair in sources)
                        if (pair.Value != null && pair.Value.Contributions.Count != 0) count++;
                    return count;
                }
            }
        }

        public void SetDown(string sourceId, InputSpec input)
        {
            ValidateSourceAndInput(sourceId, input);
            if (!InputSender.IsHoldable(input))
            {
                // Wheel events are edge events rather than state. There is nothing to own or merge.
                backend.SendDown(input.Clone());
                lock (sync) { version++; lastReason = "instant:" + sourceId; }
                return;
            }

            lock (sync)
            {
                SourceState source = GetOrCreateSourceLocked(sourceId);
                source.Contributions[ControlKey(input)] = input.Clone();
                RecomputeAndEmitLocked("down:" + sourceId);
            }
        }

        public void SetUp(string sourceId, InputSpec input)
        {
            ValidateSourceAndInput(sourceId, input);
            if (!InputSender.IsHoldable(input)) return;

            lock (sync)
            {
                SourceState source;
                if (!sources.TryGetValue(sourceId, out source) || source == null) return;
                if (!source.Contributions.Remove(ControlKey(input))) return;
                if (source.Contributions.Count == 0 && !source.Suspended) sources.Remove(sourceId);
                RecomputeAndEmitLocked("up:" + sourceId);
            }
        }

        public void ClearSource(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) return;
            lock (sync)
            {
                if (!sources.Remove(sourceId)) return;
                RecomputeAndEmitLocked("clear-source:" + sourceId);
            }
        }

        // Atomically replaces the complete persistent state contributed by one source. This is
        // used by the controller router so one XInput report becomes one ownership transition,
        // rather than a sequence of transient per-button/per-axis states.
        public void ReplaceSource(string sourceId, IEnumerable<InputSpec> inputs)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("sourceId");
            Dictionary<string, InputSpec> next = new Dictionary<string, InputSpec>(StringComparer.Ordinal);
            if (inputs != null)
            {
                foreach (InputSpec input in inputs)
                {
                    if (input == null) continue;
                    ValidateSourceAndInput(sourceId, input);
                    if (!InputSender.IsHoldable(input))
                        throw new ArgumentException("ReplaceSource accepts persistent inputs only.");
                    next[ControlKey(input)] = input.Clone();
                }
            }

            lock (sync)
            {
                if (next.Count == 0)
                {
                    if (!sources.Remove(sourceId)) return;
                }
                else
                {
                    SourceState source = GetOrCreateSourceLocked(sourceId);
                    bool same = source.Contributions.Count == next.Count;
                    if (same)
                    {
                        foreach (KeyValuePair<string, InputSpec> pair in next)
                        {
                            InputSpec existing;
                            if (!source.Contributions.TryGetValue(pair.Key, out existing) || !SameOutputState(existing, pair.Value))
                            { same = false; break; }
                        }
                    }
                    if (same) return;
                    source.Contributions.Clear();
                    foreach (KeyValuePair<string, InputSpec> pair in next) source.Contributions[pair.Key] = pair.Value;
                }
                RecomputeAndEmitLocked("replace-source:" + sourceId);
            }
        }

        // Used by UI-safety pause for any worker-backed macro source. The logical source remains intact
        // so ResumeSource can restore only that source without reconstructing or touching others.
        public void SuspendSource(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) return;
            lock (sync)
            {
                SourceState source;
                if (!sources.TryGetValue(sourceId, out source) || source == null || source.Suspended) return;
                source.Suspended = true;
                RecomputeAndEmitLocked("suspend:" + sourceId);
            }
        }

        public void ResumeSource(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) return;
            lock (sync)
            {
                SourceState source;
                if (!sources.TryGetValue(sourceId, out source) || source == null || !source.Suspended) return;
                source.Suspended = false;
                RecomputeAndEmitLocked("resume:" + sourceId);
            }
        }

        // Emergency/shutdown boundary. Digital keyboard/mouse outputs are explicitly released;
        // the virtual controller is then hard-neutralized once. All logical ownership is dropped.
        public void ClearAll(string reason)
        {
            lock (sync)
            {
                Dictionary<string, InputSpec> previous = CloneState(merged);
                sources.Clear();
                merged.Clear();
                version++;
                lastReason = string.IsNullOrWhiteSpace(reason) ? "clear-all" : reason;
                ReleasePhysicalStateFailClosed(previous);
            }
        }

        public OutputOwnershipSnapshot Snapshot()
        {
            lock (sync)
            {
                OutputOwnershipSnapshot result = new OutputOwnershipSnapshot();
                result.Version = version;
                result.LastReason = lastReason;

                List<string> sourceIds = new List<string>(sources.Keys);
                sourceIds.Sort(StringComparer.Ordinal);
                foreach (string sourceId in sourceIds)
                {
                    SourceState state = sources[sourceId];
                    if (state == null || state.Contributions.Count == 0) continue;
                    OutputSourceSnapshot source = new OutputSourceSnapshot();
                    source.SourceId = sourceId;
                    source.Suspended = state.Suspended;
                    List<string> keys = new List<string>(state.Contributions.Keys);
                    keys.Sort(StringComparer.Ordinal);
                    foreach (string key in keys)
                    {
                        InputSpec input = state.Contributions[key];
                        if (input != null) source.Contributions.Add(input.Clone());
                    }
                    result.Sources.Add(source);
                }

                List<string> mergedKeys = new List<string>(merged.Keys);
                mergedKeys.Sort(StringComparer.Ordinal);
                foreach (string key in mergedKeys)
                {
                    InputSpec input = merged[key];
                    if (input != null) result.Merged.Add(input.Clone());
                }
                return result;
            }
        }

        private SourceState GetOrCreateSourceLocked(string sourceId)
        {
            SourceState source;
            if (!sources.TryGetValue(sourceId, out source) || source == null)
            {
                source = new SourceState();
                sources[sourceId] = source;
            }
            return source;
        }

        private void RecomputeAndEmitLocked(string reason)
        {
            Dictionary<string, InputSpec> previous = CloneState(merged);
            Dictionary<string, InputSpec> next = ComputeMergedLocked();
            try
            {
                EmitDeltaLocked(previous, next);
                merged = next;
                version++;
                lastReason = reason ?? "state-change";
            }
            catch (Exception ex)
            {
                // A partial native send is worse than losing the current mappings. Fail closed:
                // forget every logical source, release keyboard/mouse state we know about, and
                // hard-neutralize the virtual controller before surfacing the original error.
                Dictionary<string, InputSpec> possible = CloneState(previous);
                foreach (KeyValuePair<string, InputSpec> pair in next)
                    possible[pair.Key] = pair.Value == null ? null : pair.Value.Clone();
                sources.Clear();
                merged.Clear();
                version++;
                lastReason = "fail-closed:" + (reason ?? "state-change");
                ReleasePhysicalStateFailClosed(possible);
                throw new OutputOwnershipException("Output ownership backend failure.", ex);
            }
        }

        private Dictionary<string, InputSpec> ComputeMergedLocked()
        {
            Dictionary<string, InputSpec> result = new Dictionary<string, InputSpec>(StringComparer.Ordinal);
            Dictionary<GamepadControl, int[]> stickTotals = new Dictionary<GamepadControl, int[]>();
            Dictionary<GamepadControl, int> triggerMax = new Dictionary<GamepadControl, int>();
            bool dpadUp = false;
            bool dpadDown = false;
            bool dpadLeft = false;
            bool dpadRight = false;

            foreach (KeyValuePair<string, SourceState> sourcePair in sources)
            {
                SourceState source = sourcePair.Value;
                if (source == null || source.Suspended) continue;
                foreach (KeyValuePair<string, InputSpec> contributionPair in source.Contributions)
                {
                    InputSpec input = contributionPair.Value;
                    if (input == null) continue;
                    if (IsStick(input))
                    {
                        int[] total;
                        if (!stickTotals.TryGetValue(input.GamepadControl, out total))
                        {
                            total = new int[2];
                            stickTotals[input.GamepadControl] = total;
                        }
                        total[0] += Clamp(input.GamepadX, -100, 100);
                        total[1] += Clamp(input.GamepadY, -100, 100);
                    }
                    else if (IsTrigger(input))
                    {
                        int value = Clamp(input.GamepadValue, 0, 100);
                        int existing;
                        if (!triggerMax.TryGetValue(input.GamepadControl, out existing) || value > existing)
                            triggerMax[input.GamepadControl] = value;
                    }
                    else if (IsDPad(input))
                    {
                        if (input.GamepadControl == GamepadControl.DPadUp) dpadUp = true;
                        else if (input.GamepadControl == GamepadControl.DPadDown) dpadDown = true;
                        else if (input.GamepadControl == GamepadControl.DPadLeft) dpadLeft = true;
                        else if (input.GamepadControl == GamepadControl.DPadRight) dpadRight = true;
                    }
                    else
                    {
                        // Digital state: presence of at least one non-suspended source means Down.
                        string key = ControlKey(input);
                        if (!result.ContainsKey(key)) result[key] = input.Clone();
                    }
                }
            }

            foreach (KeyValuePair<GamepadControl, int[]> pair in stickTotals)
            {
                int x = pair.Value[0];
                int y = pair.Value[1];
                NormalizeStick(ref x, ref y);
                if (x == 0 && y == 0) continue;
                InputSpec mergedStick = new InputSpec();
                mergedStick.Kind = InputKind.Gamepad;
                mergedStick.GamepadControl = pair.Key;
                mergedStick.GamepadX = x;
                mergedStick.GamepadY = y;
                mergedStick.GamepadValue = 100;
                result[ControlKey(mergedStick)] = mergedStick;
            }

            foreach (KeyValuePair<GamepadControl, int> pair in triggerMax)
            {
                if (pair.Value <= 0) continue;
                InputSpec mergedTrigger = new InputSpec();
                mergedTrigger.Kind = InputKind.Gamepad;
                mergedTrigger.GamepadControl = pair.Key;
                mergedTrigger.GamepadValue = pair.Value;
                mergedTrigger.GamepadX = 0;
                mergedTrigger.GamepadY = 0;
                result[ControlKey(mergedTrigger)] = mergedTrigger;
            }

            // Opposing D-pad directions are contradictory on a single virtual pad. Resolve each
            // axis independently by cancellation; orthogonal directions remain together as a
            // diagonal. This is deterministic across Xbox button reports and the DS4 hat switch.
            if (dpadUp != dpadDown) AddDigitalGamepadState(result, dpadUp ? GamepadControl.DPadUp : GamepadControl.DPadDown);
            if (dpadLeft != dpadRight) AddDigitalGamepadState(result, dpadLeft ? GamepadControl.DPadLeft : GamepadControl.DPadRight);
            return result;
        }

        private static void AddDigitalGamepadState(Dictionary<string, InputSpec> result, GamepadControl control)
        {
            InputSpec input = new InputSpec();
            input.Kind = InputKind.Gamepad;
            input.GamepadControl = control;
            input.GamepadX = 0;
            input.GamepadY = 0;
            input.GamepadValue = 100;
            result[ControlKey(input)] = input;
        }

        private void EmitDeltaLocked(Dictionary<string, InputSpec> previous, Dictionary<string, InputSpec> next)
        {
            SortedSet<string> keys = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string key in previous.Keys) keys.Add(key);
            foreach (string key in next.Keys) keys.Add(key);

            foreach (string key in keys)
            {
                InputSpec oldState;
                InputSpec newState;
                previous.TryGetValue(key, out oldState);
                next.TryGetValue(key, out newState);
                if (oldState == null && newState != null)
                {
                    backend.SendDown(newState.Clone());
                }
                else if (oldState != null && newState == null)
                {
                    backend.SendUp(oldState.Clone());
                }
                else if (oldState != null && newState != null && !SameOutputState(oldState, newState))
                {
                    // Only analog channels can materially change while remaining logically Down.
                    backend.SendDown(newState.Clone());
                }
            }
        }

        private void ReleasePhysicalStateFailClosed(Dictionary<string, InputSpec> known)
        {
            if (known != null)
            {
                foreach (KeyValuePair<string, InputSpec> pair in known)
                {
                    InputSpec input = pair.Value;
                    if (input == null || input.Kind == InputKind.Gamepad) continue;
                    try { if (InputSender.IsHoldable(input)) backend.SendUp(input.Clone()); } catch { }
                }
            }
            try { backend.NeutralizeGamepad(); } catch { }
        }

        private static Dictionary<string, InputSpec> CloneState(Dictionary<string, InputSpec> source)
        {
            Dictionary<string, InputSpec> clone = new Dictionary<string, InputSpec>(StringComparer.Ordinal);
            if (source == null) return clone;
            foreach (KeyValuePair<string, InputSpec> pair in source)
                clone[pair.Key] = pair.Value == null ? null : pair.Value.Clone();
            return clone;
        }

        internal static string ControlKey(InputSpec input)
        {
            if (input == null) return "null";
            if (input.Kind == InputKind.Gamepad)
                return "g:" + ((int)input.GamepadControl).ToString();
            return "i:" + ((int)input.Kind).ToString() + ":" + input.VirtualKey.ToString() + ":" +
                input.ScanCode.ToString() + ":" + input.Extended.ToString();
        }

        internal static bool SameOutputState(InputSpec left, InputSpec right)
        {
            if (left == null || right == null || left.Kind != right.Kind) return false;
            if (left.Kind == InputKind.Gamepad)
            {
                if (left.GamepadControl != right.GamepadControl) return false;
                if (IsStick(left)) return left.GamepadX == right.GamepadX && left.GamepadY == right.GamepadY;
                if (IsTrigger(left)) return left.GamepadValue == right.GamepadValue;
                return true;
            }
            return left.VirtualKey == right.VirtualKey && left.ScanCode == right.ScanCode && left.Extended == right.Extended;
        }

        internal static void NormalizeStick(ref int x, ref int y)
        {
            x = Clamp(x, -100000, 100000);
            y = Clamp(y, -100000, 100000);
            if (x == 0 && y == 0) return;
            double magnitude = Math.Sqrt((double)x * (double)x + (double)y * (double)y);
            if (magnitude <= 100.0)
            {
                x = Clamp(x, -100, 100);
                y = Clamp(y, -100, 100);
                return;
            }
            double scale = 100.0 / magnitude;
            x = (int)Math.Round(x * scale, MidpointRounding.AwayFromZero);
            y = (int)Math.Round(y * scale, MidpointRounding.AwayFromZero);
            x = Clamp(x, -100, 100);
            y = Clamp(y, -100, 100);
        }

        private static bool IsStick(InputSpec input)
        {
            return input != null && input.Kind == InputKind.Gamepad &&
                (input.GamepadControl == GamepadControl.LeftStick || input.GamepadControl == GamepadControl.RightStick);
        }

        private static bool IsTrigger(InputSpec input)
        {
            return input != null && input.Kind == InputKind.Gamepad &&
                (input.GamepadControl == GamepadControl.LeftTrigger || input.GamepadControl == GamepadControl.RightTrigger);
        }

        private static bool IsDPad(InputSpec input)
        {
            return input != null && input.Kind == InputKind.Gamepad &&
                (input.GamepadControl == GamepadControl.DPadUp || input.GamepadControl == GamepadControl.DPadDown ||
                 input.GamepadControl == GamepadControl.DPadLeft || input.GamepadControl == GamepadControl.DPadRight);
        }

        private static void ValidateSourceAndInput(string sourceId, InputSpec input)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Output source id is required.", "sourceId");
            if (input == null) throw new ArgumentNullException("input");
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
