
namespace Unity.Netcode.Components
{
    /// <summary>
    /// A templated 4 dimensional vector implementation.
    /// </summary>
    public struct Vector4T<T> where T : unmanaged
    {
        public T X;
        public T Y;
        public T Z;
        public T W;

        public int Length => 4;

        public unsafe T this[int index]
        {
            get
            {
                fixed (Vector4T<T>* ptr = &this)
                {
                    return ((T*)ptr)[index];
                }
            }
            set
            {
                fixed (Vector4T<T>* ptr = &this)
                {
                    ((T*)ptr)[index] = value;
                }
            }
        }
    }
}
