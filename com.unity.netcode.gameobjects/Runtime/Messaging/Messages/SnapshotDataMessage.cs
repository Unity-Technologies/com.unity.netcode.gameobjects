using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Netcode
{
    internal struct SnapshotDataMessage : INetworkMessage
    {
        public FastBufferWriter WriteBuffer;
        public FastBufferReader ReadBuffer;

        public SnapshotDataMessage(int x)
        {
            WriteBuffer = new FastBufferWriter(10000, Allocator.Temp);
            ReadBuffer = new FastBufferReader(WriteBuffer, Allocator.Temp);
        }

        public void Serialize(FastBufferWriter writer)
        {
            if (!writer.TryBeginWrite(WriteBuffer.Length))
            {
                // todo error handling
            }
            writer.CopyFrom(WriteBuffer);
        }

        public unsafe bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            ReadBuffer = reader;
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var systemOwner = context.SystemOwner;
            var senderId = context.SenderId;
            if (systemOwner is NetworkManager)
            {
                var networkManager = (NetworkManager) systemOwner;

                // todo: temporary hack around bug
                if (!networkManager.IsServer)
                {
                    senderId = networkManager.ServerClientId;
                }

                var snapshotSystem = networkManager.SnapshotSystem;
                snapshotSystem.HandleSnapshot(senderId, this);
            }
        }
    }
}

