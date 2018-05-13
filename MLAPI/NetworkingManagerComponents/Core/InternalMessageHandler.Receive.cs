using System.Reflection;
using System.Security.Cryptography;
using MLAPI.Data;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents.Core
{
    internal static partial class InternalMessageHandler
    {
        internal static void HandleConnectionRequest(uint clientId, BitReader reader, int channelId)
        {
            byte[] configHash = reader.ReadByteArray(20);
            if (!netManager.NetworkConfig.CompareConfig(configHash))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkConfiguration missmatch. The configuration between the server and client does not match");
                netManager.DisconnectClient(clientId);
                return;
            }

            if (netManager.NetworkConfig.EnableEncryption)
            {
                byte[] diffiePublic = reader.ReadByteArray();
                netManager.diffieHellmanPublicKeys.Add(clientId, diffiePublic);

            }
            if (netManager.NetworkConfig.ConnectionApproval)
            {
                byte[] connectionBuffer = reader.ReadByteArray();
                netManager.ConnectionApprovalCallback(connectionBuffer, clientId, netManager.HandleApproval);
            }
            else
            {
                netManager.HandleApproval(clientId, true, Vector3.zero, Quaternion.identity);
            }
        }

        internal static void HandleConnectionApproved(uint clientId, BitReader reader, int channelId)
        {
            netManager.myClientId = reader.ReadUInt();
            uint sceneIndex = 0;
            if (netManager.NetworkConfig.EnableSceneSwitching)
                sceneIndex = reader.ReadUInt();

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

            float netTime = reader.ReadFloat();
            int remoteStamp = reader.ReadInt();
            byte error;
            int msDelay = NetworkingManager.singleton.NetworkConfig.NetworkTransport.GetRemoteDelayTimeMS(clientId, remoteStamp, out error);
            netManager.networkTime = netTime + (msDelay / 1000f);

            netManager.connectedClients.Add(netManager.MyClientId, new NetworkedClient() { ClientId = netManager.MyClientId });
            int clientCount = reader.ReadInt();
            for (int i = 0; i < clientCount; i++)
            {
                uint _clientId = reader.ReadUInt();
                netManager.connectedClients.Add(_clientId, new NetworkedClient() { ClientId = _clientId });
                netManager.connectedClientsList.Add(netManager.connectedClients[_clientId]);
            }
            if (netManager.NetworkConfig.HandleObjectSpawning)
            {
                SpawnManager.DestroySceneObjects();
                int objectCount = reader.ReadInt();
                for (int i = 0; i < objectCount; i++)
                {
                    bool isPlayerObject = reader.ReadBool();
                    uint networkId = reader.ReadUInt();
                    uint ownerId = reader.ReadUInt();
                    int prefabId = reader.ReadInt();
                    bool isActive = reader.ReadBool();
                    bool sceneObject = reader.ReadBool();
                    bool visible = reader.ReadBool();

                    float xPos = reader.ReadFloat();
                    float yPos = reader.ReadFloat();
                    float zPos = reader.ReadFloat();

                    float xRot = reader.ReadFloat();
                    float yRot = reader.ReadFloat();
                    float zRot = reader.ReadFloat();

                    if (isPlayerObject)
                    {
                        GameObject go = SpawnManager.SpawnPlayerObject(ownerId, networkId, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), reader);
                        go.GetComponent<NetworkedObject>().SetLocalVisibility(visible);
                    }
                    else
                    {
                        GameObject go = SpawnManager.SpawnPrefabIndexClient(prefabId, networkId, ownerId,
                            new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), reader);

                        go.GetComponent<NetworkedObject>().SetLocalVisibility(visible);
                        go.GetComponent<NetworkedObject>().sceneObject = sceneObject;
                        go.SetActive(isActive);
                    }
                }
            }

            if (netManager.NetworkConfig.EnableSceneSwitching)
            {
                NetworkSceneManager.OnSceneSwitch(sceneIndex);
            }

            netManager._isClientConnected = true;
            if (netManager.OnClientConnectedCallback != null)
                netManager.OnClientConnectedCallback.Invoke(netManager.MyClientId);
        }

        internal static void HandleAddObject(uint clientId, BitReader reader, int channelId)
        {
            if (netManager.NetworkConfig.HandleObjectSpawning)
            {
                bool isPlayerObject = reader.ReadBool();
                uint networkId = reader.ReadUInt();
                uint ownerId = reader.ReadUInt();
                int prefabId = reader.ReadInt();
                bool sceneObject = reader.ReadBool();
                bool visible = reader.ReadBool();

                float xPos = reader.ReadFloat();
                float yPos = reader.ReadFloat();
                float zPos = reader.ReadFloat();

                float xRot = reader.ReadFloat();
                float yRot = reader.ReadFloat();
                float zRot = reader.ReadFloat();

                if (isPlayerObject)
                {
                    netManager.connectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                    netManager.connectedClientsList.Add(netManager.connectedClients[ownerId]);
                    GameObject go = SpawnManager.SpawnPlayerObject(ownerId, networkId, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), visible ? reader : null);
                    go.GetComponent<NetworkedObject>().SetLocalVisibility(visible);
                }
                else
                {
                    GameObject go = SpawnManager.SpawnPrefabIndexClient(prefabId, networkId, ownerId,
                                        new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), visible ? reader : null);

                    go.GetComponent<NetworkedObject>().SetLocalVisibility(visible);
                    go.GetComponent<NetworkedObject>().sceneObject = sceneObject;
                }
            }
            else
            {
                uint ownerId = reader.ReadUInt();
                netManager.connectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
            }
        }

        internal static void HandleClientDisconnect(uint clientId, BitReader reader, int channelId)
        {
            uint disconnectedClientId = reader.ReadUInt();
            netManager.OnClientDisconnect(disconnectedClientId);
        }

        internal static void HandleDestroyObject(uint clientId, BitReader reader, int channelId)
        {
            uint netId = reader.ReadUInt();
            SpawnManager.OnDestroyObject(netId, true);
        }

        internal static void HandleSwitchScene(uint clientId, BitReader reader, int channelId)
        {
            NetworkSceneManager.OnSceneSwitch(reader.ReadUInt());
        }

        internal static void HandleSpawnPoolObject(uint clientId, BitReader reader, int channelId)
        {
            uint netId = reader.ReadUInt();

            float xPos = reader.ReadFloat();
            float yPos = reader.ReadFloat();
            float zPos = reader.ReadFloat();

            float xRot = reader.ReadFloat();
            float yRot = reader.ReadFloat();
            float zRot = reader.ReadFloat();

            SpawnManager.spawnedObjects[netId].transform.position = new Vector3(xPos, yPos, zPos);
            SpawnManager.spawnedObjects[netId].transform.rotation = Quaternion.Euler(xRot, yRot, zRot);
            SpawnManager.spawnedObjects[netId].gameObject.SetActive(true);
        }

        internal static void HandleDestroyPoolObject(uint clientId, BitReader reader, int channelId)
        {
            uint netId = reader.ReadUInt();
            SpawnManager.spawnedObjects[netId].gameObject.SetActive(false);
        }

        internal static void HandleChangeOwner(uint clientId, BitReader reader, int channelId)
        {
            uint netId = reader.ReadUInt();
            uint ownerClientId = reader.ReadUInt();
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

        internal static void HandleSyncVarUpdate(uint clientId, BitReader reader, int channelId)
        {
            uint netId = reader.ReadUInt();
            ushort orderIndex = reader.ReadUShort();

            if (!SpawnManager.spawnedObjects.ContainsKey(netId))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sync message recieved for a non existant object with id: " + netId);
                return;
            }
            else if (SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex) == null)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sync message recieved for a non existant behaviour");
                return;
            }

            for (int i = 0; i < SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).syncedVarFields.Count; i++)
            {
                if (!reader.ReadBool())
                    continue;
                
                FieldType type = SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).syncedVarFields[i].FieldType;
                switch (type)
                {
                    case FieldType.Bool:
                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(reader.ReadBool(), i);
                        break;
                    case FieldType.Byte:
                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(reader.ReadByte(), i);
                        break;
                    case FieldType.Double:
                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(reader.ReadDouble(), i);
                        break;
                    case FieldType.Single:
                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(reader.ReadFloat(), i);
                        break;
                    case FieldType.Int:
                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(reader.ReadInt(), i);
                        break;
                    case FieldType.Long:
                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(reader.ReadLong(), i);
                        break;
                    case FieldType.SByte:
                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(reader.ReadSByte(), i);
                        break;
                    case FieldType.Short:
                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(reader.ReadShort(), i);
                        break;
                    case FieldType.UInt:
                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(reader.ReadUInt(), i);
                        break;
                    case FieldType.ULong:
                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(reader.ReadULong(), i);
                        break;
                    case FieldType.UShort:
                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(reader.ReadUShort(), i);
                        break;
                    case FieldType.String:
                        SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(reader.ReadString(), i);
                        break;
                    case FieldType.Vector3:
                        {   //Cases aren't their own scope. Therefor we create a scope for them as they share the X,Y,Z local variables otherwise.
                            float x = reader.ReadFloat();
                            float y = reader.ReadFloat();
                            float z = reader.ReadFloat();
                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(new Vector3(x, y, z), i);
                        }
                        break;
                    case FieldType.Vector2:
                        {
                            float x = reader.ReadFloat();
                            float y = reader.ReadFloat();
                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(new Vector2(x, y), i);
                        }
                        break;
                    case FieldType.Quaternion:
                        {
                            float x = reader.ReadFloat();
                            float y = reader.ReadFloat();
                            float z = reader.ReadFloat();
                            SpawnManager.spawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(Quaternion.Euler(x, y, z), i);
                        }
                        break;
                }
            }
        }

        internal static void HandleAddObjects(uint clientId, BitReader reader, int channelId)
        {
            if (netManager.NetworkConfig.HandleObjectSpawning)
            {
                ushort objectCount = reader.ReadUShort();
                for (int i = 0; i < objectCount; i++)
                {
                    bool isPlayerObject = reader.ReadBool();
                    uint networkId = reader.ReadUInt();
                    uint ownerId = reader.ReadUInt();
                    int prefabId = reader.ReadInt();
                    bool sceneObject = reader.ReadBool();
                    bool visible = reader.ReadBool();

                    float xPos = reader.ReadFloat();
                    float yPos = reader.ReadFloat();
                    float zPos = reader.ReadFloat();

                    float xRot = reader.ReadFloat();
                    float yRot = reader.ReadFloat();
                    float zRot = reader.ReadFloat();

                    if (isPlayerObject)
                    {
                        netManager.connectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                        netManager.connectedClientsList.Add(netManager.connectedClients[ownerId]);
                        GameObject go = SpawnManager.SpawnPlayerObject(ownerId, networkId, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), visible ? reader : null);

                        go.GetComponent<NetworkedObject>().SetLocalVisibility(visible);
                    }
                    else
                    {
                        GameObject go = SpawnManager.SpawnPrefabIndexClient(prefabId, networkId, ownerId,
                                            new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), visible ? reader : null);

                        go.GetComponent<NetworkedObject>().SetLocalVisibility(visible);
                        go.GetComponent<NetworkedObject>().sceneObject = sceneObject;
                    }
                }
            }
        }

        internal static void HandleTimeSync(uint clientId, BitReader reader, int channelId)
        {
            float netTime = reader.ReadFloat();
            int timestamp = reader.ReadInt();

            byte error;
            int msDelay = NetworkingManager.singleton.NetworkConfig.NetworkTransport.GetRemoteDelayTimeMS(clientId, timestamp, out error);
            netManager.networkTime = netTime + (msDelay / 1000f);
        }

        internal static void HandleCommand(uint clientId, BitReader reader, int channelId)
        {
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.Disabled)
                return;

            uint networkId = reader.ReadUInt();
            ushort orderId = reader.ReadUShort();
            ulong hash = reader.ReadULong();
            NetworkedBehaviour behaviour = SpawnManager.spawnedObjects[networkId].GetBehaviourAtOrderIndex(orderId);
            if (clientId != behaviour.ownerClientId)
                return; // Not owner
            MethodInfo targetMethod = null;
            if (behaviour.cachedMethods.ContainsKey(Data.Cache.GetAttributeMethodName(hash)))
                targetMethod = behaviour.cachedMethods[Data.Cache.GetAttributeMethodName(hash)];
            byte paramCount = reader.ReadBits(5);
            object[] methodParams = FieldTypeHelper.ReadObjects(reader, paramCount);
            targetMethod.Invoke(behaviour, methodParams);
        }

        internal static void HandleRpc(uint clientId, BitReader reader, int channelId)
        {
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.Disabled)     
                return;

            uint networkId = reader.ReadUInt();
            ushort orderId = reader.ReadUShort();
            ulong hash = reader.ReadULong();
            NetworkedBehaviour behaviour = SpawnManager.spawnedObjects[networkId].GetBehaviourAtOrderIndex(orderId);
            MethodInfo targetMethod = null;
            if (behaviour.cachedMethods.ContainsKey(Data.Cache.GetAttributeMethodName(hash)))
                targetMethod = behaviour.cachedMethods[Data.Cache.GetAttributeMethodName(hash)];
            byte paramCount = reader.ReadBits(5);
            object[] methodParams = FieldTypeHelper.ReadObjects(reader, paramCount);
            targetMethod.Invoke(behaviour, methodParams);
        }

        internal static void HandleTargetRpc(uint clientId, BitReader reader, int channelId)
        {
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.Disabled)
                return;

            uint networkId = reader.ReadUInt();
            ushort orderId = reader.ReadUShort();
            ulong hash = reader.ReadULong();
            NetworkedBehaviour behaviour = SpawnManager.spawnedObjects[networkId].GetBehaviourAtOrderIndex(orderId);
            MethodInfo targetMethod = null;
            if (behaviour.cachedMethods.ContainsKey(Data.Cache.GetAttributeMethodName(hash)))
                targetMethod = behaviour.cachedMethods[Data.Cache.GetAttributeMethodName(hash)];
            byte paramCount = reader.ReadBits(5);
            object[] methodParams = FieldTypeHelper.ReadObjects(reader, paramCount);
            targetMethod.Invoke(behaviour, methodParams);
        }

        internal static void HandleSetVisibility(uint clientId, BitReader reader, int channelId)
        {
            uint networkId = reader.ReadUInt();
            bool visibility = reader.ReadBool();
            if (visibility)
                SpawnManager.spawnedObjects[networkId].SetFormattedSyncedVarData(reader);
            SpawnManager.spawnedObjects[networkId].SetLocalVisibility(visibility);
        }
    }
}
