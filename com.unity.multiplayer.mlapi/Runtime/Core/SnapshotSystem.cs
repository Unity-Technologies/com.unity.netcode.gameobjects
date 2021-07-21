using System;
using System.Collections.Generic;
using System.IO;
using MLAPI.Configuration;
using MLAPI.NetworkVariable;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;
using UnityEngine;

namespace MLAPI
{
    // Structure that acts as a key for a NetworkVariable
    // Allows telling which variable we're talking about.
    // Might include tick in a future milestone, to address past variable value
    internal struct VariableKey
    {
        public ulong NetworkObjectId; // the NetworkObjectId of the owning GameObject
        public ushort BehaviourIndex; // the index of the behaviour in this GameObject
        public ushort VariableIndex; // the index of the variable in this NetworkBehaviour
        public ushort TickWritten; // the network tick at which this variable was set
    }

    // Index for a NetworkVariable in our table of variables
    // Store when a variable was written and where the variable is serialized
    internal struct Entry
    {
        public VariableKey Key;
        public ushort Position; // the offset in our Buffer
        public ushort Length; // the Length of the data in Buffer

        public const int NotFound = -1;
    }

    internal struct SnapshotCommand
    {

    }

    internal struct SnapshotSpawnCommand
    {
        // identity
        internal ulong NetworkObjectId;

        // archetype
        internal uint GlobalObjectIdHash;
        internal bool IsSceneObject;

        // parameters
        internal bool IsPlayerObject;
        internal ulong OwnerClientId;
        internal ulong ParentNetworkId;
        internal Vector3 ObjectPosition;
        internal Quaternion ObjectRotation;
        internal Vector3 ObjectScale;

        internal ushort TickWritten;
    }

    // A table of NetworkVariables that constitutes a Snapshot.
    // Stores serialized NetworkVariables
    // todo --M1--
    // The Snapshot will change for M1b with memory management, instead of just FreeMemoryPosition, there will be data structure
    // around available buffer, etc.
    internal class Snapshot
    {
        // todo --M1-- functionality to grow these will be needed in a later milestone
        private const int k_MaxVariables = 2000;
        private const int k_MaxSpawns = 100;
        private const int k_BufferSize = 30000;

        public byte[] MainBuffer = new byte[k_BufferSize]; // buffer holding a snapshot in memory
        public byte[] RecvBuffer = new byte[k_BufferSize]; // buffer holding the received snapshot message

        internal IndexAllocator Allocator;

        public Entry[] Entries = new Entry[k_MaxVariables];
        public int LastEntry = 0;

        public SnapshotSpawnCommand[] Spawns = new SnapshotSpawnCommand[k_MaxSpawns];
        public int NumSpawns = 0;

        private MemoryStream m_BufferStream;
        private NetworkManager m_NetworkManager;
        private bool m_TickIndex;

        // indexed by ObjectId
        internal Dictionary<ulong, ushort> m_TickApplied = new Dictionary<ulong, ushort>();

        /// <summary>
        /// Constructor
        /// Allocated a MemoryStream to be reused for this Snapshot
        /// </summary>
        /// <param name="networkManager">The NetworkManaher this Snapshot uses. Needed upon receive to set Variables</param>
        /// <param name="tickIndex">Whether this Snapshot uses the tick as an index</param>
        public Snapshot(NetworkManager networkManager, bool tickIndex)
        {
            m_BufferStream = new MemoryStream(RecvBuffer, 0, k_BufferSize);
            // we ask for twice as many slots because there could end up being one free spot between each pair of slot used
            Allocator = new IndexAllocator(k_BufferSize, k_MaxVariables * 2);
            m_NetworkManager = networkManager;
            m_TickIndex = tickIndex;
        }

        public void Clear()
        {
            LastEntry = 0;
            Allocator.Reset();
        }

        /// <summary>
        /// Finds the position of a given NetworkVariable, given its key
        /// </summary>
        /// <param name="key">The key we're looking for</param>
        public int Find(VariableKey key)
        {
            // todo: Add a IEquatable interface for VariableKey. Rely on that instead.
            for (int i = 0; i < LastEntry; i++)
            {
                // todo: revisit how we store past ticks
                if (Entries[i].Key.NetworkObjectId == key.NetworkObjectId &&
                    Entries[i].Key.BehaviourIndex == key.BehaviourIndex &&
                    Entries[i].Key.VariableIndex == key.VariableIndex)
                {
                    return i;
                }
            }

            return Entry.NotFound;
        }

