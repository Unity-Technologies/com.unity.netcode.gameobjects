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
    public class AddressablesTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 1;

        private const string k_ValidObject = "AddressableTestObject.prefab";
        private const string k_InvalidObject = "InvalidAddressableTestObject.prefab";
        private const string k_InvalidChild = "AddressableTestObjectWithInvalidChild.prefab";


        [UnitySetUp]
        public override IEnumerator Setup()
        {
            m_BypassStartAndWaitForClients = true;
            yield return null;
        }

        protected IEnumerator StartWithAddressableAssetAdded(AssetReferenceGameObject asset)
        {
            yield return StartSomeClientsAndServerWithPlayers(true, NbClients, _ =>
            {
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

        protected IEnumerator WaitForAllAssetsLoadedOrFailed()
        {
            while (m_ServerNetworkManager.State != NetworkManagerState.Ready && m_ServerNetworkManager.State != NetworkManagerState.Inactive)
            {
                var nextFrameNumber = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
            }
            foreach(var client in m_ClientNetworkManagers)
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
            var asset = new AssetReferenceGameObject(k_ValidObject);
            yield return StartWithAddressableAssetAdded(asset);

            Assert.AreEqual(NetworkManagerState.Ready, m_ServerNetworkManager.State);
            foreach (var client in m_ClientNetworkManagers)
            {
                Assert.AreEqual(NetworkManagerState.Ready, client.State);
            }

            var assetLoad = asset.OperationHandle.Convert<GameObject>();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, assetLoad.Status);
            var prefab = assetLoad.Result;

            var serverObj = GameObject.Instantiate(prefab);
            serverObj.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObj.GetComponent<NetworkObject>().Spawn();

            var objs = GameObject.FindObjectsOfType<AddressableTestScript>();
            // Prefabs loaded by addressables actually don't show up in this search.
            // Unlike other tests that make prefabs programmatically, those aren't added to the scene until they're instantiated
            Assert.AreEqual(1, objs.Length);

            yield return MultiInstanceHelpers.Run(
                MultiInstanceHelpers.WaitForMessageOfType<CreateObjectMessage>(m_ClientNetworkManagers[0])
            );

            objs = GameObject.FindObjectsOfType<AddressableTestScript>();
            Assert.AreEqual(2, objs.Length);
            foreach (var obj in objs)
            {
                Assert.AreEqual(1234567, obj.AnIntVal);
                Assert.AreEqual("1234567", obj.AStringVal);
                Assert.AreEqual("12345671234567", obj.GetValue());
            }
        }



        [UnityTest]
        public IEnumerator WhenLoadingAnInvalidObject_AnExceptionIsThrown()
        {
            LogAssert.Expect(LogType.Error, new Regex("Addressables assets \\(and all children\\) MUST point to a GameObject with a NetworkObject component."));
            LogAssert.Expect(LogType.Error, new Regex("Addressables assets \\(and all children\\) MUST point to a GameObject with a NetworkObject component."));
            var asset = new AssetReferenceGameObject(k_InvalidObject);
            yield return StartWithAddressableAssetAdded(asset);

            Assert.AreEqual(NetworkManagerState.Inactive, m_ServerNetworkManager.State);
            Assert.IsFalse(m_ServerNetworkManager.IsListening);
            foreach (var client in m_ClientNetworkManagers)
            {
                Assert.AreEqual(NetworkManagerState.Inactive, client.State);
                Assert.IsFalse(client.IsListening);
            }
        }
    }
}
