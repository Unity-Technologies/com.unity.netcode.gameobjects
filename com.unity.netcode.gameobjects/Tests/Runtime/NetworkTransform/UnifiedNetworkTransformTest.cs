#if UNIFIED_NETCODE
using System.Collections;
using System.Text;
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

        // This is the validation test for the unified API, so it always runs during a unified test pass.
        protected override bool UseUnifiedTests()
        {
            return true;
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

        private bool ValidatePosition(StringBuilder errorLog)
        {
            var authorityPosition = m_AuthorityInstanceTransform.position;
            foreach (var client in m_ClientNetworkManagers)
            {
                var instancePosition = client.SpawnManager.SpawnedObjects[m_Instance.NetworkObjectId].transform.position;
                if (!Approximately(authorityPosition, instancePosition))
                {
                    errorLog.AppendLine($"[Client-{client.LocalClientId}] Clone instance position {GetVector3Values(instancePosition)} " +
                        $"does not equal the authority's {GetVector3Values(authorityPosition)}");
                }
            }
            return errorLog.Length == 0;
        }

        private Transform m_AuthorityInstanceTransform;

        [UnityTest]
        public IEnumerator BasicMovementTest()
        {
            var authority = GetAuthorityNetworkManager();
            m_Instance = SpawnObject(m_Prefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            m_AuthorityInstanceTransform = m_Instance.transform;

            yield return WaitForSpawnedOnAllOrTimeOut(m_Instance);
            AssertOnTimeout($"Failed to spawn {m_Instance.name} on all clients!");

            VerboseDebug("All clients spawned instance!");

            yield return WaitForConditionOrTimeOut(ValidatePosition);
            AssertOnTimeout($"A client failed to synchronize the position on the initial spawn!");

            var originalPos = authority.LocalClient.PlayerObject.transform.position;
            var newPos = originalPos + new Vector3(1, 1, 1);

            m_Instance.transform.position = newPos;

            yield return WaitForConditionOrTimeOut(ValidatePosition);
            AssertOnTimeout($"A client failed to synchronize the changed position: {GetVector3Values(newPos)}!");
            VerboseDebug("Test Passed!");
        }
    }
}
#endif
