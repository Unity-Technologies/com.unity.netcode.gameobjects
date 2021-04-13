using System;
using MLAPI.Configuration;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;
using Unity.Collections;
using UnityEngine;

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

    public class SnapshotSystem : INetworkUpdateSystem, IDisposable
    {
        private NativeArray<Entry> m_Entries = new NativeArray<Entry>(64, Allocator.Persistent);
        private NativeArray<char> m_Buffer = new NativeArray<char>(20000, Allocator.Persistent);

        private int m_LastEntry = 0;
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

                    for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                    {
                        var clientId = NetworkManager.Singleton.ConnectedClientsList[i].ClientId;
                        // todo: code here that just sends keepalive unreliable packets
                        var buffer = PooledNetworkBuffer.Get();
                        NetworkManager.Singleton.MessageSender.Send(clientId, NetworkConstants.SNAPSHOT_DATA, NetworkChannel.SnapshotExchange, buffer);
                        buffer.Dispose();
                        // todo: code here that reads the native array and sends it

                    }
                    break;
            }
        }

        public int Find(ulong networkObjectId, int index)
        {
            for(int i = 0; i < m_LastEntry; i++)
            {
                var entry = m_Entries[i];
                if (entry.m_NetworkObjectId == networkObjectId && entry.m_Index == index)
                {
                    return i;
                }
            }

            return Entry.k_NotFound;
        }

        public int AddEntry(ulong networkObjectId, int index)
        {
            int pos = m_LastEntry;
            var entry = m_Entries[m_LastEntry++];

            entry.m_NetworkObjectId = networkObjectId;
            entry.m_Index = (ushort)index;
            entry.m_Position = 0;
            entry.m_Length = 0;

            return pos;
        }

        public void Store(ulong networkObjectId, int index, INetworkVariable networkVariable)
        {
            int pos = Find(networkObjectId, index);

            if (pos == Entry.k_NotFound)
            {
                pos = AddEntry(networkObjectId, index);
            }

            // todo: write var into buffer, possibly adjusting entry's position and length
        }

    }
}
