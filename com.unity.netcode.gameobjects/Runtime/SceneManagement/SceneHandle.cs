#if !SCENE_MANAGEMENT_SCENE_HANDLE_AVAILABLE
using System;
using System.Runtime.CompilerServices;

namespace Unity.Netcode
{
    internal struct SceneHandle : IEquatable<SceneHandle>
    {
        private int m_Handle;
        public static SceneHandle None => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(SceneHandle other)
        {
            return m_Handle == other.m_Handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is SceneHandle other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return m_Handle;
        }

        /// <summary>
        /// Test for equality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>True if the two SceneHandles are the same</returns>
        public static bool operator ==(SceneHandle left, SceneHandle right) => left.Equals(right);

        /// <summary>
        /// Test for inequality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>True if the two SceneHandles are different</returns>
        public static bool operator !=(SceneHandle left, SceneHandle right) => !left.Equals(right);

        /// <summary>
        /// Implicit conversion from <see langword="int"/> to <see cref="SceneHandle"/>.
        /// </summary>
        /// <param name="handle"></param>
        public static implicit operator SceneHandle(int handle) => new() { m_Handle = handle };

        /// <summary>
        /// Implicit conversion from <see cref="SceneHandle"/> to <see langword="int"/>.
        /// </summary>
        /// <param name="handle">The SceneHandle</param>
        public static implicit operator int(SceneHandle handle) => handle.m_Handle;
    }
}
#endif
