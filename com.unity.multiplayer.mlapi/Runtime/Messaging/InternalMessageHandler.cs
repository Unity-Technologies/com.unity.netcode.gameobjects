using System;
using System.IO;
using MLAPI.Connection;
using MLAPI.Logging;
using MLAPI.SceneManagement;
using MLAPI.Serialization.Pooled;
using MLAPI.Spawning;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using MLAPI.Configuration;
using MLAPI.Messaging.Buffering;
using MLAPI.Profiling;
using MLAPI.Serialization;
using Unity.Profiling;

namespace MLAPI.Messaging
{
    internal class InternalMessageHandler
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private static ProfilerMarker s_HandleConnectionRequest = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleConnectionRequest)}");
        private static ProfilerMarker s_HandleConnectionApproved = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleConnectionApproved)}");
        private static ProfilerMarker s_HandleAddObject = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleAddObject)}");
        private static ProfilerMarker s_HandleDestroyObject = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleDestroyObject)}");
        private static ProfilerMarker s_HandleSwitchScene = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleSwitchScene)}");
        private static ProfilerMarker s_HandleClientSwitchSceneCompleted = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleClientSwitchSceneCompleted)}");
        private static ProfilerMarker s_HandleChangeOwner = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleChangeOwner)}");
        private static ProfilerMarker s_HandleAddObjects = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleAddObjects)}");
        private static ProfilerMarker s_HandleDestroyObjects = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleDestroyObjects)}");
        private static ProfilerMarker s_HandleTimeSync = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleTimeSync)}");
        private static ProfilerMarker s_HandleNetworkVariableDelta = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleNetworkVariableDelta)}");
        private static ProfilerMarker s_HandleNetworkVariableUpdate = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleNetworkVariableUpdate)}");
        private static ProfilerMarker s_HandleUnnamedMessage = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleUnnamedMessage)}");
        private static ProfilerMarker s_HandleNamedMessage = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleNamedMessage)}");
        private static ProfilerMarker s_HandleNetworkLog = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleNetworkLog)}");
        private static ProfilerMarker s_RpcReceiveQueueItemServerRpc = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(RpcReceiveQueueItem)}.{nameof(RpcQueueContainer.QueueItemType.ServerRpc)}");
        private static ProfilerMarker s_RpcReceiveQueueItemClientRpc = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(RpcReceiveQueueItem)}.{nameof(RpcQueueContainer.QueueItemType.ClientRpc)}");
