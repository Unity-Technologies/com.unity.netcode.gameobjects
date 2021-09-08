using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Netcode
{
    /// <summary>
    /// For each usage of FixedUnmanagedArray, a storage class needs to be created for it.
    /// Rather than providing a huge list of predefined storage classes, each use should just
    /// create their own at the correct size (in bytes). The size should be an even multiple
    /// of the value size being stored.
    ///
    /// Example:
    ///
    /// [StructLayout(LayoutKind.Explicit, Size = 256 * sizeof(int))]
    /// struct FixedStorageInt256 : IFixedArrayStorage
    /// {
    /// }
    /// </summary>
    public interface IFixedArrayStorage
    {
        
    }

    public struct FixedUnmanagedArray<TPropertyType, TStorageType> : IReadOnlyList<TPropertyType> 
        where TPropertyType : unmanaged
        where TStorageType : unmanaged, IFixedArrayStorage
    {
        private int m_Length;
        private TStorageType m_Data;

        public int Count
        {
            get { return m_Length; }
            set { m_Length = value;  }
        }

        public unsafe int Capacity => sizeof(TStorageType)/sizeof(TPropertyType);

        public bool IsReadOnly => false;

        public unsafe TPropertyType[] ToArray()
        {
            TPropertyType[] ret = new TPropertyType[Count];
            fixed (TPropertyType* b = ret)
            {
                fixed (TStorageType* ptr = &m_Data)
                {
                    UnsafeUtility.MemCpy(b, ptr, Count * sizeof(TPropertyType));
                }
            }
            return ret;
        }
        
        public unsafe FixedUnmanagedArray(TPropertyType* seedData, int size)
        {
            if (size > sizeof(TStorageType))
            {
                throw new OverflowException("Seed data was larger than provided storage class.");
            }
            
            m_Data = new TStorageType();
            fixed (TStorageType* ptr = &m_Data)
            {
                UnsafeUtility.MemCpy(ptr, seedData, size);
            }

            m_Length = size;
        }

        public unsafe FixedUnmanagedArray(TPropertyType[] seedData)
        {
            if (seedData.Length > sizeof(TStorageType))
            {
                throw new OverflowException("Seed data was larger than provided storage class.");
            }
            m_Data = new TStorageType();
            fixed (TStorageType* ptr = &m_Data)
            fixed (TPropertyType* seedPtr = seedData)
            {
                UnsafeUtility.MemCpy(ptr, seedPtr, seedData.Length);
            }

            m_Length = seedData.Length;
        }

        public unsafe FixedUnmanagedArray(TPropertyType[] seedData, int size)
        {
            if (size > sizeof(TStorageType))
            {
                throw new OverflowException("Seed data was larger than provided storage class.");
            }

            if (size > seedData.Length)
            {
                throw new ArgumentException("Size cannot be greater than seed data's length.");
            }
            
            m_Data = new TStorageType();
            fixed (TStorageType* ptr = &m_Data)
            fixed (TPropertyType* seedPtr = seedData)
            {
                UnsafeUtility.MemCpy(ptr, seedPtr, size);
            }

            m_Length = size;
        }

        public unsafe TPropertyType* GetArrayPtr()
        {
            fixed (TStorageType* ptr = &m_Data)
            {
                return (TPropertyType*) ptr;
            }
        }

        public unsafe TPropertyType this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                fixed (TStorageType* ptr = &m_Data)
                {
                    TPropertyType* reinterpretPtr = (TPropertyType*) ptr;
                    return reinterpretPtr[index];
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                fixed (TStorageType* ptr = &m_Data)
                {
                    TPropertyType* reinterpretPtr = (TPropertyType*) ptr;
                    reinterpretPtr[index] = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TPropertyType GetValueRef(int index)
        {
            fixed (TStorageType* ptr = &m_Data)
            {
                TPropertyType* reinterpretPtr = (TPropertyType*) ptr;
                return ref reinterpretPtr[index];
            }
        }

        public void Add(TPropertyType value)
        {
            if (m_Length == Capacity)
            {
                throw new OverflowException("The FixedUnmanagedArray is full.");
            }

            this[m_Length++] = value;
        }

        public TPropertyType Pop()
        {
            return this[--m_Length];
        }

        public void Clear()
        {
            m_Length = 0;
        }

        public IEnumerator<TPropertyType> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
