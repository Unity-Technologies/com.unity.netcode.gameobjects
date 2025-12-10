#if UNIFIED_NETCODE
using System;
using Unity.NetCode;

namespace Unity.Netcode
{
    // Temporarily making this public
    // TODO: Make this internal when complete (if used)
    public partial class NetworkObjectBridge : GhostBehaviour
    {
        public Action<ulong> NetworkObjectIdChanged;
        
        internal GhostField<ulong> NetworkObjectId = new GhostField<ulong>();

        public void SetNetworkObjectId(ulong value)
        {
            NetworkObjectId.Value = value;
        }
        public override void Awake()
        {
            base.Awake();
            NetworkObjectId.ValueChanged += OnNetworkObjectIdChanged;
        } 

        private void OnNetworkObjectIdChanged(ulong value)
        {
            NetworkObjectIdChanged?.Invoke(value);
        }
    }
}
#endif
