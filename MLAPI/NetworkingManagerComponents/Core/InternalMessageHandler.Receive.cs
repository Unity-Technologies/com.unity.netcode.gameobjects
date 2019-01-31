using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MLAPI.Components;
#if !DISABLE_CRYPTOGRAPHY
using MLAPI.Cryptography;
#endif
using MLAPI.Data;
using MLAPI.Logging;
using MLAPI.Serialization;
using UnityEngine;

namespace MLAPI.Internal
{
    internal static partial class InternalMessageHandler
    {
#if !DISABLE_CRYPTOGRAPHY
        // Runs on client
        internal static void HandleHailRequest(uint clientId, Stream stream, int channelId)
        {
            X509Certificate2 certificate = null;
            byte[] serverDiffieHellmanPublicPart = null;
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                if (netManager.NetworkConfig.EnableEncryption)
                {
                    // Read the certificate
                    if (netManager.NetworkConfig.SignKeyExchange)
                    {
                        // Allocation justification: This runs on client and only once, at initial connection
                        certificate = new X509Certificate2(reader.ReadByteArray());
                        if (CryptographyHelper.VerifyCertificate(certificate, netManager.ConnectedHostname))
                        {
                            // The certificate is not valid :(
                            // Man in the middle.
                            if (LogHelper.CurrentLogLevel <= LogLevel.Normal) if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid certificate. Disconnecting");
                            netManager.StopClient();
                            return;
                        }
                        else
                        {
                            netManager.NetworkConfig.ServerX509Certificate = certificate;
                        }
                    }

                    // Read the ECDH
                    // Allocation justification: This runs on client and only once, at initial connection
                    serverDiffieHellmanPublicPart = reader.ReadByteArray();
                    
                    // Verify the key exchange
                    if (netManager.NetworkConfig.SignKeyExchange)
                    {
                        byte[] serverDiffieHellmanPublicPartSignature = reader.ReadByteArray();

                        RSACryptoServiceProvider rsa = certificate.PublicKey.Key as RSACryptoServiceProvider;

                        if (rsa != null)
                        {
                            using (SHA256Managed sha = new SHA256Managed())
                            {
                                if (!rsa.VerifyData(serverDiffieHellmanPublicPart, sha, serverDiffieHellmanPublicPartSignature))
                                {
                                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Invalid signature. Disconnecting");
                                    netManager.StopClient();
                                    return;
                                }   
                            }
                        }
                    }
                }
            }

