// Acceptance-only model shims. Production NetworkControl/NetworkPrivilegeHelper sources are compiled unchanged.
namespace InputStitch
{
    public enum InputKind { Keyboard, Mouse }
    public sealed class TriggerSpec
    {
        public InputKind Kind; public int VirtualKey; public bool Ctrl, Alt, Shift, Win;
        public TriggerSpec Clone() { return (TriggerSpec)MemberwiseClone(); }
    }
    internal static class ModifierSafetyPolicy
    {
        internal static bool IsModifierVirtualKey(int key)
        { return key == 16 || key == 17 || key == 18 || (key >= 160 && key <= 165); }
    }
}