#endif

        private NetworkManager m_NetworkManager;

        internal InternalMessageHandler(NetworkManager manager) { m_NetworkManager = manager; }

        internal void HandleConnectionRequest(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleConnectionRequest.Begin();
#endif
            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                ulong configHash = reader.ReadUInt64Packed();
                if (!m_NetworkManager.NetworkConfig.CompareConfig(configHash))
                {
                    if (m_NetworkManager.LogLevel <= LogLevel.Normal)
                    {
                        m_NetworkManager.NetworkLog.LogWarning($"{nameof(NetworkConfig)} mismatch. The configuration between the server and client does not match");
                    }

                    m_NetworkManager.DisconnectClient(clientId);
                    return;
                }

                if (m_NetworkManager.NetworkConfig.ConnectionApproval)
                {
                    byte[] connectionBuffer = reader.ReadByteArray();
                    m_NetworkManager.InvokeConnectionApproval(connectionBuffer, clientId, (createPlayerObject, playerPrefabHash, approved, position, rotation) => { m_NetworkManager.HandleApproval(clientId, createPlayerObject, playerPrefabHash, approved, position, rotation); });
                }
                else
                {
                    m_NetworkManager.HandleApproval(clientId, m_NetworkManager.NetworkConfig.CreatePlayerPrefab, null, true, null, null);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleConnectionRequest.End();
#endif
        }

        internal void HandleConnectionApproved(ulong clientId, Stream stream, float receiveTime)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleConnectionApproved.Begin();
#endif
            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                m_NetworkManager.LocalClientId = reader.ReadUInt64Packed();

                uint sceneIndex = 0;
                Guid sceneSwitchProgressGuid = new Guid();

                if (m_NetworkManager.NetworkConfig.EnableSceneManagement)
                {
                    sceneIndex = reader.ReadUInt32Packed();
                    sceneSwitchProgressGuid = new Guid(reader.ReadByteArray());
                }

                bool sceneSwitch = m_NetworkManager.NetworkConfig.EnableSceneManagement && m_NetworkManager.NetworkSceneManager.HasSceneMismatch(sceneIndex);

                float netTime = reader.ReadSinglePacked();
                m_NetworkManager.UpdateNetworkTime(clientId, netTime, receiveTime, true);

                m_NetworkManager.ConnectedClients.Add(m_NetworkManager.LocalClientId, new NetworkClient { ClientId = m_NetworkManager.LocalClientId });


                void DelayedSpawnAction(Stream continuationStream)
                {
                    using (var continuationReader = m_NetworkManager.NetworkReaderPool.GetReader(continuationStream))
                    {
                        if (!m_NetworkManager.NetworkConfig.EnableSceneManagement || m_NetworkManager.NetworkConfig.UsePrefabSync)
                        {
                            m_NetworkManager.NetworkSpawnManager.DestroySceneObjects();
                        }
                        else
                        {
                            m_NetworkManager.NetworkSpawnManager.ClientCollectSoftSyncSceneObjectSweep(null);
                        }

                        uint objectCount = continuationReader.ReadUInt32Packed();
                        for (int i = 0; i < objectCount; i++)
                        {
                            bool isPlayerObject = continuationReader.ReadBool();
                            ulong networkId = continuationReader.ReadUInt64Packed();
                            ulong ownerId = continuationReader.ReadUInt64Packed();
                            bool hasParent = continuationReader.ReadBool();
                            ulong? parentNetworkId = null;

                            if (hasParent)
                            {
                                parentNetworkId = continuationReader.ReadUInt64Packed();
                            }

                            ulong prefabHash;
                            ulong instanceId;
                            bool softSync;

                            if (!m_NetworkManager.NetworkConfig.EnableSceneManagement || m_NetworkManager.NetworkConfig.UsePrefabSync)
                            {
                                softSync = false;
                                instanceId = 0;
                                prefabHash = continuationReader.ReadUInt64Packed();
                            }
                            else
                            {
                                softSync = continuationReader.ReadBool();

                                if (softSync)
                                {
                                    instanceId = continuationReader.ReadUInt64Packed();
                                    prefabHash = 0;
                                }
                                else
                                {
                                    prefabHash = continuationReader.ReadUInt64Packed();
                                    instanceId = 0;
                                }
                            }

                            Vector3? pos = null;
                            Quaternion? rot = null;
                            if (continuationReader.ReadBool())
                            {
                                pos = new Vector3(continuationReader.ReadSinglePacked(), continuationReader.ReadSinglePacked(), continuationReader.ReadSinglePacked());
                                rot = Quaternion.Euler(continuationReader.ReadSinglePacked(), continuationReader.ReadSinglePacked(), continuationReader.ReadSinglePacked());
                            }

                            var networkObject = m_NetworkManager.NetworkSpawnManager.CreateLocalNetworkObject(softSync, instanceId, prefabHash, parentNetworkId, pos, rot);
                            m_NetworkManager.NetworkSpawnManager.SpawnNetworkObjectLocally(networkObject, networkId, softSync, isPlayerObject, ownerId, continuationStream, false, 0, true, false);

                            Queue<BufferManager.BufferedMessage> bufferQueue = BufferManager.ConsumeBuffersForNetworkId(networkId);

                            // Apply buffered messages
                            if (bufferQueue != null)
                            {
                                while (bufferQueue.Count > 0)
                                {
                                    BufferManager.BufferedMessage message = bufferQueue.Dequeue();
                                    m_NetworkManager.HandleIncomingData(message.SenderClientId, message.NetworkChannel, new ArraySegment<byte>(message.NetworkBuffer.GetBuffer(), (int)message.NetworkBuffer.Position, (int)message.NetworkBuffer.Length), message.ReceiveTime, false);
                                    BufferManager.RecycleConsumedBufferedMessage(message);
                                }
                            }
                        }

                        m_NetworkManager.NetworkSpawnManager.CleanDiffedSceneObjects();
                        m_NetworkManager.IsConnectedClient = true;
                        m_NetworkManager.InvokeOnClientConnectedCallback(m_NetworkManager.LocalClientId);
                    }
                }

                if (sceneSwitch)
                {
                    UnityAction<Scene, Scene> onSceneLoaded = null;

                    var continuationBuffer = new NetworkBuffer();
                    continuationBuffer.CopyUnreadFrom(stream);
                    continuationBuffer.Position = 0;

                    void OnSceneLoadComplete()
                    {
                        SceneManager.activeSceneChanged -= onSceneLoaded;
                        m_NetworkManager.NetworkSceneManager.IsSpawnedObjectsPendingInDontDestroyOnLoad = false;
                        DelayedSpawnAction(continuationBuffer);
                    }

                    onSceneLoaded = (oldScene, newScene) => { OnSceneLoadComplete(); };
                    SceneManager.activeSceneChanged += onSceneLoaded;
                    m_NetworkManager.NetworkSceneManager.OnFirstSceneSwitchSync(sceneIndex, sceneSwitchProgressGuid);
                }
                else
                {
                    DelayedSpawnAction(stream);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleConnectionApproved.End();
#endif
        }

        internal void HandleAddObject(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleAddObject.Begin();
#endif
            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                bool isPlayerObject = reader.ReadBool();
                ulong networkId = reader.ReadUInt64Packed();
                ulong ownerId = reader.ReadUInt64Packed();
                bool hasParent = reader.ReadBool();
                ulong? parentNetworkId = null;

                if (hasParent)
                {
                    parentNetworkId = reader.ReadUInt64Packed();
                }

                ulong prefabHash;
                ulong instanceId;
                bool softSync;

                if (!m_NetworkManager.NetworkConfig.EnableSceneManagement || m_NetworkManager.NetworkConfig.UsePrefabSync)
                {
                    softSync = false;
                    instanceId = 0;
                    prefabHash = reader.ReadUInt64Packed();
                }
                else
                {
                    softSync = reader.ReadBool();

                    if (softSync)
                    {
                        instanceId = reader.ReadUInt64Packed();
                        prefabHash = 0;
                    }
                    else
                    {
                        prefabHash = reader.ReadUInt64Packed();
                        instanceId = 0;
                    }
                }

                Vector3? pos = null;
                Quaternion? rot = null;
                if (reader.ReadBool())
                {
                    pos = new Vector3(reader.ReadSinglePacked(), reader.ReadSinglePacked(), reader.ReadSinglePacked());
                    rot = Quaternion.Euler(reader.ReadSinglePacked(), reader.ReadSinglePacked(), reader.ReadSinglePacked());
                }

                bool hasPayload = reader.ReadBool();
                int payLoadLength = hasPayload ? reader.ReadInt32Packed() : 0;

                var networkObject = m_NetworkManager.NetworkSpawnManager.CreateLocalNetworkObject(softSync, instanceId, prefabHash, parentNetworkId, pos, rot);
                m_NetworkManager.NetworkSpawnManager.SpawnNetworkObjectLocally(networkObject, networkId, softSync, isPlayerObject, ownerId, stream, hasPayload, payLoadLength, true, false);

                Queue<BufferManager.BufferedMessage> bufferQueue = BufferManager.ConsumeBuffersForNetworkId(networkId);

                // Apply buffered messages
                if (bufferQueue != null)
                {
                    while (bufferQueue.Count > 0)
                    {
                        BufferManager.BufferedMessage message = bufferQueue.Dequeue();
                        m_NetworkManager.HandleIncomingData(message.SenderClientId, message.NetworkChannel, new ArraySegment<byte>(message.NetworkBuffer.GetBuffer(), (int)message.NetworkBuffer.Position, (int)message.NetworkBuffer.Length), message.ReceiveTime, false);
                        BufferManager.RecycleConsumedBufferedMessage(message);
                    }
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleAddObject.End();
#endif
        }

        internal void HandleDestroyObject(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleDestroyObject.Begin();
#endif
            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                m_NetworkManager.NetworkSpawnManager.OnDestroyObject(networkId, true);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleDestroyObject.End();
#endif
        }

        internal void HandleSwitchScene(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleSwitchScene.Begin();
#endif
            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                uint sceneIndex = reader.ReadUInt32Packed();
                Guid switchSceneGuid = new Guid(reader.ReadByteArray());

                var objectBuffer = new NetworkBuffer();
                objectBuffer.CopyUnreadFrom(stream);
                objectBuffer.Position = 0;

                m_NetworkManager.NetworkSceneManager.OnSceneSwitch(sceneIndex, switchSceneGuid, objectBuffer);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleSwitchScene.End();
#endif
        }

        internal void HandleClientSwitchSceneCompleted(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleClientSwitchSceneCompleted.Begin();
#endif
            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                m_NetworkManager.NetworkSceneManager.OnClientSwitchSceneCompleted(clientId, new Guid(reader.ReadByteArray()));
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleClientSwitchSceneCompleted.End();
#endif
        }

        internal void HandleChangeOwner(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleChangeOwner.Begin();
#endif
            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                ulong ownerClientId = reader.ReadUInt64Packed();

                if (m_NetworkManager.NetworkSpawnManager.SpawnedObjects[networkId].OwnerClientId == m_NetworkManager.LocalClientId)
                {
                    //We are current owner.
                    m_NetworkManager.NetworkSpawnManager.SpawnedObjects[networkId].InvokeBehaviourOnLostOwnership();
                }

                if (ownerClientId == m_NetworkManager.LocalClientId)
                {
                    //We are new owner.
                    m_NetworkManager.NetworkSpawnManager.SpawnedObjects[networkId].InvokeBehaviourOnGainedOwnership();
                }

                m_NetworkManager.NetworkSpawnManager.SpawnedObjects[networkId].OwnerClientId = ownerClientId;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleChangeOwner.End();
#endif
        }

        internal void HandleAddObjects(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleAddObjects.Begin();
#endif
            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                ushort objectCount = reader.ReadUInt16Packed();

                for (int i = 0; i < objectCount; i++)
                {
                    HandleAddObject(clientId, stream);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleAddObjects.End();
#endif
        }

        internal void HandleDestroyObjects(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleDestroyObjects.Begin();
#endif
            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                ushort objectCount = reader.ReadUInt16Packed();

                for (int i = 0; i < objectCount; i++)
                {
                    HandleDestroyObject(clientId, stream);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleDestroyObjects.End();
#endif
        }

        internal void HandleTimeSync(ulong clientId, Stream stream, float receiveTime)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleTimeSync.Begin();
#endif
            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                float netTime = reader.ReadSinglePacked();
                m_NetworkManager.UpdateNetworkTime(clientId, netTime, receiveTime);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleTimeSync.End();
#endif
        }

        internal void HandleNetworkVariableDelta(ulong clientId, Stream stream, Action<ulong, PreBufferPreset> bufferCallback, PreBufferPreset bufferPreset)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkVariableDelta.Begin();
#endif
            if (!m_NetworkManager.NetworkConfig.EnableNetworkVariable)
            {
                if (m_NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    m_NetworkManager.NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_DELTA)} received but {nameof(NetworkConfig.EnableNetworkVariable)} is false");
                }

                return;
            }

            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                ulong networkObjectId = reader.ReadUInt64Packed();
                ushort networkBehaviourIndex = reader.ReadUInt16Packed();

                if (m_NetworkManager.NetworkSpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
                {
                    NetworkBehaviour instance = m_NetworkManager.NetworkSpawnManager.SpawnedObjects[networkObjectId].GetNetworkBehaviourAtOrderIndex(networkBehaviourIndex);

                    if (instance == null)
                    {
                        if (m_NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                        {
                            m_NetworkManager.NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_DELTA)} message received for a non-existent behaviour. {nameof(networkObjectId)}: {networkObjectId}, {nameof(networkBehaviourIndex)}: {networkBehaviourIndex}");
                        }
                    }
                    else
                    {
                        NetworkBehaviour.HandleNetworkVariableDeltas(m_NetworkManager, instance.NetworkVariableFields, stream, clientId, instance);
                    }
                }
                else if (m_NetworkManager.IsServer || !m_NetworkManager.NetworkConfig.EnableMessageBuffering)
                {
                    if (m_NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        m_NetworkManager.NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_DELTA)} message received for a non-existent object with {nameof(networkObjectId)}: {networkObjectId}. This delta was lost.");
                    }
                }
                else
                {
                    if (m_NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        m_NetworkManager.NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_DELTA)} message received for a non-existent object with {nameof(networkObjectId)}: {networkObjectId}. This delta will be buffered and might be recovered.");
                    }

                    bufferCallback(networkObjectId, bufferPreset);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkVariableDelta.End();
