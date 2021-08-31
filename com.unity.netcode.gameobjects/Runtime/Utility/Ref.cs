using System.Runtime.CompilerServices;

namespace Unity.Netcode
{
    public struct Ref<T> where T : unmanaged
    {
        private unsafe T* m_Value;

        public unsafe Ref(ref T value)
        {
            fixed (T* ptr = &value)
            {
                m_Value = ptr;
            }
        }

        public unsafe Ref(T* ptr)
        {
            m_Value = ptr;
        }

        public unsafe bool IsSet => m_Value != null;

        public unsafe ref T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref *m_Value;
        }

        public unsafe T* Ptr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Value;
        }
    }
}
