using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.MLPI.UTP
{
    internal unsafe struct UnsafeAtomicFreeList : IDisposable
    {
        // used count
        // free list size
        // free indices...
        [NativeDisableUnsafePtrRestriction]
        private int* m_Buffer;
        private int m_Length;
        private Allocator m_Allocator;

        public int Capacity => m_Length;
        public int InUse => m_Buffer[0] - m_Buffer[1];

        public bool IsCreated => m_Buffer != null;

        /// <summary>
        /// Initializes a new instance of the AtomicFreeList struct.
        /// </summary>
        /// <param name="capacity">The number of elements the free list can store.</param>
        /// <param name="allocator">The <see cref="Allocator"/> used to allocate the memory.</param>
        public UnsafeAtomicFreeList(int capacity, Allocator allocator)
        {
            m_Allocator = allocator;
            m_Length = capacity;
            var size = UnsafeUtility.SizeOf<int>() * (capacity + 2);
            m_Buffer = (int*)UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<int>(), allocator);
            UnsafeUtility.MemClear(m_Buffer, size);
        }

        public void Dispose()
        {
            if (IsCreated)
                UnsafeUtility.Free(m_Buffer, m_Allocator);
        }

        /// <summary>
        /// Inserts an item on top of the stack.
        /// </summary>
        /// <param name="item">The item to push onto the stack.</param>
        public unsafe void Push(int item)
        {
            int* buffer = m_Buffer;
            int idx = Interlocked.Increment(ref buffer[1]) - 1;
            while (Interlocked.CompareExchange(ref buffer[idx + 2], item + 1, 0) != 0)
            {
            }
        }

        /// <summary>
        /// Remove and return a value from the top of the stack
        /// </summary>
        /// <remarks>
        /// <value>The removed value from the top of the stack.</value>
        public unsafe int Pop()
        {
            int* buffer = m_Buffer;
            int idx = buffer[1] - 1;
            while (idx >= 0 && Interlocked.CompareExchange(ref buffer[1], idx, idx + 1) != idx + 1)
                idx = buffer[1] - 1;

            if (idx >= 0)
            {
                int val = 0;
                while (val == 0)
                {
                    val = Interlocked.Exchange(ref buffer[2 + idx], 0);
                }

                return val - 1;
            }

            idx = Interlocked.Increment(ref buffer[0]) - 1;
            if (idx >= Capacity)
            {
                Interlocked.Decrement(ref buffer[0]);
                return -1;
            }

            return idx;
        }
    }
}
