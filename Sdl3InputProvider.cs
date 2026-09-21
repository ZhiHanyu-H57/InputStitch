using System;
using System.Collections.Generic;

namespace InputStitch
{
    // SDL3 implementation of the controller-input provider seam (development plan H2.3 step 2).
    //
    // Devices are published at synthetic runtime indices starting at FirstSyntheticIndex (4), so the classic
    // XInput slot semantics stay untouched for indices 0..3. Poll() enumerates devices, keeps a stable index
    // per SDL device id, maps SDL gamepad state into the app's normalized XInputPadState, raises button and
    // trigger edges (trigger hysteresis matches the XInput lane) and releases held buttons when a device
    // disappears so a macro can never stay stuck.
    internal sealed class Sdl3InputProvider : IGamepadInputProvider
    {
        internal const int FirstSyntheticIndex = 4;
        // Microsoft vendor id: SDL3 reads these pads through XInput as well, so by default the XInput lane
        // owns them and SDL3 skips them to avoid double input.
        internal const ushort MicrosoftVendorId = 0x045E;

        private static readonly ushort[] EdgeMasks =
        {
            0x0001, 0x0002, 0x0004, 0x0008, 0x0010, 0x0020, 0x0040, 0x0080,
            0x0100, 0x0200, 0x0400, 0x1000, 0x2000, 0x4000, 0x8000
        };

        private static readonly GamepadControl[] EdgeControls =
        {
            GamepadControl.DPadUp, GamepadControl.DPadDown, GamepadControl.DPadLeft, GamepadControl.DPadRight,
            GamepadControl.Start, GamepadControl.Back, GamepadControl.LeftThumb, GamepadControl.RightThumb,
            GamepadControl.LeftShoulder, GamepadControl.RightShoulder, GamepadControl.Guide,
            GamepadControl.South, GamepadControl.East, GamepadControl.West, GamepadControl.North
        };

        private readonly object sync = new object();
        private readonly ISdl3Api api;
        private readonly bool suppressXboxFamily;
        private readonly Dictionary<int, int> indexByDeviceId = new Dictionary<int, int>();
        private readonly Dictionary<int, Sdl3DeviceDescriptor> descriptorByDeviceId = new Dictionary<int, Sdl3DeviceDescriptor>();
        private readonly Dictionary<int, XInputPadState> stateByIndex = new Dictionary<int, XInputPadState>();
        private readonly Dictionary<int, bool> leftTriggerDownByIndex = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> rightTriggerDownByIndex = new Dictionary<int, bool>();
        private readonly HashSet<int> openedDevices = new HashSet<int>();
        private bool initialized;
        private bool available;
        private long topologyVersion;
        private string status = "not-initialized";

        public Sdl3InputProvider(ISdl3Api api, bool suppressXboxFamily)
        {
            if (api == null) throw new ArgumentNullException("api");
            this.api = api;
            this.suppressXboxFamily = suppressXboxFamily;
        }

        public Sdl3InputProvider(ISdl3Api api) : this(api, true) { }

        public Action<InputEventInfo> InputDown { get; set; }
        public Action<InputEventInfo> InputUp { get; set; }

        public string ProviderName { get { return "sdl3"; } }

        public bool Available
        {
            get { lock (sync) return available; }
        }

        public string StatusSummary
        {
            get { lock (sync) return status; }
        }

        public long TopologyVersion
        {
            get { lock (sync) return topologyVersion; }
        }

