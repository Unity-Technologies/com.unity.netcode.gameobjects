#if UNIFIED_NETCODE
using System.Collections;
using NUnit.Framework;
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
    internal class DoNothingNetworkTransform : NetworkTransform
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

    [TestFixture(HostOrServer.UnifiedHost)]
    internal class UnifiedNetworkTransformTest : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 2;

        private GameObject m_Prefab;
        private NetworkObject m_Instance;

        public UnifiedNetworkTransformTest(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override bool OnSetVerboseDebug()
        {
            return false;
        }

        protected override IEnumerator OnSetup()
        {
            // Creates the hybrid prefab
            m_Prefab = CreateNetworkObjectPrefab("HybridPrefab");
            m_Prefab.AddComponent<DoNothingNetworkTransform>();
            return base.OnSetup();
        }

        [UnityTest]
        public IEnumerator BasicMovementTest()
        {
            var authority = GetAuthorityNetworkManager();
            m_Instance = SpawnObject(m_Prefab, m_ServerNetworkManager).GetComponent<NetworkObject>();

            // Wait 5 seconds so we will dump any deferred messages if it failed on clients
            // when checking to see if it spawned or not on the clients next.
            // Enable this to debug deferred
            //yield return new WaitForSeconds(5);

            yield return WaitForSpawnedOnAllOrTimeOut(m_Instance);
            AssertOnTimeout($"Failed to spawn {m_Instance.name} on all clients!");

            VerboseDebug("All clients spawned instance!");

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
            VerboseDebug("Test Passed!");
        }
    }
}
#endif
