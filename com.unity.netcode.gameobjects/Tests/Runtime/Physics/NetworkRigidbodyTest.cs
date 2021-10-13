using System.Collections;
using NUnit.Framework;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Physics
{
    public class NetworkRigidbodyDynamicTest : NetworkRigidbodyTestBase
    {
        public override bool Kinematic => false;
    }

    public class NetworkRigidbodyKinematicTest : NetworkRigidbodyTestBase
    {
        public override bool Kinematic => true;
    }

    public abstract class NetworkRigidbodyTestBase : BaseMultiInstanceTest
    {
        protected override int NbClients => 1;

        public abstract bool Kinematic { get; }

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(true, NbClients, playerPrefab =>
            {
                playerPrefab.AddComponent<NetworkTransform>();
                playerPrefab.AddComponent<Rigidbody>();
                playerPrefab.AddComponent<NetworkRigidbody>();
                playerPrefab.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
                playerPrefab.GetComponent<Rigidbody>().isKinematic = Kinematic;
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

            yield return WaitForFrames(5);

            // server rigidbody has authority and should have a kinematic mode of false
            Assert.True(serverPlayer.GetComponent<Rigidbody>().isKinematic == Kinematic);
            Assert.AreEqual(RigidbodyInterpolation.Interpolate, serverPlayer.GetComponent<Rigidbody>().interpolation);

            // client rigidbody has no authority and should have a kinematic mode of true
            Assert.True(clientPlayer.GetComponent<Rigidbody>().isKinematic);
            Assert.AreEqual(RigidbodyInterpolation.None, clientPlayer.GetComponent<Rigidbody>().interpolation);

            // despawn the server player (but keep it around on the server)
            serverPlayer.GetComponent<NetworkObject>().Despawn(false);

            yield return WaitForFrames(5);

            Assert.IsTrue(serverPlayer.GetComponent<Rigidbody>().isKinematic == Kinematic);

            yield return WaitForFrames(5);

            Assert.IsTrue(clientPlayer == null); // safety check that object is actually despawned.
        }

        public static IEnumerator WaitForFrames(int count)
        {
            int nextFrameNumber = Time.frameCount + count;
            yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
        }
    }
}
