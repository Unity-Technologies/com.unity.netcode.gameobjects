using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

namespace Unity.Netcode.RuntimeTests
{
    public class NetVarPermTestComp : NetworkBehaviour
    {
        public NetworkVariable<Vector3> OwnerWritable_Position = new NetworkVariable<Vector3>(Vector3.one, NetworkVariableBase.DefaultReadPerm, NetworkVariableWritePermission.Owner);
        public NetworkVariable<Vector3> ServerWritable_Position = new NetworkVariable<Vector3>(Vector3.one, NetworkVariableBase.DefaultReadPerm, NetworkVariableWritePermission.Server);
        public NetworkVariable<Vector3> OwnerReadWrite_Position = new NetworkVariable<Vector3>(Vector3.one, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
    }

    public class NetworkVariableMiddleclass<TMiddleclassName> : NetworkVariable<TMiddleclassName>
    {

    }

    public class NetworkVariableSubclass<TSubclassName> : NetworkVariableMiddleclass<TSubclassName>
    {

    }

    public class NetworkBehaviourWithNetVarArray : NetworkBehaviour
    {
        public NetworkVariable<int> Int0 = new NetworkVariable<int>();
        public NetworkVariable<int> Int1 = new NetworkVariable<int>();
        public NetworkVariable<int> Int2 = new NetworkVariable<int>();
        public NetworkVariable<int> Int3 = new NetworkVariable<int>();
        public NetworkVariable<int> Int4 = new NetworkVariable<int>();
        public NetworkVariable<int>[] AllInts = new NetworkVariable<int>[5];

        public int InitializedFieldCount => NetworkVariableFields.Count;


        private void Awake()
        {
            AllInts[0] = Int0;
            AllInts[1] = Int1;
            AllInts[2] = Int2;
            AllInts[3] = Int3;
            AllInts[4] = Int4;
        }
    }

    internal struct TypeReferencedOnlyInCustomSerialization1 : INetworkSerializeByMemcpy
    {
        public int I;
    }

    internal struct TypeReferencedOnlyInCustomSerialization2 : INetworkSerializeByMemcpy
    {
        public int I;
    }

    internal struct TypeReferencedOnlyInCustomSerialization3 : INetworkSerializeByMemcpy
    {
        public int I;
    }

    internal struct TypeReferencedOnlyInCustomSerialization4 : INetworkSerializeByMemcpy
    {
        public int I;
    }

    internal struct TypeReferencedOnlyInCustomSerialization5 : INetworkSerializeByMemcpy
    {
        public int I;
    }

    internal struct TypeReferencedOnlyInCustomSerialization6 : INetworkSerializeByMemcpy
    {
        public int I;
    }

    // Both T and U are serializable
    [GenerateSerializationForGenericParameter(0)]
    [GenerateSerializationForGenericParameter(1)]
    internal class CustomSerializableClass<TSerializableType1, TSerializableType2>
    {

    }

    // Only U is serializable
    [GenerateSerializationForGenericParameter(1)]
    internal class CustomSerializableBaseClass<TUnserializableType, TSerializableType>
    {

    }

    // T is serializable, passes TypeReferencedOnlyInCustomSerialization3 as U to the subclass, making it serializable
    [GenerateSerializationForGenericParameter(0)]
    internal class CustomSerializableSubclass<TSerializableType> : CustomSerializableBaseClass<TSerializableType, TypeReferencedOnlyInCustomSerialization3>
    {

    }

    // T is serializable, passes TypeReferencedOnlyInCustomSerialization3 as U to the subclass, making it serializable
    [GenerateSerializationForGenericParameter(0)]
    internal class CustomSerializableSubclassWithNativeArray<TSerializableType> : CustomSerializableBaseClass<TSerializableType, NativeArray<TypeReferencedOnlyInCustomSerialization3>>
    {

    }

    internal class CustomGenericSerializationTestBehaviour : NetworkBehaviour
    {
        public CustomSerializableClass<TypeReferencedOnlyInCustomSerialization1, TypeReferencedOnlyInCustomSerialization2> Value1;
        public CustomSerializableClass<NativeArray<TypeReferencedOnlyInCustomSerialization1>, NativeArray<TypeReferencedOnlyInCustomSerialization2>> Value2;
        public CustomSerializableSubclass<TypeReferencedOnlyInCustomSerialization4> Value3;
        public CustomSerializableSubclassWithNativeArray<NativeArray<TypeReferencedOnlyInCustomSerialization4>> Value4;
    }

    [GenerateSerializationForType(typeof(TypeReferencedOnlyInCustomSerialization5))]
    [GenerateSerializationForType(typeof(NativeArray<TypeReferencedOnlyInCustomSerialization5>))]
    internal struct SomeRandomStruct
    {
        [GenerateSerializationForType(typeof(TypeReferencedOnlyInCustomSerialization6))]
        [GenerateSerializationForType(typeof(NativeArray<TypeReferencedOnlyInCustomSerialization6>))]
        public void Foo()
        {

        }
    }

    public struct TemplatedValueOnlyReferencedByNetworkVariableSubclass<T> : INetworkSerializeByMemcpy
        where T : unmanaged
    {
        public T Value;
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
    public enum ULongEnum : ulong
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
                D = s_Random.Next(),
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
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<byte>> ByteListVar;
#endif
        public NetworkVariable<sbyte> SbyteVar;
        public NetworkVariable<NativeArray<sbyte>> SbyteArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<sbyte>> SbyteListVar;
#endif
        public NetworkVariable<short> ShortVar;
        public NetworkVariable<NativeArray<short>> ShortArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<short>> ShortListVar;
#endif
        public NetworkVariable<ushort> UshortVar;
        public NetworkVariable<NativeArray<ushort>> UshortArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<ushort>> UshortListVar;
#endif
        public NetworkVariable<int> IntVar;
        public NetworkVariable<NativeArray<int>> IntArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<int>> IntListVar;
#endif
        public NetworkVariable<uint> UintVar;
        public NetworkVariable<NativeArray<uint>> UintArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<uint>> UintListVar;
#endif
        public NetworkVariable<long> LongVar;
        public NetworkVariable<NativeArray<long>> LongArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<long>> LongListVar;
#endif
        public NetworkVariable<ulong> UlongVar;
        public NetworkVariable<NativeArray<ulong>> UlongArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<ulong>> UlongListVar;
#endif
        public NetworkVariable<bool> BoolVar;
        public NetworkVariable<NativeArray<bool>> BoolArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<bool>> BoolListVar;
#endif
        public NetworkVariable<char> CharVar;
        public NetworkVariable<NativeArray<char>> CharArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<char>> CharListVar;
#endif
        public NetworkVariable<float> FloatVar;
        public NetworkVariable<NativeArray<float>> FloatArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<float>> FloatListVar;
#endif
        public NetworkVariable<double> DoubleVar;
        public NetworkVariable<NativeArray<double>> DoubleArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<double>> DoubleListVar;
#endif
        public NetworkVariable<ByteEnum> ByteEnumVar;
        public NetworkVariable<NativeArray<ByteEnum>> ByteEnumArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<ByteEnum>> ByteEnumListVar;
#endif
        public NetworkVariable<SByteEnum> SByteEnumVar;
        public NetworkVariable<NativeArray<SByteEnum>> SByteEnumArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<SByteEnum>> SByteEnumListVar;
#endif
        public NetworkVariable<ShortEnum> ShortEnumVar;
        public NetworkVariable<NativeArray<ShortEnum>> ShortEnumArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<ShortEnum>> ShortEnumListVar;
#endif
        public NetworkVariable<UShortEnum> UShortEnumVar;
        public NetworkVariable<NativeArray<UShortEnum>> UShortEnumArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<UShortEnum>> UShortEnumListVar;
#endif
        public NetworkVariable<IntEnum> IntEnumVar;
        public NetworkVariable<NativeArray<IntEnum>> IntEnumArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<IntEnum>> IntEnumListVar;
#endif
        public NetworkVariable<UIntEnum> UIntEnumVar;
        public NetworkVariable<NativeArray<UIntEnum>> UIntEnumArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<UIntEnum>> UIntEnumListVar;
#endif
        public NetworkVariable<LongEnum> LongEnumVar;
        public NetworkVariable<NativeArray<LongEnum>> LongEnumArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<LongEnum>> LongEnumListVar;
#endif
        public NetworkVariable<ULongEnum> ULongEnumVar;
        public NetworkVariable<NativeArray<ULongEnum>> ULongEnumArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<ULongEnum>> ULongEnumListVar;
#endif
        public NetworkVariable<Vector2> Vector2Var;
        public NetworkVariable<NativeArray<Vector2>> Vector2ArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<Vector2>> Vector2ListVar;
#endif
        public NetworkVariable<Vector3> Vector3Var;
        public NetworkVariable<NativeArray<Vector3>> Vector3ArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<Vector3>> Vector3ListVar;
#endif
        public NetworkVariable<Vector2Int> Vector2IntVar;
        public NetworkVariable<NativeArray<Vector2Int>> Vector2IntArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<Vector2Int>> Vector2IntListVar;
#endif
        public NetworkVariable<Vector3Int> Vector3IntVar;
        public NetworkVariable<NativeArray<Vector3Int>> Vector3IntArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<Vector3Int>> Vector3IntListVar;
#endif
        public NetworkVariable<Vector4> Vector4Var;
        public NetworkVariable<NativeArray<Vector4>> Vector4ArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<Vector4>> Vector4ListVar;
#endif
        public NetworkVariable<Quaternion> QuaternionVar;
        public NetworkVariable<NativeArray<Quaternion>> QuaternionArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<Quaternion>> QuaternionListVar;
#endif
        public NetworkVariable<Color> ColorVar;
        public NetworkVariable<NativeArray<Color>> ColorArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<Color>> ColorListVar;
#endif
        public NetworkVariable<Color32> Color32Var;
        public NetworkVariable<NativeArray<Color32>> Color32ArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<Color32>> Color32ListVar;
#endif
        public NetworkVariable<Ray> RayVar;
        public NetworkVariable<NativeArray<Ray>> RayArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<Ray>> RayListVar;
#endif
        public NetworkVariable<Ray2D> Ray2DVar;
        public NetworkVariable<NativeArray<Ray2D>> Ray2DArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<Ray2D>> Ray2DListVar;
#endif
        public NetworkVariable<NetworkVariableTestStruct> TestStructVar;
        public NetworkVariable<NativeArray<NetworkVariableTestStruct>> TestStructArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<NetworkVariableTestStruct>> TestStructListVar;
#endif

        public NetworkVariable<FixedString32Bytes> FixedStringVar;
        public NetworkVariable<NativeArray<FixedString32Bytes>> FixedStringArrayVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<FixedString32Bytes>> FixedStringListVar;
#endif

        public NetworkVariable<UnmanagedNetworkSerializableType> UnmanagedNetworkSerializableTypeVar;
#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        public NetworkVariable<NativeList<UnmanagedNetworkSerializableType>> UnmanagedNetworkSerializableListVar;
#endif
        public NetworkVariable<NativeArray<UnmanagedNetworkSerializableType>> UnmanagedNetworkSerializableArrayVar;

        public NetworkVariable<ManagedNetworkSerializableType> ManagedNetworkSerializableTypeVar;

        public NetworkVariable<string> StringVar;
        public NetworkVariable<Guid> GuidVar;
        public NetworkVariableSubclass<TemplatedValueOnlyReferencedByNetworkVariableSubclass<int>> SubclassVar;
    }

