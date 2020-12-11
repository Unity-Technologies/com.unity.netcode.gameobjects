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
using BitStream = MLAPI.Serialization.BitStream;

namespace MLAPI.Messaging
{
    internal static class InternalMessageHandler
    {
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

        static ProfilerMarker s_HandleServerRPC =
            new ProfilerMarker("InternalMessageHandler.HandleServerRPC");

        static ProfilerMarker s_HandleServerRPCRequest =
            new ProfilerMarker("InternalMessageHandler.HandleServerRPCRequest");

        static ProfilerMarker s_HandleServerRPCResponse =
            new ProfilerMarker("InternalMessageHandler.HandleServerRPCResponse");

        static ProfilerMarker s_HandleClientRPC =
            new ProfilerMarker("InternalMessageHandler.HandleClientRPC");

        static ProfilerMarker s_HandleClientRPCRequest =
            new ProfilerMarker("InternalMessageHandler.HandleClientRPCRequest");

        static ProfilerMarker s_HandleClientRPCResponse =
            new ProfilerMarker("InternalMessageHandler.HandleClientRPCResponse");

        static ProfilerMarker s_HandleUnnamedMessage =
            new ProfilerMarker("InternalMessageHandler.HandleUnnamedMessage");

        static ProfilerMarker s_HandleNamedMessage =
            new ProfilerMarker("InternalMessageHandler.HandleNamedMessage");

        static ProfilerMarker s_HandleNetworkLog =
            new ProfilerMarker("InternalMessageHandler.HandleNetworkLog");

#endif

#if !DISABLE_CRYPTOGRAPHY
        // Runs on client
        internal static void HandleHailRequest(ulong clientId, Stream stream)
        {
            X509Certificate2 certificate = null;
            byte[] serverDiffieHellmanPublicPart = null;
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                if (NetworkingManager.Singleton.NetworkConfig.EnableEncryption)
                {
                    // Read the certificate
                    if (NetworkingManager.Singleton.NetworkConfig.SignKeyExchange)
                    {
                        // Allocation justification: This runs on client and only once, at initial connection
                        certificate = new X509Certificate2(reader.ReadByteArray());
                        if (CryptographyHelper.VerifyCertificate(certificate, NetworkingManager.Singleton.ConnectedHostname))
                        {
                            // The certificate is not valid :(
                            // Man in the middle.
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Invalid certificate. Disconnecting");
                            NetworkingManager.Singleton.StopClient();
                            return;
                        }
                        else
                        {
                            NetworkingManager.Singleton.NetworkConfig.ServerX509Certificate = certificate;
                        }
                    }

                    // Read the ECDH
                    // Allocation justification: This runs on client and only once, at initial connection
                    serverDiffieHellmanPublicPart = reader.ReadByteArray();

                    // Verify the key exchange
                    if (NetworkingManager.Singleton.NetworkConfig.SignKeyExchange)
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
                                        NetworkingManager.Singleton.StopClient();
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("No RSA key found in certificate. Disconnecting");
                                NetworkingManager.Singleton.StopClient();
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
                                        NetworkingManager.Singleton.StopClient();
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("No DSA key found in certificate. Disconnecting");
                                NetworkingManager.Singleton.StopClient();
                                return;
                            }
                        }
                        else
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("Invalid signature type. Disconnecting");
                            NetworkingManager.Singleton.StopClient();
                            return;
                        }
                    }
                }
            }

            using (PooledBitStream outStream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(outStream))
                {
                    if (NetworkingManager.Singleton.NetworkConfig.EnableEncryption)
                    {
                        // Create a ECDH key
                        EllipticDiffieHellman diffieHellman = new EllipticDiffieHellman(EllipticDiffieHellman.DEFAULT_CURVE, EllipticDiffieHellman.DEFAULT_GENERATOR, EllipticDiffieHellman.DEFAULT_ORDER);
                        NetworkingManager.Singleton.clientAesKey = diffieHellman.GetSharedSecret(serverDiffieHellmanPublicPart);
                        byte[] diffieHellmanPublicKey = diffieHellman.GetPublicKey();
                        writer.WriteByteArray(diffieHellmanPublicKey);
                    }
                }

                // Send HailResponse
                InternalMessageSender.Send(NetworkingManager.Singleton.ServerClientId, MLAPIConstants.MLAPI_CERTIFICATE_HAIL_RESPONSE, "MLAPI_INTERNAL", outStream, SecuritySendFlags.None, null);
            }
        }

        // Ran on server
        internal static void HandleHailResponse(ulong clientId, Stream stream)
        {
            if (!NetworkingManager.Singleton.PendingClients.ContainsKey(clientId) || NetworkingManager.Singleton.PendingClients[clientId].ConnectionState != PendingClient.State.PendingHail) return;
            if (!NetworkingManager.Singleton.NetworkConfig.EnableEncryption) return;

            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                if (NetworkingManager.Singleton.PendingClients[clientId].KeyExchange != null)
                {
                    byte[] diffieHellmanPublic = reader.ReadByteArray();
                    NetworkingManager.Singleton.PendingClients[clientId].AesKey = NetworkingManager.Singleton.PendingClients[clientId].KeyExchange.GetSharedSecret(diffieHellmanPublic);
                }
            }

            NetworkingManager.Singleton.PendingClients[clientId].ConnectionState = PendingClient.State.PendingConnection;
            NetworkingManager.Singleton.PendingClients[clientId].KeyExchange = null; // Give to GC

            // Send greetings, they have passed all the handshakes
            using (PooledBitStream outStream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(outStream))
                {
                    writer.WriteInt64Packed(DateTime.Now.Ticks); // This serves no purpose.
                }

                InternalMessageSender.Send(clientId, MLAPIConstants.MLAPI_GREETINGS, "MLAPI_INTERNAL", outStream, SecuritySendFlags.None, null);
            }
        }

        internal static void HandleGreetings(ulong clientId, Stream stream)
        {
            // Server greeted us, we can now initiate our request to connect.
            NetworkingManager.Singleton.SendConnectionRequest();
        }
