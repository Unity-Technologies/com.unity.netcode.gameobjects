#if TESTPROJECT_USE_ADDRESSABLES
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace TestProject.RuntimeTests
{
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class AddressablesTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        protected override bool m_EnableTimeTravel => true;
        protected override bool m_SetupIsACoroutine => false;
        protected override bool m_TearDownIsACoroutine => false;

        private const string k_ValidObject = "AddressableTestObject.prefab";
        private const string k_ValidScene = "Assets/Scenes/AddressableInSceneObject.unity";

        public AddressablesTests(HostOrServer hostOrServer)
        {
            m_UseHost = hostOrServer == HostOrServer.Host;
        }
        protected override NetworkManagerInstatiationMode OnSetIntegrationTestMode()
        {
            return NetworkManagerInstatiationMode.DoNotCreate;
        }

        protected override void OnInlineTearDown()
        {
            ShutdownAndCleanUp();
        }

        // TODO: [CmbServiceTests] Adapt to run with the service
        protected override bool UseCMBService()
        {
            return false;
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

        private IEnumerator LoadSceneWithInSceneObject(AssetReference asset, NetcodeIntegrationTestHelpers.ResultWrapper<GameObject> prefab)
        {
            var handle = Addressables.LoadSceneAsync(asset, LoadSceneMode.Additive);
            while (!handle.IsDone)
            {
                var nextFrameNumber = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
            }

            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status, "Addressables.LoadSceneAsync failed!");

            foreach (var networkObject in FindObjects.FromSceneByType<NetworkObject>(handle.Result.Scene, false))
            {
                prefab.Result = networkObject.gameObject;
                break;
            }
        }

        protected void StartWithAddressableAssetAdded()
        {
            StartServerAndClientsWithTimeTravel();
        }

        private void AddPrefab(GameObject prefab)
        {
            m_ServerNetworkManager.AddNetworkPrefab(prefab);
            foreach (var client in m_ClientNetworkManagers)
            {
                client.AddNetworkPrefab(prefab);
            }
        }

        private void SpawnAndValidate(GameObject prefab, bool waitAndAddOnClient = false, bool wasLoadedFromScene = false)
        {
            // Have to spawn it ourselves.
            var serverObj = Object.Instantiate(prefab);
            serverObj.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObj.GetComponent<NetworkObject>().Spawn();
            var objs = FindObjects.ByType<AddressableTestScript>();

            // Prefabs loaded by addressables actually don't show up in this search.
            // Unlike other tests that make prefabs programmatically, those aren't added to the scene until they're instantiated
            var numExpected = 1;
            if (wasLoadedFromScene)
            {
                // If prefab was loaded from the scene, there'll be an additional object found
                numExpected++;
            }

            Assert.AreEqual(numExpected, objs.Length);

            var startTime = MockTimeProvider.StaticRealTimeSinceStartup;

            WaitForMessageReceivedWithTimeTravel<CreateObjectMessage>(new List<NetworkManager> { m_ClientNetworkManagers[0] }, ReceiptType.Received);

            if (waitAndAddOnClient)
            {
                // Since it's not added, after the CreateObjectMessage is received, it's not spawned yet
                // Verify that to be the case as a precondition.
                objs = FindObjects.ByType<AddressableTestScript>();
                Assert.AreEqual(numExpected, objs.Length);
                WaitForConditionOrTimeOutWithTimeTravel(() => MockTimeProvider.StaticRealTimeSinceStartup - startTime >= m_ClientNetworkManagers[0].NetworkConfig.SpawnTimeout - 0.25);
                foreach (var client in m_ClientNetworkManagers)
                {
                    client.AddNetworkPrefab(prefab);
                }
            }

            objs = FindObjects.ByType<AddressableTestScript>();
            Assert.AreEqual(NumberOfClients + numExpected, objs.Length);
            foreach (var obj in objs)
            {
                Assert.AreEqual(1234567, obj.AnIntVal);
                Assert.AreEqual("1234567", obj.AStringVal);
                Assert.AreEqual("12345671234567", obj.GetValue());

                // TODO-[MTT-15388]: Object spawned from a scene should be InScenePlaced after this ticket
                if (obj.IsSpawned)
                {
                    Assert.IsFalse(obj.NetworkObject.InScenePlaced, "Object was dynamically spawned and should be marked as such!");
                }
                else
                {
                    Assert.IsTrue(obj.NetworkObject.InScenePlaced, "Object that was loaded from scene should have been marked as in-scene placed during loading!");
                }
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
            StartServerAndClientsWithTimeTravel();

            SpawnAndValidate(prefabResult.Result);
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
            StartServerAndClientsWithTimeTravel();
            AddPrefab(prefabResult.Result);

            SpawnAndValidate(prefabResult.Result);
        }

        [UnityTest]
        public IEnumerator WhenSpawningServerPrefabBeforeClientPrefabHasLoaded_SpawningItSucceedsOnServerAndClientAfterDelay()
        {
            var asset = new AssetReferenceGameObject(k_ValidObject);

            CreateServerAndClients();
            m_ServerNetworkManager.NetworkConfig.ForceSamePrefabs = false;
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.SpawnTimeout = 3;
                client.NetworkConfig.ForceSamePrefabs = false;
            }

            var prefabResult = new NetcodeIntegrationTestHelpers.ResultWrapper<GameObject>();
            yield return LoadAsset(asset, prefabResult);
            StartServerAndClientsWithTimeTravel();
            m_ServerNetworkManager.AddNetworkPrefab(prefabResult.Result);

            SpawnAndValidate(prefabResult.Result, true);
        }

        // TODO-[MTT-15388]: Reconsider whether this test should be valid
        // Reported on Github issue https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/4049
        [UnityTest]
        public IEnumerator RegisteringPrefabFromLoadedAddressablesSceneWorks()
        {
            var asset = new AssetReference(k_ValidScene);

            CreateServerAndClients();
            foreach (var manager in m_NetworkManagers)
            {
                manager.NetworkConfig.ForceSamePrefabs = false;
            }

            StartServerAndClientsWithTimeTravel();

            var prefabResult = new NetcodeIntegrationTestHelpers.ResultWrapper<GameObject>();
            yield return LoadSceneWithInSceneObject(asset, prefabResult);

            foreach (var manager in m_NetworkManagers)
            {
                manager.AddNetworkPrefab(prefabResult.Result);
            }

            SpawnAndValidate(prefabResult.Result, wasLoadedFromScene: true);
        }
    }
}
#endif