    public class TemplateNetworkBehaviourType<T> : NetworkBehaviour
    {
        public NetworkVariable<T> TheVar;
    }

    public class IntermediateNetworkBehavior<T> : TemplateNetworkBehaviourType<T>
    {
        public NetworkVariable<T> TheVar2;
    }

    public class ClassHavingNetworkBehaviour : IntermediateNetworkBehavior<TestClass>
    {

    }

    // Please do not reference TestClass_ReferencedOnlyByTemplateNetworkBehavourType anywhere other than here!
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
            yield return WaitForTicks(m_ServerNetworkManager, 2);

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
            yield return WaitForTicks(m_ServerNetworkManager, 2);

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
            yield return WaitForTicks(m_ServerNetworkManager, 2);

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
            yield return WaitForTicks(m_ServerNetworkManager, 2);

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

        protected override bool m_EnableTimeTravel => true;
        protected override bool m_SetupIsACoroutine => false;
        protected override bool m_TearDownIsACoroutine => false;

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
        private void InitializeServerAndClients(HostOrServer useHost)
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

            Assert.True(NetcodeIntegrationTestHelpers.Start(useHost == HostOrServer.Host, m_ServerNetworkManager, m_ClientNetworkManagers), "Failed to start server and client instances");

            RegisterSceneManagerHandler();

            // Wait for connection on client and server side
            var success = WaitForClientsConnectedOrTimeOutWithTimeTravel();
            Assert.True(success, $"Timed-out waiting for all clients to connect!");

            // These are the *SERVER VERSIONS* of the *CLIENT PLAYER 1 & 2*
            var result = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();

            NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentationWithTimeTravel(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId,
                m_ServerNetworkManager, result);

            // Assign server-side client's player
            m_Player1OnServer = result.Result.GetComponent<NetworkVariableTest>();

            // This is client1's view of itself
            NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentationWithTimeTravel(
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

            var instanceCount = useHost == HostOrServer.Host ? NumberOfClients * 3 : NumberOfClients * 2;
            // Wait for the client-side to notify it is finished initializing and spawning.
            success = WaitForConditionOrTimeOutWithTimeTravel(() => s_ClientNetworkVariableTestInstances.Count == instanceCount);

            Assert.True(success, "Timed out waiting for all client NetworkVariableTest instances to register they have spawned!");

            TimeTravelToNextTick();
        }

        /// <summary>
        /// Runs generalized tests on all predefined NetworkVariable types
        /// </summary>
        [Test]
        public void AllNetworkVariableTypes([Values] HostOrServer useHost)
        {
            // Create, instantiate, and host
            // This would normally go in Setup, but since every other test but this one
            //  uses NetworkManagerHelper, and it does its own NetworkManager setup / teardown,
            //  for now we put this within this one test until we migrate it to MIH
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager(out NetworkManager server, useHost == HostOrServer.Host ? NetworkManagerHelper.NetworkManagerOperatingMode.Host : NetworkManagerHelper.NetworkManagerOperatingMode.Server));

            Assert.IsTrue(server.IsHost == (useHost == HostOrServer.Host), $"{nameof(useHost)} does not match the server.IsHost value!");

            Guid gameObjectId = NetworkManagerHelper.AddGameNetworkObject("NetworkVariableTestComponent");

            var networkVariableTestComponent = NetworkManagerHelper.AddComponentToObject<NetworkVariableTestComponent>(gameObjectId);

            NetworkManagerHelper.SpawnNetworkObject(gameObjectId);

            // Start Testing
            networkVariableTestComponent.EnableTesting = true;

            var success = WaitForConditionOrTimeOutWithTimeTravel(() => true == networkVariableTestComponent.IsTestComplete());
            Assert.True(success, "Timed out waiting for the test to complete!");

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

        [Test]
        public void ClientWritePermissionTest([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);

            // client must not be allowed to write to a server auth variable
            Assert.Throws<InvalidOperationException>(() => m_Player1OnClient1.TheScalar.Value = k_TestVal1);
        }

        /// <summary>
        /// Runs tests that network variables sync on client whatever the local value of <see cref="Time.timeScale"/>.
        /// </summary>
        [Test]
        public void NetworkVariableSync_WithDifferentTimeScale([Values] HostOrServer useHost, [Values(0.0f, 1.0f, 2.0f)] float timeScale)
        {
            Time.timeScale = timeScale;

            InitializeServerAndClients(useHost);

            m_Player1OnServer.TheScalar.Value = k_TestVal1;

            // Now wait for the client side version to be updated to k_TestVal1
            var success = WaitForConditionOrTimeOutWithTimeTravel(() => m_Player1OnClient1.TheScalar.Value == k_TestVal1);
            Assert.True(success, "Timed out waiting for client-side NetworkVariable to update!");
        }

        [Test]
        public void FixedString32Test([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);
            m_Player1OnServer.FixedString32.Value = k_FixedStringTestValue;

            // Now wait for the client side version to be updated to k_FixedStringTestValue
            var success = WaitForConditionOrTimeOutWithTimeTravel(() => m_Player1OnClient1.FixedString32.Value == k_FixedStringTestValue);
            Assert.True(success, "Timed out waiting for client-side NetworkVariable to update!");
        }

        [Test]
        public void NetworkListAdd([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);
            m_NetworkListPredicateHandler = new NetworkListTestPredicate(m_Player1OnServer, m_Player1OnClient1, NetworkListTestPredicate.NetworkListTestStates.Add, 10);
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(m_NetworkListPredicateHandler));
        }