#endif

        internal static void HandleConnectionRequest(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleConnectionRequest.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong configHash = reader.ReadUInt64Packed();
                if (!NetworkingManager.Singleton.NetworkConfig.CompareConfig(configHash))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkConfiguration mismatch. The configuration between the server and client does not match");
                    NetworkingManager.Singleton.DisconnectClient(clientId);
                    return;
                }

                if (NetworkingManager.Singleton.NetworkConfig.ConnectionApproval)
                {
                    byte[] connectionBuffer = reader.ReadByteArray();
                    NetworkingManager.Singleton.InvokeConnectionApproval(connectionBuffer, clientId, (createPlayerObject, playerPrefabHash, approved, position, rotation) =>
                    {
                        NetworkingManager.Singleton.HandleApproval(clientId, createPlayerObject, playerPrefabHash, approved, position, rotation);
                    });
                }
                else
                {
                    NetworkingManager.Singleton.HandleApproval(clientId, NetworkingManager.Singleton.NetworkConfig.CreatePlayerPrefab, null, true, null, null);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleConnectionRequest.End();
#endif
        }

        internal static void HandleConnectionApproved(ulong clientId, Stream stream, float receiveTime)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleConnectionApproved.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                NetworkingManager.Singleton.LocalClientId = reader.ReadUInt64Packed();

                uint sceneIndex = 0;
                Guid sceneSwitchProgressGuid = new Guid();

                if (NetworkingManager.Singleton.NetworkConfig.EnableSceneManagement)
                {
                    sceneIndex = reader.ReadUInt32Packed();
                    sceneSwitchProgressGuid = new Guid(reader.ReadByteArray());
                }

                bool sceneSwitch = NetworkingManager.Singleton.NetworkConfig.EnableSceneManagement && NetworkSceneManager.HasSceneMismatch(sceneIndex);

                float netTime = reader.ReadSinglePacked();
                NetworkingManager.Singleton.UpdateNetworkTime(clientId, netTime, receiveTime, true);

                NetworkingManager.Singleton.ConnectedClients.Add(NetworkingManager.Singleton.LocalClientId, new NetworkedClient() { ClientId = NetworkingManager.Singleton.LocalClientId });


                void DelayedSpawnAction(Stream continuationStream)
                {
                    using (PooledBitReader continuationReader = PooledBitReader.Get(continuationStream))
                    {
                        if (!NetworkingManager.Singleton.NetworkConfig.EnableSceneManagement || NetworkingManager.Singleton.NetworkConfig.UsePrefabSync)
                        {
                            SpawnManager.DestroySceneObjects();
                        }
                        else
                        {
                            SpawnManager.ClientCollectSoftSyncSceneObjectSweep(null);
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

                            if (!NetworkingManager.Singleton.NetworkConfig.EnableSceneManagement || NetworkingManager.Singleton.NetworkConfig.UsePrefabSync)
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

                            NetworkedObject netObject = SpawnManager.CreateLocalNetworkedObject(softSync, instanceId, prefabHash, parentNetworkId, pos, rot);
                            SpawnManager.SpawnNetworkedObjectLocally(netObject, networkId, softSync, isPlayerObject, ownerId, continuationStream, false, 0, true, false);

                            Queue<BufferManager.BufferedMessage> bufferQueue = BufferManager.ConsumeBuffersForNetworkId(networkId);

                            // Apply buffered messages
                            if (bufferQueue != null)
                            {
                                while (bufferQueue.Count > 0)
                                {
                                    BufferManager.BufferedMessage message = bufferQueue.Dequeue();

                                    NetworkingManager.Singleton.HandleIncomingData(message.sender, message.channelName, new ArraySegment<byte>(message.payload.GetBuffer(), (int)message.payload.Position, (int)message.payload.Length), message.receiveTime, false);

                                    BufferManager.RecycleConsumedBufferedMessage(message);
                                }
                            }
                        }

                        NetworkingManager.Singleton.IsConnectedClient = true;

                        NetworkingManager.Singleton.InvokeOnClientConnectedCallback(NetworkingManager.Singleton.LocalClientId);
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
                        NetworkSceneManager.isSpawnedObjectsPendingInDontDestroyOnLoad = false;
                        DelayedSpawnAction(continuationStream);
                    }

                    onSceneLoaded = (oldScene, newScene) => { OnSceneLoadComplete(); };

                    SceneManager.activeSceneChanged += onSceneLoaded;

                    NetworkSceneManager.OnFirstSceneSwitchSync(sceneIndex, sceneSwitchProgressGuid);
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

        internal static void HandleAddObject(ulong clientId, Stream stream)
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

                if (!NetworkingManager.Singleton.NetworkConfig.EnableSceneManagement || NetworkingManager.Singleton.NetworkConfig.UsePrefabSync)
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

                NetworkedObject netObject = SpawnManager.CreateLocalNetworkedObject(softSync, instanceId, prefabHash, parentNetworkId, pos, rot);
                SpawnManager.SpawnNetworkedObjectLocally(netObject, networkId, softSync, isPlayerObject, ownerId, stream, hasPayload, payLoadLength, true, false);

                Queue<BufferManager.BufferedMessage> bufferQueue = BufferManager.ConsumeBuffersForNetworkId(networkId);

                // Apply buffered messages
                if (bufferQueue != null)
                {
                    while (bufferQueue.Count > 0)
                    {
                        BufferManager.BufferedMessage message = bufferQueue.Dequeue();

                        NetworkingManager.Singleton.HandleIncomingData(message.sender, message.channelName, new ArraySegment<byte>(message.payload.GetBuffer(), (int)message.payload.Position, (int)message.payload.Length), message.receiveTime, false);

                        BufferManager.RecycleConsumedBufferedMessage(message);
                    }
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleAddObject.End();
#endif
        }

        internal static void HandleDestroyObject(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleDestroyObject.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                SpawnManager.OnDestroyObject(networkId, true);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleDestroyObject.End();
#endif
        }

        internal static void HandleSwitchScene(ulong clientId, Stream stream)
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

                NetworkSceneManager.OnSceneSwitch(sceneIndex, switchSceneGuid, objectStream);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleSwitchScene.End();
#endif
        }

        internal static void HandleClientSwitchSceneCompleted(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleClientSwitchSceneCompleted.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                NetworkSceneManager.OnClientSwitchSceneCompleted(clientId, new Guid(reader.ReadByteArray()));
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleClientSwitchSceneCompleted.End();
#endif
        }

        internal static void HandleChangeOwner(ulong clientId, Stream stream)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleChangeOwner.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                ulong ownerClientId = reader.ReadUInt64Packed();

                if (SpawnManager.SpawnedObjects[networkId].OwnerClientId == NetworkingManager.Singleton.LocalClientId)
                {
                    //We are current owner.
                    SpawnManager.SpawnedObjects[networkId].InvokeBehaviourOnLostOwnership();
                }
                if (ownerClientId == NetworkingManager.Singleton.LocalClientId)
                {
                    //We are new owner.
                    SpawnManager.SpawnedObjects[networkId].InvokeBehaviourOnGainedOwnership();
                }
                SpawnManager.SpawnedObjects[networkId].OwnerClientId = ownerClientId;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleChangeOwner.End();
