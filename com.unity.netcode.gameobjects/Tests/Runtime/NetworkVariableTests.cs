using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode.TestHelpers.Runtime;
using Random = UnityEngine.Random;
using UnityEngine;

namespace Unity.Netcode.RuntimeTests
{
    public class NetVarPermTestComp : NetworkBehaviour
    {
        public NetworkVariable<Vector3> OwnerWritable_Position = new NetworkVariable<Vector3>(Vector3.one, NetworkVariableBase.DefaultReadPerm, NetworkVariableWritePermission.Owner);
        public NetworkVariable<Vector3> ServerWritable_Position = new NetworkVariable<Vector3>(Vector3.one, NetworkVariableBase.DefaultReadPerm, NetworkVariableWritePermission.Server);
        public NetworkVariable<Vector3> OwnerReadWrite_Position = new NetworkVariable<Vector3>(Vector3.one, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
    }

    // The ILPP code for NetworkVariables to determine how to serialize them relies on them existing as fields of a NetworkBehaviour to find them.
    // Some of the tests below create NetworkVariables on the stack, so this class is here just to make sure the relevant types are all accounted for.
    public class NetVarILPPClassForTests : NetworkBehaviour
    {
        public NetworkVariable<UnmanagedNetworkSerializableType> UnmanagedNetworkSerializableTypeVar;
        public NetworkVariable<ManagedNetworkSerializableType> ManagedNetworkSerializableTypeVar;
        public NetworkVariable<string> StringVar;
        public NetworkVariable<Guid> GuidVar;
    }

    public class TemplateNetworkBehaviourType<T> : NetworkBehaviour
    {
        public NetworkVariable<T> TheVar;
    }

    public class ClassHavingNetworkBehaviour : TemplateNetworkBehaviourType<TestClass>
    {

    }

    // Please do not reference TestClass2 anywhere other than here!
    public class ClassHavingNetworkBehaviour2 : TemplateNetworkBehaviourType<TestClass_ReferencedOnlyByTemplateNetworkBehavourType>
    {

    }

    public class StructHavingNetworkBehaviour : TemplateNetworkBehaviourType<TestStruct>
    {

    }

    public struct StructUsedOnlyInNetworkList : IEquatable<StructUsedOnlyInNetworkList>, INetworkSerializeByMemcpy
    {
        public int Value;

        public bool Equals(StructUsedOnlyInNetworkList other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is StructUsedOnlyInNetworkList other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }

    [TestFixtureSource(nameof(TestDataSource))]
    public class NetworkVariablePermissionTests : NetcodeIntegrationTest
    {
        public static IEnumerable<TestFixtureData> TestDataSource()
        {
            foreach (HostOrServer hostOrServer in Enum.GetValues(typeof(HostOrServer)))
            {
                yield return new TestFixtureData(hostOrServer);
            }
        }

        protected override int NumberOfClients => 3;

        public NetworkVariablePermissionTests(HostOrServer hostOrServer)
            : base(hostOrServer)
        {
        }

        private GameObject m_TestObjPrefab;
        private ulong m_TestObjId = 0;

        protected override void OnServerAndClientsCreated()
        {
            m_TestObjPrefab = CreateNetworkObjectPrefab($"[{nameof(NetworkVariablePermissionTests)}.{nameof(m_TestObjPrefab)}]");
            var testComp = m_TestObjPrefab.AddComponent<NetVarPermTestComp>();
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {
            m_TestObjId = SpawnObject(m_TestObjPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>().NetworkObjectId;
            yield return null;
        }

        private IEnumerator WaitForPositionsAreEqual(NetworkVariable<Vector3> netvar, Vector3 expected)
        {
            yield return WaitForConditionOrTimeOut(() => netvar.Value == expected);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut);
        }

        private IEnumerator WaitForOwnerWritableAreEqualOnAll()
        {
            yield return WaitForConditionOrTimeOut(CheckOwnerWritableAreEqualOnAll);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut);
        }

        private bool CheckOwnerWritableAreEqualOnAll()
        {
            var testObjServer = m_ServerNetworkManager.SpawnManager.SpawnedObjects[m_TestObjId];
            var testCompServer = testObjServer.GetComponent<NetVarPermTestComp>();
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                var testObjClient = clientNetworkManager.SpawnManager.SpawnedObjects[m_TestObjId];
                var testCompClient = testObjClient.GetComponent<NetVarPermTestComp>();
                if (testObjServer.OwnerClientId != testObjClient.OwnerClientId ||
                    testCompServer.OwnerWritable_Position.Value != testCompClient.OwnerWritable_Position.Value ||
                    testCompServer.OwnerWritable_Position.ReadPerm != testCompClient.OwnerWritable_Position.ReadPerm ||
                    testCompServer.OwnerWritable_Position.WritePerm != testCompClient.OwnerWritable_Position.WritePerm)
                {
                    return false;
                }
            }
            return true;
        }

        private IEnumerator WaitForServerWritableAreEqualOnAll()
        {
            yield return WaitForConditionOrTimeOut(CheckServerWritableAreEqualOnAll);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut);
        }

        private bool CheckServerWritableAreEqualOnAll()
        {
            var testObjServer = m_ServerNetworkManager.SpawnManager.SpawnedObjects[m_TestObjId];
            var testCompServer = testObjServer.GetComponent<NetVarPermTestComp>();
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                var testObjClient = clientNetworkManager.SpawnManager.SpawnedObjects[m_TestObjId];
                var testCompClient = testObjClient.GetComponent<NetVarPermTestComp>();
                if (testCompServer.ServerWritable_Position.Value != testCompClient.ServerWritable_Position.Value ||
                    testCompServer.ServerWritable_Position.ReadPerm != testCompClient.ServerWritable_Position.ReadPerm ||
                    testCompServer.ServerWritable_Position.WritePerm != testCompClient.ServerWritable_Position.WritePerm)
                {
                    return false;
                }
            }
            return true;
        }

