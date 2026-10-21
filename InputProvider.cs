using System;

namespace InputStitch
{
    // Controller-input provider seam (architecture-review finding F6; development plan H2.3).
    //
    // Device addressing stays a single integer space: indices 0..3 keep their classic XInput-slot meaning and
    // remain owned by XInput semantics (takeover, slot acquisition, Router own-slot detection). Additional
    // providers (SDL3) expose their devices at synthetic indices above 3, so existing consumers - Router,
    // triggers, analog conditions, diagnostics - keep working while the provider list grows.
    //
    // The edge callbacks mirror the historical XInput service fields: a provider performs its own edge
    // detection and reports physical down/up events with the provider device index in DeviceIndex.
    internal interface IGamepadInputProvider : IDisposable
    {
        string ProviderName { get; }
        // Increment whenever ConnectedDeviceIndices membership or index ownership changes.
        // Consumers may cache membership until this version changes; state and edge polling stay live.
        long TopologyVersion { get; }
        Action<InputEventInfo> InputDown { get; set; }
        Action<InputEventInfo> InputUp { get; set; }
        void Poll();
        int[] ConnectedDeviceIndices();
        bool TryGetState(int deviceIndex, out XInputPadState state);
        bool IsTriggerSatisfied(TriggerSpec trigger);
    }
}
