using System;
using System.Collections.Generic;

namespace InputStitch
{
    // Aggregates several input providers into one IGamepadInputProvider over a shared device-index space
    // (development plan H2.3 step 2c). Providers must publish disjoint indices - XInput owns the classic
    // 0..3 slots and the SDL3 provider starts at 4 - so no remapping is needed. If two providers ever report
    // the same index, the first provider in constructor order owns it and the duplicate is ignored.
    //
    // TopologyVersion is the sum of the child versions: it changes whenever any child's device set changes,
    // which is the property the identity service and the router policy rely on.
    internal sealed class CompositeInputProvider : IGamepadInputProvider
    {
        private readonly object sync = new object();
        private readonly IGamepadInputProvider[] providers;
        private readonly Dictionary<int, IGamepadInputProvider> ownerByIndex = new Dictionary<int, IGamepadInputProvider>();
        private Action<InputEventInfo> inputDown;
        private Action<InputEventInfo> inputUp;
        private long topologyVersion;

        public CompositeInputProvider(params IGamepadInputProvider[] providers)
        {
            if (providers == null || providers.Length == 0)
                throw new ArgumentException("At least one provider is required.", "providers");
            this.providers = providers;
            AttachChildHandlers();
        }

        public string ProviderName { get { return "composite"; } }

        public IGamepadInputProvider[] Providers { get { return (IGamepadInputProvider[])providers.Clone(); } }

        public Action<InputEventInfo> InputDown { get { return inputDown; } set { inputDown = value; } }
        public Action<InputEventInfo> InputUp { get { return inputUp; } set { inputUp = value; } }

        public long TopologyVersion { get { lock (sync) return topologyVersion; } }

        public void Poll()
        {
            foreach (IGamepadInputProvider provider in providers)
            {
                try { provider.Poll(); }
                catch { }
            }
            lock (sync)
            {
                ownerByIndex.Clear();
                long combined = 0;
                foreach (IGamepadInputProvider provider in providers)
                {
                    combined += provider.TopologyVersion;
                    foreach (int index in provider.ConnectedDeviceIndices())
                        if (!ownerByIndex.ContainsKey(index)) ownerByIndex[index] = provider;
                }
                topologyVersion = combined;
            }
        }

        public int[] ConnectedDeviceIndices()
        {
            lock (sync)
            {
                List<int> indices = new List<int>(ownerByIndex.Keys);
                indices.Sort();
                return indices.ToArray();
            }
        }

        public bool TryGetState(int deviceIndex, out XInputPadState state)
        {
            IGamepadInputProvider owner;
            lock (sync)
            {
                if (!ownerByIndex.TryGetValue(deviceIndex, out owner))
                {
                    state = new XInputPadState();
                    return false;
                }
            }
            return owner.TryGetState(deviceIndex, out state);
        }

        public bool IsTriggerSatisfied(TriggerSpec trigger)
        {
            foreach (IGamepadInputProvider provider in providers)
                if (provider.IsTriggerSatisfied(trigger)) return true;
            return false;
        }

        public void Dispose()
        {
            DetachChildHandlers();
            foreach (IGamepadInputProvider provider in providers)
            {
                try { provider.Dispose(); }
                catch { }
            }
            lock (sync) ownerByIndex.Clear();
        }

        private void AttachChildHandlers()
        {
            foreach (IGamepadInputProvider provider in providers)
            {
                provider.InputDown = ForwardDown;
                provider.InputUp = ForwardUp;
            }
        }

        private void DetachChildHandlers()
        {
            foreach (IGamepadInputProvider provider in providers)
            {
                try
                {
                    provider.InputDown = null;
                    provider.InputUp = null;
                }
                catch { }
            }
        }

        private void ForwardDown(InputEventInfo inputEvent)
        {
            Action<InputEventInfo> handler = inputDown;
            if (handler != null) handler(inputEvent);
        }

        private void ForwardUp(InputEventInfo inputEvent)
        {
            Action<InputEventInfo> handler = inputUp;
            if (handler != null) handler(inputEvent);
        }
    }
}
