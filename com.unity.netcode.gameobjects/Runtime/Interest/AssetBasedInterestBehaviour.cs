using System;
using UnityEngine;

namespace Unity.Netcode.Interest
{
    public class AssetBasedInterestBehaviour : NetworkBehaviour
    {
        [SerializeField]
        InterestNodeAsset m_NodeAsset;

        public override void OnNetworkSpawn()
        {
            if (!m_NodeAsset)
            {
                return;
            }

            if (!IsHost)
            {
                return;
            }

            var stateBehaviour = NetworkManager.GetComponent<AssetBasedInterestStateBehaviour>();
            if (!stateBehaviour)
            {
                stateBehaviour = NetworkManager.gameObject.AddComponent<AssetBasedInterestStateBehaviour>();
            }

            stateBehaviour.RegisterObjectWithNode(NetworkObject, m_NodeAsset);
        }
    }
}
