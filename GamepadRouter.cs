using System;
using System.Collections.Generic;

namespace InputStitch
{
    // Mirrors every visible XInput source into InputStitch's virtual Xbox controller through the
    // same ownership layer used by macros. This is the routing half of controlled replacement:
    // source hiding/slot acquisition is deliberately separate and may require a filter driver.
    internal sealed class GamepadRouterService : IDisposable
    {
        private const string SourcePrefix = "router:xinput:";
        private readonly OutputOwnershipManager ownership;
        private readonly IRouterSourcePolicyEvaluator sourcePolicy;
        private bool enabled;
        private int ownSlot = -1;
        private int routedControllerCount;
        private int policyBlockedCount;
        private int policyUnresolvedCount;
        private string policyStatus = "all-visible";
        private string lastStatus = "disabled";
        private bool disposed;

        internal GamepadRouterService(OutputOwnershipManager ownershipManager)
            : this(ownershipManager, null)
        {
        }

        internal GamepadRouterService(OutputOwnershipManager ownershipManager, IRouterSourcePolicyEvaluator policyEvaluator)
        {
            if (ownershipManager == null) throw new ArgumentNullException("ownershipManager");
            ownership = ownershipManager;
            sourcePolicy = policyEvaluator;
        }

        internal bool Enabled { get { return enabled; } }
        internal int OwnSlot { get { return ownSlot; } }
        internal int RoutedControllerCount { get { return routedControllerCount; } }
        internal int PolicyBlockedCount { get { return policyBlockedCount; } }
        internal int PolicyUnresolvedCount { get { return policyUnresolvedCount; } }
        internal string PolicyStatus { get { return policyStatus; } }
        internal bool OwnsPreferredSlotZero { get { return ownSlot == 0; } }
        internal string LastStatus { get { return lastStatus; } }

        internal void Configure(bool value)
        {
            if (disposed) return;
            policyStatus = sourcePolicy == null ? "all-visible" : sourcePolicy.Summary;
            if (enabled == value) return;
            enabled = value;
            if (!enabled)
            {
                ClearAllSources();
                ownSlot = -1;
                routedControllerCount = 0;
                policyBlockedCount = 0;
                policyUnresolvedCount = 0;
                lastStatus = "disabled";
            }
            else lastStatus = "waiting-for-virtual-xbox";
        }

