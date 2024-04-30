using System.Collections;
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
    public class NetworkBehaviourGenericTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        private bool m_AllowServerToStart;

        private GameObject m_PrePostSpawnObject;

        protected override bool CanStartServerAndClients()
        {
            return m_AllowServerToStart;
        }

        public class SimpleNetworkBehaviour : NetworkBehaviour
        {
            public bool OnNetworkDespawnCalled;

            public override void OnNetworkDespawn()
            {
                OnNetworkDespawnCalled = true;
                base.OnNetworkDespawn();
            }
        }

        public class NetworkBehaviourPreSpawn : NetworkBehaviour
        {
            public static int ValueToSet;
            public bool OnNetworkPreSpawnCalled;
            public bool NetworkVarValueMatches;

            public NetworkVariable<int> TestNetworkVariable;

            public override void OnNetworkPreSpawn(ref NetworkManager networkManager)
            {
                OnNetworkPreSpawnCalled = true;
                var val = networkManager.IsServer ? ValueToSet : 0;
                TestNetworkVariable = new NetworkVariable<int>(val, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
                base.OnNetworkPreSpawn(ref networkManager);
            }

            public override void OnNetworkSpawn()
            {
                // For the server this should
                NetworkVarValueMatches = TestNetworkVariable.Value == ValueToSet;
                base.OnNetworkSpawn();
            }
        }

        public class NetworkBehaviourPostSpawn : NetworkBehaviour
        {
            public bool OnNetworkPostSpawnCalled;

            private NetworkBehaviourPreSpawn m_NetworkBehaviourPreSpawn;

            public int ValueSet;

            public override void OnNetworkSpawn()
            {
                m_NetworkBehaviourPreSpawn = GetComponent<NetworkBehaviourPreSpawn>();
                base.OnNetworkSpawn();
            }

            public override void OnNetworkPostSpawn()
            {
                OnNetworkPostSpawnCalled = true;
                ValueSet = m_NetworkBehaviourPreSpawn.TestNetworkVariable.Value;
                base.OnNetworkPostSpawn();
            }

        }

        protected override IEnumerator OnSetup()
        {
            m_AllowServerToStart = false;
            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_PrePostSpawnObject = CreateNetworkObjectPrefab("PrePostSpawn");
            // Reverse the order of the components to get inverted spawn sequence
            m_PrePostSpawnObject.AddComponent<NetworkBehaviourPostSpawn>();
            m_PrePostSpawnObject.AddComponent<NetworkBehaviourPreSpawn>();
            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// This validates the fix for when a child GameObject with a NetworkBehaviour
        /// is deleted while the parent GameObject with a NetworkObject is spawned and
        /// is not deleted until a later time would cause an exception due to the
        /// NetworkBehaviour not being removed from the NetworkObject.ChildNetworkBehaviours
        /// list.
        /// </summary>
        [UnityTest]
        public IEnumerator ValidatedDisableddNetworkBehaviourWarning()
        {
            m_AllowServerToStart = true;

            yield return s_DefaultWaitForTick;

            // Now just start the Host
            yield return StartServerAndClients();

            var parentObject = new GameObject();
            var childObject = new GameObject
            {
                name = "ChildObject"
            };
            childObject.transform.parent = parentObject.transform;
            var parentNetworkObject = parentObject.AddComponent<NetworkObject>();
            var childBehaviour = childObject.AddComponent<NetworkTransform>();

            // Set the child object to be inactive in the hierarchy
            childObject.SetActive(false);

            LogAssert.Expect(LogType.Warning, $"{childObject.name} is disabled! Netcode for GameObjects does not support spawning disabled NetworkBehaviours! The {childBehaviour.GetType().Name} component was skipped during spawn!");

            parentNetworkObject.Spawn();
            yield return s_DefaultWaitForTick;
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

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            networkManager.NetworkConfig.Prefabs = m_ServerNetworkManager.NetworkConfig.Prefabs;
            base.OnNewClientCreated(networkManager);
        }

        /// <summary>
        /// This validates that pre spawn can be used to instantiate and assign a NetworkVariable (or other prespawn tasks)
        /// which can be useful for assigning a NetworkVariable value on the server side when the NetworkVariable has owner write permissions.
        /// This also assures that duruing post spawn all associated NetworkBehaviours have run through the OnNetworkSpawn pass (i.e. OnNetworkSpawn order is not an issue)
        /// </summary>
        [UnityTest]
        public IEnumerator OnNetworkPreAndPostSpawn()
        {
            m_AllowServerToStart = true;
            NetworkBehaviourPreSpawn.ValueToSet = Random.Range(1, 200);
            yield return StartServerAndClients();

            yield return CreateAndStartNewClient();

            // Spawn the object with the newly joined client as the owner
            var serverInstance = SpawnObject(m_PrePostSpawnObject, m_ClientNetworkManagers[0]);
            var serverNetworkObject = serverInstance.GetComponent<NetworkObject>();
            var serverPreSpawn = serverInstance.GetComponent<NetworkBehaviourPreSpawn>();
            var serverPostSpawn = serverInstance.GetComponent<NetworkBehaviourPostSpawn>();

            yield return WaitForConditionOrTimeOut(() => s_GlobalNetworkObjects.ContainsKey(m_ClientNetworkManagers[0].LocalClientId)
            && s_GlobalNetworkObjects[m_ClientNetworkManagers[0].LocalClientId].ContainsKey(serverNetworkObject.NetworkObjectId));
            AssertOnTimeout($"Client-{m_ClientNetworkManagers[0].LocalClientId} failed to spawn {nameof(NetworkObject)} id-{serverNetworkObject.NetworkObjectId}!");

            var clientNetworkObject = s_GlobalNetworkObjects[m_ClientNetworkManagers[0].LocalClientId][serverNetworkObject.NetworkObjectId];
            var clientPreSpawn = clientNetworkObject.GetComponent<NetworkBehaviourPreSpawn>();
            var clientPostSpawn = clientNetworkObject.GetComponent<NetworkBehaviourPostSpawn>();

            Assert.IsTrue(serverPreSpawn.OnNetworkPreSpawnCalled, $"[Server-side] OnNetworkPreSpawn not invoked!");
            Assert.IsTrue(clientPreSpawn.OnNetworkPreSpawnCalled, $"[Client-side] OnNetworkPreSpawn not invoked!");
            Assert.IsTrue(serverPostSpawn.OnNetworkPostSpawnCalled, $"[Server-side] OnNetworkPostSpawn not invoked!");
            Assert.IsTrue(clientPostSpawn.OnNetworkPostSpawnCalled, $"[Client-side] OnNetworkPostSpawn not invoked!");

            Assert.IsTrue(serverPreSpawn.NetworkVarValueMatches, $"[Server-side][PreSpawn] Value {NetworkBehaviourPreSpawn.ValueToSet} does not match {serverPreSpawn.TestNetworkVariable.Value}!");
            Assert.IsTrue(clientPreSpawn.NetworkVarValueMatches, $"[Client-side][PreSpawn] Value {NetworkBehaviourPreSpawn.ValueToSet} does not match {clientPreSpawn.TestNetworkVariable.Value}!");

            Assert.IsTrue(serverPostSpawn.ValueSet == NetworkBehaviourPreSpawn.ValueToSet, $"[Server-side][PostSpawn] Value {NetworkBehaviourPreSpawn.ValueToSet} does not match {serverPostSpawn.ValueSet}!");
            Assert.IsTrue(clientPostSpawn.ValueSet == NetworkBehaviourPreSpawn.ValueToSet, $"[Client-side][PostSpawn] Value {NetworkBehaviourPreSpawn.ValueToSet} does not match {clientPostSpawn.ValueSet}!");
        }

    }
}
