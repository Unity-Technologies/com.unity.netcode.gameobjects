using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// This class is for testing general fixes or functionality of NetworkBehaviours
    /// </summary>
    internal class NetworkBehaviourGenericTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        private bool m_AllowServerToStart;

        private GameObject m_PrefabToSpawn;

        protected override bool CanStartServerAndClients()
        {
            return m_AllowServerToStart;
        }

        internal class SimpleNetworkBehaviour : NetworkBehaviour
        {
            public bool OnNetworkDespawnCalled;

            public override void OnNetworkDespawn()
            {
                OnNetworkDespawnCalled = true;
                base.OnNetworkDespawn();
            }
        }

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("TestPrefab");

            var childObject = new GameObject
            {
                name = "ChildObject"
            };
            childObject.transform.parent = m_PrefabToSpawn.transform;
            childObject.AddComponent<NetworkTransform>();
            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnSetup()
        {
            m_AllowServerToStart = false;
            return base.OnSetup();
        }


        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            networkManager.NetworkConfig.Prefabs.Add(new NetworkPrefab()
            {
                Prefab = m_PrefabToSpawn,
            });
            base.OnNewClientCreated(networkManager);
        }


        /// <summary>
        /// This validates:
        /// - The fix for when a child GameObject with a NetworkBehaviour
        /// is deleted while the parent GameObject with a NetworkObject is spawned and
        /// is not deleted until a later time would cause an exception due to the
        /// NetworkBehaviour not being removed from the NetworkObject.ChildNetworkBehaviours
        /// list.
        /// - When a NetworkBehaviour is disabled but the associated GameObject is enabled,
        /// the object spawns without any issues.
        /// </summary>
        [UnityTest]
        public IEnumerator ValidatedDisableddNetworkBehaviourWarning([Values] bool disableGameObject)
        {
            m_AllowServerToStart = true;

            // Now just start the Host
            yield return StartServerAndClients();

            // Now join a new client to make sure a connected client spawns the instance.
            yield return CreateAndStartNewClient();

            // Adjust the prefab to either have the child GameObject completely disabled or the NetworkBehaviour
            // disabled.
            var childBehaviour = m_PrefabToSpawn.GetComponentInChildren<NetworkTransform>(true);
            if (disableGameObject)
            {
                childBehaviour.enabled = true;
                childBehaviour.gameObject.SetActive(false);
            }
            else
            {
                childBehaviour.enabled = false;
                childBehaviour.gameObject.SetActive(true);
            }
            // Now create an instance of the prefab
            var instance = Object.Instantiate(m_PrefabToSpawn);
            var instanceNetworkObject = instance.GetComponent<NetworkObject>();
            // When the GameObject is disabled, check for the warning.
            if (disableGameObject)
            {
                // Generate the expected warning message
                var expectedWarning = instanceNetworkObject.GenerateDisabledNetworkBehaviourWarning(instanceNetworkObject.GetComponentInChildren<NetworkTransform>(true));
                var expectedSplit = expectedWarning.Split(']');
                var expectedWarningBody = expectedSplit.Last();
                LogAssert.Expect(LogType.Warning, new Regex($".*{expectedWarningBody}*."));
            }

            // Spawn the instance
            SpawnObjectInstance(instanceNetworkObject, m_ServerNetworkManager);
            // Asure the connected client spawned the object first
            yield return WaitForSpawnedOnAllOrTimeOut(instanceNetworkObject);
            AssertOnTimeout($"Not all clients spawned {instanceNetworkObject.name}!");

            // Now join a new client to make sure the client synchronizes with the disabled GameObject or NetworkBehaviour component.
            yield return CreateAndStartNewClient();

            // Asure the newly connected client synchronizes the spawned object correctly
            yield return WaitForSpawnedOnAllOrTimeOut(instanceNetworkObject);
            AssertOnTimeout($"Not all clients spawned {instanceNetworkObject.name}!");
        }

        /// <summary>
        /// This test validates a fix to NetworkBehaviour.NetworkObject when
        /// the NetworkManager.LogLevel is set to Developer
        /// Note: This test does not require any clients, but should not impact this
        /// particular test if new tests are added to this class that do require clients
        /// </summary>
        [UnityTest]
        public IEnumerator ValidateNoSpam()
        {
            m_AllowServerToStart = true;
            var objectToTest = new GameObject();
            var simpleNetworkBehaviour = objectToTest.AddComponent<SimpleNetworkBehaviour>();

            // Now just start the Host
            yield return StartServerAndClients();

            // set the log level to developer
            m_ServerNetworkManager.LogLevel = LogLevel.Developer;

            // The only valid condition for this would be if the NetworkBehaviour is spawned.
            simpleNetworkBehaviour.IsSpawned = true;

            // Verify the warning gets logged under normal conditions
            var isNull = simpleNetworkBehaviour.NetworkObject == null;
            LogAssert.Expect(LogType.Warning, $"[Netcode] Could not get {nameof(NetworkObject)} for the {nameof(NetworkBehaviour)}. Are you missing a {nameof(NetworkObject)} component?");

            var networkObjectToTest = objectToTest.AddComponent<NetworkObject>();
            networkObjectToTest.NetworkManagerOwner = m_ServerNetworkManager;
            networkObjectToTest.Spawn();

            // Assure no log messages are logged when they should not be logged
            isNull = simpleNetworkBehaviour.NetworkObject != null;
            LogAssert.NoUnexpectedReceived();

            networkObjectToTest.Despawn();
            Object.Destroy(networkObjectToTest);
        }

        /// <summary>
        /// This validates the fix for when a child GameObject with a NetworkBehaviour
        /// is deleted while the parent GameObject with a NetworkObject is spawned and
        /// is not deleted until a later time would cause an exception due to the
        /// NetworkBehaviour not being removed from the NetworkObject.ChildNetworkBehaviours
        /// list.
        /// </summary>
        [UnityTest]
        public IEnumerator ValidateDeleteChildNetworkBehaviour()
        {
            m_AllowServerToStart = true;

            yield return s_DefaultWaitForTick;

            // Now just start the Host
            yield return StartServerAndClients();

            var parentObject = new GameObject();
            var childObject = new GameObject();
            childObject.transform.parent = parentObject.transform;
            var parentNetworkObject = parentObject.AddComponent<NetworkObject>();
            childObject.AddComponent<SimpleNetworkBehaviour>();

            parentNetworkObject.Spawn();
            yield return s_DefaultWaitForTick;

            // Destroy the child object with child NetworkBehaviour
            Object.Destroy(childObject);

            yield return s_DefaultWaitForTick;

            // Assure no log messages are logged when they should not be logged
            LogAssert.NoUnexpectedReceived();

            // Destroy the parent object which should not cause any exceptions
            // (validating the fix)
            Object.Destroy(parentObject);
        }

        protected override void OnPlayerPrefabGameObjectCreated()
        {
            // Adds the SimpleNetworkBehaviour before the NetworkObject
            // for OnNetworkDespawnInvokedWhenClientDisconnects testing
            m_PlayerPrefab.AddComponent<SimpleNetworkBehaviour>();
        }

        /// <summary>
        /// This validates that upon a client disconnecting, the server-side
        /// client's player clone will invoke NetworkBehaviour.OnNetworkDespawn
        /// when the component precedes the NetworkObject component.(PR-2323)
        /// </summary>
        [UnityTest]
        public IEnumerator OnNetworkDespawnInvokedWhenClientDisconnects()
        {
            m_AllowServerToStart = true;

            // Now just start the Host
            yield return StartServerAndClients();

            // Now create and connect a new client
            yield return CreateAndStartNewClient();

            var serverSidePlayer = m_PlayerNetworkObjects[NetworkManager.ServerClientId][m_ClientNetworkManagers[0].LocalClientId].GetComponent<SimpleNetworkBehaviour>();

            yield return StopOneClient(m_ClientNetworkManagers[0]);

            Assert.True(serverSidePlayer.OnNetworkDespawnCalled, $"Server-side player clone did not invoke OnNetworkDespawn!");
        }
    }
}