        private bool CheckOwnerReadWriteAreEqualOnOwnerAndServer()
        {
            var testObjServer = m_ServerNetworkManager.SpawnManager.SpawnedObjects[m_TestObjId];
            var testCompServer = testObjServer.GetComponent<NetVarPermTestComp>();
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                var testObjClient = clientNetworkManager.SpawnManager.SpawnedObjects[m_TestObjId];
                var testCompClient = testObjClient.GetComponent<NetVarPermTestComp>();
                if (testObjServer.OwnerClientId == testObjClient.OwnerClientId &&
                    testCompServer.OwnerReadWrite_Position.Value == testCompClient.ServerWritable_Position.Value &&
                    testCompServer.OwnerReadWrite_Position.ReadPerm == testCompClient.ServerWritable_Position.ReadPerm &&
                    testCompServer.OwnerReadWrite_Position.WritePerm == testCompClient.ServerWritable_Position.WritePerm)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckOwnerReadWriteAreNotEqualOnNonOwnerClients(NetVarPermTestComp ownerReadWriteObject)
        {
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                var testObjClient = clientNetworkManager.SpawnManager.SpawnedObjects[m_TestObjId];
                var testCompClient = testObjClient.GetComponent<NetVarPermTestComp>();
                if (testObjClient.OwnerClientId != ownerReadWriteObject.OwnerClientId ||
                    ownerReadWriteObject.OwnerReadWrite_Position.Value == testCompClient.ServerWritable_Position.Value ||
                    ownerReadWriteObject.OwnerReadWrite_Position.ReadPerm != testCompClient.ServerWritable_Position.ReadPerm ||
                    ownerReadWriteObject.OwnerReadWrite_Position.WritePerm != testCompClient.ServerWritable_Position.WritePerm)
                {
                    return false;
                }
            }
            return true;
        }

        [UnityTest]
        public IEnumerator ServerChangesOwnerWritableNetVar()
        {
            yield return WaitForOwnerWritableAreEqualOnAll();

            var testObjServer = m_ServerNetworkManager.SpawnManager.SpawnedObjects[m_TestObjId];
            var testCompServer = testObjServer.GetComponent<NetVarPermTestComp>();

            var oldValue = testCompServer.OwnerWritable_Position.Value;
            var newValue = oldValue + new Vector3(Random.Range(0, 100.0f), Random.Range(0, 100.0f), Random.Range(0, 100.0f));

            testCompServer.OwnerWritable_Position.Value = newValue;
            yield return WaitForPositionsAreEqual(testCompServer.OwnerWritable_Position, newValue);

            yield return WaitForOwnerWritableAreEqualOnAll();
        }

        [UnityTest]
        public IEnumerator ServerChangesServerWritableNetVar()
        {
            yield return WaitForServerWritableAreEqualOnAll();

            var testObjServer = m_ServerNetworkManager.SpawnManager.SpawnedObjects[m_TestObjId];
            var testCompServer = testObjServer.GetComponent<NetVarPermTestComp>();

            var oldValue = testCompServer.ServerWritable_Position.Value;
            var newValue = oldValue + new Vector3(Random.Range(0, 100.0f), Random.Range(0, 100.0f), Random.Range(0, 100.0f));

            testCompServer.ServerWritable_Position.Value = newValue;
            yield return WaitForPositionsAreEqual(testCompServer.ServerWritable_Position, newValue);

            yield return WaitForServerWritableAreEqualOnAll();
        }

        [UnityTest]
        public IEnumerator ClientChangesOwnerWritableNetVar()
        {
            yield return WaitForOwnerWritableAreEqualOnAll();

            var testObjServer = m_ServerNetworkManager.SpawnManager.SpawnedObjects[m_TestObjId];

            int clientManagerIndex = m_ClientNetworkManagers.Length - 1;
            var newOwnerClientId = m_ClientNetworkManagers[clientManagerIndex].LocalClientId;
            testObjServer.ChangeOwnership(newOwnerClientId);
            yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 2);

            yield return WaitForOwnerWritableAreEqualOnAll();

            var testObjClient = m_ClientNetworkManagers[clientManagerIndex].SpawnManager.SpawnedObjects[m_TestObjId];
            var testCompClient = testObjClient.GetComponent<NetVarPermTestComp>();

            var oldValue = testCompClient.OwnerWritable_Position.Value;
            var newValue = oldValue + new Vector3(Random.Range(0, 100.0f), Random.Range(0, 100.0f), Random.Range(0, 100.0f));

            testCompClient.OwnerWritable_Position.Value = newValue;
            yield return WaitForPositionsAreEqual(testCompClient.OwnerWritable_Position, newValue);