        /// <summary>
        /// Adds an entry in the table for a new key
        /// </summary>
        public int AddEntry(in VariableKey k)
        {
            var pos = LastEntry++;
            var entry = Entries[pos];

            entry.Key = k;
            entry.Position = 0;
            entry.Length = 0;
            Entries[pos] = entry;

            return pos;
        }

        internal void AddSpawn(SnapshotSpawnCommand command)
        {
            if (NumSpawns < k_MaxSpawns)
            {
                Debug.Log(string.Format("Spawning {0} {1} {2} {3} {4} {5} {6}", command.NetworkObjectId, command.GlobalObjectIdHash, command.IsSceneObject, command.IsPlayerObject, command.OwnerClientId, command.ParentNetworkId, command.ObjectPosition));

                Spawns[NumSpawns] = command;
                NumSpawns++;
            }
        }

        /// <summary>
        /// Write an Entry to send
        /// Must match ReadEntry
        /// </summary>
        /// <param name="writer">The writer to write the entry to</param>
        internal void WriteEntry(NetworkWriter writer, in Entry entry)
        {
            //todo: major refactor.
            // use blittable types and copy variable in memory locally
            // only serialize when put on the wire for network transfer
            writer.WriteUInt64(entry.Key.NetworkObjectId);
            writer.WriteUInt16(entry.Key.BehaviourIndex);
            writer.WriteUInt16(entry.Key.VariableIndex);
            writer.WriteUInt16(entry.Key.TickWritten);
            writer.WriteUInt16(entry.Position);
            writer.WriteUInt16(entry.Length);
        }

        internal void WriteSpawn(NetworkWriter writer, in SnapshotSpawnCommand spawn)
        {
            writer.WriteUInt64(spawn.NetworkObjectId);
            writer.WriteUInt64(spawn.GlobalObjectIdHash);
            writer.WriteBool(spawn.IsSceneObject);

            writer.WriteBool(spawn.IsPlayerObject);
            writer.WriteUInt64(spawn.OwnerClientId);
            writer.WriteUInt64(spawn.ParentNetworkId);
            writer.WriteVector3(spawn.ObjectPosition);
            writer.WriteRotation(spawn.ObjectRotation);
            writer.WriteVector3(spawn.ObjectScale);

            writer.WriteUInt16(spawn.TickWritten);
        }

        /// <summary>
        /// Read a received Entry
        /// Must match WriteEntry
        /// </summary>
        /// <param name="reader">The readed to read the entry from</param>
        internal Entry ReadEntry(NetworkReader reader)
        {
            Entry entry;
            entry.Key.NetworkObjectId = reader.ReadUInt64();
            entry.Key.BehaviourIndex = reader.ReadUInt16();
            entry.Key.VariableIndex = reader.ReadUInt16();
            entry.Key.TickWritten = reader.ReadUInt16();
            entry.Position = reader.ReadUInt16();
            entry.Length = reader.ReadUInt16();

            return entry;
        }

        internal SnapshotSpawnCommand ReadSpawn(NetworkReader reader)
        {
            SnapshotSpawnCommand command;

            command.NetworkObjectId = reader.ReadUInt64();
            command.GlobalObjectIdHash = (uint)reader.ReadUInt64();
            command.IsSceneObject = reader.ReadBool();
            command.IsPlayerObject = reader.ReadBool();
            command.OwnerClientId = reader.ReadUInt64();
            command.ParentNetworkId = reader.ReadUInt64();
            command.ObjectPosition = reader.ReadVector3();
            command.ObjectRotation = reader.ReadRotation();
            command.ObjectScale = reader.ReadVector3();

            command.TickWritten = reader.ReadUInt16();

            return command;
        }


        /// <summary>
        /// Allocate memory from the buffer for the Entry and update it to point to the right location
        /// </summary>
        /// <param name="entry">The entry to allocate for</param>
        /// <param name="size">The need size in bytes</param>
        public void AllocateEntry(ref Entry entry, int index, int size)
        {
            // todo: deal with full buffer

            if (entry.Length > 0)
            {
                Allocator.Deallocate(index);
            }

            int pos;
            bool ret = Allocator.Allocate(index, size, out pos);

            if (!ret)
            {
                //todo: error handling
            }

            entry.Position = (ushort)pos;
            entry.Length = (ushort)size;
        }