#endif
        }

        internal void HandleNetworkVariableUpdate(ulong clientId, Stream stream, Action<ulong, PreBufferPreset> bufferCallback, PreBufferPreset bufferPreset)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkVariableUpdate.Begin();
#endif
            if (!m_NetworkManager.NetworkConfig.EnableNetworkVariable)
            {
                if (m_NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    m_NetworkManager.NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_UPDATE)} update received but {nameof(NetworkConfig.EnableNetworkVariable)} is false");
                }

                return;
            }

            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                ulong networkObjectId = reader.ReadUInt64Packed();
                ushort networkBehaviourIndex = reader.ReadUInt16Packed();

                if (m_NetworkManager.NetworkSpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
                {
                    var networkBehaviour = m_NetworkManager.NetworkSpawnManager.SpawnedObjects[networkObjectId].GetNetworkBehaviourAtOrderIndex(networkBehaviourIndex);

                    if (networkBehaviour == null)
                    {
                        if (m_NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                        {
                            m_NetworkManager.NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_UPDATE)} message received for a non-existent behaviour. {nameof(networkObjectId)}: {networkObjectId}, {nameof(networkBehaviourIndex)}: {networkBehaviourIndex}");
                        }
                    }
                    else
                    {
                        NetworkBehaviour.HandleNetworkVariableUpdate(m_NetworkManager, networkBehaviour.NetworkVariableFields, stream, clientId, networkBehaviour);
                    }
                }
                else if (m_NetworkManager.IsServer || !m_NetworkManager.NetworkConfig.EnableMessageBuffering)
                {
                    if (m_NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        m_NetworkManager.NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_UPDATE)} message received for a non-existent object with {nameof(networkObjectId)}: {networkObjectId}. This delta was lost.");
                    }
                }
                else
                {
                    if (m_NetworkManager.NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        m_NetworkManager.NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_UPDATE)} message received for a non-existent object with {nameof(networkObjectId)}: {networkObjectId}. This delta will be buffered and might be recovered.");
                    }

                    bufferCallback(networkObjectId, bufferPreset);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkVariableUpdate.End();
