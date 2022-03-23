using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport;

namespace Unity.Netcode.Transports.UTP
{
    /// <summary>Queue for batched messages meant to be sent through UTP.</summary>
    /// <remarks>
    /// Messages should be pushed on the queue with <see cref="PushMessage"/>. To send batched
    /// messages, call <see cref="FillWriter"> with the <see cref="DataStreamWriter"/> obtained from
    /// <see cref="NetworkDriver.BeginSend"/>. This will fill the writer with as many messages as
    /// possible. If the send is successful, call <see cref="Consume"/> to remove the data from the
    /// queue.
    ///
    /// This is meant as a companion to <see cref="BatchedReceiveQueue"/>, which should be used to
    /// read messages sent with this queue.
    /// </remarks>
    internal struct BatchedSendQueue : IDisposable
    {
        private NativeArray<byte> m_Data;
        private NativeArray<int> m_HeadTailIndices;

        /// <summary>Overhead that is added to each message in the queue.</summary>
        public const int PerMessageOverhead = sizeof(int);

        // Indices into m_HeadTailIndicies.
        private const int k_HeadInternalIndex = 0;
        private const int k_TailInternalIndex = 1;

        /// <summary>Index of the first byte of the oldest data in the queue.</summary>
        private int HeadIndex
        {
            get { return m_HeadTailIndices[k_HeadInternalIndex]; }
            set { m_HeadTailIndices[k_HeadInternalIndex] = value; }
        }

        /// <summary>Index one past the last byte of the most recent data in the queue.</summary>
        private int TailIndex
        {
            get { return m_HeadTailIndices[k_TailInternalIndex]; }
            set { m_HeadTailIndices[k_TailInternalIndex] = value; }
        }

        public int Length => TailIndex - HeadIndex;

        public bool IsEmpty => HeadIndex == TailIndex;

        public bool IsCreated => m_Data.IsCreated;

        /// <summary>Construct a new empty send queue.</summary>
        /// <param name="capacity">Maximum capacity of the send queue.</param>
        public BatchedSendQueue(int capacity)
        {
            m_Data = new NativeArray<byte>(capacity, Allocator.Persistent);
            m_HeadTailIndices = new NativeArray<int>(2, Allocator.Persistent);

            HeadIndex = 0;
            TailIndex = 0;
        }

        public void Dispose()
        {
            if (IsCreated)
            {
                m_Data.Dispose();
                m_HeadTailIndices.Dispose();
            }
        }

        /// <summary>Append data at the tail of the queue. No safety checks.</summary>
        private void AppendDataAtTail(ArraySegment<byte> data)
        {
            unsafe
            {
                var writer = new DataStreamWriter((byte*)m_Data.GetUnsafePtr() + TailIndex, m_Data.Length - TailIndex);

                writer.WriteInt(data.Count);

                fixed (byte* dataPtr = data.Array)
                {
                    writer.WriteBytes(dataPtr + data.Offset, data.Count);
                }
            }

            TailIndex += sizeof(int) + data.Count;
        }

        /// <summary>Append a new message to the queue.</summary>
        /// <param name="message">Message to append to the queue.</param>
        /// <returns>
        /// Whether the message was appended successfully. The only way it can fail is if there's
        /// no more room in the queue. On failure, nothing is written to the queue.
        /// </returns>
        public bool PushMessage(ArraySegment<byte> message)
        {
            if (!IsCreated)
            {
                return false;
            }

            // Check if there's enough room after the current tail index.
            if (m_Data.Length - TailIndex >= sizeof(int) + message.Count)
            {
                AppendDataAtTail(message);
                return true;
            }

            // Check if there would be enough room if we moved data at the beginning of m_Data.
            if (m_Data.Length - TailIndex + HeadIndex >= sizeof(int) + message.Count)
            {
                // Move the data back at the beginning of m_Data.
                unsafe
                {
                    UnsafeUtility.MemMove(m_Data.GetUnsafePtr(), (byte*)m_Data.GetUnsafePtr() + HeadIndex, Length);
                }

                TailIndex = Length;
                HeadIndex = 0;

                AppendDataAtTail(message);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Fill as much of a <see cref="DataStreamWriter"/> as possible with data from the head of
        /// the queue. Only full messages (and their length) are written to the writer.
        /// </summary>
        /// <remarks>
        /// This does NOT actually consume anything from the queue. That is, calling this method
        /// does not reduce the length of the queue. Callers are expected to call
        /// <see cref="Consume"/> with the value returned by this method afterwards if the data can
        /// be safely removed from the queue (e.g. if it was sent successfully).
        ///
        /// This method should not be used together with <see cref="FillWriterWithBytes"> since this
        /// could lead to a corrupted queue.
        /// </remarks>
        /// <param name="writer">The <see cref="DataStreamWriter"/> to write to.</param>
        /// <returns>How many bytes were written to the writer.</returns>
        public int FillWriterWithMessages(ref DataStreamWriter writer)
        {
            if (!IsCreated || Length == 0)
            {
                return 0;
            }

            unsafe
            {
                var reader = new DataStreamReader((byte*)m_Data.GetUnsafePtr() + HeadIndex, Length);

                var writerAvailable = writer.Capacity;
                var readerOffset = 0;

                while (readerOffset < Length)
                {
                    reader.SeekSet(readerOffset);
                    var messageLength = reader.ReadInt();

                    if (writerAvailable < sizeof(int) + messageLength)
                    {
                        break;
                    }
                    else
                    {
                        writer.WriteInt(messageLength);

                        var messageOffset = HeadIndex + reader.GetBytesRead();
                        writer.WriteBytes((byte*)m_Data.GetUnsafePtr() + messageOffset, messageLength);

                        writerAvailable -= sizeof(int) + messageLength;
                        readerOffset += sizeof(int) + messageLength;
                    }
                }

                return writer.Capacity - writerAvailable;
            }
        }

        /// <summary>
        /// Fill the given <see cref="DataStreamWriter"/> with as many bytes from the queue as
        /// possible, disregarding message boundaries.
        /// </summary>
        ///<remarks>
        /// This does NOT actually consume anything from the queue. That is, calling this method
        /// does not reduce the length of the queue. Callers are expected to call
        /// <see cref="Consume"/> with the value returned by this method afterwards if the data can
        /// be safely removed from the queue (e.g. if it was sent successfully).
        ///
        /// This method should not be used together with <see cref="FillWriterWithMessages"/> since
        /// this could lead to reading messages from a corrupted queue.
        /// </remarks>
        /// <param name="writer">The <see cref="DataStreamWriter"/> to write to.</param>
        /// <returns>How many bytes were written to the writer.</returns>
        public int FillWriterWithBytes(ref DataStreamWriter writer)
        {
            if (!IsCreated || Length == 0)
            {
                return 0;
            }

            var copyLength = Math.Min(writer.Capacity, Length);

            unsafe
            {
                writer.WriteBytes((byte*)m_Data.GetUnsafePtr() + HeadIndex, copyLength);
            }

            return copyLength;
        }

        /// <summary>Consume a number of bytes from the head of the queue.</summary>
        /// <remarks>
        /// This should only be called with a size that matches the last value returned by
        /// <see cref="FillWriter"/>. Anything else will result in a corrupted queue.
        /// </remarks>
        /// <param name="size">Number of bytes to consume from the queue.</param>
        public void Consume(int size)
        {
            if (size >= Length)
            {
                HeadIndex = 0;
                TailIndex = 0;
            }
            else
            {
                HeadIndex += size;
            }
        }
    }
}
