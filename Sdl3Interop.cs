using System;

namespace InputStitch
{
    // SDL3 source-state contract. The native binding samples raw SDL controls;
    // Sdl3InputProvider alone normalizes their mapping, axes and trigger values.
    // Test implementations exercise the same contract without a loaded SDL3.dll.
    internal sealed class Sdl3DeviceDescriptor
    {
        public int DeviceId = 0;
        public string Name = "";
        public ushort VendorId = 0;
        public ushort ProductId = 0;
        public string TypeName = "";
        public string Path = "";
    }

    internal struct Sdl3DeviceSnapshot
    {
        // Bit index = SDL_GamepadButton value. This independent value frame never
        // exposes mutable native/scratch buffers or allocates per sampled device.
        public uint Buttons;
        public short LeftX, LeftY, RightX, RightY, LeftTrigger, RightTrigger;
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
        // Refreshes device state (SDL_UpdateGamepads) before snapshots are read.
        void Update();
        // Read-only member snapshot. An implementation may reuse it while members
        // are unchanged; it must replace rather than mutate a published array.
        int[] ListDeviceIds();
        Sdl3DeviceDescriptor Describe(int deviceId);
        bool Open(int deviceId);
        void Close(int deviceId);
        // False means no live handle; the out frame is zero in that case.
        bool TryGetSnapshot(int deviceId, out Sdl3DeviceSnapshot snapshot);
    }
}
