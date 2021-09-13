using Unity.Netcode.Components;

namespace Unity.Netcode.Samples
{
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool CanWriteToTransform => IsClient && IsOwner;

        protected override void DoUpdateToGhosts()
        {
            if (UpdateNetworkStateCheckDirty(ref LocalAuthoritativeNetworkState, NetworkManager.LocalTime.Time))
            {
                SubmitNetworkStateServerRpc(LocalAuthoritativeNetworkState);
            }
        }

        [ServerRpc]
        private void SubmitNetworkStateServerRpc(NetworkState networkState)
        {
            LocalAuthoritativeNetworkState = networkState;

            // as a server, apply whatever networkstate owner client sent to us, to make NetworkTransform move locally on the server
            ApplyNetworkStateFromAuthority(networkState);

            // as a server, update netvar<networkstate> to cause it to be replicated down to other non-owner clients
            UpdateNetworkVariable();
        }
    }
}
