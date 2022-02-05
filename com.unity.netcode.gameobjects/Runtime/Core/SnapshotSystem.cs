using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

// SnapshotSystem stores:
//
// - Spawn, Despwan commands (done)
// - NetworkVariable value updates (todo)
// - RPC commands (todo)
//
// and sends a SnapshotDataMessage every tick containing all the un-acknowledged commands.
//
// SnapshotSystem can function even if some messages are lost. It provides eventual consistency.
// The client receiving a message will get a consistent state for a given tick, but possibly not every ticks
// Reliable RPCs will be guaranteed, unreliable ones not
//
// SnapshotSystem relies on the Transport adapter to fragment an arbitrary-sized message into packets
// This comes with a tradeoff. The Transport-level fragmentation is specialized for networking
// but lacks the context that SnapshotSystem has of the meaning of the RPC, Spawns, etc...
// This could be revisited in the future
//
// It also relies on the INetworkMessage interface and MessagingSystem, but deals directly
// with the FastBufferReader and FastBufferWriter to read/write the messages

namespace Unity.Netcode
{
    /// <summary>
    /// Header information for a SnapshotDataMessage
    /// </summary>
    internal struct SnapshotHeader
    {
        internal int CurrentTick; // the tick this captures information for
        internal int LastReceivedSequence; // what we are ack'ing
        internal int SpawnCount; // number of spawn commands included
        internal int DespawnCount; // number of despawn commands included
        internal int UpdateCount; // number of update commands included
    }

    internal struct UpdateCommand
    {
        internal ulong NetworkObjectId;
        internal ushort BehaviourIndex;
        internal int VariableIndex;

        // snapshot internal
        internal int TickWritten;
        internal int SerializedLength;
        internal bool IsDelta; // Is this carrying a ReadDelta(). Should always be true except for spawn-generated updates
    }

    internal struct UpdateCommandMeta
    {
        internal int Index; // the index for the index allocator
        internal int BufferPos; // the allocated position in the buffer
        internal List<ulong> TargetClientIds;
    }

    /// <summary>
    /// A command to despawn an object
    /// Which object it is, and the tick at which it was despawned
    /// </summary>
    internal struct SnapshotDespawnCommand
    {
        // identity
        internal ulong NetworkObjectId;

        // snapshot internal
        internal int TickWritten;
    }

    /// <summary>
    /// A command to spawn an object
    /// Which object it is, what type it has, the spawn parameters and the tick at which it was spawned
    /// </summary>
    internal struct SnapshotSpawnCommand
    {
        // identity
        internal ulong NetworkObjectId;

        // archetype
        internal uint GlobalObjectIdHash;
        internal bool IsSceneObject; //todo: how is this unused ?

        // parameters
        internal bool IsPlayerObject;
        internal ulong OwnerClientId;
        internal ulong ParentNetworkId;
        internal Vector3 ObjectPosition;
        internal Quaternion ObjectRotation;
        internal Vector3 ObjectScale; //todo: how is this unused ?

        // internal
        internal int TickWritten;
    }

    /// <summary>
    /// Stores supplemental meta-information about a Spawn or Despawn command.
    /// This part doesn't get sent, so is stored elsewhere in order to allow writing just the SnapshotSpawnCommand
    /// </summary>
    internal struct SnapshotSpawnDespawnCommandMeta
    {
        // The remaining clients a command still has to be sent to
        internal List<ulong> TargetClientIds;
    };

    /// <summary>
    /// Stores information about a specific client.
    /// What tick they ack'ed, for now.
    /// </summary>
    internal struct ClientData
    {
        internal int LastReceivedTick; // the last tick received by this client

        internal ClientData(int unused)
        {
            LastReceivedTick = -1;
        }
    }

    internal delegate int SendMessageHandler(SnapshotDataMessage message, ulong clientId);
    internal delegate void SpawnObjectHandler(SnapshotSpawnCommand spawnCommand, ulong srcClientId);
    internal delegate void DespawnObjectHandler(SnapshotDespawnCommand despawnCommand, ulong srcClientId);
    internal delegate void GetBehaviourVariableHandler(UpdateCommand updateCommand, out NetworkBehaviour behaviour, out NetworkVariableBase variable, ulong srcClientId);


