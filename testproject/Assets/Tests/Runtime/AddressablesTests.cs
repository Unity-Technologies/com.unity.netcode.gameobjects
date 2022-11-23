#if TESTPROJECT_USE_ADDRESSABLES
using System.Collections;
using DefaultNamespace;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace TestProject.RuntimeTests
{
    [TestFixture(HostOrServer.Server)]
    [TestFixture(HostOrServer.Host)]
    public class AddressablesTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private const string k_ValidObject = "AddressableTestObject.prefab";

        public AddressablesTests(HostOrServer hostOrServer)
        {
            m_UseHost = hostOrServer == HostOrServer.Host;
        }
        protected override NetworkManagerInstatiationMode OnSetIntegrationTestMode()
        {
            return NetworkManagerInstatiationMode.DoNotCreate;
        }


        protected override IEnumerator OnTearDown()
        {
            ShutdownAndCleanUp();
            yield return null;
        }

        private IEnumerator LoadAsset(AssetReferenceGameObject asset, NetcodeIntegrationTestHelpers.ResultWrapper<GameObject> prefab)
        {
            var handle = asset.LoadAssetAsync();
            while (!handle.IsDone)
            {
                var nextFrameNumber = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
            }
            prefab.Result = handle.Result;
        }

        protected IEnumerator StartWithAddressableAssetAdded()
        {
            yield return StartServerAndClients();
        }

        private void AddPrefab(GameObject prefab)
        {
            m_ServerNetworkManager.AddNetworkPrefab(prefab);
            foreach (var client in m_ClientNetworkManagers)
            {
                client.AddNetworkPrefab(prefab);
            }
        }

        private IEnumerator SpawnAndValidate(GameObject prefab, bool waitAndAddOnClient = false)
        {
            // Have to spawn it ourselves.
            var serverObj = Object.Instantiate(prefab);
            serverObj.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObj.GetComponent<NetworkObject>().Spawn();

#if UNITY_2023_1_OR_NEWER
            var objs = Object.FindObjectsByType<AddressableTestScript>(FindObjectsSortMode.InstanceID);
#else
            var objs = Object.FindObjectsOfType<AddressableTestScript>();
#endif

            // Prefabs loaded by addressables actually don't show up in this search.
            // Unlike other tests that make prefabs programmatically, those aren't added to the scene until they're instantiated
            Assert.AreEqual(1, objs.Length);

            var startTime = Time.realtimeSinceStartup;

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<CreateObjectMessage>(m_ClientNetworkManagers[0]);

            if (waitAndAddOnClient)
            {
                // Since it's not added, after the CreateObjectMessage is received, it's not spawned yet
                // Verify that to be the case as a precondition.
#if UNITY_2023_1_OR_NEWER
                objs = Object.FindObjectsByType<AddressableTestScript>(FindObjectsSortMode.InstanceID);
#else
                objs = Object.FindObjectsOfType<AddressableTestScript>();
#endif
                Assert.AreEqual(1, objs.Length);
                yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime >= m_ClientNetworkManagers[0].NetworkConfig.SpawnTimeout - 0.25);
                foreach (var client in m_ClientNetworkManagers)
                {
                    client.AddNetworkPrefab(prefab);
                }
            }

#if UNITY_2023_1_OR_NEWER
            objs = Object.FindObjectsByType<AddressableTestScript>(FindObjectsSortMode.InstanceID);
#else
            objs = Object.FindObjectsOfType<AddressableTestScript>();
#endif
            Assert.AreEqual(NumberOfClients + 1, objs.Length);
            foreach (var obj in objs)
            {
                Assert.AreEqual(1234567, obj.AnIntVal);
                Assert.AreEqual("1234567", obj.AStringVal);
                Assert.AreEqual("12345671234567", obj.GetValue());
            }
        }

        [UnityTest]
        public IEnumerator WhenLoadingAValidObjectBeforeStarting_SpawningItSucceedsOnServerAndClient()
        {
            var asset = new AssetReferenceGameObject(k_ValidObject);

            CreateServerAndClients();
            var prefabResult = new NetcodeIntegrationTestHelpers.ResultWrapper<GameObject>();
            yield return LoadAsset(asset, prefabResult);
            AddPrefab(prefabResult.Result);
            yield return StartServerAndClients();

            yield return SpawnAndValidate(prefabResult.Result);
        }

        [UnityTest]
        public IEnumerator WhenLoadingAValidObjectAfterStarting_SpawningItSucceedsOnServerAndClient()
        {
            var asset = new AssetReferenceGameObject(k_ValidObject);

            CreateServerAndClients();
            m_ServerNetworkManager.NetworkConfig.ForceSamePrefabs = false;
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.ForceSamePrefabs = false;
            }

            var prefabResult = new NetcodeIntegrationTestHelpers.ResultWrapper<GameObject>();
            yield return LoadAsset(asset, prefabResult);
            yield return StartServerAndClients();
            AddPrefab(prefabResult.Result);

            yield return SpawnAndValidate(prefabResult.Result);
        }

        [UnityTest]
        public IEnumerator WhenSpawningServerPrefabBeforeClientPrefabHasLoaded_SpawningItSucceedsOnServerAndClientAfterConfiguredDelay([Values(1, 2, 3)] int timeout)
        {
            var asset = new AssetReferenceGameObject(k_ValidObject);

            CreateServerAndClients();
            m_ServerNetworkManager.NetworkConfig.ForceSamePrefabs = false;
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.SpawnTimeout = timeout;
                client.NetworkConfig.ForceSamePrefabs = false;
            }

            var prefabResult = new NetcodeIntegrationTestHelpers.ResultWrapper<GameObject>();
            yield return LoadAsset(asset, prefabResult);
            yield return StartServerAndClients();
            m_ServerNetworkManager.AddNetworkPrefab(prefabResult.Result);

            yield return SpawnAndValidate(prefabResult.Result, true);
        }
    }
}
#endif
