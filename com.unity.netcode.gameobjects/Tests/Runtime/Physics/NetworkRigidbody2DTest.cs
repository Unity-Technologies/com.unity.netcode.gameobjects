using System.Collections;
using NUnit.Framework;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Physics
{
    public class NetworkRigidbody2DDynamicTest : NetworkRigidbody2DTestBase
    {
        public override bool Kinematic => false;
    }

    public class NetworkRigidbody2DKinematicTest : NetworkRigidbody2DTestBase
    {
        public override bool Kinematic => true;
    }

    public abstract class NetworkRigidbody2DTestBase : BaseMultiInstanceTest
    {
        protected override int NbClients => 1;

        public abstract bool Kinematic { get; }

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(true, NbClients, playerPrefab =>
            {
                playerPrefab.AddComponent<NetworkTransform>();
                playerPrefab.AddComponent<Rigidbody2D>();
                playerPrefab.AddComponent<NetworkRigidbody2D>();
                playerPrefab.GetComponent<Rigidbody2D>().interpolation = RigidbodyInterpolation2D.Interpolate;
                playerPrefab.GetComponent<Rigidbody2D>().isKinematic = Kinematic;
            });
        }

        /// <summary>
        /// Tests that a server can destroy a NetworkObject and that it gets despawned correctly.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestRigidbodyKinematicEnableDisable()
        {
            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult));
            var serverPlayer = serverClientPlayerResult.Result.gameObject;

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult));
            var clientPlayer = clientClientPlayerResult.Result.gameObject;

            Assert.IsNotNull(serverPlayer);
            Assert.IsNotNull(clientPlayer);

            yield return NetworkRigidbodyTestBase.WaitForFrames(5);

            // server rigidbody has authority and should have a kinematic mode of false
            Assert.True(serverPlayer.GetComponent<Rigidbody2D>().isKinematic == Kinematic);
            Assert.AreEqual(RigidbodyInterpolation2D.Interpolate, serverPlayer.GetComponent<Rigidbody2D>().interpolation);

            // client rigidbody has no authority and should have a kinematic mode of true
            Assert.True(clientPlayer.GetComponent<Rigidbody2D>().isKinematic);
            Assert.AreEqual(RigidbodyInterpolation2D.None, clientPlayer.GetComponent<Rigidbody2D>().interpolation);

            // despawn the server player, (but keep it around on the server)
            serverPlayer.GetComponent<NetworkObject>().Despawn(false);

            yield return NetworkRigidbodyTestBase.WaitForFrames(5);

            Assert.IsTrue(serverPlayer.GetComponent<Rigidbody2D>().isKinematic == Kinematic);

            yield return NetworkRigidbodyTestBase.WaitForFrames(5);

            Assert.IsTrue(clientPlayer == null); // safety check that object is actually despawned.
        }

    }
}
