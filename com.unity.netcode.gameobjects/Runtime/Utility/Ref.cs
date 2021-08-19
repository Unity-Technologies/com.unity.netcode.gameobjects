using System.Runtime.CompilerServices;

namespace Unity.Multiplayer.Netcode
{
    public ref struct Ref<T> where T: unmanaged
    {
        private unsafe T* m_Value;

        public unsafe Ref(ref T value)
        {
            fixed (T* ptr = &value)
            {
                m_Value = ptr;
            }
        }

        public unsafe ref T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref *m_Value;
        }
    }
}