using System;
using Unity.Networking.Transport;
#if UTP_TRANSPORT_2_0_ABOVE
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Unity.Netcode.Transports.UTP
{
    /// <summary>Queue for batched messages received through UTP.</summary>
    /// <remarks>This is meant as a companion to <see cref="BatchedSendQueue"/>.</remarks>
    internal class BatchedReceiveQueue
    {
        private byte[] m_Data;
        private int m_Offset;
        private int m_Length;

        public bool IsEmpty => m_Length <= 0;

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
#if UTP_TRANSPORT_2_0_ABOVE
                    reader.ReadBytesUnsafe(dataPtr, reader.Length);
#else
                    reader.ReadBytes(dataPtr, reader.Length);
#endif
                }
            }

            m_Offset = 0;
            m_Length = reader.Length;
        }

        /// <summary>
        /// Push the entire data from a <see cref="DataStreamReader"/> (as returned by popping an
        /// event from a <see cref="NetworkDriver">) to the queue.
        /// </summary>
        /// <param name="reader">The <see cref="DataStreamReader"/> to push the data of.</param>
        public void PushReader(DataStreamReader reader)
        {
            // Resize the array and copy the existing data to the beginning if there's not enough
            // room to copy the reader's data at the end of the existing data.
            var available = m_Data.Length - (m_Offset + m_Length);
            if (available < reader.Length)
            {
                if (m_Length > 0)
                {
                    Array.Copy(m_Data, m_Offset, m_Data, 0, m_Length);
                }

                m_Offset = 0;

                while (m_Data.Length - m_Length < reader.Length)
                {
                    Array.Resize(ref m_Data, m_Data.Length * 2);
                }
            }

            unsafe
            {
                fixed (byte* dataPtr = m_Data)
                {
#if UTP_TRANSPORT_2_0_ABOVE
                    reader.ReadBytesUnsafe(dataPtr + m_Offset + m_Length, reader.Length);
#else
                    reader.ReadBytes(dataPtr + m_Offset + m_Length, reader.Length);
#endif
                }
            }

            m_Length += reader.Length;
        }

        /// <summary>Pop the next full message in the queue.</summary>
        /// <returns>The message, or the default value if no more full messages.</returns>
        public ArraySegment<byte> PopMessage()
        {
            if (m_Length < sizeof(int))
            {
                return default;
            }

            var messageLength = BitConverter.ToInt32(m_Data, m_Offset);

            if (m_Length - sizeof(int) < messageLength)
            {
                return default;
            }

            var data = new ArraySegment<byte>(m_Data, m_Offset + sizeof(int), messageLength);

            m_Offset += sizeof(int) + messageLength;
            m_Length -= sizeof(int) + messageLength;

            return data;
        }
    }
}
