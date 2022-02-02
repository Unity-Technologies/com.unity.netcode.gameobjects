#if INCLUDE_NETCODE_RUNTIME_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Tests calling destroy on spawned / unspawned <see cref="NetworkObject"/>s. Expected behavior:
    /// - Server or client destroy on unspawned => Object gets destroyed, no exceptions
    /// - Server destroy spawned => Object gets destroyed and despawned/destroyed on all clients. Server does not run <see cref="NetworkPrefaInstanceHandler.HandleNetworkPrefabDestroy"/>. Client runs it.
    /// - Client destroy spawned => throw exception.
    /// </summary>
    public class NetworkObjectDestroyTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 1;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            return base.Setup();
        }

        /// <summary>
        /// Tests that a server can destroy a NetworkObject and that it gets despawned correctly.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestNetworkObjectServerDestroy()
        {
            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult));

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult));

            Assert.IsNotNull(serverClientPlayerResult.Result.gameObject);
            Assert.IsNotNull(clientClientPlayerResult.Result.gameObject);

            // destroy the server player
            Object.Destroy(serverClientPlayerResult.Result.gameObject);

            // Wait for two snapshot messages, because there's likely one already in flight that doesn't have the spawn yet.
            yield return MultiInstanceHelpers.WaitForMessageOfType<SnapshotDataMessage>(m_ClientNetworkManagers[0]);
            yield return MultiInstanceHelpers.WaitForMessageOfType<SnapshotDataMessage>(m_ClientNetworkManagers[0]);

            Assert.IsTrue(serverClientPlayerResult.Result == null); // Assert.IsNull doesn't work here
            Assert.IsTrue(clientClientPlayerResult.Result == null);

            // create an unspawned networkobject and destroy it
            var go = new GameObject();
            go.AddComponent<NetworkObject>();
            Object.Destroy(go);

            yield return null;
            Assert.IsTrue(go == null);
        }

        public enum ClientDestroyWithOwner
        {
            DestroyWithOwner,
            DontDestroyWithOwner
        }

        /// <summary>
        /// Tests that a client cannot destroy a spawned networkobject.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestNetworkObjectClientDestroy([Values(ClientDestroyWithOwner.DestroyWithOwner, ClientDestroyWithOwner.DontDestroyWithOwner)] ClientDestroyWithOwner destroyWithOwner)
        {
            var clientNetworkManager = m_ClientNetworkManagers[0];
            clientNetworkManager.LocalClient.PlayerObject.DontDestroyWithOwner = destroyWithOwner != ClientDestroyWithOwner.DestroyWithOwner;

            if (!clientNetworkManager.LocalClient.PlayerObject.DontDestroyWithOwner)
            {
                // destroy the client player, this is not allowed
                LogAssert.Expect(LogType.Exception, "NotServerException: Destroy a spawned NetworkObject on a non-host client is not valid. Call Destroy or Despawn on the server/host instead.");
                Object.DestroyImmediate(clientNetworkManager.LocalClient.PlayerObject);
            }
            else
            {
                Assert.True(m_ServerNetworkManager.ConnectedClients.ContainsKey(clientNetworkManager.LocalClientId));
                var serverRelativeClientPlayerObject = m_ServerNetworkManager.ConnectedClients[clientNetworkManager.LocalClientId].PlayerObject;
                serverRelativeClientPlayerObject.DontDestroyWithOwner = true;
                clientNetworkManager.Shutdown();
                var waitForFrameCount = Time.frameCount + 2;
                yield return new WaitUntil(() => Time.frameCount >= waitForFrameCount);

                Assert.True(serverRelativeClientPlayerObject.gameObject.activeInHierarchy);
            }
        }
    }
}
#endif
