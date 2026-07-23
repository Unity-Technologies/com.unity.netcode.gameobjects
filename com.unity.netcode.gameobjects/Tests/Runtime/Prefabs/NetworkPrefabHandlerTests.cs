using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// The NetworkPrefabHandler unit tests validates:
    /// Registering with GameObject, NetworkObject, or GlobalObjectIdHash
    /// Newly assigned rotation or position values for newly spawned NetworkObject instances are valid
    /// Destroying a newly spawned NetworkObject instance works
    /// Removing a INetworkPrefabInstanceHandler is removed and can be verified (very last check)
    /// </summary>
    internal class NetworkPrefabHandlerTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        protected override void OnOneTimeSetup()
        {
            // TODO: [CmbServiceTests] if this test is deemed needed to test against the CMB server then update this test.
            NetcodeIntegrationTestHelpers.IgnoreIfServiceEnviromentVariableSet();
            base.OnOneTimeSetup();
        }

        private const string k_TestPrefabObjectName = "NetworkPrefabTestObject";
        private uint m_ObjectId = 0;

        private bool m_CanStart;

        protected override bool CanStartServerAndClients()
        {
            return m_CanStart;
        }

        private GameObject MakeValidNetworkPrefab()
        {
            m_ObjectId++;
            return CreateNetworkObjectPrefab(k_TestPrefabObjectName + m_ObjectId.ToString());
        }

        /// <summary>
        /// Tests the NetwokConfig NetworkPrefabsList initialization during NetworkManager's Init method to make sure that
        /// it will still initialize but remove the invalid prefabs
        /// </summary>
        [UnityTest]
        public IEnumerator NetworkConfigInvalidNetworkPrefabTest()
        {
            var authority = GetAuthorityNetworkManager();

            // Add null entry
            authority.NetworkConfig.Prefabs.Add(null);

            // Add a NetworkPrefab with no prefab
            authority.NetworkConfig.Prefabs.Add(new NetworkPrefab());

            // Add a NetworkPrefab override with an invalid hash
            authority.NetworkConfig.Prefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.Hash, SourceHashToOverride = 0 });

            // Add a NetworkPrefab override with a valid hash but an invalid target prefab
            authority.NetworkConfig.Prefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.Hash, SourceHashToOverride = 654321, OverridingTargetPrefab = null });

            // Add a NetworkPrefab override with a valid hash to override but an invalid target prefab
            authority.NetworkConfig.Prefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.Prefab, SourceHashToOverride = 654321, OverridingTargetPrefab = null });

            // Add a NetworkPrefab override with an invalid source prefab to override
            authority.NetworkConfig.Prefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.Prefab, SourcePrefabToOverride = null });

            // Create a valid network prefab "asset".
            var validPrefabAsset = MakeValidNetworkPrefab().GetComponent<NetworkObject>();

            // Add a NetworkPrefab override with a valid source prefab to override but an invalid target prefab.
            authority.NetworkConfig.Prefabs.Add(new NetworkPrefab() { Override = NetworkPrefabOverride.Prefab, SourcePrefabToOverride = validPrefabAsset.gameObject, OverridingTargetPrefab = null });

            var validPrefabForSourceHash = MakeValidNetworkPrefab().GetComponent<NetworkObject>();
            // This would be the scenario that a hash would be used (typically when scene management is disabled)
            validPrefabForSourceHash.InScenePlaced = true;

            var networkPrefab = authority.NetworkConfig.Prefabs.InternalPrefabs[authority.NetworkConfig.Prefabs.InternalPrefabs.Count - 1];
            networkPrefab.SourceHashToOverride = validPrefabForSourceHash.GlobalObjectIdHash;
            networkPrefab.OverridingTargetPrefab = validPrefabAsset.gameObject;
            networkPrefab.Override = NetworkPrefabOverride.Hash;
            authority.NetworkConfig.Prefabs.InternalPrefabs[authority.NetworkConfig.Prefabs.InternalPrefabs.Count - 1] = networkPrefab;

            var sourcePrefab = MakeValidNetworkPrefab();
            networkPrefab = authority.NetworkConfig.Prefabs.InternalPrefabs[authority.NetworkConfig.Prefabs.InternalPrefabs.Count - 1];
            var index = authority.NetworkConfig.Prefabs.Prefabs.Count - 1;
            var targetPrefab = MakeValidNetworkPrefab();
            networkPrefab.Prefab = sourcePrefab;
            networkPrefab.SourcePrefabToOverride = sourcePrefab;
            networkPrefab.OverridingTargetPrefab = targetPrefab;
            authority.NetworkConfig.Prefabs.InternalPrefabs[index] = networkPrefab;

            m_CanStart = true;
            yield return StartServerAndClients();

            // In the end we should only have 3 valid registered network prefabs
            Assert.AreEqual(5, authority.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks.Count);
        }

        private const string k_PrefabObjectName = "NetworkPrefabHandlerTestObject";


        [UnityTest]
        public IEnumerator NetworkPrefabHandlerClass([Values] NetworkTopologyTypes topologyType)
        {
            var authority = GetAuthorityNetworkManager();
            authority.NetworkConfig.NetworkTopology = topologyType;
            var baseObject = MakeValidNetworkPrefab().GetComponent<NetworkObject>();

            m_CanStart = true;
            yield return StartServerAndClients();

            var testPrefabObjectName = k_TestPrefabObjectName;

            var networkPrefabHandler = authority.PrefabHandler;
            var prefabHandlerObject = new GameObject();
            var networkPrefabInstanceHandler = prefabHandlerObject.AddComponent<NetworkPrefabInstanceHandler>();
            networkPrefabInstanceHandler.Initialize(authority, baseObject);

            var prefabPosition = new Vector3(1.0f, 5.0f, 3.0f);
            var prefabRotation = new Quaternion(1.0f, 0.5f, 0.4f, 0.1f);

            //Register via GameObject
            var gameObjectRegistered = authority.PrefabHandler.ContainsHandler(baseObject);

            //Test result of registering via GameObject reference
            Assert.True(gameObjectRegistered);

            var spawnedObject = authority.PrefabHandler.HandleNetworkPrefabSpawn(baseObject.GlobalObjectIdHash, 0, prefabPosition, prefabRotation);

            //Test that something was instantiated
            Assert.NotNull(spawnedObject);

            //Test that this is indeed an instance of our original object
            Assert.True(spawnedObject.name.Contains(testPrefabObjectName));

            //Test for position and rotation
            Assert.True(prefabPosition == spawnedObject.transform.position);
            Assert.True(prefabRotation == spawnedObject.transform.rotation);

            authority.PrefabHandler.HandleNetworkPrefabDestroy(spawnedObject);     //Destroy our prefab instance
            authority.PrefabHandler.RemoveHandler(baseObject);                     //Remove our handler

            //Register via NetworkObject
            gameObjectRegistered = authority.PrefabHandler.AddHandler(baseObject, networkPrefabInstanceHandler);

            //Test result of registering via NetworkObject reference
            Assert.True(gameObjectRegistered);

            //Change it up
            prefabPosition = new Vector3(2.0f, 1.0f, 5.0f);
            prefabRotation = new Quaternion(4.0f, 1.5f, 5.4f, 5.1f);

            spawnedObject = authority.PrefabHandler.HandleNetworkPrefabSpawn(baseObject.GlobalObjectIdHash, 0, prefabPosition, prefabRotation);

            //Test that something was instantiated
            Assert.NotNull(spawnedObject);

            //Test that this is indeed an instance of our original object
            Assert.True(spawnedObject.name.Contains(testPrefabObjectName));

            //Test for position and rotation
            Assert.True(prefabPosition == spawnedObject.transform.position);
            Assert.True(prefabRotation == spawnedObject.transform.rotation);

            authority.PrefabHandler.HandleNetworkPrefabDestroy(spawnedObject);     //Destroy our prefab instance
            authority.PrefabHandler.RemoveHandler(baseObject);                     //Remove our handler

            //Register via GlobalObjectIdHash
            gameObjectRegistered = authority.PrefabHandler.AddHandler(baseObject.GlobalObjectIdHash, networkPrefabInstanceHandler);

            //Test result of registering via GlobalObjectIdHash reference
            Assert.True(gameObjectRegistered);

            //Change it up
            prefabPosition = new Vector3(6.0f, 4.0f, 1.0f);
            prefabRotation = new Quaternion(3f, 2f, 4f, 1f);

            spawnedObject = authority.PrefabHandler.HandleNetworkPrefabSpawn(baseObject.GlobalObjectIdHash, 0, prefabPosition, prefabRotation);

            //Test that something was instantiated
            Assert.NotNull(spawnedObject);

            //Test that this is indeed an instance of our original object
            Assert.True(spawnedObject.name.Contains(testPrefabObjectName));

            //Test for position and rotation
            Assert.True(prefabPosition == spawnedObject.transform.position);
            Assert.True(prefabRotation == spawnedObject.transform.rotation);

            authority.PrefabHandler.HandleNetworkPrefabDestroy(spawnedObject);     //Destroy our prefab instance
            authority.PrefabHandler.RemoveHandler(baseObject);                     //Remove our handler

            // Register a handler that throws an exception
            var networkPrefabExceptionThrower = new NetworkPrefabExceptionThrower();
            gameObjectRegistered = authority.PrefabHandler.AddHandler(baseObject, networkPrefabExceptionThrower);
            //Test result of registering exception handler
            Assert.True(gameObjectRegistered);

            LogAssert.Expect(LogType.Exception, "Exception: exception while instantiating");
            spawnedObject = authority.PrefabHandler.HandleNetworkPrefabSpawn(baseObject.GlobalObjectIdHash, 0, prefabPosition, prefabRotation);

            // No object should have been spawned, but test should have continued
            Assert.Null(spawnedObject);

            authority.PrefabHandler.RemoveHandler(baseObject);                     //Remove our handler

            Assert.False(networkPrefabInstanceHandler.StillHasInstances());

            UnityEngine.Object.Destroy(prefabHandlerObject);
        }

        protected override IEnumerator OnTearDown()
        {
            m_CanStart = false;
            return base.OnTearDown();
        }
    }

    /// <summary>
    /// The Prefab instance handler to use for this test
    /// </summary>
    internal class NetworkPrefabInstanceHandler : MonoBehaviour, INetworkPrefabInstanceHandler
    {
        private NetworkObject m_NetworkObject;

        private List<NetworkObject> m_Instances;

        private NetworkManager m_NetworkManager;

        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            var networkObjectInstance = Instantiate(m_NetworkObject.gameObject).GetComponent<NetworkObject>();
            networkObjectInstance.transform.SetPositionAndRotation(position, rotation);
            m_Instances.Add(networkObjectInstance);
            return networkObjectInstance;
        }

        public void Destroy(NetworkObject networkObject)
        {
            if (m_Instances == null || m_Instances.Count > 0)
            {
                var instancesContainsNetworkObject = m_Instances.Contains(networkObject);
                Assert.True(instancesContainsNetworkObject);
                m_Instances.Remove(networkObject);
                Destroy(networkObject.gameObject);
            }
        }

        public bool StillHasInstances()
        {
            return m_Instances.Count > 0;
        }

        private void OnDestroy()
        {
            m_NetworkManager?.PrefabHandler.RemoveHandler(m_NetworkObject);
            m_Instances.Clear();
            m_Instances = null;
        }

        public void Initialize(NetworkManager networkManager, NetworkObject networkObject)
        {
            m_NetworkManager = networkManager;
            m_NetworkObject = networkObject;
            m_Instances = new List<NetworkObject>();
            networkManager.PrefabHandler.AddHandler(networkObject, this);
        }
    }

    /// <summary>
    /// Causes an exception during client connection
    /// </summary>
    internal class NetworkPrefabExceptionThrower : MonoBehaviour, INetworkPrefabInstanceHandler
    {
        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            throw new Exception("exception while instantiating");
        }

        public void Destroy(NetworkObject networkObject)
        {
            Destroy(networkObject.gameObject);
        }
    }
}
