using System;

namespace InputStitch
{
    // Normalized SDL3 gamepad surface used by Sdl3InputProvider. The native P/Invoke implementation
    // (Sdl3NativeApi) and the provider tests both target this contract, so mapping logic is verifiable
    // without a real controller or a loaded SDL3.dll.
    internal sealed class Sdl3DeviceDescriptor
    {
        public int DeviceId = 0;
        public string Name = "";
        public ushort VendorId = 0;
        public ushort ProductId = 0;
        public string TypeName = "";
        public string Path = "";
    }

    internal sealed class Sdl3DeviceSnapshot
    {
        // Index = SDL_GamepadButton value (see Sdl3GamepadButtons). Missing entries count as not pressed.
        public bool[] Buttons = null;
        // Index = SDL_GamepadAxis value (see Sdl3GamepadAxes). Missing entries count as zero.
        public short[] Axes = null;
    }

    internal static class Sdl3GamepadButtons
    {
        // SDL3 enum order (SDL_gamepad.h, release-3.4.16).
        public const int South = 0;
        public const int East = 1;
        public const int West = 2;
        public const int North = 3;
        public const int Back = 4;
        public const int Guide = 5;
        public const int Start = 6;
        public const int LeftStick = 7;
        public const int RightStick = 8;
        public const int LeftShoulder = 9;
        public const int RightShoulder = 10;
        public const int DPadUp = 11;
        public const int DPadDown = 12;
        public const int DPadLeft = 13;
        public const int DPadRight = 14;
        public const int Misc1 = 15;
        public const int Count = 25;
    }

    internal static class Sdl3GamepadAxes
    {
        public const int LeftX = 0;
        public const int LeftY = 1;
        public const int RightX = 2;
        public const int RightY = 3;
        public const int LeftTrigger = 4;
        public const int RightTrigger = 5;
        public const int Count = 6;
    }

    internal interface ISdl3Api
    {
        // Returns false when the SDL3 runtime is unavailable; the provider then stays inert.
        bool Initialize();
        void Shutdown();
        int[] ListDeviceIds();
        Sdl3DeviceDescriptor Describe(int deviceId);
        bool Open(int deviceId);
        void Close(int deviceId);
        Sdl3DeviceSnapshot GetSnapshot(int deviceId);
    }
}
