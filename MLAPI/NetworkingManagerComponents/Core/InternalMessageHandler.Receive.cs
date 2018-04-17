using System;
using System.IO;
using System.Security.Cryptography;
using MLAPI.Data;
using MLAPI.MonoBehaviours.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace MLAPI.NetworkingManagerComponents.Core
{
    internal static partial class InternalMessageHandler
    {
        internal static void HandleConnectionRequest(uint clientId, byte[] incommingData, int channelId)
        {
            using (MemoryStream messageReadStream = new MemoryStream(incommingData))
            {
                using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                {
                    byte[] configHash = messageReader.ReadBytes(32);
                    if (!netManager.NetworkConfig.CompareConfig(configHash))
                    {
                        Debug.LogWarning("MLAPI: NetworkConfiguration missmatch. The configuration between the server and client does not match.");
                        netManager.DisconnectClient(clientId);
                        return;
                    }
                    byte[] aesKey = new byte[0];
                    if (netManager.NetworkConfig.EnableEncryption)
                    {
                        ushort diffiePublicSize = messageReader.ReadUInt16();
                        byte[] diffiePublic = messageReader.ReadBytes(diffiePublicSize);
                        netManager.diffieHellmanPublicKeys.Add(clientId, diffiePublic);

                    }
                    if (netManager.NetworkConfig.ConnectionApproval)
                    {
                        ushort bufferSize = messageReader.ReadUInt16();
                        byte[] connectionBuffer = messageReader.ReadBytes(bufferSize);
                        netManager.ConnectionApprovalCallback(connectionBuffer, clientId, netManager.HandleApproval);
                    }
                    else
                    {
                        netManager.HandleApproval(clientId, true, Vector3.zero, Quaternion.identity);
                    }
                }
            }
        }

        internal static void HandleConnectionApproved(uint clientId, byte[] incommingData, int channelId)
        {
            using (MemoryStream messageReadStream = new MemoryStream(incommingData))
            {
                using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                {
                    netManager.myClientId = messageReader.ReadUInt32();
                    uint sceneIndex = 0;
                    if (netManager.NetworkConfig.EnableSceneSwitching)
                    {
                        sceneIndex = messageReader.ReadUInt32();
                    }

                    if (netManager.NetworkConfig.EnableEncryption)
                    {
                        ushort keyLength = messageReader.ReadUInt16();
                        byte[] serverPublicKey = messageReader.ReadBytes(keyLength);
                        netManager.clientAesKey = netManager.clientDiffieHellman.GetSharedSecret(serverPublicKey);
                        if (netManager.NetworkConfig.SignKeyExchange)
                        {
                            ushort signatureLength = messageReader.ReadUInt16();
                            byte[] publicKeySignature = messageReader.ReadBytes(signatureLength);
                            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                            {
                                rsa.PersistKeyInCsp = false;
                                rsa.FromXmlString(netManager.NetworkConfig.RSAPublicKey);
                                if (!rsa.VerifyData(serverPublicKey, new SHA512CryptoServiceProvider(), publicKeySignature))
                                {
                                    //Man in the middle.
                                    Debug.LogWarning("MLAPI: Signature doesnt match for the key exchange public part. Disconnecting");
                                    netManager.StopClient();
                                    return;
                                }
                            }
                        }
                    }

                    float netTime = messageReader.ReadSingle();
                    int remoteStamp = messageReader.ReadInt32();
                    byte error;
                    NetId netId = new NetId(clientId);
                    int msDelay = NetworkTransport.GetRemoteDelayTimeMS(netId.HostId, netId.ConnectionId, remoteStamp, out error);
                    if ((NetworkError)error != NetworkError.Ok)
                        msDelay = 0;
                    netManager.networkTime = netTime + (msDelay / 1000f);

                    netManager.connectedClients.Add(clientId, new NetworkedClient() { ClientId = clientId });
                    int clientCount = messageReader.ReadInt32();
                    for (int i = 0; i < clientCount; i++)
                    {
                        uint _clientId = messageReader.ReadUInt32();
                        netManager.connectedClients.Add(_clientId, new NetworkedClient() { ClientId = _clientId });
                    }
                    if (netManager.NetworkConfig.HandleObjectSpawning)
                    {
                        SpawnManager.DestroySceneObjects();
                        int objectCount = messageReader.ReadInt32();
                        for (int i = 0; i < objectCount; i++)
                        {
                            bool isPlayerObject = messageReader.ReadBoolean();
                            uint networkId = messageReader.ReadUInt32();
                            uint ownerId = messageReader.ReadUInt32();
                            int prefabId = messageReader.ReadInt32();
                            bool isActive = messageReader.ReadBoolean();
                            bool sceneObject = messageReader.ReadBoolean();

                            float xPos = messageReader.ReadSingle();
                            float yPos = messageReader.ReadSingle();
                            float zPos = messageReader.ReadSingle();

                            float xRot = messageReader.ReadSingle();
                            float yRot = messageReader.ReadSingle();
                            float zRot = messageReader.ReadSingle();

                            if (isPlayerObject)
                            {
                                SpawnManager.SpawnPlayerObject(ownerId, networkId, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot));
                            }
                            else
                            {
                                GameObject go = SpawnManager.SpawnPrefabIndexClient(prefabId, networkId, ownerId,
                                    new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot));

                                go.GetComponent<NetworkedObject>().sceneObject = sceneObject;
                                go.SetActive(isActive);
                            }
                        }
                    }
                    if (netManager.NetworkConfig.EnableSceneSwitching)
                    {
                        NetworkSceneManager.OnSceneSwitch(sceneIndex);
                    }
                }
            }
            netManager._isClientConnected = true;
            if (netManager.OnClientConnectedCallback != null)
                netManager.OnClientConnectedCallback.Invoke(clientId);
        }

        internal static void HandleAddObject(uint clientId, byte[] incommingData, int channelId)
        {
            using (MemoryStream messageReadStream = new MemoryStream(incommingData))
            {
                using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                {
                    if (netManager.NetworkConfig.HandleObjectSpawning)
                    {
                        bool isPlayerObject = messageReader.ReadBoolean();
                        uint networkId = messageReader.ReadUInt32();
                        uint ownerId = messageReader.ReadUInt32();
                        int prefabId = messageReader.ReadInt32();
                        bool sceneObject = messageReader.ReadBoolean();

                        float xPos = messageReader.ReadSingle();
                        float yPos = messageReader.ReadSingle();
                        float zPos = messageReader.ReadSingle();

                        float xRot = messageReader.ReadSingle();
                        float yRot = messageReader.ReadSingle();
                        float zRot = messageReader.ReadSingle();

                        if (isPlayerObject)
                        {
                            netManager.connectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                            SpawnManager.SpawnPlayerObject(ownerId, networkId, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot));
                        }
                        else
                        {
                            GameObject go = SpawnManager.SpawnPrefabIndexClient(prefabId, networkId, ownerId,
                                                new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot));
                            go.GetComponent<NetworkedObject>().sceneObject = sceneObject;
                        }
                    }
                    else
                    {
                        uint ownerId = messageReader.ReadUInt32();
                        netManager.connectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                    }
                }
            }
        }

        internal static void HandleClientDisconnect(uint clientId, byte[] incommingData, int channelId)
        {
            using (MemoryStream messageReadStream = new MemoryStream(incommingData))
            {
                using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                {
                    uint disconnectedClientId = messageReader.ReadUInt32();
                    netManager.OnClientDisconnect(disconnectedClientId);
                }
            }
        }

        internal static void HandleDestroyObject(uint clientId, byte[] incommingData, int channelId)
        {
            using (MemoryStream messageReadStream = new MemoryStream(incommingData))
            {
                using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                {
                    uint netId = messageReader.ReadUInt32();
                    SpawnManager.OnDestroyObject(netId, true);
                }
            }
        }

        internal static void HandleSwitchScene(uint clientId, byte[] incommingData, int channelId)
        {
            using (MemoryStream messageReadStream = new MemoryStream(incommingData))
            {
                using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                {
                    NetworkSceneManager.OnSceneSwitch(messageReader.ReadUInt32());
                }
            }
        }

        internal static void HandleSpawnPoolObject(uint clientId, byte[] incommingData, int channelId)
        {
            using (MemoryStream messageReadStream = new MemoryStream(incommingData))
            {
                using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                {
                    uint netId = messageReader.ReadUInt32();

                    float xPos = messageReader.ReadSingle();
                    float yPos = messageReader.ReadSingle();
                    float zPos = messageReader.ReadSingle();

                    float xRot = messageReader.ReadSingle();
                    float yRot = messageReader.ReadSingle();
                    float zRot = messageReader.ReadSingle();

                    SpawnManager.spawnedObjects[netId].transform.position = new Vector3(xPos, yPos, zPos);
                    SpawnManager.spawnedObjects[netId].transform.rotation = Quaternion.Euler(xRot, yRot, zRot);
                    SpawnManager.spawnedObjects[netId].gameObject.SetActive(true);
                }
            }
        }

        internal static void HandleDestroyPoolObject(uint clientId, byte[] incommingData, int channelId)
        {
            using (MemoryStream messageReadStream = new MemoryStream(incommingData))
            {
                using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                {
                    uint netId = messageReader.ReadUInt32();
                    SpawnManager.spawnedObjects[netId].gameObject.SetActive(false);
                }
            }
        }

        internal static void HandleChangeOwner(uint clientId, byte[] incommingData, int channelId)
        {
            using (MemoryStream messageReadStream = new MemoryStream(incommingData))
            {
                using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                {
                    uint netId = messageReader.ReadUInt32();
                    uint ownerClientId = messageReader.ReadUInt32();
                    if (SpawnManager.spawnedObjects[netId].OwnerClientId == netManager.MyClientId)
                    {
                        //We are current owner.
                        SpawnManager.spawnedObjects[netId].InvokeBehaviourOnLostOwnership();
                    }
                    if (ownerClientId == netManager.MyClientId)
                    {
                        //We are new owner.
                        SpawnManager.spawnedObjects[netId].InvokeBehaviourOnGainedOwnership();
                    }
                    SpawnManager.spawnedObjects[netId].ownerClientId = ownerClientId;
                }
            }
        }

        internal static void HandleSyncVarUpdate(uint clientId, byte[] incommingData, int channelId)
        {
            using (MemoryStream messageReadStream = new MemoryStream(incommingData))
            {
                using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                {
                    byte dirtyCount = messageReader.ReadByte();
                    uint netId = messageReader.ReadUInt32();
                    ushort orderIndex = messageReader.ReadUInt16();
                    if (dirtyCount > 0)
                    {
                        for (int i = 0; i < dirtyCount; i++)
                        {
                            byte fieldIndex = messageReader.ReadByte();
                            if (!SpawnManager.spawnedObjects.ContainsKey(netId))
                            {
                                Debug.LogWarning("MLAPI: Sync message recieved for a non existant object with id: " + netId);
                                return;
                            }
                            else if (SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex) == null)
                            {
                                Debug.LogWarning("MLAPI: Sync message recieved for a non existant behaviour");
                                return;
                            }
                            else if (fieldIndex > (SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).syncedVarFields.Count - 1))
                            {
                                Debug.LogWarning("MLAPI: Sync message recieved for field out of bounds");
                                return;
                            }
                            FieldType type = SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).syncedVarFields[fieldIndex].FieldType;
                            switch (type)
                            {
                                case FieldType.Bool:
                                    SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadBoolean(), fieldIndex);
                                    break;
                                case FieldType.Byte:
                                    SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadByte(), fieldIndex);
                                    break;
                                case FieldType.Char:
                                    SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadChar(), fieldIndex);
                                    break;
                                case FieldType.Double:
                                    SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadDouble(), fieldIndex);
                                    break;
                                case FieldType.Single:
                                    SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadSingle(), fieldIndex);
                                    break;
                                case FieldType.Int:
                                    SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadInt32(), fieldIndex);
                                    break;
                                case FieldType.Long:
                                    SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadInt64(), fieldIndex);
                                    break;
                                case FieldType.SByte:
                                    SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadSByte(), fieldIndex);
                                    break;
                                case FieldType.Short:
                                    SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadInt16(), fieldIndex);
                                    break;
                                case FieldType.UInt:
                                    SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadUInt32(), fieldIndex);
                                    break;
                                case FieldType.ULong:
                                    SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadUInt64(), fieldIndex);
                                    break;
                                case FieldType.UShort:
                                    SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadUInt16(), fieldIndex);
                                    break;
                                case FieldType.String:
                                    SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(messageReader.ReadString(), fieldIndex);
                                    break;
                                case FieldType.Vector3:
                                    {   //Cases aren't their own scope. Therefor we create a scope for them as they share the X,Y,Z local variables otherwise.
                                        float x = messageReader.ReadSingle();
                                        float y = messageReader.ReadSingle();
                                        float z = messageReader.ReadSingle();
                                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(new Vector3(x, y, z), fieldIndex);
                                    }
                                    break;
                                case FieldType.Vector2:
                                    {
                                        float x = messageReader.ReadSingle();
                                        float y = messageReader.ReadSingle();
                                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(new Vector2(x, y), fieldIndex);
                                    }
                                    break;
                                case FieldType.Quaternion:
                                    {
                                        float x = messageReader.ReadSingle();
                                        float y = messageReader.ReadSingle();
                                        float z = messageReader.ReadSingle();
                                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(Quaternion.Euler(x, y, z), fieldIndex);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        internal static void HandleAddObjects(uint clientId, byte[] incommingData, int channelId)
        {
            using (MemoryStream messageReadStream = new MemoryStream(incommingData))
            {
                using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                {
                    if (netManager.NetworkConfig.HandleObjectSpawning)
                    {
                        ushort objectCount = messageReader.ReadUInt16();
                        for (int i = 0; i < objectCount; i++)
                        {
                            bool isPlayerObject = messageReader.ReadBoolean();
                            uint networkId = messageReader.ReadUInt32();
                            uint ownerId = messageReader.ReadUInt32();
                            int prefabId = messageReader.ReadInt32();
                            bool sceneObject = messageReader.ReadBoolean();

                            float xPos = messageReader.ReadSingle();
                            float yPos = messageReader.ReadSingle();
                            float zPos = messageReader.ReadSingle();

                            float xRot = messageReader.ReadSingle();
                            float yRot = messageReader.ReadSingle();
                            float zRot = messageReader.ReadSingle();

                            if (isPlayerObject)
                            {
                                netManager.connectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                                SpawnManager.SpawnPlayerObject(ownerId, networkId, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot));
                            }
                            else
                            {
                                GameObject go = SpawnManager.SpawnPrefabIndexClient(prefabId, networkId, ownerId,
                                                    new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot));
                                go.GetComponent<NetworkedObject>().sceneObject = sceneObject;
                            }
                        }
                    }
                }
            }
        }

        internal static void HandleTimeSync(uint clientId, byte[] incommingData, int channelId)
        {
            using (MemoryStream messageReadStream = new MemoryStream(incommingData))
            {
                using (BinaryReader messageReader = new BinaryReader(messageReadStream))
                {
                    float netTime = messageReader.ReadSingle();
                    int timestamp = messageReader.ReadInt32();

                    NetId netId = new NetId(clientId);
                    byte error;
                    int msDelay = NetworkTransport.GetRemoteDelayTimeMS(netId.HostId, netId.ConnectionId, timestamp, out error);
                    if ((NetworkError)error != NetworkError.Ok)
                        msDelay = 0;
                    netManager.networkTime = netTime + (msDelay / 1000f);
                }
            }
        }
    }
}