    internal class SnapshotSystem : INetworkUpdateSystem, IDisposable
    {
        private NetworkManager m_NetworkManager;
        private NetworkTickSystem m_NetworkTickSystem;

        private Dictionary<ulong, ClientData> m_ClientData = new Dictionary<ulong, ClientData>();
        // todo: how is this unused and does it belong here ?
        private Dictionary<ulong, ConnectionRtt> m_ConnectionRtts = new Dictionary<ulong, ConnectionRtt>();

        // The tick we're currently processing (or last we processed, outside NetworkUpdate())
        private int m_CurrentTick = NetworkTickSystem.NoTick;

        internal UpdateCommand[] Updates;
        internal UpdateCommandMeta[] UpdatesMeta;
        internal int NumUpdates = 0;

        // This arrays contains all the spawn commands received by the game code.
        // This part can be written as-is to the message.
        // Those are cleaned-up once the spawns are ack'ed by all target clients
        internal SnapshotSpawnCommand[] Spawns;
        // Meta-information about Spawns. Entries are matched by index
        internal SnapshotSpawnDespawnCommandMeta[] SpawnsMeta;
        // Number of spawns used in the array. The array might actually be bigger, as it reserves space for performance reasons
        internal int NumSpawns = 0;

        // This arrays contains all the despawn commands received by the game code.
        // This part can be written as-is to the message.
        // Those are cleaned-up once the despawns are ack'ed by all target clients
        internal SnapshotDespawnCommand[] Despawns;
        // Meta-information about Despawns. Entries are matched by index
        internal SnapshotSpawnDespawnCommandMeta[] DespawnsMeta;
        // Number of spawns used in the array. The array might actually be bigger, as it reserves space for performance reasons
        internal int NumDespawns = 0;

        // Local state. Stores which spawns and despawns were applied locally
        // indexed by ObjectId
        internal Dictionary<ulong, int> TickAppliedSpawn = new Dictionary<ulong, int>();
        internal Dictionary<ulong, int> TickAppliedDespawn = new Dictionary<ulong, int>();

        // Settings
        internal bool IsServer { get; set; }
        internal bool IsConnectedClient { get; set; }
        internal ulong ServerClientId { get; set; }
        internal List<ulong> ConnectedClientsId { get; } = new List<ulong>();

        // The following handlers decouple SnapshotSystem from its dependencies

        // Handler that is called by SnapshotSystem to send a SnapshotMessage.
        internal SendMessageHandler SendMessage;
        // Handlers that are called by SnapshotSystem to spawn an object locally.
        // The pre- version is called first, to allow the SDK to create the object.
        // The post- version is called later, after reading the initial values of the NetworkVariable an object contains
        internal SpawnObjectHandler PreSpawnObject;
        internal SpawnObjectHandler PostSpawnObject;
        // Handler that is called by SnapshotSystem to despawn an object locally.
        internal DespawnObjectHandler DespawnObject;
        // Handler that is called by SnapshotSystem to obtain a specific NetworkBehaviour and NetworkVariable.
        internal GetBehaviourVariableHandler GetBehaviourVariable;

        // Property showing visibility into inner workings, for testing
        internal int SpawnsBufferCount { get; private set; } = 100;
        internal int DespawnsBufferCount { get; private set; } = 100;

        internal int UpdatesBufferCount { get; private set; } = 100;

        internal const int TotalMaxIndices = 1000;
        internal const int TotalBufferMemory = 100000;

        internal IndexAllocator MemoryStorage = new IndexAllocator(TotalBufferMemory, TotalMaxIndices);
        internal byte[] MemoryBuffer = new byte[TotalBufferMemory];

        private int[] m_AvailableIndices; // The IndexAllocator indices for memory management
        private int m_AvailableIndicesBufferCount = TotalMaxIndices; // Size of the buffer storing indices
        private int m_NumAvailableIndices = TotalMaxIndices; // Current number of valid indices in m_AvailableIndices

        private FastBufferWriter m_Writer;

