using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class CustomType : INetworkSerializable
    {
        public int SomeValue;
        private const int k_ByteFactor = 256;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.TryBeginRead(2);
                byte value;
                reader.ReadByte(out value);
                SomeValue = value * k_ByteFactor;
                reader.ReadByte(out value);
                SomeValue += value;
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.TryBeginWrite(2);
                writer.WriteByte((byte)(SomeValue / k_ByteFactor));
                writer.WriteByte((byte)(SomeValue % k_ByteFactor));
            }
        }
    };

    public class DifferentCustomType : INetworkSerializable
    {
        public int SomeValue;
        public bool SomeBoolean;
        private const int k_ByteFactor = 256;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.TryBeginRead(3);
                byte value;
                reader.ReadByte(out value);
                SomeValue = value * k_ByteFactor;
                reader.ReadByte(out value);
                SomeValue += value;

                reader.ReadByte(out value);
                SomeBoolean = value > 0;
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.TryBeginWrite(3);
                writer.WriteByte((byte)(SomeValue / k_ByteFactor));
                writer.WriteByte((byte)(SomeValue % k_ByteFactor));
                writer.WriteByte((byte)(SomeBoolean ? 1 : 0));
            }
        }
    };

    public class RpcOwnershipTest : NetworkBehaviour
    {

    }

    public class RpcOwnershipObject : NetworkBehaviour
    {
        public int RequireOwnershipCount = 0;
        public int DoesntRequireOwnershipCount = 0;
        public int ArrayRpcCount = 0;
        public bool ReliableServerRpcCalled;
        public bool ReliableClientRpcCalled;
        public bool UnreliableServerRpcCalled;
        public bool UnreliableClientRpcCalled;

        [ServerRpc]
        public void ExpectingNullObjectServerRpc(CustomType anObject)
        {
            Debug.Assert(anObject == null);
        }

        [ServerRpc(RequireOwnership = true)]
        public void RequireOwnershipServerRpc()
        {
            RequireOwnershipCount++;
        }

        [ServerRpc(RequireOwnership = false)]
        public void DoesntRequireOwnershipServerRpc()
        {
            DoesntRequireOwnershipCount++;
        }

        [ServerRpc]
        public void TwoCustomTypesAndVect3ServerRpc(CustomType someObject, DifferentCustomType someOtherObject, Vector3 vect)
        {
            Debug.Assert(someObject.SomeValue == 50);
            Debug.Assert(someOtherObject.SomeValue == 300);
            Debug.Assert(someOtherObject.SomeBoolean = true);
            Debug.Assert(vect.x == 20);
            Debug.Assert(vect.y == 30);
            Debug.Assert(vect.z == 40);
        }

        [ServerRpc]
        public void ArrayOfCustomTypesServerRpc(CustomType[] arrayOfObjects, CustomType[] emptyArray, CustomType[] nullArray)
        {
            Debug.Assert(arrayOfObjects.Length > 0);
            for (int i = 0; i < arrayOfObjects.Length; i++)
            {
                if (i > 0)
                {
                    Debug.Assert(arrayOfObjects[i].SomeValue > arrayOfObjects[i - 1].SomeValue);
                }
            }

            Debug.Assert(emptyArray.Length == 0);
            Debug.Assert(nullArray == null);

            ArrayRpcCount++;
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        public void UnreliableServerRpc()
        {
            UnreliableServerRpcCalled = true;
        }
        [ServerRpc(Delivery = RpcDelivery.Reliable)]
        public void ReliableServerRpc()
        {
            ReliableServerRpcCalled = true;
        }
        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        public void UnreliableClientRpc()
        {
            UnreliableClientRpcCalled = true;
        }
        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        public void ReliableClientRpc()
        {
            ReliableClientRpcCalled = true;
        }
    }

    public class RpcOwnershipCustomTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 2;

        private GameObject m_PrefabToSpawn;

        private int m_ExpectedRequireOwnershipCount = 0;
        private int m_ExpectedDoesntRequireOwnershipCount = 0;


        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(useHost: true, nbClients: NbClients,
                updatePlayerPrefab: playerPrefab =>
                {
                    var networkTransform = playerPrefab.AddComponent<RpcOwnershipTest>();
                    m_PrefabToSpawn = PreparePrefab(typeof(RpcOwnershipObject));
                });
        }

        public GameObject PreparePrefab(Type type)
        {
            var prefabToSpawn = new GameObject();
            prefabToSpawn.AddComponent(type);
            var networkObjectPrefab = prefabToSpawn.AddComponent<NetworkObject>();
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObjectPrefab);
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
            }
            return prefabToSpawn;
        }

        [UnityTest]
        public IEnumerator RpcOwnershipTest()
        {
            yield return RunTests(false);
            yield return RunTests(true);
        }

        [UnityTest]
        public IEnumerator RpcArrayCustomTypesTest()
        {
            var spawnedObject = UnityEngine.Object.Instantiate(m_PrefabToSpawn);
            var netSpawnedObject = spawnedObject.GetComponent<NetworkObject>();
            netSpawnedObject.NetworkManagerOwner = m_ServerNetworkManager;

            netSpawnedObject.Spawn();

            netSpawnedObject.GetComponent<RpcOwnershipObject>().ExpectingNullObjectServerRpc(null);

            var arrayOfObjects = new CustomType[2] { new CustomType(), new CustomType() };
            var emptyArray = new CustomType[0];

            arrayOfObjects[0].SomeValue = 1;
            arrayOfObjects[1].SomeValue = 2;

            netSpawnedObject.GetComponent<RpcOwnershipObject>().ArrayOfCustomTypesServerRpc(arrayOfObjects, emptyArray, null);

            Debug.Assert(netSpawnedObject.GetComponent<RpcOwnershipObject>().ArrayRpcCount == 1);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CustomTypesTest()
        {
            var spawnedObject = UnityEngine.Object.Instantiate(m_PrefabToSpawn);
            var netSpawnedObject = spawnedObject.GetComponent<NetworkObject>();
            netSpawnedObject.NetworkManagerOwner = m_ServerNetworkManager;

            netSpawnedObject.Spawn();

            var someObject = new CustomType() { SomeValue = 50 };
            var someOtherObject = new DifferentCustomType() { SomeValue = 300, SomeBoolean = true };
            var vect = new Vector3() { x = 20, y = 30, z = 40 };

            spawnedObject.GetComponent<RpcOwnershipObject>().TwoCustomTypesAndVect3ServerRpc(someObject, someOtherObject, vect);

            yield return null;
        }

        [UnityTest]
        public IEnumerator RpcDeliveryTests()
        {
            var spawnedObject = UnityEngine.Object.Instantiate(m_PrefabToSpawn);
            var netSpawnedObject = spawnedObject.GetComponent<NetworkObject>();
            netSpawnedObject.NetworkManagerOwner = m_ServerNetworkManager;

            var rpcObject = spawnedObject.GetComponent<RpcOwnershipObject>();

            netSpawnedObject.Spawn();

            rpcObject.ReliableServerRpc();
            rpcObject.ReliableClientRpc();

            // Those two calls are unreliable. So, for testing, we'll call them multiple
            // times (just one call might be dropped). If it so happens that 10 calls in a row are missed, then some
            // debugging would be worth.
            for (int i = 0; i < 10; i++)
            {
                rpcObject.UnreliableServerRpc();
                rpcObject.UnreliableClientRpc();

                var nextFrameNumber = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
            }

            Debug.Assert(rpcObject.ReliableServerRpcCalled);
            Debug.Assert(rpcObject.UnreliableServerRpcCalled);
            Debug.Assert(rpcObject.ReliableClientRpcCalled);
            Debug.Assert(rpcObject.UnreliableClientRpcCalled);
        }

        private IEnumerator RunTests(bool serverOwned)
        {
            m_ExpectedRequireOwnershipCount = 0;
            m_ExpectedDoesntRequireOwnershipCount = 0;

            var spawnedObject = UnityEngine.Object.Instantiate(m_PrefabToSpawn);
            var netSpawnedObject = spawnedObject.GetComponent<NetworkObject>();
            netSpawnedObject.NetworkManagerOwner = m_ServerNetworkManager;

            if (serverOwned)
            {
                netSpawnedObject.Spawn();
            }
            else
            {
                netSpawnedObject.SpawnWithOwnership(m_ClientNetworkManagers[1].LocalClientId);
            }

            // send RPCs from server
            if (!serverOwned)
            {
                LogAssert.Expect(LogType.Error, "Only the owner can invoke a ServerRpc that requires ownership!");
            }
            else
            {
                m_ExpectedRequireOwnershipCount++;
            }

            m_ExpectedDoesntRequireOwnershipCount++;
            spawnedObject.GetComponent<RpcOwnershipObject>().RequireOwnershipServerRpc();
            spawnedObject.GetComponent<RpcOwnershipObject>().DoesntRequireOwnershipServerRpc();

            // get the matching object on the client side
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(
                MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                    x => x.NetworkObjectId == netSpawnedObject.NetworkObjectId,
                    m_ClientNetworkManagers[1],
                    serverClientPlayerResult));
            var netSpawnedObjectOnClient = serverClientPlayerResult.Result;
            netSpawnedObjectOnClient.NetworkManagerOwner = m_ClientNetworkManagers[1];

            // send RPCs from the client
            if (serverOwned) // condition is reversed, compared to above
            {
                LogAssert.Expect(LogType.Error, "Only the owner can invoke a ServerRpc that requires ownership!");
            }
            else
            {
                m_ExpectedRequireOwnershipCount++;
            }

            m_ExpectedDoesntRequireOwnershipCount++;
            netSpawnedObjectOnClient.GetComponent<RpcOwnershipObject>().RequireOwnershipServerRpc();
            netSpawnedObjectOnClient.GetComponent<RpcOwnershipObject>().DoesntRequireOwnershipServerRpc();

            var nextFrameNumber = Time.frameCount + 3;
            yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);

            // verify counts
            Debug.Assert(spawnedObject.GetComponent<RpcOwnershipObject>().RequireOwnershipCount == m_ExpectedRequireOwnershipCount);
            Debug.Assert(spawnedObject.GetComponent<RpcOwnershipObject>().DoesntRequireOwnershipCount == m_ExpectedDoesntRequireOwnershipCount);
        }
    }
}
