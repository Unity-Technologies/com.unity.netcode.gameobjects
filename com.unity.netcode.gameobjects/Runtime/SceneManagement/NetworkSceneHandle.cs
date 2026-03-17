using System;
using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    /// <summary>
    /// An internal to NGO representation of the Scene <see cref="Scene.handle"/>.
    /// </summary>
    //
    // The underlying representation of the scene handle is changing in 6.3
    // This allows us to wrap the change and hide it from the rest of the package.
    internal struct NetworkSceneHandle : IEquatable<NetworkSceneHandle>, INetworkSerializable
    {
#if SCENE_MANAGEMENT_SCENE_HANDLE_AVAILABLE
        private SceneHandle m_Handle;
#else
        private int m_Handle;
#endif

        /// <summary>
        /// Whether this <see cref="NetworkSceneHandle"/> represents an uninitialized scene handle
        /// </summary>
        /// <returns>True if this scene handle represents the <see langword="default"/>; otherwise false.</returns>
        public bool IsEmpty() => Equals(default(NetworkSceneHandle));

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsWriter)
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(GetRawData());
            }
            else
            {
                var reader = serializer.GetFastBufferReader();
                // DANGO-TODO Rust needs to be updated to either handle this ulong or to remove the scene store.
#if SCENE_MANAGEMENT_SCENE_HANDLE_MUST_USE_ULONG
                reader.ReadValueSafe(out ulong rawData);
                m_Handle = SceneHandle.FromRawData(rawData);
#elif SCENE_MANAGEMENT_SCENE_HANDLE_NO_INT_CONVERSION
                reader.ReadValueSafe(out int rawData);
                m_Handle = SceneHandle.FromRawData((ulong)rawData);
#else
                reader.ReadValueSafe(out int rawData);
                m_Handle = rawData;
#endif
            }
        }

#if SCENE_MANAGEMENT_SCENE_HANDLE_AVAILABLE
        internal NetworkSceneHandle(SceneHandle handle)
        {
            m_Handle = handle;
        }

#else
        internal NetworkSceneHandle(int handle)
        {
            m_Handle = handle;
        }
#endif

        /// <summary>
        /// A separate constructor for using during tests.
        /// This is required as the tests need to be able to create NetworkSceneHandles that represent only mock data.
        /// </summary>
        /// <param name="handle">The number to use as the underlying sceneHandle representation</param>
        /// <param name="asMock">Empty parameter that ensures the tests use the mock constructor.</param>
#if SCENE_MANAGEMENT_SCENE_HANDLE_NO_INT_CONVERSION
        internal NetworkSceneHandle(ulong handle, bool asMock)
        {
            m_Handle = SceneHandle.FromRawData(handle);
        }
#else
        internal NetworkSceneHandle(int handle, bool asMock)
        {
            m_Handle = handle;
        }
#endif

#if SCENE_MANAGEMENT_SCENE_HANDLE_MUST_USE_ULONG
        public ulong GetRawData() => m_Handle.GetRawData();
#elif SCENE_MANAGEMENT_SCENE_HANDLE_NO_INT_CONVERSION
        public int GetRawData() => (int)m_Handle.GetRawData();
#else
        public int GetRawData() => m_Handle;
#endif

        #region Implicit conversions
#if SCENE_MANAGEMENT_SCENE_HANDLE_AVAILABLE
        /// <summary>
        /// Implicit conversion from <see cref="SceneHandle"/> to <see cref="NetworkSceneHandle"/>.
        /// </summary>
        /// <param name="handle">The SceneHandle to covert</param>
        public static implicit operator NetworkSceneHandle(SceneHandle handle) => new(handle);

        /// <summary>
        /// Implicit conversion from <see cref="NetworkSceneHandle"/> to <see cref="SceneHandle"/>.
        /// </summary>
        /// <param name="handle">The NetworkSceneHandle to convert</param>
        public static implicit operator SceneHandle(NetworkSceneHandle handle) => handle.m_Handle;
#else
        /// <summary>
        /// Implicit conversion from <see langword="int"/> to <see cref="NetworkSceneHandle"/>.
        /// </summary>
        /// <param name="handle"></param>
        public static implicit operator NetworkSceneHandle(int handle) => new(handle);
#endif
        #endregion

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(NetworkSceneHandle other) => m_Handle == other.m_Handle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is NetworkSceneHandle other && Equals(other);

#if SCENE_MANAGEMENT_SCENE_HANDLE_AVAILABLE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Equals(SceneHandle other) => m_Handle == other;
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Equals(int other) => m_Handle == other;
#endif

#if SCENE_MANAGEMENT_SCENE_HANDLE_AVAILABLE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => m_Handle.GetHashCode();
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => m_Handle;
#endif

        /// <summary>
        /// Test for equality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>True if the two SceneHandles are the same</returns>
        public static bool operator ==(NetworkSceneHandle left, NetworkSceneHandle right) => left.Equals(right);

        /// <summary>
        /// Test for inequality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>True if the two SceneHandles are different</returns>
        public static bool operator !=(NetworkSceneHandle left, NetworkSceneHandle right) => !left.Equals(right);


#if SCENE_MANAGEMENT_SCENE_HANDLE_AVAILABLE
        public static bool operator ==(SceneHandle left, NetworkSceneHandle right) => left.Equals(right.m_Handle);
        public static bool operator !=(SceneHandle left, NetworkSceneHandle right) => !left.Equals(right.m_Handle);

        public static bool operator ==(NetworkSceneHandle left, SceneHandle right) => left.Equals(right);
        public static bool operator !=(NetworkSceneHandle left, SceneHandle right) => !left.Equals(right);
#else
        public static bool operator ==(int left, NetworkSceneHandle right) => left.Equals(right.m_Handle);
        public static bool operator !=(int left, NetworkSceneHandle right) => !left.Equals(right.m_Handle);

        public static bool operator ==(NetworkSceneHandle left, int right) => left.Equals(right);
        public static bool operator !=(NetworkSceneHandle left, int right) => !left.Equals(right);
#endif
        #endregion
    }
}