        internal SnapshotSystem(NetworkManager networkManager, NetworkConfig config, NetworkTickSystem networkTickSystem)
        {
            m_NetworkManager = networkManager;
            m_NetworkTickSystem = networkTickSystem;

            m_Writer = new FastBufferWriter(TotalBufferMemory, Allocator.Persistent);

            if (networkManager != null)
            {
                // If we have a NetworkManager, let's send on the network. This can be overriden for tests
                SendMessage = NetworkSendMessage;
                // If we have a NetworkManager, let's (de)spawn with the rest of our package. This can be overriden for tests
                PreSpawnObject = NetworkPreSpawnObject;
                PostSpawnObject = NetworkPostSpawnObject;
                DespawnObject = NetworkDespawnObject;
                GetBehaviourVariable = NetworkGetBehaviourVariable;
            }

            // register for updates in EarlyUpdate
            this.RegisterNetworkUpdate(NetworkUpdateStage.PostLateUpdate);

            Spawns = new SnapshotSpawnCommand[SpawnsBufferCount];
            SpawnsMeta = new SnapshotSpawnDespawnCommandMeta[SpawnsBufferCount];
            Despawns = new SnapshotDespawnCommand[DespawnsBufferCount];
            DespawnsMeta = new SnapshotSpawnDespawnCommandMeta[DespawnsBufferCount];
            Updates = new UpdateCommand[UpdatesBufferCount];
            UpdatesMeta = new UpdateCommandMeta[UpdatesBufferCount];

            m_AvailableIndices = new int[m_AvailableIndicesBufferCount];
            for (var i = 0; i < m_AvailableIndicesBufferCount; i++)
            {
                m_AvailableIndices[i] = i;
            }
        }

        // returns the default client list: just the server, on clients, all clients, on the server
        internal List<ulong> GetClientList()
        {
            List<ulong> clientList;
            clientList = new List<ulong>();

            if (!IsServer)
            {
                clientList.Add(m_NetworkManager.ServerClientId);
            }
            else
            {
                foreach (var clientId in ConnectedClientsId)
                {
                    if (clientId != m_NetworkManager.ServerClientId)
                    {
                        clientList.Add(clientId);
                    }
                }
            }

            return clientList;
        }

        /// <summary>
        /// Shrink the buffer to the minimum needed. Frees the reserved space.
        /// Mostly for testing at the moment, but could be useful for game code to reclaim memory
        /// </summary>
        internal void ReduceBufferUsage()
        {
            var count = Math.Max(1, NumDespawns);
            Array.Resize(ref Despawns, count);
            DespawnsBufferCount = count;

            count = Math.Max(1, NumSpawns);
            Array.Resize(ref Spawns, count);
            SpawnsBufferCount = count;
        }

        /// <summary>
        /// Called by SnapshotSystem, to spawn an object locally
        /// In the pre- phase, we trigger the object creation and call
        /// PreSpawnNetworkObjectLocallyCommon which trigger internal things like creating the NetworkVariables
        /// </summary>
        internal void NetworkPreSpawnObject(SnapshotSpawnCommand spawnCommand, ulong srcClientId)
        {
            NetworkObject networkObject;
            if (spawnCommand.ParentNetworkId == spawnCommand.NetworkObjectId)
            {
                networkObject = m_NetworkManager.SpawnManager.CreateLocalNetworkObject(false,
                    spawnCommand.GlobalObjectIdHash, spawnCommand.OwnerClientId, null, spawnCommand.ObjectPosition,
                    spawnCommand.ObjectRotation);
            }
            else
            {
                networkObject = m_NetworkManager.SpawnManager.CreateLocalNetworkObject(false,
                    spawnCommand.GlobalObjectIdHash, spawnCommand.OwnerClientId, spawnCommand.ParentNetworkId, spawnCommand.ObjectPosition,
                    spawnCommand.ObjectRotation);
            }

            m_NetworkManager.SpawnManager.PreSpawnNetworkObjectLocallyCommon(networkObject, spawnCommand.NetworkObjectId,
                true, spawnCommand.IsPlayerObject, spawnCommand.OwnerClientId, false);

            //todo: discuss with tools how to report shared bytes
            m_NetworkManager.NetworkMetrics.TrackObjectSpawnReceived(srcClientId, networkObject, 8);
        }

