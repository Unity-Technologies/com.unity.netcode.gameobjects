using System;
using Unity.Networking.Transport;

namespace Unity.Netcode.UTP.Utilities
{
    /// <summary>Queue for batched messages received through UTP.</summary>
    /// <remarks>This is meant as a companion to <see cref="BatchedSendQueue"/>.</remarks>
    internal class BatchedReceiveQueue
    {
        private byte[] m_Data;
        private int m_Offset;

        private int Length => m_Data.Length - m_Offset;

        public bool IsEmpty => m_Offset >= m_Data.Length;

        /// <summary>
        /// Construct a new receive queue from a <see cref="DataStreamReader"/> returned by
        /// <see cref="NetworkDriver"/> when popping a data event.
        /// </summary>
        /// <param name="reader">The <see cref="DataStreamReader"/> to construct from.</param>
        public BatchedReceiveQueue(DataStreamReader reader)
        {
            m_Data = new byte[reader.Length];
            unsafe
            {
                fixed (byte* dataPtr = m_Data)
                {
                    reader.ReadBytes(dataPtr, reader.Length);
                }
            }

            m_Offset = 0;
        }

        /// <summary>Pop the next full message in the queue.</summary>
        /// <returns>The message, or the default value if no more full messages.</returns>
        public ArraySegment<byte> PopMessage()
        {
            if (Length < sizeof(int))
            {
                return default;
            }

            var messageLength = BitConverter.ToInt32(m_Data, m_Offset);
            m_Offset += sizeof(int);

            if (Length < messageLength)
            {
                m_Offset -= sizeof(int);
                return default;
            }

            var data = new ArraySegment<byte>(m_Data, m_Offset, messageLength);
            m_Offset += messageLength;

            return data;
        }
    }
}
