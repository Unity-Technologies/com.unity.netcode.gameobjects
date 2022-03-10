using System.Collections;
using System.Text.RegularExpressions;
using DefaultNamespace;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.RuntimeTests;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace TestProject.RuntimeTests
{
    public enum LoadMode
    {
        None,
        StartAsync,
        WaitForFinish
    }

    public enum LookupType
    {
        NetworkManagerByAddress,
        NetworkManagerByAsset,
        AssetByOperation
    }

    public enum AddMode
    {
        String,
        AssetReference
    }

    [TestFixture(LookupType.NetworkManagerByAddress, LoadMode.None, AddMode.AssetReference)]
    [TestFixture(LookupType.NetworkManagerByAddress, LoadMode.StartAsync, AddMode.AssetReference)]
    [TestFixture(LookupType.NetworkManagerByAddress, LoadMode.WaitForFinish, AddMode.AssetReference)]
    [TestFixture(LookupType.NetworkManagerByAsset, LoadMode.None, AddMode.AssetReference)]
    [TestFixture(LookupType.NetworkManagerByAsset, LoadMode.StartAsync, AddMode.AssetReference)]
    [TestFixture(LookupType.NetworkManagerByAsset, LoadMode.WaitForFinish, AddMode.AssetReference)]
    [TestFixture(LookupType.AssetByOperation, LoadMode.None, AddMode.AssetReference)]
    [TestFixture(LookupType.AssetByOperation, LoadMode.StartAsync, AddMode.AssetReference)]
    [TestFixture(LookupType.AssetByOperation, LoadMode.WaitForFinish, AddMode.AssetReference)]
    [TestFixture(LookupType.NetworkManagerByAddress, LoadMode.None, AddMode.String)]
    public class AddressablesTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 1;

        private const string k_ValidObject = "AddressableTestObject.prefab";
        private const string k_InvalidObject = "InvalidAddressableTestObject.prefab";
        private const string k_NonexistentObject = "IfIExistSomethingIsWrong.prefab";
        private LookupType m_LookupType;
        private LoadMode m_LoadMode;
        private AddMode m_AddMode;
        private NetworkManager.StartupFailureReason m_ServerFailureReason = NetworkManager.StartupFailureReason.None;
        private NetworkManager.StartupFailureReason[] m_ClientFailureReasons = new[] { NetworkManager.StartupFailureReason.None };

        public AddressablesTests(LookupType lookupType, LoadMode loadMode, AddMode addMode)
        {
            m_LookupType = lookupType;
            m_LoadMode = loadMode;
            m_AddMode = addMode;
        }


        [UnitySetUp]
        public override IEnumerator Setup()
        {
            m_ServerFailureReason = NetworkManager.StartupFailureReason.None;
            m_ClientFailureReasons = new[] { NetworkManager.StartupFailureReason.None };

            m_BypassStartAndWaitForClients = true;
            yield return null;
        }

        protected IEnumerator StartWithAddressableAssetAdded(AssetReferenceGameObject asset)
        {
            switch (m_LoadMode)
            {
                case LoadMode.StartAsync:
                    asset.LoadAssetAsync();
                    break;
                case LoadMode.WaitForFinish:
                    asset.LoadAssetAsync();
                    while (!asset.OperationHandle.IsDone)
                    {
                        var nextFrameNumber = Time.frameCount + 1;
                        yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
                    }

                    break;
            }
            yield return StartSomeClientsAndServerWithPlayers(true, NbClients, _ =>
            {
                m_ServerNetworkManager.OnStartupFailedCallback += (reason) => { m_ServerFailureReason = reason; };
                for (var i = 0; i < m_ClientNetworkManagers.Length; ++i)
                {
                    var setIndex = i;
                    m_ClientNetworkManagers[i].OnStartupFailedCallback += (reason) => { m_ClientFailureReasons[setIndex] = reason; };
                }

                m_ServerNetworkManager.AddNetworkPrefab(asset);
                foreach (var client in m_ClientNetworkManagers)
                {
                    client.AddNetworkPrefab(asset);
                }
            });
            if (m_ServerNetworkManager != null)
            {
                m_DefaultWaitForTick = new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            }
            MultiInstanceHelpers.Start(true, m_ServerNetworkManager, m_ClientNetworkManagers);

            RegisterSceneManagerHandler();

            yield return WaitForAllAssetsLoadedOrFailed();
        }

        protected IEnumerator StartWithAddressableAssetAdded(string address)
        {
            yield return StartSomeClientsAndServerWithPlayers(true, NbClients, _ =>
            {
                m_ServerNetworkManager.OnStartupFailedCallback += (reason) => { m_ServerFailureReason = reason; };
                for (var i = 0; i < m_ClientNetworkManagers.Length; ++i)
                {
                    var setIndex = i;
                    m_ClientNetworkManagers[i].OnStartupFailedCallback += (reason) => { m_ClientFailureReasons[setIndex] = reason; };
                }
                m_ServerNetworkManager.AddNetworkPrefab(address);
                foreach (var client in m_ClientNetworkManagers)
                {
                    client.AddNetworkPrefab(address);
                }
            });
            if (m_ServerNetworkManager != null)
            {
                m_DefaultWaitForTick = new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            }
            MultiInstanceHelpers.Start(true, m_ServerNetworkManager, m_ClientNetworkManagers);

            RegisterSceneManagerHandler();

            yield return WaitForAllAssetsLoadedOrFailed();
        }

        protected IEnumerator WaitForAllAssetsLoadedOrFailed()
        {
            while (m_ServerNetworkManager.State != NetworkManagerState.Ready && m_ServerNetworkManager.State != NetworkManagerState.Inactive)
            {
                var nextFrameNumber = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
            }
            foreach (var client in m_ClientNetworkManagers)
            {
                while (client.State != NetworkManagerState.Ready && client.State != NetworkManagerState.Inactive)
                {
                    var nextFrameNumber = Time.frameCount + 1;
                    yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
                }
            }
        }

        [UnityTest]
        public IEnumerator WhenLoadingAValidObject_SpawningItSucceedsOnServerAndClient()
        {
            GameObject prefab = null;
            if (m_AddMode == AddMode.AssetReference)
            {
                var asset = new AssetReferenceGameObject(k_ValidObject);
                yield return StartWithAddressableAssetAdded(asset);

                Assert.AreEqual(NetworkManagerState.Ready, m_ServerNetworkManager.State);
                foreach (var client in m_ClientNetworkManagers)
                {
                    Assert.AreEqual(NetworkManagerState.Ready, client.State);
                }

                switch (m_LookupType)
                {
                    case LookupType.NetworkManagerByAsset:
                        prefab = m_ServerNetworkManager.GetGameObjectForAddressable(asset);
                        break;
                    case LookupType.NetworkManagerByAddress:
                        prefab = m_ServerNetworkManager.GetGameObjectForAddress(k_ValidObject);
                        break;
                    case LookupType.AssetByOperation:
                        var assetLoad = asset.OperationHandle.Convert<GameObject>();
                        Assert.AreEqual(AsyncOperationStatus.Succeeded, assetLoad.Status);
                        prefab = assetLoad.Result;
                        break;
                }
            }
            else
            {
                yield return StartWithAddressableAssetAdded(k_ValidObject);

                Assert.AreEqual(NetworkManagerState.Ready, m_ServerNetworkManager.State);
                foreach (var client in m_ClientNetworkManagers)
                {
                    Assert.AreEqual(NetworkManagerState.Ready, client.State);
                }

                prefab = m_ServerNetworkManager.GetGameObjectForAddress(k_ValidObject);
            }

            var serverObj = Object.Instantiate(prefab);
            serverObj.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObj.GetComponent<NetworkObject>().Spawn();

            var objs = Object.FindObjectsOfType<AddressableTestScript>();
            // Prefabs loaded by addressables actually don't show up in this search.
            // Unlike other tests that make prefabs programmatically, those aren't added to the scene until they're instantiated
            Assert.AreEqual(1, objs.Length);

            yield return MultiInstanceHelpers.Run(
                MultiInstanceHelpers.WaitForMessageOfType<CreateObjectMessage>(m_ClientNetworkManagers[0])
            );

            objs = Object.FindObjectsOfType<AddressableTestScript>();
            Assert.AreEqual(2, objs.Length);
            foreach (var obj in objs)
            {
                Assert.AreEqual(1234567, obj.AnIntVal);
                Assert.AreEqual("1234567", obj.AStringVal);
                Assert.AreEqual("12345671234567", obj.GetValue());
            }
        }

        [UnityTest]
        public IEnumerator WhenLoadingAValidObject_StartupFailedCallbackIsNotCalled()
        {
            yield return WhenLoadingAValidObject_SpawningItSucceedsOnServerAndClient();
            Assert.AreEqual(NetworkManager.StartupFailureReason.None, m_ServerFailureReason);
            foreach (var reason in m_ClientFailureReasons)
            {
                Assert.AreEqual(NetworkManager.StartupFailureReason.None, reason);
            }
        }

        [UnityTest]
        public IEnumerator WhenLoadingAnInvalidObject_AnExceptionIsLogged()
        {
            LogAssert.Expect(LogType.Exception, new Regex("NetworkPrefab assets \\(and all children\\) MUST point to a GameObject with a NetworkObject component."));
            LogAssert.Expect(LogType.Exception, new Regex("NetworkPrefab assets \\(and all children\\) MUST point to a GameObject with a NetworkObject component."));
            if (m_AddMode == AddMode.AssetReference)
            {
                var asset = new AssetReferenceGameObject(k_InvalidObject);
                yield return StartWithAddressableAssetAdded(asset);
            }
            else
            {
                yield return StartWithAddressableAssetAdded(k_InvalidObject);
            }
        }

        public void AssertNetworkManagerShutDown()
        {
            Assert.AreEqual(NetworkManagerState.Inactive, m_ServerNetworkManager.State);
            Assert.IsFalse(m_ServerNetworkManager.IsListening);
            foreach (var client in m_ClientNetworkManagers)
            {
                Assert.AreEqual(NetworkManagerState.Inactive, client.State);
                Assert.IsFalse(client.IsListening);
            }
        }

        public void AssertStartupFailureWasTriggered()
        {
            Assert.AreEqual(NetworkManager.StartupFailureReason.AssetLoadFailed, m_ServerFailureReason);
            for (var i = 0; i < m_ClientNetworkManagers.Length; ++i)
            {
                Assert.AreEqual(NetworkManager.StartupFailureReason.AssetLoadFailed, m_ClientFailureReasons[i]);
            }
        }

        [UnityTest]
        public IEnumerator WhenLoadingAnInvalidObject_NetworkManagerShutsDown()
        {
            yield return WhenLoadingAnInvalidObject_AnExceptionIsLogged();
            AssertNetworkManagerShutDown();
        }

        [UnityTest]
        public IEnumerator WhenLoadingAnInvalidObject_StartupFailedCallbackIsExecuted()
        {
            yield return WhenLoadingAnInvalidObject_AnExceptionIsLogged();
            AssertStartupFailureWasTriggered();
        }

        [UnityTest]
        public IEnumerator WhenLoadingANonexistentObject_AnExceptionIsLogged()
        {
            // Addressables logs errors when something can't be loaded. The nature of that error
            // seems to be non-deterministic. For example, when running this test by itself, it
            // logs two errors; when running after another test that's loaded an addressable already,
            // it only loads one. Since LogAssert doesn't have a "LogAssert.Allow()" we just have
            // to not fail on unexpected messages.
            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Exception, new Regex("Could not load addressable object: IfIExistSomethingIsWrong.prefab"));
            LogAssert.Expect(LogType.Exception, new Regex("Could not load addressable object: IfIExistSomethingIsWrong.prefab"));
            if (m_AddMode == AddMode.AssetReference)
            {
                var asset = new AssetReferenceGameObject(k_NonexistentObject);
                yield return StartWithAddressableAssetAdded(asset);
            }
            else
            {
                yield return StartWithAddressableAssetAdded(k_NonexistentObject);
            }

            Assert.AreEqual(NetworkManagerState.Inactive, m_ServerNetworkManager.State);
            Assert.IsFalse(m_ServerNetworkManager.IsListening);
            foreach (var client in m_ClientNetworkManagers)
            {
                Assert.AreEqual(NetworkManagerState.Inactive, client.State);
                Assert.IsFalse(client.IsListening);
            }
        }

        [UnityTest]
        public IEnumerator WhenLoadingANonexistentObject_NetworkManagerShutsDown()
        {
            yield return WhenLoadingANonexistentObject_AnExceptionIsLogged();
            AssertNetworkManagerShutDown();
        }

        [UnityTest]
        public IEnumerator WhenLoadingANonexistentObject_StartupFailedCallbackIsExecuted()
        {
            yield return WhenLoadingANonexistentObject_AnExceptionIsLogged();
            AssertStartupFailureWasTriggered();
        }
    }
}