#endif
        }

        internal static void HandleAddObjects(ulong clientId, Stream stream)
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

        internal static void HandleDestroyObjects(ulong clientId, Stream stream)
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

        internal static void HandleTimeSync(ulong clientId, Stream stream, float receiveTime)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleTimeSync.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                float netTime = reader.ReadSinglePacked();
                NetworkingManager.Singleton.UpdateNetworkTime(clientId, netTime, receiveTime);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleTimeSync.End();
#endif
        }

        internal static void HandleNetworkedVarDelta(ulong clientId, Stream stream, Action<ulong, PreBufferPreset> bufferCallback, PreBufferPreset bufferPreset)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkedVarDelta.Begin();
#endif
            if (!NetworkingManager.Singleton.NetworkConfig.EnableNetworkedVar)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVar delta received but EnableNetworkedVar is false");
                return;
            }

            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                ushort orderIndex = reader.ReadUInt16Packed();

                if (SpawnManager.SpawnedObjects.ContainsKey(networkId))
                {
                    NetworkedBehaviour instance = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(orderIndex);

                    if (instance == null)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVarDelta message received for a non-existent behaviour. NetworkId: " + networkId + ", behaviourIndex: " + orderIndex);
                    }
                    else
                    {
                        NetworkedBehaviour.HandleNetworkedVarDeltas(instance.networkedVarFields, stream, clientId, instance);
                    }
                }
                else if (NetworkingManager.Singleton.IsServer || !NetworkingManager.Singleton.NetworkConfig.EnableMessageBuffering)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVarDelta message received for a non-existent object with id: " + networkId + ". This delta was lost.");
                }
                else
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVarDelta message received for a non-existent object with id: " + networkId + ". This delta will be buffered and might be recovered.");
                    bufferCallback(networkId, bufferPreset);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkedVarDelta.End();
