using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Netcode
{
    // Structure that acts as a key for a NetworkVariable
    // Allows telling which variable we're talking about.
    // Might include tick in a future milestone, to address past variable value
    internal struct VariableKey
    {
        internal ulong NetworkObjectId; // the NetworkObjectId of the owning GameObject
        internal ushort BehaviourIndex; // the index of the behaviour in this GameObject
        internal ushort VariableIndex; // the index of the variable in this NetworkBehaviour
        internal int TickWritten; // the network tick at which this variable was set
    }

    // Index for a NetworkVariable in our table of variables
    // Store when a variable was written and where the variable is serialized
    internal struct Entry
    {
        internal VariableKey Key;
        internal ushort Position; // the offset in our Buffer
        internal ushort Length; // the Length of the data in Buffer

        internal const int NotFound = -1;
    }

    internal struct SnapshotDespawnCommand
    {
        // identity
        internal ulong NetworkObjectId;

        // snapshot internal
        internal int TickWritten;
        internal List<ulong> TargetClientIds;
        internal int TimesWritten;
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

        // snapshot internal
        internal int TickWritten;
        internal List<ulong> TargetClientIds;
        internal int TimesWritten;
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
        private int m_MaxSpawns = 100;
        private int m_MaxDespawns = 100;

        private const int k_BufferSize = 30000;

        internal byte[] MainBuffer = new byte[k_BufferSize]; // buffer holding a snapshot in memory
        internal byte[] RecvBuffer = new byte[k_BufferSize]; // buffer holding the received snapshot message

        internal IndexAllocator Allocator;

        internal Entry[] Entries = new Entry[k_MaxVariables];
        internal int LastEntry = 0;

        internal SnapshotSpawnCommand[] Spawns;
        internal int NumSpawns = 0;

        internal SnapshotDespawnCommand[] Despawns;
        internal int NumDespawns = 0;

        internal NetworkManager NetworkManager;

        // indexed by ObjectId
        internal Dictionary<ulong, int> TickAppliedSpawn = new Dictionary<ulong, int>();
        internal Dictionary<ulong, int> TickAppliedDespawn = new Dictionary<ulong, int>();

        /// <summary>
        /// Constructor
        /// Allocated a MemoryStream to be reused for this Snapshot
        /// </summary>
        internal Snapshot()
        {
            // we ask for twice as many slots because there could end up being one free spot between each pair of slot used
            Allocator = new IndexAllocator(k_BufferSize, k_MaxVariables * 2);
            Spawns = new SnapshotSpawnCommand[m_MaxSpawns];
            Despawns = new SnapshotDespawnCommand[m_MaxDespawns];
        }

        internal void Clear()
        {
            LastEntry = 0;
            Allocator.Reset();
        }

        /// <summary>
        /// Finds the position of a given NetworkVariable, given its key
        /// </summary>
        /// <param name="key">The key we're looking for</param>
        internal int Find(VariableKey key)
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
        internal int AddEntry(in VariableKey k)
        {
            var pos = LastEntry++;
            var entry = Entries[pos];

            entry.Key = k;
            entry.Position = 0;
            entry.Length = 0;
            Entries[pos] = entry;

            return pos;
        }

        internal List<ulong> GetClientList()
        {
            List<ulong> clientList;
            clientList = new List<ulong>();

            if (!NetworkManager.IsServer)
            {
                clientList.Add(NetworkManager.ServerClientId);
            }
            else
            {
                foreach (var clientId in NetworkManager.ConnectedClientsIds)
                {
                    if (clientId != NetworkManager.ServerClientId)
                    {
                        clientList.Add(clientId);
                    }
                }
            }

            return clientList;
        }

        internal void AddSpawn(SnapshotSpawnCommand command)
        {
            if (NumSpawns >= m_MaxSpawns)
            {
                Array.Resize(ref Spawns, 2 * m_MaxSpawns);
                m_MaxSpawns = m_MaxSpawns * 2;
                // Debug.Log($"[JEFF] spawn size is now {m_MaxSpawns}");
            }

            if (NumSpawns < m_MaxSpawns)
            {
                if (command.TargetClientIds == default)
                {
                    command.TargetClientIds = GetClientList();
                }

                // todo: store, for each client, the spawn not ack'ed yet,
                // to prevent sending despawns to them.
                // for clientData in client list
                // clientData.SpawnSet.Add(command.NetworkObjectId);

                // todo:
                // this 'if' might be temporary, but is needed to help in debugging
                // or maybe it stays
                if (command.TargetClientIds.Count > 0)
                {
                    Spawns[NumSpawns] = command;
                    NumSpawns++;
                }
            }
        }

        internal void AddDespawn(SnapshotDespawnCommand command)
        {
            if (NumDespawns >= m_MaxDespawns)
            {
                Array.Resize(ref Despawns, 2 * m_MaxDespawns);
                m_MaxDespawns = m_MaxDespawns * 2;
                // Debug.Log($"[JEFF] despawn size is now {m_MaxDespawns}");
            }

            if (NumDespawns < m_MaxDespawns)
            {
                if (command.TargetClientIds == default)
                {
                    command.TargetClientIds = GetClientList();
                }
                if (command.TargetClientIds.Count > 0)
                {
                    Despawns[NumDespawns] = command;
                    NumDespawns++;
                }
            }
        }

        internal ClientData.SentSpawn GetSpawnData(in ClientData clientData, in SnapshotSpawnCommand spawn, out SnapshotDataMessage.SpawnData data)
        {
            // remember which spawn we sent this connection with which sequence number
            // that way, upon ack, we can track what is being ack'ed
            ClientData.SentSpawn sentSpawn;
            sentSpawn.ObjectId = spawn.NetworkObjectId;
            sentSpawn.Tick = spawn.TickWritten;
            sentSpawn.SequenceNumber = clientData.SequenceNumber;

            data = new SnapshotDataMessage.SpawnData
            {
                NetworkObjectId = spawn.NetworkObjectId,
                Hash = spawn.GlobalObjectIdHash,
                IsSceneObject = spawn.IsSceneObject,

                IsPlayerObject = spawn.IsPlayerObject,
                OwnerClientId = spawn.OwnerClientId,
                ParentNetworkId = spawn.ParentNetworkId,
                Position = spawn.ObjectPosition,
                Rotation = spawn.ObjectRotation,
                Scale = spawn.ObjectScale,

                TickWritten = spawn.TickWritten
            };
            return sentSpawn;
        }

        internal ClientData.SentSpawn GetDespawnData(in ClientData clientData, in SnapshotDespawnCommand despawn, out SnapshotDataMessage.DespawnData data)
        {
            // remember which spawn we sent this connection with which sequence number
            // that way, upon ack, we can track what is being ack'ed
            ClientData.SentSpawn sentSpawn;
            sentSpawn.ObjectId = despawn.NetworkObjectId;
            sentSpawn.Tick = despawn.TickWritten;
            sentSpawn.SequenceNumber = clientData.SequenceNumber;

            data = new SnapshotDataMessage.DespawnData
            {
                NetworkObjectId = despawn.NetworkObjectId,
                TickWritten = despawn.TickWritten
            };

            return sentSpawn;
        }
        /// <summary>
        /// Read a received Entry
        /// Must match WriteEntry
        /// </summary>
        /// <param name="data">Deserialized snapshot entry data</param>
        internal Entry ReadEntry(SnapshotDataMessage.EntryData data)
        {
            Entry entry;
            entry.Key.NetworkObjectId = data.NetworkObjectId;
            entry.Key.BehaviourIndex = data.BehaviourIndex;
            entry.Key.VariableIndex = data.VariableIndex;
            entry.Key.TickWritten = data.TickWritten;
            entry.Position = data.Position;
            entry.Length = data.Length;

            return entry;
        }

        internal SnapshotSpawnCommand ReadSpawn(SnapshotDataMessage.SpawnData data)
        {
            var command = new SnapshotSpawnCommand();

            command.NetworkObjectId = data.NetworkObjectId;
            command.GlobalObjectIdHash = data.Hash;
            command.IsSceneObject = data.IsSceneObject;
            command.IsPlayerObject = data.IsPlayerObject;
            command.OwnerClientId = data.OwnerClientId;
            command.ParentNetworkId = data.ParentNetworkId;
            command.ObjectPosition = data.Position;
            command.ObjectRotation = data.Rotation;
            command.ObjectScale = data.Scale;

            command.TickWritten = data.TickWritten;

            return command;
        }

        internal SnapshotDespawnCommand ReadDespawn(SnapshotDataMessage.DespawnData data)
        {
            var command = new SnapshotDespawnCommand();

            command.NetworkObjectId = data.NetworkObjectId;
            command.TickWritten = data.TickWritten;

            return command;
        }

        /// <summary>
        /// Allocate memory from the buffer for the Entry and update it to point to the right location
        /// </summary>
        /// <param name="entry">The entry to allocate for</param>
        /// <param name="size">The need size in bytes</param>
        internal void AllocateEntry(ref Entry entry, int index, int size)
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
        /// <param name="message">The message to pull the buffer from</param>
        internal void ReadBuffer(in SnapshotDataMessage message)
        {
            RecvBuffer = message.ReceiveMainBuffer.ToArray(); // Note: Allocates
        }

        /// <summary>
        /// Read the snapshot index from a buffer
        /// Stores the entry. Allocates memory if needed. The actual buffer will be read later
        /// </summary>
        /// <param name="message">The message to read the index from</param>
        internal void ReadIndex(in SnapshotDataMessage message)
        {
            Entry entry;

            for (var i = 0; i < message.Entries.Length; i++)
            {
                bool added = false;

                entry = ReadEntry(message.Entries[i]);

                int pos = Find(entry.Key);// should return if there's anything more recent
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
                        unsafe
                        {
                            // This avoids copies - using Allocator.None creates a direct memory view into the buffer.
                            fixed (byte* buffer = RecvBuffer)
                            {
                                var reader = new FastBufferReader(buffer, Collections.Allocator.None, RecvBuffer.Length);
                                using (reader)
                                {
                                    reader.Seek(Entries[pos].Position);
                                    // todo: consider refactoring out in its own function to accomodate
                                    // other ways to (de)serialize
                                    // Not using keepDirtyDelta anymore which is great. todo: remove and check for the overall effect on > 2 player
                                    networkVariable.ReadDelta(reader, false);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void ReadSpawns(in SnapshotDataMessage message)
        {
            SnapshotSpawnCommand spawnCommand;
            SnapshotDespawnCommand despawnCommand;

            for (var i = 0; i < message.Spawns.Length; i++)
            {
                spawnCommand = ReadSpawn(message.Spawns[i]);

                if (TickAppliedSpawn.ContainsKey(spawnCommand.NetworkObjectId) &&
                    spawnCommand.TickWritten <= TickAppliedSpawn[spawnCommand.NetworkObjectId])
                {
                    continue;
                }

                TickAppliedSpawn[spawnCommand.NetworkObjectId] = spawnCommand.TickWritten;

                // Debug.Log($"[Spawn] {spawnCommand.NetworkObjectId} {spawnCommand.TickWritten}");

                if (spawnCommand.ParentNetworkId == spawnCommand.NetworkObjectId)
                {
                    var networkObject = NetworkManager.SpawnManager.CreateLocalNetworkObject(false, spawnCommand.GlobalObjectIdHash, spawnCommand.OwnerClientId, null, spawnCommand.ObjectPosition, spawnCommand.ObjectRotation);
                    NetworkManager.SpawnManager.SpawnNetworkObjectLocally(networkObject, spawnCommand.NetworkObjectId, true, spawnCommand.IsPlayerObject, spawnCommand.OwnerClientId, false);
                }
                else
                {
                    var networkObject = NetworkManager.SpawnManager.CreateLocalNetworkObject(false, spawnCommand.GlobalObjectIdHash, spawnCommand.OwnerClientId, spawnCommand.ParentNetworkId, spawnCommand.ObjectPosition, spawnCommand.ObjectRotation);
                    NetworkManager.SpawnManager.SpawnNetworkObjectLocally(networkObject, spawnCommand.NetworkObjectId, true, spawnCommand.IsPlayerObject, spawnCommand.OwnerClientId, false);
                }
            }
            for (var i = 0; i < message.Despawns.Length; i++)
            {
                despawnCommand = ReadDespawn(message.Despawns[i]);

                if (TickAppliedDespawn.ContainsKey(despawnCommand.NetworkObjectId) &&
                    despawnCommand.TickWritten <= TickAppliedDespawn[despawnCommand.NetworkObjectId])
                {
                    continue;
                }

                TickAppliedDespawn[despawnCommand.NetworkObjectId] = despawnCommand.TickWritten;

                // Debug.Log($"[DeSpawn] {despawnCommand.NetworkObjectId} {despawnCommand.TickWritten}");

                NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(despawnCommand.NetworkObjectId,
                    out NetworkObject networkObject);

                NetworkManager.SpawnManager.OnDespawnObject(networkObject, true);
            }
        }

        internal void ReadAcks(ulong clientId, ClientData clientData, in SnapshotDataMessage message, ConnectionRtt connection)
        {
            ushort ackSequence = message.Ack.LastReceivedSequence;
            ushort seqMask = message.Ack.ReceivedSequenceMask;

            // process the latest acknowledgment
            ProcessSingleAck(ackSequence, clientId, clientData, connection);

            // for each bit in the mask, acknowledge one message before
            while (seqMask != 0)
            {
                ackSequence--;
                // extract least bit
                if (seqMask % 2 == 1)
                {
                    ProcessSingleAck(ackSequence, clientId, clientData, connection);
                }
                // move to next bit
                seqMask >>= 1;
            }
        }

        internal void ProcessSingleAck(ushort ackSequence, ulong clientId, ClientData clientData, ConnectionRtt connection)
        {
            // look through the spawns sent
            for (int index = 0; index < clientData.SentSpawns.Count; /*no increment*/)
            {
                // needless copy, but I didn't find a way around
                ClientData.SentSpawn sent = clientData.SentSpawns[index];

                // for those with the sequence number being ack'ed
                if (sent.SequenceNumber == ackSequence)
                {
                    // remember the tick
                    if (!clientData.SpawnAck.ContainsKey(sent.ObjectId))
                    {
                        clientData.SpawnAck.Add(sent.ObjectId, sent.Tick);
                    }
                    else
                    {
                        clientData.SpawnAck[sent.ObjectId] = sent.Tick;
                    }

                    // check the spawn and despawn commands, find them, and if this is the last connection
                    // to ack, let's remove them
                    for (var i = 0; i < NumSpawns; i++)
                    {
                        if (Spawns[i].TickWritten == sent.Tick &&
                            Spawns[i].NetworkObjectId == sent.ObjectId)
                        {
                            Spawns[i].TargetClientIds.Remove(clientId);

                            if (Spawns[i].TargetClientIds.Count == 0)
                            {
                                // remove by moving the last spawn over
                                Spawns[i] = Spawns[NumSpawns - 1];
                                NumSpawns--;
                                break;
                            }
                        }
                    }
                    for (var i = 0; i < NumDespawns; i++)
                    {
                        if (Despawns[i].TickWritten == sent.Tick &&
                            Despawns[i].NetworkObjectId == sent.ObjectId)
                        {
                            Despawns[i].TargetClientIds.Remove(clientId);

                            if (Despawns[i].TargetClientIds.Count == 0)
                            {
                                // remove by moving the last spawn over
                                Despawns[i] = Despawns[NumDespawns - 1];
                                NumDespawns--;
                                break;
                            }
                        }
                    }

                    // remove current `sent`, by moving last over,
                    // as it was acknowledged.
                    // skip incrementing index
                    clientData.SentSpawns[index] = clientData.SentSpawns[clientData.SentSpawns.Count - 1];
                    clientData.SentSpawns.RemoveAt(clientData.SentSpawns.Count - 1);
                }
                else
                {
                    index++;
                }
            }

            // keep track of RTTs, using the sequence number acknowledgement as a marker
            connection.NotifyAck(ackSequence, Time.unscaledTime);
        }

        /// <summary>
        /// Helper function to find the NetworkVariable object from a key
        /// This will look into all spawned objects
        /// </summary>
        /// <param name="key">The key to search for</param>
        private NetworkVariableBase FindNetworkVar(VariableKey key)
        {
            var spawnedObjects = NetworkManager.SpawnManager.SpawnedObjects;

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
        internal struct SentSpawn // this struct also stores Despawns, not just Spawns
        {
            internal ulong SequenceNumber;
            internal ulong ObjectId;
            internal int Tick;
        }

        internal ushort SequenceNumber = 0; // the next sequence number to use for this client
        internal ushort LastReceivedSequence = 0; // the last sequence number received by this client
        internal ushort ReceivedSequenceMask = 0; // bitmask of the messages before the last one that we received.

        internal int NextSpawnIndex = 0; // index of the last spawn sent. Used to cycle through spawns (LRU scheme)
        internal int NextDespawnIndex = 0; // same as above, but for despawns.

        // by objectId
        // which spawns and despawns did this connection ack'ed ?
        internal Dictionary<ulong, int> SpawnAck = new Dictionary<ulong, int>();

        // list of spawn and despawns commands we sent, with sequence number
        // need to manage acknowledgements
        internal List<SentSpawn> SentSpawns = new List<SentSpawn>();
    }

    internal class SnapshotSystem : INetworkUpdateSystem, IDisposable
    {
        // temporary, debugging sentinels
        internal const ushort SentinelBefore = 0x4246;
        internal const ushort SentinelAfter = 0x89CE;

        private NetworkManager m_NetworkManager = default;
        private Snapshot m_Snapshot = default;

        // by clientId
        private Dictionary<ulong, ClientData> m_ClientData = new Dictionary<ulong, ClientData>();
        private Dictionary<ulong, ConnectionRtt> m_ConnectionRtts = new Dictionary<ulong, ConnectionRtt>();

        private int m_CurrentTick = NetworkTickSystem.NoTick;

        /// <summary>
        /// Constructor
        /// </summary>
        /// Registers the snapshot system for early updates, keeps reference to the NetworkManager
        internal SnapshotSystem(NetworkManager networkManager)
        {
            m_Snapshot = new Snapshot();

            m_NetworkManager = networkManager;
            m_Snapshot.NetworkManager = networkManager;

            this.RegisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);
        }

        internal ConnectionRtt GetConnectionRtt(ulong clientId)
        {
            if (!m_ConnectionRtts.ContainsKey(clientId))
            {
                m_ConnectionRtts.Add(clientId, new ConnectionRtt());
            }

            return m_ConnectionRtts[clientId];
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
            if (!m_NetworkManager.NetworkConfig.UseSnapshotDelta && !m_NetworkManager.NetworkConfig.UseSnapshotSpawn)
            {
                return;
            }

            if (updateStage == NetworkUpdateStage.EarlyUpdate)
            {
                var tick = m_NetworkManager.NetworkTickSystem.LocalTime.Tick;

                if (tick != m_CurrentTick)
                {
                    m_CurrentTick = tick;
                    if (m_NetworkManager.IsServer)
                    {
                        for (int i = 0; i < m_NetworkManager.ConnectedClientsList.Count; i++)
                        {
                            var clientId = m_NetworkManager.ConnectedClientsList[i].ClientId;

                            // don't send to ourselves
                            if (clientId != m_NetworkManager.ServerClientId)
                            {
                                SendSnapshot(clientId);
                            }
                        }
                    }
                    else if (m_NetworkManager.IsConnectedClient)
                    {
                        SendSnapshot(m_NetworkManager.ServerClientId);
                    }
                }

                // useful for debugging, but generates LOTS of spam
                // DebugDisplayStore();
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
            // make sure we have a ClientData and ConnectionRtt entry for each client
            if (!m_ClientData.ContainsKey(clientId))
            {
                m_ClientData.Add(clientId, new ClientData());
            }

            if (!m_ConnectionRtts.ContainsKey(clientId))
            {
                m_ConnectionRtts.Add(clientId, new ConnectionRtt());
            }

            m_ConnectionRtts[clientId].NotifySend(m_ClientData[clientId].SequenceNumber, Time.unscaledTime);

            var sequence = m_ClientData[clientId].SequenceNumber;
            var message = new SnapshotDataMessage
            {
                CurrentTick = m_CurrentTick,
                Sequence = sequence,
                Range = (ushort)m_Snapshot.Allocator.Range,

                // todo --M1--
                // this sends the whole buffer
                // we'll need to build a per-client list
                SendMainBuffer = m_Snapshot.MainBuffer,

                Ack = new SnapshotDataMessage.AckData
                {
                    LastReceivedSequence = m_ClientData[clientId].LastReceivedSequence,
                    ReceivedSequenceMask = m_ClientData[clientId].ReceivedSequenceMask
                }
            };


            // write the snapshot: buffer, index, spawns, despawns
            WriteIndex(ref message);
            WriteSpawns(ref message, clientId);

            m_NetworkManager.SendMessage(message, NetworkDelivery.Unreliable, clientId);

            m_ClientData[clientId].LastReceivedSequence = 0;

            // todo: this is incorrect (well, sub-optimal)
            // we should still continue ack'ing past messages, in case this one is dropped
            m_ClientData[clientId].ReceivedSequenceMask = 0;
            m_ClientData[clientId].SequenceNumber++;
        }

        // Checks if a given SpawnCommand should be written to a Snapshot Message
        // Performs exponential back off. To write a spawn a second time
        // two ticks must have gone by. To write it a third time, four ticks, etc...
        // This prioritize commands that have been re-sent less than others
        private bool ShouldWriteSpawn(in SnapshotSpawnCommand spawnCommand)
        {
            if (m_CurrentTick < spawnCommand.TickWritten)
            {
                return false;
            }

            // 63 as we can't shift more than that.
            var diff = Math.Min(63, m_CurrentTick - spawnCommand.TickWritten);

            // -1 to make the first resend immediate
            return (1 << diff) > (spawnCommand.TimesWritten - 1);
        }

        private bool ShouldWriteDespawn(in SnapshotDespawnCommand despawnCommand)
        {
            if (m_CurrentTick < despawnCommand.TickWritten)
            {
                return false;
            }

            // 63 as we can't shift more than that.
            var diff = Math.Min(63, m_CurrentTick - despawnCommand.TickWritten);

            // -1 to make the first resend immediate
            return (1 << diff) > (despawnCommand.TimesWritten - 1);
        }

        private void WriteSpawns(ref SnapshotDataMessage message, ulong clientId)
        {
            var spawnWritten = 0;
            var despawnWritten = 0;
            var overSize = false;

            ClientData clientData = m_ClientData[clientId];

            // this is needed because spawns being removed may have reduce the size below LRU position
            if (m_Snapshot.NumSpawns > 0)
            {
                clientData.NextSpawnIndex %= m_Snapshot.NumSpawns;
            }
            else
            {
                clientData.NextSpawnIndex = 0;
            }

            if (m_Snapshot.NumDespawns > 0)
            {
                clientData.NextDespawnIndex %= m_Snapshot.NumDespawns;
            }
            else
            {
                clientData.NextDespawnIndex = 0;
            }

            message.Spawns = new NativeList<SnapshotDataMessage.SpawnData>(m_Snapshot.NumSpawns, Allocator.TempJob);
            message.Despawns = new NativeList<SnapshotDataMessage.DespawnData>(m_Snapshot.NumDespawns, Allocator.TempJob);
            var spawnUsage = 0;

            for (var j = 0; j < m_Snapshot.NumSpawns && !overSize; j++)
            {
                var index = clientData.NextSpawnIndex;

                // todo: re-enable ShouldWriteSpawn, once we have a mechanism to not let despawn pass in front of spawns
                if (m_Snapshot.Spawns[index].TargetClientIds.Contains(clientId) /*&& ShouldWriteSpawn(m_Snapshot.Spawns[index])*/)
                {
                    spawnUsage += FastBufferWriter.GetWriteSize<SnapshotDataMessage.SpawnData>();

                    // limit spawn sizes, compare current pos to very first position we wrote to
                    if (spawnUsage > m_NetworkManager.NetworkConfig.SnapshotMaxSpawnUsage)
                    {
                        overSize = true;
                        break;
                    }
                    var sentSpawn = m_Snapshot.GetSpawnData(clientData, in m_Snapshot.Spawns[index], out var spawn);
                    message.Spawns.Add(spawn);

                    m_Snapshot.Spawns[index].TimesWritten++;
                    clientData.SentSpawns.Add(sentSpawn);
                    spawnWritten++;
                }
                clientData.NextSpawnIndex = (clientData.NextSpawnIndex + 1) % m_Snapshot.NumSpawns;
            }

            // even though we might have a spawn we could not fit, it's possible despawns will fit (they're smaller)

            // todo: this next line is commented for now because there's no check for a spawn command to have been
            // ack'ed before sending a despawn for the same object.
            // Uncommenting this line would allow some despawn to be sent while spawns are pending.
            // As-is it is overly restrictive but allows us to go forward without the spawn/despawn dependency check
            // overSize = false;

            for (var j = 0; j < m_Snapshot.NumDespawns && !overSize; j++)
            {
                var index = clientData.NextDespawnIndex;

                // todo: re-enable ShouldWriteSpawn, once we have a mechanism to not let despawn pass in front of spawns
                if (m_Snapshot.Despawns[index].TargetClientIds.Contains(clientId) /*&& ShouldWriteDespawn(m_Snapshot.Despawns[index])*/)
                {
                    spawnUsage += FastBufferWriter.GetWriteSize<SnapshotDataMessage.DespawnData>();

                    // limit spawn sizes, compare current pos to very first position we wrote to
                    if (spawnUsage > m_NetworkManager.NetworkConfig.SnapshotMaxSpawnUsage)
                    {
                        overSize = true;
                        break;
                    }
                    var sentDespawn = m_Snapshot.GetDespawnData(clientData, in m_Snapshot.Despawns[index], out var despawn);
                    message.Despawns.Add(despawn);
                    m_Snapshot.Despawns[index].TimesWritten++;
                    clientData.SentSpawns.Add(sentDespawn);
                    despawnWritten++;
                }
                clientData.NextDespawnIndex = (clientData.NextDespawnIndex + 1) % m_Snapshot.NumDespawns;
            }
        }

        /// <summary>
        /// Write the snapshot index to a buffer
        /// </summary>
        /// <param name="message">The message to write the index to</param>
        private void WriteIndex(ref SnapshotDataMessage message)
        {
            message.Entries = new NativeList<SnapshotDataMessage.EntryData>(m_Snapshot.LastEntry, Allocator.TempJob);
            for (var i = 0; i < m_Snapshot.LastEntry; i++)
            {
                var entryMeta = m_Snapshot.Entries[i];
                var entry = entryMeta.Key;
                message.Entries.Add(new SnapshotDataMessage.EntryData
                {
                    NetworkObjectId = entry.NetworkObjectId,
                    BehaviourIndex = entry.BehaviourIndex,
                    VariableIndex = entry.VariableIndex,
                    TickWritten = entry.TickWritten,
                    Position = entryMeta.Position,
                    Length = entryMeta.Length
                });
            }
        }

        internal void Spawn(SnapshotSpawnCommand command)
        {
            command.TickWritten = m_CurrentTick;
            m_Snapshot.AddSpawn(command);

            // Debug.Log($"[Spawn] {command.NetworkObjectId} {command.TickWritten}");
        }

        internal void Despawn(SnapshotDespawnCommand command)
        {
            command.TickWritten = m_CurrentTick;
            m_Snapshot.AddDespawn(command);

            // Debug.Log($"[DeSpawn] {command.NetworkObjectId} {command.TickWritten}");
        }

        // todo: consider using a Key, instead of 3 ints, if it can be exposed
        /// <summary>
        /// Called by the rest of the netcode when a NetworkVariable changed and need to go in our snapshot
        /// Might not happen for all variable on every frame. Might even happen more than once.
        /// </summary>
        /// <param name="networkVariable">The NetworkVariable to write, or rather, its INetworkVariable</param>
        internal void Store(ulong networkObjectId, int behaviourIndex, int variableIndex, NetworkVariableBase networkVariable)
        {
            VariableKey k;
            k.NetworkObjectId = networkObjectId;
            k.BehaviourIndex = (ushort)behaviourIndex;
            k.VariableIndex = (ushort)variableIndex;
            k.TickWritten = m_NetworkManager.NetworkTickSystem.LocalTime.Tick;

            int pos = m_Snapshot.Find(k);
            if (pos == Entry.NotFound)
            {
                pos = m_Snapshot.AddEntry(k);
            }

            m_Snapshot.Entries[pos].Key.TickWritten = k.TickWritten;

            WriteVariableToSnapshot(m_Snapshot, networkVariable, pos);
        }

        private unsafe void WriteVariableToSnapshot(Snapshot snapshot, NetworkVariableBase networkVariable, int index)
        {
            // write var into buffer, possibly adjusting entry's position and Length
            var varBuffer = new FastBufferWriter(MessagingSystem.NON_FRAGMENTED_MESSAGE_MAX_SIZE, Allocator.Temp);
            using (varBuffer)
            {
                networkVariable.WriteDelta(varBuffer);
                if (varBuffer.Length > snapshot.Entries[index].Length)
                {
                    // allocate this Entry's buffer
                    snapshot.AllocateEntry(ref snapshot.Entries[index], index, (int)varBuffer.Length);
                }

                fixed (byte* buffer = snapshot.MainBuffer)
                {
                    UnsafeUtility.MemCpy(buffer + snapshot.Entries[index].Position, varBuffer.GetUnsafePtr(), varBuffer.Length);
                }
            }
        }


        /// <summary>
        /// Entry point when a Snapshot is received
        /// This is where we read and store the received snapshot
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="message">The message to read from</param>
        internal void HandleSnapshot(ulong clientId, in SnapshotDataMessage message)
        {
            // make sure we have a ClientData entry for each client
            if (!m_ClientData.ContainsKey(clientId))
            {
                m_ClientData.Add(clientId, new ClientData());
            }

            if (message.Sequence >= m_ClientData[clientId].LastReceivedSequence)
            {
                if (m_ClientData[clientId].ReceivedSequenceMask != 0)
                {
                    // since each bit in ReceivedSequenceMask is relative to the last received sequence
                    // we need to shift all the bits by the difference in sequence
                    var shift = message.Sequence - m_ClientData[clientId].LastReceivedSequence;
                    if (shift < sizeof(ushort) * 8)
                    {
                        m_ClientData[clientId].ReceivedSequenceMask <<= shift;
                    }
                    else
                    {
                        m_ClientData[clientId].ReceivedSequenceMask = 0;
                    }
                }

                if (m_ClientData[clientId].LastReceivedSequence != 0)
                {
                    // because the bit we're adding for the previous ReceivedSequenceMask
                    // was implicit, it needs to be shift by one less
                    var shift = message.Sequence - 1 - m_ClientData[clientId].LastReceivedSequence;
                    if (shift < sizeof(ushort) * 8)
                    {
                        m_ClientData[clientId].ReceivedSequenceMask |= (ushort)(1 << shift);
                    }
                }

                m_ClientData[clientId].LastReceivedSequence = message.Sequence;
            }
            else
            {
                // todo: Missing: dealing with out-of-order message acknowledgments
                // we should set m_ClientData[clientId].ReceivedSequenceMask accordingly
                // testing this will require a way to reorder SnapshotMessages, which we lack at the moment
                //
                // without this, we incur extra retransmit, not a catastrophic failure
            }

            m_Snapshot.ReadBuffer(message);
            m_Snapshot.ReadIndex(message);
            m_Snapshot.ReadAcks(clientId, m_ClientData[clientId], message, GetConnectionRtt(clientId));
            m_Snapshot.ReadSpawns(message);
        }

        // todo --M1--
        // This is temporary debugging code. Once the feature is complete, we can consider removing it
        // But we could also leave it in in debug to help developers
        private void DebugDisplayStore()
        {
            string table = "=== Snapshot table ===\n";
            table += $"We're clientId {m_NetworkManager.LocalClientId}\n";

            table += "=== Variables ===\n";
            for (int i = 0; i < m_Snapshot.LastEntry; i++)
            {
                table += string.Format("NetworkVariable {0}:{1}:{2} written {5}, range [{3}, {4}] ", m_Snapshot.Entries[i].Key.NetworkObjectId, m_Snapshot.Entries[i].Key.BehaviourIndex,
                    m_Snapshot.Entries[i].Key.VariableIndex, m_Snapshot.Entries[i].Position, m_Snapshot.Entries[i].Position + m_Snapshot.Entries[i].Length, m_Snapshot.Entries[i].Key.TickWritten);

                for (int j = 0; j < m_Snapshot.Entries[i].Length && j < 4; j++)
                {
                    table += m_Snapshot.MainBuffer[m_Snapshot.Entries[i].Position + j].ToString("X2") + " ";
                }

                table += "\n";
            }

            table += "=== Spawns ===\n";

            for (int i = 0; i < m_Snapshot.NumSpawns; i++)
            {
                string targets = "";
                foreach (var target in m_Snapshot.Spawns[i].TargetClientIds)
                {
                    targets += target.ToString() + ", ";
                }
                table += $"Spawn Object Id {m_Snapshot.Spawns[i].NetworkObjectId}, Tick {m_Snapshot.Spawns[i].TickWritten}, Target {targets}\n";
            }

            table += $"=== RTTs ===\n";
            foreach (var iterator in m_ConnectionRtts)
            {
                table += $"client {iterator.Key} RTT {iterator.Value.GetRtt().AverageSec}\n";
            }

            table += "======\n";
            Debug.Log(table);
        }
    }
}