        [Test]
        public void WhenListContainsManyLargeValues_OverflowExceptionIsNotThrown([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);
            m_NetworkListPredicateHandler = new NetworkListTestPredicate(m_Player1OnServer, m_Player1OnClient1, NetworkListTestPredicate.NetworkListTestStates.ContainsLarge, 20);
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(m_NetworkListPredicateHandler));
        }

        [Test]
        public void NetworkListContains([Values] HostOrServer useHost)
        {
            // Re-use the NetworkListAdd to initialize the server and client as well as make sure the list is populated
            NetworkListAdd(useHost);

            // Now test the NetworkList.Contains method
            m_NetworkListPredicateHandler.SetNetworkListTestState(NetworkListTestPredicate.NetworkListTestStates.Contains);
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(m_NetworkListPredicateHandler));
        }


        [Test]
        public void NetworkListInsert([Values] HostOrServer useHost)
        {
            // Re-use the NetworkListAdd to initialize the server and client as well as make sure the list is populated
            NetworkListAdd(useHost);

            // Now randomly insert a random value entry
            m_Player1OnServer.TheList.Insert(Random.Range(0, 9), Random.Range(1, 99));

            // Verify the element count and values on the client matches the server
            m_NetworkListPredicateHandler.SetNetworkListTestState(NetworkListTestPredicate.NetworkListTestStates.VerifyData);
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(m_NetworkListPredicateHandler));
        }

        [Test]
        public void NetworkListIndexOf([Values] HostOrServer useHost)
        {
            // Re-use the NetworkListAdd to initialize the server and client as well as make sure the list is populated
            NetworkListAdd(useHost);

            m_NetworkListPredicateHandler.SetNetworkListTestState(NetworkListTestPredicate.NetworkListTestStates.IndexOf);
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(m_NetworkListPredicateHandler));
        }

        [Test]
        public void NetworkListValueUpdate([Values] HostOrServer useHost)
        {
            var testSucceeded = false;
            InitializeServerAndClients(useHost);
            // Add 1 element value and verify it is the same on the client
            m_NetworkListPredicateHandler = new NetworkListTestPredicate(m_Player1OnServer, m_Player1OnClient1, NetworkListTestPredicate.NetworkListTestStates.Add, 1);
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(m_NetworkListPredicateHandler));

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
            WaitForConditionOrTimeOutWithTimeTravel(m_NetworkListPredicateHandler);

            Assert.That(testSucceeded);
            m_Player1OnClient1.TheList.OnListChanged -= TestValueUpdatedCallback;
        }

        private List<int> m_ExpectedValuesServer = new List<int>();
        private List<int> m_ExpectedValuesClient = new List<int>();

        public enum ListRemoveTypes
        {
            Remove,
            RemoveAt
        }


        [Test]
        public void NetworkListRemoveTests([Values] HostOrServer useHost, [Values] ListRemoveTypes listRemoveType)
        {
            m_ExpectedValuesServer.Clear();
            m_ExpectedValuesClient.Clear();
            // Re-use the NetworkListAdd to initialize the server and client as well as make sure the list is populated
            NetworkListAdd(useHost);

            // Randomly remove a few entries
            m_Player1OnServer.TheList.OnListChanged += Server_OnListChanged;
            m_Player1OnClient1.TheList.OnListChanged += Client_OnListChanged;

            // Remove half of the elements
            for (int i = 0; i < (int)(m_Player1OnServer.TheList.Count * 0.5f); i++)
            {
                var index = Random.Range(0, m_Player1OnServer.TheList.Count - 1);
                var value = m_Player1OnServer.TheList[index];
                m_ExpectedValuesServer.Add(value);
                m_ExpectedValuesClient.Add(value);

                if (listRemoveType == ListRemoveTypes.RemoveAt)
                {
                    m_Player1OnServer.TheList.RemoveAt(index);
                }
                else
                {
                    m_Player1OnServer.TheList.Remove(value);
                }
            }

            // Verify the element count and values on the client matches the server
            m_NetworkListPredicateHandler.SetNetworkListTestState(NetworkListTestPredicate.NetworkListTestStates.VerifyData);
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(m_NetworkListPredicateHandler));

            Assert.True(m_ExpectedValuesServer.Count == 0, $"Server was not notified of all elements removed and still has {m_ExpectedValuesServer.Count} elements left!");
            Assert.True(m_ExpectedValuesClient.Count == 0, $"Client was not notified of all elements removed and still has {m_ExpectedValuesClient.Count} elements left!");
        }

        private void Server_OnListChanged(NetworkListEvent<int> changeEvent)
        {
            Assert.True(m_ExpectedValuesServer.Contains(changeEvent.Value));
            m_ExpectedValuesServer.Remove(changeEvent.Value);
        }

        private void Client_OnListChanged(NetworkListEvent<int> changeEvent)
        {
            Assert.True(m_ExpectedValuesClient.Contains(changeEvent.Value));
            m_ExpectedValuesClient.Remove(changeEvent.Value);
        }

        [Test]
        public void NetworkListClear([Values] HostOrServer useHost)
        {
            // Re-use the NetworkListAdd to initialize the server and client as well as make sure the list is populated
            NetworkListAdd(useHost);
            m_Player1OnServer.TheList.Clear();
            // Verify the element count and values on the client matches the server
            m_NetworkListPredicateHandler.SetNetworkListTestState(NetworkListTestPredicate.NetworkListTestStates.VerifyData);
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(m_NetworkListPredicateHandler));
        }

        [Test]
        public void TestNetworkVariableClass([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);

            bool VerifyClass()
            {
                return m_Player1OnClient1.TheClass.Value != null &&
                       m_Player1OnClient1.TheClass.Value.SomeBool == m_Player1OnServer.TheClass.Value.SomeBool &&
                       m_Player1OnClient1.TheClass.Value.SomeInt == m_Player1OnServer.TheClass.Value.SomeInt;
            }

            m_Player1OnServer.TheClass.Value = new TestClass { SomeInt = k_TestUInt, SomeBool = false };
            m_Player1OnServer.TheClass.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(VerifyClass));
        }

        [Test]
        public void TestNetworkVariableTemplateClass([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);

            bool VerifyClass()
            {
                return m_Player1OnClient1.TheTemplateClass.Value.Value != null && m_Player1OnClient1.TheTemplateClass.Value.Value.SomeBool == m_Player1OnServer.TheTemplateClass.Value.Value.SomeBool &&
                       m_Player1OnClient1.TheTemplateClass.Value.Value.SomeInt == m_Player1OnServer.TheTemplateClass.Value.Value.SomeInt;
            }

            m_Player1OnServer.TheTemplateClass.Value = new ManagedTemplateNetworkSerializableType<TestClass> { Value = new TestClass { SomeInt = k_TestUInt, SomeBool = false } };
            m_Player1OnServer.TheTemplateClass.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(VerifyClass));
        }

        [Test]
        public void TestNetworkListStruct([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);

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
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(VerifyList));
        }

        [Test]
        public void TestNetworkVariableStruct([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);

            bool VerifyStructure()
            {
                return m_Player1OnClient1.TheStruct.Value.SomeBool == m_Player1OnServer.TheStruct.Value.SomeBool &&
                       m_Player1OnClient1.TheStruct.Value.SomeInt == m_Player1OnServer.TheStruct.Value.SomeInt;
            }

            m_Player1OnServer.TheStruct.Value = new TestStruct { SomeInt = k_TestUInt, SomeBool = false };
            m_Player1OnServer.TheStruct.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(VerifyStructure));
        }

        [Test]
        public void TestNetworkVariableTemplateStruct([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);

            bool VerifyStructure()
            {
                return m_Player1OnClient1.TheTemplateStruct.Value.Value.SomeBool == m_Player1OnServer.TheTemplateStruct.Value.Value.SomeBool &&
                       m_Player1OnClient1.TheTemplateStruct.Value.Value.SomeInt == m_Player1OnServer.TheTemplateStruct.Value.Value.SomeInt;
            }

            m_Player1OnServer.TheTemplateStruct.Value = new UnmanagedTemplateNetworkSerializableType<TestStruct> { Value = new TestStruct { SomeInt = k_TestUInt, SomeBool = false } };
            m_Player1OnServer.TheTemplateStruct.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(VerifyStructure));
        }

        [Test]
        public void TestNetworkVariableTemplateBehaviourClass([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);

            bool VerifyClass()
            {
                return (m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour>().TheVar.Value != null && m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour>().TheVar.Value.SomeBool == m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour>().TheVar.Value.SomeBool &&
                       m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour>().TheVar.Value.SomeInt == m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour>().TheVar.Value.SomeInt)
                       && (m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour>().TheVar2.Value != null && m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour>().TheVar2.Value.SomeBool == m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour>().TheVar2.Value.SomeBool &&
                       m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour>().TheVar2.Value.SomeInt == m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour>().TheVar2.Value.SomeInt);
            }

            m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour>().TheVar.Value = new TestClass { SomeInt = k_TestUInt, SomeBool = false };
            m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour>().TheVar2.Value = new TestClass { SomeInt = k_TestUInt, SomeBool = false };
            m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour>().TheVar.SetDirty(true);
            m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour>().TheVar2.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(VerifyClass));
        }

        [Test]
        public void TestNetworkVariableTemplateBehaviourClassNotReferencedElsewhere([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);

            bool VerifyClass()
            {
                return m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.Value != null && m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.Value.SomeBool == m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.Value.SomeBool &&
                       m_Player1OnClient1.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.Value.SomeInt == m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.Value.SomeInt;
            }

            m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.Value = new TestClass_ReferencedOnlyByTemplateNetworkBehavourType { SomeInt = k_TestUInt, SomeBool = false };
            m_Player1OnServer.GetComponent<ClassHavingNetworkBehaviour2>().TheVar.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(VerifyClass));
        }

        [Test]
        public void TestNetworkVariableTemplateBehaviourStruct([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);

            bool VerifyClass()
            {
                return m_Player1OnClient1.GetComponent<StructHavingNetworkBehaviour>().TheVar.Value.SomeBool == m_Player1OnServer.GetComponent<StructHavingNetworkBehaviour>().TheVar.Value.SomeBool &&
                       m_Player1OnClient1.GetComponent<StructHavingNetworkBehaviour>().TheVar.Value.SomeInt == m_Player1OnServer.GetComponent<StructHavingNetworkBehaviour>().TheVar.Value.SomeInt;
            }

            m_Player1OnServer.GetComponent<StructHavingNetworkBehaviour>().TheVar.Value = new TestStruct { SomeInt = k_TestUInt, SomeBool = false };
            m_Player1OnServer.GetComponent<StructHavingNetworkBehaviour>().TheVar.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(VerifyClass));
        }

        [Test]
        public void TestNetworkVariableEnum([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);

            bool VerifyStructure()
            {
                return m_Player1OnClient1.TheEnum.Value == NetworkVariableTest.SomeEnum.C;
            }

            m_Player1OnServer.TheEnum.Value = NetworkVariableTest.SomeEnum.C;
            m_Player1OnServer.TheEnum.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(VerifyStructure));
        }

        [Test]
        public void TestINetworkSerializableClassCallsNetworkSerialize([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);
            TestClass.NetworkSerializeCalledOnWrite = false;
            TestClass.NetworkSerializeCalledOnRead = false;
            m_Player1OnServer.TheClass.Value = new TestClass
            {
                SomeBool = true,
                SomeInt = 32
            };

            static bool VerifyCallback() => TestClass.NetworkSerializeCalledOnWrite && TestClass.NetworkSerializeCalledOnRead;

            // Wait for the client-side to notify it is finished initializing and spawning.
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(VerifyCallback));
        }

        [Test]
        public void TestINetworkSerializableStructCallsNetworkSerialize([Values] HostOrServer useHost)
        {
            InitializeServerAndClients(useHost);
            TestStruct.NetworkSerializeCalledOnWrite = false;
            TestStruct.NetworkSerializeCalledOnRead = false;
            m_Player1OnServer.TheStruct.Value = new TestStruct() { SomeInt = k_TestUInt, SomeBool = false };

            static bool VerifyCallback() => TestStruct.NetworkSerializeCalledOnWrite && TestStruct.NetworkSerializeCalledOnRead;

            // Wait for the client-side to notify it is finished initializing and spawning.
            Assert.True(WaitForConditionOrTimeOutWithTimeTravel(VerifyCallback));
        }

        [Test]
        public void TestCustomGenericSerialization()
        {
            // Just verifies that the ILPP codegen initialized these values for this type.
            Assert.AreEqual(typeof(UnmanagedTypeSerializer<TypeReferencedOnlyInCustomSerialization1>), NetworkVariableSerialization<TypeReferencedOnlyInCustomSerialization1>.Serializer.GetType());
            Assert.AreEqual(typeof(UnmanagedTypeSerializer<TypeReferencedOnlyInCustomSerialization2>), NetworkVariableSerialization<TypeReferencedOnlyInCustomSerialization2>.Serializer.GetType());
            Assert.AreEqual(typeof(UnmanagedTypeSerializer<TypeReferencedOnlyInCustomSerialization3>), NetworkVariableSerialization<TypeReferencedOnlyInCustomSerialization3>.Serializer.GetType());
            Assert.AreEqual(typeof(UnmanagedTypeSerializer<TypeReferencedOnlyInCustomSerialization4>), NetworkVariableSerialization<TypeReferencedOnlyInCustomSerialization4>.Serializer.GetType());
            Assert.AreEqual(typeof(UnmanagedTypeSerializer<TypeReferencedOnlyInCustomSerialization5>), NetworkVariableSerialization<TypeReferencedOnlyInCustomSerialization5>.Serializer.GetType());
            Assert.AreEqual(typeof(UnmanagedTypeSerializer<TypeReferencedOnlyInCustomSerialization6>), NetworkVariableSerialization<TypeReferencedOnlyInCustomSerialization6>.Serializer.GetType());
            Assert.IsNotNull(NetworkVariableSerialization<TypeReferencedOnlyInCustomSerialization1>.AreEqual);
            Assert.IsNotNull(NetworkVariableSerialization<TypeReferencedOnlyInCustomSerialization2>.AreEqual);
            Assert.IsNotNull(NetworkVariableSerialization<TypeReferencedOnlyInCustomSerialization3>.AreEqual);
            Assert.IsNotNull(NetworkVariableSerialization<TypeReferencedOnlyInCustomSerialization4>.AreEqual);
            Assert.IsNotNull(NetworkVariableSerialization<TypeReferencedOnlyInCustomSerialization5>.AreEqual);
            Assert.IsNotNull(NetworkVariableSerialization<TypeReferencedOnlyInCustomSerialization6>.AreEqual);

            // Verify no issues with generic values...

            Assert.AreEqual(typeof(UnmanagedArraySerializer<TypeReferencedOnlyInCustomSerialization1>), NetworkVariableSerialization<NativeArray<TypeReferencedOnlyInCustomSerialization1>>.Serializer.GetType());
            Assert.AreEqual(typeof(UnmanagedArraySerializer<TypeReferencedOnlyInCustomSerialization2>), NetworkVariableSerialization<NativeArray<TypeReferencedOnlyInCustomSerialization2>>.Serializer.GetType());
            Assert.AreEqual(typeof(UnmanagedArraySerializer<TypeReferencedOnlyInCustomSerialization3>), NetworkVariableSerialization<NativeArray<TypeReferencedOnlyInCustomSerialization3>>.Serializer.GetType());
            Assert.AreEqual(typeof(UnmanagedArraySerializer<TypeReferencedOnlyInCustomSerialization4>), NetworkVariableSerialization<NativeArray<TypeReferencedOnlyInCustomSerialization4>>.Serializer.GetType());
            Assert.AreEqual(typeof(UnmanagedArraySerializer<TypeReferencedOnlyInCustomSerialization5>), NetworkVariableSerialization<NativeArray<TypeReferencedOnlyInCustomSerialization5>>.Serializer.GetType());
            Assert.AreEqual(typeof(UnmanagedArraySerializer<TypeReferencedOnlyInCustomSerialization6>), NetworkVariableSerialization<NativeArray<TypeReferencedOnlyInCustomSerialization6>>.Serializer.GetType());
            Assert.IsNotNull(NetworkVariableSerialization<NativeArray<TypeReferencedOnlyInCustomSerialization1>>.AreEqual);
            Assert.IsNotNull(NetworkVariableSerialization<NativeArray<TypeReferencedOnlyInCustomSerialization2>>.AreEqual);
            Assert.IsNotNull(NetworkVariableSerialization<NativeArray<TypeReferencedOnlyInCustomSerialization3>>.AreEqual);
            Assert.IsNotNull(NetworkVariableSerialization<NativeArray<TypeReferencedOnlyInCustomSerialization4>>.AreEqual);
            Assert.IsNotNull(NetworkVariableSerialization<NativeArray<TypeReferencedOnlyInCustomSerialization5>>.AreEqual);
            Assert.IsNotNull(NetworkVariableSerialization<NativeArray<TypeReferencedOnlyInCustomSerialization6>>.AreEqual);
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
            UserNetworkVariableSerialization<string>.DuplicateValue = null;
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
            UserNetworkVariableSerialization<string>.DuplicateValue = (in string a, ref string b) =>
            {
                b = string.Copy(a);
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
                UserNetworkVariableSerialization<string>.DuplicateValue = null;
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
            UserNetworkVariableSerialization<Guid>.DuplicateValue = null;
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
        public void TestTypesReferencedInSubclassSerializeSuccessfully()
        {
            var variable = new NetworkVariableSubclass<TemplatedValueOnlyReferencedByNetworkVariableSubclass<int>>();
            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            var value = new TemplatedValueOnlyReferencedByNetworkVariableSubclass<int> { Value = 12345 };
            variable.Value = value;
            variable.WriteField(writer);
            variable.Value = new TemplatedValueOnlyReferencedByNetworkVariableSubclass<int> { Value = 54321 };

            using var reader = new FastBufferReader(writer, Allocator.None);
            variable.ReadField(reader);
            Assert.AreEqual(value.Value, variable.Value.Value);
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
            UserNetworkVariableSerialization<Guid>.DuplicateValue = (in Guid a, ref Guid b) =>
            {
                b = a;
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
                UserNetworkVariableSerialization<Guid>.DuplicateValue = null;
            }
        }
        [Test]
        public void WhenCreatingAnArrayOfNetVars_InitializingVariablesDoesNotThrowAnException()
        {
            var testObjPrefab = CreateNetworkObjectPrefab($"NetVarArrayPrefab");
            var testComp = testObjPrefab.AddComponent<NetworkBehaviourWithNetVarArray>();
            testComp.InitializeVariables();

            // Verify all variables were initialized
            Assert.AreEqual(testComp.InitializedFieldCount, 5);

            Assert.NotNull(testComp.Int0.GetBehaviour());
            Assert.NotNull(testComp.Int1.GetBehaviour());
            Assert.NotNull(testComp.Int2.GetBehaviour());
            Assert.NotNull(testComp.Int3.GetBehaviour());
            Assert.NotNull(testComp.Int4.GetBehaviour());

            Assert.NotNull(testComp.Int0.Name);
            Assert.NotNull(testComp.Int1.Name);
            Assert.NotNull(testComp.Int2.Name);
            Assert.NotNull(testComp.Int3.Name);
            Assert.NotNull(testComp.Int4.Name);

            Assert.AreNotEqual("", testComp.Int0.Name);
            Assert.AreNotEqual("", testComp.Int1.Name);
            Assert.AreNotEqual("", testComp.Int2.Name);
            Assert.AreNotEqual("", testComp.Int3.Name);
            Assert.AreNotEqual("", testComp.Int4.Name);

            Assert.AreSame(testComp.AllInts[0], testComp.Int0);
            Assert.AreSame(testComp.AllInts[1], testComp.Int1);
            Assert.AreSame(testComp.AllInts[2], testComp.Int2);
            Assert.AreSame(testComp.AllInts[3], testComp.Int3);
            Assert.AreSame(testComp.AllInts[4], testComp.Int4);
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

            serverVariable.Dispose();
            serverVariable.Value = changedValue;
            Assert.IsFalse(NetworkVariableSerialization<NativeArray<T>>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));

            writer.Seek(0);

            serverVariable.WriteDelta(writer);

            Assert.IsFalse(NetworkVariableSerialization<NativeArray<T>>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));

            using var reader2 = new FastBufferReader(writer, Allocator.None);
            clientVariable.ReadDelta(reader2, false);
            Assert.IsTrue(NetworkVariableSerialization<NativeArray<T>>.AreEqual(ref serverVariable.RefValue(), ref clientVariable.RefValue()));

            serverVariable.ResetDirty();
            Assert.IsFalse(serverVariable.IsDirty());
            var cachedValue = changedValue[0];
            changedValue[0] = testValue[0];
            Assert.IsTrue(serverVariable.IsDirty());
            serverVariable.ResetDirty();
            Assert.IsFalse(serverVariable.IsDirty());
            changedValue[0] = cachedValue;
            Assert.IsTrue(serverVariable.IsDirty());


            serverVariable.Dispose();
            clientVariable.Dispose();
        }

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
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

            serverVariable.Dispose();
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

            serverVariable.ResetDirty();
            Assert.IsFalse(serverVariable.IsDirty());
            serverVariable.Value.Clear();
            Assert.IsTrue(serverVariable.IsDirty());

            serverVariable.ResetDirty();

            Assert.IsFalse(serverVariable.IsDirty());
            serverVariable.Value.Add(default);
            Assert.IsTrue(serverVariable.IsDirty());

            serverVariable.Dispose();
            clientVariable.Dispose();
        }