#endif
        }

        internal static void HandleNetworkedVarUpdate(ulong clientId, Stream stream, Action<ulong, PreBufferPreset> bufferCallback, PreBufferPreset bufferPreset)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNetworkedVarUpdate.Begin();
#endif
            if (!NetworkingManager.Singleton.NetworkConfig.EnableNetworkedVar)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVar update received but EnableNetworkedVar is false");
                return;
            }

            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                ushort orderIndex = reader.ReadUInt16Packed();

                if (SpawnManager.SpawnedObjects.ContainsKey(networkId))
                {
                    NetworkedBehaviour instance = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(orderIndex);

                    if (instance == null)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVarUpdate message received for a non-existent behaviour. NetworkId: " + networkId + ", behaviourIndex: " + orderIndex);
                    }
                    else
                    {
                        NetworkedBehaviour.HandleNetworkedVarUpdate(instance.networkedVarFields, stream, clientId, instance);
                    }
                }
                else if (NetworkingManager.Singleton.IsServer || !NetworkingManager.Singleton.NetworkConfig.EnableMessageBuffering)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVarUpdate message received for a non-existent object with id: " + networkId + ". This delta was lost.");
                }
                else
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("NetworkedVarUpdate message received for a non-existent object with id: " + networkId + ". This delta will be buffered and might be recovered.");
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
        internal static void RPCReceiveQueueItem(ulong clientId, Stream stream,float receiveTime,RPCQueueManager.QueueItemType queueItemType, long length)
        {
            if (NetworkingManager.Singleton.IsServer && clientId == NetworkingManager.Singleton.ServerClientId)
            {
                return;
            }
            ProfilerStatManager.rpcsRcvd.Record();
            RPCQueueManager rpcQueueManager = NetworkingManager.Singleton.GetRPCQueueManager();
            if(rpcQueueManager != null)
            {
                rpcQueueManager.AddQueueItemToInboundFrame(queueItemType, receiveTime, clientId, (BitStream)stream, length);
            }
        }




        internal static void HandleServerRPC(ulong clientId, Stream stream)
        {
            ProfilerStatManager.rpcsRcvd.Record();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleServerRPC.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                ushort behaviourId = reader.ReadUInt16Packed();
                ulong hash = reader.ReadUInt64Packed();

                if (SpawnManager.SpawnedObjects.ContainsKey(networkId))
                {
                    NetworkedBehaviour behaviour = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(behaviourId);

                    if (behaviour == null)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("ServerRPC message received for a non-existent behaviour. NetworkId: " + networkId + ", behaviourIndex: " + behaviourId);
                    }
                    else
                    {
                        behaviour.OnRemoteServerRPC(hash, clientId, stream);
                    }
                }
                else if (NetworkingManager.Singleton.IsServer || !NetworkingManager.Singleton.NetworkConfig.EnableMessageBuffering)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("ServerRPC message received for a non-existent object with id: " + networkId + ". This message is lost.");
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleServerRPC.End();
#endif
        }

        internal static void HandleServerRPCRequest(ulong clientId, Stream stream, string channelName, SecuritySendFlags security)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleServerRPCRequest.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                ushort behaviourId = reader.ReadUInt16Packed();
                ulong hash = reader.ReadUInt64Packed();
                ulong responseId = reader.ReadUInt64Packed();

                if (SpawnManager.SpawnedObjects.ContainsKey(networkId))
                {
                    NetworkedBehaviour behaviour = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(behaviourId);

                    if (behaviour == null)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("ServerRPCRequest message received for a non-existent behaviour. NetworkId: " + networkId + ", behaviourIndex: " + behaviourId);
                    }
                    else
                    {
                        object result = behaviour.OnRemoteServerRPC(hash, clientId, stream);

                        using (PooledBitStream responseStream = PooledBitStream.Get())
                        {
                            using (PooledBitWriter responseWriter = PooledBitWriter.Get(responseStream))
                            {
                                responseWriter.WriteUInt64Packed(responseId);
                                responseWriter.WriteObjectPacked(result);
                            }

                            InternalMessageSender.Send(clientId, MLAPIConstants.MLAPI_SERVER_RPC_RESPONSE, channelName, responseStream, security);
                            ProfilerStatManager.rpcsSent.Record();
                        }
                    }
                }
                else
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("ServerRPCRequest message received for a non-existent object with id: " + networkId + ". This message is lost.");
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleServerRPCRequest.End();
#endif
        }

        internal static void HandleServerRPCResponse(ulong clientId, Stream stream)
        {
            ProfilerStatManager.rpcsRcvd.Record();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleServerRPCResponse.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong responseId = reader.ReadUInt64Packed();

                if (ResponseMessageManager.ContainsKey(responseId))
                {
                    RpcResponseBase responseBase = ResponseMessageManager.GetByKey(responseId);

                    ResponseMessageManager.Remove(responseId);

                    responseBase.IsDone = true;
                    responseBase.Result = reader.ReadObjectPacked(responseBase.Type);
                    responseBase.IsSuccessful = true;
                }
                else
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("ServerRPCResponse message received for a non-existent responseId: " + responseId + ". This response is lost.");
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleServerRPCResponse.End();
#endif
        }

        internal static void HandleClientRPC(ulong clientId, Stream stream, Action<ulong, PreBufferPreset> bufferCallback, PreBufferPreset bufferPreset)
        {
            ProfilerStatManager.rpcsRcvd.Record();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleClientRPC.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                ushort behaviourId = reader.ReadUInt16Packed();
                ulong hash = reader.ReadUInt64Packed();

                if (SpawnManager.SpawnedObjects.ContainsKey(networkId))
                {
                    NetworkedBehaviour behaviour = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(behaviourId);

                    if (behaviour == null)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("ClientRPC message received for a non-existent behaviour. NetworkId: " + networkId + ", behaviourIndex: " + behaviourId);
                    }
                    else
                    {
                        behaviour.OnRemoteClientRPC(hash, clientId, stream);
                    }
                }
                else if (NetworkingManager.Singleton.IsServer || !NetworkingManager.Singleton.NetworkConfig.EnableMessageBuffering)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("ClientRPC message received for a non-existent object with id: " + networkId + ". This message is lost.");
                }
                else
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("ClientRPC message received for a non-existent object with id: " + networkId + ". This message will be buffered and might be recovered.");
                    bufferCallback(networkId, bufferPreset);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleClientRPC.End();