        internal void Tick(XInputInputService input, int currentOwnSlot)
        {
            if (disposed || !enabled) return;
            ownSlot = currentOwnSlot;
            if (input == null || currentOwnSlot < 0 || currentOwnSlot > 3)
            {
                ClearAllSources();
                routedControllerCount = 0;
                policyBlockedCount = 0;
                policyUnresolvedCount = 0;
                policyStatus = sourcePolicy == null ? "all-visible" : sourcePolicy.Summary;
                lastStatus = "waiting-for-virtual-xbox";
                return;
            }

            int routed = 0;
            int blocked = 0;
            int unresolved = 0;
            long topologyVersion = input.TopologyVersion;
            AnalogTransformProfile analogTransform = sourcePolicy == null
                ? new AnalogTransformProfile() : sourcePolicy.AnalogTransform;
            for (int index = 0; index < 4; index++)
            {
                string sourceId = SourcePrefix + index.ToString();
                if (index == currentOwnSlot)
                {
                    ownership.ClearSource(sourceId);
                    continue;
                }
                XInputPadState state = input.GetState(index);
                if (!state.Connected)
                {
                    ownership.ClearSource(sourceId);
                    continue;
                }

                RouterSourceDecision decision = sourcePolicy == null
                    ? RouterSourceDecision.Allowed("", "all-visible")
                    : sourcePolicy.Evaluate(index, topologyVersion);
                if (decision == null || !decision.Route)
                {
                    ownership.ClearSource(sourceId);
                    blocked++;
                    string reason = decision == null ? "policy-no-decision" : (decision.Reason ?? "");
                    if (reason.IndexOf("unresolved", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        reason.IndexOf("identity", StringComparison.OrdinalIgnoreCase) >= 0) unresolved++;
                    continue;
                }
                ownership.ReplaceSource(sourceId, AnalogTransformEngine.Apply(ConvertState(state), analogTransform));
                routed++;
            }
            routedControllerCount = routed;
            policyBlockedCount = blocked;
            policyUnresolvedCount = unresolved;
            policyStatus = (sourcePolicy == null ? "all-visible" : sourcePolicy.Summary) +
                "; blocked=" + blocked.ToString() + "; unresolved=" + unresolved.ToString();
            lastStatus = currentOwnSlot == 0 ? "routing-slot-zero" : "routing-nonzero-slot";
        }

        internal void Stop(string reason)
        {
            ClearAllSources();
            routedControllerCount = 0;
            policyBlockedCount = 0;
            policyUnresolvedCount = 0;
            lastStatus = string.IsNullOrWhiteSpace(reason) ? "stopped" : reason;
        }

        private void ClearAllSources()
        {
            for (int i = 0; i < 4; i++) ownership.ClearSource(SourcePrefix + i.ToString());
        }

        internal static List<InputSpec> ConvertState(XInputPadState state)
        {
            List<InputSpec> result = new List<InputSpec>();
            if (!state.Connected) return result;

            AddButton(result, state.Buttons, 0x0001, GamepadControl.DPadUp);
            AddButton(result, state.Buttons, 0x0002, GamepadControl.DPadDown);
            AddButton(result, state.Buttons, 0x0004, GamepadControl.DPadLeft);
            AddButton(result, state.Buttons, 0x0008, GamepadControl.DPadRight);
            AddButton(result, state.Buttons, 0x0010, GamepadControl.Start);
            AddButton(result, state.Buttons, 0x0020, GamepadControl.Back);
            AddButton(result, state.Buttons, 0x0040, GamepadControl.LeftThumb);
            AddButton(result, state.Buttons, 0x0080, GamepadControl.RightThumb);
            AddButton(result, state.Buttons, 0x0100, GamepadControl.LeftShoulder);
            AddButton(result, state.Buttons, 0x0200, GamepadControl.RightShoulder);
            AddButton(result, state.Buttons, 0x0400, GamepadControl.Guide);
            AddButton(result, state.Buttons, 0x1000, GamepadControl.South);
            AddButton(result, state.Buttons, 0x2000, GamepadControl.East);
            AddButton(result, state.Buttons, 0x4000, GamepadControl.West);
            AddButton(result, state.Buttons, 0x8000, GamepadControl.North);

            int leftTrigger = ScaleTrigger(state.LeftTrigger);
            int rightTrigger = ScaleTrigger(state.RightTrigger);
            if (leftTrigger != 0) result.Add(Pad(GamepadControl.LeftTrigger, 0, 0, leftTrigger));
            if (rightTrigger != 0) result.Add(Pad(GamepadControl.RightTrigger, 0, 0, rightTrigger));

            int lx = ScaleAxis(state.ThumbLX);
            int ly = ScaleAxis(state.ThumbLY);
            int rx = ScaleAxis(state.ThumbRX);
            int ry = ScaleAxis(state.ThumbRY);
            if (lx != 0 || ly != 0) result.Add(Pad(GamepadControl.LeftStick, lx, ly, 100));
            if (rx != 0 || ry != 0) result.Add(Pad(GamepadControl.RightStick, rx, ry, 100));
            return result;
        }

        private static void AddButton(List<InputSpec> result, ushort buttons, ushort mask, GamepadControl control)
        {
            if ((buttons & mask) != 0) result.Add(Pad(control, 0, 0, 100));
        }

        private static InputSpec Pad(GamepadControl control, int x, int y, int value)
        {
            return new InputSpec
            {
                Kind = InputKind.Gamepad,
                GamepadControl = control,
                GamepadX = x,
                GamepadY = y,
                GamepadValue = value
            };
        }

        internal static int ScaleTrigger(byte value)
        {
            return Math.Max(0, Math.Min(100,
                (int)Math.Round(value * 100.0 / 255.0, MidpointRounding.AwayFromZero)));
        }

        internal static int ScaleAxis(short value)
        {
            if (value == 0) return 0;
            double divisor = value < 0 ? 32768.0 : 32767.0;
            return Math.Max(-100, Math.Min(100,
                (int)Math.Round(value * 100.0 / divisor, MidpointRounding.AwayFromZero)));
        }

        public void Dispose()
        {
            if (disposed) return;
            ClearAllSources();
            enabled = false;
            disposed = true;
            ownSlot = -1;
            routedControllerCount = 0;
            policyBlockedCount = 0;
            policyUnresolvedCount = 0;
            lastStatus = "disposed";
        }
    }
}
