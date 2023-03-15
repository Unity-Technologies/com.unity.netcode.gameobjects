
namespace Unity.Netcode.Components
{
    /// <summary>
    /// A templated 3 dimensional vector implementation.
    /// </summary>
    public struct Vector3T<T> where T : unmanaged
    {
        public T X;
        public T Y;
        public T Z;

        public int Length => 3;

        public unsafe T this[int index]
        {
            get
            {
                fixed (Vector3T<T>* ptr = &this)
                {
                    return ((T*)ptr)[index];
                }
            }
            set
            {
                fixed (Vector3T<T>* ptr = &this)
                {
                    ((T*)ptr)[index] = value;
                }
            }
        }
    }
}
