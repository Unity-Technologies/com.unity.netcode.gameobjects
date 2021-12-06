using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Netcode
{
    internal struct SnapshotDataMessage : INetworkMessage
    {
        internal FastBufferWriter WriteBuffer;
        internal FastBufferReader ReadBuffer;
        private int m_BufferSize;

        // a constructor with an unused parameter is used because C# doesn't allow parameter-less constructors
        public SnapshotDataMessage(int unused)
        {
            m_BufferSize = 10000;
            WriteBuffer = new FastBufferWriter(m_BufferSize, Allocator.Temp);
            ReadBuffer = new FastBufferReader(WriteBuffer, Allocator.Temp);
        }

        public void Serialize(FastBufferWriter writer)
        {
            // grow WriteBuffer in an amortized linear fashion
            if (WriteBuffer.Length > m_BufferSize)
            {
                m_BufferSize = Math.Max(2 * m_BufferSize, WriteBuffer.Length);
                WriteBuffer = new FastBufferWriter(m_BufferSize, Allocator.Temp);
                ReadBuffer = new FastBufferReader(WriteBuffer, Allocator.Temp);
            }

            // this will succeed because the above grows the buffer
            writer.TryBeginWrite(WriteBuffer.Length);
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
                var networkManager = (NetworkManager)systemOwner;

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

