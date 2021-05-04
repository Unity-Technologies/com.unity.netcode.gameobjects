using System;
using System.Collections.Generic;
using System.IO;
using MLAPI.Configuration;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;
using UnityEngine;
using UnityEngine.UIElements;

namespace MLAPI
{
    internal struct Key
    {
        public ulong m_NetworkObjectId; // the NetworkObjectId of the owning GameObject
        public ushort m_BehaviourIndex; // the index of the behaviour in this GameObject
        public ushort m_VariableIndex; // the index of the variable in this NetworkBehaviour
    }
    internal struct Entry
    {
        public Key key;
        public ushort m_TickWritten; // the network tick at which this variable was set
        public ushort m_Position; // the offset in our m_Buffer
        public ushort m_Length; // the length of the data in m_Buffer
        public bool m_Fresh; // indicates entries that were just received

        public const int k_NotFound = -1;
    }

    internal class EntryBlock
    {
        private const int k_MaxVariables = 64;
        public byte[] m_Buffer = new byte[20000];
        public int m_Beg = 0; // todo: clarify usage. Right now, this is the beginning of the _free_ space.
        public int m_End = 0;

        public Entry[] m_Entries = new Entry[k_MaxVariables];
        public int m_LastEntry = 0;

        public int Find(Key key)
        {
            for (int i = 0; i < m_LastEntry; i++)
            {
                if (m_Entries[i].key.m_NetworkObjectId == key.m_NetworkObjectId &&
                    m_Entries[i].key.m_BehaviourIndex == key.m_BehaviourIndex &&
                    m_Entries[i].key.m_VariableIndex == key.m_VariableIndex)
                {
                    return i;
                }
            }

            return Entry.k_NotFound;
        }

        public int AddEntry(ulong networkObjectId, int behaviourIndex, int variableIndex)
        {
            var pos = m_LastEntry++;
            var entry = m_Entries[pos];

            entry.key.m_NetworkObjectId = networkObjectId;
            entry.key.m_BehaviourIndex = (ushort)behaviourIndex;
            entry.key.m_VariableIndex = (ushort)variableIndex;
            entry.m_TickWritten = 0;
            entry.m_Position = 0;
            entry.m_Length = 0;
            entry.m_Fresh = false;
            m_Entries[pos] = entry;

            return pos;
        }

        public void AllocateEntry(ref Entry entry, long size)
        {
            // todo: deal with free space
            // todo: deal with full buffer

            entry.m_Position = (ushort)m_Beg;
            entry.m_Length = (ushort)size;
            m_Beg += (int)size;
        }
    }

    public class SnapshotSystem : INetworkUpdateSystem, IDisposable
    {
        private EntryBlock m_Snapshot = new EntryBlock();
        private EntryBlock m_ReceivedSnapshot = new EntryBlock();

        public SnapshotSystem()
        {
            this.RegisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);
        }