            using (PooledBitStream outStream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(outStream))
                {
                    if (netManager.NetworkConfig.EnableEncryption)
                    {
                        // Create a ECDH key
                        EllipticDiffieHellman diffieHellman = new EllipticDiffieHellman(EllipticDiffieHellman.DEFAULT_CURVE, EllipticDiffieHellman.DEFAULT_GENERATOR, EllipticDiffieHellman.DEFAULT_ORDER);
                        netManager.clientAesKey = diffieHellman.GetSharedSecret(serverDiffieHellmanPublicPart);
                        byte[] diffieHellmanPublicKey = diffieHellman.GetPublicKey();
                        writer.WriteByteArray(diffieHellmanPublicKey);
                        if (netManager.NetworkConfig.SignKeyExchange)
                        {
                            RSACryptoServiceProvider rsa = certificate.PublicKey.Key as RSACryptoServiceProvider;

                            if (rsa != null)
                            {
                                using (SHA256CryptoServiceProvider sha = new SHA256CryptoServiceProvider())
                                {
                                    writer.WriteByteArray(rsa.Encrypt(sha.ComputeHash(diffieHellmanPublicKey), false));   
                                }
                            }
                            else
                            {
                                throw new CryptographicException("[MLAPI] Only RSA certificates are supported. No valid RSA key was found");
                            }
                        }
                    }
                }
                // Send HailResponse
                InternalMessageHandler.Send(NetworkingManager.Singleton.ServerClientId, MLAPIConstants.MLAPI_CERTIFICATE_HAIL_RESPONSE, "MLAPI_INTERNAL", outStream, SecuritySendFlags.None, true);
            }
        }

        // Ran on server
        internal static void HandleHailResponse(uint clientId, Stream stream, int channelId)
        {
            if (!netManager.PendingClients.ContainsKey(clientId) || netManager.PendingClients[clientId].ConnectionState != PendingClient.State.PendingHail) return;
            if (!netManager.NetworkConfig.EnableEncryption) return;

            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                if (NetworkingManager.Singleton.PendingClients[clientId].KeyExchange != null)
                {
                    byte[] diffieHellmanPublic = reader.ReadByteArray();
                    netManager.PendingClients[clientId].AesKey = netManager.PendingClients[clientId].KeyExchange.GetSharedSecret(diffieHellmanPublic);
                    if (netManager.NetworkConfig.SignKeyExchange)
                    {
                        byte[] diffieHellmanPublicSignature = reader.ReadByteArray();
                        X509Certificate2 certificate = netManager.NetworkConfig.ServerX509Certificate;
                        RSACryptoServiceProvider rsa = certificate.PrivateKey as RSACryptoServiceProvider;

                        if (rsa != null)
                        {
                            using (SHA256Managed sha = new SHA256Managed())
                            {
                                byte[] clientHash = rsa.Decrypt(diffieHellmanPublicSignature, false);
                                byte[] serverHash = sha.ComputeHash(diffieHellmanPublic);
                                
                                if (!CryptographyHelper.ConstTimeArrayEqual(clientHash, serverHash))
                                {
                                    //Man in the middle.
                                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Signature doesnt match for the key exchange public part. Disconnecting");
                                    netManager.DisconnectClient(clientId);
                                    return;
                                }
                            }
                        }
                        else
                        {
                            throw new CryptographicException("[MLAPI] Only RSA certificates are supported. No valid RSA key was found");
                        }
                    }
                }
            }

            netManager.PendingClients[clientId].ConnectionState = PendingClient.State.PendingConnection;
            netManager.PendingClients[clientId].KeyExchange = null; // Give to GC
            
            // Send greetings, they have passed all the handshakes
            using (PooledBitStream outStream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(outStream))
                {
                    writer.WriteInt64Packed(DateTime.Now.Ticks); // This serves no purpose.
                }
                InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_GREETINGS, "MLAPI_INTERNAL", outStream, SecuritySendFlags.None, true);
            }
        }

        internal static void HandleGreetings(uint clientId, Stream stream, int channelId)
        {
            // Server greeted us, we can now initiate our request to connect.
            NetworkingManager.Singleton.SendConnectionRequest();
        }