#endif
        }

        /// <summary>
        /// Converts the stream to a PerformanceQueueItem and adds it to the receive queue
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="stream"></param>
        /// <param name="receiveTime"></param>
        internal void RpcReceiveQueueItem(ulong clientId, Stream stream, float receiveTime, RpcQueueContainer.QueueItemType queueItemType)
        {
            if (m_NetworkManager.IsServer && clientId == m_NetworkManager.ServerClientId)
            {
                return;
            }

            ProfilerStatManager.RpcsRcvd.Record();
            PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsReceived);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            switch (queueItemType)
            {
                case RpcQueueContainer.QueueItemType.ServerRpc:
                    s_RpcReceiveQueueItemServerRpc.Begin();
                    break;
                case RpcQueueContainer.QueueItemType.ClientRpc:
                    s_RpcReceiveQueueItemClientRpc.Begin();
                    break;
            }
#endif

            var rpcQueueContainer = m_NetworkManager.RpcQueueContainer;
            rpcQueueContainer.AddQueueItemToInboundFrame(queueItemType, receiveTime, clientId, (NetworkBuffer)stream);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            switch (queueItemType)
            {
                case RpcQueueContainer.QueueItemType.ServerRpc:
                    s_RpcReceiveQueueItemServerRpc.End();
                    break;
                case RpcQueueContainer.QueueItemType.ClientRpc:
                    s_RpcReceiveQueueItemClientRpc.End();
                    break;
            }