            yield return WaitForOwnerWritableAreEqualOnAll();
        }

        /// <summary>
        /// This tests the scenario where a client owner has both read and write
        /// permissions set. The server should be the only instance that can read
        /// the NetworkVariable.  ServerCannotChangeOwnerWritableNetVar performs
        /// the same check to make sure the server cannot write to a client owner
        /// NetworkVariable with owner write permissions.
        /// </summary>
        [UnityTest]
        public IEnumerator ClientOwnerWithReadWriteChangesNetVar()
        {
            yield return WaitForOwnerWritableAreEqualOnAll();

            var testObjServer = m_ServerNetworkManager.SpawnManager.SpawnedObjects[m_TestObjId];

            int clientManagerIndex = m_ClientNetworkManagers.Length - 1;
            var newOwnerClientId = m_ClientNetworkManagers[clientManagerIndex].LocalClientId;
            testObjServer.ChangeOwnership(newOwnerClientId);
            yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 2);

            yield return WaitForOwnerWritableAreEqualOnAll();

            var testObjClient = m_ClientNetworkManagers[clientManagerIndex].SpawnManager.SpawnedObjects[m_TestObjId];
            var testCompClient = testObjClient.GetComponent<NetVarPermTestComp>();

            var oldValue = testCompClient.OwnerReadWrite_Position.Value;
            var newValue = oldValue + new Vector3(Random.Range(0, 100.0f), Random.Range(0, 100.0f), Random.Range(0, 100.0f));

            testCompClient.OwnerWritable_Position.Value = newValue;
            yield return WaitForPositionsAreEqual(testCompClient.OwnerWritable_Position, newValue);

            // Verify the client owner and server match
            yield return CheckOwnerReadWriteAreEqualOnOwnerAndServer();

            // Verify the non-owner clients do not have the same Value but do have the same permissions
            yield return CheckOwnerReadWriteAreNotEqualOnNonOwnerClients(testCompClient);
        }


        [UnityTest]
        public IEnumerator ClientCannotChangeServerWritableNetVar()
        {
            yield return WaitForServerWritableAreEqualOnAll();

            var testObjServer = m_ServerNetworkManager.SpawnManager.SpawnedObjects[m_TestObjId];
            var testCompServer = testObjServer.GetComponent<NetVarPermTestComp>();

            int clientManagerIndex = m_ClientNetworkManagers.Length - 1;
            var newOwnerClientId = m_ClientNetworkManagers[clientManagerIndex].LocalClientId;
            testObjServer.ChangeOwnership(newOwnerClientId);
            yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 2);

            yield return WaitForServerWritableAreEqualOnAll();

            var testObjClient = m_ClientNetworkManagers[clientManagerIndex].SpawnManager.SpawnedObjects[m_TestObjId];
            var testCompClient = testObjClient.GetComponent<NetVarPermTestComp>();

            var oldValue = testCompClient.ServerWritable_Position.Value;
            var newValue = oldValue + new Vector3(Random.Range(0, 100.0f), Random.Range(0, 100.0f), Random.Range(0, 100.0f));

            Assert.That(() => testCompClient.ServerWritable_Position.Value = newValue, Throws.TypeOf<InvalidOperationException>());
            yield return WaitForPositionsAreEqual(testCompServer.ServerWritable_Position, oldValue);

            yield return WaitForServerWritableAreEqualOnAll();

            testCompServer.ServerWritable_Position.Value = newValue;
            yield return WaitForPositionsAreEqual(testCompServer.ServerWritable_Position, newValue);

            yield return WaitForServerWritableAreEqualOnAll();
        }

        [UnityTest]
        public IEnumerator ServerCannotChangeOwnerWritableNetVar()
        {
            yield return WaitForOwnerWritableAreEqualOnAll();

            var testObjServer = m_ServerNetworkManager.SpawnManager.SpawnedObjects[m_TestObjId];
            var testCompServer = testObjServer.GetComponent<NetVarPermTestComp>();

            int clientManagerIndex = m_ClientNetworkManagers.Length - 1;
            var newOwnerClientId = m_ClientNetworkManagers[clientManagerIndex].LocalClientId;
            testObjServer.ChangeOwnership(newOwnerClientId);
            yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 2);

            yield return WaitForOwnerWritableAreEqualOnAll();

            var oldValue = testCompServer.OwnerWritable_Position.Value;
            var newValue = oldValue + new Vector3(Random.Range(0, 100.0f), Random.Range(0, 100.0f), Random.Range(0, 100.0f));

            Assert.That(() => testCompServer.OwnerWritable_Position.Value = newValue, Throws.TypeOf<InvalidOperationException>());
            yield return WaitForPositionsAreEqual(testCompServer.OwnerWritable_Position, oldValue);

            yield return WaitForOwnerWritableAreEqualOnAll();

            var testObjClient = m_ClientNetworkManagers[clientManagerIndex].SpawnManager.SpawnedObjects[m_TestObjId];
            var testCompClient = testObjClient.GetComponent<NetVarPermTestComp>();

            testCompClient.OwnerWritable_Position.Value = newValue;
            yield return WaitForPositionsAreEqual(testCompClient.OwnerWritable_Position, newValue);

            yield return WaitForOwnerWritableAreEqualOnAll();
        }
    }

    public struct TestStruct : INetworkSerializable, IEquatable<TestStruct>
    {
        public uint SomeInt;
        public bool SomeBool;
        public static bool NetworkSerializeCalledOnWrite;
        public static bool NetworkSerializeCalledOnRead;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                NetworkSerializeCalledOnRead = true;
            }
            else
            {
                NetworkSerializeCalledOnWrite = true;
            }
            serializer.SerializeValue(ref SomeInt);
            serializer.SerializeValue(ref SomeBool);
        }

        public bool Equals(TestStruct other)
        {
            return SomeInt == other.SomeInt && SomeBool == other.SomeBool;
        }

        public override bool Equals(object obj)
        {
            return obj is TestStruct other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)SomeInt * 397) ^ SomeBool.GetHashCode();
            }
        }
    }

    public class TestClass : INetworkSerializable, IEquatable<TestClass>
    {
        public uint SomeInt;
        public bool SomeBool;
        public static bool NetworkSerializeCalledOnWrite;
        public static bool NetworkSerializeCalledOnRead;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                NetworkSerializeCalledOnRead = true;
            }
            else
            {
                NetworkSerializeCalledOnWrite = true;
            }
            serializer.SerializeValue(ref SomeInt);
            serializer.SerializeValue(ref SomeBool);
        }

        public bool Equals(TestClass other)
        {
            return SomeInt == other.SomeInt && SomeBool == other.SomeBool;
        }

        public override bool Equals(object obj)
        {
            return obj is TestClass other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)SomeInt * 397) ^ SomeBool.GetHashCode();
            }
        }
    }

    // Used just to create a NetworkVariable in the templated NetworkBehaviour type that isn't referenced anywhere else
    // Please do not reference this class anywhere else!
    public class TestClass_ReferencedOnlyByTemplateNetworkBehavourType : TestClass
    {

    }

    public class NetworkVariableTest : NetworkBehaviour
    {
        public enum SomeEnum
        {
            A,
            B,
            C
        }
        public readonly NetworkVariable<int> TheScalar = new NetworkVariable<int>();
        public readonly NetworkVariable<SomeEnum> TheEnum = new NetworkVariable<SomeEnum>();
        public readonly NetworkList<int> TheList = new NetworkList<int>();
        public readonly NetworkList<StructUsedOnlyInNetworkList> TheStructList = new NetworkList<StructUsedOnlyInNetworkList>();
        public readonly NetworkList<FixedString128Bytes> TheLargeList = new NetworkList<FixedString128Bytes>();

        public readonly NetworkVariable<FixedString32Bytes> FixedString32 = new NetworkVariable<FixedString32Bytes>();

        private void ListChanged(NetworkListEvent<int> e)
        {
            ListDelegateTriggered = true;
        }

        public void Awake()
        {
            TheList.OnListChanged += ListChanged;
        }

        public readonly NetworkVariable<TestStruct> TheStruct = new NetworkVariable<TestStruct>();
        public readonly NetworkVariable<TestClass> TheClass = new NetworkVariable<TestClass>();

        public NetworkVariable<UnmanagedTemplateNetworkSerializableType<TestStruct>> TheTemplateStruct = new NetworkVariable<UnmanagedTemplateNetworkSerializableType<TestStruct>>();
        public NetworkVariable<ManagedTemplateNetworkSerializableType<TestClass>> TheTemplateClass = new NetworkVariable<ManagedTemplateNetworkSerializableType<TestClass>>();

        public bool ListDelegateTriggered;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                NetworkVariableTests.ClientNetworkVariableTestSpawned(this);
            }
            base.OnNetworkSpawn();
        }
    }

    [TestFixture(true)]
    [TestFixture(false)]
    public class NetworkVariableTests : NetcodeIntegrationTest
    {
        private const string k_StringTestValue = "abcdefghijklmnopqrstuvwxyz";
        private static readonly FixedString32Bytes k_FixedStringTestValue = k_StringTestValue;
        protected override int NumberOfClients => 2;

        private const uint k_TestUInt = 0x12345678;

        private const int k_TestVal1 = 111;
        private const int k_TestVal2 = 222;
        private const int k_TestVal3 = 333;

        private static List<NetworkVariableTest> s_ClientNetworkVariableTestInstances = new List<NetworkVariableTest>();
        public static void ClientNetworkVariableTestSpawned(NetworkVariableTest networkVariableTest)
        {
            s_ClientNetworkVariableTestInstances.Add(networkVariableTest);
        }

        // Player1 component on the server
        private NetworkVariableTest m_Player1OnServer;

        // Player1 component on client1
        private NetworkVariableTest m_Player1OnClient1;

        private NetworkListTestPredicate m_NetworkListPredicateHandler;

        private readonly bool m_EnsureLengthSafety;

        public NetworkVariableTests(bool ensureLengthSafety)
        {
            m_EnsureLengthSafety = ensureLengthSafety;
        }

        protected override bool CanStartServerAndClients()
        {
            return false;
        }

        /// <summary>
        /// This is an adjustment to how the server and clients are started in order
        /// to avoid timing issues when running in a stand alone test runner build.
        /// </summary>
        private IEnumerator InitializeServerAndClients(bool useHost)
        {
            s_ClientNetworkVariableTestInstances.Clear();
            m_PlayerPrefab.AddComponent<NetworkVariableTest>();

            m_PlayerPrefab.AddComponent<ClassHavingNetworkBehaviour>();
            m_PlayerPrefab.AddComponent<ClassHavingNetworkBehaviour2>();
            m_PlayerPrefab.AddComponent<StructHavingNetworkBehaviour>();

            m_ServerNetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety = m_EnsureLengthSafety;
            m_ServerNetworkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.EnsureNetworkVariableLengthSafety = m_EnsureLengthSafety;
                client.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            }

            Assert.True(NetcodeIntegrationTestHelpers.Start(useHost, m_ServerNetworkManager, m_ClientNetworkManagers), "Failed to start server and client instances");

            RegisterSceneManagerHandler();

            // Wait for connection on client and server side
            yield return WaitForClientsConnectedOrTimeOut();
            AssertOnTimeout($"Timed-out waiting for all clients to connect!");

            // These are the *SERVER VERSIONS* of the *CLIENT PLAYER 1 & 2*
            var result = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();

            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId,
                m_ServerNetworkManager, result);

            // Assign server-side client's player
            m_Player1OnServer = result.Result.GetComponent<NetworkVariableTest>();

            // This is client1's view of itself
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId,
                m_ClientNetworkManagers[0], result);

            // Assign client-side local player
            m_Player1OnClient1 = result.Result.GetComponent<NetworkVariableTest>();

            m_Player1OnServer.TheList.Clear();

            if (m_Player1OnServer.TheList.Count > 0)
            {
                throw new Exception("at least one server network container not empty at start");
            }
            if (m_Player1OnClient1.TheList.Count > 0)
            {
                throw new Exception("at least one client network container not empty at start");
            }

            var instanceCount = useHost ? NumberOfClients * 3 : NumberOfClients * 2;
            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(() => s_ClientNetworkVariableTestInstances.Count == instanceCount);

            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for all client NetworkVariableTest instances to register they have spawned!");

            yield return s_DefaultWaitForTick;
        }

        /// <summary>
        /// Runs generalized tests on all predefined NetworkVariable types
        /// </summary>
        [UnityTest]
        public IEnumerator AllNetworkVariableTypes([Values(true, false)] bool useHost)
        {
            // Create, instantiate, and host
            // This would normally go in Setup, but since every other test but this one
            //  uses NetworkManagerHelper, and it does its own NetworkManager setup / teardown,
            //  for now we put this within this one test until we migrate it to MIH
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager(out NetworkManager server, useHost ? NetworkManagerHelper.NetworkManagerOperatingMode.Host : NetworkManagerHelper.NetworkManagerOperatingMode.Server));

            Assert.IsTrue(server.IsHost == useHost, $"{nameof(useHost)} does not match the server.IsHost value!");

            Guid gameObjectId = NetworkManagerHelper.AddGameNetworkObject("NetworkVariableTestComponent");

            var networkVariableTestComponent = NetworkManagerHelper.AddComponentToObject<NetworkVariableTestComponent>(gameObjectId);

            NetworkManagerHelper.SpawnNetworkObject(gameObjectId);

            // Start Testing
            networkVariableTestComponent.EnableTesting = true;

            yield return WaitForConditionOrTimeOut(() => true == networkVariableTestComponent.IsTestComplete());
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for the test to complete!");

            // Stop Testing
            networkVariableTestComponent.EnableTesting = false;

            Assert.IsTrue(networkVariableTestComponent.DidAllValuesChange());
            networkVariableTestComponent.AssertAllValuesAreCorrect();

            // Disable this once we are done.
            networkVariableTestComponent.gameObject.SetActive(false);

            // This would normally go in Teardown, but since every other test but this one
            //  uses NetworkManagerHelper, and it does its own NetworkManager setup / teardown,
            //  for now we put this within this one test until we migrate it to MIH
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        [UnityTest]
        public IEnumerator ClientWritePermissionTest([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            // client must not be allowed to write to a server auth variable
            Assert.Throws<InvalidOperationException>(() => m_Player1OnClient1.TheScalar.Value = k_TestVal1);
        }

        /// <summary>
        /// Runs tests that network variables sync on client whatever the local value of <see cref="Time.timeScale"/>.
        /// </summary>
        [UnityTest]
        public IEnumerator NetworkVariableSync_WithDifferentTimeScale([Values(true, false)] bool useHost, [Values(0.0f, 1.0f, 2.0f)] float timeScale)
        {
            Time.timeScale = timeScale;

            yield return InitializeServerAndClients(useHost);

            m_Player1OnServer.TheScalar.Value = k_TestVal1;

            // Now wait for the client side version to be updated to k_TestVal1
            yield return WaitForConditionOrTimeOut(() => m_Player1OnClient1.TheScalar.Value == k_TestVal1);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for client-side NetworkVariable to update!");
        }

        [UnityTest]
        public IEnumerator FixedString32Test([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            m_Player1OnServer.FixedString32.Value = k_FixedStringTestValue;

            // Now wait for the client side version to be updated to k_FixedStringTestValue
            yield return WaitForConditionOrTimeOut(() => m_Player1OnClient1.FixedString32.Value == k_FixedStringTestValue);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for client-side NetworkVariable to update!");
        }

        [UnityTest]
        public IEnumerator NetworkListAdd([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            m_NetworkListPredicateHandler = new NetworkListTestPredicate(m_Player1OnServer, m_Player1OnClient1, NetworkListTestPredicate.NetworkListTestStates.Add, 10);
            yield return WaitForConditionOrTimeOut(m_NetworkListPredicateHandler);
        }

        [UnityTest]
        public IEnumerator WhenListContainsManyLargeValues_OverflowExceptionIsNotThrown([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            m_NetworkListPredicateHandler = new NetworkListTestPredicate(m_Player1OnServer, m_Player1OnClient1, NetworkListTestPredicate.NetworkListTestStates.ContainsLarge, 20);
            yield return WaitForConditionOrTimeOut(m_NetworkListPredicateHandler);
        }

        [UnityTest]
        public IEnumerator NetworkListContains([Values(true, false)] bool useHost)
        {
            // Re-use the NetworkListAdd to initialize the server and client as well as make sure the list is populated
            yield return NetworkListAdd(useHost);

            // Now test the NetworkList.Contains method
            m_NetworkListPredicateHandler.SetNetworkListTestState(NetworkListTestPredicate.NetworkListTestStates.Contains);
            yield return WaitForConditionOrTimeOut(m_NetworkListPredicateHandler);
        }

        [UnityTest]
        public IEnumerator NetworkListRemove([Values(true, false)] bool useHost)
        {
            // Re-use the NetworkListAdd to initialize the server and client as well as make sure the list is populated
            yield return NetworkListAdd(useHost);

            // Remove two entries by index
            m_Player1OnServer.TheList.Remove(3);
            m_Player1OnServer.TheList.Remove(5);

            // Really just verifies the data at this point
            m_NetworkListPredicateHandler.SetNetworkListTestState(NetworkListTestPredicate.NetworkListTestStates.VerifyData);
            yield return WaitForConditionOrTimeOut(m_NetworkListPredicateHandler);
        }

        [UnityTest]
        public IEnumerator NetworkListInsert([Values(true, false)] bool useHost)
        {
            // Re-use the NetworkListAdd to initialize the server and client as well as make sure the list is populated
            yield return NetworkListAdd(useHost);

            // Now randomly insert a random value entry
            m_Player1OnServer.TheList.Insert(Random.Range(0, 9), Random.Range(1, 99));

            // Verify the element count and values on the client matches the server
            m_NetworkListPredicateHandler.SetNetworkListTestState(NetworkListTestPredicate.NetworkListTestStates.VerifyData);
            yield return WaitForConditionOrTimeOut(m_NetworkListPredicateHandler);
        }

        [UnityTest]
        public IEnumerator NetworkListIndexOf([Values(true, false)] bool useHost)
        {
            // Re-use the NetworkListAdd to initialize the server and client as well as make sure the list is populated
            yield return NetworkListAdd(useHost);

            m_NetworkListPredicateHandler.SetNetworkListTestState(NetworkListTestPredicate.NetworkListTestStates.IndexOf);
            yield return WaitForConditionOrTimeOut(m_NetworkListPredicateHandler);
        }

        [UnityTest]
        public IEnumerator NetworkListValueUpdate([Values(true, false)] bool useHost)
        {
            var testSucceeded = false;
            yield return InitializeServerAndClients(useHost);
            // Add 1 element value and verify it is the same on the client
            m_NetworkListPredicateHandler = new NetworkListTestPredicate(m_Player1OnServer, m_Player1OnClient1, NetworkListTestPredicate.NetworkListTestStates.Add, 1);
            yield return WaitForConditionOrTimeOut(m_NetworkListPredicateHandler);

            // Setup our original and
            var previousValue = m_Player1OnServer.TheList[0];
            var updatedValue = previousValue + 10;

            // Callback that verifies the changed event occurred and that the original and new values are correct
            void TestValueUpdatedCallback(NetworkListEvent<int> changedEvent)
            {
                testSucceeded = changedEvent.PreviousValue == previousValue &&
                                changedEvent.Value == updatedValue;
            }

            // Subscribe to the OnListChanged event on the client side and
            m_Player1OnClient1.TheList.OnListChanged += TestValueUpdatedCallback;
            m_Player1OnServer.TheList[0] = updatedValue;

            // Wait until we know the client side matches the server side before checking if the callback was a success
            m_NetworkListPredicateHandler.SetNetworkListTestState(NetworkListTestPredicate.NetworkListTestStates.VerifyData);
            yield return WaitForConditionOrTimeOut(m_NetworkListPredicateHandler);

            Assert.That(testSucceeded);
            m_Player1OnClient1.TheList.OnListChanged -= TestValueUpdatedCallback;
        }

        [UnityTest]
        public IEnumerator NetworkListRemoveAt([Values(true, false)] bool useHost)
        {
            // Re-use the NetworkListAdd to initialize the server and client as well as make sure the list is populated
            yield return NetworkListAdd(useHost);

            // Randomly remove a few entries
            m_Player1OnServer.TheList.RemoveAt(Random.Range(0, m_Player1OnServer.TheList.Count - 1));
            m_Player1OnServer.TheList.RemoveAt(Random.Range(0, m_Player1OnServer.TheList.Count - 1));
            m_Player1OnServer.TheList.RemoveAt(Random.Range(0, m_Player1OnServer.TheList.Count - 1));

            // Verify the element count and values on the client matches the server
            m_NetworkListPredicateHandler.SetNetworkListTestState(NetworkListTestPredicate.NetworkListTestStates.VerifyData);
            yield return WaitForConditionOrTimeOut(m_NetworkListPredicateHandler);
        }

        [UnityTest]
        public IEnumerator NetworkListClear([Values(true, false)] bool useHost)
        {
            // Re-use the NetworkListAdd to initialize the server and client as well as make sure the list is populated
            yield return NetworkListAdd(useHost);
            m_Player1OnServer.TheList.Clear();
            // Verify the element count and values on the client matches the server
            m_NetworkListPredicateHandler.SetNetworkListTestState(NetworkListTestPredicate.NetworkListTestStates.VerifyData);
            yield return WaitForConditionOrTimeOut(m_NetworkListPredicateHandler);
        }

        [UnityTest]
        public IEnumerator TestNetworkVariableClass([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            bool VerifyClass()
            {
                return m_Player1OnClient1.TheClass.Value != null &&
                       m_Player1OnClient1.TheClass.Value.SomeBool == m_Player1OnServer.TheClass.Value.SomeBool &&
                       m_Player1OnClient1.TheClass.Value.SomeInt == m_Player1OnServer.TheClass.Value.SomeInt;
            }

            m_Player1OnServer.TheClass.Value = new TestClass { SomeInt = k_TestUInt, SomeBool = false };
            m_Player1OnServer.TheClass.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(VerifyClass);
        }

        [UnityTest]
        public IEnumerator TestNetworkVariableTemplateClass([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            bool VerifyClass()
            {
                return m_Player1OnClient1.TheTemplateClass.Value.Value != null && m_Player1OnClient1.TheTemplateClass.Value.Value.SomeBool == m_Player1OnServer.TheTemplateClass.Value.Value.SomeBool &&
                       m_Player1OnClient1.TheTemplateClass.Value.Value.SomeInt == m_Player1OnServer.TheTemplateClass.Value.Value.SomeInt;
            }

            m_Player1OnServer.TheTemplateClass.Value = new ManagedTemplateNetworkSerializableType<TestClass> { Value = new TestClass { SomeInt = k_TestUInt, SomeBool = false } };
            m_Player1OnServer.TheTemplateClass.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(VerifyClass);
        }

        [UnityTest]
        public IEnumerator TestNetworkListStruct([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            bool VerifyList()
            {
                return m_Player1OnClient1.TheStructList.Count == m_Player1OnServer.TheStructList.Count &&
                       m_Player1OnClient1.TheStructList[0].Value == m_Player1OnServer.TheStructList[0].Value &&
                       m_Player1OnClient1.TheStructList[1].Value == m_Player1OnServer.TheStructList[1].Value;
            }

            m_Player1OnServer.TheStructList.Add(new StructUsedOnlyInNetworkList { Value = 1 });
            m_Player1OnServer.TheStructList.Add(new StructUsedOnlyInNetworkList { Value = 2 });
            m_Player1OnServer.TheStructList.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(VerifyList);
        }

        [UnityTest]
        public IEnumerator TestNetworkVariableStruct([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            bool VerifyStructure()
            {
                return m_Player1OnClient1.TheStruct.Value.SomeBool == m_Player1OnServer.TheStruct.Value.SomeBool &&
                       m_Player1OnClient1.TheStruct.Value.SomeInt == m_Player1OnServer.TheStruct.Value.SomeInt;
            }

            m_Player1OnServer.TheStruct.Value = new TestStruct { SomeInt = k_TestUInt, SomeBool = false };
            m_Player1OnServer.TheStruct.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(VerifyStructure);
        }

        [UnityTest]
        public IEnumerator TestNetworkVariableTemplateStruct([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            bool VerifyStructure()
            {
                return m_Player1OnClient1.TheTemplateStruct.Value.Value.SomeBool == m_Player1OnServer.TheTemplateStruct.Value.Value.SomeBool &&
                       m_Player1OnClient1.TheTemplateStruct.Value.Value.SomeInt == m_Player1OnServer.TheTemplateStruct.Value.Value.SomeInt;
            }

            m_Player1OnServer.TheTemplateStruct.Value = new UnmanagedTemplateNetworkSerializableType<TestStruct> { Value = new TestStruct { SomeInt = k_TestUInt, SomeBool = false } };
            m_Player1OnServer.TheTemplateStruct.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(VerifyStructure);
        }

        [UnityTest]
        public IEnumerator TestNetworkVariableTemplateBehaviourClass([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            bool VerifyClass()
            {
                return m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour>().TheVar.Value != null && m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour>().TheVar.Value.SomeBool == m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour>().TheVar.Value.SomeBool &&
                       m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour>().TheVar.Value.SomeInt == m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour>().TheVar.Value.SomeInt;
            }

            m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour>().TheVar.Value = new TestClass { SomeInt = k_TestUInt, SomeBool = false };
            m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour>().TheVar.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(VerifyClass);
        }

        [UnityTest]
        public IEnumerator TestNetworkVariableTemplateBehaviourClassNotReferencedElsewhere([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            bool VerifyClass()
            {
                return m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.Value != null && m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.Value.SomeBool == m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.Value.SomeBool &&
                       m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.Value.SomeInt == m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.Value.SomeInt;
            }

            m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.Value = new TestClass_ReferencedOnlyByTemplateNetworkBehavourType { SomeInt = k_TestUInt, SomeBool = false };
            m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(VerifyClass);
        }

        [UnityTest]
        public IEnumerator TestNetworkVariableTemplateBehaviourStruct([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            bool VerifyClass()
            {
                return m_Player1OnClient1.GetComponent<StructHavingNetworkBehaviour>().TheVar.Value.SomeBool == m_Player1OnServer.GetComponent<StructHavingNetworkBehaviour>().TheVar.Value.SomeBool &&
                       m_Player1OnClient1.GetComponent<StructHavingNetworkBehaviour>().TheVar.Value.SomeInt == m_Player1OnServer.GetComponent<StructHavingNetworkBehaviour>().TheVar.Value.SomeInt;
            }

            m_Player1OnServer.GetComponent<StructHavingNetworkBehaviour>().TheVar.Value = new TestStruct { SomeInt = k_TestUInt, SomeBool = false };
            m_Player1OnServer.GetComponent<StructHavingNetworkBehaviour>().TheVar.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(VerifyClass);
        }

        [UnityTest]
        public IEnumerator TestNetworkVariableEnum([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            bool VerifyStructure()
            {
                return m_Player1OnClient1.TheEnum.Value == NetworkVariableTest.SomeEnum.C;
            }

            m_Player1OnServer.TheEnum.Value = NetworkVariableTest.SomeEnum.C;
            m_Player1OnServer.TheEnum.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(VerifyStructure);
        }

        [UnityTest]
        public IEnumerator TestINetworkSerializableClassCallsNetworkSerialize([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            TestClass.NetworkSerializeCalledOnWrite = false;
            TestClass.NetworkSerializeCalledOnRead = false;
            m_Player1OnServer.TheClass.Value = new TestClass
            {
                SomeBool = true,
                SomeInt = 32
            };

            static bool VerifyCallback() => TestClass.NetworkSerializeCalledOnWrite && TestClass.NetworkSerializeCalledOnRead;

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(VerifyCallback);
        }

        [UnityTest]
        public IEnumerator TestINetworkSerializableStructCallsNetworkSerialize([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            TestStruct.NetworkSerializeCalledOnWrite = false;
            TestStruct.NetworkSerializeCalledOnRead = false;
            m_Player1OnServer.TheStruct.Value = new TestStruct() { SomeInt = k_TestUInt, SomeBool = false };

            static bool VerifyCallback() => TestStruct.NetworkSerializeCalledOnWrite && TestStruct.NetworkSerializeCalledOnRead;

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(VerifyCallback);
        }

        #region COULD_BE_REMOVED
        [UnityTest]
        [Ignore("This is used several times already in the NetworkListPredicate")]
        // TODO: If we end up using the new suggested pattern, then delete this
        public IEnumerator NetworkListArrayOperator([Values(true, false)] bool useHost)
        {
            yield return NetworkListAdd(useHost);
        }

        [UnityTest]
        [Ignore("This is used several times already in the NetworkListPredicate")]
        // TODO: If we end up using the new suggested pattern, then delete this
        public IEnumerator NetworkListIEnumerator([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            var correctVals = new int[3];
            correctVals[0] = k_TestVal1;
            correctVals[1] = k_TestVal2;
            correctVals[2] = k_TestVal3;

            m_Player1OnServer.TheList.Add(correctVals[0]);
            m_Player1OnServer.TheList.Add(correctVals[1]);
            m_Player1OnServer.TheList.Add(correctVals[2]);

            Assert.IsTrue(m_Player1OnServer.TheList.Count == 3);

            int index = 0;
            foreach (var val in m_Player1OnServer.TheList)
            {
                if (val != correctVals[index++])
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void TestUnsupportedManagedTypesThrowExceptions()
        {
            var variable = new NetworkVariable<string>();
            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            using var reader = new FastBufferReader(writer, Allocator.None);
            // Just making sure these are null, just in case.
            UserNetworkVariableSerialization<string>.ReadValue = null;
            UserNetworkVariableSerialization<string>.WriteValue = null;
            Assert.Throws<ArgumentException>(() =>
            {
                variable.WriteField(writer);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                variable.ReadField(reader);
            });
        }

        [Test]
        public void TestUnsupportedManagedTypesWithUserSerializationDoNotThrowExceptions()
        {
            var variable = new NetworkVariable<string>();
            UserNetworkVariableSerialization<string>.ReadValue = (FastBufferReader reader, out string value) =>
            {
                reader.ReadValueSafe(out value);
            };
            UserNetworkVariableSerialization<string>.WriteValue = (FastBufferWriter writer, in string value) =>
            {
                writer.WriteValueSafe(value);
            };
            try
            {
                using var writer = new FastBufferWriter(1024, Allocator.Temp);
                variable.Value = "012345";
                variable.WriteField(writer);
                variable.Value = "";

                using var reader = new FastBufferReader(writer, Allocator.None);
                variable.ReadField(reader);
                Assert.AreEqual("012345", variable.Value);
            }
            finally
            {
                UserNetworkVariableSerialization<string>.ReadValue = null;
                UserNetworkVariableSerialization<string>.WriteValue = null;
            }
        }

        [Test]
        public void TestUnsupportedUnmanagedTypesThrowExceptions()
        {
            var variable = new NetworkVariable<Guid>();
            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            using var reader = new FastBufferReader(writer, Allocator.None);
            // Just making sure these are null, just in case.
            UserNetworkVariableSerialization<Guid>.ReadValue = null;
            UserNetworkVariableSerialization<Guid>.WriteValue = null;
            Assert.Throws<ArgumentException>(() =>
            {
                variable.WriteField(writer);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                variable.ReadField(reader);
            });
        }

        [Test]
        public void TestUnsupportedUnmanagedTypesWithUserSerializationDoNotThrowExceptions()
        {
            var variable = new NetworkVariable<Guid>();
            UserNetworkVariableSerialization<Guid>.ReadValue = (FastBufferReader reader, out Guid value) =>
            {
                var tmpValue = new ForceNetworkSerializeByMemcpy<Guid>();
                reader.ReadValueSafe(out tmpValue);
                value = tmpValue.Value;
            };
            UserNetworkVariableSerialization<Guid>.WriteValue = (FastBufferWriter writer, in Guid value) =>
            {
                var tmpValue = new ForceNetworkSerializeByMemcpy<Guid>(value);
                writer.WriteValueSafe(tmpValue);
            };
            try
            {
                using var writer = new FastBufferWriter(1024, Allocator.Temp);
                var guid = Guid.NewGuid();
                variable.Value = guid;
                variable.WriteField(writer);
                variable.Value = Guid.Empty;

                using var reader = new FastBufferReader(writer, Allocator.None);
                variable.ReadField(reader);
                Assert.AreEqual(guid, variable.Value);
            }
            finally
            {
                UserNetworkVariableSerialization<Guid>.ReadValue = null;
                UserNetworkVariableSerialization<Guid>.WriteValue = null;
            }
        }

        [Test]
        public void TestManagedINetworkSerializableNetworkVariablesDeserializeInPlace()
        {
            var variable = new NetworkVariable<ManagedNetworkSerializableType>();
            variable.Value = new ManagedNetworkSerializableType
            {
                InMemoryValue = 1,
                Ints = new[] { 2, 3, 4 },
                Str = "five"
            };

            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            variable.WriteField(writer);
            Assert.AreEqual(1, variable.Value.InMemoryValue);
            Assert.AreEqual(new[] { 2, 3, 4 }, variable.Value.Ints);
            Assert.AreEqual("five", variable.Value.Str);
            variable.Value = new ManagedNetworkSerializableType
            {
                InMemoryValue = 10,
                Ints = new[] { 20, 30, 40, 50 },
                Str = "sixty"
            };

            using var reader = new FastBufferReader(writer, Allocator.None);
            variable.ReadField(reader);
            Assert.AreEqual(10, variable.Value.InMemoryValue, "In-memory value was not the same - in-place deserialization should not change this");
            Assert.AreEqual(new[] { 2, 3, 4 }, variable.Value.Ints, "Ints were not correctly deserialized");
            Assert.AreEqual("five", variable.Value.Str, "Str was not correctly deserialized");
        }

        [Test]
        public void TestUnmnagedINetworkSerializableNetworkVariablesDeserializeInPlace()
        {
            var variable = new NetworkVariable<UnmanagedNetworkSerializableType>();
            variable.Value = new UnmanagedNetworkSerializableType
            {
                InMemoryValue = 1,
                Int = 2,
                Str = "three"
            };
            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            variable.WriteField(writer);
            Assert.AreEqual(1, variable.Value.InMemoryValue);
            Assert.AreEqual(2, variable.Value.Int);
            Assert.AreEqual("three", variable.Value.Str);
            variable.Value = new UnmanagedNetworkSerializableType
            {
                InMemoryValue = 10,
                Int = 20,
                Str = "thirty"
            };

            using var reader = new FastBufferReader(writer, Allocator.None);
            variable.ReadField(reader);
            Assert.AreEqual(10, variable.Value.InMemoryValue, "In-memory value was not the same - in-place deserialization should not change this");
            Assert.AreEqual(2, variable.Value.Int, "Int was not correctly deserialized");
            Assert.AreEqual("three", variable.Value.Str, "Str was not correctly deserialized");
        }
        #endregion

        private float m_OriginalTimeScale = 1.0f;

        protected override IEnumerator OnSetup()
        {
            m_OriginalTimeScale = Time.timeScale;
            yield return null;
        }

        protected override IEnumerator OnTearDown()
        {
            Time.timeScale = m_OriginalTimeScale;

            m_NetworkListPredicateHandler = null;
            yield return base.OnTearDown();
        }
    }

    /// <summary>
    /// Handles the more generic conditional logic for NetworkList tests
    /// which can be used with the <see cref="NetcodeIntegrationTest.WaitForConditionOrTimeOut"/>
    /// that accepts anything derived from the <see cref="ConditionalPredicateBase"/> class
    /// as a parameter.
    /// </summary>
    public class NetworkListTestPredicate : ConditionalPredicateBase
    {
        private const int k_MaxRandomValue = 1000;

        private Dictionary<NetworkListTestStates, Func<bool>> m_StateFunctions;

        // Player1 component on the Server
        private NetworkVariableTest m_Player1OnServer;

        // Player1 component on client1
        private NetworkVariableTest m_Player1OnClient1;

        private string m_TestStageFailedMessage;

        public enum NetworkListTestStates
        {
            Add,
            ContainsLarge,
            Contains,
            VerifyData,
            IndexOf,
        }

        private NetworkListTestStates m_NetworkListTestState;

        public void SetNetworkListTestState(NetworkListTestStates networkListTestState)
        {
            m_NetworkListTestState = networkListTestState;
        }

        /// <summary>
        /// Determines if the condition has been reached for the current NetworkListTestState
        /// </summary>
        protected override bool OnHasConditionBeenReached()
        {
            var isStateRegistered = m_StateFunctions.ContainsKey(m_NetworkListTestState);
            Assert.IsTrue(isStateRegistered);
            return m_StateFunctions[m_NetworkListTestState].Invoke();
        }

        /// <summary>
        /// Provides all information about the players for both sides for simplicity and informative sake.
        /// </summary>
        /// <returns></returns>
        private string ConditionFailedInfo()
        {
            return $"{m_NetworkListTestState} condition test failed:\n Server List Count: {m_Player1OnServer.TheList.Count} vs  Client List Count: {m_Player1OnClient1.TheList.Count}\n" +
                $"Server List Count: {m_Player1OnServer.TheLargeList.Count} vs  Client List Count: {m_Player1OnClient1.TheLargeList.Count}\n" +
                $"Server Delegate Triggered: {m_Player1OnServer.ListDelegateTriggered} | Client Delegate Triggered: {m_Player1OnClient1.ListDelegateTriggered}\n";
        }

        /// <summary>
        /// When finished, check if a time out occurred and if so assert and provide meaningful information to troubleshoot why
        /// </summary>
        protected override void OnFinished()
        {
            Assert.IsFalse(TimedOut, $"{nameof(NetworkListTestPredicate)} timed out waiting for the {m_NetworkListTestState} condition to be reached! \n" + ConditionFailedInfo());
        }

        // Uses the ArrayOperator and validates that on both sides the count and values are the same
        private bool OnVerifyData()
        {
            // Wait until both sides have the same number of elements
            if (m_Player1OnServer.TheList.Count != m_Player1OnClient1.TheList.Count)
            {
                return false;
            }

            // Check the client values against the server values to make sure they match
            for (int i = 0; i < m_Player1OnServer.TheList.Count; i++)
            {
                if (m_Player1OnServer.TheList[i] != m_Player1OnClient1.TheList[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Verifies the data count, values, and that the ListDelegate on both sides was triggered
        /// </summary>
        private bool OnAdd()
        {
            bool wasTriggerred = m_Player1OnServer.ListDelegateTriggered && m_Player1OnClient1.ListDelegateTriggered;
            return wasTriggerred && OnVerifyData();
        }

        /// <summary>
        /// The current version of this test only verified the count of the large list, so that is what this does
        /// </summary>
        private bool OnContainsLarge()
        {
            return m_Player1OnServer.TheLargeList.Count == m_Player1OnClient1.TheLargeList.Count;
        }

        /// <summary>
        /// Tests NetworkList.Contains which also verifies all values are the same on both sides
        /// </summary>
        private bool OnContains()
        {
            // Wait until both sides have the same number of elements
            if (m_Player1OnServer.TheList.Count != m_Player1OnClient1.TheList.Count)
            {
                return false;
            }

            // Parse through all server values and use the NetworkList.Contains method to check if the value is in the list on the client side
            foreach (var serverValue in m_Player1OnServer.TheList)
            {
                if (!m_Player1OnClient1.TheList.Contains(serverValue))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Tests NetworkList.IndexOf and verifies that all values are aligned on both sides
        /// </summary>
        private bool OnIndexOf()
        {
            foreach (var serverSideValue in m_Player1OnServer.TheList)
            {
                var indexToTest = m_Player1OnServer.TheList.IndexOf(serverSideValue);
                if (indexToTest != m_Player1OnServer.TheList.IndexOf(serverSideValue))
                {
                    return false;
                }
            }
            return true;
        }

        public NetworkListTestPredicate(NetworkVariableTest player1OnServer, NetworkVariableTest player1OnClient1, NetworkListTestStates networkListTestState, int elementCount)
        {
            m_NetworkListTestState = networkListTestState;
            m_Player1OnServer = player1OnServer;
            m_Player1OnClient1 = player1OnClient1;
            m_StateFunctions = new Dictionary<NetworkListTestStates, Func<bool>>
            {
                { NetworkListTestStates.Add, OnAdd },
                { NetworkListTestStates.ContainsLarge, OnContainsLarge },
                { NetworkListTestStates.Contains, OnContains },
                { NetworkListTestStates.VerifyData, OnVerifyData },
                { NetworkListTestStates.IndexOf, OnIndexOf }
            };

            if (networkListTestState == NetworkListTestStates.ContainsLarge)
            {
                for (var i = 0; i < elementCount; ++i)
                {
                    m_Player1OnServer.TheLargeList.Add(new FixedString128Bytes());
                }
            }
            else
            {
                for (int i = 0; i < elementCount; i++)
                {
                    m_Player1OnServer.TheList.Add(Random.Range(0, k_MaxRandomValue));
                }
            }
        }
    }
}
