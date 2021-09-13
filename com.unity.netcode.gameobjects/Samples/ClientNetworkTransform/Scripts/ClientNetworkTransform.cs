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
                if (CanWriteToTransform && UpdateNetworkStateCheckDirty(ref LocalAuthoritativeNetworkState, NetworkManager.LocalTime.Time))
                {
                    SubmitNetworkStateServerRpc(LocalAuthoritativeNetworkState);
                }
            }
        }

        [ServerRpc]
        private void SubmitNetworkStateServerRpc(NetworkTransformState networkState)
        {
            LocalAuthoritativeNetworkState = networkState;

            AddInterpolatedState(networkState);

            // state application and state sending to other clients will be done in next update
        }
    }
}
