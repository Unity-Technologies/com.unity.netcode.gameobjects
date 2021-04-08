using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;

namespace Unity.MLPI.UTP
{
    [BurstCompatible]
    internal unsafe struct UnsafePayloadBuffer : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        private IntPtr      m_dataPtr;

        readonly int        m_bufferSizeInBytes;
        UnsafeBitArray      m_freeList;

        public UnsafePayloadBuffer(int capacity, int bufferSizeInBytes)
        {
            m_bufferSizeInBytes = bufferSizeInBytes;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be >= 0");

            if (capacity > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(capacity), $"Capacity * sizeof(T) cannot exceed {int.MaxValue} bytes");
#endif
            var defaultPageSize = VirtualMemoryUtility.DefaultPageSizeInBytes;

            var pageCount = VirtualMemoryUtility.BytesToPageCount((uint)capacity, defaultPageSize);

            var errorState = default(BaselibErrorState);

            var pagedMemory = VirtualMemoryUtility.ReserveAddressSpace(pageCount, defaultPageSize, out errorState);
            VirtualMemoryUtility.CommitMemory(pagedMemory, out errorState);

            m_dataPtr = pagedMemory.ptr;

            CheckReserveAddressSpace(errorState.Success, capacity);

            BufferCapacity = (int)VirtualMemoryUtility.BytesToPageCount((uint)capacity, (uint)bufferSizeInBytes);
            m_freeList = new UnsafeBitArray(BufferCapacity, Allocator.Persistent);

        }

        public int Allocate()
        {
            int index = m_freeList.Find(0, BufferCapacity, 1);
            if (index >= BufferCapacity)
            {
                ThrowAllocatorEmpty(index);
                return -1;
            }

            //BaselibErrorState errorState;
            //var range = new VMRange((IntPtr)this[index], (uint)m_bufferSizeInBytes, VirtualMemoryUtility.DefaultPageSizeInBytes);
            //VirtualMemoryUtility.CommitMemory(range, out errorState);

            //if (!errorState.Success)
            //{
            //    ThrowCommitMemoryFailed(errorState, index);
            //    return -1;
            //}

            m_freeList.Set(index, true);

            return index;
        }

        public void Free(int index)
        {
            CheckValidIndexToFree(index);
            //BaselibErrorState errorState;
            //var range = new VMRange((IntPtr)this[index], (uint)m_bufferSizeInBytes, VirtualMemoryUtility.DefaultPageSizeInBytes);
            //VirtualMemoryUtility.DecommitMemory(range, out errorState);
            //CheckDecommitMemory(errorState.Success, index);
            m_freeList.Set(index, false);
        }

        public void* this[int index] => (void*)(m_dataPtr + (m_bufferSizeInBytes * index));

        public void Dispose()
        {
            m_freeList.Dispose();

            var errorState = default(BaselibErrorState);
            var range = new VMRange((IntPtr)m_dataPtr, (uint)m_bufferSizeInBytes, VirtualMemoryUtility.DefaultPageSizeInBytes);
            VirtualMemoryUtility.FreeAddressSpace(range, out errorState);
            CheckFreeAddressSpace(errorState.Success);
        }

        public int BufferCapacity { get; }

        public bool IsEmpty => m_freeList.CountBits(0, m_freeList.Length) >= BufferCapacity;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ThrowAllocatorEmpty(int allocatedIndex)
        {
            throw new InvalidOperationException("Cannot allocate, allocator is exhausted.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ThrowCommitMemoryFailed(BaselibErrorState commitErrorState, int index)
        {
            VirtualMemoryUtility.ReportWrappedBaselibError(commitErrorState);
            throw new InvalidOperationException($"Failed to commit address range for index {index}.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckReserveAddressSpace(bool reserveSuccess, int budgetInBytes)
        {
            if (!reserveSuccess)
            {
                throw new InvalidOperationException($"Failed to reserve address range for {budgetInBytes} bytes");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckFreeAddressSpace(bool freeAddressSpaceSuccess)
        {
            if (!freeAddressSpaceSuccess)
            {
                throw new InvalidOperationException($"Failed to free the reserved address range.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckValidIndexToFree(int index)
        {
            if (index < 0 || index >= BufferCapacity)
            {
                throw new ArgumentException($"Cannot free index {index}, it is outside the expected range [0, {BufferCapacity}).");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckDecommitMemory(bool decommitSuccess, int index)
        {
            if (!decommitSuccess)
            {
                throw new InvalidOperationException($"Failed to decommit address range for index {index}.");
            }
        }
    }
}