#endif
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
                TestValueType(int.MinValue + 5, int.MaxValue);
            }
            else if (testType == typeof(uint))
            {
                TestValueType(uint.MinValue + 5, uint.MaxValue);
            }
            else if (testType == typeof(long))
            {
                TestValueType(long.MinValue + 5, long.MaxValue);
            }
            else if (testType == typeof(ulong))
            {
                TestValueType(ulong.MinValue + 5, ulong.MaxValue);
            }
            else if (testType == typeof(bool))
            {
                TestValueType(true, false);
            }
            else if (testType == typeof(char))
            {
                TestValueType('z', ' ');
            }
            else if (testType == typeof(float))
            {
                TestValueType(float.MinValue + 5.12345678f, float.MaxValue);
            }
            else if (testType == typeof(double))
            {
                TestValueType(double.MinValue + 5.12345678, double.MaxValue);
            }
            else if (testType == typeof(ByteEnum))
            {
                TestValueType(ByteEnum.B, ByteEnum.C);
            }
            else if (testType == typeof(SByteEnum))
            {
                TestValueType(SByteEnum.B, SByteEnum.C);
            }
            else if (testType == typeof(ShortEnum))
            {
                TestValueType(ShortEnum.B, ShortEnum.C);
            }
            else if (testType == typeof(UShortEnum))
            {
                TestValueType(UShortEnum.B, UShortEnum.C);
            }
            else if (testType == typeof(IntEnum))
            {
                TestValueType(IntEnum.B, IntEnum.C);
            }
            else if (testType == typeof(UIntEnum))
            {
                TestValueType(UIntEnum.B, UIntEnum.C);
            }
            else if (testType == typeof(LongEnum))
            {
                TestValueType(LongEnum.B, LongEnum.C);
            }
            else if (testType == typeof(ULongEnum))
            {
                TestValueType(ULongEnum.B, ULongEnum.C);
            }
            else if (testType == typeof(Vector2))
            {
                TestValueType(
                    new Vector2(5, 10),
                    new Vector2(15, 20));
            }
            else if (testType == typeof(Vector3))
            {
                TestValueType(
                    new Vector3(5, 10, 15),
                    new Vector3(20, 25, 30));
            }
            else if (testType == typeof(Vector2Int))
            {
                TestValueType(
                    new Vector2Int(5, 10),
                    new Vector2Int(15, 20));
            }
            else if (testType == typeof(Vector3Int))
            {
                TestValueType(
                    new Vector3Int(5, 10, 15),
                    new Vector3Int(20, 25, 30));
            }
            else if (testType == typeof(Vector4))
            {
                TestValueType(
                    new Vector4(5, 10, 15, 20),
                    new Vector4(25, 30, 35, 40));
            }
            else if (testType == typeof(Quaternion))
            {
                TestValueType(
                    new Quaternion(5, 10, 15, 20),
                    new Quaternion(25, 30, 35, 40));
            }
            else if (testType == typeof(Color))
            {
                TestValueType(
                    new Color(1, 0, 0),
                    new Color(0, 1, 1));
            }
            else if (testType == typeof(Color32))
            {
                TestValueType(
                    new Color32(255, 0, 0, 128),
                    new Color32(0, 255, 255, 255));
            }
            else if (testType == typeof(Ray))
            {
                TestValueType(
                    new Ray(new Vector3(0, 1, 2), new Vector3(3, 4, 5)),
                    new Ray(new Vector3(6, 7, 8), new Vector3(9, 10, 11)));
            }
            else if (testType == typeof(Ray2D))
            {
                TestValueType(
                    new Ray2D(new Vector2(0, 1), new Vector2(2, 3)),
                    new Ray2D(new Vector2(4, 5), new Vector2(6, 7)));
            }
            else if (testType == typeof(NetworkVariableTestStruct))
            {
                TestValueType(NetworkVariableTestStruct.GetTestStruct(), NetworkVariableTestStruct.GetTestStruct());
            }
            else if (testType == typeof(FixedString32Bytes))
            {
                TestValueType(new FixedString32Bytes("foobar"), new FixedString32Bytes("12345678901234567890123456789"));
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
                TestValueTypeNativeArray(
                    new NativeArray<byte>(new byte[] { byte.MinValue + 5, byte.MaxValue }, Allocator.Temp),
                    new NativeArray<byte>(new byte[] { 0, byte.MinValue + 10, byte.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(sbyte))
            {
                TestValueTypeNativeArray(
                    new NativeArray<sbyte>(new sbyte[] { sbyte.MinValue + 5, sbyte.MaxValue }, Allocator.Temp),
                    new NativeArray<sbyte>(new sbyte[] { 0, sbyte.MinValue + 10, sbyte.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(short))
            {
                TestValueTypeNativeArray(
                    new NativeArray<short>(new short[] { short.MinValue + 5, short.MaxValue }, Allocator.Temp),
                    new NativeArray<short>(new short[] { 0, short.MinValue + 10, short.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(ushort))
            {
                TestValueTypeNativeArray(
                    new NativeArray<ushort>(new ushort[] { ushort.MinValue + 5, ushort.MaxValue }, Allocator.Temp),
                    new NativeArray<ushort>(new ushort[] { 0, ushort.MinValue + 10, ushort.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(int))
            {
                TestValueTypeNativeArray(
                    new NativeArray<int>(new int[] { int.MinValue + 5, int.MaxValue }, Allocator.Temp),
                    new NativeArray<int>(new int[] { 0, int.MinValue + 10, int.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(uint))
            {
                TestValueTypeNativeArray(
                    new NativeArray<uint>(new uint[] { uint.MinValue + 5, uint.MaxValue }, Allocator.Temp),
                    new NativeArray<uint>(new uint[] { 0, uint.MinValue + 10, uint.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(long))
            {
                TestValueTypeNativeArray(
                    new NativeArray<long>(new long[] { long.MinValue + 5, long.MaxValue }, Allocator.Temp),
                    new NativeArray<long>(new long[] { 0, long.MinValue + 10, long.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(ulong))
            {
                TestValueTypeNativeArray(
                    new NativeArray<ulong>(new ulong[] { ulong.MinValue + 5, ulong.MaxValue }, Allocator.Temp),
                    new NativeArray<ulong>(new ulong[] { 0, ulong.MinValue + 10, ulong.MaxValue - 10 }, Allocator.Temp));
            }
            else if (testType == typeof(bool))
            {
                TestValueTypeNativeArray(
                    new NativeArray<bool>(new bool[] { true, false, true }, Allocator.Temp),
                    new NativeArray<bool>(new bool[] { false, true, false, true, false }, Allocator.Temp));
            }
            else if (testType == typeof(char))
            {
                TestValueTypeNativeArray(
                    new NativeArray<char>(new char[] { 'z', ' ', '?' }, Allocator.Temp),
                    new NativeArray<char>(new char[] { 'n', 'e', 'w', ' ', 'v', 'a', 'l', 'u', 'e' }, Allocator.Temp));
            }
            else if (testType == typeof(float))
            {
                TestValueTypeNativeArray(
                    new NativeArray<float>(new float[] { float.MinValue + 5.12345678f, float.MaxValue }, Allocator.Temp),
                    new NativeArray<float>(new float[] { 0, float.MinValue + 10.987654321f, float.MaxValue - 10.135792468f }, Allocator.Temp));
            }
            else if (testType == typeof(double))
            {
                TestValueTypeNativeArray(
                    new NativeArray<double>(new double[] { double.MinValue + 5.12345678, double.MaxValue }, Allocator.Temp),
                    new NativeArray<double>(new double[] { 0, double.MinValue + 10.987654321, double.MaxValue - 10.135792468 }, Allocator.Temp));
            }
            else if (testType == typeof(ByteEnum))
            {
                TestValueTypeNativeArray(
                    new NativeArray<ByteEnum>(new ByteEnum[] { ByteEnum.C, ByteEnum.B, ByteEnum.A }, Allocator.Temp),
                    new NativeArray<ByteEnum>(new ByteEnum[] { ByteEnum.B, ByteEnum.C, ByteEnum.B, ByteEnum.A, ByteEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(SByteEnum))
            {
                TestValueTypeNativeArray(
                    new NativeArray<SByteEnum>(new SByteEnum[] { SByteEnum.C, SByteEnum.B, SByteEnum.A }, Allocator.Temp),
                    new NativeArray<SByteEnum>(new SByteEnum[] { SByteEnum.B, SByteEnum.C, SByteEnum.B, SByteEnum.A, SByteEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(ShortEnum))
            {
                TestValueTypeNativeArray(
                    new NativeArray<ShortEnum>(new ShortEnum[] { ShortEnum.C, ShortEnum.B, ShortEnum.A }, Allocator.Temp),
                    new NativeArray<ShortEnum>(new ShortEnum[] { ShortEnum.B, ShortEnum.C, ShortEnum.B, ShortEnum.A, ShortEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(UShortEnum))
            {
                TestValueTypeNativeArray(
                    new NativeArray<UShortEnum>(new UShortEnum[] { UShortEnum.C, UShortEnum.B, UShortEnum.A }, Allocator.Temp),
                    new NativeArray<UShortEnum>(new UShortEnum[] { UShortEnum.B, UShortEnum.C, UShortEnum.B, UShortEnum.A, UShortEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(IntEnum))
            {
                TestValueTypeNativeArray(
                    new NativeArray<IntEnum>(new IntEnum[] { IntEnum.C, IntEnum.B, IntEnum.A }, Allocator.Temp),
                    new NativeArray<IntEnum>(new IntEnum[] { IntEnum.B, IntEnum.C, IntEnum.B, IntEnum.A, IntEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(UIntEnum))
            {
                TestValueTypeNativeArray(
                    new NativeArray<UIntEnum>(new UIntEnum[] { UIntEnum.C, UIntEnum.B, UIntEnum.A }, Allocator.Temp),
                    new NativeArray<UIntEnum>(new UIntEnum[] { UIntEnum.B, UIntEnum.C, UIntEnum.B, UIntEnum.A, UIntEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(LongEnum))
            {
                TestValueTypeNativeArray(
                    new NativeArray<LongEnum>(new LongEnum[] { LongEnum.C, LongEnum.B, LongEnum.A }, Allocator.Temp),
                    new NativeArray<LongEnum>(new LongEnum[] { LongEnum.B, LongEnum.C, LongEnum.B, LongEnum.A, LongEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(ULongEnum))
            {
                TestValueTypeNativeArray(
                    new NativeArray<ULongEnum>(new ULongEnum[] { ULongEnum.C, ULongEnum.B, ULongEnum.A }, Allocator.Temp),
                    new NativeArray<ULongEnum>(new ULongEnum[] { ULongEnum.B, ULongEnum.C, ULongEnum.B, ULongEnum.A, ULongEnum.C }, Allocator.Temp));
            }
            else if (testType == typeof(Vector2))
            {
                TestValueTypeNativeArray(
                    new NativeArray<Vector2>(new Vector2[] { new Vector2(5, 10), new Vector2(15, 20) }, Allocator.Temp),
                    new NativeArray<Vector2>(new Vector2[] { new Vector2(25, 30), new Vector2(35, 40), new Vector2(45, 50) }, Allocator.Temp));
            }
            else if (testType == typeof(Vector3))
            {
                TestValueTypeNativeArray(
                    new NativeArray<Vector3>(new Vector3[] { new Vector3(5, 10, 15), new Vector3(20, 25, 30) }, Allocator.Temp),
                    new NativeArray<Vector3>(new Vector3[] { new Vector3(35, 40, 45), new Vector3(50, 55, 60), new Vector3(65, 70, 75) }, Allocator.Temp));
            }
            else if (testType == typeof(Vector2Int))
            {
                TestValueTypeNativeArray(
                    new NativeArray<Vector2Int>(new Vector2Int[] { new Vector2Int(5, 10), new Vector2Int(15, 20) }, Allocator.Temp),
                    new NativeArray<Vector2Int>(new Vector2Int[] { new Vector2Int(25, 30), new Vector2Int(35, 40), new Vector2Int(45, 50) }, Allocator.Temp));
            }
            else if (testType == typeof(Vector3Int))
            {
                TestValueTypeNativeArray(
                    new NativeArray<Vector3Int>(new Vector3Int[] { new Vector3Int(5, 10, 15), new Vector3Int(20, 25, 30) }, Allocator.Temp),
                    new NativeArray<Vector3Int>(new Vector3Int[] { new Vector3Int(35, 40, 45), new Vector3Int(50, 55, 60), new Vector3Int(65, 70, 75) }, Allocator.Temp));
            }
            else if (testType == typeof(Vector4))
            {
                TestValueTypeNativeArray(
                    new NativeArray<Vector4>(new Vector4[] { new Vector4(5, 10, 15, 20), new Vector4(25, 30, 35, 40) }, Allocator.Temp),
                    new NativeArray<Vector4>(new Vector4[] { new Vector4(45, 50, 55, 60), new Vector4(65, 70, 75, 80), new Vector4(85, 90, 95, 100) }, Allocator.Temp));
            }
            else if (testType == typeof(Quaternion))
            {
                TestValueTypeNativeArray(
                    new NativeArray<Quaternion>(new Quaternion[] { new Quaternion(5, 10, 15, 20), new Quaternion(25, 30, 35, 40) }, Allocator.Temp),
                    new NativeArray<Quaternion>(new Quaternion[] { new Quaternion(45, 50, 55, 60), new Quaternion(65, 70, 75, 80), new Quaternion(85, 90, 95, 100) }, Allocator.Temp));
            }
            else if (testType == typeof(Color))
            {
                TestValueTypeNativeArray(
                    new NativeArray<Color>(new Color[] { new Color(.5f, .10f, .15f), new Color(.20f, .25f, .30f) }, Allocator.Temp),
                    new NativeArray<Color>(new Color[] { new Color(.35f, .40f, .45f), new Color(.50f, .55f, .60f), new Color(.65f, .70f, .75f) }, Allocator.Temp));
            }
            else if (testType == typeof(Color32))
            {
                TestValueTypeNativeArray(
                    new NativeArray<Color32>(new Color32[] { new Color32(5, 10, 15, 20), new Color32(25, 30, 35, 40) }, Allocator.Temp),
                    new NativeArray<Color32>(new Color32[] { new Color32(45, 50, 55, 60), new Color32(65, 70, 75, 80), new Color32(85, 90, 95, 100) }, Allocator.Temp));
            }
            else if (testType == typeof(Ray))
            {
                TestValueTypeNativeArray(
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
                TestValueTypeNativeArray(
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
                TestValueTypeNativeArray(
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
                TestValueTypeNativeArray(
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

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
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
                TestValueTypeNativeList(
                    new NativeList<byte>(Allocator.Temp) { byte.MinValue + 5, byte.MaxValue },
                    new NativeList<byte>(Allocator.Temp) { 0, byte.MinValue + 10, byte.MaxValue - 10 });
            }
            else if (testType == typeof(sbyte))
            {
                TestValueTypeNativeList(
                    new NativeList<sbyte>(Allocator.Temp) { sbyte.MinValue + 5, sbyte.MaxValue },
                    new NativeList<sbyte>(Allocator.Temp) { 0, sbyte.MinValue + 10, sbyte.MaxValue - 10 });
            }
            else if (testType == typeof(short))
            {
                TestValueTypeNativeList(
                    new NativeList<short>(Allocator.Temp) { short.MinValue + 5, short.MaxValue },
                    new NativeList<short>(Allocator.Temp) { 0, short.MinValue + 10, short.MaxValue - 10 });
            }
            else if (testType == typeof(ushort))
            {
                TestValueTypeNativeList(
                    new NativeList<ushort>(Allocator.Temp) { ushort.MinValue + 5, ushort.MaxValue },
                    new NativeList<ushort>(Allocator.Temp) { 0, ushort.MinValue + 10, ushort.MaxValue - 10 });
            }
            else if (testType == typeof(int))
            {
                TestValueTypeNativeList(
                    new NativeList<int>(Allocator.Temp) { int.MinValue + 5, int.MaxValue },
                    new NativeList<int>(Allocator.Temp) { 0, int.MinValue + 10, int.MaxValue - 10 });
            }
            else if (testType == typeof(uint))
            {
                TestValueTypeNativeList(
                    new NativeList<uint>(Allocator.Temp) { uint.MinValue + 5, uint.MaxValue },
                    new NativeList<uint>(Allocator.Temp) { 0, uint.MinValue + 10, uint.MaxValue - 10 });
            }
            else if (testType == typeof(long))
            {
                TestValueTypeNativeList(
                    new NativeList<long>(Allocator.Temp) { long.MinValue + 5, long.MaxValue },
                    new NativeList<long>(Allocator.Temp) { 0, long.MinValue + 10, long.MaxValue - 10 });
            }
            else if (testType == typeof(ulong))
            {
                TestValueTypeNativeList(
                    new NativeList<ulong>(Allocator.Temp) { ulong.MinValue + 5, ulong.MaxValue },
                    new NativeList<ulong>(Allocator.Temp) { 0, ulong.MinValue + 10, ulong.MaxValue - 10 });
            }
            else if (testType == typeof(bool))
            {
                TestValueTypeNativeList(
                    new NativeList<bool>(Allocator.Temp) { true, false, true },
                    new NativeList<bool>(Allocator.Temp) { false, true, false, true, false });
            }
            else if (testType == typeof(char))
            {
                TestValueTypeNativeList(
                    new NativeList<char>(Allocator.Temp) { 'z', ' ', '?' },
                    new NativeList<char>(Allocator.Temp) { 'n', 'e', 'w', ' ', 'v', 'a', 'l', 'u', 'e' });
            }
            else if (testType == typeof(float))
            {
                TestValueTypeNativeList(
                    new NativeList<float>(Allocator.Temp) { float.MinValue + 5.12345678f, float.MaxValue },
                    new NativeList<float>(Allocator.Temp) { 0, float.MinValue + 10.987654321f, float.MaxValue - 10.135792468f });
            }
            else if (testType == typeof(double))
            {
                TestValueTypeNativeList(
                    new NativeList<double>(Allocator.Temp) { double.MinValue + 5.12345678, double.MaxValue },
                    new NativeList<double>(Allocator.Temp) { 0, double.MinValue + 10.987654321, double.MaxValue - 10.135792468 });
            }
            else if (testType == typeof(ByteEnum))
            {
                TestValueTypeNativeList(
                    new NativeList<ByteEnum>(Allocator.Temp) { ByteEnum.C, ByteEnum.B, ByteEnum.A },
                    new NativeList<ByteEnum>(Allocator.Temp) { ByteEnum.B, ByteEnum.C, ByteEnum.B, ByteEnum.A, ByteEnum.C });
            }
            else if (testType == typeof(SByteEnum))
            {
                TestValueTypeNativeList(
                    new NativeList<SByteEnum>(Allocator.Temp) { SByteEnum.C, SByteEnum.B, SByteEnum.A },
                    new NativeList<SByteEnum>(Allocator.Temp) { SByteEnum.B, SByteEnum.C, SByteEnum.B, SByteEnum.A, SByteEnum.C });
            }
            else if (testType == typeof(ShortEnum))
            {
                TestValueTypeNativeList(
                    new NativeList<ShortEnum>(Allocator.Temp) { ShortEnum.C, ShortEnum.B, ShortEnum.A },
                    new NativeList<ShortEnum>(Allocator.Temp) { ShortEnum.B, ShortEnum.C, ShortEnum.B, ShortEnum.A, ShortEnum.C });
            }
            else if (testType == typeof(UShortEnum))
            {
                TestValueTypeNativeList(
                    new NativeList<UShortEnum>(Allocator.Temp) { UShortEnum.C, UShortEnum.B, UShortEnum.A },
                    new NativeList<UShortEnum>(Allocator.Temp) { UShortEnum.B, UShortEnum.C, UShortEnum.B, UShortEnum.A, UShortEnum.C });
            }
            else if (testType == typeof(IntEnum))
            {
                TestValueTypeNativeList(
                    new NativeList<IntEnum>(Allocator.Temp) { IntEnum.C, IntEnum.B, IntEnum.A },
                    new NativeList<IntEnum>(Allocator.Temp) { IntEnum.B, IntEnum.C, IntEnum.B, IntEnum.A, IntEnum.C });
            }
            else if (testType == typeof(UIntEnum))
            {
                TestValueTypeNativeList(
                    new NativeList<UIntEnum>(Allocator.Temp) { UIntEnum.C, UIntEnum.B, UIntEnum.A },
                    new NativeList<UIntEnum>(Allocator.Temp) { UIntEnum.B, UIntEnum.C, UIntEnum.B, UIntEnum.A, UIntEnum.C });
            }
            else if (testType == typeof(LongEnum))
            {
                TestValueTypeNativeList(
                    new NativeList<LongEnum>(Allocator.Temp) { LongEnum.C, LongEnum.B, LongEnum.A },
                    new NativeList<LongEnum>(Allocator.Temp) { LongEnum.B, LongEnum.C, LongEnum.B, LongEnum.A, LongEnum.C });
            }
            else if (testType == typeof(ULongEnum))
            {
                TestValueTypeNativeList(
                    new NativeList<ULongEnum>(Allocator.Temp) { ULongEnum.C, ULongEnum.B, ULongEnum.A },
                    new NativeList<ULongEnum>(Allocator.Temp) { ULongEnum.B, ULongEnum.C, ULongEnum.B, ULongEnum.A, ULongEnum.C });
            }
            else if (testType == typeof(Vector2))
            {
                TestValueTypeNativeList(
                    new NativeList<Vector2>(Allocator.Temp) { new Vector2(5, 10), new Vector2(15, 20) },
                    new NativeList<Vector2>(Allocator.Temp) { new Vector2(25, 30), new Vector2(35, 40), new Vector2(45, 50) });
            }
            else if (testType == typeof(Vector3))
            {
                TestValueTypeNativeList(
                    new NativeList<Vector3>(Allocator.Temp) { new Vector3(5, 10, 15), new Vector3(20, 25, 30) },
                    new NativeList<Vector3>(Allocator.Temp) { new Vector3(35, 40, 45), new Vector3(50, 55, 60), new Vector3(65, 70, 75) });
            }
            else if (testType == typeof(Vector2Int))
            {
                TestValueTypeNativeList(
                    new NativeList<Vector2Int>(Allocator.Temp) { new Vector2Int(5, 10), new Vector2Int(15, 20) },
                    new NativeList<Vector2Int>(Allocator.Temp) { new Vector2Int(25, 30), new Vector2Int(35, 40), new Vector2Int(45, 50) });
            }
            else if (testType == typeof(Vector3Int))
            {
                TestValueTypeNativeList(
                    new NativeList<Vector3Int>(Allocator.Temp) { new Vector3Int(5, 10, 15), new Vector3Int(20, 25, 30) },
                    new NativeList<Vector3Int>(Allocator.Temp) { new Vector3Int(35, 40, 45), new Vector3Int(50, 55, 60), new Vector3Int(65, 70, 75) });
            }
            else if (testType == typeof(Vector4))
            {
                TestValueTypeNativeList(
                    new NativeList<Vector4>(Allocator.Temp) { new Vector4(5, 10, 15, 20), new Vector4(25, 30, 35, 40) },
                    new NativeList<Vector4>(Allocator.Temp) { new Vector4(45, 50, 55, 60), new Vector4(65, 70, 75, 80), new Vector4(85, 90, 95, 100) });
            }
            else if (testType == typeof(Quaternion))
            {
                TestValueTypeNativeList(
                    new NativeList<Quaternion>(Allocator.Temp) { new Quaternion(5, 10, 15, 20), new Quaternion(25, 30, 35, 40) },
                    new NativeList<Quaternion>(Allocator.Temp) { new Quaternion(45, 50, 55, 60), new Quaternion(65, 70, 75, 80), new Quaternion(85, 90, 95, 100) });
            }
            else if (testType == typeof(Color))
            {
                TestValueTypeNativeList(
                    new NativeList<Color>(Allocator.Temp) { new Color(.5f, .10f, .15f), new Color(.20f, .25f, .30f) },
                    new NativeList<Color>(Allocator.Temp) { new Color(.35f, .40f, .45f), new Color(.50f, .55f, .60f), new Color(.65f, .70f, .75f) });
            }
            else if (testType == typeof(Color32))
            {
                TestValueTypeNativeList(
                    new NativeList<Color32>(Allocator.Temp) { new Color32(5, 10, 15, 20), new Color32(25, 30, 35, 40) },
                    new NativeList<Color32>(Allocator.Temp) { new Color32(45, 50, 55, 60), new Color32(65, 70, 75, 80), new Color32(85, 90, 95, 100) });
            }
            else if (testType == typeof(Ray))
            {
                TestValueTypeNativeList(
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
                TestValueTypeNativeList(
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
                TestValueTypeNativeList(
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
                TestValueTypeNativeList(
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
#endif

        [Test]
        public void TestManagedINetworkSerializableNetworkVariablesDeserializeInPlace()
        {
            var variable = new NetworkVariable<ManagedNetworkSerializableType>
            {
                Value = new ManagedNetworkSerializableType
                {
                    InMemoryValue = 1,
                    Ints = new[] { 2, 3, 4 },
                    Str = "five",
                    Embedded = new EmbeddedManagedNetworkSerializableType { Int = 6 }
                }
            };

            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            variable.WriteField(writer);
            Assert.AreEqual(1, variable.Value.InMemoryValue);
            Assert.AreEqual(new[] { 2, 3, 4 }, variable.Value.Ints);
            Assert.AreEqual("five", variable.Value.Str);
            Assert.AreEqual(6, variable.Value.Embedded.Int);
            variable.Value = new ManagedNetworkSerializableType
            {
                InMemoryValue = 10,
                Ints = new[] { 20, 30, 40, 50 },
                Str = "sixty",
                Embedded = new EmbeddedManagedNetworkSerializableType { Int = 60 }
            };

            using var reader = new FastBufferReader(writer, Allocator.None);
            variable.ReadField(reader);
            Assert.AreEqual(10, variable.Value.InMemoryValue, "In-memory value was not the same - in-place deserialization should not change this");
            Assert.AreEqual(new[] { 2, 3, 4 }, variable.Value.Ints, "Ints were not correctly deserialized");
            Assert.AreEqual("five", variable.Value.Str, "Str was not correctly deserialized");
            Assert.AreEqual(6, variable.Value.Embedded.Int, "Embedded int was not correctly deserialized");
        }

        [Test]
        public void TestUnmnagedINetworkSerializableNetworkVariablesDeserializeInPlace()
        {
            var variable = new NetworkVariable<UnmanagedNetworkSerializableType>
            {
                Value = new UnmanagedNetworkSerializableType
                {
                    InMemoryValue = 1,
                    Int = 2,
                    Str = "three"
                }
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

    [TestFixtureSource(nameof(TestDataSource))]
    public class NetworkVariableInheritanceTests : NetcodeIntegrationTest
    {
        public NetworkVariableInheritanceTests(HostOrServer hostOrServer)
            : base(hostOrServer)
        {
        }

        protected override int NumberOfClients => 2;

        public static IEnumerable<TestFixtureData> TestDataSource() =>
            Enum.GetValues(typeof(HostOrServer)).OfType<HostOrServer>().Select(x => new TestFixtureData(x));

        public class ComponentA : NetworkBehaviour
        {
            public NetworkVariable<int> PublicFieldA = new NetworkVariable<int>(1);
            protected NetworkVariable<int> m_ProtectedFieldA = new NetworkVariable<int>(2);
            private NetworkVariable<int> m_PrivateFieldA = new NetworkVariable<int>(3);

            public void ChangeValuesA(int pub, int pro, int pri)
            {
                PublicFieldA.Value = pub;
                m_ProtectedFieldA.Value = pro;
                m_PrivateFieldA.Value = pri;
            }

            public bool CompareValuesA(ComponentA other)
            {
                return PublicFieldA.Value == other.PublicFieldA.Value &&
                    m_ProtectedFieldA.Value == other.m_ProtectedFieldA.Value &&
                    m_PrivateFieldA.Value == other.m_PrivateFieldA.Value;
            }
        }

        public class ComponentB : ComponentA
        {
            public NetworkVariable<int> PublicFieldB = new NetworkVariable<int>(11);
            protected NetworkVariable<int> m_ProtectedFieldB = new NetworkVariable<int>(22);
            private NetworkVariable<int> m_PrivateFieldB = new NetworkVariable<int>(33);

            public void ChangeValuesB(int pub, int pro, int pri)
            {
                PublicFieldB.Value = pub;
                m_ProtectedFieldB.Value = pro;
                m_PrivateFieldB.Value = pri;
            }

            public bool CompareValuesB(ComponentB other)
            {
                return PublicFieldB.Value == other.PublicFieldB.Value &&
                    m_ProtectedFieldB.Value == other.m_ProtectedFieldB.Value &&
                    m_PrivateFieldB.Value == other.m_PrivateFieldB.Value;
            }
        }

        public class ComponentC : ComponentB
        {
            public NetworkVariable<int> PublicFieldC = new NetworkVariable<int>(111);
            protected NetworkVariable<int> m_ProtectedFieldC = new NetworkVariable<int>(222);
            private NetworkVariable<int> m_PrivateFieldC = new NetworkVariable<int>(333);

            public void ChangeValuesC(int pub, int pro, int pri)
            {
                PublicFieldC.Value = pub;
                m_ProtectedFieldA.Value = pro;
                m_PrivateFieldC.Value = pri;
            }

            public bool CompareValuesC(ComponentC other)
            {
                return PublicFieldC.Value == other.PublicFieldC.Value &&
                    m_ProtectedFieldC.Value == other.m_ProtectedFieldC.Value &&
                    m_PrivateFieldC.Value == other.m_PrivateFieldC.Value;
            }
        }

        private GameObject m_TestObjectPrefab;
        private ulong m_TestObjectId = 0;

        protected override void OnServerAndClientsCreated()
        {
            m_TestObjectPrefab = CreateNetworkObjectPrefab($"[{nameof(NetworkVariableInheritanceTests)}.{nameof(m_TestObjectPrefab)}]");
            m_TestObjectPrefab.AddComponent<ComponentA>();
            m_TestObjectPrefab.AddComponent<ComponentB>();
            m_TestObjectPrefab.AddComponent<ComponentC>();
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {
            var serverTestObject = SpawnObject(m_TestObjectPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            m_TestObjectId = serverTestObject.NetworkObjectId;

            var serverTestComponentA = serverTestObject.GetComponent<ComponentA>();
            var serverTestComponentB = serverTestObject.GetComponent<ComponentB>();
            var serverTestComponentC = serverTestObject.GetComponent<ComponentC>();

            serverTestComponentA.ChangeValuesA(1000, 2000, 3000);
            serverTestComponentB.ChangeValuesA(1000, 2000, 3000);
            serverTestComponentB.ChangeValuesB(1100, 2200, 3300);
            serverTestComponentC.ChangeValuesA(1000, 2000, 3000);
            serverTestComponentC.ChangeValuesB(1100, 2200, 3300);
            serverTestComponentC.ChangeValuesC(1110, 2220, 3330);

            yield return WaitForTicks(m_ServerNetworkManager, 2);
        }

        private bool CheckTestObjectComponentValuesOnAll()
        {
            var serverTestObject = m_ServerNetworkManager.SpawnManager.SpawnedObjects[m_TestObjectId];
            var serverTestComponentA = serverTestObject.GetComponent<ComponentA>();
            var serverTestComponentB = serverTestObject.GetComponent<ComponentB>();
            var serverTestComponentC = serverTestObject.GetComponent<ComponentC>();
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                var clientTestObject = clientNetworkManager.SpawnManager.SpawnedObjects[m_TestObjectId];
                var clientTestComponentA = clientTestObject.GetComponent<ComponentA>();
                var clientTestComponentB = clientTestObject.GetComponent<ComponentB>();
                var clientTestComponentC = clientTestObject.GetComponent<ComponentC>();
                if (!serverTestComponentA.CompareValuesA(clientTestComponentA) ||
                    !serverTestComponentB.CompareValuesA(clientTestComponentB) ||
                    !serverTestComponentB.CompareValuesB(clientTestComponentB) ||
                    !serverTestComponentC.CompareValuesA(clientTestComponentC) ||
                    !serverTestComponentC.CompareValuesB(clientTestComponentC) ||
                    !serverTestComponentC.CompareValuesC(clientTestComponentC))
                {
                    return false;
                }
            }

            return true;
        }

        [UnityTest]
        public IEnumerator TestInheritedFields()
        {
            yield return WaitForConditionOrTimeOut(CheckTestObjectComponentValuesOnAll);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, nameof(CheckTestObjectComponentValuesOnAll));
        }
    }

    public class NetvarDespawnShutdown : NetworkBehaviour
    {
        private NetworkVariable<int> m_IntNetworkVariable = new NetworkVariable<int>();
        private NetworkList<int> m_IntList;

        private void Awake()
        {
            m_IntList = new NetworkList<int>();
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                m_IntNetworkVariable.Value = 5;
                for (int i = 0; i < 10; i++)
                {
                    m_IntList.Add(i);
                }
            }
            base.OnNetworkDespawn();
        }
    }

    /// <summary>
    /// Validates that setting values for NetworkVariable or NetworkList during the
    /// OnNetworkDespawn method will not cause an exception to occur.
    /// </summary>
    public class NetworkVariableModifyOnNetworkDespawn : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_TestPrefab;

        protected override void OnServerAndClientsCreated()
        {
            m_TestPrefab = CreateNetworkObjectPrefab("NetVarDespawn");
            m_TestPrefab.AddComponent<NetvarDespawnShutdown>();
            base.OnServerAndClientsCreated();
        }

        private bool OnClientSpawnedTestPrefab(ulong networkObjectId)
        {
            var clientId = m_ClientNetworkManagers[0].LocalClientId;
            if (!s_GlobalNetworkObjects.ContainsKey(clientId))
            {
                return false;
            }

            if (!s_GlobalNetworkObjects[clientId].ContainsKey(networkObjectId))
            {
                return false;
            }

            return true;
        }

        [UnityTest]
        public IEnumerator ModifyNetworkVariableOrListOnNetworkDespawn()
        {
            var instance = SpawnObject(m_TestPrefab, m_ServerNetworkManager);
            yield return WaitForConditionOrTimeOut(() => OnClientSpawnedTestPrefab(instance.GetComponent<NetworkObject>().NetworkObjectId));
            m_ServerNetworkManager.Shutdown();
            // As long as no excetptions occur, the test passes.
        }
    }
}
