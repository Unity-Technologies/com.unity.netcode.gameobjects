using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// A prototype component for syncing Mecanim Animator state in a client-driven manner
    /// </summary>
    [AddComponentMenu("Netcode/" + nameof(ClientNetworkAnimator))]
    public class ClientNetworkAnimator : NetworkAnimator
    {
        public override bool IsAuthorityOverAnimator => IsClient && IsOwner;
    }
}
