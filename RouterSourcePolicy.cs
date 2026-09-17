using System;
using System.Collections.Generic;

namespace InputStitch
{
    public static class RouterSourceModes
    {
        public const string AllVisible = "AllVisible";
        public const string SelectedDevices = "SelectedDevices";

        public static string Normalize(string value)
        {
            return string.Equals(value, SelectedDevices, StringComparison.OrdinalIgnoreCase)
                ? SelectedDevices : AllVisible;
        }
    }

    [Serializable]
    public sealed class RouterSourcePolicyConfig
    {
        public string Mode = RouterSourceModes.AllVisible;
        public List<string> SelectedDeviceKeys = new List<string>();
        // Identity-by-default analog transform applied independently to every routed source before
        // Output Ownership performs its existing cross-source merge.
        public AnalogTransformProfile AnalogTransform = new AnalogTransformProfile();

        public RouterSourcePolicyConfig Clone()
        {
            RouterSourcePolicyConfig copy = new RouterSourcePolicyConfig();
            copy.Mode = RouterSourceModes.Normalize(Mode);
            copy.SelectedDeviceKeys = SelectedDeviceKeys == null
                ? new List<string>() : new List<string>(SelectedDeviceKeys);
            copy.AnalogTransform = AnalogTransform == null ? new AnalogTransformProfile() : AnalogTransform.Clone();
            Normalize(copy);
            return copy;
        }

        public static void Normalize(RouterSourcePolicyConfig value)
        {
            if (value == null) return;
            value.Mode = RouterSourceModes.Normalize(value.Mode);
            if (value.SelectedDeviceKeys == null) value.SelectedDeviceKeys = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> normalized = new List<string>();
            foreach (string key in value.SelectedDeviceKeys)
            {
                string clean = (key ?? "").Trim();
                if (clean.Length == 0 || !seen.Add(clean)) continue;
                normalized.Add(clean);
            }
            value.SelectedDeviceKeys = normalized;
            if (value.AnalogTransform == null) value.AnalogTransform = new AnalogTransformProfile();
            AnalogTransformProfile.Normalize(value.AnalogTransform);
        }
    }

    internal sealed class RouterSourceDecision
    {
        public bool Route;
        public string DeviceKey = "";
        public string Reason = "";

        internal static RouterSourceDecision Allowed(string key, string reason)
        {
            return new RouterSourceDecision { Route = true, DeviceKey = key ?? "", Reason = reason ?? "allowed" };
        }

        internal static RouterSourceDecision Blocked(string key, string reason)
        {
            return new RouterSourceDecision { Route = false, DeviceKey = key ?? "", Reason = reason ?? "blocked" };
        }
    }

    internal interface IRouterSourcePolicyEvaluator
    {
        RouterSourceDecision Evaluate(int xinputSlot, long topologyVersion);
        bool IsAllVisible { get; }
        AnalogTransformProfile AnalogTransform { get; }
        string Summary { get; }
    }

    // Persistent policy is keyed only by stable DeviceKey. XInput slot is accepted here solely as
    // transient runtime context for a binding that DeviceIdentityService has already proven.
    internal sealed class RouterSourcePolicyEvaluator : IRouterSourcePolicyEvaluator
    {
        private readonly DeviceIdentityService identities;
        private readonly object sync = new object();
        private string mode = RouterSourceModes.AllVisible;
        private HashSet<string> selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private AnalogTransformProfile analogTransform = new AnalogTransformProfile();

        internal RouterSourcePolicyEvaluator(DeviceIdentityService deviceIdentities, RouterSourcePolicyConfig initial)
        {
            identities = deviceIdentities;
            Update(initial);
        }

        internal void Update(RouterSourcePolicyConfig value)
        {
            RouterSourcePolicyConfig copy = value == null ? new RouterSourcePolicyConfig() : value.Clone();
            HashSet<string> next = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in copy.SelectedDeviceKeys) next.Add(key);
            lock (sync)
            {
                mode = copy.Mode;
                selected = next;
                analogTransform = copy.AnalogTransform == null ? new AnalogTransformProfile() : copy.AnalogTransform.Clone();
            }
        }

        public bool IsAllVisible
        {
            get
            {
                lock (sync) return string.Equals(mode, RouterSourceModes.AllVisible, StringComparison.OrdinalIgnoreCase);
            }
        }

        public AnalogTransformProfile AnalogTransform
        {
            get
            {
                lock (sync) return analogTransform == null ? new AnalogTransformProfile() : analogTransform.Clone();
            }
        }

        public string Summary
        {
            get
            {
                lock (sync)
                {
                    string selection = string.Equals(mode, RouterSourceModes.AllVisible, StringComparison.OrdinalIgnoreCase)
                        ? "all-visible" : "selected-devices=" + selected.Count.ToString();
                    string transform = analogTransform == null ? "identity" : analogTransform.Summary;
                    return string.Equals(transform, "identity", StringComparison.Ordinal)
                        ? selection : selection + "; analog=" + transform;
                }
            }
        }

        public RouterSourceDecision Evaluate(int xinputSlot, long topologyVersion)
        {
            bool all;
            lock (sync) all = string.Equals(mode, RouterSourceModes.AllVisible, StringComparison.OrdinalIgnoreCase);
            if (all) return RouterSourceDecision.Allowed("", "all-visible");

            if (identities == null) return RouterSourceDecision.Blocked("", "identity-unavailable");
            string key;
            if (!identities.TryResolveDeviceKeyForXInputSlot(xinputSlot, topologyVersion, out key))
                return RouterSourceDecision.Blocked("", "unresolved-device");

            bool isSelected;
            lock (sync) isSelected = selected.Contains(key);
            return isSelected
                ? RouterSourceDecision.Allowed(key, "selected-device")
                : RouterSourceDecision.Blocked(key, "device-not-selected");
        }
    }
}
