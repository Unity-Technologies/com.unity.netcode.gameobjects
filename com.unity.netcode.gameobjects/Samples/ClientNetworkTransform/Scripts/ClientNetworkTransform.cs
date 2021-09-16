using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode.Samples
{
    /// <summary>
    /// Used for syncing a transform with client side changes. This includes host. Pure server as owner isn't supported by this. Please use NetworkTransform
    /// for transforms that'll always be owned by the server.
    /// </summary>
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool CanWriteToTransform => IsClient && IsOwner;

        protected override void Update()
        {
            base.Update();
            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening))
            {
                if (CanWriteToTransform)
                {
                    TryCommitTransformToServer(transform, NetworkManager.LocalTime.Time);
                }
            }
        }
    }
}
