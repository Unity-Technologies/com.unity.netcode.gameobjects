#if SCENE_MANAGEMENT_SCENE_HANDLE_NO_INT_CONVERSION
using System;
#endif
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    /// <summary>
    /// The scene map type <see cref="SceneMap"/>.
    /// </summary>
    public enum MapTypes
    {
        /// <summary>
        /// Denotes the server to client scene map type.
        /// </summary>
        ServerToClient,
        /// <summary>
        /// Denotes the client to server scene map type.
        /// </summary>
        ClientToServer
    }

    /// <summary>
    /// Provides the status of a loaded scene
    /// </summary>
    public struct SceneMap : INetworkSerializable
    {
        /// <summary>
        /// The scene mapping type <see cref="MapTypes"/>.
        /// </summary>
        public MapTypes MapType;
        /// <summary>
        /// The <see cref="UnityEngine.SceneManagement.Scene"/> struct of the scene mapped.
        /// </summary>
        public Scene Scene;
        /// <summary>
        /// When true, the scene is present.
        /// </summary>
        public bool ScenePresent;
        /// <summary>
        /// The name of the scene
        /// </summary>
        public string SceneName;

#if SCENE_MANAGEMENT_SCENE_HANDLE_NO_INT_CONVERSION
        /// <summary>
        /// The scene's server handle (a.k.a network scene handle)
        /// </summary>
        /// <remarks>
        /// This is deprecated in favor of ServerSceneHandle
        /// </remarks>
        [Obsolete("Int representation of a SceneHandle is deprecated, please use SceneHandle instead. (UnityUpgradable) -> ServerSceneHandle")]
#else
        /// <summary>
        /// The scene's server handle (a.k.a network scene handle)
        /// </summary>
#endif
        public int ServerHandle;

#if SCENE_MANAGEMENT_SCENE_HANDLE_NO_INT_CONVERSION
        /// <summary>
        /// The mapped handled. This could be the ServerHandle or LocalHandle depending upon context (client or server).
        /// </summary>
        /// <remarks>
        /// This is deprecated in favor of MappedLocalSceneHandle
        /// </remarks>
        [Obsolete("Int representation of a SceneHandle is deprecated, please use SceneHandle instead. (UnityUpgradable) -> MappedLocalSceneHandle")]
#else
        /// <summary>
        /// The mapped handled. This could be the ServerHandle or LocalHandle depending upon context (client or server).
        /// </summary>
#endif
        public int MappedLocalHandle;

#if SCENE_MANAGEMENT_SCENE_HANDLE_NO_INT_CONVERSION
        /// <summary>
        /// The local handle of the scene.
        /// </summary>
        /// <remarks>
        /// This is deprecated in favor of LocalSceneHandle
        /// </remarks>
        [Obsolete("Int representation of a SceneHandle is deprecated, please use SceneHandle instead. (UnityUpgradable) -> LocalSceneHandle")]
#else
        /// <summary>
        /// The local handle of the scene.
        /// </summary>
#endif
        public int LocalHandle;

#if SCENE_MANAGEMENT_SCENE_HANDLE_AVAILABLE
        /// <summary>
        /// The scene's server handle (a.k.a network scene handle)
        /// </summary>
        public SceneHandle ServerSceneHandle;
        /// <summary>
        /// The mapped handled. This could be the ServerSceneHandle or LocalSceneHandle depending upon context (client or server).
        /// </summary>
        public SceneHandle MappedLocalSceneHandle;
        /// <summary>
        /// The local handle of the scene.
        /// </summary>
        public SceneHandle LocalSceneHandle;
#endif

        private NetworkSceneHandle m_ServerHandle;
        private NetworkSceneHandle m_MappedLocalHandle;
        private NetworkSceneHandle m_LocalHandle;

        internal SceneMap(MapTypes mapType, Scene scene, bool isScenePresent, NetworkSceneHandle serverHandle, NetworkSceneHandle mappedLocalHandle)
        {
            MapType = mapType;
            Scene = scene;
            ScenePresent = isScenePresent;
            SceneName = isScenePresent ? scene.name : "Not Present";

            m_ServerHandle = serverHandle;
            m_MappedLocalHandle = mappedLocalHandle;
            m_LocalHandle = new NetworkSceneHandle(scene.handle);

#if SCENE_MANAGEMENT_SCENE_HANDLE_AVAILABLE
            ServerSceneHandle = serverHandle;
            MappedLocalSceneHandle = mappedLocalHandle;
            LocalSceneHandle = scene.handle;
#endif


#pragma warning disable CS0618 // Type or member is obsolete
#if SCENE_MANAGEMENT_SCENE_HANDLE_MUST_USE_ULONG
            ServerHandle = (int)Server.GetRawData();
            MappedLocalHandle = (int)MappedLocal.GetRawData();
            LocalHandle = (int)Local.GetRawData();
#else
            ServerHandle = m_ServerHandle.GetRawData();
            MappedLocalHandle = m_MappedLocalHandle.GetRawData();
            LocalHandle = m_LocalHandle.GetRawData();
#endif
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <inheritdoc cref="INetworkSerializable.NetworkSerialize{T}(BufferSerializer{T})"/>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref MapType);
            serializer.SerializeValue(ref ScenePresent);
            if (serializer.IsReader)
            {
                SceneName = "Not Present";
            }
            if (ScenePresent)
            {
                serializer.SerializeValue(ref SceneName);
#pragma warning disable CS0618 // Type or member is obsolete
                serializer.SerializeValue(ref LocalHandle);
            }
            serializer.SerializeValue(ref ServerHandle);
            serializer.SerializeValue(ref MappedLocalHandle);
#pragma warning restore CS0618 // Type or member is obsolete


#if SCENE_MANAGEMENT_SCENE_HANDLE_AVAILABLE
            // Ensure the SceneHandles are valid to be serialized
            if (serializer.IsWriter)
            {
                if (m_LocalHandle.IsEmpty() && LocalSceneHandle != SceneHandle.None)
                {
                    m_LocalHandle = LocalSceneHandle;
                }
                if (m_ServerHandle.IsEmpty() && ServerSceneHandle != SceneHandle.None)
                {
                    m_ServerHandle = ServerSceneHandle;
                }
                if (m_MappedLocalHandle.IsEmpty() && MappedLocalSceneHandle != SceneHandle.None)
                {
                    m_MappedLocalHandle = MappedLocalSceneHandle;
                }
            }

            // Serialize the INetworkSerializable representations
            serializer.SerializeValue(ref m_LocalHandle);
            serializer.SerializeValue(ref m_ServerHandle);
            serializer.SerializeValue(ref m_MappedLocalHandle);

            // If we're reading, convert back into the raw SceneHandle
            if (serializer.IsReader)
            {
                ServerSceneHandle = m_ServerHandle;
                ServerSceneHandle = m_LocalHandle;
                ServerSceneHandle = m_MappedLocalHandle;
            }
#endif
        }
    }
}
