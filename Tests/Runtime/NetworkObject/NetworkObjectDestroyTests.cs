using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Tests calling destroy on spawned / unspawned <see cref="NetworkObject"/>s. Expected behavior:
    /// - Server or client destroy on unspawned => Object gets destroyed, no exceptions
    /// - Server destroy spawned => Object gets destroyed and despawned/destroyed on all clients. Server does not run <see cref="NetworkPrefaInstanceHandler.HandleNetworkPrefabDestroy"/>. Client runs it.
    /// - Client destroy spawned => throw exception.
    /// </summary>
    public class NetworkObjectDestroyTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        /// <summary>
        /// Tests that a server can destroy a NetworkObject and that it gets despawned correctly.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestNetworkObjectServerDestroy()
        {
            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId, m_ServerNetworkManager, serverClientPlayerResult);

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId, m_ClientNetworkManagers[0], clientClientPlayerResult);

            Assert.IsNotNull(serverClientPlayerResult.Result.gameObject);
            Assert.IsNotNull(clientClientPlayerResult.Result.gameObject);

            // destroy the server player
            Object.Destroy(serverClientPlayerResult.Result.gameObject);

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeHandled<DestroyObjectMessage>(m_ClientNetworkManagers[0]);

            Assert.IsTrue(serverClientPlayerResult.Result == null); // Assert.IsNull doesn't work here
            Assert.IsTrue(clientClientPlayerResult.Result == null);

            // create an unspawned networkobject and destroy it
            var go = new GameObject();
            go.AddComponent<NetworkObject>();
            Object.Destroy(go);

            yield return null;
            Assert.IsTrue(go == null);
        }

        /// <summary>
        /// Tests that a client cannot destroy a spawned networkobject.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestNetworkObjectClientDestroy()
        {
            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId, m_ServerNetworkManager, serverClientPlayerResult);

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId, m_ClientNetworkManagers[0], clientClientPlayerResult);

            // destroy the client player, this is not allowed
            LogAssert.Expect(LogType.Exception, "NotServerException: Destroy a spawned NetworkObject on a non-host client is not valid. Call Destroy or Despawn on the server/host instead.");
            Object.DestroyImmediate(clientClientPlayerResult.Result.gameObject);
        }
    }
}
