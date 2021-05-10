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
    internal class InternalMessageHandler : IInternalMessageHandler
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
        private static ProfilerMarker s_HandleAllClientsSwitchSceneCompleted = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleAllClientsSwitchSceneCompleted)}");
#endif

        public NetworkManager NetworkManager => m_NetworkManager;
        private NetworkManager m_NetworkManager;

        public InternalMessageHandler(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        public void HandleConnectionRequest(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleConnectionRequest.Begin();
#endif
            if (NetworkManager.PendingClients.TryGetValue(clientId, out PendingClient client))
            {
                // Set to pending approval to prevent future connection requests from being approved
                client.ConnectionState = PendingClient.State.PendingApproval;
            }

            using (var reader = PooledNetworkReader.Get(stream))
            {
                ulong configHash = reader.ReadUInt64Packed();
                if (!NetworkManager.NetworkConfig.CompareConfig(configHash))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkConfig)} mismatch. The configuration between the server and client does not match");
                    }

                    NetworkManager.DisconnectClient(clientId);
                    return;
                }

                if (NetworkManager.NetworkConfig.ConnectionApproval)
                {
                    byte[] connectionBuffer = reader.ReadByteArray();
                    NetworkManager.InvokeConnectionApproval(connectionBuffer, clientId,
                        (createPlayerObject, playerPrefabHash, approved, position, rotation) =>
                            NetworkManager.HandleApproval(clientId, createPlayerObject, playerPrefabHash, approved, position, rotation));
                }
                else
                {
                    NetworkManager.HandleApproval(clientId, NetworkManager.NetworkConfig.PlayerPrefab != null, null, true, null, null);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleConnectionRequest.End();
#endif
        }

        public void HandleConnectionApproved(ulong clientId, Stream stream, float receiveTime)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleConnectionApproved.Begin();
