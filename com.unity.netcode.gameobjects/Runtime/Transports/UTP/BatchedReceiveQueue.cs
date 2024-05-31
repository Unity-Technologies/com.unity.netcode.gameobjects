using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport;
using UnityEngine;

namespace Unity.Netcode.Transports.UTP
{
    /// <summary>Queue for batched messages received through UTP.</summary>
    /// <remarks>This is meant as a companion to <see cref="BatchedSendQueue"/>.</remarks>
    internal class BatchedReceiveQueue
    {
        private byte[] m_Data;
        private int m_ReadHead;
        private int m_UnreadDataLength;

        public bool IsEmpty => m_UnreadDataLength <= 0;
        private int m_Capacity => m_Data.Length;
        private int m_UsedSpace => m_ReadHead + m_UnreadDataLength;
        private int m_AvailableSpace => m_Capacity - m_UsedSpace;

        private HashSet<ulong> m_receivedFragmentIds = new HashSet<ulong>();
        private ulong m_HighestReceivedFragmentId = 0;


        /// <summary>
        /// Construct a new receive queue from a <see cref="DataStreamReader"/> returned by
        /// <see cref="NetworkDriver"/> when popping a data event.
        /// </summary>
        /// <param name="reader">The <see cref="DataStreamReader"/> to construct from.</param>
        public BatchedReceiveQueue(DataStreamReader reader)
        {
            var fragmentId = reader.ReadULong();
            Debug.Log($"BRQ: Received fragment {fragmentId}: {DataStreamReaderToString(reader)}");
            m_receivedFragmentIds.Add(fragmentId);
            m_HighestReceivedFragmentId = fragmentId;

            m_Data = new byte[reader.Length - sizeof(ulong)];
            unsafe
            {
                fixed (byte* dataPtr = m_Data)
                {
#if UTP_TRANSPORT_2_0_ABOVE
                    reader.ReadBytesUnsafe(dataPtr, reader.Length - sizeof(ulong));
#else
                    reader.ReadBytes(dataPtr, reader.Length - sizeof(ulong));
#endif
                }
            }

            m_ReadHead = 0;
            m_UnreadDataLength = reader.Length - sizeof(ulong);
        }

        internal unsafe static string DataStreamReaderToString(DataStreamReader reader)
        {
            var array = new NativeArray<byte>(reader.Length, Allocator.Temp);
            reader.SeekSet(0);
            reader.ReadBytes(array);
            reader.SeekSet(0);
            byte* bytes = (byte*)array.GetUnsafePtr();
            var hex = new StringBuilder(reader.Length * 3);
            for (int i = 0; i < reader.Length; ++i)
            {
                hex.AppendFormat("{0:x2} ", bytes[i]);
            }

            return hex.ToString();
        }

        /// <summary>
        /// Push the entire data from a <see cref="DataStreamReader"/> (as returned by popping an
        /// event from a <see cref="NetworkDriver">) to the queue.
        /// </summary>
        /// <param name="reader">The <see cref="DataStreamReader"/> to push the data of.</param>
        public void PushReader(DataStreamReader reader)
        {
            var fragmentId = reader.ReadULong();
            Debug.Log($"BRQ: Received fragment {fragmentId}: {DataStreamReaderToString(reader)}");
            if (m_receivedFragmentIds.Contains(fragmentId))
            {
                Debug.LogError($"Received duplicate packet fragment {fragmentId}, ignoring");
                return;
            }

            if (m_HighestReceivedFragmentId > 200 && fragmentId < m_HighestReceivedFragmentId - 200)
            {
                Debug.LogError($"Received fragment {fragmentId} more than 200 fragments out of date from {m_HighestReceivedFragmentId}, ignoring");
            }

            m_HighestReceivedFragmentId = Math.Max(m_HighestReceivedFragmentId, fragmentId);

            m_receivedFragmentIds.Add(fragmentId);
            //Debug.Log("PushReader");
            // Resize the array and copy the existing data to the beginning if there's not enough
            // room to copy the reader's data at the end of the existing data.
            if (m_AvailableSpace < reader.Length)
            {
                //Debug.Log("available < reader.Length");
                if (m_UnreadDataLength > 0)
                {
                    //Debug.Log("m_Length > 0");
                    Array.Copy(m_Data, m_ReadHead, m_Data, 0, m_UnreadDataLength);
                }

                m_ReadHead = 0;

                while (m_AvailableSpace < reader.Length)
                {
                    //Debug.Log("m_Data.Length - m_Length < reader.Length");
                    Array.Resize(ref m_Data, m_Data.Length * 2);
                }
            }

            unsafe
            {
                fixed (byte* dataPtr = m_Data)
                {
#if UTP_TRANSPORT_2_0_ABOVE
                    reader.ReadBytesUnsafe(dataPtr + m_ReadHead + m_UnreadDataLength, reader.Length - sizeof(ulong));
#else
                    reader.ReadBytes(dataPtr + m_ReadHead + m_UnreadDataLength, reader.Length - sizeof(ulong));
#endif
                }
            }

            m_UnreadDataLength += reader.Length - sizeof(ulong);
        }

        /// <summary>Pop the next full message in the queue.</summary>
        /// <returns>The message, or the default value if no more full messages.</returns>
        public ArraySegment<byte> PopMessage()
        {
            //Debug.Log("PopMessage");
            if (m_UnreadDataLength < sizeof(int))
            {
                //Debug.Log("Too small");
                return default;
            }

            var messageLength = BitConverter.ToInt32(m_Data, m_ReadHead);

            if (m_UnreadDataLength - sizeof(int) < messageLength)
            {
                //Debug.Log("Too small");
                return default;
            }

            var data = new ArraySegment<byte>(m_Data, m_ReadHead + sizeof(int), messageLength);

            m_ReadHead += sizeof(int) + messageLength;
            m_UnreadDataLength -= sizeof(int) + messageLength;

            return data;
        }
    }
}
