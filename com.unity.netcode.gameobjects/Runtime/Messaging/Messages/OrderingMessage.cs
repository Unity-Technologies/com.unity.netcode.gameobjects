using System;

namespace Unity.Netcode
{
    /// <summary>
    /// This particular struct is a little weird because it doesn't actually contain the data
    /// it's serializing. Instead, it contains references to the data it needs to do the
    /// serialization. This is due to the generally amorphous nature of network variable
    /// deltas, since they're all driven by custom virtual method overloads.
    /// </summary>
    internal struct OrderingMessage : INetworkMessage
    {
        public int Order;
        public int Hash;

        public void Serialize(FastBufferWriter writer)
        {
            if (!writer.TryBeginWrite(FastBufferWriter.GetWriteSize(Order) + FastBufferWriter.GetWriteSize(Hash)))
            {
                throw new OverflowException($"Not enough space in the buffer to write {nameof(OrderingMessage)}");
            }

            writer.WriteValue(Order);
            writer.WriteValue(Hash);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            if (!reader.TryBeginRead(FastBufferWriter.GetWriteSize(Order) + FastBufferWriter.GetWriteSize(Hash)))
            {
                throw new OverflowException($"Not enough data in the buffer to read {nameof(OrderingMessage)}");
            }

            reader.ReadValue(out Order);
            reader.ReadValue(out Hash);

            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            ((NetworkManager)context.SystemOwner).MessagingSystem.ReorderMessage(Order, Hash);
        }
    }
}