        /// <summary>
        /// Read the buffer part of a snapshot
        /// Must match WriteBuffer
        /// The stream is actually a memory stream and we seek to each variable position as we deserialize them
        /// </summary>
        /// <param name="reader">The NetworkReader to read our buffer of variables from</param>
        /// <param name="snapshotStream">The stream to read our buffer of variables from</param>
        internal void ReadBuffer(NetworkReader reader, Stream snapshotStream)
        {
            int snapshotSize = reader.ReadUInt16();
            snapshotStream.Read(RecvBuffer, 0, snapshotSize);
        }

        /// <summary>
        /// Read the snapshot index from a buffer
        /// Stores the entry. Allocates memory if needed. The actual buffer will be read later
        /// </summary>
        /// <param name="reader">The reader to read the index from</param>
        internal void ReadIndex(NetworkReader reader)
        {
            Entry entry;
            short entries = reader.ReadInt16();

            for (var i = 0; i < entries; i++)
            {
                bool added = false;

                entry = ReadEntry(reader);

                int pos = Find(entry.Key); // should return if there's anything more recent
                if (pos == Entry.NotFound)
                {
                    pos = AddEntry(entry.Key);
                    added = true;
                }

                // if we need to allocate more memory (the variable grew in size)
                if (Entries[pos].Length < entry.Length)
                {
                    AllocateEntry(ref entry, pos, entry.Length);
                    added = true;
                }

                if (added || entry.Key.TickWritten > Entries[pos].Key.TickWritten)
                {
                    Buffer.BlockCopy(RecvBuffer, entry.Position, MainBuffer, Entries[pos].Position, entry.Length);

                    Entries[pos] = entry;

                    // copy from readbuffer into buffer
                    var networkVariable = FindNetworkVar(Entries[pos].Key);
                    if (networkVariable != null)
                    {
                        m_BufferStream.Seek(Entries[pos].Position, SeekOrigin.Begin);
                        // todo: consider refactoring out in its own function to accomodate
                        // other ways to (de)serialize
                        // Not using keepDirtyDelta anymore which is great. todo: remove and check for the overall effect on > 2 player
                        networkVariable.ReadDelta(m_BufferStream, false);
                    }
                }
            }
        }

        internal void ReadSpawns(NetworkReader reader)
        {
            SnapshotSpawnCommand command;
            short count = reader.ReadInt16();

            for (var i = 0; i < count; i++)
            {
                command = ReadSpawn(reader);

                if (m_TickApplied.ContainsKey(command.NetworkObjectId) &&
                    command.TickWritten <= m_TickApplied[command.NetworkObjectId])
                {
                    continue;
                }

                m_TickApplied[command.NetworkObjectId] = command.TickWritten;

                // what is a soft sync ?
                // what are spawn payloads ?

                Debug.Log(string.Format("Spawning command ObjectId {0} Parent {1}", command.NetworkObjectId, command.ParentNetworkId));

                if (command.ParentNetworkId == command.NetworkObjectId)
                {
                    var networkObject = m_NetworkManager.SpawnManager.CreateLocalNetworkObject(false, command.GlobalObjectIdHash, command.OwnerClientId, null, command.ObjectPosition, command.ObjectRotation);
                    m_NetworkManager.SpawnManager.SpawnNetworkObjectLocally(networkObject, command.NetworkObjectId, true, command.IsPlayerObject, command.OwnerClientId, null, false, 0, false, false);
                }
                else
                {
                    var networkObject = m_NetworkManager.SpawnManager.CreateLocalNetworkObject(false, command.GlobalObjectIdHash, command.OwnerClientId, command.ParentNetworkId, command.ObjectPosition, command.ObjectRotation);
                    m_NetworkManager.SpawnManager.SpawnNetworkObjectLocally(networkObject, command.NetworkObjectId, true, command.IsPlayerObject, command.OwnerClientId, null, false, 0, false, false);
                }
            }
        }

        internal void ReadAcks(NetworkReader reader)
        {
            ushort ack = reader.ReadUInt16();
        }

        /// <summary>
        /// Helper function to find the NetworkVariable object from a key
        /// This will look into all spawned objects
        /// </summary>
        /// <param name="key">The key to search for</param>
        private INetworkVariable FindNetworkVar(VariableKey key)
        {
            var spawnedObjects = m_NetworkManager.SpawnManager.SpawnedObjects;

            if (spawnedObjects.ContainsKey(key.NetworkObjectId))
            {
                var behaviour = spawnedObjects[key.NetworkObjectId]
                    .GetNetworkBehaviourAtOrderIndex(key.BehaviourIndex);
                return behaviour.NetworkVariableFields[key.VariableIndex];
            }

            return null;
        }
    }

