using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
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


    [TestFixture(NetworkTopologyTypes.DistributedAuthority)]
    [TestFixture(NetworkTopologyTypes.ClientServer)]
    internal class NetworkObjectDestroyTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        public class DestroyTestComponent : NetworkBehaviour
        {
            public static List<string> ObjectsDestroyed = new List<string>();

            public override void OnDestroy()
            {
                ObjectsDestroyed.Add(gameObject.name);
                base.OnDestroy();
            }
        }

        public NetworkObjectDestroyTests(NetworkTopologyTypes networkTopologyType) : base(networkTopologyType) { }

        protected override IEnumerator OnSetup()
        {
            // Re-apply the default for each test
            LogAssert.ignoreFailingMessages = false;
            DestroyTestComponent.ObjectsDestroyed.Clear();
            return base.OnSetup();
        }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<DestroyTestComponent>();
            var playerNetworkObject = m_PlayerPrefab.GetComponent<NetworkObject>();
            playerNetworkObject.SceneMigrationSynchronization = true;
            base.OnCreatePlayerPrefab();
        }

        private NetworkManager GetAuthorityOfNetworkObject(ulong networkObjectId)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
                {
                    continue;
                }

                if (networkManager.SpawnManager.SpawnedObjects[networkObjectId].HasAuthority)
                {
                    return networkManager;
                }
            }
            return null;
        }

        private bool NetworkObjectDoesNotExist(ulong networkObjectId)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Tests that the authority NetworkManager instance of a NetworkObject is allowed to destroy it.
        /// </summary>
        /// <returns>IEnumerator</returns>
        [UnityTest]
        public IEnumerator TestNetworkObjectAuthorityDestroy()
        {

            var ownerNetworkManager = m_ClientNetworkManagers[1];
            var clientId = ownerNetworkManager.LocalClientId;
            var localClientPlayer = ownerNetworkManager.LocalClient.PlayerObject;
            var localNetworkObjectId = localClientPlayer.NetworkObjectId;

            var authorityNetworkManager = GetAuthorityOfNetworkObject(localClientPlayer.NetworkObjectId);
            Assert.True(authorityNetworkManager != null, $"Could not find the authority of {localClientPlayer}!");

            var authorityPlayerClone = authorityNetworkManager.ConnectedClients[clientId].PlayerObject;

            // Have the authority NetworkManager destroy the player instance
            Object.Destroy(authorityPlayerClone.gameObject);

            var messageListener = m_DistributedAuthority ? m_ClientNetworkManagers[0] : m_ClientNetworkManagers[1];

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeHandled<DestroyObjectMessage>(messageListener);

            yield return WaitForConditionOrTimeOut(() => NetworkObjectDoesNotExist(localNetworkObjectId));
            AssertOnTimeout($"Not all network managers despawned and destroyed player instance NetworkObjectId: {localNetworkObjectId}");

            // validate that any unspawned networkobject can be destroyed
            var go = new GameObject();
            go.AddComponent<NetworkObject>();
            Object.Destroy(go);

            yield return null;
            Assert.IsTrue(go == null);
        }


        public enum ClientDestroyObject
        {
            ShuttingDown,
            ActiveSession
        }

        private string m_ClientPlayerName;
        private ulong m_ClientNetworkObjectId;
        /// <summary>
        /// Validates the expected behavior when the client-side destroys a <see cref="NetworkObject"/>
        /// </summary>
        [UnityTest]
        public IEnumerator TestNetworkObjectClientDestroy([Values] ClientDestroyObject clientDestroyObject)
        {
            var isShuttingDown = clientDestroyObject == ClientDestroyObject.ShuttingDown;

            var localNetworkManager = m_ClientNetworkManagers[1];
            var clientId = localNetworkManager.LocalClientId;
            var localClientPlayer = localNetworkManager.LocalClient.PlayerObject;

            var nonAuthorityClient = m_ClientNetworkManagers[0];
            var clientPlayerClone = nonAuthorityClient.ConnectedClients[clientId].PlayerObject;

            if (isShuttingDown)
            {
                // The non-authority client is allowed to destroy any spawned object it does not
                // have authority over when it shuts down.
                nonAuthorityClient.Shutdown();
            }
            else
            {
                // The non-authority client is =NOT= allowed to destroy any spawned object it does not
                // have authority over during runtime.
                LogAssert.ignoreFailingMessages = true;
                NetworkLog.NetworkManagerOverride = nonAuthorityClient;
                Object.Destroy(clientPlayerClone.gameObject);
            }

            m_ClientPlayerName = clientPlayerClone.gameObject.name;
            m_ClientNetworkObjectId = clientPlayerClone.NetworkObjectId;

            // destroying a NetworkObject while a session is active is not allowed
            if (!isShuttingDown)
            {
                yield return WaitForConditionOrTimeOut(HaveLogsBeenReceived);
                AssertOnTimeout($"Not all expected logs were received when destroying a {nameof(NetworkObject)} on the client side during an active session!");
            }
            else
            {
                bool NonAuthorityClientDestroyed()
                {
                    return DestroyTestComponent.ObjectsDestroyed.Contains(m_ClientPlayerName);
                }

                yield return WaitForConditionOrTimeOut(NonAuthorityClientDestroyed);
                AssertOnTimeout($"Timed out waiting for player object {m_ClientNetworkObjectId} to no longer exist within {nameof(NetworkSpawnManager.NetworkObjectsToSynchronizeSceneChanges)}!");
            }
        }

        private bool HaveLogsBeenReceived()
        {
            if (m_DistributedAuthority)
            {
                if (!NetcodeLogAssert.HasLogBeenReceived(LogType.Error, $"[Netcode] [Invalid Destroy][{m_ClientPlayerName}][NetworkObjectId:{m_ClientNetworkObjectId}] Destroy a spawned {nameof(NetworkObject)} on a non-owner client is not valid during a distributed authority session. Call Destroy or Despawn on the client-owner instead."))
                {
                    return false;
                }
            }
            else
            {
                if (!NetcodeLogAssert.HasLogBeenReceived(LogType.Error, $"[Netcode] [Invalid Destroy][{m_ClientPlayerName}][NetworkObjectId:{m_ClientNetworkObjectId}] Destroy a spawned {nameof(NetworkObject)} on a non-host client is not valid. Call Destroy or Despawn on the server/host instead."))
                {
                    return false;
                }

                if (!NetcodeLogAssert.HasLogBeenReceived(LogType.Error, $"[Netcode-Server Sender={m_ClientNetworkManagers[0].LocalClientId}] [Invalid Destroy][{m_ClientPlayerName}][NetworkObjectId:{m_ClientNetworkObjectId}] Destroy a spawned {nameof(NetworkObject)} on a non-host client is not valid. Call Destroy or Despawn on the server/host instead."))
                {
                    return false;
                }
            }
            return true;
        }

        protected override IEnumerator OnTearDown()
        {
            NetworkLog.NetworkManagerOverride = null;
            return base.OnTearDown();
        }

        protected override void OnOneTimeTearDown()
        {
            // Re-apply the default as the last exiting action
            LogAssert.ignoreFailingMessages = false;
            base.OnOneTimeTearDown();
        }
    }
}