        public void Poll()
        {
            List<InputEventInfo> downs = new List<InputEventInfo>();
            List<InputEventInfo> ups = new List<InputEventInfo>();
            lock (sync)
            {
                if (!initialized)
                {
                    initialized = true;
                    try
                    {
                        available = api.Initialize();
                        status = available ? "ready" : "unavailable";
                    }
                    catch (Exception ex)
                    {
                        available = false;
                        status = "initialize-failed: " + ex.Message;
                    }
                }
                if (!available) return;

                try { api.Update(); }
                catch (Exception ex) { status = "update-failed: " + ex.Message; return; }

                int[] ids;
                try { ids = api.ListDeviceIds() ?? new int[0]; }
                catch (Exception ex)
                {
                    status = "enumerate-failed: " + ex.Message;
                    ids = new int[0];
                }

                HashSet<int> present = new HashSet<int>();
                foreach (int deviceId in ids)
                {
                    if (!present.Add(deviceId)) continue;
                    Sdl3DeviceDescriptor descriptor = DescribeSafely(deviceId);
                    if (descriptor == null) continue;
                    if (suppressXboxFamily && descriptor.VendorId == MicrosoftVendorId) continue;
                    if (indexByDeviceId.ContainsKey(deviceId)) continue;
                    indexByDeviceId[deviceId] = AllocateIndexLocked();
                    descriptorByDeviceId[deviceId] = descriptor;
                    try
                    {
                        if (api.Open(deviceId)) openedDevices.Add(deviceId);
                    }
                    catch { }
                    topologyVersion++;
                }

                List<int> removed = new List<int>();
                foreach (KeyValuePair<int, int> pair in indexByDeviceId)
                    if (!present.Contains(pair.Key)) removed.Add(pair.Key);
                foreach (int deviceId in removed)
                {
                    int index = indexByDeviceId[deviceId];
                    XInputPadState previous;
                    if (stateByIndex.TryGetValue(index, out previous))
                        CollectEdges(downs, ups, index, previous, new XInputPadState());
                    stateByIndex.Remove(index);
                    leftTriggerDownByIndex.Remove(index);
                    rightTriggerDownByIndex.Remove(index);
                    indexByDeviceId.Remove(deviceId);
                    descriptorByDeviceId.Remove(deviceId);
                    if (openedDevices.Remove(deviceId))
                    {
                        try { api.Close(deviceId); } catch { }
                    }
                    topologyVersion++;
                }

                foreach (KeyValuePair<int, int> pair in new List<KeyValuePair<int, int>>(indexByDeviceId))
                {
                    Sdl3DeviceSnapshot snapshot = SnapshotSafely(pair.Key);
                    if (snapshot == null) continue;
                    XInputPadState mapped = MapSnapshot(snapshot);
                    mapped.Connected = true;
                    XInputPadState previous;
                    bool hadPrevious = stateByIndex.TryGetValue(pair.Value, out previous);
                    if (!hadPrevious) previous = new XInputPadState();
                    stateByIndex[pair.Value] = mapped;
                    if (hadPrevious)
                    {
                        CollectEdges(downs, ups, pair.Value, previous, mapped);
                        CollectTriggerEdges(downs, ups, pair.Value, mapped);
                    }
                    else
                    {
                        // Baseline: a control already held when the device is first seen must not synthesize an
                        // activation, matching the XInput lane's reconnect baseline.
                        int threshold = XInputInputService.TriggerActivatePercent * 255 / 100;
                        leftTriggerDownByIndex[pair.Value] = mapped.LeftTrigger >= threshold;
                        rightTriggerDownByIndex[pair.Value] = mapped.RightTrigger >= threshold;
                    }
                }

                status = "ready; devices=" + indexByDeviceId.Count.ToString();
            }

            foreach (InputEventInfo edge in downs) Raise(InputDown, edge);
            foreach (InputEventInfo edge in ups) Raise(InputUp, edge);
        }

        public int[] ConnectedDeviceIndices()
        {
            lock (sync)
            {
                List<int> indices = new List<int>(indexByDeviceId.Values);
                indices.Sort();
                return indices.ToArray();
            }
        }

        // Current devices mapped onto the persistent identity model (see Sdl3DeviceIdentity). Used by the
        // identity service's provider lane so SDL3 pads appear in the device registry and Device Manager.
        internal List<GamingDeviceDescriptor> CurrentGamingDevices()
        {
            List<GamingDeviceDescriptor> result = new List<GamingDeviceDescriptor>();
            lock (sync)
            {
                foreach (Sdl3DeviceDescriptor descriptor in descriptorByDeviceId.Values)
                {
                    GamingDeviceDescriptor mapped = Sdl3DeviceIdentity.ToGamingDevice(descriptor);
                    if (mapped != null) result.Add(mapped);
                }
            }
            return result;
        }

