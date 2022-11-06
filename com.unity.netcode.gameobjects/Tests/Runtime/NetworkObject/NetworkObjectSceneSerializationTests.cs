using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using Random = UnityEngine.Random;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(VariableLengthSafety.DisableNetVarSafety)]
    [TestFixture(VariableLengthSafety.EnabledNetVarSafety)]
    public class NetworkObjectSceneSerializationTests : NetcodeIntegrationTest
    {
        private const int k_NumberToSpawn = 30;
        protected override int NumberOfClients => 0;

        private GameObject m_NetworkPrefab;
        private GameObject m_InValidNetworkPrefab;
        private GameObject m_SynchronizationPrefab;
        private VariableLengthSafety m_VariableLengthSafety;

        private LogLevel m_CurrentLogLevel;

        public enum VariableLengthSafety
        {
            DisableNetVarSafety,
            EnabledNetVarSafety,
        }

        public NetworkObjectSceneSerializationTests(VariableLengthSafety variableLengthSafety)
        {
            m_VariableLengthSafety = variableLengthSafety;
        }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<NetworkBehaviourWithOwnerNetworkVariables>();
            base.OnCreatePlayerPrefab();
        }

        protected override void OnServerAndClientsCreated()
        {

            // Set the NetworkVariable Safety Check setting
            m_ServerNetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety = m_VariableLengthSafety == VariableLengthSafety.EnabledNetVarSafety;

            // Ignore the errors generated during this test (they are expected)
            m_ServerNetworkManager.LogLevel = LogLevel.Nothing;

            // Disable forcing the same prefabs to avoid failed connections
            m_ServerNetworkManager.NetworkConfig.ForceSamePrefabs = false;

            // Create the valid network prefab
            m_NetworkPrefab = CreateNetworkObjectPrefab("ValidObject");
            m_NetworkPrefab.AddComponent<NetworkBehaviourWithNetworkVariables>();

            // Create the invalid network prefab (that will fail on client side)
            m_InValidNetworkPrefab = CreateNetworkObjectPrefab("InvalidObject");
            m_InValidNetworkPrefab.AddComponent<NetworkBehaviourWithNetworkVariables>();

            // Create the synchronization network prefab (some pass and some fail)
            m_SynchronizationPrefab = CreateNetworkObjectPrefab("SyncObject");
            m_SynchronizationPrefab.AddComponent<NetworkBehaviourSynchronizeFailureComponent>();

            base.OnServerAndClientsCreated();
        }

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            networkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            networkManager.NetworkConfig.EnsureNetworkVariableLengthSafety = m_VariableLengthSafety == VariableLengthSafety.EnabledNetVarSafety;
            foreach (var networkPrefab in m_ServerNetworkManager.NetworkConfig.NetworkPrefabs)
            {
                // To simulate a failure, we exclude the m_InValidNetworkPrefab from the connecting
                // client's side.
                if (networkPrefab.Prefab.name != m_InValidNetworkPrefab.name)
                {
                    networkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
                }
            }
            // Disable forcing the same prefabs to avoid failed connections
            networkManager.NetworkConfig.ForceSamePrefabs = false;
            networkManager.LogLevel = m_CurrentLogLevel;
            base.OnNewClientCreated(networkManager);
        }

        [UnityTest]
        public IEnumerator NetworkObjectDeserializationFailure()
        {
            m_CurrentLogLevel = LogLevel.Nothing;
            var validSpawnedNetworkObjects = new List<GameObject>();
            NetworkBehaviourWithNetworkVariables.ResetSpawnCount();

            // Spawn NetworkObjects on the server side with half of them being the
            // invalid network prefabs to simulate NetworkObject synchronization failure
            for (int i = 0; i < k_NumberToSpawn; i++)
            {
                if (i % 2 == 0)
                {
                    SpawnObject(m_InValidNetworkPrefab, m_ServerNetworkManager);
                }
                else
                {
                    // Keep track of the prefabs that should successfully spawn on the client side
                    validSpawnedNetworkObjects.Add(SpawnObject(m_NetworkPrefab, m_ServerNetworkManager));
                }
            }

            // Assure the server-side spawned all NetworkObjects
            yield return WaitForConditionOrTimeOut(() => NetworkBehaviourWithNetworkVariables.ServerSpawnCount == k_NumberToSpawn);

            // Now spawn and connect a client that will fail to spawn half of the NetworkObjects spawned
            yield return CreateAndStartNewClient();

            yield return WaitForConditionOrTimeOut(() => NetworkBehaviourWithNetworkVariables.ClientSpawnCount == validSpawnedNetworkObjects.Count);

            var serverSidePlayerComponent = NetworkBehaviourWithOwnerNetworkVariables.ServerSideClientInstance;
            var clientSidePlayerComponent = m_ClientNetworkManagers[0].LocalClient.PlayerObject.GetComponent<NetworkBehaviourWithOwnerNetworkVariables>();

            Assert.IsTrue(serverSidePlayerComponent.NetworkVariableData1.Value == clientSidePlayerComponent.NetworkVariableData1.Value);
            Assert.IsTrue(serverSidePlayerComponent.NetworkVariableData2.Value == clientSidePlayerComponent.NetworkVariableData2.Value);
            Assert.IsTrue(serverSidePlayerComponent.NetworkVariableData3.Value == clientSidePlayerComponent.NetworkVariableData3.Value);

            //Validate that the connected client has spawned the
            var clientSideNetworkObjects = s_GlobalNetworkObjects[m_ClientNetworkManagers[0].LocalClientId];

            foreach (var spawnedObject in validSpawnedNetworkObjects)
            {
                var spawnedNetworkObject = spawnedObject.GetComponent<NetworkObject>();
                Assert.IsTrue(clientSideNetworkObjects.ContainsKey(spawnedNetworkObject.NetworkObjectId), $"Failed to find valid spawned {nameof(NetworkObject)} on the client-side with a {nameof(NetworkObject.NetworkObjectId)} of {spawnedNetworkObject.NetworkObjectId}");
                var clientSideObject = clientSideNetworkObjects[spawnedNetworkObject.NetworkObjectId];
                Assert.IsTrue(clientSideObject.NetworkManager == m_ClientNetworkManagers[0], $"Client-side object {clientSideObject}'s {nameof(NetworkManager)} is not valid!");
                var serverSideComponent = spawnedObject.GetComponent<NetworkBehaviourWithNetworkVariables>();
                var clientSideComponent = clientSideObject.GetComponent<NetworkBehaviourWithNetworkVariables>();

                Assert.IsTrue(serverSideComponent.NetworkVariableData1.Count == clientSideComponent.NetworkVariableData1.Count);
                for (int i = 0; i < serverSideComponent.NetworkVariableData1.Count; i++)
                {
                    Assert.IsTrue(serverSideComponent.NetworkVariableData1[i] == clientSideComponent.NetworkVariableData1[i]);
                }

                Assert.IsTrue(serverSideComponent.NetworkVariableData2.Value == clientSideComponent.NetworkVariableData2.Value);
                Assert.IsTrue(serverSideComponent.NetworkVariableData3.Value == clientSideComponent.NetworkVariableData3.Value);
                Assert.IsTrue(serverSideComponent.NetworkVariableData4.Value == clientSideComponent.NetworkVariableData4.Value);
            }
        }

        /// <summary>
        /// This tests a mixture of failed an valid NetworkBeahviour synchronization all while synchronizing a newly connected client
        /// </summary>
        [UnityTest]
        public IEnumerator NetworkBehaviourSynchronization()
        {
            m_ServerNetworkManager.LogLevel = LogLevel.Normal;
            m_CurrentLogLevel = LogLevel.Normal;
            NetworkBehaviourSynchronizeFailureComponent.ResetBehaviour();

            var synchronizationObject = SpawnObject(m_SynchronizationPrefab, m_ServerNetworkManager);
            var synchronizationBehaviour = synchronizationObject.GetComponent<NetworkBehaviourSynchronizeFailureComponent>();
            synchronizationBehaviour.AssignNextFailureType();

            // Spawn 11 more NetworkObjects where there should be 4 of each failure type
            for (int i = 0; i < 11; i++)
            {
                synchronizationObject = SpawnObject(m_SynchronizationPrefab, m_ServerNetworkManager);
                synchronizationBehaviour = synchronizationObject.GetComponent<NetworkBehaviourSynchronizeFailureComponent>();
                synchronizationBehaviour.AssignNextFailureType();
            }

            // Now spawn and connect a client that will fail to spawn half of the NetworkObjects spawned
            yield return CreateAndStartNewClient();
        }
    }

    /// <summary>
    /// A test NetworkBeahviour that provides a varying NetworkList size as well as
    /// additional NetworkVariables to assure if a NetworkObject fails to be created
    /// the synchronization process will continue (i.e. it will skip over that block
    /// of the reader buffer).
    /// </summary>
    public class NetworkBehaviourWithNetworkVariables : NetworkBehaviour
    {
        public static int ServerSpawnCount { get; internal set; }
        public static int ClientSpawnCount { get; internal set; }

        public static void ResetSpawnCount()
        {
            ServerSpawnCount = 0;
            ClientSpawnCount = 0;
        }

        private const uint k_MinDataBlocks = 1;
        private const uint k_MaxDataBlocks = 64;

        // Add various types of NetworkVariables
        public NetworkList<ulong> NetworkVariableData1;
        public NetworkVariable<int> NetworkVariableData2;
        public NetworkVariable<long> NetworkVariableData3;
        public NetworkVariable<byte> NetworkVariableData4;

        private void Awake()
        {
            var dataBlocksAssigned = new List<ulong>();
            var numberDataBlocks = Random.Range(k_MinDataBlocks, k_MaxDataBlocks);
            for (var i = 0; i < numberDataBlocks; i++)
            {
                dataBlocksAssigned.Add((ulong)Random.Range(0.0f, float.MaxValue));
            }

            NetworkVariableData1 = new NetworkList<ulong>(dataBlocksAssigned);
            NetworkVariableData2 = new NetworkVariable<int>(Random.Range(1, 1000));
            NetworkVariableData3 = new NetworkVariable<long>(Random.Range(1, 1000));
            NetworkVariableData4 = new NetworkVariable<byte>((byte)Random.Range(1, 255));
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                ServerSpawnCount++;
            }
            else
            {
                ClientSpawnCount++;
            }

            base.OnNetworkSpawn();
        }
    }

    /// <summary>
    /// A test NetworkBeahviour that simulates various types of synchronization failures
    /// and provides a synchronization success version to validate that synchronization
    /// will continue if user synchronization code fails.
    /// </summary>
    public class NetworkBehaviourSynchronizeFailureComponent : NetworkBehaviour
    {
        public static int ServerSpawnCount { get; internal set; }
        public static int ClientSpawnCount { get; internal set; }

        private static FailureTypes s_FailureType = FailureTypes.None;

        public enum FailureTypes
        {
            None,
            DuringWriting,
            DuringReading
        }

        public static void ResetBehaviour()
        {
            ServerSpawnCount = 0;
            ClientSpawnCount = 0;
            s_FailureType = FailureTypes.None;
        }

        private MyCustomData m_MyCustomData;

        private struct MyCustomData : INetworkSerializable
        {
            public FailureTypes FailureType;
            private ushort m_DataSize;
            private byte[] m_DataBlock;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                if (serializer.IsWriter)
                {
                    var writer = serializer.GetFastBufferWriter();
                    switch (FailureType)
                    {
                        case FailureTypes.None:
                        case FailureTypes.DuringReading:
                            {
                                writer.WriteValueSafe(m_DataSize);
                                for (int i = 0; i < m_DataSize; i++)
                                {
                                    writer.WriteValueSafe(m_DataBlock[i]);
                                }
                                break;
                            }
                        case FailureTypes.DuringWriting:
                            {
                                writer.WriteValueSafe(m_DataSize);
                                // Try to write past the allocated size to generate an exception
                                // while also filling the buffer to verify that the buffer will be
                                // reset back to the original position.
                                for (int i = 0; i <= m_DataSize; i++)
                                {
                                    writer.WriteValueSafe(m_DataBlock[i]);
                                }
                                break;
                            }
                    }
                }
                else
                {
                    var reader = serializer.GetFastBufferReader();
                    switch (FailureType)
                    {
                        case FailureTypes.None:
                            {
                                reader.ReadValueSafe(out m_DataSize);
                                m_DataBlock = new byte[m_DataSize];
                                for (int i = 0; i < m_DataSize; i++)
                                {
                                    reader.ReadValueSafe(out m_DataBlock[i]);
                                }
                                break;
                            }
                        case FailureTypes.DuringReading:
                            {
                                reader.ReadValueSafe(out m_DataSize);
                                // Allocate more space than needed
                                m_DataBlock = new byte[(int)(m_DataSize * 1.5f)];
                                // Now read past the size of this message to verify
                                // that the reader will get rest back to the appropriate
                                // position and an error will be generated for this
                                for (int i = 0; i < m_DataBlock.Length; i++)
                                {
                                    reader.ReadValueSafe(out m_DataBlock[i]);
                                }
                                break;
                            }
                    }
                }
            }

            public void GenerateData(ushort size)
            {
                m_DataSize = size;
                m_DataBlock = new byte[size];
                for (int i = 0; i < m_DataSize; i++)
                {
                    m_DataBlock[i] = (byte)Random.Range(0, 512);
                }
            }
        }

        // This NetworkVariable is synchronized before OnSynchronize is invoked
        // which enables us to perform the tests.
        // Users could follow the same pattern for game assets and synchronize
        // clients based on NetworkVariable settings. (i.e. a specific NPC type or the like)
        private NetworkVariable<FailureTypes> m_FailureType;

        public void AssignNextFailureType()
        {
            var currentPosition = (int)s_FailureType;
            currentPosition = (++currentPosition) % 3;
            s_FailureType = (FailureTypes)currentPosition;
            m_FailureType.Value = s_FailureType;
        }


        private void Awake()
        {
            m_FailureType = new NetworkVariable<FailureTypes>();
            m_MyCustomData = new MyCustomData();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                ServerSpawnCount++;
                m_MyCustomData.GenerateData((ushort)Random.Range(1, 512));
            }
            else
            {
                ClientSpawnCount++;
            }

            base.OnNetworkSpawn();
        }

        protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer)
        {
            // Assign the failure type first
            m_MyCustomData.FailureType = m_FailureType.Value;
            // Now handle the serialization for this failure type
            m_MyCustomData.NetworkSerialize(serializer);
        }
    }

    /// <summary>
    /// A test NetworkBeahviour that has varying permissions in order to validate that
    /// when variable length safety checks are off NetworkVariables still are updated
    /// properly.
    /// </summary>
    public class NetworkBehaviourWithOwnerNetworkVariables : NetworkBehaviour
    {
        public static NetworkBehaviourWithOwnerNetworkVariables ServerSideClientInstance;

        public NetworkVariable<int> NetworkVariableData1 = new NetworkVariable<int>(default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
        public NetworkVariable<long> NetworkVariableData2 = new NetworkVariable<long>();
        public NetworkVariable<byte> NetworkVariableData3 = new NetworkVariable<byte>(default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            if (IsServer && !IsOwner)
            {
                ServerSideClientInstance = this;
                NetworkVariableData1.Value = Random.Range(1, 1000);
                NetworkVariableData2.Value = Random.Range(1, 1000);
                NetworkVariableData3.Value = (byte)Random.Range(1, 255);
            }
        }
    }
}