        public void Dispose()
        {
            this.UnregisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.EarlyUpdate:
                {
                    if (NetworkManager.Singleton.IsServer)
                    {
                        for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                        {
                            var clientId = NetworkManager.Singleton.ConnectedClientsList[i].ClientId;
                            SendSnapshot(clientId);
                        }
                    }
                    else
                    {
                        SendSnapshot(NetworkManager.Singleton.ServerClientId);
                    }

                    DebugDisplayStore(m_Snapshot, "Entries");
                    DebugDisplayStore(m_ReceivedSnapshot, "Received Entries");
                    break;
                }
            }
        }

        private void SendSnapshot(ulong clientId)
        {
            // Send the entry index and the buffer where the variables are serialized
            using (var buffer = PooledNetworkBuffer.Get())
            {
                WriteIndex(buffer);
                WriteBuffer(buffer);

                NetworkManager.Singleton.MessageSender.Send(clientId, NetworkConstants.SNAPSHOT_DATA,
                    NetworkChannel.SnapshotExchange, buffer);
                buffer.Dispose();
            }
        }

        private void WriteIndex(NetworkBuffer buffer)
        {
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteInt16((short)m_Snapshot.m_LastEntry);

                for (var i = 0; i < m_Snapshot.m_LastEntry; i++)
                {
                    writer.WriteUInt64(m_Snapshot.m_Entries[i].key.m_NetworkObjectId);
                    writer.WriteUInt16(m_Snapshot.m_Entries[i].key.m_BehaviourIndex);
                    writer.WriteUInt16(m_Snapshot.m_Entries[i].key.m_VariableIndex);
                    writer.WriteUInt16(m_Snapshot.m_Entries[i].m_TickWritten);
                    writer.WriteUInt16(m_Snapshot.m_Entries[i].m_Position);
                    writer.WriteUInt16(m_Snapshot.m_Entries[i].m_Length);
                }
            }
        }

        private void WriteBuffer(NetworkBuffer buffer)
        {
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteUInt16((ushort)m_Snapshot.m_Beg);
            }
            Debug.Log(string.Format("Writing {0} bytes", m_Snapshot.m_Beg));

            // todo: this sends the whole buffer
            // we'll need to build a per-client list
            buffer.Write(m_Snapshot.m_Buffer, 0, m_Snapshot.m_Beg);
        }

        public void Store(ulong networkObjectId, int behaviourIndex, int variableIndex, INetworkVariable networkVariable)
        {
            Key k;
            k.m_NetworkObjectId = networkObjectId;
            k.m_BehaviourIndex = (ushort)behaviourIndex;
            k.m_VariableIndex = (ushort)variableIndex;

            int pos = m_Snapshot.Find(k);
            if (pos == Entry.k_NotFound)
            {
                pos = m_Snapshot.AddEntry(networkObjectId, behaviourIndex, variableIndex);
            }

            // write var into buffer, possibly adjusting entry's position and length
            using (var varBuffer = PooledNetworkBuffer.Get())
            {
                networkVariable.WriteDelta(varBuffer);
                if (varBuffer.Length > m_Snapshot.m_Entries[pos].m_Length)
                {
                    // allocate this Entry's buffer
                    m_Snapshot.AllocateEntry(ref m_Snapshot.m_Entries[pos], varBuffer.Length);
                }

                m_Snapshot.m_Entries[pos].m_TickWritten = NetworkManager.Singleton.NetworkTickSystem.GetTick();
                // Copy the serialized NetworkVariable into our buffer
                Buffer.BlockCopy(varBuffer.GetBuffer(), 0, m_Snapshot.m_Buffer, m_Snapshot.m_Entries[pos].m_Position, (int)varBuffer.Length);
            }
        }

        public void ReadSnapshot(Stream snapshotStream)
        {
            int snapshotSize = 0;
            using (var reader = PooledNetworkReader.Get(snapshotStream))
            {
                Entry entry;
                short entries = reader.ReadInt16();
                Debug.Log(string.Format("Got {0} entries", entries));

                for (var i = 0; i < entries; i++)
                {
                    entry.key.m_NetworkObjectId = reader.ReadUInt64();
                    entry.key.m_BehaviourIndex = reader.ReadUInt16();
                    entry.key.m_VariableIndex = reader.ReadUInt16();
                    entry.m_TickWritten = reader.ReadUInt16();
                    entry.m_Position = reader.ReadUInt16();
                    entry.m_Length = reader.ReadUInt16();
                    entry.m_Fresh = true;

                    int pos = m_ReceivedSnapshot.Find(entry.key);
                    if (pos == Entry.k_NotFound)
                    {
                        pos = m_ReceivedSnapshot.AddEntry(entry.key.m_NetworkObjectId, entry.key.m_BehaviourIndex, entry.key.m_VariableIndex);
                    }

                    if (m_ReceivedSnapshot.m_Entries[pos].m_Length < entry.m_Length)
                    {
                        m_ReceivedSnapshot.AllocateEntry(ref entry, entry.m_Length);
                    }
                    m_ReceivedSnapshot.m_Entries[pos] = entry;
                }

                snapshotSize = reader.ReadUInt16();
            }

            Debug.Log(string.Format("Reading {0} bytes", snapshotSize));
            snapshotStream.Read(m_ReceivedSnapshot.m_Buffer, 0, snapshotSize);

            for (var i = 0; i < m_ReceivedSnapshot.m_LastEntry; i++)
            {
                if (m_ReceivedSnapshot.m_Entries[i].m_Fresh && m_ReceivedSnapshot.m_Entries[i].m_TickWritten > 0)
                {
                    Debug.Log("applied variable");

                    var nv = FindNetworkVar(m_ReceivedSnapshot.m_Entries[i].key);

                    var stream = new MemoryStream(m_ReceivedSnapshot.m_Buffer, m_ReceivedSnapshot.m_Entries[i].m_Position,
                        m_ReceivedSnapshot.m_Entries[i].m_Length);

                    nv.ReadDelta(stream, false, 0, 0);
                }

                m_ReceivedSnapshot.m_Entries[i].m_Fresh = false;
            }
        }

        private INetworkVariable FindNetworkVar(Key key)
        {
            var spawnedObjects = NetworkManager.Singleton.SpawnManager.SpawnedObjects;

            if (spawnedObjects.ContainsKey(key.m_NetworkObjectId))
            {
                var behaviour = spawnedObjects[key.m_NetworkObjectId]
                    .GetNetworkBehaviourAtOrderIndex(key.m_BehaviourIndex);
                var nv = behaviour.NetworkVariableFields[key.m_VariableIndex];

                return nv;
            }

            return null;
        }

        private void DebugDisplayStore(EntryBlock block, string name)
        {
            string table = "=== Snapshot table === " + name + " ===\n";
            for (int i = 0; i < block.m_LastEntry; i++)
            {
                table += string.Format("NetworkObject {0}:{1}:{2} range [{3}, {4}] ", block.m_Entries[i].key.m_NetworkObjectId, block.m_Entries[i].key.m_BehaviourIndex,
                    block.m_Entries[i].key.m_VariableIndex, block.m_Entries[i].m_Position, block.m_Entries[i].m_Position + block.m_Entries[i].m_Length);

                for (int j = 0; j < block.m_Entries[i].m_Length && j < 4; j++)
                {
                    table += block.m_Buffer[block.m_Entries[i].m_Position + j].ToString("X2") + " ";
                }

                table += "\n";
            }
            Debug.Log(table);
        }
    }
}