        /// <summary>
        /// Called by SnapshotSystem, to spawn an object locally
        /// In the post- phase, we call PostSpawnNetworkObjectLocallyCommon
        /// which will notify the game code. This is done in two steps as it needs to happen after the object's
        /// NetworkVariables have been read
        /// </summary>
        internal void NetworkPostSpawnObject(SnapshotSpawnCommand spawnCommand, ulong srcClientId)
        {
            if (m_NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(spawnCommand.NetworkObjectId,
                out NetworkObject networkObject))
            {
                m_NetworkManager.SpawnManager.PostSpawnNetworkObjectLocallyCommon(networkObject, spawnCommand.NetworkObjectId,
                    true, spawnCommand.IsPlayerObject, spawnCommand.OwnerClientId, false);
            }
            else
            {
                Debug.LogError($"Didn't find expected NetworkObject for NetworkObjectId {spawnCommand.NetworkObjectId}");
            }
        }

        /// <summary>
        /// Called by SnapshotSystem, to despawn an object locally
        /// </summary>
        internal void NetworkDespawnObject(SnapshotDespawnCommand despawnCommand, ulong srcClientId)
        {
            m_NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(despawnCommand.NetworkObjectId, out NetworkObject networkObject);

            if (networkObject == null)
            {
                return;
            }

            m_NetworkManager.SpawnManager.OnDespawnObject(networkObject, true);
            //todo: discuss with tools how to report shared bytes
            m_NetworkManager.NetworkMetrics.TrackObjectDestroyReceived(srcClientId, networkObject, 8);
        }

        /// <summary>
        /// Updates the internal state of SnapshotSystem to refresh its knowledge of:
        /// - am I a server
        /// - what are the client Ids
        /// todo: consider optimizing
        /// </summary>
        internal void UpdateClientServerData()
        {
            if (m_NetworkManager)
            {
                IsServer = m_NetworkManager.IsServer;
                IsConnectedClient = m_NetworkManager.IsConnectedClient;
                ServerClientId = m_NetworkManager.ServerClientId;

                // todo: This is extremely inefficient. What is the efficient and idiomatic way ?
                ConnectedClientsId.Clear();
                if (IsServer)
                {
                    foreach (var id in m_NetworkManager.ConnectedClientsIds)
                    {
                        ConnectedClientsId.Add(id);
                    }
                }
            }
        }

        internal ConnectionRtt GetConnectionRtt(ulong clientId)
        {
            return new ConnectionRtt();
        }

        public void Dispose()
        {
            this.UnregisterNetworkUpdate(NetworkUpdateStage.PostLateUpdate);
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            if (updateStage == NetworkUpdateStage.PostLateUpdate)
            {
                if (m_NetworkManager)
                {
                    m_NetworkManager.BehaviourUpdater.NetworkBehaviourUpdate(m_NetworkManager);
                }

                UpdateClientServerData();

                var tick = m_NetworkTickSystem.LocalTime.Tick;

                if (tick != m_CurrentTick)
                {
                    m_CurrentTick = tick;
                    if (IsServer)
                    {
                        for (int i = 0; i < ConnectedClientsId.Count; i++)
                        {
                            var clientId = ConnectedClientsId[i];

                            // don't send to ourselves
                            if (clientId != ServerClientId)
                            {
                                SendSnapshot(clientId);
                            }
                        }
                    }
                    else if (IsConnectedClient)
                    {
                        SendSnapshot(ServerClientId);
                    }
                }
            }
        }

