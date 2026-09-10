using System;
using System.Collections.Generic;

namespace InputStitch
{
    internal struct XInputPadState
    {
        public bool Connected;
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short ThumbLX;
        public short ThumbLY;
        public short ThumbRX;
        public short ThumbRY;
    }

    // Turns every visible Windows XInput slot into a first-class InputStitch input source.
    // The InputStitch-owned ViGEm Xbox slot is always excluded so generated output can never
    // feed back into controller triggers. Newly connected/re-enumerated slots establish a
    // baseline first; a held control must be released and pressed again before it can trigger.
    internal sealed class XInputInputService : IDisposable
    {
        internal const int AnyController = -1;
        internal const int TriggerActivatePercent = 80;
        internal const int TriggerReleasePercent = 70;

        private const ushort DPadUp = 0x0001;
        private const ushort DPadDown = 0x0002;
        private const ushort DPadLeft = 0x0004;
        private const ushort DPadRight = 0x0008;
        private const ushort Start = 0x0010;
        private const ushort Back = 0x0020;
        private const ushort LeftThumb = 0x0040;
        private const ushort RightThumb = 0x0080;
        private const ushort LeftShoulder = 0x0100;
        private const ushort RightShoulder = 0x0200;
        private const ushort Guide = 0x0400;
        private const ushort South = 0x1000;
        private const ushort East = 0x2000;
        private const ushort West = 0x4000;
        private const ushort North = 0x8000;

        private static readonly ButtonBinding[] ButtonBindings = new[]
        {
            new ButtonBinding(DPadUp, GamepadControl.DPadUp),
            new ButtonBinding(DPadDown, GamepadControl.DPadDown),
            new ButtonBinding(DPadLeft, GamepadControl.DPadLeft),
            new ButtonBinding(DPadRight, GamepadControl.DPadRight),
            new ButtonBinding(Start, GamepadControl.Start),
            new ButtonBinding(Back, GamepadControl.Back),
            new ButtonBinding(LeftThumb, GamepadControl.LeftThumb),
            new ButtonBinding(RightThumb, GamepadControl.RightThumb),
            new ButtonBinding(LeftShoulder, GamepadControl.LeftShoulder),
            new ButtonBinding(RightShoulder, GamepadControl.RightShoulder),
            new ButtonBinding(Guide, GamepadControl.Guide),
            new ButtonBinding(South, GamepadControl.South),
            new ButtonBinding(East, GamepadControl.East),
            new ButtonBinding(West, GamepadControl.West),
            new ButtonBinding(North, GamepadControl.North)
        };

        private struct ButtonBinding
        {
            public ushort Mask;
            public GamepadControl Control;
            public ButtonBinding(ushort mask, GamepadControl control) { Mask = mask; Control = control; }
        }

        private sealed class SlotState
        {
            public bool Connected;
            public XInputPadState Pad;
            public bool LeftTriggerDown;
            public bool RightTriggerDown;
        }

        private readonly SlotState[] slots = new SlotState[4];
        private readonly Func<int> ownXboxSlot;
        private readonly Func<int, XInputPadState> readState;
        private readonly XInputNativeReader xinput = new XInputNativeReader();
        private bool disposed;
        private int excludedIndex = -1;
        private string lastEdge = "none";

        public Action<InputEventInfo> InputDown;
        public Action<InputEventInfo> InputUp;

        internal XInputInputService(Func<int> ownXboxSlot)
            : this(ownXboxSlot, null)
        {
        }

        internal XInputInputService(Func<int> ownXboxSlot, Func<int, XInputPadState> reader)
        {
            this.ownXboxSlot = ownXboxSlot;
            readState = reader ?? ReadNativeState;
            for (int i = 0; i < slots.Length; i++) slots[i] = new SlotState();
        }

        internal int ExcludedUserIndex { get { return excludedIndex; } }
        internal string LastEdge { get { return lastEdge; } }

        internal int[] ConnectedUserIndices()
        {
            List<int> result = new List<int>();
            for (int i = 0; i < slots.Length; i++) if (slots[i].Connected) result.Add(i);
            return result.ToArray();
        }

        internal XInputPadState GetState(int index)
        {
            if (index < 0 || index >= slots.Length || index == excludedIndex || !slots[index].Connected)
                return new XInputPadState();
            XInputPadState state = slots[index].Pad;
            state.Connected = true;
            return state;
        }

