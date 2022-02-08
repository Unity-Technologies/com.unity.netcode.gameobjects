using Unity.Collections;
using UnityEngine;

namespace Unity.Netcode
{
    internal struct SnapshotDataMessage : INetworkMessage
    {
        internal FastBufferWriter WriteBuffer;
        internal FastBufferReader ReadBuffer;

        // a constructor with an unused parameter is used because C# doesn't allow parameter-less constructors
        public SnapshotDataMessage(int bufferSize)
        {
            WriteBuffer = new FastBufferWriter(bufferSize, Allocator.Temp);
            ReadBuffer = new FastBufferReader(WriteBuffer, Allocator.Temp);
        }

        public void Serialize(FastBufferWriter writer)
        {
            if (!writer.TryBeginWrite(WriteBuffer.Length))
            {
                Debug.Log("Serialize. Not enough buffer");
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

