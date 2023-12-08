using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;


namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Tests that check OnNetworkDespawn being invoked
    /// </summary>
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class NetworkObjectOnNetworkDespawnTests : NetcodeIntegrationTest
    {
        private const string k_ObjectName = "TestDespawn";
        public enum InstanceType
        {
            Server,
            Client
        }

        protected override int NumberOfClients => 1;
        private GameObject m_ObjectToSpawn;
        private HostOrServer m_HostOrServer;
        public NetworkObjectOnNetworkDespawnTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
            m_HostOrServer = hostOrServer;
        }

        internal class OnNetworkDespawnTestComponent : NetworkBehaviour
        {
            public static bool OnServerNetworkDespawnCalled { get; internal set; }
            public static bool OnClientNetworkDespawnCalled { get; internal set; }

            public override void OnNetworkSpawn()
            {
                if (IsServer)
                {
                    OnServerNetworkDespawnCalled = false;
                }
                else
                {
                    OnClientNetworkDespawnCalled = false;
                }
                base.OnNetworkSpawn();
            }

            public override void OnNetworkDespawn()
            {
                if (IsServer)
                {
                    OnServerNetworkDespawnCalled = true;
                }
                else
                {
                    OnClientNetworkDespawnCalled = true;
                }
                base.OnNetworkDespawn();
            }
        }

        protected override void OnServerAndClientsCreated()
        {
            m_ObjectToSpawn = CreateNetworkObjectPrefab(k_ObjectName);
            m_ObjectToSpawn.AddComponent<OnNetworkDespawnTestComponent>();
            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// This test validates that <see cref="NetworkBehaviour.OnNetworkDespawn"/> is invoked when the
        /// <see cref="NetworkManager"/> is shutdown.
        /// </summary>
        [UnityTest]
        public IEnumerator TestNetworkObjectDespawnOnShutdown()
        {
            // Spawn the test object
            var spawnedObject = SpawnObject(m_ObjectToSpawn, m_ServerNetworkManager);
            var spawnedNetworkObject = spawnedObject.GetComponent<NetworkObject>();

            // Wait for the client to spawn the object
            yield return WaitForConditionOrTimeOut(() =>
            {
                if (!s_GlobalNetworkObjects.ContainsKey(m_ClientNetworkManagers[0].LocalClientId))
                {
                    return false;
                }
                if (!s_GlobalNetworkObjects[m_ClientNetworkManagers[0].LocalClientId].ContainsKey(spawnedNetworkObject.NetworkObjectId))
                {
                    return false;
                }
                return true;
            });

            AssertOnTimeout($"Timed out waiting for client to spawn {k_ObjectName}!");

            // Confirm it is not set before shutting down the NetworkManager
            Assert.IsFalse(OnNetworkDespawnTestComponent.OnClientNetworkDespawnCalled, "[Client-side] despawn state is already set (should not be set at this point)!");
            Assert.IsFalse(OnNetworkDespawnTestComponent.OnServerNetworkDespawnCalled, $"[{m_HostOrServer}-side] despawn state is already set (should not be set at this point)!");

            // Shutdown the client-side first to validate the client-side instance invokes OnNetworkDespawn
            m_ClientNetworkManagers[0].Shutdown();
            yield return WaitForConditionOrTimeOut(() => OnNetworkDespawnTestComponent.OnClientNetworkDespawnCalled);
            AssertOnTimeout($"[Client-side] Timed out waiting for {k_ObjectName}'s {nameof(NetworkBehaviour.OnNetworkDespawn)} to be invoked!");

            // Shutdown the servr-host-side second to validate servr-host-side instance invokes OnNetworkDespawn
            m_ServerNetworkManager.Shutdown();
            yield return WaitForConditionOrTimeOut(() => OnNetworkDespawnTestComponent.OnClientNetworkDespawnCalled);
            AssertOnTimeout($"[{m_HostOrServer}-side]Timed out waiting for {k_ObjectName}'s {nameof(NetworkBehaviour.OnNetworkDespawn)} to be invoked!");
        }
    }
}