#endif
            using (var reader = PooledNetworkReader.Get(stream))
            {
                NetworkManager.LocalClientId = reader.ReadUInt64Packed();

                uint sceneIndex = 0;
                var sceneSwitchProgressGuid = new Guid();

                if (NetworkManager.NetworkConfig.EnableSceneManagement)
                {
                    sceneIndex = reader.ReadUInt32Packed();
                    sceneSwitchProgressGuid = new Guid(reader.ReadByteArray());
                }

                bool sceneSwitch = NetworkManager.NetworkConfig.EnableSceneManagement && NetworkManager.SceneManager.HasSceneMismatch(sceneIndex);

                float netTime = reader.ReadSinglePacked();
                NetworkManager.UpdateNetworkTime(clientId, netTime, receiveTime, true);

                NetworkManager.ConnectedClients.Add(NetworkManager.LocalClientId, new NetworkClient { ClientId = NetworkManager.LocalClientId });


                void DelayedSpawnAction(Stream continuationStream)
                {
                    using (var continuationReader = PooledNetworkReader.Get(continuationStream))
                    {
                        if (!NetworkManager.NetworkConfig.EnableSceneManagement)
                        {
                            NetworkManager.SpawnManager.DestroySceneObjects();
                        }
                        else
                        {
                            NetworkManager.SpawnManager.ClientCollectSoftSyncSceneObjectSweep(null);
                        }

                        var objectCount = continuationReader.ReadUInt32Packed();
                        for (int i = 0; i < objectCount; i++)
                        {
                            NetworkObject.DeserializeSceneObject(continuationStream as NetworkBuffer, continuationReader, m_NetworkManager);
                        }

                        NetworkManager.SpawnManager.CleanDiffedSceneObjects();
                        NetworkManager.IsConnectedClient = true;
                        NetworkManager.InvokeOnClientConnectedCallback(NetworkManager.LocalClientId);
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
                        NetworkSceneManager.IsSpawnedObjectsPendingInDontDestroyOnLoad = false;
                        DelayedSpawnAction(continuationBuffer);
                    }

                    onSceneLoaded = (oldScene, newScene) => { OnSceneLoadComplete(); };
                    SceneManager.activeSceneChanged += onSceneLoaded;
                    m_NetworkManager.SceneManager.OnFirstSceneSwitchSync(sceneIndex, sceneSwitchProgressGuid);
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

        public void HandleAddObject(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleAddObject.Begin();
#endif
            using (var reader = PooledNetworkReader.Get(stream))
            {
                var isPlayerObject = reader.ReadBool();
                var networkId = reader.ReadUInt64Packed();
                var ownerClientId = reader.ReadUInt64Packed();
                var hasParent = reader.ReadBool();
                ulong? parentNetworkId = null;

                if (hasParent)
                {
                    parentNetworkId = reader.ReadUInt64Packed();
                }

                var softSync = reader.ReadBool();
                var prefabHash = reader.ReadUInt32Packed();

                Vector3? pos = null;
                Quaternion? rot = null;
                if (reader.ReadBool())
                {
                    pos = new Vector3(reader.ReadSinglePacked(), reader.ReadSinglePacked(), reader.ReadSinglePacked());
                    rot = Quaternion.Euler(reader.ReadSinglePacked(), reader.ReadSinglePacked(), reader.ReadSinglePacked());
                }

                var hasPayload = reader.ReadBool();
                var payLoadLength = hasPayload ? reader.ReadInt32Packed() : 0;

                var networkObject = NetworkManager.SpawnManager.CreateLocalNetworkObject(softSync, prefabHash, ownerClientId, parentNetworkId, pos, rot);
                NetworkManager.SpawnManager.SpawnNetworkObjectLocally(networkObject, networkId, softSync, isPlayerObject, ownerClientId, stream, hasPayload, payLoadLength, true, false);

                Queue<BufferManager.BufferedMessage> bufferQueue = NetworkManager.BufferManager.ConsumeBuffersForNetworkId(networkId);

                // Apply buffered messages
                if (bufferQueue != null)
                {
                    while (bufferQueue.Count > 0)
                    {
                        BufferManager.BufferedMessage message = bufferQueue.Dequeue();
                        NetworkManager.HandleIncomingData(message.SenderClientId, message.NetworkChannel, new ArraySegment<byte>(message.NetworkBuffer.GetBuffer(), (int)message.NetworkBuffer.Position, (int)message.NetworkBuffer.Length), message.ReceiveTime, false);
                        BufferManager.RecycleConsumedBufferedMessage(message);
                    }
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleAddObject.End();
#endif
        }

        public void HandleDestroyObject(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleDestroyObject.Begin();
#endif
            using (var reader = PooledNetworkReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                NetworkManager.SpawnManager.OnDestroyObject(networkId, true);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleDestroyObject.End();
#endif
        }

        public void HandleSwitchScene(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleSwitchScene.Begin();
#endif
            using (var reader = PooledNetworkReader.Get(stream))
            {
                uint sceneIndex = reader.ReadUInt32Packed();
                var switchSceneGuid = new Guid(reader.ReadByteArray());

                var objectBuffer = new NetworkBuffer();
                objectBuffer.CopyUnreadFrom(stream);
                objectBuffer.Position = 0;

                m_NetworkManager.SceneManager.OnSceneSwitch(sceneIndex, switchSceneGuid, objectBuffer);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleSwitchScene.End();
#endif
        }

        public void HandleClientSwitchSceneCompleted(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleClientSwitchSceneCompleted.Begin();
#endif
            using (var reader = PooledNetworkReader.Get(stream))
            {
                m_NetworkManager.SceneManager.OnClientSwitchSceneCompleted(clientId, new Guid(reader.ReadByteArray()));
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleClientSwitchSceneCompleted.End();
#endif
        }

        public void HandleChangeOwner(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleChangeOwner.Begin();
#endif
            using (var reader = PooledNetworkReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                ulong ownerClientId = reader.ReadUInt64Packed();

                if (NetworkManager.SpawnManager.SpawnedObjects[networkId].OwnerClientId == NetworkManager.LocalClientId)
                {
                    //We are current owner.
                    NetworkManager.SpawnManager.SpawnedObjects[networkId].InvokeBehaviourOnLostOwnership();
                }

                if (ownerClientId == NetworkManager.LocalClientId)
                {
                    //We are new owner.
                    NetworkManager.SpawnManager.SpawnedObjects[networkId].InvokeBehaviourOnGainedOwnership();
                }

                NetworkManager.SpawnManager.SpawnedObjects[networkId].OwnerClientId = ownerClientId;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleChangeOwner.End();
#endif
        }

        public void HandleAddObjects(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleAddObjects.Begin();
#endif
            using (var reader = PooledNetworkReader.Get(stream))
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

        public void HandleDestroyObjects(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleDestroyObjects.Begin();
#endif
            using (var reader = PooledNetworkReader.Get(stream))
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

        public void HandleTimeSync(ulong clientId, Stream stream, float receiveTime)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleTimeSync.Begin();
#endif
            using (var reader = PooledNetworkReader.Get(stream))
            {
                float netTime = reader.ReadSinglePacked();
                NetworkManager.UpdateNetworkTime(clientId, netTime, receiveTime);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleTimeSync.End();
#endif
        }

        public void HandleNetworkVariableDelta(ulong clientId, Stream stream, Action<ulong, PreBufferPreset> bufferCallback, PreBufferPreset bufferPreset)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkVariableDelta.Begin();
#endif
            if (!NetworkManager.NetworkConfig.EnableNetworkVariable)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_DELTA)} received but {nameof(NetworkConfig.EnableNetworkVariable)} is false");
                }

                return;
            }

            using (var reader = PooledNetworkReader.Get(stream))
            {
                ulong networkObjectId = reader.ReadUInt64Packed();
                ushort networkBehaviourIndex = reader.ReadUInt16Packed();

                if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
                {
                    NetworkBehaviour instance = networkObject.GetNetworkBehaviourAtOrderIndex(networkBehaviourIndex);

                    if (instance == null)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                        {
                            NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_DELTA)} message received for a non-existent behaviour. {nameof(networkObjectId)}: {networkObjectId}, {nameof(networkBehaviourIndex)}: {networkBehaviourIndex}");
                        }
                    }
                    else
                    {
                        NetworkBehaviour.HandleNetworkVariableDeltas(instance.NetworkVariableFields, stream, clientId, instance, NetworkManager);
                    }
                }
                else if (NetworkManager.IsServer || !NetworkManager.NetworkConfig.EnableMessageBuffering)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_DELTA)} message received for a non-existent object with {nameof(networkObjectId)}: {networkObjectId}. This delta was lost.");
                    }
                }
                else
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_DELTA)} message received for a non-existent object with {nameof(networkObjectId)}: {networkObjectId}. This delta will be buffered and might be recovered.");
                    }

                    bufferCallback(networkObjectId, bufferPreset);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkVariableDelta.End();