        // where we build and send a snapshot to a given client
        private void SendSnapshot(ulong clientId)
        {
            var header = new SnapshotHeader();
            // todo: we should have a way to get an upper bound for that
            var message = new SnapshotDataMessage(TotalBufferMemory);

            // Verify we allocated client Data for this clientId
            if (!m_ClientData.ContainsKey(clientId))
            {
                m_ClientData.Add(clientId, new ClientData(0));
            }

            // Find which spawns must be included
            var spawnsToInclude = new List<int>();
            for (var index = 0; index < NumSpawns; index++)
            {
                if (SpawnsMeta[index].TargetClientIds.Contains(clientId))
                {
                    spawnsToInclude.Add(index);
                }
            }

            // Find which despawns must be included
            var despawnsToInclude = new List<int>();
            for (var index = 0; index < NumDespawns; index++)
            {
                if (DespawnsMeta[index].TargetClientIds.Contains(clientId))
                {
                    despawnsToInclude.Add(index);
                }
            }

            // Find which value updates must be included
            var updatesToInclude = new List<int>();
            var updatesPayloadLength = 0;
            for (var index = 0; index < NumUpdates; index++)
            {
                if (UpdatesMeta[index].TargetClientIds.Contains(clientId))
                {
                    updatesToInclude.Add(index);
                    updatesPayloadLength += Updates[index].SerializedLength;
                }
            }


            header.CurrentTick = m_CurrentTick;
            header.SpawnCount = spawnsToInclude.Count;
            header.DespawnCount = despawnsToInclude.Count;
            header.UpdateCount = updatesToInclude.Count;
            header.LastReceivedSequence = m_ClientData[clientId].LastReceivedTick;

            if (!message.WriteBuffer.TryBeginWrite(
                FastBufferWriter.GetWriteSize(header) +
                spawnsToInclude.Count * FastBufferWriter.GetWriteSize(Spawns[0]) +
                despawnsToInclude.Count * FastBufferWriter.GetWriteSize(Despawns[0]) +
                updatesToInclude.Count * FastBufferWriter.GetWriteSize(Updates[0]) +
                updatesPayloadLength))
            {
                // todo: error handling
                Debug.Assert(false, "Unable to secure buffer for sending");
            }

            message.WriteBuffer.WriteValue(header);

            // Write the Spawns.
            foreach (var index in spawnsToInclude)
            {
                message.WriteBuffer.WriteValue(Spawns[index]);
            }

            // Write the Updates, interleaved with the variable payload
            foreach (var index in updatesToInclude)
            {
                message.WriteBuffer.WriteValue(Updates[index]);
                message.WriteBuffer.WriteBytes(MemoryBuffer, Updates[index].SerializedLength, UpdatesMeta[index].BufferPos);
            }

            // Write the Despawns.
            foreach (var index in despawnsToInclude)
            {
                message.WriteBuffer.WriteValue(Despawns[index]);
            }

            SendMessage(message, clientId);
        }

        internal void CleanUpdateFromSnapshot(SnapshotDespawnCommand despawnCommand)
        {
            for (int i = 0; i < NumUpdates; /*increment done below*/)
            {
                // if this is a despawn command for an object we have an update for, let's forget it
                if (Updates[i].NetworkObjectId == despawnCommand.NetworkObjectId)
                {
                    // deallocate the memory
                    MemoryStorage.Deallocate(UpdatesMeta[i].Index);
                    // retrieve the index as available
                    m_AvailableIndices[m_NumAvailableIndices++] = UpdatesMeta[i].Index;

                    Updates[i] = Updates[NumUpdates - 1];
                    UpdatesMeta[i] = UpdatesMeta[NumUpdates - 1];
                    NumUpdates--;

                    // skip incrementing i
                }
                else
                {
                    i++;
                }
            }
        }

        /// <summary>
        /// Entry-point into SnapshotSystem to spawn an object
        /// called with a SnapshotSpawnCommand, the NetworkObject and a list of target clientIds, where null means all clients
        /// </summary>
        internal void Spawn(SnapshotSpawnCommand command, NetworkObject networkObject, List<ulong> targetClientIds)
        {
            command.TickWritten = m_CurrentTick;

            if (NumSpawns >= SpawnsBufferCount)
            {
                SpawnsBufferCount = SpawnsBufferCount * 2;
                Array.Resize(ref Spawns, SpawnsBufferCount);
                Array.Resize(ref SpawnsMeta, SpawnsBufferCount);
            }

            if (targetClientIds == default)
            {
                targetClientIds = GetClientList();
            }

            // todo:
            // this 'if' might be temporary, but is needed to help in debugging
            // or maybe it stays
            if (targetClientIds.Count > 0)
            {
                Spawns[NumSpawns] = command;
                SpawnsMeta[NumSpawns].TargetClientIds = targetClientIds;
                NumSpawns++;
            }

            if (m_NetworkManager)
            {
                foreach (var dstClientId in targetClientIds)
                {
                    m_NetworkManager.NetworkMetrics.TrackObjectSpawnSent(dstClientId, networkObject, 8);
                }

                // When we spawn an object we need to include its initial value.
                // This scans the NetworkVariable of the spawn object and store its NetworkVariables

                var updateCommand = new UpdateCommand();
                for (ushort childIndex = 0; childIndex < networkObject.ChildNetworkBehaviours.Count; childIndex++)
                {
                    var behaviour = networkObject.ChildNetworkBehaviours[childIndex];
                    for (var variableIndex = 0; variableIndex < behaviour.NetworkVariableFields.Count; variableIndex++)
                    {
                        updateCommand.NetworkObjectId = command.NetworkObjectId;
                        updateCommand.BehaviourIndex = childIndex;
                        updateCommand.VariableIndex = variableIndex;

                        // because this is a spawn, we specify that this isn't a delta update
                        Store(updateCommand, behaviour.NetworkVariableFields[variableIndex], /* IsDelta = */ false);
                    }
                }
            }
        }

