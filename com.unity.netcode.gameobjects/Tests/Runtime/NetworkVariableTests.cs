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
    
    public enum ByteEnum : byte
    {
        A,
        B,
        C = byte.MaxValue
    }
    public enum SByteEnum : sbyte
    {
        A,
        B,
        C = sbyte.MaxValue
    }
    public enum ShortEnum : short
    {
        A,
        B,
        C = short.MaxValue
    }
    public enum UShortEnum : ushort
    {
        A,
        B,
        C = ushort.MaxValue
    }
    public enum IntEnum : int
    {
        A,
        B,
        C = int.MaxValue
    }
    public enum UIntEnum : uint
    {
        A,
        B,
        C = uint.MaxValue
    }
    public enum LongEnum : long
    {
        A,
        B,
        C = long.MaxValue
    }
    public  enum ULongEnum : ulong
    {
        A,
        B,
        C = ulong.MaxValue
    }

    public struct NetworkVariableTestStruct : INetworkSerializeByMemcpy
    {
        public byte A;
        public short B;
        public ushort C;
        public int D;
        public uint E;
        public long F;
        public ulong G;
        public bool H;
        public char I;
        public float J;
        public double K;

        private static System.Random s_Random = new System.Random();

        public static NetworkVariableTestStruct GetTestStruct()
        {
            var testStruct = new NetworkVariableTestStruct
            {
                A = (byte)s_Random.Next(),
                B = (short)s_Random.Next(),
                C = (ushort)s_Random.Next(),
                D = (int)s_Random.Next(),
                E = (uint)s_Random.Next(),
                F = ((long)s_Random.Next() << 32) + s_Random.Next(),
                G = ((ulong)s_Random.Next() << 32) + (ulong)s_Random.Next(),
                H = true,
                I = '\u263a',
                J = (float)s_Random.NextDouble(),
                K = s_Random.NextDouble(),
            };

            return testStruct;
        }
    }

    // The ILPP code for NetworkVariables to determine how to serialize them relies on them existing as fields of a NetworkBehaviour to find them.
    // Some of the tests below create NetworkVariables on the stack, so this class is here just to make sure the relevant types are all accounted for.
    public class NetVarILPPClassForTests : NetworkBehaviour
    {
        public NetworkVariable<byte> ByteVar;
        public NetworkVariable<NativeArray<byte>> ByteArrayVar;
        public NetworkVariable<NativeList<byte>> ByteListVar;
        public NetworkVariable<sbyte> SbyteVar;
        public NetworkVariable<NativeArray<sbyte>> SbyteArrayVar;
        public NetworkVariable<NativeList<sbyte>> SbyteListVar;
        public NetworkVariable<short> ShortVar;
        public NetworkVariable<NativeArray<short>> ShortArrayVar;
        public NetworkVariable<NativeList<short>> ShortListVar;
        public NetworkVariable<ushort> UshortVar;
        public NetworkVariable<NativeArray<ushort>> UshortArrayVar;
        public NetworkVariable<NativeList<ushort>> UshortListVar;
        public NetworkVariable<int> IntVar;
        public NetworkVariable<NativeArray<int>> IntArrayVar;
        public NetworkVariable<NativeList<int>> IntListVar;
        public NetworkVariable<uint> UintVar;
        public NetworkVariable<NativeArray<uint>> UintArrayVar;
        public NetworkVariable<NativeList<uint>> UintListVar;
        public NetworkVariable<long> LongVar;
        public NetworkVariable<NativeArray<long>> LongArrayVar;
        public NetworkVariable<NativeList<long>> LongListVar;
        public NetworkVariable<ulong> UlongVar;
        public NetworkVariable<NativeArray<ulong>> UlongArrayVar;
        public NetworkVariable<NativeList<ulong>> UlongListVar;
        public NetworkVariable<bool> BoolVar;
        public NetworkVariable<NativeArray<bool>> BoolArrayVar;
        public NetworkVariable<NativeList<bool>> BoolListVar;
        public NetworkVariable<char> CharVar;
        public NetworkVariable<NativeArray<char>> CharArrayVar;
        public NetworkVariable<NativeList<char>> CharListVar;
        public NetworkVariable<float> FloatVar;
        public NetworkVariable<NativeArray<float>> FloatArrayVar;
        public NetworkVariable<NativeList<float>> FloatListVar;
        public NetworkVariable<double> DoubleVar;
        public NetworkVariable<NativeArray<double>> DoubleArrayVar;
        public NetworkVariable<NativeList<double>> DoubleListVar;
        public NetworkVariable<ByteEnum> ByteEnumVar;
        public NetworkVariable<NativeArray<ByteEnum>> ByteEnumArrayVar;
        public NetworkVariable<NativeList<ByteEnum>> ByteEnumListVar;
        public NetworkVariable<SByteEnum> SByteEnumVar;
        public NetworkVariable<NativeArray<SByteEnum>> SByteEnumArrayVar;
        public NetworkVariable<NativeList<SByteEnum>> SByteEnumListVar;
        public NetworkVariable<ShortEnum> ShortEnumVar;
        public NetworkVariable<NativeArray<ShortEnum>> ShortEnumArrayVar;
        public NetworkVariable<NativeList<ShortEnum>> ShortEnumListVar;
        public NetworkVariable<UShortEnum> UShortEnumVar;
        public NetworkVariable<NativeArray<UShortEnum>> UShortEnumArrayVar;
        public NetworkVariable<NativeList<UShortEnum>> UShortEnumListVar;
        public NetworkVariable<IntEnum> IntEnumVar;
        public NetworkVariable<NativeArray<IntEnum>> IntEnumArrayVar;
        public NetworkVariable<NativeList<IntEnum>> IntEnumListVar;
        public NetworkVariable<UIntEnum> UIntEnumVar;
        public NetworkVariable<NativeArray<UIntEnum>> UIntEnumArrayVar;
        public NetworkVariable<NativeList<UIntEnum>> UIntEnumListVar;
        public NetworkVariable<LongEnum> LongEnumVar;
        public NetworkVariable<NativeArray<LongEnum>> LongEnumArrayVar;
        public NetworkVariable<NativeList<LongEnum>> LongEnumListVar;
        public NetworkVariable<ULongEnum> ULongEnumVar;
        public NetworkVariable<NativeArray<ULongEnum>> ULongEnumArrayVar;
        public NetworkVariable<NativeList<ULongEnum>> ULongEnumListVar;
        public NetworkVariable<Vector2> Vector2Var;
        public NetworkVariable<NativeArray<Vector2>> Vector2ArrayVar;
        public NetworkVariable<NativeList<Vector2>> Vector2ListVar;
        public NetworkVariable<Vector3> Vector3Var;
        public NetworkVariable<NativeArray<Vector3>> Vector3ArrayVar;
        public NetworkVariable<NativeList<Vector3>> Vector3ListVar;
        public NetworkVariable<Vector2Int> Vector2IntVar;
        public NetworkVariable<NativeArray<Vector2Int>> Vector2IntArrayVar;
        public NetworkVariable<NativeList<Vector2Int>> Vector2IntListVar;
        public NetworkVariable<Vector3Int> Vector3IntVar;
        public NetworkVariable<NativeArray<Vector3Int>> Vector3IntArrayVar;
        public NetworkVariable<NativeList<Vector3Int>> Vector3IntListVar;
        public NetworkVariable<Vector4> Vector4Var;
        public NetworkVariable<NativeArray<Vector4>> Vector4ArrayVar;
        public NetworkVariable<NativeList<Vector4>> Vector4ListVar;
        public NetworkVariable<Quaternion> QuaternionVar;
        public NetworkVariable<NativeArray<Quaternion>> QuaternionArrayVar;
        public NetworkVariable<NativeList<Quaternion>> QuaternionListVar;
        public NetworkVariable<Color> ColorVar;
        public NetworkVariable<NativeArray<Color>> ColorArrayVar;
        public NetworkVariable<NativeList<Color>> ColorListVar;
        public NetworkVariable<Color32> Color32Var;
        public NetworkVariable<NativeArray<Color32>> Color32ArrayVar;
        public NetworkVariable<NativeList<Color32>> Color32ListVar;
        public NetworkVariable<Ray> RayVar;
        public NetworkVariable<NativeArray<Ray>> RayArrayVar;
        public NetworkVariable<NativeList<Ray>> RayListVar;
        public NetworkVariable<Ray2D> Ray2DVar;
        public NetworkVariable<NativeArray<Ray2D>> Ray2DArrayVar;
        public NetworkVariable<NativeList<Ray2D>> Ray2DListVar;
        public NetworkVariable<NetworkVariableTestStruct> TestStructVar;
        public NetworkVariable<NativeArray<NetworkVariableTestStruct>> TestStructArrayVar;
        public NetworkVariable<NativeList<NetworkVariableTestStruct>> TestStructListVar;

        public NetworkVariable<FixedString32Bytes> FixedStringVar;
        public NetworkVariable<NativeArray<FixedString32Bytes>> FixedStringArrayVar;
        public NetworkVariable<NativeList<FixedString32Bytes>> FixedStringListVar;

        public NetworkVariable<UnmanagedNetworkSerializableType> UnmanagedNetworkSerializableTypeVar;
        public NetworkVariable<NativeList<UnmanagedNetworkSerializableType>> UnmanagedNetworkSerializableListVar;
        public NetworkVariable<NativeArray<UnmanagedNetworkSerializableType>> UnmanagedNetworkSerializableArrayVar;

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

        private void TestValueType<T>(T testValue, T changedValue) where T : unmanaged
        {
            var serverVariable = new NetworkVariable<T>(testValue);
            var clientVariable = new NetworkVariable<T>();
            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            serverVariable.WriteField(writer);

            Assert.IsFalse(NetworkVariableSerialization<T>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));

            using var reader = new FastBufferReader(writer, Allocator.None);
            clientVariable.ReadField(reader);

            Assert.IsTrue(NetworkVariableSerialization<T>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));

            serverVariable.Value = changedValue;
            Assert.IsFalse(NetworkVariableSerialization<T>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));

            writer.Seek(0);

            serverVariable.WriteDelta(writer);
            
            Assert.IsFalse(NetworkVariableSerialization<T>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));

            using var reader2 = new FastBufferReader(writer, Allocator.None);
            clientVariable.ReadDelta(reader2, false);
            Assert.IsTrue(NetworkVariableSerialization<T>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));
        }

        private void TestValueTypeNativeArray<T>(NativeArray<T> testValue, NativeArray<T> changedValue) where T : unmanaged
        {
            var serverVariable = new NetworkVariable<NativeArray<T>>(testValue);
            var clientVariable = new NetworkVariable<NativeArray<T>>(new NativeArray<T>(1, Allocator.Persistent));
            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            serverVariable.WriteField(writer);

            Assert.IsFalse(NetworkVariableSerialization<NativeArray<T>>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));

            using var reader = new FastBufferReader(writer, Allocator.None);
            clientVariable.ReadField(reader);

            Assert.IsTrue(NetworkVariableSerialization<NativeArray<T>>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));

            serverVariable.Value.Dispose();
            serverVariable.Value = changedValue;
            Assert.IsFalse(NetworkVariableSerialization<NativeArray<T>>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));

            writer.Seek(0);

            serverVariable.WriteDelta(writer);

            Assert.IsFalse(NetworkVariableSerialization<NativeArray<T>>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));

            using var reader2 = new FastBufferReader(writer, Allocator.None);
            clientVariable.ReadDelta(reader2, false);
            Assert.IsTrue(NetworkVariableSerialization<NativeArray<T>>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));

            serverVariable.Value.Dispose();
            clientVariable.Value.Dispose();
        }

        private void TestValueTypeNativeList<T>(NativeList<T> testValue, NativeList<T> changedValue) where T : unmanaged
        {
            var serverVariable = new NetworkVariable<NativeList<T>>(testValue);
            var inPlaceList = new NativeList<T>(1, Allocator.Temp);
            var clientVariable = new NetworkVariable<NativeList<T>>(inPlaceList);
            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            serverVariable.WriteField(writer);

            Assert.IsFalse(NetworkVariableSerialization<NativeList<T>>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));
            // Lists are deserialized in place so this should ALWAYS be true. Checking it every time to make sure!
            Assert.IsTrue(NetworkVariableSerialization<NativeList<T>>.AreEqual(ref clientVariable.RefValue(), ref inPlaceList));

            using var reader = new FastBufferReader(writer, Allocator.None);
            clientVariable.ReadField(reader);

            Assert.IsTrue(NetworkVariableSerialization<NativeList<T>>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));
            // Lists are deserialized in place so this should ALWAYS be true. Checking it every time to make sure!
            Assert.IsTrue(NetworkVariableSerialization<NativeList<T>>.AreEqual(ref clientVariable.RefValue(), ref inPlaceList));

            serverVariable.Value.Dispose();
            serverVariable.Value = changedValue;
            Assert.IsFalse(NetworkVariableSerialization<NativeList<T>>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));
            // Lists are deserialized in place so this should ALWAYS be true. Checking it every time to make sure!
            Assert.IsTrue(NetworkVariableSerialization<NativeList<T>>.AreEqual(ref clientVariable.RefValue(), ref inPlaceList));

            writer.Seek(0);

            serverVariable.WriteDelta(writer);

            Assert.IsFalse(NetworkVariableSerialization<NativeList<T>>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));
            // Lists are deserialized in place so this should ALWAYS be true. Checking it every time to make sure!
            Assert.IsTrue(NetworkVariableSerialization<NativeList<T>>.AreEqual(ref clientVariable.RefValue(), ref inPlaceList));

            using var reader2 = new FastBufferReader(writer, Allocator.None);
            clientVariable.ReadDelta(reader2, false);
            Assert.IsTrue(NetworkVariableSerialization<NativeList<T>>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));
            // Lists are deserialized in place so this should ALWAYS be true. Checking it every time to make sure!
            Assert.IsTrue(NetworkVariableSerialization<NativeList<T>>.AreEqual(ref clientVariable.RefValue(), ref inPlaceList));

            serverVariable.Value.Dispose();
            clientVariable.Value.Dispose();
        }

        [Test]
        public void WhenSerializingAndDeserializingValueTypeNetworkVariables_ValuesAreSerializedCorrectly(
            
            [Values(typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                typeof(long), typeof(ulong), typeof(bool), typeof(char), typeof(float), typeof(double),
                typeof(ByteEnum), typeof(SByteEnum), typeof(ShortEnum), typeof(UShortEnum), typeof(IntEnum),
                typeof(UIntEnum), typeof(LongEnum), typeof(ULongEnum), typeof(Vector2), typeof(Vector3),
                typeof(Vector2Int), typeof(Vector3Int), typeof(Vector4), typeof(Quaternion), typeof(Color),
                typeof(Color32), typeof(Ray), typeof(Ray2D), typeof(NetworkVariableTestStruct), typeof(FixedString32Bytes))]
            Type testType)
        {
            if (testType == typeof(byte))
            {
                TestValueType<byte>(byte.MinValue + 5, byte.MaxValue);
            }
            else if (testType == typeof(sbyte))
            {
                TestValueType<sbyte>(sbyte.MinValue + 5, sbyte.MaxValue);
            }
            else if (testType == typeof(short))
            {
                TestValueType<short>(short.MinValue + 5, short.MaxValue);
            }
            else if (testType == typeof(ushort))
            {
                TestValueType<ushort>(ushort.MinValue + 5, ushort.MaxValue);
            }
            else if (testType == typeof(int))
            {
                TestValueType<int>(int.MinValue + 5, int.MaxValue);
            }
            else if (testType == typeof(uint))
            {
                TestValueType<uint>(uint.MinValue + 5, uint.MaxValue);
            }
            else if (testType == typeof(long))
            {
                TestValueType<long>(long.MinValue + 5, long.MaxValue);
            }
            else if (testType == typeof(ulong))
            {
                TestValueType<ulong>(ulong.MinValue + 5, ulong.MaxValue);
            }
            else if (testType == typeof(bool))
            {
                TestValueType<bool>(true, false);
            }
            else if (testType == typeof(char))
            {
                TestValueType<char>('z', ' ');
            }
            else if (testType == typeof(float))
            {
                TestValueType<float>(float.MinValue + 5.12345678f, float.MaxValue);
            }
            else if (testType == typeof(double))
            {
                TestValueType<double>(double.MinValue + 5.12345678, double.MaxValue);
            }
            else if (testType == typeof(ByteEnum))
            {
                TestValueType<ByteEnum>(ByteEnum.B, ByteEnum.C);
            }
            else if (testType == typeof(SByteEnum))
            {
                TestValueType<SByteEnum>(SByteEnum.B, SByteEnum.C);
            }
            else if (testType == typeof(ShortEnum))
            {
                TestValueType<ShortEnum>(ShortEnum.B, ShortEnum.C);
            }
            else if (testType == typeof(UShortEnum))
            {
                TestValueType<UShortEnum>(UShortEnum.B, UShortEnum.C);
            }
            else if (testType == typeof(IntEnum))
            {
                TestValueType<IntEnum>(IntEnum.B, IntEnum.C);
            }
            else if (testType == typeof(UIntEnum))
            {
                TestValueType<UIntEnum>(UIntEnum.B, UIntEnum.C);
            }
            else if (testType == typeof(LongEnum))
            {
                TestValueType<LongEnum>(LongEnum.B, LongEnum.C);
            }
            else if (testType == typeof(ULongEnum))
            {
                TestValueType<ULongEnum>(ULongEnum.B, ULongEnum.C);
            }
            else if (testType == typeof(Vector2))
            {
                TestValueType<Vector2>(
                    new Vector2(5, 10),
                    new Vector2(15, 20));
            }
            else if (testType == typeof(Vector3))
            {
                TestValueType<Vector3>(
                    new Vector3(5, 10, 15),
                    new Vector3(20, 25, 30));
            }
            else if (testType == typeof(Vector2Int))
            {
                TestValueType<Vector2Int>(
                    new Vector2Int(5, 10),
                    new Vector2Int(15, 20));
            }
            else if (testType == typeof(Vector3Int))
            {
                TestValueType<Vector3Int>(
                    new Vector3Int(5, 10, 15),
                    new Vector3Int(20, 25, 30));
            }
            else if (testType == typeof(Vector4))
            {
                TestValueType<Vector4>(
                    new Vector4(5, 10, 15, 20),
                    new Vector4(25, 30, 35, 40));
            }
            else if (testType == typeof(Quaternion))
            {
                TestValueType<Quaternion>(
                    new Quaternion(5, 10, 15, 20),
                    new Quaternion(25, 30, 35, 40));
            }
            else if (testType == typeof(Color))
            {
                TestValueType<Color>(
                    new Color(1, 0, 0),
                    new Color(0, 1, 1));
            }
            else if (testType == typeof(Color32))
            {
                TestValueType<Color32>(
                    new Color32(255, 0, 0, 128),
                    new Color32(0, 255, 255, 255));
            }
            else if (testType == typeof(Ray))
            {
                TestValueType<Ray>(
                    new Ray(new Vector3(0, 1, 2), new Vector3(3, 4, 5)),
                    new Ray(new Vector3(6, 7, 8), new Vector3(9, 10, 11)));
            }
            else if (testType == typeof(Ray2D))
            {
                TestValueType<Ray2D>(
                    new Ray2D(new Vector2(0, 1), new Vector2(2, 3)),
                    new Ray2D(new Vector2(4, 5), new Vector2(6, 7)));
            }
            else if (testType == typeof(NetworkVariableTestStruct))
            {
                TestValueType<NetworkVariableTestStruct>(NetworkVariableTestStruct.GetTestStruct(), NetworkVariableTestStruct.GetTestStruct());
            }
            else if (testType == typeof(FixedString32Bytes))
            {
                TestValueType<FixedString32Bytes>(new FixedString32Bytes("foobar"), new FixedString32Bytes("12345678901234567890123456789"));
            }
        }

        [Test]
        public void WhenSerializingAndDeserializingValueTypeNativeArrayNetworkVariables_ValuesAreSerializedCorrectly(

            [Values(typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                typeof(long), typeof(ulong), typeof(bool), typeof(char), typeof(float), typeof(double),
                typeof(ByteEnum), typeof(SByteEnum), typeof(ShortEnum), typeof(UShortEnum), typeof(IntEnum),
                typeof(UIntEnum), typeof(LongEnum), typeof(ULongEnum), typeof(Vector2), typeof(Vector3),
                typeof(Vector2Int), typeof(Vector3Int), typeof(Vector4), typeof(Quaternion), typeof(Color),
                typeof(Color32), typeof(Ray), typeof(Ray2D), typeof(NetworkVariableTestStruct), typeof(FixedString32Bytes))]
            Type testType)
        {
            if (testType == typeof(byte))
            {
                TestValueTypeNativeArray<byte>(
                    new NativeArray<byte>(new byte[] { byte.MinValue + 5, byte.MaxValue }, Allocator.Temp),
                    new NativeArray<byte>(new byte[] { 0, byte.MinValue + 10, byte.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(sbyte))
            {
                TestValueTypeNativeArray<sbyte>(
                    new NativeArray<sbyte>(new sbyte[] { sbyte.MinValue + 5, sbyte.MaxValue }, Allocator.Temp),
                    new NativeArray<sbyte>(new sbyte[] { 0, sbyte.MinValue + 10, sbyte.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(short))
            {
                TestValueTypeNativeArray<short>(
                    new NativeArray<short>(new short[] { short.MinValue + 5, short.MaxValue }, Allocator.Temp),
                    new NativeArray<short>(new short[] { 0, short.MinValue + 10, short.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(ushort))
            {
                TestValueTypeNativeArray<ushort>(
                    new NativeArray<ushort>(new ushort[] { ushort.MinValue + 5, ushort.MaxValue }, Allocator.Temp),
                    new NativeArray<ushort>(new ushort[] { 0, ushort.MinValue + 10, ushort.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(int))
            {
                TestValueTypeNativeArray<int>(
                    new NativeArray<int>(new int[] { int.MinValue + 5, int.MaxValue }, Allocator.Temp),
                    new NativeArray<int>(new int[] { 0, int.MinValue + 10, int.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(uint))
            {
                TestValueTypeNativeArray<uint>(
                    new NativeArray<uint>(new uint[] { uint.MinValue + 5, uint.MaxValue }, Allocator.Temp),
                    new NativeArray<uint>(new uint[] { 0, uint.MinValue + 10, uint.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(long))
            {
                TestValueTypeNativeArray<long>(
                    new NativeArray<long>(new long[] { long.MinValue + 5, long.MaxValue }, Allocator.Temp),
                    new NativeArray<long>(new long[] { 0, long.MinValue + 10, long.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(ulong))
            {
                TestValueTypeNativeArray<ulong>(
                    new NativeArray<ulong>(new ulong[] { ulong.MinValue + 5, ulong.MaxValue }, Allocator.Temp),
                    new NativeArray<ulong>(new ulong[] { 0, ulong.MinValue + 10, ulong.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(bool))
            {
                TestValueTypeNativeArray<bool>(
                    new NativeArray<bool>(new bool[] { true, false, true }, Allocator.Temp),
                    new NativeArray<bool>(new bool[] { false, true, false, true, false }, Allocator.Temp));
            }
            else if (testType == typeof(char))
            {
                TestValueTypeNativeArray<char>(
                    new NativeArray<char>(new char[] { 'z', ' ', '?' }, Allocator.Temp),
                    new NativeArray<char>(new char[] { 'n', 'e', 'w', ' ', 'v', 'a', 'l', 'u', 'e' }, Allocator.Temp));
            }
            else if (testType == typeof(float))
            {
                TestValueTypeNativeArray<float>(
                    new NativeArray<float>(new float[] { float.MinValue + 5.12345678f, float.MaxValue }, Allocator.Temp),
                    new NativeArray<float>(new float[] { 0, float.MinValue + 10.987654321f, float.MaxValue - 10.135792468f }, Allocator.Temp));
            }
            else if (testType == typeof(double))
            {
                TestValueTypeNativeArray<double>(
                    new NativeArray<double>(new double[] { double.MinValue + 5.12345678, double.MaxValue }, Allocator.Temp),
                    new NativeArray<double>(new double[] { 0, double.MinValue + 10.987654321, double.MaxValue - 10.135792468 }, Allocator.Temp));
            }
            else if (testType == typeof(ByteEnum))
            {
                TestValueTypeNativeArray<ByteEnum>(
                    new NativeArray<ByteEnum>(new ByteEnum[] { ByteEnum.C, ByteEnum.B, ByteEnum.A }, Allocator.Temp),
                    new NativeArray<ByteEnum>(new ByteEnum[] { ByteEnum.B, ByteEnum.C, ByteEnum.B, ByteEnum.A, ByteEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(SByteEnum))
            {
                TestValueTypeNativeArray<SByteEnum>(
                    new NativeArray<SByteEnum>(new SByteEnum[] { SByteEnum.C, SByteEnum.B, SByteEnum.A }, Allocator.Temp),
                    new NativeArray<SByteEnum>(new SByteEnum[] { SByteEnum.B, SByteEnum.C, SByteEnum.B, SByteEnum.A, SByteEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(ShortEnum))
            {
                TestValueTypeNativeArray<ShortEnum>(
                    new NativeArray<ShortEnum>(new ShortEnum[] { ShortEnum.C, ShortEnum.B, ShortEnum.A }, Allocator.Temp),
                    new NativeArray<ShortEnum>(new ShortEnum[] { ShortEnum.B, ShortEnum.C, ShortEnum.B, ShortEnum.A, ShortEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(UShortEnum))
            {
                TestValueTypeNativeArray<UShortEnum>(
                    new NativeArray<UShortEnum>(new UShortEnum[] { UShortEnum.C, UShortEnum.B, UShortEnum.A }, Allocator.Temp),
                    new NativeArray<UShortEnum>(new UShortEnum[] { UShortEnum.B, UShortEnum.C, UShortEnum.B, UShortEnum.A, UShortEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(IntEnum))
            {
                TestValueTypeNativeArray<IntEnum>(
                    new NativeArray<IntEnum>(new IntEnum[] { IntEnum.C, IntEnum.B, IntEnum.A }, Allocator.Temp),
                    new NativeArray<IntEnum>(new IntEnum[] { IntEnum.B, IntEnum.C, IntEnum.B, IntEnum.A, IntEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(UIntEnum))
            {
                TestValueTypeNativeArray<UIntEnum>(
                    new NativeArray<UIntEnum>(new UIntEnum[] { UIntEnum.C, UIntEnum.B, UIntEnum.A }, Allocator.Temp),
                    new NativeArray<UIntEnum>(new UIntEnum[] { UIntEnum.B, UIntEnum.C, UIntEnum.B, UIntEnum.A, UIntEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(LongEnum))
            {
                TestValueTypeNativeArray<LongEnum>(
                    new NativeArray<LongEnum>(new LongEnum[] { LongEnum.C, LongEnum.B, LongEnum.A }, Allocator.Temp),
                    new NativeArray<LongEnum>(new LongEnum[] { LongEnum.B, LongEnum.C, LongEnum.B, LongEnum.A, LongEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(ULongEnum))
            {
                TestValueTypeNativeArray<ULongEnum>(
                    new NativeArray<ULongEnum>(new ULongEnum[] { ULongEnum.C, ULongEnum.B, ULongEnum.A }, Allocator.Temp),
                    new NativeArray<ULongEnum>(new ULongEnum[] { ULongEnum.B, ULongEnum.C, ULongEnum.B, ULongEnum.A, ULongEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(Vector2))
            {
                TestValueTypeNativeArray<Vector2>(
                    new NativeArray<Vector2>(new Vector2[] { new Vector2(5, 10), new Vector2(15, 20) }, Allocator.Temp),
                    new NativeArray<Vector2>(new Vector2[] { new Vector2(25, 30), new Vector2(35, 40), new Vector2(45, 50) }, Allocator.Temp));
            }
            else if (testType == typeof(Vector3))
            {
                TestValueTypeNativeArray<Vector3>(
                    new NativeArray<Vector3>(new Vector3[] { new Vector3(5, 10, 15), new Vector3(20, 25, 30) }, Allocator.Temp),
                    new NativeArray<Vector3>(new Vector3[] { new Vector3(35, 40, 45), new Vector3(50, 55, 60), new Vector3(65, 70, 75) }, Allocator.Temp));
            }
            else if (testType == typeof(Vector2Int))
            {
                TestValueTypeNativeArray<Vector2Int>(
                    new NativeArray<Vector2Int>(new Vector2Int[] { new Vector2Int(5, 10), new Vector2Int(15, 20) }, Allocator.Temp),
                    new NativeArray<Vector2Int>(new Vector2Int[] { new Vector2Int(25, 30), new Vector2Int(35, 40), new Vector2Int(45, 50) }, Allocator.Temp));
            }
            else if (testType == typeof(Vector3Int))
            {
                TestValueTypeNativeArray<Vector3Int>(
                    new NativeArray<Vector3Int>(new Vector3Int[] { new Vector3Int(5, 10, 15), new Vector3Int(20, 25, 30) }, Allocator.Temp),
                    new NativeArray<Vector3Int>(new Vector3Int[] { new Vector3Int(35, 40, 45), new Vector3Int(50, 55, 60), new Vector3Int(65, 70, 75) }, Allocator.Temp));
            }
            else if (testType == typeof(Vector4))
            {
                TestValueTypeNativeArray<Vector4>(
                    new NativeArray<Vector4>(new Vector4[] { new Vector4(5, 10, 15, 20), new Vector4(25, 30, 35, 40) }, Allocator.Temp),
                    new NativeArray<Vector4>(new Vector4[] { new Vector4(45, 50, 55, 60), new Vector4(65, 70, 75, 80), new Vector4(85, 90, 95, 100) }, Allocator.Temp));
            }
            else if (testType == typeof(Quaternion))
            {
                TestValueTypeNativeArray<Quaternion>(
                    new NativeArray<Quaternion>(new Quaternion[] { new Quaternion(5, 10, 15, 20), new Quaternion(25, 30, 35, 40) }, Allocator.Temp),
                    new NativeArray<Quaternion>(new Quaternion[] { new Quaternion(45, 50, 55, 60), new Quaternion(65, 70, 75, 80), new Quaternion(85, 90, 95, 100) }, Allocator.Temp));
            }
            else if (testType == typeof(Color))
            {
                TestValueTypeNativeArray<Color>(
                    new NativeArray<Color>(new Color[] { new Color(.5f, .10f, .15f), new Color(.20f, .25f, .30f) }, Allocator.Temp),
                    new NativeArray<Color>(new Color[] { new Color(.35f, .40f, .45f), new Color(.50f, .55f, .60f), new Color(.65f, .70f, .75f) }, Allocator.Temp));
            }
            else if (testType == typeof(Color32))
            {
                TestValueTypeNativeArray<Color32>(
                    new NativeArray<Color32>(new Color32[] { new Color32(5, 10, 15, 20), new Color32(25, 30, 35, 40) }, Allocator.Temp),
                    new NativeArray<Color32>(new Color32[] { new Color32(45, 50, 55, 60), new Color32(65, 70, 75, 80), new Color32(85, 90, 95, 100) }, Allocator.Temp));
            }
            else if (testType == typeof(Ray))
            {
                TestValueTypeNativeArray<Ray>(
                    new NativeArray<Ray>(new Ray[]
                    {
                        new Ray(new Vector3(0, 1, 2), new Vector3(3, 4, 5)),
                        new Ray(new Vector3(6, 7, 8), new Vector3(9, 10, 11)),
                    }, Allocator.Temp),
                    new NativeArray<Ray>(new Ray[]
                    {
                        new Ray(new Vector3(12, 13, 14), new Vector3(15, 16, 17)),
                        new Ray(new Vector3(18, 19, 20), new Vector3(21, 22, 23)),
                        new Ray(new Vector3(24, 25, 26), new Vector3(27, 28, 29)),
                    }, Allocator.Temp));
            }
            else if (testType == typeof(Ray2D))
            {
                TestValueTypeNativeArray<Ray2D>(
                    new NativeArray<Ray2D>(new Ray2D[]
                    {
                        new Ray2D(new Vector2(0, 1), new Vector2(3, 4)),
                        new Ray2D(new Vector2(6, 7), new Vector2(9, 10)),
                    }, Allocator.Temp),
                    new NativeArray<Ray2D>(new Ray2D[]
                    {
                        new Ray2D(new Vector2(12, 13), new Vector2(15, 16)),
                        new Ray2D(new Vector2(18, 19), new Vector2(21, 22)),
                        new Ray2D(new Vector2(24, 25), new Vector2(27, 28)),
                    }, Allocator.Temp));
            }
            else if (testType == typeof(NetworkVariableTestStruct))
            {
                TestValueTypeNativeArray<NetworkVariableTestStruct>(
                    new NativeArray<NetworkVariableTestStruct>(new NetworkVariableTestStruct[]
                    {
                        NetworkVariableTestStruct.GetTestStruct(),
                        NetworkVariableTestStruct.GetTestStruct()
                    }, Allocator.Temp),
                    new NativeArray<NetworkVariableTestStruct>(new NetworkVariableTestStruct[]
                    {
                        NetworkVariableTestStruct.GetTestStruct(),
                        NetworkVariableTestStruct.GetTestStruct(),
                        NetworkVariableTestStruct.GetTestStruct()
                    }, Allocator.Temp));
            }
            else if (testType == typeof(FixedString32Bytes))
            {
                TestValueTypeNativeArray<FixedString32Bytes>(
                    new NativeArray<FixedString32Bytes>(new FixedString32Bytes[]
                    {
                        new FixedString32Bytes("foobar"),
                        new FixedString32Bytes("12345678901234567890123456789")
                    }, Allocator.Temp),
                    new NativeArray<FixedString32Bytes>(new FixedString32Bytes[]
                    {
                        new FixedString32Bytes("BazQux"),
                        new FixedString32Bytes("98765432109876543210987654321"),
                        new FixedString32Bytes("FixedString32Bytes")
                    }, Allocator.Temp));
            }
        }

        [Test]
        public void WhenSerializingAndDeserializingValueTypeNativeListNetworkVariables_ValuesAreSerializedCorrectly(
            
            [Values(typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                typeof(long), typeof(ulong), typeof(bool), typeof(char), typeof(float), typeof(double),
                typeof(ByteEnum), typeof(SByteEnum), typeof(ShortEnum), typeof(UShortEnum), typeof(IntEnum),
                typeof(UIntEnum), typeof(LongEnum), typeof(ULongEnum), typeof(Vector2), typeof(Vector3),
                typeof(Vector2Int), typeof(Vector3Int), typeof(Vector4), typeof(Quaternion), typeof(Color),
                typeof(Color32), typeof(Ray), typeof(Ray2D), typeof(NetworkVariableTestStruct), typeof(FixedString32Bytes))]
            Type testType)
        {
            if (testType == typeof(byte))
            {
                TestValueTypeNativeList<byte>(
                    new NativeList<byte>(Allocator.Temp){byte.MinValue + 5, byte.MaxValue}, 
                    new NativeList<byte>(Allocator.Temp){0, byte.MinValue + 10, byte.MaxValue - 10});
            }
            else if (testType == typeof(sbyte))
            {
                TestValueTypeNativeList<sbyte>(
                    new NativeList<sbyte>(Allocator.Temp){sbyte.MinValue + 5, sbyte.MaxValue}, 
                    new NativeList<sbyte>(Allocator.Temp){0, sbyte.MinValue + 10, sbyte.MaxValue - 10});
            }
            else if (testType == typeof(short))
            {
                TestValueTypeNativeList<short>(
                    new NativeList<short>(Allocator.Temp){short.MinValue + 5, short.MaxValue}, 
                    new NativeList<short>(Allocator.Temp){0, short.MinValue + 10, short.MaxValue - 10});
            }
            else if (testType == typeof(ushort))
            {
                TestValueTypeNativeList<ushort>(
                    new NativeList<ushort>(Allocator.Temp){ushort.MinValue + 5, ushort.MaxValue}, 
                    new NativeList<ushort>(Allocator.Temp){0, ushort.MinValue + 10, ushort.MaxValue - 10});
            }
            else if (testType == typeof(int))
            {
                TestValueTypeNativeList<int>(
                    new NativeList<int>(Allocator.Temp){int.MinValue + 5, int.MaxValue}, 
                    new NativeList<int>(Allocator.Temp){0, int.MinValue + 10, int.MaxValue - 10});
            }
            else if (testType == typeof(uint))
            {
                TestValueTypeNativeList<uint>(
                    new NativeList<uint>(Allocator.Temp){uint.MinValue + 5, uint.MaxValue}, 
                    new NativeList<uint>(Allocator.Temp){0, uint.MinValue + 10, uint.MaxValue - 10});
            }
            else if (testType == typeof(long))
            {
                TestValueTypeNativeList<long>(
                    new NativeList<long>(Allocator.Temp){long.MinValue + 5, long.MaxValue}, 
                    new NativeList<long>(Allocator.Temp){0, long.MinValue + 10, long.MaxValue - 10});
            }
            else if (testType == typeof(ulong))
            {
                TestValueTypeNativeList<ulong>(
                    new NativeList<ulong>(Allocator.Temp){ulong.MinValue + 5, ulong.MaxValue}, 
                    new NativeList<ulong>(Allocator.Temp){0, ulong.MinValue + 10, ulong.MaxValue - 10});
            }
            else if (testType == typeof(bool))
            {
                TestValueTypeNativeList<bool>(
                    new NativeList<bool>(Allocator.Temp){true, false, true}, 
                    new NativeList<bool>(Allocator.Temp){false, true, false, true, false});
            }
            else if (testType == typeof(char))
            {
                TestValueTypeNativeList<char>(
                    new NativeList<char>(Allocator.Temp){'z', ' ', '?'}, 
                    new NativeList<char>(Allocator.Temp){'n','e','w',' ','v','a','l','u','e'});
            }
            else if (testType == typeof(float))
            {
                TestValueTypeNativeList<float>(
                    new NativeList<float>(Allocator.Temp){float.MinValue + 5.12345678f, float.MaxValue}, 
                    new NativeList<float>(Allocator.Temp){0, float.MinValue + 10.987654321f, float.MaxValue - 10.135792468f});
            }
            else if (testType == typeof(double))
            {
                TestValueTypeNativeList<double>(
                    new NativeList<double>(Allocator.Temp){double.MinValue + 5.12345678, double.MaxValue}, 
                    new NativeList<double>(Allocator.Temp){0, double.MinValue + 10.987654321, double.MaxValue - 10.135792468});
            }
            else if (testType == typeof(ByteEnum))
            {
                TestValueTypeNativeList<ByteEnum>(
                    new NativeList<ByteEnum>(Allocator.Temp){ByteEnum.C, ByteEnum.B, ByteEnum.A}, 
                    new NativeList<ByteEnum>(Allocator.Temp){ByteEnum.B, ByteEnum.C, ByteEnum.B, ByteEnum.A, ByteEnum.C});
            }
            else if (testType == typeof(SByteEnum))
            {
                TestValueTypeNativeList<SByteEnum>(
                    new NativeList<SByteEnum>(Allocator.Temp){SByteEnum.C, SByteEnum.B, SByteEnum.A}, 
                    new NativeList<SByteEnum>(Allocator.Temp){SByteEnum.B, SByteEnum.C, SByteEnum.B, SByteEnum.A, SByteEnum.C});
            }
            else if (testType == typeof(ShortEnum))
            {
                TestValueTypeNativeList<ShortEnum>(
                    new NativeList<ShortEnum>(Allocator.Temp){ShortEnum.C, ShortEnum.B, ShortEnum.A}, 
                    new NativeList<ShortEnum>(Allocator.Temp){ShortEnum.B, ShortEnum.C, ShortEnum.B, ShortEnum.A, ShortEnum.C});
            }
            else if (testType == typeof(UShortEnum))
            {
                TestValueTypeNativeList<UShortEnum>(
                    new NativeList<UShortEnum>(Allocator.Temp){UShortEnum.C, UShortEnum.B, UShortEnum.A}, 
                    new NativeList<UShortEnum>(Allocator.Temp){UShortEnum.B, UShortEnum.C, UShortEnum.B, UShortEnum.A, UShortEnum.C});
            }
            else if (testType == typeof(IntEnum))
            {
                TestValueTypeNativeList<IntEnum>(
                    new NativeList<IntEnum>(Allocator.Temp){IntEnum.C, IntEnum.B, IntEnum.A}, 
                    new NativeList<IntEnum>(Allocator.Temp){IntEnum.B, IntEnum.C, IntEnum.B, IntEnum.A, IntEnum.C});
            }
            else if (testType == typeof(UIntEnum))
            {
                TestValueTypeNativeList<UIntEnum>(
                    new NativeList<UIntEnum>(Allocator.Temp){UIntEnum.C, UIntEnum.B, UIntEnum.A}, 
                    new NativeList<UIntEnum>(Allocator.Temp){UIntEnum.B, UIntEnum.C, UIntEnum.B, UIntEnum.A, UIntEnum.C});
            }
            else if (testType == typeof(LongEnum))
            {
                TestValueTypeNativeList<LongEnum>(
                    new NativeList<LongEnum>(Allocator.Temp){LongEnum.C, LongEnum.B, LongEnum.A}, 
                    new NativeList<LongEnum>(Allocator.Temp){LongEnum.B, LongEnum.C, LongEnum.B, LongEnum.A, LongEnum.C});
            }
            else if (testType == typeof(ULongEnum))
            {
                TestValueTypeNativeList<ULongEnum>(
                    new NativeList<ULongEnum>(Allocator.Temp){ULongEnum.C, ULongEnum.B, ULongEnum.A}, 
                    new NativeList<ULongEnum>(Allocator.Temp){ULongEnum.B, ULongEnum.C, ULongEnum.B, ULongEnum.A, ULongEnum.C});
            }
            else if (testType == typeof(Vector2))
            {
                TestValueTypeNativeList<Vector2>(
                    new NativeList<Vector2>(Allocator.Temp){new Vector2(5, 10), new Vector2(15, 20)}, 
                    new NativeList<Vector2>(Allocator.Temp){new Vector2(25, 30), new Vector2(35, 40), new Vector2(45, 50)});
            }
            else if (testType == typeof(Vector3))
            {
                TestValueTypeNativeList<Vector3>(
                    new NativeList<Vector3>(Allocator.Temp){new Vector3(5, 10, 15), new Vector3(20, 25, 30)}, 
                    new NativeList<Vector3>(Allocator.Temp){new Vector3(35, 40, 45), new Vector3(50, 55, 60), new Vector3(65, 70, 75)});
            }
            else if (testType == typeof(Vector2Int))
            {
                TestValueTypeNativeList<Vector2Int>(
                    new NativeList<Vector2Int>(Allocator.Temp){new Vector2Int(5, 10), new Vector2Int(15, 20)}, 
                    new NativeList<Vector2Int>(Allocator.Temp){new Vector2Int(25, 30), new Vector2Int(35, 40), new Vector2Int(45, 50)});
            }
            else if (testType == typeof(Vector3Int))
            {
                TestValueTypeNativeList<Vector3Int>(
                    new NativeList<Vector3Int>(Allocator.Temp){new Vector3Int(5, 10, 15), new Vector3Int(20, 25, 30)}, 
                    new NativeList<Vector3Int>(Allocator.Temp){new Vector3Int(35, 40, 45), new Vector3Int(50, 55, 60), new Vector3Int(65, 70, 75)});
            }
            else if (testType == typeof(Vector4))
            {
                TestValueTypeNativeList<Vector4>(
                    new NativeList<Vector4>(Allocator.Temp){new Vector4(5, 10, 15, 20), new Vector4(25, 30, 35, 40)}, 
                    new NativeList<Vector4>(Allocator.Temp){new Vector4(45, 50, 55, 60), new Vector4(65, 70, 75, 80), new Vector4(85, 90, 95, 100)});
            }
            else if (testType == typeof(Quaternion))
            {
                TestValueTypeNativeList<Quaternion>(
                    new NativeList<Quaternion>(Allocator.Temp){new Quaternion(5, 10, 15, 20), new Quaternion(25, 30, 35, 40)}, 
                    new NativeList<Quaternion>(Allocator.Temp){new Quaternion(45, 50, 55, 60), new Quaternion(65, 70, 75, 80), new Quaternion(85, 90, 95, 100)});
            }
            else if (testType == typeof(Color))
            {
                TestValueTypeNativeList<Color>(
                    new NativeList<Color>(Allocator.Temp){new Color(.5f, .10f, .15f), new Color(.20f, .25f, .30f)}, 
                    new NativeList<Color>(Allocator.Temp){new Color(.35f, .40f, .45f), new Color(.50f, .55f, .60f), new Color(.65f, .70f, .75f)});
            }
            else if (testType == typeof(Color32))
            {
                TestValueTypeNativeList<Color32>(
                    new NativeList<Color32>(Allocator.Temp){new Color32(5, 10, 15, 20), new Color32(25, 30, 35, 40)}, 
                    new NativeList<Color32>(Allocator.Temp){new Color32(45, 50, 55, 60), new Color32(65, 70, 75, 80), new Color32(85, 90, 95, 100)});
            }
            else if (testType == typeof(Ray))
            {
                TestValueTypeNativeList<Ray>(
                    new NativeList<Ray>(Allocator.Temp)
                    {
                        new Ray(new Vector3(0, 1, 2), new Vector3(3, 4, 5)),
                        new Ray(new Vector3(6, 7, 8), new Vector3(9, 10, 11)),
                    },
                    new NativeList<Ray>(Allocator.Temp)
                    {
                        new Ray(new Vector3(12, 13, 14), new Vector3(15, 16, 17)),
                        new Ray(new Vector3(18, 19, 20), new Vector3(21, 22, 23)),
                        new Ray(new Vector3(24, 25, 26), new Vector3(27, 28, 29)),
                    });
            }
            else if (testType == typeof(Ray2D))
            {
                TestValueTypeNativeList<Ray2D>(
                    new NativeList<Ray2D>(Allocator.Temp)
                    {
                        new Ray2D(new Vector2(0, 1), new Vector2(3, 4)),
                        new Ray2D(new Vector2(6, 7), new Vector2(9, 10)),
                    },
                    new NativeList<Ray2D>(Allocator.Temp)
                    {
                        new Ray2D(new Vector2(12, 13), new Vector2(15, 16)),
                        new Ray2D(new Vector2(18, 19), new Vector2(21, 22)),
                        new Ray2D(new Vector2(24, 25), new Vector2(27, 28)),
                    });
            }
            else if (testType == typeof(NetworkVariableTestStruct))
            {
                TestValueTypeNativeList<NetworkVariableTestStruct>(
                    new NativeList<NetworkVariableTestStruct>(Allocator.Temp)
                    {
                        NetworkVariableTestStruct.GetTestStruct(),
                        NetworkVariableTestStruct.GetTestStruct()
                    },
                    new NativeList<NetworkVariableTestStruct>(Allocator.Temp)
                    {
                        NetworkVariableTestStruct.GetTestStruct(),
                        NetworkVariableTestStruct.GetTestStruct(),
                        NetworkVariableTestStruct.GetTestStruct()
                    });
            }
            else if (testType == typeof(FixedString32Bytes))
            {
                TestValueTypeNativeList<FixedString32Bytes>(
                    new NativeList<FixedString32Bytes>(Allocator.Temp)
                    {
                        new FixedString32Bytes("foobar"),
                        new FixedString32Bytes("12345678901234567890123456789")
                    },
                    new NativeList<FixedString32Bytes>(Allocator.Temp)
                    {
                        new FixedString32Bytes("BazQux"),
                        new FixedString32Bytes("98765432109876543210987654321"),
                        new FixedString32Bytes("FixedString32Bytes")
                    });
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
