using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Unity.Multiplayer.Netcode
{
    public ref struct RefArray<T> where T : unmanaged
    {
        public struct RefArrayImplementation<T> : IReadOnlyList<T>
            where T : unmanaged
        {
            private unsafe T* m_Value;
            private int m_Length;

            internal unsafe RefArrayImplementation(T* ptr, int length)
            {
                m_Value = ptr;
                m_Length = length;
            }

            public unsafe ref T Value
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref *m_Value;
            }

            public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
            {
                private RefArrayImplementation<T> m_Array;
                private int m_Index;

                public Enumerator(ref RefArrayImplementation<T> array)
                {
                    m_Array = array;
                    m_Index = -1;
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    ++m_Index;
                    return m_Index < m_Array.Length;
                }

                public void Reset() => m_Index = -1;

                public T Current => m_Array[m_Index];

                object IEnumerator.Current => (object)Current;
            }

            public Enumerator GetEnumerator() =>
                new Enumerator(ref this);

            IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
                (IEnumerator<T>)new Enumerator(ref this);

            IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)GetEnumerator();

            public int Count => m_Length;
            public int Length => m_Length;

            public unsafe T this[int index]
            {
                get => m_Value[index];
                set => m_Value[index] = value;
            }
        }

        private RefArrayImplementation<T> m_Value;

        public unsafe RefArray(T* ptr, int length)
        {
            m_Value = new RefArrayImplementation<T>(ptr, length);
        }

        public unsafe ref RefArrayImplementation<T> Value
        {
            get
            {
                fixed (RefArrayImplementation<T>* ptr = &m_Value)
                {
                    return ref *ptr;
                }
            }
        }
    }
}