        /// <summary>
        /// Entry-point into SnapshotSystem to despawn an object
        /// called with a SnapshotDespawnCommand, the NetworkObject and a list of target clientIds, where null means all clients
        /// </summary>
        internal void Despawn(SnapshotDespawnCommand command, NetworkObject networkObject, List<ulong> targetClientIds)
        {
            command.TickWritten = m_CurrentTick;

            if (NumDespawns >= DespawnsBufferCount)
            {
                DespawnsBufferCount = DespawnsBufferCount * 2;
                Array.Resize(ref Despawns, DespawnsBufferCount);
                Array.Resize(ref DespawnsMeta, DespawnsBufferCount);
            }

            if (targetClientIds == default)
            {
                targetClientIds = GetClientList();
            }

            // todo:
            // this 'if' might be temporary, but is needed to help in debugging
            // or maybe it stays
            if (targetClientIds.Count > 0)
            {
                Despawns[NumDespawns] = command;
                DespawnsMeta[NumDespawns].TargetClientIds = targetClientIds;
                NumDespawns++;
            }

            CleanUpdateFromSnapshot(command);

            if (m_NetworkManager)
            {
                foreach (var dstClientId in targetClientIds)
                {
                    m_NetworkManager.NetworkMetrics.TrackObjectDestroySent(dstClientId, networkObject, 8);
                }
            }
        }

        // entry-point for value updates
        internal void Store(UpdateCommand command, NetworkVariableBase networkVariable, bool isDelta = true)
        {
            command.TickWritten = m_CurrentTick;
            command.IsDelta = isDelta;

            var commandPosition = -1;

            List<ulong> targetClientIds = GetClientList();

            if (targetClientIds.Count == 0)
            {
                return;
            }

            // Look for an existing variable's position to update before adding a new entry
            for (var i = 0; i < NumUpdates; i++)
            {
                if (Updates[i].BehaviourIndex == command.BehaviourIndex &&
                    Updates[i].NetworkObjectId == command.NetworkObjectId &&
                    Updates[i].VariableIndex == command.VariableIndex)
                {
                    commandPosition = i;
                    break;
                }
            }

            if (commandPosition == -1)
            {
                int index = -1;

                if (NumUpdates >= UpdatesBufferCount)
                {
                    UpdatesBufferCount = UpdatesBufferCount * 2;
                    Array.Resize(ref Updates, UpdatesBufferCount);
                    Array.Resize(ref UpdatesMeta, UpdatesBufferCount);
                }

                commandPosition = NumUpdates;
                NumUpdates++;

                index = m_AvailableIndices[0];
                m_AvailableIndices[0] = m_AvailableIndices[m_NumAvailableIndices - 1];
                m_NumAvailableIndices--;

                UpdatesMeta[commandPosition].Index = index;
            }
            else
            {
                // de-allocate previous buffer as a new one will be allocated
                MemoryStorage.Deallocate(UpdatesMeta[commandPosition].Index);
            }

            // the position we'll be serializing the network variable at, in our memory buffer
            int bufferPos = 0;

            m_Writer.Seek(0);
            m_Writer.Truncate(0);

            if (m_NumAvailableIndices == 0)
            {
                // todo: error handling
                Debug.Assert(false);
            }

            // we use WriteDelta for updates, but WriteField for spawns. It is important to use the correct one
            // for NetworkList. And, in the future, NetworkVariables might care, too.
            if (command.IsDelta)
            {
                networkVariable.WriteDelta(m_Writer);
            }
            else
            {
                networkVariable.WriteField(m_Writer);
            }

            command.SerializedLength = m_Writer.Length;

            var allocated = MemoryStorage.Allocate(UpdatesMeta[commandPosition].Index, m_Writer.Length, out bufferPos);

            Debug.Assert(allocated);

            unsafe
            {
                fixed (byte* buff = &MemoryBuffer[0])
                {
                    Buffer.MemoryCopy(m_Writer.GetUnsafePtr(), buff + bufferPos, TotalBufferMemory - bufferPos,
                        m_Writer.Length);
                }
            }

            Updates[commandPosition] = command;
            UpdatesMeta[commandPosition].TargetClientIds = targetClientIds;
            UpdatesMeta[commandPosition].BufferPos = bufferPos;
        }