    internal class ClientData
    {
        internal ushort m_SequenceNumber = 0;
        internal ushort m_LastReceivedSequence = 0;
    }

    public class SnapshotSystem : INetworkUpdateSystem, IDisposable
    {
        private NetworkManager m_NetworkManager = NetworkManager.Singleton;
        private Snapshot m_Snapshot = new Snapshot(NetworkManager.Singleton, false);

        private Dictionary<ulong, ClientData> m_ClientData = new Dictionary<ulong, ClientData>();
        private Dictionary<ulong, Snapshot> m_ClientReceivedSnapshot = new Dictionary<ulong, Snapshot>();
        private Dictionary<ulong, ConnectionRtt> m_ClientRtts = new Dictionary<ulong, ConnectionRtt>();

        private ushort m_CurrentTick = 0;

        internal ConnectionRtt GetConnectionRtt(ulong clientId)
        {
            if (!m_ClientRtts.ContainsKey(clientId))
            {
                m_ClientRtts.Add(clientId, new ConnectionRtt());
            }

            return m_ClientRtts[clientId];
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// Registers the snapshot system for early updates
        public SnapshotSystem()
        {
            this.RegisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// Unregisters the snapshot system from early updates
        public void Dispose()
        {
            this.UnregisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            if (!NetworkManager.UseSnapshot)
            {
                return;
            }

            if (updateStage == NetworkUpdateStage.EarlyUpdate)
            {
                var tick = m_NetworkManager.NetworkTickSystem.GetTick();

                if (tick != m_CurrentTick)
                {
                    m_CurrentTick = tick;
                    if (m_NetworkManager.IsServer)
                    {
                        for (int i = 0; i < m_NetworkManager.ConnectedClientsList.Count; i++)
                        {
                            var clientId = m_NetworkManager.ConnectedClientsList[i].ClientId;
                            SendSnapshot(clientId);
                        }
                    }
                    else if (m_NetworkManager.IsConnectedClient)
                    {
                        SendSnapshot(m_NetworkManager.ServerClientId);
                    }
                }
            }
        }

        // todo --M1--
        // for now, the full snapshot is always sent
        // this will change significantly
        /// <summary>
        /// Send the snapshot to a specific client
        /// </summary>
        /// <param name="clientId">The client index to send to</param>
        private void SendSnapshot(ulong clientId)
        {
            if (!m_ClientData.ContainsKey(clientId))
            {
                m_ClientData.Add(clientId, new ClientData());
            }

            // Send the entry index and the buffer where the variables are serialized
            using (var buffer = PooledNetworkBuffer.Get())
            {
                var sequence = m_ClientData[clientId].m_SequenceNumber;
                m_ClientData[clientId].m_SequenceNumber++;

                using (var writer = PooledNetworkWriter.Get(buffer))
                {
                    writer.WriteUInt16(m_CurrentTick);
                    writer.WriteUInt16(sequence);
                }

                WriteBuffer(buffer);
                WriteIndex(buffer);
                WriteSpawns(buffer);
                WriteAcks(buffer, clientId);

                m_NetworkManager.MessageSender.Send(clientId, NetworkConstants.SNAPSHOT_DATA,
                    NetworkChannel.SnapshotExchange, buffer);
            }
        }

        /// <summary>
        /// Write the snapshot index to a buffer
        /// </summary>
        /// <param name="buffer">The buffer to write the index to</param>
        private void WriteIndex(NetworkBuffer buffer)
        {
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteInt16((short)m_Snapshot.LastEntry);
                for (var i = 0; i < m_Snapshot.LastEntry; i++)
                {
                    m_Snapshot.WriteEntry(writer, in m_Snapshot.Entries[i]);
                }
            }
        }

        private void WriteSpawns(NetworkBuffer buffer)
        {
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteInt16((short)m_Snapshot.NumSpawns);
                for (var i = 0; i < m_Snapshot.NumSpawns; i++)
                {
                    m_Snapshot.WriteSpawn(writer, in m_Snapshot.Spawns[i]);
                }
            }
        }

        private void WriteAcks(NetworkBuffer buffer, ulong clientId)
        {
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteUInt16(m_ClientData[clientId].m_LastReceivedSequence);
            }
        }

