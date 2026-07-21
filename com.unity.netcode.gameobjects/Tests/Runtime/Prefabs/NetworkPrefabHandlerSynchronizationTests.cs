using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkPrefabHandlerSynchronizationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        public NetworkPrefabHandlerSynchronizationTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        private GameObject m_ValidPrefab;
        private GameObject m_ClientSideValidPrefab;
        private GameObject m_ClientSideExceptionPrefab;

        protected override void OnServerAndClientsCreated()
        {
            m_ValidPrefab = CreateNetworkObjectPrefab("ValidPrefab");
            m_ClientSideValidPrefab = CreateNetworkObjectPrefab("ClientSideValidPrefab");
            m_ClientSideExceptionPrefab = CreateNetworkObjectPrefab("ClientSideExceptionPrefab");
            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        [UnityPlatform(exclude = new[] { RuntimePlatform.IPhonePlayer, RuntimePlatform.OSXPlayer, RuntimePlatform.OSXEditor })] // Ignored test tracked in MTT-15473
        public IEnumerator NetworkPrefabHandlerSpawnAndSynchronizeTests()
        {
            var nonAuthority = GetNonAuthorityNetworkManager();

            var networkObjectToSpawnOnClient = m_ClientSideValidPrefab.GetComponent<NetworkObject>();
            nonAuthority.PrefabHandler.AddHandler(m_ClientSideExceptionPrefab, new NetworkPrefabExceptionThrower());
            var prefabHandlerObject = new GameObject();
            var prefabHandler = prefabHandlerObject.AddComponent<NetworkPrefabInstanceHandler>();
            prefabHandler.Initialize(nonAuthority, m_ValidPrefab.GetComponent<NetworkObject>());
            //nonAuthority.PrefabHandler.AddHandler(m_ValidPrefab, new NetworkPrefabInstanceHandler(networkObjectToSpawnOnClient));

            var authority = GetAuthorityNetworkManager();

            // Spawn the invalid object first.
            var exceptionObject = SpawnObject(m_ClientSideExceptionPrefab, authority).GetComponent<NetworkObject>();

            // Check the invalid object spawns on the authority, expect an error from non-authority.
            LogAssert.Expect(LogType.Exception, "Exception: exception while instantiating");
            LogAssert.Expect(LogType.Error, new Regex("Failed to spawn NetworkObject!"));
            // Authority should receive an error from non-authority and should use the globalObjectIdHash to find the failing object
            LogAssert.Expect(LogType.Error, new Regex($@"SenderId:{nonAuthority.LocalClientId}\]\[{Regex.Escape(exceptionObject.name)}"));

            yield return WaitForConditionOrTimeOut(() => exceptionObject.IsSpawned);
            AssertOnTimeout("Failed to spawn object on authority!");

            // Now spawn a valid object
            var validObject = SpawnObject(m_ValidPrefab, authority).GetComponent<NetworkObject>();

            // The valid object should spawn as expected
            yield return WaitForSpawnedOnAllOrTimeOut(validObject);
            AssertOnTimeout("Failed to spawn valid prefab on all clients!");

            // Create a new client and register the same PrefabHandlers on the client
            var newClient = CreateNewClient();
            var prefabHandlerObject2 = new GameObject();
            var prefabHandler2 = prefabHandlerObject2.AddComponent<NetworkPrefabExceptionThrower>();

            newClient.PrefabHandler.AddHandler(m_ClientSideExceptionPrefab, new NetworkPrefabExceptionThrower());

            var prefabHandlerObject3 = new GameObject();
            var prefabHandler3 = prefabHandlerObject3.AddComponent<NetworkPrefabInstanceHandler>();
            prefabHandler3.Initialize(nonAuthority, networkObjectToSpawnOnClient);

            // Expect assertions from the new client
            LogAssert.Expect(LogType.Exception, "Exception: exception while instantiating");
            LogAssert.Expect(LogType.Error, new Regex("Failed to spawn NetworkObject!"));

            // Authority will receive an error from new client and should use the globalObjectIdHash to find the failing object
            var expectedNewClientId = nonAuthority.LocalClientId + 1;
            LogAssert.Expect(LogType.Error, new Regex($@"SenderId:{expectedNewClientId}\]\[{Regex.Escape(exceptionObject.name)}"));

            // Start and synchronize the new client
            yield return StartClient(newClient);

            // Validate the valid prefab spawned on all clients without issue
            var expectedAuthorityHash = m_ValidPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash;
            var expectedNonAuthorityHash = m_ClientSideValidPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash;
            foreach (var networkManager in m_NetworkManagers)
            {
                Assert.True(networkManager.SpawnManager.SpawnedObjects.TryGetValue(validObject.NetworkObjectId, out NetworkObject spawnedObject), $"Client-{networkManager.LocalClientId} failed to spawn version of valid object!");

                if (spawnedObject.HasAuthority)
                {
                    Assert.That(spawnedObject.GlobalObjectIdHash, Is.EqualTo(expectedAuthorityHash), "NetworkObject spawned with unexpected GlobalObjectIdHash!");
                    Assert.That(networkManager.SpawnManager.SpawnedObjects.ContainsKey(exceptionObject.NetworkObjectId), Is.True, "Authority missing spawned NetworkObject!");
                }
                else
                {
                    Assert.That(spawnedObject.GlobalObjectIdHash, Is.EqualTo(expectedNonAuthorityHash), "NetworkObject spawned with unexpected GlobalObjectIdHash!");
                    Assert.That(networkManager.SpawnManager.SpawnedObjects.ContainsKey(exceptionObject.NetworkObjectId), Is.False, "Non authority should not have spawned exception object!");
                }
            }

            Object.Destroy(prefabHandlerObject);
        }
    }
}