#endif
        }

        internal void HandleUnnamedMessage(ulong clientId, Stream stream)
        {
            PerformanceDataManager.Increment(ProfilerConstants.NumberOfUnnamedMessages);
            ProfilerStatManager.UnnamedMessage.Record();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleUnnamedMessage.Begin();
#endif
            m_NetworkManager.CustomMessagingManager.InvokeUnnamedMessage(clientId, stream);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleUnnamedMessage.End();
#endif
        }

        internal void HandleNamedMessage(ulong clientId, Stream stream)
        {
            PerformanceDataManager.Increment(ProfilerConstants.NumberOfNamedMessages);
            ProfilerStatManager.NamedMessage.Record();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNamedMessage.Begin();
#endif
            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                ulong hash = reader.ReadUInt64Packed();

                m_NetworkManager.CustomMessagingManager.InvokeNamedMessage(hash, clientId, stream);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNamedMessage.End();
#endif
        }

        internal void HandleNetworkLog(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkLog.Begin();
#endif
            using (var reader = m_NetworkManager.NetworkReaderPool.GetReader(stream))
            {
                NetworkLog.LogType logType = (NetworkLog.LogType)reader.ReadByte();
                string message = reader.ReadStringPacked();

                switch (logType)
                {
                    case NetworkLog.LogType.Info:
                        NetworkLog.LogInfoServerLocal(message, clientId);
                        break;
                    case NetworkLog.LogType.Warning:
                        NetworkLog.LogWarningServerLocal(message, clientId);
                        break;
                    case NetworkLog.LogType.Error:
                        NetworkLog.LogErrorServerLocal(message, clientId);
                        break;
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkLog.End();
#endif
        }
    }
}