        /// <summary>
        /// Write the buffer of a snapshot
        /// Must match ReadBuffer
        /// </summary>
        /// <param name="buffer">The NetworkBuffer to write our buffer of variables to</param>
        private void WriteBuffer(NetworkBuffer buffer)
        {
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteUInt16((ushort)m_Snapshot.Allocator.Range);
            }

            // todo --M1--
            // // this sends the whole buffer
            // we'll need to build a per-client list
            buffer.Write(m_Snapshot.MainBuffer, 0, m_Snapshot.Allocator.Range);
        }

        internal void Spawn(SnapshotSpawnCommand command)
        {
            command.TickWritten = m_CurrentTick;
            m_Snapshot.AddSpawn(command);
        }

        // todo: consider using a Key, instead of 3 ints, if it can be exposed
        /// <summary>
        /// Called by the rest of MLAPI when a NetworkVariable changed and need to go in our snapshot
        /// Might not happen for all variable on every frame. Might even happen more than once.
        /// </summary>
        /// <param name="networkVariable">The NetworkVariable to write, or rather, its INetworkVariable</param>
        public void Store(ulong networkObjectId, int behaviourIndex, int variableIndex, INetworkVariable networkVariable)
        {
            VariableKey k;
            k.NetworkObjectId = networkObjectId;
            k.BehaviourIndex = (ushort)behaviourIndex;
            k.VariableIndex = (ushort)variableIndex;
            k.TickWritten = m_NetworkManager.NetworkTickSystem.GetTick();

            int pos = m_Snapshot.Find(k);
            if (pos == Entry.NotFound)
            {
                pos = m_Snapshot.AddEntry(k);
            }

            m_Snapshot.Entries[pos].Key.TickWritten = k.TickWritten;

            WriteVariableToSnapshot(m_Snapshot, networkVariable, pos);
        }

        private void WriteVariableToSnapshot(Snapshot snapshot, INetworkVariable networkVariable, int index)
        {
            // write var into buffer, possibly adjusting entry's position and Length
            using (var varBuffer = PooledNetworkBuffer.Get())
            {
                networkVariable.WriteDelta(varBuffer);
                if (varBuffer.Length > snapshot.Entries[index].Length)
                {
                    // allocate this Entry's buffer
                    snapshot.AllocateEntry(ref snapshot.Entries[index], index, (int)varBuffer.Length);
                }

                // Copy the serialized NetworkVariable into our buffer
                Buffer.BlockCopy(varBuffer.GetBuffer(), 0, snapshot.MainBuffer, snapshot.Entries[index].Position, (int)varBuffer.Length);
            }
        }


        /// <summary>
        /// Entry point when a Snapshot is received
        /// This is where we read and store the received snapshot
        /// </summary>
        /// <param name="clientId">
        /// <param name="snapshotStream">The stream to read from</param>
        public void ReadSnapshot(ulong clientId, Stream snapshotStream)
        {
            ushort snapshotTick = default;

            if (!m_ClientData.ContainsKey(clientId))
            {
                m_ClientData.Add(clientId, new ClientData());
            }

            using (var reader = PooledNetworkReader.Get(snapshotStream))
            {
                snapshotTick = reader.ReadUInt16();
                var sequence = reader.ReadUInt16();

                // todo: check we didn't miss any and deal with gaps
                m_ClientData[clientId].m_LastReceivedSequence = sequence;

                m_Snapshot.ReadBuffer(reader, snapshotStream);
                m_Snapshot.ReadIndex(reader);
                m_Snapshot.ReadSpawns(reader);
                m_Snapshot.ReadAcks(reader);
            }

            // todo: handle acks
        }

        // todo --M1--
        // This is temporary debugging code. Once the feature is complete, we can consider removing it
        // But we could also leave it in in debug to help developers
        private void DebugDisplayStore(Snapshot block, string name)
        {
            string table = "=== Snapshot table === " + name + " ===\n";
            for (int i = 0; i < block.LastEntry; i++)
            {
                table += string.Format("NetworkVariable {0}:{1}:{2} written {5}, range [{3}, {4}] ", block.Entries[i].Key.NetworkObjectId, block.Entries[i].Key.BehaviourIndex,
                    block.Entries[i].Key.VariableIndex, block.Entries[i].Position, block.Entries[i].Position + block.Entries[i].Length, block.Entries[i].Key.TickWritten);

                for (int j = 0; j < block.Entries[i].Length && j < 4; j++)
                {
                    table += block.MainBuffer[block.Entries[i].Position + j].ToString("X2") + " ";
                }

                table += "\n";
            }
            Debug.Log(table);
        }
    }
}
