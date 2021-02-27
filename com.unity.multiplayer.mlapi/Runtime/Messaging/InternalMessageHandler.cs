using System;
using System.IO;
using MLAPI.Configuration;
using MLAPI.Connection;
#if !DISABLE_CRYPTOGRAPHY
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
#endif
using MLAPI.Security;
using MLAPI.Logging;
using MLAPI.SceneManagement;
using MLAPI.Serialization.Pooled;
using MLAPI.Spawning;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using MLAPI.Messaging.Buffering;
using MLAPI.Profiling;
using Unity.Profiling;
using MLAPI.Serialization;
using MLAPI.Transports;
using BitStream = MLAPI.Serialization.BitStream;

namespace MLAPI.Messaging
{
    internal class InternalMessageHandler
    {
        private NetworkingManager networkingManager;

        internal InternalMessageHandler(NetworkingManager manager )
        {
            networkingManager = manager;
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        static ProfilerMarker s_HandleConnectionRequest = new ProfilerMarker("InternalMessageHandler.HandleConnectionRequest");
        static ProfilerMarker s_HandleConnectionApproved = new ProfilerMarker("InternalMessageHandler.HandleConnectionApproved");
        static ProfilerMarker s_HandleAddObject = new ProfilerMarker("InternalMessageHandler.HandleAddObject");
        static ProfilerMarker s_HandleDestroyObject = new ProfilerMarker("InternalMessageHandler.HandleDestroyObject");
        static ProfilerMarker s_HandleSwitchScene = new ProfilerMarker("InternalMessageHandler.HandleSwitchScene");
        static ProfilerMarker s_HandleClientSwitchSceneCompleted = new ProfilerMarker("InternalMessageHandler.HandleClientSwitchSceneCompleted");
        static ProfilerMarker s_HandleChangeOwner =
            new ProfilerMarker("InternalMessageHandler.HandleChangeOwner");
        static ProfilerMarker s_HandleAddObjects =
            new ProfilerMarker("InternalMessageHandler.HandleAddObjects");
        static ProfilerMarker s_HandleDestroyObjects =
            new ProfilerMarker("InternalMessageHandler.HandleDestroyObjects");
        static ProfilerMarker s_HandleTimeSync =
            new ProfilerMarker("InternalMessageHandler.HandleTimeSync");
        static ProfilerMarker s_HandleNetworkedVarDelta =
            new ProfilerMarker("InternalMessageHandler.HandleNetworkedVarDelta");
        static ProfilerMarker s_HandleNetworkedVarUpdate =
            new ProfilerMarker("InternalMessageHandler.HandleNetworkedVarUpdate");
        static ProfilerMarker s_HandleUnnamedMessage =
            new ProfilerMarker("InternalMessageHandler.HandleUnnamedMessage");
        static ProfilerMarker s_HandleNamedMessage =
            new ProfilerMarker("InternalMessageHandler.HandleNamedMessage");
        static ProfilerMarker s_HandleNetworkLog =
            new ProfilerMarker("InternalMessageHandler.HandleNetworkLog");

#endif

#if !DISABLE_CRYPTOGRAPHY
        // Runs on client
        internal void HandleHailRequest(ulong clientId, Stream stream)
        {
            X509Certificate2 certificate = null;
            byte[] serverDiffieHellmanPublicPart = null;
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                if (networkingManager.NetworkConfig.EnableEncryption)
                {
                    // Read the certificate
                    if (networkingManager.NetworkConfig.SignKeyExchange)
                    {
                        // Allocation justification: This runs on client and only once, at initial connection
                        certificate = new X509Certificate2(reader.ReadByteArray());
                        if (CryptographyHelper.VerifyCertificate(certificate, networkingManager.ConnectedHostname))
                        {
                            // The certificate is not valid :(
                            // Man in the middle.
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Invalid certificate. Disconnecting");
                            networkingManager.StopClient();
                            return;
                        }
                        else
                        {
                            networkingManager.NetworkConfig.ServerX509Certificate = certificate;
                        }
                    }

                    // Read the ECDH
                    // Allocation justification: This runs on client and only once, at initial connection
                    serverDiffieHellmanPublicPart = reader.ReadByteArray();

                    // Verify the key exchange
                    if (networkingManager.NetworkConfig.SignKeyExchange)
                    {
                        int signatureType = reader.ReadByte();

                        byte[] serverDiffieHellmanPublicPartSignature = reader.ReadByteArray();

                        if (signatureType == 0)
                        {
                            RSACryptoServiceProvider rsa = certificate.PublicKey.Key as RSACryptoServiceProvider;

                            if (rsa != null)
                            {
                                using (SHA256Managed sha = new SHA256Managed())
                                {
                                    if (!rsa.VerifyData(serverDiffieHellmanPublicPart, sha, serverDiffieHellmanPublicPartSignature))
                                    {
                                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Invalid RSA signature. Disconnecting");
                                        networkingManager.StopClient();
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("No RSA key found in certificate. Disconnecting");
                                networkingManager.StopClient();
                                return;
                            }
                        }
                        else if (signatureType == 1)
                        {
                            DSACryptoServiceProvider dsa = certificate.PublicKey.Key as DSACryptoServiceProvider;

                            if (dsa != null)
                            {
                                using (SHA256Managed sha = new SHA256Managed())
                                {
                                    if (!dsa.VerifyData(sha.ComputeHash(serverDiffieHellmanPublicPart), serverDiffieHellmanPublicPartSignature))
                                    {
                                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Invalid DSA signature. Disconnecting");
                                        networkingManager.StopClient();
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("No DSA key found in certificate. Disconnecting");
                                networkingManager.StopClient();
                                return;
                            }
                        }
                        else
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Invalid signature type. Disconnecting");
                            networkingManager.StopClient();
                            return;
                        }
                    }
                }
            }

            using (PooledBitStream outStream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(outStream))
                {
                    if (networkingManager.NetworkConfig.EnableEncryption)
                    {
                        // Create a ECDH key
                        EllipticDiffieHellman diffieHellman = new EllipticDiffieHellman(EllipticDiffieHellman.DEFAULT_CURVE, EllipticDiffieHellman.DEFAULT_GENERATOR, EllipticDiffieHellman.DEFAULT_ORDER);
                        networkingManager.clientAesKey = diffieHellman.GetSharedSecret(serverDiffieHellmanPublicPart);
                        byte[] diffieHellmanPublicKey = diffieHellman.GetPublicKey();
                        writer.WriteByteArray(diffieHellmanPublicKey);
                    }
                }

                // Send HailResponse
                InternalMessageSender.Send(networkingManager.ServerClientId, MLAPIConstants.MLAPI_CERTIFICATE_HAIL_RESPONSE, Channel.Internal, outStream, SecuritySendFlags.None);
            }
        }

        // Ran on server
        internal void HandleHailResponse(ulong clientId, Stream stream)
        {
            if (!networkingManager.PendingClients.ContainsKey(clientId) || networkingManager.PendingClients[clientId].ConnectionState != PendingClient.State.PendingHail) return;
            if (!networkingManager.NetworkConfig.EnableEncryption) return;

            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                if (networkingManager.PendingClients[clientId].KeyExchange != null)
                {
                    byte[] diffieHellmanPublic = reader.ReadByteArray();
                    networkingManager.PendingClients[clientId].AesKey = networkingManager.PendingClients[clientId].KeyExchange.GetSharedSecret(diffieHellmanPublic);
                }
            }

            networkingManager.PendingClients[clientId].ConnectionState = PendingClient.State.PendingConnection;
            networkingManager.PendingClients[clientId].KeyExchange = null; // Give to GC

            // Send greetings, they have passed all the handshakes
            using (PooledBitStream outStream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(outStream))
                {
                    writer.WriteInt64Packed(DateTime.Now.Ticks); // This serves no purpose.
                }

                InternalMessageSender.Send(clientId, MLAPIConstants.MLAPI_GREETINGS, Channel.Internal, outStream, SecuritySendFlags.None);
            }
        }

