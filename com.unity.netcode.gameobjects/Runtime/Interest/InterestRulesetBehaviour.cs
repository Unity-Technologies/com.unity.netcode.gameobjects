using System;
using UnityEngine;

namespace Unity.Netcode.Interest
{
    public class InterestRulesetBehaviour : NetworkBehaviour
    {
        [SerializeField]
        InterestRuleset m_Ruleset;

        public override void OnNetworkSpawn()
        {
            if (!m_Ruleset)
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

            stateBehaviour.RegisterObjectWithNode(NetworkObject, m_Ruleset);
        }
    }
}
