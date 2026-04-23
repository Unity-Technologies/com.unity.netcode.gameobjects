using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.NetCode;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Test class that deliberately removes some functionality from NetworkTransform that is conditionally disabled
    /// by the presence of ghost objects in the base class. This is to help be certain that the network transform
    /// is not doing the work, but that the work is being done by N4E's snapshots.
    /// </summary>
    public class DoNothingNetworkTransform : NetworkTransform
    {
        public override void OnNetworkSpawn()
        {
            // Deliberately left empty
        }

        internal override void InternalInitialization(bool isOwnershipChange = false)
        {
            // Deliberately left empty
        }
    }
    
    public class UnifiedNetworkTransformTest : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 2;

        private GameObject m_Prefab;
        private NetworkObject m_Instance;

        protected override void OnServerAndClientsCreated()
        {
            m_Prefab = CreateNetworkObjectPrefab("Test prefab");
            SetupGhostAdapterForNetworkObjectPrefab(ref m_Prefab);
            
            m_Prefab.AddComponent<DoNothingNetworkTransform>();

            /*NetCode.Netcode.RunOnServerStarted(() =>
            {
                NetCode.Netcode.RegisterPrefabSingleWorld(m_PlayerPrefab, true);
            });*/
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {
            m_Instance = SpawnObject(m_Prefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            yield return WaitForConditionOrTimeOut(() =>
            {
                foreach (var client in m_ClientNetworkManagers)
                {
                    if (!s_GlobalNetworkObjects.ContainsKey(client.LocalClientId) || !s_GlobalNetworkObjects[client.LocalClientId].ContainsKey(m_Instance.NetworkObjectId))
                    {
                        return false;
                    }
                }

                return true;
            });
            AssertOnTimeout($"Timed out waiting for objects to spawn!");
            yield return null;
        }

        [UnityTest]
        public IEnumerator BasicMovementTest()
        {
            var authority = GetAuthorityNetworkManager();
            var originalPos = authority.LocalClient.PlayerObject.transform.position;
            var newPos = originalPos + new Vector3(1, 1, 1);
            
            m_Instance.transform.position = newPos;

            foreach (var client in m_ClientNetworkManagers)
            {
                Assert.IsTrue(Approximately(originalPos, s_GlobalNetworkObjects[client.LocalClientId][m_Instance.NetworkObjectId].transform.position));
            }

            yield return new WaitForSeconds(1);
            
            foreach (var client in m_ClientNetworkManagers)
            {
                Assert.IsTrue(Approximately(newPos, s_GlobalNetworkObjects[client.LocalClientId][m_Instance.NetworkObjectId].transform.position));
            }
        }
    }
}