using System;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;
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
        /// <summary>
        /// This is the data itself. This array may contain already-popped data.
        /// The data read comes from a window into this array going from m_Offset to
        /// m_Offset + m_Length
        /// </summary>
        private byte[] m_Data;

        /// <summary>
        /// The read head of the data array. Increments every time something is read.
        /// Resets to zero when the data array runs out of space for new data and existing
        /// unread data is moved to the beginning of the array.
        /// </summary>
        private int m_ReadHead;

        /// <summary>
        /// The amount of unread data in the array. Doesn't need to get adjusted when
        /// m_Offset resets to zero because when data is moved in the array, the length
        /// of unread data remains constant.
        /// </summary>
        private int m_UnpoppedDataLength;

        public bool IsEmpty => m_UnpoppedDataLength <= 0;

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

            m_ReadHead = 0;
            m_UnpoppedDataLength = reader.Length;
        }

        private unsafe void Check(FixedString64Bytes source)
        {
            fixed (byte* dataPtr = m_Data)
            {
                var reader = new DataStreamReader(dataPtr, m_Data.Length);

                var readerOffset = m_ReadHead;

                while (readerOffset < m_ReadHead + m_UnpoppedDataLength)
                {
                    reader.SeekSet(readerOffset);
                    var messageLength = reader.ReadInt();
                    if (m_UnpoppedDataLength - readerOffset < sizeof(int) + messageLength)
                    {
                        return;
                    }

                    var magic = reader.ReadInt();
                    if (magic != BatchHeader.MagicValue)
                    {
                        Debug.LogError($"Corruption found in BatchedReceiveQueue: Queue has been corrupted: {source}");
                        return;
                    }
                    readerOffset += sizeof(int) + messageLength;
                }
            }
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
            var available = m_Data.Length - (m_ReadHead + m_UnpoppedDataLength);
            if (available < reader.Length)
            {
                if (m_UnpoppedDataLength > 0)
                {
                    Array.Copy(m_Data, m_ReadHead, m_Data, 0, m_UnpoppedDataLength);
                }

                m_ReadHead = 0;
                Check("Move");

                while (m_Data.Length - m_UnpoppedDataLength < reader.Length)
                {
                    Array.Resize(ref m_Data, m_Data.Length * 2);
                    Check("Resize");
                }
            }

            unsafe
            {
                fixed (byte* dataPtr = m_Data)
                {
#if UTP_TRANSPORT_2_0_ABOVE
                    reader.ReadBytesUnsafe(dataPtr + m_ReadHead + m_UnpoppedDataLength, reader.Length);
#else
                    reader.ReadBytes(dataPtr + m_ReadHead + m_UnpoppedDataLength, reader.Length);
#endif
                }
            }

            m_UnpoppedDataLength += reader.Length;
            Check("PushReader");
        }

        /// <summary>Pop the next full message in the queue.</summary>
        /// <returns>The message, or the default value if no more full messages.</returns>
        public ArraySegment<byte> PopMessage()
        {
            if (m_UnpoppedDataLength < sizeof(int))
            {
                return default;
            }

            var messageLength = BitConverter.ToInt32(m_Data, m_ReadHead);

            if (m_UnpoppedDataLength - sizeof(int) < messageLength)
            {
                return default;
            }

            var data = new ArraySegment<byte>(m_Data, m_ReadHead + sizeof(int), messageLength);

            m_ReadHead += sizeof(int) + messageLength;
            m_UnpoppedDataLength -= sizeof(int) + messageLength;

            return data;
        }
    }
}