        public void Poll()
        {
            if (disposed) return;
            int own = -1;
            if (ownXboxSlot != null)
            {
                try { own = ownXboxSlot(); }
                catch { own = -1; }
            }
            excludedIndex = own;

            for (int index = 0; index < slots.Length; index++)
            {
                SlotState slot = slots[index];
                if (index == own)
                {
                    if (slot.Connected) ReleaseAll(index, slot, "own-virtual-excluded");
                    ResetSlot(slot);
                    continue;
                }

                XInputPadState current;
                try { current = readState(index); }
                catch { current = new XInputPadState(); }

                if (!current.Connected)
                {
                    if (slot.Connected) ReleaseAll(index, slot, "disconnect");
                    ResetSlot(slot);
                    continue;
                }

                if (!slot.Connected)
                {
                    // Establish a baseline without generating Down edges. This prevents a held
                    // button during startup/reconnect from starting a macro unexpectedly.
                    slot.Connected = true;
                    slot.Pad = current;
                    slot.LeftTriggerDown = Percent(current.LeftTrigger) >= TriggerActivatePercent;
                    slot.RightTriggerDown = Percent(current.RightTrigger) >= TriggerActivatePercent;
                    continue;
                }

                EmitButtonEdges(index, slot.Pad.Buttons, current.Buttons);
                UpdateAnalogTrigger(index, GamepadControl.LeftTrigger, Percent(current.LeftTrigger), ref slot.LeftTriggerDown);
                UpdateAnalogTrigger(index, GamepadControl.RightTrigger, Percent(current.RightTrigger), ref slot.RightTriggerDown);
                slot.Pad = current;
            }
        }

        internal bool IsTriggerSatisfied(TriggerSpec trigger)
        {
            if (trigger == null || trigger.Kind != InputKind.Gamepad) return false;
            if (trigger.GamepadUserIndex >= 0 && trigger.GamepadUserIndex < slots.Length)
                return IsControlDown(slots[trigger.GamepadUserIndex], trigger.GamepadControl);
            for (int i = 0; i < slots.Length; i++)
                if (IsControlDown(slots[i], trigger.GamepadControl)) return true;
            return false;
        }

        private static bool IsControlDown(SlotState slot, GamepadControl control)
        {
            if (slot == null || !slot.Connected) return false;
            if (control == GamepadControl.LeftTrigger) return slot.LeftTriggerDown;
            if (control == GamepadControl.RightTrigger) return slot.RightTriggerDown;
            foreach (ButtonBinding binding in ButtonBindings)
                if (binding.Control == control) return (slot.Pad.Buttons & binding.Mask) != 0;
            return false;
        }

        private void EmitButtonEdges(int index, ushort previous, ushort current)
        {
            ushort changed = (ushort)(previous ^ current);
            if (changed == 0) return;
            foreach (ButtonBinding binding in ButtonBindings)
            {
                if ((changed & binding.Mask) == 0) continue;
                bool down = (current & binding.Mask) != 0;
                Emit(index, binding.Control, down, 100, down ? "button-down" : "button-up");
            }
        }

        private void UpdateAnalogTrigger(int index, GamepadControl control, int percent, ref bool down)
        {
            if (!down && percent >= TriggerActivatePercent)
            {
                down = true;
                Emit(index, control, true, percent, "trigger-down");
            }
            else if (down && percent <= TriggerReleasePercent)
            {
                down = false;
                Emit(index, control, false, percent, "trigger-up");
            }
        }

        private void ReleaseAll(int index, SlotState slot, string reason)
        {
            if (slot == null || !slot.Connected) return;
            foreach (ButtonBinding binding in ButtonBindings)
                if ((slot.Pad.Buttons & binding.Mask) != 0) Emit(index, binding.Control, false, 0, reason);
            if (slot.LeftTriggerDown) Emit(index, GamepadControl.LeftTrigger, false, 0, reason);
            if (slot.RightTriggerDown) Emit(index, GamepadControl.RightTrigger, false, 0, reason);
        }

        private void Emit(int index, GamepadControl control, bool down, int value, string reason)
        {
            InputEventInfo inputEvent = new InputEventInfo();
            inputEvent.DeviceIndex = index;
            inputEvent.Input = new InputSpec
            {
                Kind = InputKind.Gamepad,
                VirtualKey = 0,
                GamepadControl = control,
                GamepadValue = Math.Max(0, Math.Min(100, value))
            };
            lastEdge = "XInput " + index.ToString() + " " + control.ToString() + " " + (down ? "down" : "up") + " (" + reason + ")";
            Action<InputEventInfo> callback = down ? InputDown : InputUp;
            if (callback != null) callback(inputEvent);
        }

        private static void ResetSlot(SlotState slot)
        {
            slot.Connected = false;
            slot.Pad = new XInputPadState();
            slot.LeftTriggerDown = false;
            slot.RightTriggerDown = false;
        }

        private static int Percent(byte raw)
        {
            return (int)Math.Round(raw * 100.0 / 255.0, MidpointRounding.AwayFromZero);
        }

        private XInputPadState ReadNativeState(int index)
        {
            XInputNativeState native;
            bool connected = xinput.TryGetState((uint)index, out native);
            if (!connected) return new XInputPadState();
            return new XInputPadState
            {
                Connected = true,
                Buttons = native.Gamepad.Buttons,
                LeftTrigger = native.Gamepad.LeftTrigger,
                RightTrigger = native.Gamepad.RightTrigger,
                ThumbLX = native.Gamepad.ThumbLX,
                ThumbLY = native.Gamepad.ThumbLY,
                ThumbRX = native.Gamepad.ThumbRX,
                ThumbRY = native.Gamepad.ThumbRY
            };
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Connected) ReleaseAll(i, slots[i], "dispose");
                ResetSlot(slots[i]);
            }
        }

    }
}