#endif
        }

        internal static void HandleClientRPCRequest(ulong clientId, Stream stream, string channelName, SecuritySendFlags security, Action<ulong, PreBufferPreset> bufferCallback, PreBufferPreset bufferPreset)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleClientRPCRequest.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong networkId = reader.ReadUInt64Packed();
                ushort behaviourId = reader.ReadUInt16Packed();
                ulong hash = reader.ReadUInt64Packed();
                ulong responseId = reader.ReadUInt64Packed();

                if (SpawnManager.SpawnedObjects.ContainsKey(networkId))
                {
                    NetworkedBehaviour behaviour = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(behaviourId);

                    if (behaviour == null)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("ClientRPCRequest message received for a non-existent behaviour. NetworkId: " + networkId + ", behaviourIndex: " + behaviourId);
                    }
                    else
                    {
                        object result = behaviour.OnRemoteClientRPC(hash, clientId, stream);

                        using (PooledBitStream responseStream = PooledBitStream.Get())
                        {
                            using (PooledBitWriter responseWriter = PooledBitWriter.Get(responseStream))
                            {
                                responseWriter.WriteUInt64Packed(responseId);
                                responseWriter.WriteObjectPacked(result);
                            }

                            InternalMessageSender.Send(clientId, MLAPIConstants.MLAPI_CLIENT_RPC_RESPONSE, channelName, responseStream, security);
                            ProfilerStatManager.rpcsSent.Record();
                        }
                    }
                }
                else if (NetworkingManager.Singleton.IsServer || !NetworkingManager.Singleton.NetworkConfig.EnableMessageBuffering)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("ClientRPCRequest message received for a non-existent object with id: " + networkId + ". This message is lost.");
                }
                else
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogWarning("ClientRPCRequest message received for a non-existent object with id: " + networkId + ". This message will be buffered and might be recovered.");
                    bufferCallback(networkId, bufferPreset);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleClientRPCRequest.End();
