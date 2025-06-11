using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(NetworkTopologyTypes.ClientServer)]
    [TestFixture(NetworkTopologyTypes.DistributedAuthority)]
    internal class NetworkPrefabHandlerWithDataTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 4;
        private const string k_TestPrefabObjectName = "NetworkPrefabTestObject";
        private GameObject m_Prefab;
        private PrefabInstanceHandlerWithData[] m_ClientHandlers;

        public NetworkPrefabHandlerWithDataTests(NetworkTopologyTypes topology) : base(topology)
        {
        }

        protected override void OnServerAndClientsCreated()
        {
            // Creates a network object prefab and registers it to all clients.
            m_Prefab = CreateNetworkObjectPrefab(k_TestPrefabObjectName).gameObject;
            var authority = GetAuthorityNetworkManager();

            m_ClientHandlers = new PrefabInstanceHandlerWithData[NumberOfClients];
            var idx = 0;
            foreach (var manager in m_NetworkManagers)
            {
                RegisterPrefabHandler(manager, out var handler);
                if (manager != authority)
                {
                    m_ClientHandlers[idx] = handler;
                    idx++;
                }
            }
        }

        private PrefabInstanceHandlerWithData m_LateJoinPrefabHandler;
        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            // This will register all prefabs from the authority to the newly created client.
            base.OnNewClientCreated(networkManager);

            RegisterPrefabHandler(networkManager, out var lateJoinPrefabHandler);
            m_LateJoinPrefabHandler = lateJoinPrefabHandler;
        }

        [UnityTest]
        public IEnumerator InstantiationPayload_SyncsCorrectly()
        {
            var data = new NetworkSerializableTest { Value = 12, Value2 = 3.14f };

            SpawnPrefabWithData(data);

            yield return WaitForConditionOrTimeOut(() => AllHandlersSynchronized(data));
            AssertOnTimeout("Not all handlers synchronized");
        }

        [UnityTest]
        public IEnumerator InstantiationPayload_LateJoinersReceiveData()
        {
            var data = new NetworkSerializableTest { Value = 42, Value2 = 2.71f };
            var spawned = SpawnPrefabWithData(data);

            yield return WaitForConditionOrTimeOut(() => AllHandlersSynchronized(data));
            AssertOnTimeout("Not all handlers synchronized");

            // When running with Distributed Authority, test a late-joiner after an ownership change
            // The object owner will synchronize the late joining client, showing that the instantiationData will survive host migration.
            if (m_DistributedAuthority)
            {
                var newOwner = m_NetworkManagers.First(m => m.LocalClientId != spawned.OwnerClientId);
                spawned.ChangeOwnership(newOwner.LocalClientId);

                yield return WaitForConditionOrTimeOut(() =>
                {
                    if (newOwner.SpawnManager.SpawnedObjects.TryGetValue(spawned.NetworkObjectId, out var clientObject))
                    {
                        return clientObject.OwnerClientId == newOwner.LocalClientId;
                    }

                    return false;
                });
                AssertOnTimeout($"Timed out while waiting for Client-{newOwner.LocalClientId} to own object");
            }

            // Late join a client
            yield return CreateAndStartNewClient();

            // Confirm late joiner got correct data
            yield return WaitForConditionOrTimeOut(() => m_LateJoinPrefabHandler.InstantiationData.IsSynchronizedWith(data));
            AssertOnTimeout("Late joiner received incorrect data");
        }

        private void RegisterPrefabHandler(NetworkManager manager, out PrefabInstanceHandlerWithData handler)
        {
            handler = new PrefabInstanceHandlerWithData(m_Prefab);
            manager.PrefabHandler.AddHandler(m_Prefab, handler);
        }

        private NetworkObject SpawnPrefabWithData(NetworkSerializableTest data)
        {
            var instance = UnityEngine.Object.Instantiate(m_Prefab).GetComponent<NetworkObject>();
            GetAuthorityNetworkManager().PrefabHandler.SetInstantiationData(instance, data);
            instance.Spawn();
            return instance;
        }

        private bool AllHandlersSynchronized(NetworkSerializableTest expectedData)
        {
            return m_ClientHandlers.All(handler => handler.InstantiationData.IsSynchronizedWith(expectedData));
        }

        private class PrefabInstanceHandlerWithData : NetworkPrefabInstanceHandlerWithData<NetworkSerializableTest>
        {
            private readonly GameObject m_Prefab;
            public NetworkSerializableTest InstantiationData;

            public PrefabInstanceHandlerWithData(GameObject prefab)
            {
                m_Prefab = prefab;
            }

            public override NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, NetworkSerializableTest data)
            {
                InstantiationData = data;
                return UnityEngine.Object.Instantiate(m_Prefab, position, rotation).GetComponent<NetworkObject>();
            }

            public override void Destroy(NetworkObject networkObject)
            {
                UnityEngine.Object.DestroyImmediate(networkObject.gameObject);
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