        internal void HandleSnapshot(ulong clientId, SnapshotDataMessage message)
        {
            // Read the Spawns. Count first, then each spawn
            var spawnCommand = new SnapshotSpawnCommand();
            var despawnCommand = new SnapshotDespawnCommand();
            var updateCommand = new UpdateCommand();

            var header = new SnapshotHeader();

            // Verify we allocated client Data for this clientId
            if (!m_ClientData.ContainsKey(clientId))
            {
                m_ClientData.Add(clientId, new ClientData(0));
            }

            if (message.ReadBuffer.TryBeginRead(FastBufferWriter.GetWriteSize(header)))
            {
                // todo: error handling
                message.ReadBuffer.ReadValue(out header);
            }
            else
            {
                Debug.LogError("Error reading header");
                return;
            }

            var clientData = m_ClientData[clientId];
            clientData.LastReceivedTick = header.CurrentTick;
            m_ClientData[clientId] = clientData;

            if (!message.ReadBuffer.TryBeginRead(FastBufferWriter.GetWriteSize(spawnCommand) * header.SpawnCount))
            {
                // todo: deal with error
            }

            var spawnPosition = message.ReadBuffer.Position;
            for (int index = 0; index < header.SpawnCount; index++)
            {
                message.ReadBuffer.ReadValue(out spawnCommand);

                if (TickAppliedSpawn.ContainsKey(spawnCommand.NetworkObjectId) &&
                    spawnCommand.TickWritten <= TickAppliedSpawn[spawnCommand.NetworkObjectId])
                {
                    continue;
                }

                PreSpawnObject(spawnCommand, clientId);
            }

            for (int index = 0; index < header.UpdateCount; index++)
            {
                message.ReadBuffer.TryBeginRead(FastBufferWriter.GetWriteSize(updateCommand));
                message.ReadBuffer.ReadValue(out updateCommand);

                // Find the network variable;
                GetBehaviourVariable(updateCommand, out NetworkBehaviour behaviour, out NetworkVariableBase variable, clientId);

                // if the variable is not present anymore (despawned) or if this is an older update, let skip
                // we still need to seek over the message, though
                if (variable != null && updateCommand.TickWritten > variable.TickRead)
                {
                    variable.TickRead = updateCommand.TickWritten;
                    if (updateCommand.IsDelta)
                    {
                        // todo: revisit if we need to pass something for keepDirtyDelta
                        // since we currently only have server-authoritative changes, this makes no difference
                        variable.ReadDelta(message.ReadBuffer, /* keepDirtyDelta = */false);
                    }
                    else
                    {
                        variable.ReadField(message.ReadBuffer);
                    }

                    m_NetworkManager.NetworkMetrics.TrackNetworkVariableDeltaReceived(
                        clientId, behaviour.NetworkObject, variable.Name, behaviour.__getTypeName(), 20); // todo: what length ?
                }
                else
                {
                    // skip over the value update payload we don't need to read
                    message.ReadBuffer.Seek(message.ReadBuffer.Position + updateCommand.SerializedLength);
                }
            }

            if (!message.ReadBuffer.TryBeginRead(FastBufferWriter.GetWriteSize(despawnCommand) * header.DespawnCount))
            {
                // todo: deal with error
            }

            for (int index = 0; index < header.DespawnCount; index++)
            {
                message.ReadBuffer.ReadValue(out despawnCommand);

                // todo: can we keep a single value of which tick we applied instead of per object ?
                if (TickAppliedDespawn.ContainsKey(despawnCommand.NetworkObjectId) &&
                    despawnCommand.TickWritten <= TickAppliedDespawn[despawnCommand.NetworkObjectId])
                {
                    continue;
                }

                TickAppliedDespawn[despawnCommand.NetworkObjectId] = despawnCommand.TickWritten;
                DespawnObject(despawnCommand, clientId);
            }

            // todo: can we keep a single value of which tick we applied instead of per object ?

            for (int i = 0; i < NumSpawns;)
            {
                if (Spawns[i].TickWritten < header.LastReceivedSequence &&
                    SpawnsMeta[i].TargetClientIds.Contains(clientId))
                {
                    SpawnsMeta[i].TargetClientIds.Remove(clientId);

                    if (SpawnsMeta[i].TargetClientIds.Count == 0)
                    {
                        SpawnsMeta[i] = SpawnsMeta[NumSpawns - 1];
                        Spawns[i] = Spawns[NumSpawns - 1];
                        NumSpawns--;

                        continue; // skip the i++ below
                    }
                }
                i++;
            }
            for (int i = 0; i < NumDespawns;)
            {
                if (Despawns[i].TickWritten < header.LastReceivedSequence &&
                    DespawnsMeta[i].TargetClientIds.Contains(clientId))
                {
                    DespawnsMeta[i].TargetClientIds.Remove(clientId);

                    if (DespawnsMeta[i].TargetClientIds.Count == 0)
                    {
                        DespawnsMeta[i] = DespawnsMeta[NumDespawns - 1];
                        Despawns[i] = Despawns[NumDespawns - 1];
                        NumDespawns--;

                        continue; // skip the i++ below
                    }
                }
                i++;
            }
            for (int i = 0; i < NumUpdates;)
            {
                if (Updates[i].TickWritten < header.LastReceivedSequence &&
                    UpdatesMeta[i].TargetClientIds.Contains(clientId))
                {
                    UpdatesMeta[i].TargetClientIds.Remove(clientId);

                    if (UpdatesMeta[i].TargetClientIds.Count == 0)
                    {
                        MemoryStorage.Deallocate(UpdatesMeta[i].Index);
                        m_AvailableIndices[m_NumAvailableIndices++] = UpdatesMeta[i].Index;

                        UpdatesMeta[i] = UpdatesMeta[NumUpdates - 1];
                        Updates[i] = Updates[NumUpdates - 1];
                        NumUpdates--;

                        continue; // skip the i++ below
                    }
                }
                i++;
            }

            var endPosition = message.ReadBuffer.Position;
            message.ReadBuffer.Seek(spawnPosition);
            for (int index = 0; index < header.SpawnCount; index++)
            {
                message.ReadBuffer.ReadValue(out spawnCommand);

                if (TickAppliedSpawn.ContainsKey(spawnCommand.NetworkObjectId) &&
                    spawnCommand.TickWritten <= TickAppliedSpawn[spawnCommand.NetworkObjectId])
                {
                    continue;
                }

                TickAppliedSpawn[spawnCommand.NetworkObjectId] = spawnCommand.TickWritten;
                PostSpawnObject(spawnCommand, clientId);
            }
            message.ReadBuffer.Seek(endPosition);
        }

        internal void NetworkGetBehaviourVariable(UpdateCommand updateCommand, out NetworkBehaviour behaviour, out NetworkVariableBase variable, ulong srcClientId)
        {
            if (m_NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(updateCommand.NetworkObjectId,
                out NetworkObject networkObject))
            {
                behaviour = networkObject.GetNetworkBehaviourAtOrderIndex(updateCommand.BehaviourIndex);

                Debug.Assert(networkObject != null);

                variable = behaviour.NetworkVariableFields[updateCommand.VariableIndex];
            }
            else
            {
                variable = null;
                behaviour = null;
            }
        }

        internal int NetworkSendMessage(SnapshotDataMessage message, ulong clientId)
        {
            m_NetworkManager.SendPreSerializedMessage(message.WriteBuffer, TotalBufferMemory, ref message, NetworkDelivery.Unreliable, clientId);
            return 0;
        }
    }
}
