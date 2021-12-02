using System;
using Unity.Collections;
using Unity.Networking.Transport;

namespace Unity.Netcode.UTP.Utilities
{
    /// <summary>Queue for batched messages received through UTP.</summary>
    /// <remarks>This is meant as a companion to <see cref="BatchedSendQueue"/>.</remarks>
    internal class BatchedReceiveQueue
    {
        private byte[] m_Data;
        private DataStreamReader m_Reader;
        private int m_ReaderOffset;

        public bool IsEmpty => m_ReaderOffset >= m_Reader.Length;

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

            m_Reader = reader;
            m_ReaderOffset = 0;
        }

        /// <summary>Pop the next message in the queue.</summary>
        /// <returns>The message, or the default value if no more messages.</returns>
        public ArraySegment<byte> PopMessage()
        {
            if (m_ReaderOffset >= m_Reader.Length)
            {
                return default;
            }

            m_Reader.SeekSet(m_ReaderOffset);

            var messageLength = m_Reader.ReadInt();
            m_ReaderOffset += sizeof(int);

            var data = new ArraySegment<byte>(m_Data, m_ReaderOffset, messageLength);
            m_ReaderOffset += messageLength;

            return data;
        }
    }
}