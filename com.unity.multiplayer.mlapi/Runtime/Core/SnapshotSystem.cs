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
    internal struct Entry
    {
        public ulong m_NetworkObjectId; // the NetworkObjectId of the owning GameObject
        public ushort m_Index; // the index of the variable in this GameObject
        public ushort m_Position; // the offset in our m_Buffer
        public ushort m_Length; // the length of the data in m_Buffer

        public const int k_NotFound = -1;
    }

    internal class EntryBlock
    {
        public Entry[] m_Entries = new Entry[64];
        public int m_LastEntry = 0;

        public int Find(ulong networkObjectId, int index)
        {
            for (int i = 0; i < m_LastEntry; i++)
            {
                if (m_Entries[i].m_NetworkObjectId == networkObjectId && m_Entries[i].m_Index == index)
                {
                    return i;
                }
            }

            return Entry.k_NotFound;
        }

        public int AddEntry(ulong networkObjectId, int index)
        {
            var pos = m_LastEntry++;
            var entry = m_Entries[pos];

            entry.m_NetworkObjectId = networkObjectId;
            entry.m_Index = (ushort)index;
            entry.m_Position = 0;
            entry.m_Length = 0;
            m_Entries[pos] = entry;

            return pos;
        }

    }

    public class SnapshotSystem : INetworkUpdateSystem, IDisposable
    {
        private EntryBlock m_Snapshot = new EntryBlock();
        private EntryBlock m_ReceivedSnapshot = new EntryBlock();

        byte[] m_Buffer = new byte[20000];
        private int m_Beg = 0;
        private int m_End = 0;

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

                    // todo: ConnectedClientsList is only valid on the host
                    for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                    {
                        var clientId = NetworkManager.Singleton.ConnectedClientsList[i].ClientId;


                        // Send the entry index and the buffer where the variables are serialized
                        var buffer = PooledNetworkBuffer.Get();

                        WriteIndex(buffer);
                        WriteBuffer(buffer);

                        NetworkManager.Singleton.MessageSender.Send(clientId, NetworkConstants.SNAPSHOT_DATA, NetworkChannel.SnapshotExchange, buffer);
                        buffer.Dispose();
                    }

                    DebugDisplayStore(m_Snapshot.m_Entries, m_Snapshot.m_LastEntry, "Entries");
                    DebugDisplayStore(m_Snapshot.m_Entries, m_Snapshot.m_LastEntry, "Received Entries");
                    break;
            }
        }

        private void WriteIndex(NetworkBuffer buffer)
        {
            using (var writer = PooledNetworkWriter.Get(buffer))
            {
                writer.WriteInt16((short)m_Snapshot.m_LastEntry);

                for (var i = 0; i < m_Snapshot.m_LastEntry; i++)
                {
                    writer.WriteUInt64(m_Snapshot.m_Entries[i].m_NetworkObjectId);
                    writer.WriteUInt16(m_Snapshot.m_Entries[i].m_Index);
                    writer.WriteUInt16(m_Snapshot.m_Entries[i].m_Position);
                    writer.WriteUInt16(m_Snapshot.m_Entries[i].m_Length);
                }
            }
        }

        private void WriteBuffer(NetworkBuffer buffer)
        {
            // todo: this sends the whole buffer
            // we'll need to build a per-client list
            buffer.Write(m_Buffer, 0, m_End);
        }

        private void AllocateEntry(ref Entry entry, long size)
        {
            // todo: deal with free space
            // todo: deal with full buffer

            entry.m_Position = (ushort)m_Beg;
            entry.m_Length = (ushort)size;
            m_Beg += (int)size;
        }

        public void Store(ulong networkObjectId, int index, INetworkVariable networkVariable)
        {
            int pos = m_Snapshot.Find(networkObjectId, index);
            if (pos == Entry.k_NotFound)
            {
                pos = m_Snapshot.AddEntry(networkObjectId, index);
            }

            // write var into buffer, possibly adjusting entry's position and length
            using (var varBuffer = PooledNetworkBuffer.Get())
            {
                networkVariable.WriteDelta(varBuffer);
                if (varBuffer.Length > m_Snapshot.m_Entries[pos].m_Length)
                {
                    // allocate this Entry's buffer
                    AllocateEntry(ref m_Snapshot.m_Entries[pos], varBuffer.Length);
                }

                // Copy the serialized NetworkVariable into our buffer
                Buffer.BlockCopy(varBuffer.GetBuffer(), 0, m_Buffer, m_Snapshot.m_Entries[pos].m_Position, (int)varBuffer.Length);
            }
        }

        public void ReadSnapshot(Stream snapshotStream)
        {
            // todo: this is sub-optimal, review
            List<int> entriesPositionToRead = new List<int>();


            using (var reader = PooledNetworkReader.Get(snapshotStream))
            {
                Entry entry;
                short entries = reader.ReadInt16();
                Debug.Log(string.Format("Got {0} entries", entries));

                for (var i = 0; i < entries; i++)
                {
                    entry.m_NetworkObjectId = reader.ReadUInt64();
                    entry.m_Index = reader.ReadUInt16();
                    entry.m_Position = reader.ReadUInt16();
                    entry.m_Length = reader.ReadUInt16();

                    int pos = m_ReceivedSnapshot.Find(entry.m_NetworkObjectId, entry.m_Index);
                    if (pos == Entry.k_NotFound)
                    {
                        pos = m_ReceivedSnapshot.AddEntry(entry.m_NetworkObjectId, entry.m_Index);
                    }

                    if (m_ReceivedSnapshot.m_Entries[pos].m_Length < entry.m_Length)
                    {
                        AllocateEntry(ref entry, entry.m_Length);
                    }
                    m_ReceivedSnapshot.m_Entries[pos] = entry;

                    entriesPositionToRead.Add(pos);
                }
            }

            foreach (var pos in entriesPositionToRead)
            {
                snapshotStream.Read(m_Buffer, m_ReceivedSnapshot.m_Entries[pos].m_Position, m_ReceivedSnapshot.m_Entries[pos].m_Length);
            }
        }

        private void DebugDisplayStore(Entry[] entries, int entryLength, string name)
        {
            string table = "=== Snapshot table === " + name + " ===\n";
            for (int i = 0; i < entryLength; i++)
            {
                table += string.Format("NetworkObject {0}:{1} range [{2}, {3}]\n", entries[i].m_NetworkObjectId,
                    entries[i].m_Index, entries[i].m_Position, entries[i].m_Position + entries[i].m_Length);
            }
            Debug.Log(table);
        }
    }
}