        public bool TryGetState(int deviceIndex, out XInputPadState state)
        {
            lock (sync)
            {
                XInputPadState stored;
                if (stateByIndex.TryGetValue(deviceIndex, out stored) && stored.Connected)
                {
                    state = stored;
                    return true;
                }
                state = new XInputPadState();
                return false;
            }
        }

        public bool IsTriggerSatisfied(TriggerSpec trigger)
        {
            if (trigger == null || trigger.Kind != InputKind.Gamepad) return false;
            lock (sync)
            {
                if (trigger.GamepadUserIndex >= 0)
                {
                    XInputPadState specific;
                    return stateByIndex.TryGetValue(trigger.GamepadUserIndex, out specific) && IsControlDown(specific, trigger.GamepadControl);
                }
                foreach (XInputPadState candidate in stateByIndex.Values)
                    if (IsControlDown(candidate, trigger.GamepadControl)) return true;
                return false;
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (available)
                {
                    foreach (int deviceId in new List<int>(openedDevices))
                    {
                        try { api.Close(deviceId); } catch { }
                    }
                    try { api.Shutdown(); } catch { }
                }
                openedDevices.Clear();
                indexByDeviceId.Clear();
                stateByIndex.Clear();
                leftTriggerDownByIndex.Clear();
                rightTriggerDownByIndex.Clear();
                available = false;
                status = "disposed";
            }
        }

        // Maps one SDL3 gamepad snapshot onto the app's normalized pad state. SDL reports stick Y as positive
        // downward while the app (and XInput) use positive upward, so Y axes are inverted; SDL triggers are
        // 0..32767 and are scaled to the app's 0..255 trigger bytes.
        internal static XInputPadState MapSnapshot(Sdl3DeviceSnapshot snapshot)
        {
            XInputPadState state = new XInputPadState();
            state.Connected = true;
            if (snapshot == null) return state;
            bool[] buttons = snapshot.Buttons;
            if (buttons != null)
            {
                if (Pressed(buttons, Sdl3GamepadButtons.South)) state.Buttons |= 0x1000;
                if (Pressed(buttons, Sdl3GamepadButtons.East)) state.Buttons |= 0x2000;
                if (Pressed(buttons, Sdl3GamepadButtons.West)) state.Buttons |= 0x4000;
                if (Pressed(buttons, Sdl3GamepadButtons.North)) state.Buttons |= 0x8000;
                if (Pressed(buttons, Sdl3GamepadButtons.Back)) state.Buttons |= 0x0020;
                if (Pressed(buttons, Sdl3GamepadButtons.Guide)) state.Buttons |= 0x0400;
                if (Pressed(buttons, Sdl3GamepadButtons.Start)) state.Buttons |= 0x0010;
                if (Pressed(buttons, Sdl3GamepadButtons.LeftStick)) state.Buttons |= 0x0040;
                if (Pressed(buttons, Sdl3GamepadButtons.RightStick)) state.Buttons |= 0x0080;
                if (Pressed(buttons, Sdl3GamepadButtons.LeftShoulder)) state.Buttons |= 0x0100;
                if (Pressed(buttons, Sdl3GamepadButtons.RightShoulder)) state.Buttons |= 0x0200;
                if (Pressed(buttons, Sdl3GamepadButtons.DPadUp)) state.Buttons |= 0x0001;
                if (Pressed(buttons, Sdl3GamepadButtons.DPadDown)) state.Buttons |= 0x0002;
                if (Pressed(buttons, Sdl3GamepadButtons.DPadLeft)) state.Buttons |= 0x0004;
                if (Pressed(buttons, Sdl3GamepadButtons.DPadRight)) state.Buttons |= 0x0008;
            }
            short[] axes = snapshot.Axes;
            state.ThumbLX = Axis(axes, Sdl3GamepadAxes.LeftX);
            state.ThumbLY = InvertY(Axis(axes, Sdl3GamepadAxes.LeftY));
            state.ThumbRX = Axis(axes, Sdl3GamepadAxes.RightX);
            state.ThumbRY = InvertY(Axis(axes, Sdl3GamepadAxes.RightY));
            state.LeftTrigger = ScaleTrigger(Axis(axes, Sdl3GamepadAxes.LeftTrigger));
            state.RightTrigger = ScaleTrigger(Axis(axes, Sdl3GamepadAxes.RightTrigger));
            return state;
        }

