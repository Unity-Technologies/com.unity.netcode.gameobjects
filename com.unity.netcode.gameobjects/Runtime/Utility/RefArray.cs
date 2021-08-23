using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Unity.Multiplayer.Netcode
{
    public ref struct RefArray<T> where T: unmanaged
    {
        public struct RefArrayImplementation<T> : IReadOnlyList<T>
            where T : unmanaged
        {
            internal unsafe T* m_Value;
            internal int m_Length;

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
                private RefArrayImplementation<T> m_array;
                private int m_Index;

                public Enumerator(ref RefArrayImplementation<T> array)
                {
                    this.m_array = array;
                    this.m_Index = -1;
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    ++this.m_Index;
                    return this.m_Index < this.m_array.Length;
                }

                public void Reset() => this.m_Index = -1;

                public T Current => this.m_array[this.m_Index];

                object IEnumerator.Current => (object) this.Current;
            }

            public RefArrayImplementation<T>.Enumerator GetEnumerator() =>
                new RefArrayImplementation<T>.Enumerator(ref this);

            IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
                (IEnumerator<T>) new RefArrayImplementation<T>.Enumerator(ref this);

            IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) this.GetEnumerator();

            public int Count => m_Length;
            public int Length => m_Length;

            public unsafe T this[int index]
            {
                get => m_Value[index];
                set => m_Value[index] = value;
            }
        }

        internal RefArrayImplementation<T> m_Value;
        
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