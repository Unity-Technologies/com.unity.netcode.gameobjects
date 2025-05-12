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
        private const int k_ClientCount = 4;
        private const string k_TestPrefabObjectName = "NetworkPrefabTestObject";
        private uint m_ObjectId = 1;

        private GameObject _prefab;
        private NetworkManager server;
        private NetworkManager[] clients;

        private PrefabInstanceHandlerWithData[] clientHandlers;

        [SetUp]
        public void Setup()
        {
            NetcodeIntegrationTestHelpers.Create(k_ClientCount, out server, out clients);
            _prefab = CreateNetworkPrefab();

            RegisterPrefab(server, out _);

            clientHandlers = new PrefabInstanceHandlerWithData[clients.Length];
            for (int i = 0; i < clients.Length; i++)
            {
                RegisterPrefab(clients[i], out clientHandlers[i]);
            }
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var client in clients)
            {
                client.PrefabHandler.RemoveHandler(_prefab);
                client.NetworkConfig.Prefabs.Remove(_prefab);
                client.Shutdown();
            }

            server.PrefabHandler.RemoveHandler(_prefab);
            server.NetworkConfig.Prefabs.Remove(_prefab);
            server.Shutdown();

            UnityEngine.Object.DestroyImmediate(_prefab);
            NetcodeIntegrationTestHelpers.Destroy();
        }

        [UnityTest]
        public IEnumerator InstantiationPayload_SyncsCorrectly()
        {
            yield return StartAndWaitForClients();
            var data = new NetworkSerializableTest { Value = 12, Value2 = 3.14f };
            SpawnPrefabWithData(data);
            yield return WaitForAllClientsToSync(data);
        }

        [UnityTest]
        public IEnumerator InstantiationPayload_LateJoinersReceiveData()
        {
            yield return StartAndWaitForClients();
            var data = new NetworkSerializableTest { Value = 42, Value2 = 2.71f };
            SpawnPrefabWithData(data);

            // Disconnect and destroy one client to simulate late join
            var lateJoiner = clients[0];
            lateJoiner.Shutdown();
            yield return null;

            var lateJoinerIndex = 0;
            clients[lateJoinerIndex] = NetcodeIntegrationTestHelpers.CreateNewClient(k_ClientCount);
            RegisterPrefab(clients[lateJoinerIndex], out clientHandlers[lateJoinerIndex]);

            NetcodeIntegrationTestHelpers.StartOneClient(clients[lateJoinerIndex]);
            yield return NetcodeIntegrationTestHelpers.WaitForClientConnected(clients[lateJoinerIndex]);

            // Confirm late joiner got correct data
            var timeoutHelper = new TimeoutHelper();
            yield return NetcodeIntegrationTest.WaitForConditionOrTimeOut(() => clientHandlers[lateJoinerIndex].instantiationData.IsSynchronizedWith(data));
            Assert.False(timeoutHelper.TimedOut, "Late joiner did not synchronize properly with instantiation data.");
        }

        private GameObject CreateNetworkPrefab()
        {
            var guid = NetworkManagerHelper.AddGameNetworkObject($"{k_TestPrefabObjectName}{m_ObjectId++}");
            var networkObject = NetworkManagerHelper.InstantiatedNetworkObjects[guid];
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);
            return networkObject.gameObject;
        }

        private void RegisterPrefab(NetworkManager manager, out PrefabInstanceHandlerWithData handler)
        {
            var networkPrefab = new NetworkPrefab { Prefab = _prefab };
            manager.NetworkConfig.Prefabs.Add(networkPrefab);

            handler = new PrefabInstanceHandlerWithData(_prefab);
            manager.PrefabHandler.AddHandler(_prefab, handler);
        }

        private NetworkObject SpawnPrefabWithData(NetworkSerializableTest data)
        {
            var instance = GameObject.Instantiate(_prefab).GetComponent<NetworkObject>();
            server.PrefabHandler.InjectInstantiationData(instance, data);
            instance.Spawn();
            return instance;
        }

        private IEnumerator StartAndWaitForClients()
        {
            if (!NetcodeIntegrationTestHelpers.Start(true, server, clients))
                Assert.Fail("Failed to start instances");

            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnected(clients, null, 512);
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnectedToServer(server, clients.Length + 1, null, 512);
        }

        private IEnumerator WaitForAllClientsToSync(NetworkSerializableTest expectedData)
        {
            var timeoutHelper = new TimeoutHelper();
            yield return NetcodeIntegrationTest.WaitForConditionOrTimeOut(() => clientHandlers.All(h => h.instantiationData.IsSynchronizedWith(expectedData)));
            Assert.False(timeoutHelper.TimedOut, "Data did not synchronize correctly to all clients.");
        }

        private class PrefabInstanceHandlerWithData : INetworkPrefabInstanceHandlerWithData<NetworkSerializableTest>
        {
            public GameObject Prefab;
            public NetworkSerializableTest instantiationData;

            public PrefabInstanceHandlerWithData(GameObject prefab)
            {
                Prefab = prefab;
            }

            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, NetworkSerializableTest data)
            {
                instantiationData = data;
                return GameObject.Instantiate(Prefab, position, rotation).GetComponent<NetworkObject>();
            }

            public void Destroy(NetworkObject networkObject)
            {
                GameObject.DestroyImmediate(networkObject.gameObject);
            }
        }

        private struct NetworkSerializableTest : INetworkSerializable
        {
            public int Value;
            public float Value2;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Value);
                serializer.SerializeValue(ref Value2);
            }

            public bool IsSynchronizedWith(NetworkSerializableTest other)
                => Value == other.Value && Math.Abs(Value2 - other.Value2) < 0.0001f;
        }
    }
}
