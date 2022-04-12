using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode.Samples
{
    /// <summary>
    /// Used for syncing a transform with client side changes. This includes host. Pure server as owner isn't supported by this. Please use NetworkTransform
    /// for transforms that'll always be owned by the server.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override NetworkVariable<NetworkTransformState> m_ReplicatedNetworkState { get; } = new(
            new NetworkTransformState(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Used to determine who can write to this transform. Owner client only.
        /// This imposes state to the server. This is putting trust on your clients. Make sure no security-sensitive features use this transform.
        // This is public to make sure that users don't depend on IsOwner check in their code. If this logic changes in the future, we can make it invisible here
        /// </summary>
        public override bool CanCommitToTransform => IsOwner;
    }
}