        internal void HandleGreetings(ulong clientId, Stream stream)
        {
            // Server greeted us, we can now initiate our request to connect.
            networkingManager.SendConnectionRequest();
        }
#endif

        internal void HandleConnectionRequest(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleConnectionRequest.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong configHash = reader.ReadUInt64Packed();
                if (!networkingManager.NetworkConfig.CompareConfig(configHash))
                {
                    if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkConfiguration mismatch. The configuration between the server and client does not match");
                    networkingManager.DisconnectClient(clientId);
                    return;
                }

                if (networkingManager.NetworkConfig.ConnectionApproval)
                {
                    byte[] connectionBuffer = reader.ReadByteArray();
                    networkingManager.InvokeConnectionApproval(connectionBuffer, clientId, (createPlayerObject, playerPrefabHash, approved, position, rotation) =>
                    {
                        networkingManager.HandleApproval(clientId, createPlayerObject, playerPrefabHash, approved, position, rotation);
                    });
                }
                else
                {
                    networkingManager.HandleApproval(clientId, networkingManager.NetworkConfig.CreatePlayerPrefab, null, true, null, null);
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
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                networkingManager.LocalClientId = reader.ReadUInt64Packed();

                uint sceneIndex = 0;
                Guid sceneSwitchProgressGuid = new Guid();

                if (networkingManager.NetworkConfig.EnableSceneManagement)
                {
                    sceneIndex = reader.ReadUInt32Packed();
                    sceneSwitchProgressGuid = new Guid(reader.ReadByteArray());
                }

                bool sceneSwitch = networkingManager.NetworkConfig.EnableSceneManagement && networkingManager.NetworkSceneManager.HasSceneMismatch(sceneIndex);

                float netTime = reader.ReadSinglePacked();
                networkingManager.UpdateNetworkTime(clientId, netTime, receiveTime, true);

                networkingManager.ConnectedClients.Add(networkingManager.LocalClientId, new NetworkedClient() { ClientId = networkingManager.LocalClientId });


                void DelayedSpawnAction(Stream continuationStream)
                {
                    using (PooledBitReader continuationReader = PooledBitReader.Get(continuationStream))
                    {
                        if (!networkingManager.NetworkConfig.EnableSceneManagement || networkingManager.NetworkConfig.UsePrefabSync)
                        {
                            networkingManager.SpawnManager.DestroySceneObjects();
                        }
                        else
                        {
                            networkingManager.SpawnManager.ClientCollectSoftSyncSceneObjectSweep(null);
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

                            if (!networkingManager.NetworkConfig.EnableSceneManagement || networkingManager.NetworkConfig.UsePrefabSync)
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

                            NetworkedObject netObject = networkingManager.SpawnManager.CreateLocalNetworkedObject(softSync, instanceId, prefabHash, parentNetworkId, pos, rot);
                            networkingManager.SpawnManager.SpawnNetworkedObjectLocally(netObject, networkId, softSync, isPlayerObject, ownerId, continuationStream, false, 0, true, false);

                            Queue<BufferManager.BufferedMessage> bufferQueue = BufferManager.ConsumeBuffersForNetworkId(networkId);

                            // Apply buffered messages
                            if (bufferQueue != null)
                            {
                                while (bufferQueue.Count > 0)
                                {
                                    BufferManager.BufferedMessage message = bufferQueue.Dequeue();

                                    networkingManager.HandleIncomingData(message.sender, message.channel, new ArraySegment<byte>(message.payload.GetBuffer(), (int)message.payload.Position, (int)message.payload.Length), message.receiveTime, false);

                                    BufferManager.RecycleConsumedBufferedMessage(message);
                                }
                            }
                        }

                        networkingManager.SpawnManager.CleanDiffedSceneObjects();

                        networkingManager.IsConnectedClient = true;

                        networkingManager.InvokeOnClientConnectedCallback(networkingManager.LocalClientId);
                    }
                }

                if (sceneSwitch)
                {
                    UnityAction<Scene, Scene> onSceneLoaded = null;

                    Serialization.BitStream continuationStream = new Serialization.BitStream();
                    continuationStream.CopyUnreadFrom(stream);
                    continuationStream.Position = 0;

                    void OnSceneLoadComplete()
                    {
                        SceneManager.activeSceneChanged -= onSceneLoaded;
                        networkingManager.NetworkSceneManager.isSpawnedObjectsPendingInDontDestroyOnLoad = false;
                        DelayedSpawnAction(continuationStream);
                    }

                    onSceneLoaded = (oldScene, newScene) => { OnSceneLoadComplete(); };

                    SceneManager.activeSceneChanged += onSceneLoaded;

                    networkingManager.NetworkSceneManager.OnFirstSceneSwitchSync(sceneIndex, sceneSwitchProgressGuid);
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
            using (PooledBitReader reader = PooledBitReader.Get(stream))
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

                if (!networkingManager.NetworkConfig.EnableSceneManagement || networkingManager.NetworkConfig.UsePrefabSync)
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

                NetworkedObject netObject = networkingManager.SpawnManager.CreateLocalNetworkedObject(softSync, instanceId, prefabHash, parentNetworkId, pos, rot);
                networkingManager.SpawnManager.SpawnNetworkedObjectLocally(netObject, networkId, softSync, isPlayerObject, ownerId, stream, hasPayload, payLoadLength, true, false);

                Queue<BufferManager.BufferedMessage> bufferQueue = BufferManager.ConsumeBuffersForNetworkId(networkId);

                // Apply buffered messages
                if (bufferQueue != null)
                {
                    while (bufferQueue.Count > 0)
                    {
                        BufferManager.BufferedMessage message = bufferQueue.Dequeue();

                        networkingManager.HandleIncomingData(message.sender, message.channel, new ArraySegment<byte>(message.payload.GetBuffer(), (int)message.payload.Position, (int)message.payload.Length), message.receiveTime, false);

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
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                networkingManager.SpawnManager.OnDestroyObject(networkId, true);
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
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint sceneIndex = reader.ReadUInt32Packed();
                Guid switchSceneGuid = new Guid(reader.ReadByteArray());

                Serialization.BitStream objectStream = new Serialization.BitStream();
                objectStream.CopyUnreadFrom(stream);
                objectStream.Position = 0;

                networkingManager.NetworkSceneManager.OnSceneSwitch(sceneIndex, switchSceneGuid, objectStream);
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
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                networkingManager.NetworkSceneManager.OnClientSwitchSceneCompleted(clientId, new Guid(reader.ReadByteArray()));
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
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                ulong ownerClientId = reader.ReadUInt64Packed();

                if (networkingManager.SpawnManager.SpawnedObjects[networkId].OwnerClientId == networkingManager.LocalClientId)
                {
                    //We are current owner.
                    networkingManager.SpawnManager.SpawnedObjects[networkId].InvokeBehaviourOnLostOwnership();
                }
                if (ownerClientId == networkingManager.LocalClientId)
                {
                    //We are new owner.
                    networkingManager.SpawnManager.SpawnedObjects[networkId].InvokeBehaviourOnGainedOwnership();
                }
                networkingManager.SpawnManager.SpawnedObjects[networkId].OwnerClientId = ownerClientId;
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
            using (PooledBitReader reader = PooledBitReader.Get(stream))
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
            using (PooledBitReader reader = PooledBitReader.Get(stream))
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
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                float netTime = reader.ReadSinglePacked();
                networkingManager.UpdateNetworkTime(clientId, netTime, receiveTime);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleTimeSync.End();
#endif
        }

        internal void HandleNetworkedVarDelta(ulong clientId, Stream stream, Action<ulong, PreBufferPreset> bufferCallback, PreBufferPreset bufferPreset)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkedVarDelta.Begin();
#endif
            if (!networkingManager.NetworkConfig.EnableNetworkedVar)
            {
                if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVar delta received but EnableNetworkedVar is false");
                return;
            }

            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                ushort orderIndex = reader.ReadUInt16Packed();

                if (networkingManager.SpawnManager.SpawnedObjects.ContainsKey(networkId))
                {
                    NetworkedBehaviour instance = networkingManager.SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(orderIndex);

                    if (instance == null)
                    {
                        if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVarDelta message received for a non-existent behaviour. NetworkId: " + networkId + ", behaviourIndex: " + orderIndex);
                    }
                    else
                    {
                        NetworkedBehaviour.HandleNetworkedVarDeltas(networkingManager, instance.networkedVarFields, stream, clientId, instance);
                    }
                }
                else if (networkingManager.IsServer || !networkingManager.NetworkConfig.EnableMessageBuffering)
                {
                    if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVarDelta message received for a non-existent object with id: " + networkId + ". This delta was lost.");
                }
                else
                {
                    if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVarDelta message received for a non-existent object with id: " + networkId + ". This delta will be buffered and might be recovered.");
                    bufferCallback(networkId, bufferPreset);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkedVarDelta.End();
#endif
        }

        internal void HandleNetworkedVarUpdate(ulong clientId, Stream stream, Action<ulong, PreBufferPreset> bufferCallback, PreBufferPreset bufferPreset)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkedVarUpdate.Begin();
#endif
            if (!networkingManager.NetworkConfig.EnableNetworkedVar)
            {
                if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVar update received but EnableNetworkedVar is false");
                return;
            }

            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                ushort orderIndex = reader.ReadUInt16Packed();

                if (networkingManager.SpawnManager.SpawnedObjects.ContainsKey(networkId))
                {
                    NetworkedBehaviour instance = networkingManager.SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(orderIndex);

                    if (instance == null)
                    {
                        if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVarUpdate message received for a non-existent behaviour. NetworkId: " + networkId + ", behaviourIndex: " + orderIndex);
                    }
                    else
                    {
                        NetworkedBehaviour.HandleNetworkedVarUpdate(networkingManager, instance.networkedVarFields, stream, clientId, instance);
                    }
                }
                else if (networkingManager.IsServer || !networkingManager.NetworkConfig.EnableMessageBuffering)
                {
                    if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVarUpdate message received for a non-existent object with id: " + networkId + ". This delta was lost.");
                }
                else
                {
                    if (NetworkingManager.LogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVarUpdate message received for a non-existent object with id: " + networkId + ". This delta will be buffered and might be recovered.");
                    bufferCallback(networkId, bufferPreset);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkedVarUpdate.End();
#endif
        }

        /// <summary>
        /// Converts the stream to a PerformanceQueueItem and adds it to the receive queue
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="stream"></param>
        /// <param name="receiveTime"></param>
        internal void RPCReceiveQueueItem(ulong clientId, Stream stream, float receiveTime, RpcQueueContainer.QueueItemType queueItemType)
        {
            if (networkingManager.IsServer && clientId == networkingManager.ServerClientId)
            {
                return;
            }

            ProfilerStatManager.rpcsRcvd.Record();

            var rpcQueueContainer = networkingManager.rpcQueueContainer;
            rpcQueueContainer.AddQueueItemToInboundFrame(queueItemType, receiveTime, clientId, (BitStream)stream);
        }

        internal void HandleUnnamedMessage(ulong clientId, Stream stream)
        {
            ProfilerStatManager.unnamedMessage.Record();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleUnnamedMessage.Begin();
#endif
            CustomMessagingManager.InvokeUnnamedMessage(networkingManager, clientId, stream);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleUnnamedMessage.End();
#endif
        }

        internal void HandleNamedMessage(ulong clientId, Stream stream)
        {
            ProfilerStatManager.namedMessage.Record();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNamedMessage.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong hash = reader.ReadUInt64Packed();

                CustomMessagingManager.InvokeNamedMessage(networkingManager, hash, clientId, stream);
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
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                NetworkLog.LogType logType = (NetworkLog.LogType)reader.ReadByte();
                string message = reader.ReadStringPacked().ToString();

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
