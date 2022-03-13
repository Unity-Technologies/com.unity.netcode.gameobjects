using System.Collections;
using NUnit.Framework;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkRigidbodyDynamicTest : NetworkRigidbodyTestBase
    {
        public override bool Kinematic => false;
    }

    public class NetworkRigidbodyKinematicTest : NetworkRigidbodyTestBase
    {
        public override bool Kinematic => true;
    }

    public abstract class NetworkRigidbodyTestBase : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        public abstract bool Kinematic { get; }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<NetworkTransform>();
            m_PlayerPrefab.AddComponent<Rigidbody>();
            m_PlayerPrefab.AddComponent<NetworkRigidbody>();
            m_PlayerPrefab.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
            m_PlayerPrefab.GetComponent<Rigidbody>().isKinematic = Kinematic;
        }

        /// <summary>
        /// Tests that a server can destroy a NetworkObject and that it gets despawned correctly.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestRigidbodyKinematicEnableDisable()
        {
            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult);
            var serverPlayer = serverClientPlayerResult.Result.gameObject;

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult);
            var clientPlayer = clientClientPlayerResult.Result.gameObject;

            Assert.IsNotNull(serverPlayer, "serverPlayer is not null");
            Assert.IsNotNull(clientPlayer, "clientPlayer is not null");

            yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 3);

            // server rigidbody has authority and should have a kinematic mode of false
            Assert.True(serverPlayer.GetComponent<Rigidbody>().isKinematic == Kinematic, "serverPlayer kinematic");
            Assert.AreEqual(RigidbodyInterpolation.Interpolate, serverPlayer.GetComponent<Rigidbody>().interpolation, "server equal interpolate");

            // client rigidbody has no authority and should have a kinematic mode of true
            Assert.True(clientPlayer.GetComponent<Rigidbody>().isKinematic, "clientPlayer kinematic");
            Assert.AreEqual(RigidbodyInterpolation.None, clientPlayer.GetComponent<Rigidbody>().interpolation, "client equal interpolate");

            // despawn the server player (but keep it around on the server)
            serverPlayer.GetComponent<NetworkObject>().Despawn(false);

            yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 3);

            Assert.IsTrue(serverPlayer.GetComponent<Rigidbody>().isKinematic == Kinematic, "serverPlayer second kinematic");

            yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 3);

            Assert.IsTrue(clientPlayer == null, "clientPlayer being null"); // safety check that object is actually despawned.
        }
    }
}
