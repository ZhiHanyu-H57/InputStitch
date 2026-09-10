using System.Collections.Generic;

namespace InputStitch.Tools.InputLab
{
    internal sealed class ObservationSnapshot
    {
        internal bool XInputConnected;
        internal uint XInputSlot;
        internal XInputReader.XINPUT_GAMEPAD Gamepad;
        internal readonly List<int> KeysDown = new List<int>();
        internal readonly List<string> MouseButtonsDown = new List<string>();
        internal readonly List<int> RawKeysDown = new List<int>();
        internal readonly List<string> RawMouseButtonsDown = new List<string>();
        internal bool RawInputRegistered;
        internal int RawKeyboardEvents;
        internal int RawMouseEvents;
        internal int WindowKeyboardEvents;
        internal int WindowMouseEvents;
        internal int InjectedKeyboardEvents;
        internal int InjectedMouseEvents;
        internal int LastHookKeyboardVirtualKey = -1;
        internal bool LastHookKeyboardDown;
        internal int LastRawKeyboardVirtualKey = -1;
        internal bool LastRawKeyboardDown;
    }
}