        internal static byte ScaleTrigger(short value)
        {
            if (value <= 0) return 0;
            if (value >= 32767) return 255;
            return (byte)((value * 255 + 16383) / 32767);
        }

        private static bool Pressed(bool[] buttons, int index)
        {
            return buttons != null && index >= 0 && index < buttons.Length && buttons[index];
        }

        private static short Axis(short[] axes, int index)
        {
            return axes == null || index < 0 || index >= axes.Length ? (short)0 : axes[index];
        }

        private static short InvertY(short value)
        {
            return value == short.MinValue ? short.MaxValue : (short)(-value);
        }

        private static bool IsControlDown(XInputPadState state, GamepadControl control)
        {
            if (!state.Connected) return false;
            if (control == GamepadControl.LeftTrigger) return state.LeftTrigger >= XInputInputService.TriggerActivatePercent * 255 / 100;
            if (control == GamepadControl.RightTrigger) return state.RightTrigger >= XInputInputService.TriggerActivatePercent * 255 / 100;
            for (int i = 0; i < EdgeControls.Length; i++)
                if (EdgeControls[i] == control) return (state.Buttons & EdgeMasks[i]) != 0;
            return false;
        }

        private int AllocateIndexLocked()
        {
            HashSet<int> used = new HashSet<int>(indexByDeviceId.Values);
            int candidate = FirstSyntheticIndex;
            while (used.Contains(candidate)) candidate++;
            return candidate;
        }

        private void CollectEdges(List<InputEventInfo> downs, List<InputEventInfo> ups, int index, XInputPadState previous, XInputPadState current)
        {
            for (int i = 0; i < EdgeMasks.Length; i++)
            {
                bool was = (previous.Buttons & EdgeMasks[i]) != 0;
                bool now = (current.Buttons & EdgeMasks[i]) != 0;
                if (was == now) continue;
                InputEventInfo edge = new InputEventInfo
                {
                    Input = new InputSpec { Kind = InputKind.Gamepad, GamepadControl = EdgeControls[i] },
                    DeviceIndex = index
                };
                if (now) downs.Add(edge); else ups.Add(edge);
            }
        }

        private void CollectTriggerEdges(List<InputEventInfo> downs, List<InputEventInfo> ups, int index, XInputPadState current)
        {
            UpdateTriggerEdge(downs, ups, index, GamepadControl.LeftTrigger,
                current.LeftTrigger * 100 / 255, leftTriggerDownByIndex);
            UpdateTriggerEdge(downs, ups, index, GamepadControl.RightTrigger,
                current.RightTrigger * 100 / 255, rightTriggerDownByIndex);
        }

        private void UpdateTriggerEdge(List<InputEventInfo> downs, List<InputEventInfo> ups, int index, GamepadControl control,
            int percent, Dictionary<int, bool> stateMap)
        {
            bool down;
            if (!stateMap.TryGetValue(index, out down)) down = false;
            if (!down && percent >= XInputInputService.TriggerActivatePercent)
            {
                stateMap[index] = true;
                downs.Add(Edge(index, control));
            }
            else if (down && percent <= XInputInputService.TriggerReleasePercent)
            {
                stateMap[index] = false;
                ups.Add(Edge(index, control));
            }
        }

        private static InputEventInfo Edge(int index, GamepadControl control)
        {
            return new InputEventInfo
            {
                Input = new InputSpec { Kind = InputKind.Gamepad, GamepadControl = control },
                DeviceIndex = index
            };
        }

        private static void Raise(Action<InputEventInfo> handler, InputEventInfo edge)
        {
            if (handler == null) return;
            handler(edge);
        }

        private Sdl3DeviceDescriptor DescribeSafely(int deviceId)
        {
            try { return api.Describe(deviceId); }
            catch { return null; }
        }

        private Sdl3DeviceSnapshot SnapshotSafely(int deviceId)
        {
            try { return api.GetSnapshot(deviceId); }
            catch { return null; }
        }
    }
}
