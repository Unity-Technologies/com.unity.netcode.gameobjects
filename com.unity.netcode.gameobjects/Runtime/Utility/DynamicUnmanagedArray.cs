using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Netcode
{
    public struct DynamicUnmanagedArray<T> : IReadOnlyList<T>, IDisposable where T : unmanaged
    {
        private unsafe T* m_Data;
        private Allocator m_Allocator;
        private int m_Length;
        private int m_Capacity;

        public int Count => m_Length;

        private int Capacity => m_Capacity;

        public bool IsReadOnly => false;

        public unsafe DynamicUnmanagedArray(int capacity, Allocator allocator = Allocator.Persistent)
        {
            m_Data = (T*)UnsafeUtility.Malloc(capacity * sizeof(T), UnsafeUtility.AlignOf<T>(), allocator);
            m_Allocator = allocator;
            m_Length = 0;
            m_Capacity = capacity;
        }

        public unsafe void Dispose()
        {
            UnsafeUtility.Free(m_Data, m_Allocator);
        }

        public unsafe T this[int index]
        {
            get => m_Data[index];
            set => m_Data[index] = value;
        }

        public unsafe ref T GetValueRef(int index)
        {
            return ref m_Data[index];
        }

        private unsafe void Resize()
        {
            m_Capacity *= 2;
            var data = (T*)UnsafeUtility.Malloc(m_Capacity * sizeof(T), UnsafeUtility.AlignOf<T>(), m_Allocator);
            UnsafeUtility.MemCpy(data, m_Data, m_Length);
            UnsafeUtility.Free(m_Data, m_Allocator);
            m_Data = data;
        }

        public unsafe void Add(T item)
        {
            if (m_Length == m_Capacity)
            {
                Resize();
            }
            m_Data[m_Length++] = item;
        }

        public unsafe T Pop()
        {
            return m_Data[--m_Length];
        }

        public void Clear()
        {
            m_Length = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static unsafe Ref<DynamicUnmanagedArray<T>> CreateRef(int capacity = 16)
        {
            DynamicUnmanagedArray<T>* array =
                (DynamicUnmanagedArray<T>*) UnsafeUtility.Malloc(
                    sizeof(DynamicUnmanagedArray<T>),
                    UnsafeUtility.AlignOf<DynamicUnmanagedArray<T>>(), Allocator.Persistent);
            
            array->m_Data = (T*)UnsafeUtility.Malloc(capacity * sizeof(T), UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
            array->m_Allocator = Allocator.Persistent;
            array->m_Length = 0;
            array->m_Capacity = capacity;
            return new Ref<DynamicUnmanagedArray<T>>(array);
        }

        public static unsafe void ReleaseRef(Ref<DynamicUnmanagedArray<T>> array)
        {
            array.Value.Dispose();
            UnsafeUtility.Free(array.Ptr, Allocator.Persistent);
        }
    }
}
