using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode.Interest
{
    [RequireComponent(typeof(NetworkManager))]
    public class AssetBasedInterestStateBehaviour : MonoBehaviour
    {
        Dictionary<InterestNodeAsset, IInterestNode<NetworkObject>> m_NodesByAsset = new Dictionary<InterestNodeAsset, IInterestNode<NetworkObject>>();
        NetworkManager m_NetworkManager;

        private void Awake()
        {
            m_NetworkManager = GetComponent<NetworkManager>();
        }

        public void RegisterObjectWithNode(NetworkObject networkObject, InterestNodeAsset nodeAsset)
        {
            if (!m_NodesByAsset.TryGetValue(nodeAsset, out var node))
            {
                node = nodeAsset.ConstructNode();
                m_NodesByAsset.Add(nodeAsset, node);
            }

            m_NetworkManager.InterestManager.AddInterestNode(ref networkObject, node);
        }
    }
}