#endif
        }

        public void HandleNetworkVariableUpdate(ulong clientId, Stream stream, Action<ulong, PreBufferPreset> bufferCallback, PreBufferPreset bufferPreset)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkVariableUpdate.Begin();
#endif
            if (!NetworkManager.NetworkConfig.EnableNetworkVariable)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_UPDATE)} update received but {nameof(NetworkConfig.EnableNetworkVariable)} is false");
                }

                return;
            }

            using (var reader = PooledNetworkReader.Get(stream))
            {
                ulong networkObjectId = reader.ReadUInt64Packed();
                ushort networkBehaviourIndex = reader.ReadUInt16Packed();

                if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
                {
                    var networkBehaviour = networkObject.GetNetworkBehaviourAtOrderIndex(networkBehaviourIndex);

                    if (networkBehaviour == null)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                        {
                            NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_UPDATE)} message received for a non-existent behaviour. {nameof(networkObjectId)}: {networkObjectId}, {nameof(networkBehaviourIndex)}: {networkBehaviourIndex}");
                        }
                    }
                    else
                    {
                        NetworkBehaviour.HandleNetworkVariableUpdate(networkBehaviour.NetworkVariableFields, stream, clientId, networkBehaviour, NetworkManager);
                    }
                }
                else if (NetworkManager.IsServer || !NetworkManager.NetworkConfig.EnableMessageBuffering)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_UPDATE)} message received for a non-existent object with {nameof(networkObjectId)}: {networkObjectId}. This delta was lost.");
                    }
                }
                else
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkConstants.NETWORK_VARIABLE_UPDATE)} message received for a non-existent object with {nameof(networkObjectId)}: {networkObjectId}. This delta will be buffered and might be recovered.");
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
        public void RpcReceiveQueueItem(ulong clientId, Stream stream, float receiveTime, RpcQueueContainer.QueueItemType queueItemType)
        {
            if (NetworkManager.IsServer && clientId == NetworkManager.ServerClientId)
            {
                return;
            }

            ProfilerStatManager.RpcsRcvd.Record();
            PerformanceDataManager.Increment(ProfilerConstants.RpcReceived);

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

            var rpcQueueContainer = NetworkManager.RpcQueueContainer;
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

        public void HandleUnnamedMessage(ulong clientId, Stream stream)
        {
            PerformanceDataManager.Increment(ProfilerConstants.UnnamedMessageReceived);
            ProfilerStatManager.UnnamedMessage.Record();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleUnnamedMessage.Begin();
#endif
            NetworkManager.CustomMessagingManager.InvokeUnnamedMessage(clientId, stream);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleUnnamedMessage.End();
#endif
        }

        public void HandleNamedMessage(ulong clientId, Stream stream)
        {
            PerformanceDataManager.Increment(ProfilerConstants.NamedMessageReceived);
            ProfilerStatManager.NamedMessage.Record();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNamedMessage.Begin();
#endif
            using (var reader = PooledNetworkReader.Get(stream))
            {
                ulong hash = reader.ReadUInt64Packed();

                NetworkManager.CustomMessagingManager.InvokeNamedMessage(hash, clientId, stream);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNamedMessage.End();
#endif
        }

        public void HandleNetworkLog(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkLog.Begin();
#endif
            using (var reader = PooledNetworkReader.Get(stream))
            {
                var logType = (NetworkLog.LogType)reader.ReadByte();
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

        internal static void HandleSnapshot(ulong clientId, Stream messageStream)
        {
            NetworkManager.Singleton.SnapshotSystem.ReadSnapshot(messageStream);
        }

        public void HandleAllClientsSwitchSceneCompleted(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleAllClientsSwitchSceneCompleted.Begin();
#endif
            using (var reader = PooledNetworkReader.Get(stream))
            {
                var clientIds = reader.ReadULongArray();
                var timedOutClientIds = reader.ReadULongArray();
                NetworkManager.SceneManager.AllClientsReady(clientIds, timedOutClientIds);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleAllClientsSwitchSceneCompleted.End();
#endif
        }
    }
}
