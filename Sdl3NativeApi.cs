using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace InputStitch
{
    // Native SDL3 binding for ISdl3Api (development plan H2.3 step 2b).
    //
    // Functions are resolved dynamically through LoadLibrary/GetProcAddress, so the application keeps working
    // unchanged when SDL3.dll is absent: the provider simply reports "unavailable" instead of failing to
    // start. State is read with the SDL3 per-control getters (SDL_GetGamepadButton / SDL_GetGamepadAxis after
    // SDL_UpdateGamepads), which is the API shape SDL 3.4 actually provides.
    internal sealed class Sdl3NativeApi : ISdl3Api, IDisposable
    {
        internal const uint SdlInitGamepad = 0x00002000;
        // Optional override used by developer machines and the smoke test.
        internal const string DllPathEnvironmentVariable = "INPUTSTITCH_SDL3_DLL";
        // Controls queried per poll: the 15 mapped buttons (South..DPadRight) plus the 6 axes.
        private const int FirstMappedButton = 0;
        private const int LastMappedButton = 14;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryW(string fileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr module, string name);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr module);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private delegate bool InitDelegate(uint flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SimpleDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void QuitDelegate(uint flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GamepadsDelegate(out int count);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr StringForIdDelegate(int deviceId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate ushort UshortForIdDelegate(int deviceId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int IntForIdDelegate(int deviceId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr OpenGamepadDelegate(int deviceId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CloseGamepadDelegate(IntPtr gamepad);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private delegate bool ButtonDelegate(IntPtr gamepad, int button);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate short AxisDelegate(IntPtr gamepad, int axis);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FreeDelegate(IntPtr pointer);

        private readonly string explicitPath;
        private readonly Dictionary<int, IntPtr> handles = new Dictionary<int, IntPtr>();
        private IntPtr module = IntPtr.Zero;
        private bool initialized;
        private bool available;
        private string status = "not-initialized";

        private InitDelegate sdlInit;
        private QuitDelegate sdlQuitSubSystem;
        private SimpleDelegate sdlUpdateGamepads;
        private SimpleDelegate sdlUpdateJoysticks;
        private GamepadsDelegate sdlGetGamepads;
        private StringForIdDelegate sdlGetGamepadNameForId;
        private StringForIdDelegate sdlGetGamepadPathForId;
        private UshortForIdDelegate sdlGetGamepadVendorForId;
        private UshortForIdDelegate sdlGetGamepadProductForId;
        private IntForIdDelegate sdlGetGamepadTypeForId;
        private OpenGamepadDelegate sdlOpenGamepad;
        private CloseGamepadDelegate sdlCloseGamepad;
        private ButtonDelegate sdlGetGamepadButton;
        private AxisDelegate sdlGetGamepadAxis;
        private FreeDelegate sdlFree;

        public Sdl3NativeApi() : this(null) { }

        public Sdl3NativeApi(string explicitLibraryPath)
        {
            explicitPath = explicitLibraryPath;
        }

        public bool Available { get { return available; } }
        public string StatusSummary { get { return status; } }

        public bool Initialize()
        {
            if (initialized) return available;
            initialized = true;
            string path = ResolveLibraryPath(explicitPath);
            try { module = LoadLibraryW(path); }
            catch (Exception ex)
            {
                status = "load-failed: " + ex.Message;
                return false;
            }
            if (module == IntPtr.Zero)
            {
                status = "SDL3.dll not found (" + path + ")";
                return false;
            }
            if (!ResolveRequiredFunctions())
            {
                status = "SDL3.dll is missing required entry points";
                FreeModule();
                return false;
            }
            try
            {
                if (!sdlInit(SdlInitGamepad))
                {
                    status = "SDL_Init(SDL_INIT_GAMEPAD) failed";
                    FreeModule();
                    return false;
                }
            }
            catch (Exception ex)
            {
                status = "init-failed: " + ex.Message;
                FreeModule();
                return false;
            }
            available = true;
            status = "ready";
            return true;
        }

        public void Update()
        {
            if (!available) return;
            if (sdlUpdateJoysticks != null) sdlUpdateJoysticks();
            if (sdlUpdateGamepads != null) sdlUpdateGamepads();
        }

        public int[] ListDeviceIds()
        {
            if (!available || sdlGetGamepads == null) return new int[0];
            int count = 0;
            IntPtr pointer = sdlGetGamepads(out count);
            if (pointer == IntPtr.Zero || count <= 0) return new int[0];
            try
            {
                int[] ids = new int[count];
                for (int i = 0; i < count; i++) ids[i] = Marshal.ReadInt32(pointer, i * 4);
                return ids;
            }
            finally
            {
                if (sdlFree != null) sdlFree(pointer);
            }
        }

        public Sdl3DeviceDescriptor Describe(int deviceId)
        {
            if (!available) return null;
            Sdl3DeviceDescriptor descriptor = new Sdl3DeviceDescriptor();
            descriptor.DeviceId = deviceId;
            descriptor.Name = ReadString(sdlGetGamepadNameForId, deviceId);
            descriptor.Path = ReadString(sdlGetGamepadPathForId, deviceId);
            if (sdlGetGamepadVendorForId != null) descriptor.VendorId = sdlGetGamepadVendorForId(deviceId);
            if (sdlGetGamepadProductForId != null) descriptor.ProductId = sdlGetGamepadProductForId(deviceId);
            if (sdlGetGamepadTypeForId != null) descriptor.TypeName = GamepadTypeName(sdlGetGamepadTypeForId(deviceId));
            return descriptor;
        }

        public bool Open(int deviceId)
        {
            if (!available || sdlOpenGamepad == null) return false;
            if (handles.ContainsKey(deviceId)) return true;
            IntPtr handle = sdlOpenGamepad(deviceId);
            if (handle == IntPtr.Zero) return false;
            handles[deviceId] = handle;
            return true;
        }

        public void Close(int deviceId)
        {
            IntPtr handle;
            if (!handles.TryGetValue(deviceId, out handle)) return;
            handles.Remove(deviceId);
            if (handle != IntPtr.Zero && sdlCloseGamepad != null) sdlCloseGamepad(handle);
        }

        public Sdl3DeviceSnapshot GetSnapshot(int deviceId)
        {
            if (!available) return null;
            IntPtr handle;
            if (!handles.TryGetValue(deviceId, out handle) || handle == IntPtr.Zero) return null;
            Sdl3DeviceSnapshot snapshot = new Sdl3DeviceSnapshot();
            bool[] buttons = new bool[Sdl3GamepadButtons.Count];
            for (int index = FirstMappedButton; index <= LastMappedButton && index < buttons.Length; index++)
                buttons[index] = sdlGetGamepadButton != null && sdlGetGamepadButton(handle, index);
            snapshot.Buttons = buttons;
            short[] axes = new short[Sdl3GamepadAxes.Count];
            for (int axis = 0; axis < axes.Length; axis++)
                axes[axis] = sdlGetGamepadAxis == null ? (short)0 : sdlGetGamepadAxis(handle, axis);
            snapshot.Axes = axes;
            return snapshot;
        }

        public void Shutdown()
        {
            if (!available) return;
            try
            {
                foreach (KeyValuePair<int, IntPtr> pair in new List<KeyValuePair<int, IntPtr>>(handles))
                    Close(pair.Key);
                if (sdlQuitSubSystem != null) sdlQuitSubSystem(SdlInitGamepad);
            }
            catch { }
            available = false;
            status = "shutdown";
        }

        public void Dispose()
        {
            Shutdown();
            FreeModule();
        }

        private void FreeModule()
        {
            if (module != IntPtr.Zero)
            {
                try { FreeLibrary(module); } catch { }
                module = IntPtr.Zero;
            }
        }

        internal static string ResolveLibraryPath(string explicitPath)
        {
            if (!string.IsNullOrWhiteSpace(explicitPath)) return explicitPath;
            string fromEnvironment = Environment.GetEnvironmentVariable(DllPathEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(fromEnvironment)) return fromEnvironment;
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string beside = Path.Combine(baseDirectory, "SDL3.dll");
            if (File.Exists(beside)) return beside;
            string arch = Environment.Is64BitProcess ? "x64" : "x86";
            string development = Path.Combine(baseDirectory, "third-party", "SDL3", arch, "SDL3.dll");
            if (File.Exists(development)) return development;
            return "SDL3.dll";
        }

        internal static string GamepadTypeName(int type)
        {
            switch (type)
            {
                case 1: return "standard";
                case 2: return "xbox360";
                case 3: return "xboxone";
                case 4: return "ps3";
                case 5: return "ps4";
                case 6: return "ps5";
                case 7: return "switchpro";
                default: return "unknown";
            }
        }

        private bool ResolveRequiredFunctions()
        {
            sdlInit = Resolve<InitDelegate>("SDL_Init");
            sdlGetGamepads = Resolve<GamepadsDelegate>("SDL_GetGamepads");
            sdlOpenGamepad = Resolve<OpenGamepadDelegate>("SDL_OpenGamepad");
            sdlCloseGamepad = Resolve<CloseGamepadDelegate>("SDL_CloseGamepad");
            sdlGetGamepadButton = Resolve<ButtonDelegate>("SDL_GetGamepadButton");
            sdlGetGamepadAxis = Resolve<AxisDelegate>("SDL_GetGamepadAxis");
            sdlGetGamepadNameForId = Resolve<StringForIdDelegate>("SDL_GetGamepadNameForID");
            sdlGetGamepadVendorForId = Resolve<UshortForIdDelegate>("SDL_GetGamepadVendorForID");
            sdlGetGamepadProductForId = Resolve<UshortForIdDelegate>("SDL_GetGamepadProductForID");
            sdlGetGamepadTypeForId = Resolve<IntForIdDelegate>("SDL_GetGamepadTypeForID");
            // Optional entry points: absence only disables the corresponding nicety.
            sdlQuitSubSystem = Resolve<QuitDelegate>("SDL_QuitSubSystem");
            sdlUpdateGamepads = Resolve<SimpleDelegate>("SDL_UpdateGamepads");
            sdlUpdateJoysticks = Resolve<SimpleDelegate>("SDL_UpdateJoysticks");
            sdlGetGamepadPathForId = Resolve<StringForIdDelegate>("SDL_GetGamepadPathForID");
            sdlFree = Resolve<FreeDelegate>("SDL_free");
            return sdlInit != null && sdlGetGamepads != null && sdlOpenGamepad != null && sdlCloseGamepad != null &&
                sdlGetGamepadButton != null && sdlGetGamepadAxis != null && sdlGetGamepadNameForId != null;
        }

        private T Resolve<T>(string name) where T : class
        {
            if (module == IntPtr.Zero) return null;
            IntPtr address = GetProcAddress(module, name);
            if (address == IntPtr.Zero) return null;
            return Marshal.GetDelegateForFunctionPointer(address, typeof(T)) as T;
        }

        private static string ReadString(StringForIdDelegate getter, int deviceId)
        {
            if (getter == null) return "";
            IntPtr pointer = getter(deviceId);
            return pointer == IntPtr.Zero ? "" : (Marshal.PtrToStringAnsi(pointer) ?? "");
        }
    }
}
