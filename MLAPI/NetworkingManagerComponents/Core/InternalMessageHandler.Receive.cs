using System.IO;
using System.Security.Cryptography;
using MLAPI.Components;
using MLAPI.Data;
using MLAPI.Logging;
using MLAPI.Serialization;
using UnityEngine;

namespace MLAPI.Internal
{
    internal static partial class InternalMessageHandler
    {
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

#if !DISABLE_CRYPTOGRAPHY
                if (netManager.NetworkConfig.EnableEncryption)
                {
                    byte[] diffiePublic = reader.ReadByteArray();
                    netManager.diffieHellmanPublicKeys.Add(clientId, diffiePublic);

                }
#endif
                if (netManager.NetworkConfig.ConnectionApproval)
                {
                    byte[] connectionBuffer = reader.ReadByteArray();
                    netManager.ConnectionApprovalCallback(connectionBuffer, clientId, netManager.HandleApproval);
                }
                else
                {
                    netManager.HandleApproval(clientId, -1, true, Vector3.zero, Quaternion.identity);
                }
            }
        }

        internal static void HandleConnectionApproved(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                netManager.LocalClientId = reader.ReadUInt32Packed();
                uint sceneIndex = 0;
                if (netManager.NetworkConfig.EnableSceneSwitching)
                    sceneIndex = reader.ReadUInt32Packed();

#if !DISABLE_CRYPTOGRAPHY
                if (netManager.NetworkConfig.EnableEncryption)
                {
                    byte[] serverPublicKey = reader.ReadByteArray();
                    netManager.clientAesKey = netManager.clientDiffieHellman.GetSharedSecret(serverPublicKey);
                    if (netManager.NetworkConfig.SignKeyExchange)
                    {
                        byte[] publicKeySignature = reader.ReadByteArray();
                        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                        {
                            rsa.PersistKeyInCsp = false;
                            rsa.FromXmlString(netManager.NetworkConfig.RSAPublicKey);
                            if (!rsa.VerifyData(serverPublicKey, new SHA512CryptoServiceProvider(), publicKeySignature))
                            {
                                //Man in the middle.
                                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Signature doesnt match for the key exchange public part. Disconnecting");
                                netManager.StopClient();
                                return;
                            }
                        }
                    }
                }
#endif

                float netTime = reader.ReadSinglePacked();
                int remoteStamp = reader.ReadInt32Packed();
                int msDelay = NetworkingManager.singleton.NetworkConfig.NetworkTransport.GetRemoteDelayTimeMS(clientId, remoteStamp, out byte error);
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
                        int prefabId = reader.ReadInt32Packed();
                        bool isActive = reader.ReadBool();
                        bool sceneObject = reader.ReadBool();

                        float xPos = reader.ReadSinglePacked();
                        float yPos = reader.ReadSinglePacked();
                        float zPos = reader.ReadSinglePacked();

                        float xRot = reader.ReadSinglePacked();
                        float yRot = reader.ReadSinglePacked();
                        float zRot = reader.ReadSinglePacked();

                        NetworkedObject netObject = SpawnManager.CreateSpawnedObject(prefabId, networkId, ownerId, isPlayerObject,
                            new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), stream, false, true);
                        netObject.sceneObject = sceneObject;
                        netObject.gameObject.SetActive(isActive);
                    }
                }

                if (netManager.NetworkConfig.EnableSceneSwitching)
                {
                    NetworkSceneManager.OnSceneSwitch(sceneIndex);
                }

                netManager.isConnectedClients = true;
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
                    int prefabId = reader.ReadInt32Packed();
                    bool sceneObject = reader.ReadBool();

                    float xPos = reader.ReadSinglePacked();
                    float yPos = reader.ReadSinglePacked();
                    float zPos = reader.ReadSinglePacked();

                    float xRot = reader.ReadSinglePacked();
                    float yRot = reader.ReadSinglePacked();
                    float zRot = reader.ReadSinglePacked();

                    bool hasPayload = reader.ReadBool();

                    if (isPlayerObject)
                    {
                        netManager.ConnectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                        netManager.ConnectedClientsList.Add(netManager.ConnectedClients[ownerId]);
                    }
                    NetworkedObject netObject = SpawnManager.CreateSpawnedObject(prefabId, networkId, ownerId, isPlayerObject,
                        new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), stream, hasPayload, true);

                    netObject.sceneObject = sceneObject;

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
                NetworkSceneManager.OnSceneSwitch(reader.ReadUInt32Packed());
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
                        int prefabId = reader.ReadInt32Packed();
                        bool sceneObject = reader.ReadBool();

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
                        NetworkedObject netObject = SpawnManager.CreateSpawnedObject(prefabId, networkId, ownerId, isPlayerObject,
                            new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), stream, false, true);
                        netObject.sceneObject = sceneObject;
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

                int msDelay = NetworkingManager.singleton.NetworkConfig.NetworkTransport.GetRemoteDelayTimeMS(clientId, timestamp, out byte error);
                netManager.NetworkTime = netTime + (msDelay / 1000f);
            }
        }

        internal static void HandleNetworkedVarDelta(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint netId = reader.ReadUInt32Packed();
                ushort orderIndex = reader.ReadUInt16Packed();

                if (!SpawnManager.SpawnedObjects.ContainsKey(netId))
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkedVar message recieved for a non existant object with id: " + netId);
                    return;
                }
                else if (SpawnManager.SpawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex) == null)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkedVar message recieved for a non existant behaviour");
                    return;
                }

                SpawnManager.SpawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).HandleNetworkedVarDeltas(stream, clientId);
            }
        }

        internal static void HandleNetworkedVarUpdate(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint netId = reader.ReadUInt32Packed();
                ushort orderIndex = reader.ReadUInt16Packed();

                if (!SpawnManager.SpawnedObjects.ContainsKey(netId))
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkedVar message recieved for a non existant object with id: " + netId);
                    return;
                }
                else if (SpawnManager.SpawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex) == null)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkedVar message recieved for a non existant behaviour");
                    return;
                }

                SpawnManager.SpawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).HandleNetworkedVarUpdate(stream, clientId);
            }
        }
        
        internal static void HandleServerRPC(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint networkId = reader.ReadUInt32Packed();
                ushort behaviourId = reader.ReadUInt16Packed();
                ulong hash = reader.ReadUInt64Packed();

                NetworkedBehaviour behaviour = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(behaviourId);
                behaviour.OnRemoteServerRPC(hash, clientId, stream);
            }
        }
        
        internal static void HandleClientRPC(uint clientId, Stream stream, int channelId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                uint networkId = reader.ReadUInt32Packed();
                ushort behaviourId = reader.ReadUInt16Packed();
                ulong hash = reader.ReadUInt64Packed();

                NetworkedBehaviour behaviour = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(behaviourId);
                behaviour.OnRemoteClientRPC(hash, clientId, stream);
            }
        }
        
        internal static void HandleCustomMessage(uint clientId, Stream stream, int channelId)
        {
            NetworkingManager.singleton.InvokeOnIncommingCustomMessage(clientId, stream);
        }
    }
}
