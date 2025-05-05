using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class NetworkPrefabHandlerWithDataTests
    {
        private GameObject _prefab;

        private NetworkManager server;
        private NetworkManager[] clients;
        const int k_clientCount = 4;

        private PrefabInstanceHandlerWithData server_handler;
        private PrefabInstanceHandlerWithData[] client_handlers;

        private const string k_TestPrefabObjectName = "NetworkPrefabTestObject";
        private uint m_ObjectId = 1;
        private GameObject MakeValidNetworkPrefab()
        {
            Guid baseObjectID = NetworkManagerHelper.AddGameNetworkObject(k_TestPrefabObjectName + m_ObjectId.ToString());
            NetworkObject validPrefab = NetworkManagerHelper.InstantiatedNetworkObjects[baseObjectID];
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(validPrefab);
            m_ObjectId++;
            return validPrefab.gameObject;
        }

        [SetUp]
        public void Setup()
        {
            NetcodeIntegrationTestHelpers.Create(k_clientCount, out server, out clients);

            _prefab = MakeValidNetworkPrefab();

            NetworkPrefab networkPrefab = new NetworkPrefab() { Prefab = _prefab };

            server.NetworkConfig.Prefabs.Add(networkPrefab);
            server_handler = new PrefabInstanceHandlerWithData(_prefab);
            server.PrefabHandler.AddHandler(_prefab, server_handler);

            client_handlers = new PrefabInstanceHandlerWithData[clients.Length];
            for (int i = 0; i < clients.Length; i++)
            {
                client_handlers[i] = new PrefabInstanceHandlerWithData(_prefab);
                clients[i].NetworkConfig.Prefabs.Add(networkPrefab);
                clients[i].PrefabHandler.AddHandler(_prefab, client_handlers[i]);
            }
        }

        [TearDown]
        public void Teardown()
        {
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].PrefabHandler.RemoveHandler(_prefab);
                clients[i].NetworkConfig.Prefabs.Remove(_prefab);
                clients[i].Shutdown();
            }

            server.PrefabHandler.RemoveHandler(_prefab);
            server.NetworkConfig.Prefabs.Remove(_prefab);
            server.Shutdown();

            UnityEngine.Object.DestroyImmediate(_prefab);
        }

        [UnityTest]
        public IEnumerator InstantiationPayload_SyncsCorrectly()
        {
            // Start the instances
            if (!NetcodeIntegrationTestHelpers.Start(true, server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // [Client-Side] Wait for a connection to the server
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnected(clients, null, 512);

            // [Host-Side] Check to make sure all clients are connected
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnectedToServer(server, clients.Length + 1, null, 512);

            //Sets the values to synchronize
            server_handler.ValueToSynchronize = 48;
            server_handler.networksSerializableToSynchronize = new NetworkSerializableTest() { Value = 12, Value2 = 3.14f };

            // Spawn the prefab on the server
            var spawned = server.SpawnManager.InstantiateAndSpawn(_prefab.GetComponent<NetworkObject>());
            Assert.NotNull(spawned);

            // wait for the clients to receive the instantiation payload
            var timeoutHelper = new TimeoutHelper();
            yield return NetcodeIntegrationTest.WaitForConditionOrTimeOut(() => client_handlers.All(handler => handler.ValueToSynchronize == server_handler.ValueToSynchronize));
            Assert.False(timeoutHelper.TimedOut, "Did not successfully sync all handlers");

            // Check that the values are synchronized
            for (int i = 0; i < client_handlers.Length; i++)
            {
                Assert.IsTrue(client_handlers[i].IsSynchronizedWith(server_handler), "Client handler " + i + " is not synchronized with server handler");
            }
        }

        private class PrefabInstanceHandlerWithData : INetworkPrefabInstanceHandlerWithData
        {
            public GameObject Prefab;
            public int ValueToSynchronize;
            public NetworkSerializableTest networksSerializableToSynchronize;

            public PrefabInstanceHandlerWithData(GameObject prefab)
            {
                Prefab = prefab;
            }

            public virtual void OnSynchronizeInstantiationData<T>(ref BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ValueToSynchronize);
                serializer.SerializeValue(ref networksSerializableToSynchronize);
            }

            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                var instance = GameObject.Instantiate(Prefab, position, rotation).GetComponent<NetworkObject>();
                return instance;
            }

            public void Destroy(NetworkObject networkObject)
            {
                GameObject.DestroyImmediate(networkObject.gameObject);
            }

            public bool IsSynchronizedWith(PrefabInstanceHandlerWithData other)
            {
                if (other == null)
                    return false;

                bool isSynchronized = true;
                isSynchronized &= ValueToSynchronize == other.ValueToSynchronize;
                isSynchronized &= networksSerializableToSynchronize.Value == other.networksSerializableToSynchronize.Value;
                isSynchronized &= networksSerializableToSynchronize.Value2 == other.networksSerializableToSynchronize.Value2;
                return isSynchronized;
            }
        }

        struct NetworkSerializableTest : INetworkSerializable
        {
            public int Value;
            public float Value2;
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Value);
                serializer.SerializeValue(ref Value2);
            }
        }
    }
}
