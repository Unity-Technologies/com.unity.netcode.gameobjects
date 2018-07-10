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
            ulong configHash = reader.ReadULong();
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

        internal static void HandleConnectionApproved(uint clientId, BitReader reader, int channelId)
        {
            netManager.LocalClientId = reader.ReadUInt();
            uint sceneIndex = 0;
            if (netManager.NetworkConfig.EnableSceneSwitching)
                sceneIndex = reader.ReadUInt();

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

            float netTime = reader.ReadFloat();
            int remoteStamp = reader.ReadInt();
            int msDelay = NetworkingManager.singleton.NetworkConfig.NetworkTransport.GetRemoteDelayTimeMS(clientId, remoteStamp, out byte error);
            netManager.NetworkTime = netTime + (msDelay / 1000f);

            netManager.ConnectedClients.Add(netManager.LocalClientId, new NetworkedClient() { ClientId = netManager.LocalClientId });
            int clientCount = reader.ReadInt();
            for (int i = 0; i < clientCount; i++)
            {
                uint _clientId = reader.ReadUInt();
                netManager.ConnectedClients.Add(_clientId, new NetworkedClient() { ClientId = _clientId });
                netManager.ConnectedClientsList.Add(netManager.ConnectedClients[_clientId]);
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

                    NetworkedObject netObject = SpawnManager.CreateSpawnedObject(prefabId, networkId, ownerId, isPlayerObject,
                        new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), reader, visible, false);
                    netObject.SetLocalVisibility(visible);
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

                bool hasPayload = reader.ReadBool();

                if (isPlayerObject)
                {
                    netManager.ConnectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                    netManager.ConnectedClientsList.Add(netManager.ConnectedClients[ownerId]);
                }
                NetworkedObject netObject = SpawnManager.CreateSpawnedObject(prefabId, networkId, ownerId, isPlayerObject,
                    new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), reader, visible, hasPayload);

                netObject.SetLocalVisibility(visible);
                netObject.sceneObject = sceneObject;

            }
            else
            {
                uint ownerId = reader.ReadUInt();
                netManager.ConnectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
            }
        }

        internal static void HandleClientDisconnect(uint clientId, BitReader reader, int channelId)
        {
            uint disconnectedClientId = reader.ReadUInt();
            netManager.OnClientDisconnectFromServer(disconnectedClientId);
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

            SpawnManager.SpawnedObjects[netId].transform.position = new Vector3(xPos, yPos, zPos);
            SpawnManager.SpawnedObjects[netId].transform.rotation = Quaternion.Euler(xRot, yRot, zRot);
            SpawnManager.SpawnedObjects[netId].gameObject.SetActive(true);
        }

        internal static void HandleDestroyPoolObject(uint clientId, BitReader reader, int channelId)
        {
            uint netId = reader.ReadUInt();
            SpawnManager.SpawnedObjects[netId].gameObject.SetActive(false);
        }

        internal static void HandleChangeOwner(uint clientId, BitReader reader, int channelId)
        {
            uint netId = reader.ReadUInt();
            uint ownerClientId = reader.ReadUInt();
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

        internal static void HandleSyncVarUpdate(uint clientId, BitReader reader, int channelId)
        {
            uint netId = reader.ReadUInt();
            ushort orderIndex = reader.ReadUShort();

            if (!SpawnManager.SpawnedObjects.ContainsKey(netId))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sync message recieved for a non existant object with id: " + netId);
                return;
            }
            else if (SpawnManager.SpawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex) == null)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Sync message recieved for a non existant behaviour");
                return;
            }

            for (int i = 0; i < SpawnManager.SpawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).syncedVarFields.Count; i++)
            {
                if (!reader.ReadBool())
                    continue;
                SyncedVarField field = SpawnManager.SpawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).syncedVarFields[i];
                SpawnManager.SpawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate(FieldTypeHelper.ReadFieldType(reader,
                    field.FieldInfo.FieldType, field.FieldValue), i);
            }
            SpawnManager.SpawnedObjects[netId].GetBehaviourAtOrderIndex(orderIndex).OnSyncVarUpdate();
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
                        netManager.ConnectedClients.Add(ownerId, new NetworkedClient() { ClientId = ownerId });
                        netManager.ConnectedClientsList.Add(netManager.ConnectedClients[ownerId]);
                    }
                    NetworkedObject netObject = SpawnManager.CreateSpawnedObject(prefabId, networkId, ownerId, isPlayerObject,
                        new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), reader, visible, false);
                    netObject.SetLocalVisibility(visible);
                    netObject.sceneObject = sceneObject;

                }
            }
        }

        internal static void HandleTimeSync(uint clientId, BitReader reader, int channelId)
        {
            float netTime = reader.ReadFloat();
            int timestamp = reader.ReadInt();

            int msDelay = NetworkingManager.singleton.NetworkConfig.NetworkTransport.GetRemoteDelayTimeMS(clientId, timestamp, out byte error);
            netManager.NetworkTime = netTime + (msDelay / 1000f);
        }

        internal static void HandleCommand(uint clientId, BitReader reader, int channelId)
        {
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.Disabled)
                return;

            uint networkId = reader.ReadUInt();
            ushort orderId = reader.ReadUShort();
            ulong hash = reader.ReadULong();
            NetworkedBehaviour behaviour = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(orderId);
            if (clientId != behaviour.OwnerClientId)
                return; // Not owner
            MethodInfo targetMethod = null;
            if (behaviour.cachedMethods.ContainsKey(hash))
                targetMethod = behaviour.cachedMethods[hash];
            else return; //No method
            ParameterInfo[] parameters = targetMethod.GetParameters();
            object[] methodParams = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                methodParams[i] = FieldTypeHelper.ReadFieldType(reader, parameters[i].ParameterType);
            targetMethod.Invoke(behaviour, methodParams);
        }

        internal static void HandleRpc(uint clientId, BitReader reader, int channelId)
        {
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.Disabled)     
                return;

            uint networkId = reader.ReadUInt();
            ushort orderId = reader.ReadUShort();
            ulong hash = reader.ReadULong();
            NetworkedBehaviour behaviour = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(orderId);
            MethodInfo targetMethod = null;
            if (behaviour.cachedMethods.ContainsKey(hash))
                targetMethod = behaviour.cachedMethods[hash];
            else return; //No method
            ParameterInfo[] parameters = targetMethod.GetParameters();
            object[] methodParams = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                methodParams[i] = FieldTypeHelper.ReadFieldType(reader, parameters[i].ParameterType);
            targetMethod.Invoke(behaviour, methodParams);
        }

        internal static void HandleTargetRpc(uint clientId, BitReader reader, int channelId)
        {
            if (NetworkingManager.singleton.NetworkConfig.AttributeMessageMode == AttributeMessageMode.Disabled)
                return;

            uint networkId = reader.ReadUInt();
            ushort orderId = reader.ReadUShort();
            ulong hash = reader.ReadULong();
            NetworkedBehaviour behaviour = SpawnManager.SpawnedObjects[networkId].GetBehaviourAtOrderIndex(orderId);
            MethodInfo targetMethod = null;
            if (behaviour.cachedMethods.ContainsKey(hash))
                targetMethod = behaviour.cachedMethods[hash];
            else return; //No method
            ParameterInfo[] parameters = targetMethod.GetParameters();
            object[] methodParams = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                methodParams[i] = FieldTypeHelper.ReadFieldType(reader, parameters[i].ParameterType);
            targetMethod.Invoke(behaviour, methodParams);
        }

        internal static void HandleSetVisibility(uint clientId, BitReader reader, int channelId)
        {
            uint networkId = reader.ReadUInt();
            bool visibility = reader.ReadBool();
            if (visibility)
                SpawnManager.SpawnedObjects[networkId].SetFormattedSyncedVarData(reader);
            SpawnManager.SpawnedObjects[networkId].SetLocalVisibility(visibility);
        }
    }
}
