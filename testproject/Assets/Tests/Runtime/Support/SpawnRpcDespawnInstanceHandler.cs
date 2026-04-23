using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

namespace TestProject.RuntimeTests.Support
{
    public class SpawnRpcDespawnInstanceHandler : INetworkPrefabInstanceHandler
    {
        private uint m_PrefabHash;

        public bool WasSpawned = false;
        public bool WasDestroyed = false;
        private NetworkManager m_NetworkManager;

        public SpawnRpcDespawnInstanceHandler(uint prefabHash, NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            m_PrefabHash = prefabHash;
        }

        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            WasSpawned = true;

            if (ownerClientId != NetworkManager.ServerClientId)
            {
                Assert.AreEqual(NetworkUpdateStage.EarlyUpdate, NetworkUpdateLoop.UpdateStage);
            }

            // See if there is a valid registered NetworkPrefabOverrideLink associated with the provided prefabHash
            GameObject networkPrefabReference = null;
            if (m_NetworkManager.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks.ContainsKey(m_PrefabHash))
            {
                switch (m_NetworkManager.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks[m_PrefabHash].Override)
                {
                    default:
                    case NetworkPrefabOverride.None:
                        networkPrefabReference = m_NetworkManager.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks[m_PrefabHash].Prefab;
                        break;
                    case NetworkPrefabOverride.Hash:
                    case NetworkPrefabOverride.Prefab:
                        networkPrefabReference = m_NetworkManager.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks[m_PrefabHash].OverridingTargetPrefab;
                        break;
                }
            }

            // If not, then there is an issue (user possibly didn't register the prefab properly?)
            if (networkPrefabReference == null)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    NetworkLog.LogError($"[{nameof(m_PrefabHash)}={m_PrefabHash}] Failed to create object locally. {nameof(NetworkPrefab)} could not be found. Is the prefab registered with {nameof(NetworkManager)}?");
                }
                return null;
            }

            // Otherwise, instantiate an instance of the NetworkPrefab linked to the prefabHash
            return Object.Instantiate(networkPrefabReference, position, rotation).GetComponent<NetworkObject>();
        }

        public void Destroy(NetworkObject networkObject)
        {
            if (m_NetworkManager.ShutdownInProgress)
            {
                return;
            }
            WasDestroyed = true;
            if (!networkObject.NetworkManager.IsServer)
            {
                Assert.AreEqual(NetworkUpdateStage.EarlyUpdate, NetworkUpdateLoop.UpdateStage);
            }
            Object.Destroy(networkObject.gameObject);
        }
    }
}
