using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode.Interest
{
    [RequireComponent(typeof(NetworkManager))]
    public class AssetBasedInterestStateBehaviour : MonoBehaviour
    {
        Dictionary<InterestRuleset, IInterestNode<NetworkObject>> m_NodesByAsset = new Dictionary<InterestRuleset, IInterestNode<NetworkObject>>();
        NetworkManager m_NetworkManager;

        private void Awake()
        {
            m_NetworkManager = GetComponent<NetworkManager>();
        }

        public void RegisterObjectWithNode(NetworkObject networkObject, InterestRuleset ruleset)
        {
            if (!m_NodesByAsset.TryGetValue(ruleset, out var node))
            {
                node = ruleset.ConstructNode();
                m_NodesByAsset.Add(ruleset, node);
            }

            m_NetworkManager.InterestManager.AddInterestNode(ref networkObject, node);
        }
    }
}
