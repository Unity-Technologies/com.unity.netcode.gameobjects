using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(NetworkTopologyTypes.DistributedAuthority, true)]
    [TestFixture(NetworkTopologyTypes.ClientServer, true)]
    [TestFixture(NetworkTopologyTypes.DistributedAuthority, false)]
    [TestFixture(NetworkTopologyTypes.ClientServer, false)]
    internal class RpcDuringOnNetworkSpawn : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private bool m_EnableSceneManagement;

        public RpcDuringOnNetworkSpawn(NetworkTopologyTypes topologyType, bool enableSceneManagement) : base(topologyType)
        {
            m_EnableSceneManagement = enableSceneManagement;
        }

        /// <summary>
        /// Enables or disables scene management when the <see cref="NetworkManager"/>s
        /// are created.
        /// </summary>
        protected override void OnServerAndClientsCreated()
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                networkManager.NetworkConfig.EnableSceneManagement = m_EnableSceneManagement;
            }
            base.OnServerAndClientsCreated();
        }

        protected override void OnPlayerPrefabGameObjectCreated()
        {
            var first = m_PlayerPrefab.AddComponent<FirstNetworkBehaviour>();
            var second = m_PlayerPrefab.AddComponent<SecondNetworkBehaviour>();
            first.SecondNetworkBehaviour = second;
            base.OnPlayerPrefabGameObjectCreated();
        }

        /// <summary>
        /// Validates that invoking an RPC on another NetworkBehvaiour from within
        /// OnNetworkSpawn when scene management is disabled does not throw an exception
        /// </summary>
        [UnityTest]
        public IEnumerator ValidateRPCInvocationDuringNetworkSpawn()
        {
            // Just validating the values sent, but in reality if this breaks then an exception would be thrown prior to reaching this point.
            foreach (var networkManager in m_NetworkManagers)
            {
                var first = networkManager.LocalClient.PlayerObject.GetComponent<FirstNetworkBehaviour>();
                var second = networkManager.LocalClient.PlayerObject.GetComponent<SecondNetworkBehaviour>();

                Assert.IsTrue(first.ValueSent == second.ValueReceived, $"[{networkManager.LocalClient.PlayerObject.name}] Value sent {first.ValueSent} does not equal the value received {second.ValueReceived}!");
            }

            yield return null;
        }


        #region Test Components
        /// <summary>
        /// Should be added before the <see cref="SecondNetworkBehaviour"/>.
        /// This invokes the RPC on the <see cref="SecondNetworkBehaviour"/>
        /// during <see cref="NetworkBehaviour.OnNetworkSpawn"/>.
        /// </summary>
        public class FirstNetworkBehaviour : NetworkBehaviour
        {
            public SecondNetworkBehaviour SecondNetworkBehaviour;

            public int ValueSent { get; private set; }
            public override void OnNetworkSpawn()
            {
                // Just invoke on the local player to test for this issue
                if (IsLocalPlayer)
                {
                    ValueSent = Random.Range(0, 100);
                    SecondNetworkBehaviour.SomeRpc(ValueSent);
                }
                base.OnNetworkSpawn();
            }
        }

        public class SecondNetworkBehaviour : NetworkBehaviour
        {
            public int ValueReceived { get; private set; }

            [Rpc(SendTo.Owner)]
            public void SomeRpc(int valueReceived)
            {
                ValueReceived = valueReceived;
            }
        }
        #endregion
    }
}