#endif

        internal static void HandleConnectionRequest(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                ulong configHash = reader.ReadUInt64Packed();
                if (!netManager.NetworkConfig.CompareConfig(configHash))
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkConfiguration mismatch. The configuration between the server and client does not match");
                    netManager.DisconnectClient(clientId);
                    return;
                }

                if (netManager.NetworkConfig.ConnectionApproval)
                {
                    byte[] connectionBuffer = reader.ReadByteArray();
                    netManager.ConnectionApprovalCallback(connectionBuffer, clientId, netManager.HandleApproval);
                }
                else
                {
                    netManager.HandleApproval(clientId, -1, true, null, null);
                }
            }
        }

        internal static void HandleConnectionApproved(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                netManager.LocalClientId = reader.ReadUInt32Packed();
                uint sceneIndex = 0;
                Guid sceneSwitchProgressGuid = new Guid();
                if (netManager.NetworkConfig.EnableSceneSwitching) 
                {
                    sceneIndex = reader.ReadUInt32Packed();
                    sceneSwitchProgressGuid = new Guid(reader.ReadByteArray());
                }

                float netTime = reader.ReadSinglePacked();
                int remoteStamp = reader.ReadInt32Packed();
                int msDelay = NetworkingManager.Singleton.NetworkConfig.NetworkTransport.GetRemoteDelayTimeMS(clientId, remoteStamp, out byte error);
                netManager.NetworkTime = netTime + (msDelay / 1000f);

                netManager.ConnectedClients.Add(netManager.LocalClientId, new NetworkedClient() { ClientId = netManager.LocalClientId });
                int clientCount = reader.ReadInt32Packed();
                for (int i = 0; i < clientCount; i++)
                {
                    uint _clientId = reader.ReadUInt32Packed();
                    netManager.ConnectedClients.Add(_clientId, new NetworkedClient() { ClientId = _clientId });
                    netManager.ConnectedClientsList.Add(netManager.ConnectedClients[_clientId]);
                }
                if (netManager.NetworkConfig.HandleObjectSpawning)
                {
                    SpawnManager.DestroySceneObjects();
                    int objectCount = reader.ReadInt32Packed();
                    for (int i = 0; i < objectCount; i++)
                    {
                        bool isPlayerObject = reader.ReadBool();
                        uint networkId = reader.ReadUInt32Packed();
                        uint ownerId = reader.ReadUInt32Packed();
                        ulong prefabHash = reader.ReadUInt64Packed();
                        bool isActive = reader.ReadBool();
                        bool destroyWithScene = reader.ReadBool();

                        bool sceneDelayedSpawn = reader.ReadBool();
                        uint sceneSpawnedInIndex = reader.ReadUInt32Packed();

                        float xPos = reader.ReadSinglePacked();
                        float yPos = reader.ReadSinglePacked();
                        float zPos = reader.ReadSinglePacked();

                        float xRot = reader.ReadSinglePacked();
                        float yRot = reader.ReadSinglePacked();
                        float zRot = reader.ReadSinglePacked();

                        NetworkedObject netObject = SpawnManager.CreateSpawnedObject(SpawnManager.GetNetworkedPrefabIndexOfHash(prefabHash), networkId, ownerId, isPlayerObject,
                            sceneSpawnedInIndex, sceneDelayedSpawn, destroyWithScene, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), isActive, stream, false, 0, true);
                    }
                }

                if (netManager.NetworkConfig.EnableSceneSwitching)
                {
                    NetworkSceneManager.OnSceneSwitch(sceneIndex, sceneSwitchProgressGuid);
                }

                netManager.IsConnectedClient = true;
                if (netManager.OnClientConnectedCallback != null)
                    netManager.OnClientConnectedCallback.Invoke(netManager.LocalClientId);
            }
        }

        internal static void HandleAddObject(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                if (netManager.NetworkConfig.HandleObjectSpawning)
                {
                    bool isPlayerObject = reader.ReadBool();
                    uint networkId = reader.ReadUInt32Packed();
                    uint ownerId = reader.ReadUInt32Packed();
                    ulong prefabHash = reader.ReadUInt64Packed();

                    bool destroyWithScene = reader.ReadBool();
                    bool sceneDelayedSpawn = reader.ReadBool();
                    uint sceneSpawnedInIndex = reader.ReadUInt32Packed();

                    float xPos = reader.ReadSinglePacked();
                    float yPos = reader.ReadSinglePacked();
                    float zPos = reader.ReadSinglePacked();

                    float xRot = reader.ReadSinglePacked();
                    float yRot = reader.ReadSinglePacked();
                    float zRot = reader.ReadSinglePacked();

                    bool hasPayload = reader.ReadBool();
                    int payLoadLength = hasPayload ? reader.ReadInt32Packed() : 0;

                    if (isPlayerObject)
                    {
                        netManager.ConnectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                        netManager.ConnectedClientsList.Add(netManager.ConnectedClients[ownerId]);
                    }

                    NetworkedObject netObject = SpawnManager.CreateSpawnedObject(SpawnManager.GetNetworkedPrefabIndexOfHash(prefabHash), networkId, ownerId, isPlayerObject,
                        sceneSpawnedInIndex, sceneDelayedSpawn, destroyWithScene, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), true, stream, hasPayload, payLoadLength, true);

                }
                else
                {
                    uint ownerId = reader.ReadUInt32Packed();
                    netManager.ConnectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                }
            }
        }

        internal static void HandleClientDisconnect(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint disconnectedClientId = reader.ReadUInt32Packed();
                netManager.OnClientDisconnectFromServer(disconnectedClientId);
            }
        }

        internal static void HandleDestroyObject(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint netId = reader.ReadUInt32Packed();
                SpawnManager.OnDestroyObject(netId, true);
            }
        }

        internal static void HandleSwitchScene(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint sceneIndex = reader.ReadUInt32Packed();
                Guid switchSceneGuid = new Guid(reader.ReadByteArray());
                NetworkSceneManager.OnSceneSwitch(sceneIndex, switchSceneGuid);
            }
        }

        internal static void HandleClientSwitchSceneCompleted(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream)) 
            {
                NetworkSceneManager.OnClientSwitchSceneCompleted(clientId, new Guid(reader.ReadByteArray()));
            }
        }

        internal static void HandleSpawnPoolObject(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint netId = reader.ReadUInt32Packed();

                float xPos = reader.ReadSinglePacked();
                float yPos = reader.ReadSinglePacked();
                float zPos = reader.ReadSinglePacked();

                float xRot = reader.ReadSinglePacked();
                float yRot = reader.ReadSinglePacked();
                float zRot = reader.ReadSinglePacked();

                SpawnManager.SpawnedObjects[netId].transform.position = new Vector3(xPos, yPos, zPos);
                SpawnManager.SpawnedObjects[netId].transform.rotation = Quaternion.Euler(xRot, yRot, zRot);
                SpawnManager.SpawnedObjects[netId].gameObject.SetActive(true);
            }
        }

        internal static void HandleDestroyPoolObject(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint netId = reader.ReadUInt32Packed();
                SpawnManager.SpawnedObjects[netId].gameObject.SetActive(false);
            }
        }

        internal static void HandleChangeOwner(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint netId = reader.ReadUInt32Packed();
                uint ownerClientId = reader.ReadUInt32Packed();
                if (SpawnManager.SpawnedObjects[netId].OwnerClientId == netManager.LocalClientId)
                {
                    //We are current owner.
                    SpawnManager.SpawnedObjects[netId].InvokeBehaviourOnLostOwnership();
                }
                if (ownerClientId == netManager.LocalClientId)
                {
                    //We are new owner.
                    SpawnManager.SpawnedObjects[netId].InvokeBehaviourOnGainedOwnership();
                }
                SpawnManager.SpawnedObjects[netId].OwnerClientId = ownerClientId;
            }
        }

        internal static void HandleAddObjects(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                if (netManager.NetworkConfig.HandleObjectSpawning)
                {
                    ushort objectCount = reader.ReadUInt16Packed();
                    for (int i = 0; i < objectCount; i++)
                    {
                        bool isPlayerObject = reader.ReadBool();
                        uint networkId = reader.ReadUInt32Packed();
                        uint ownerId = reader.ReadUInt32Packed();
                        ulong prefabHash = reader.ReadUInt64Packed();

                        bool destroyWithScene = reader.ReadBool();
                        bool sceneDelayedSpawn = reader.ReadBool();
                        uint sceneSpawnedInIndex = reader.ReadUInt32Packed();

                        float xPos = reader.ReadSinglePacked();
                        float yPos = reader.ReadSinglePacked();
                        float zPos = reader.ReadSinglePacked();

                        float xRot = reader.ReadSinglePacked();
                        float yRot = reader.ReadSinglePacked();
                        float zRot = reader.ReadSinglePacked();

                        if (isPlayerObject)
                        {
                            netManager.ConnectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                            netManager.ConnectedClientsList.Add(netManager.ConnectedClients[ownerId]);
                        }
                        NetworkedObject netObject = SpawnManager.CreateSpawnedObject(SpawnManager.GetNetworkedPrefabIndexOfHash(prefabHash), networkId, ownerId, isPlayerObject,
                            sceneSpawnedInIndex, sceneDelayedSpawn, destroyWithScene, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), true, stream, false, 0, true);
                    }
                }
            }
        }

        internal static void HandleTimeSync(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                float netTime = reader.ReadSinglePacked();
                int timestamp = reader.ReadInt32Packed();

                int msDelay = NetworkingManager.Singleton.NetworkConfig.NetworkTransport.GetRemoteDelayTimeMS(clientId, timestamp, out byte error);
                netManager.NetworkTime = netTime + (msDelay / 1000f);
            }
        }

        internal static void HandleNetworkedVarDelta(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint netId = reader.ReadUInt32Packed();
                ushort orderIndex = reader.ReadUInt16Packed();

                if (SpawnManager.SpawnedObjects.ContainsKey(netId))
                {
                    NetworkedBehaviour instance = SpawnManager.SpawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex);
                    if (instance == null)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkedVar message recieved for a non existant behaviour");
                        return;
                    }
                    NetworkedBehaviour.HandleNetworkedVarDeltas(instance.networkedVarFields, stream, clientId, instance);
                }
                else if (SpawnManager.PendingSpawnObjects.ContainsKey(netId))
                {
                    NetworkedBehaviour.HandleNetworkedVarDeltas(SpawnManager.PendingSpawnObjects[netId].GetDummyNetworkedVarListAtOrderIndex(orderIndex), stream, clientId, null);
                }
                else
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkedVar message recieved for a non existant object with id: " + netId);
                    return;
                }
            }
        }

        internal static void HandleNetworkedVarUpdate(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint netId = reader.ReadUInt32Packed();
                ushort orderIndex = reader.ReadUInt16Packed();

                if (SpawnManager.SpawnedObjects.ContainsKey(netId))
                {
                    NetworkedBehaviour instance = SpawnManager.SpawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex);
                    if (instance == null)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkedVar message recieved for a non existant behaviour");
                        return;
                    }
                    NetworkedBehaviour.HandleNetworkedVarUpdate(instance.networkedVarFields, stream, clientId, instance);
                }
                else if (SpawnManager.PendingSpawnObjects.ContainsKey(netId))
                {
                    NetworkedBehaviour.HandleNetworkedVarUpdate(SpawnManager.PendingSpawnObjects[netId].GetDummyNetworkedVarListAtOrderIndex(orderIndex), stream, clientId, null);
                }
                else
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkedVar message recieved for a non existant object with id: " + netId);
                    return;
                }
            }
        }
        
        internal static void HandleServerRPC(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint networkId = reader.ReadUInt32Packed();
                ushort behaviourId = reader.ReadUInt16Packed();
                ulong hash = reader.ReadUInt64Packed();

                if (SpawnManager.SpawnedObjects.ContainsKey(networkId)) 
                { 
                    NetworkedBehaviour behaviour = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(behaviourId);
                    if (behaviour != null)
                    {
                        behaviour.OnRemoteServerRPC(hash, clientId, stream);
                    }
                }
            }
        }
        
        internal static void HandleServerRPCRequest(uint clientId, Stream stream, int channelId, SecuritySendFlags security)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint networkId = reader.ReadUInt32Packed();
                ushort behaviourId = reader.ReadUInt16Packed();
                ulong hash = reader.ReadUInt64Packed();
                ulong responseId = reader.ReadUInt64Packed();

                if (SpawnManager.SpawnedObjects.ContainsKey(networkId)) 
                { 
                    NetworkedBehaviour behaviour = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(behaviourId);
                    if (behaviour != null)
                    {
                        object result = behaviour.OnRemoteServerRPC(hash, clientId, stream);

                        using (PooledBitStream responseStream = PooledBitStream.Get())
                        {
                            using (PooledBitWriter responseWriter = PooledBitWriter.Get(responseStream))
                            {
                                responseWriter.WriteUInt64Packed(responseId);
                                responseWriter.WriteObjectPacked(result);
                            }
                            
                            InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_SERVER_RPC_RESPONSE, MessageManager.reverseChannels[channelId], responseStream, security);
                        }
                    }
                }
            }
        }
        
        internal static void HandleServerRPCResponse(uint clientId, Stream stream, int channelId)
        {
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
        }
        
        internal static void HandleClientRPC(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint networkId = reader.ReadUInt32Packed();
                ushort behaviourId = reader.ReadUInt16Packed();
                ulong hash = reader.ReadUInt64Packed();
                
                if (SpawnManager.SpawnedObjects.ContainsKey(networkId)) 
                {
                    NetworkedBehaviour behaviour = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(behaviourId);
                    if (behaviour != null)
                    {
                        behaviour.OnRemoteClientRPC(hash, clientId, stream);
                    }
                }
            }
        }
        
        internal static void HandleClientRPCRequest(uint clientId, Stream stream, int channelId, SecuritySendFlags security)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint networkId = reader.ReadUInt32Packed();
                ushort behaviourId = reader.ReadUInt16Packed();
                ulong hash = reader.ReadUInt64Packed();
                ulong responseId = reader.ReadUInt64Packed();
                
                if (SpawnManager.SpawnedObjects.ContainsKey(networkId)) 
                {
                    NetworkedBehaviour behaviour = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(behaviourId);
                    if (behaviour != null)
                    {
                        object result = behaviour.OnRemoteClientRPC(hash, clientId, stream);
                        
                        using (PooledBitStream responseStream = PooledBitStream.Get())
                        {
                            using (PooledBitWriter responseWriter = PooledBitWriter.Get(responseStream))
                            {
                                responseWriter.WriteUInt64Packed(responseId);
                                responseWriter.WriteObjectPacked(result);
                            }
                            
                            InternalMessageHandler.Send(clientId, MLAPIConstants.MLAPI_CLIENT_RPC_RESPONSE, MessageManager.reverseChannels[channelId], responseStream, security);
                        }
                    }
                }
            }
        }
        
        internal static void HandleClientRPCResponse(uint clientId, Stream stream, int channelId)
        {
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
        }
        
        internal static void HandleCustomMessage(uint clientId, Stream stream, int channelId)
        {
            NetworkingManager.Singleton.InvokeOnIncomingCustomMessage(clientId, stream);
        }
    }
}
