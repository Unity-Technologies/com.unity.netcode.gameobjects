using System;

namespace Unity.Netcode
{
    /// <summary>
    /// Upon connecting, the host sends a series of OrderingMessage to the client so that it can make sure both sides
    /// have the same message types in the same positions in
    /// - MessagingSystem.m_MessageHandlers
    /// - MessagingSystem.m_ReverseTypeMap
    /// even if one side has extra messages (compilation, version, patch, or platform differences, etc...)
    ///
    /// The ConnectionRequestedMessage, ConnectionApprovedMessage and OrderingMessage are prioritized at the beginning
    /// of the mapping, to guarantee they can be exchanged before the two sides share their ordering
    /// The sorting used in also stable so that even if MessageType names share hashes, it will work most of the time
    /// </summary>
    internal struct OrderingMessage : INetworkMessage
    {
        public int Order;
        public uint Hash;

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
