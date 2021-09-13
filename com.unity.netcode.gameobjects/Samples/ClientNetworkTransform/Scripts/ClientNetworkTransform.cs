using Unity.Netcode.Components;

namespace Unity.Netcode.Samples
{
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool CanWriteToTransform => IsClient && IsOwner;

        protected override void Update()
        {
            base.Update();
            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening))
            {
                if (CanWriteToTransform && UpdateNetworkStateCheckDirty(ref m_LocalAuthoritativeNetworkState, NetworkManager.LocalTime.Time))
                {
                    SubmitNetworkStateServerRpc(m_LocalAuthoritativeNetworkState);
                }
            }
        }

        [ServerRpc]
        private void SubmitNetworkStateServerRpc(NetworkTransformState networkState)
        {
            m_LocalAuthoritativeNetworkState = networkState;
            m_ReplicatedNetworkState.Value = networkState;
            m_ReplicatedNetworkState.SetDirty(true);
            AddInterpolatedState(networkState);
        }
    }
}