#endif
        }

        internal static void HandleClientRPCResponse(ulong clientId, Stream stream)
        {
            ProfilerStatManager.rpcsRcvd.Record();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleClientRPCResponse.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong responseId = reader.ReadUInt64Packed();

                if (ResponseMessageManager.ContainsKey(responseId))
                {
                    RpcResponseBase responseBase = ResponseMessageManager.GetByKey(responseId);

                    if (responseBase.ClientId != clientId) return;

                    ResponseMessageManager.Remove(responseId);

                    responseBase.IsDone = true;
                    responseBase.Result = reader.ReadObjectPacked(responseBase.Type);
                    responseBase.IsSuccessful = true;
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleClientRPCResponse.End();
#endif
        }

        internal static void HandleUnnamedMessage(ulong clientId, Stream stream)
        {
            ProfilerStatManager.unnamedMessage.Record();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleUnnamedMessage.Begin();
#endif
            CustomMessagingManager.InvokeUnnamedMessage(clientId, stream);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleUnnamedMessage.End();
#endif
        }

        internal static void HandleNamedMessage(ulong clientId, Stream stream)
        {
            ProfilerStatManager.namedMessage.Record();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNamedMessage.Begin();
#endif
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong hash = reader.ReadUInt64Packed();

                CustomMessagingManager.InvokeNamedMessage(hash, clientId, stream);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleNamedMessage.End();
#endif
        }

        internal static void HandleNetworkLog(ulong clientId, Stream stream)
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
