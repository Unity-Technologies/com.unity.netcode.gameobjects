using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace DocumentationCodeSamples
{
    #region TestMyCustomNetworkVariable
    // Using MyCustomNetworkVariable within a NetworkBehaviour
    internal class TestMyCustomNetworkVariable : NetworkBehaviour
    {
        public MyCustomNetworkVariable CustomNetworkVariable = new MyCustomNetworkVariable();
        public MyCustomGenericNetworkVariable<int> CustomGenericNetworkVariable = new MyCustomGenericNetworkVariable<int>();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                for (int i = 0; i < 4; i++)
                {
                    var someData = new SomeData
                    {
                        SomeFloatData = i,
                        SomeIntData = i
                    };
                    someData.SomeListOfValues.Add((ulong)i + 1000000);
                    someData.SomeListOfValues.Add((ulong)i + 2000000);
                    someData.SomeListOfValues.Add((ulong)i + 3000000);
                    CustomNetworkVariable.SomeDataToSynchronize.Add(someData);
                    CustomNetworkVariable.SetDirty(true);

                    CustomGenericNetworkVariable.SomeDataToSynchronize.Add(i);
                    CustomGenericNetworkVariable.SetDirty(true);
                }
            }
        }
    }

    /// <summary>
    /// Bare minimum example of NetworkVariableBase derived class
    /// </summary>
    [Serializable]
    public class MyCustomNetworkVariable : NetworkVariableBase
    {
        /// <summary>
        /// Managed list of class instances
        /// </summary>
        internal List<SomeData> SomeDataToSynchronize = new List<SomeData>();

        /// <summary>
        /// Writes the complete state of the variable to the writer
        /// </summary>
        /// <param name="writer">The stream to write the state to</param>
        public override void WriteField(FastBufferWriter writer)
        {
            // Serialize the data we need to synchronize
            writer.WriteValueSafe(SomeDataToSynchronize.Count);
            foreach (var dataEntry in SomeDataToSynchronize)
            {
                writer.WriteValueSafe(dataEntry.SomeIntData);
                writer.WriteValueSafe(dataEntry.SomeFloatData);
                writer.WriteValueSafe(dataEntry.SomeListOfValues.Count);
                foreach (var valueItem in dataEntry.SomeListOfValues)
                {
                    writer.WriteValueSafe(valueItem);
                }
            }
        }

        /// <summary>
        /// Reads the complete state from the reader and applies it
        /// </summary>
        /// <param name="reader">The stream to read the state from</param>
        public override void ReadField(FastBufferReader reader)
        {
            // De-Serialize the data being synchronized
            var itemsToUpdate = 0;
            reader.ReadValueSafe(out itemsToUpdate);
            SomeDataToSynchronize.Clear();
            for (int i = 0; i < itemsToUpdate; i++)
            {
                var newEntry = new SomeData();
                reader.ReadValueSafe(out newEntry.SomeIntData);
                reader.ReadValueSafe(out newEntry.SomeFloatData);
                var itemsCount = 0;
                var tempValue = (ulong)0;
                reader.ReadValueSafe(out itemsCount);
                newEntry.SomeListOfValues.Clear();
                for (int j = 0; j < itemsCount; j++)
                {
                    reader.ReadValueSafe(out tempValue);
                    newEntry.SomeListOfValues.Add(tempValue);
                }

                SomeDataToSynchronize.Add(newEntry);
            }
        }

        /// <summary>
        /// Used to write partial updates rather than synchronizing the full state on every change.
        /// </summary>
        /// <param name="writer">The stream to write the state to</param>
        public override void WriteDelta(FastBufferWriter writer)
        {
            // Not implemented for this example, instead we can write the field
            WriteField(writer);
        }

        /// <summary>
        /// Used to read partial updates rather than synchronizing the full state on every change.
        /// </summary>
        /// <inheritdoc/>
        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            // Not implemented for this example, instead we can read the field
            ReadField(reader);
        }

    }

    /// <summary>
    /// Bare minimum example of generic NetworkVariableBase derived class
    /// </summary>
    /// <typeparam name="T">Generic type marker</typeparam>
    [Serializable]
    [GenerateSerializationForGenericParameter(0)]
    public class MyCustomGenericNetworkVariable<T> : NetworkVariableBase
    {
        /// <summary>Managed list of class instances</summary>
        public List<T> SomeDataToSynchronize = new List<T>();

        /// <summary>
        /// Writes the complete state of the variable to the writer
        /// </summary>
        /// <param name="writer">The stream to write the state to</param>
        public override void WriteField(FastBufferWriter writer)
        {
            // Serialize the data we need to synchronize
            writer.WriteValueSafe(SomeDataToSynchronize.Count);
            for (var i = 0; i < SomeDataToSynchronize.Count; ++i)
            {
                var dataEntry = SomeDataToSynchronize[i];
                // NetworkVariableSerialization<T> is used for serializing generic types
                NetworkVariableSerialization<T>.Write(writer, ref dataEntry);
            }
        }

        /// <summary>
        /// Reads the complete state from the reader and applies it
        /// </summary>
        /// <param name="reader">The stream to read the state from</param>
        public override void ReadField(FastBufferReader reader)
        {
            // De-Serialize the data being synchronized
            var itemsToUpdate = 0;
            reader.ReadValueSafe(out itemsToUpdate);
            SomeDataToSynchronize.Clear();
            for (int i = 0; i < itemsToUpdate; i++)
            {
                T newEntry = default;
                // NetworkVariableSerialization<T> is used for serializing generic types
                NetworkVariableSerialization<T>.Read(reader, ref newEntry);
                SomeDataToSynchronize.Add(newEntry);
            }
        }

        /// <summary>
        /// Used to write partial updates rather than synchronizing the full state on every change.
        /// </summary>
        /// <param name="writer">The stream to write the state to</param>
        public override void WriteDelta(FastBufferWriter writer)
        {
            // Not implemented for this example, instead we can write the field
            WriteField(writer);
        }

        /// <summary>
        /// Used to read partial updates rather than synchronizing the full state on every change.
        /// </summary>
        /// <inheritdoc/>
        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            // Not implemented for this example, instead we can read the field
            ReadField(reader);
        }
    }

    [Serializable]
    internal class SomeData
    {
        public int SomeIntData = default;
        public float SomeFloatData = default;
        public List<ulong> SomeListOfValues = new List<ulong>();
    }
    #endregion


    internal class CustomNetworkVariableTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private GameObject m_PrefabToSpawn;

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab(nameof(TestMyCustomNetworkVariable));
            m_PrefabToSpawn.AddComponent<TestMyCustomNetworkVariable>();
        }

        /// <summary>
        /// Validates when the authority applies a <see cref="NetworkVariable{T}"/> value during spawn or
        /// post spawn of a newly instantiated and spawned object the value is set by the time non-authority
        /// instances invoke <see cref="NetworkBehaviour.OnNetworkSpawn"/>.
        /// </summary>
        [UnityTest]
        public IEnumerator CustomNetworkVariableCodeWorks()
        {
            var authority = GetAuthorityNetworkManager();
            var authorityObject = SpawnObject(m_PrefabToSpawn, authority).GetComponent<TestMyCustomNetworkVariable>();
            var authorityBehaviour = authorityObject.GetComponent<TestMyCustomNetworkVariable>();

            yield return WaitForSpawnedOnAllOrTimeOut(authorityObject.NetworkObjectId);
            AssertOnTimeout("Failed to spawn network object");

            foreach (var networkManager in m_NetworkManagers)
            {
                Assert.True(networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityObject.NetworkObjectId, out var localObject));
                var testBehaviour = localObject.GetComponent<TestMyCustomNetworkVariable>();
                Assert.NotNull(testBehaviour);
                Assert.AreEqual(authorityBehaviour.CustomNetworkVariable.SomeDataToSynchronize.Count, testBehaviour.CustomNetworkVariable.SomeDataToSynchronize.Count, $"[Client-{networkManager.LocalClientId}] Incorrect length found for {nameof(MyCustomNetworkVariable)}");
                Assert.AreEqual(authorityBehaviour.CustomGenericNetworkVariable.SomeDataToSynchronize, testBehaviour.CustomGenericNetworkVariable.SomeDataToSynchronize, $"[Client-{networkManager.LocalClientId}] Incorrect length found for {nameof(MyCustomNetworkVariable)}");
            }
        }
    }
}
