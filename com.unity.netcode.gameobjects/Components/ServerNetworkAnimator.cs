using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// A prototype component for syncing Mecanim Animator state in a server-driven manner
    /// </summary>
    [AddComponentMenu("Netcode/" + nameof(ServerNetworkAnimator))]
    public class ServerNetworkAnimator : NetworkAnimator
    {
        public override bool IsAuthorityOverAnimator => IsServer;
    }
}
