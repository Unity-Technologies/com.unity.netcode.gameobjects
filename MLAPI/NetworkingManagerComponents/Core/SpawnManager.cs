using MLAPI.Data;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents.Core
{
    internal static class SpawnManager
    {
        internal static readonly Dictionary<uint, NetworkedObject> spawnedObjects = new Dictionary<uint, NetworkedObject>();
        internal static readonly Stack<uint> releasedNetworkObjectIds = new Stack<uint>();
        private static uint networkObjectIdCounter;
        internal static uint GetNetworkObjectId()
        {
            if (releasedNetworkObjectIds.Count > 0)
            {
                return releasedNetworkObjectIds.Pop();
            }
            else
            {
                networkObjectIdCounter++;
                return networkObjectIdCounter;
            }
        }

        private static NetworkingManager netManager
        {
            get
            {
                return NetworkingManager.singleton;
            }
        }

        internal static void RemoveOwnership(uint netId)
        {
            if (!netManager.isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("You can only remove ownership from Server");
                return;
            }
            NetworkedObject netObject = SpawnManager.spawnedObjects[netId];
            NetworkingManager.singleton.connectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAll(x => x.NetworkId == netId);
            netObject.ownerClientId = NetworkingManager.singleton.NetworkConfig.NetworkTransport.InvalidDummyId;

            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteUInt(netId);
                writer.WriteUInt(netObject.ownerClientId);

                InternalMessageHandler.Send("MLAPI_CHANGE_OWNER", "MLAPI_INTERNAL", writer, null);
            }
        }

        internal static void ChangeOwnership(uint netId, uint clientId)
        {
            if (!netManager.isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("You can only change ownership from Server");
                return;
            }
            NetworkedObject netObject = SpawnManager.spawnedObjects[netId];
            NetworkingManager.singleton.connectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAll(x => x.NetworkId == netId);
            NetworkingManager.singleton.connectedClients[clientId].OwnedObjects.Add(netObject);
            netObject.ownerClientId = clientId;

            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteUInt(netId);
                writer.WriteUInt(clientId);

                InternalMessageHandler.Send("MLAPI_CHANGE_OWNER", "MLAPI_INTERNAL", writer, null);
            }
        }
  
        internal static void DestroyNonSceneObjects()
        {
            if(spawnedObjects != null)
            {
                foreach (KeyValuePair<uint, NetworkedObject> netObject in spawnedObjects)
                {
                    if (netObject.Value.sceneObject != null && netObject.Value.sceneObject.Value == false)
                        MonoBehaviour.Destroy(netObject.Value.gameObject);
                }
            }
        }

        internal static void DestroySceneObjects()
        {
            NetworkedObject[] netObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();
            for (int i = 0; i < netObjects.Length; i++)
            {
                if (netObjects[i].sceneObject == null || netObjects[i].sceneObject.Value == true)
                    MonoBehaviour.Destroy(netObjects[i].gameObject);
            }
        }

        internal static void MarkSceneObjects()
        {
            NetworkedObject[] netObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();
            for (int i = 0; i < netObjects.Length; i++)
            {
                if (netObjects[i].sceneObject == null)
                {
                    netObjects[i].InvokeBehaviourNetworkSpawn();
                    netObjects[i].sceneObject = true;
                }
            }
        }

        internal static void FlushSceneObjects()
        {
            if (!NetworkingManager.singleton.isServer)
                return;

            //This loop is bad. For each client, we loop over every object twice.
            foreach (KeyValuePair<uint, NetworkedClient> client in netManager.connectedClients)
            {
                int sceneObjects = 0;
                foreach (var netObject in SpawnManager.spawnedObjects)
                    if (netObject.Value.sceneObject == null || netObject.Value.sceneObject == true)
                        sceneObjects++;

                using (BitWriter writer = BitWriter.Get())
                {
                    writer.WriteUShort((ushort)sceneObjects);
                    foreach (var netObject in SpawnManager.spawnedObjects)
                    {
                        if (netObject.Value.sceneObject == null || netObject.Value.sceneObject == true)
                        {
                            writer.WriteBool(false); //isLocalPlayer
                            writer.WriteUInt(netObject.Value.NetworkId);
                            writer.WriteUInt(netObject.Value.OwnerClientId);
                            writer.WriteInt(NetworkingManager.singleton.NetworkConfig.NetworkPrefabIds[netObject.Value.NetworkedPrefabName]);
                            writer.WriteBool(netObject.Value.sceneObject == null ? true : netObject.Value.sceneObject.Value);
                            writer.WriteBool(netObject.Value.observers.Contains(client.Key));

                            writer.WriteFloat(netObject.Value.transform.position.x);
                            writer.WriteFloat(netObject.Value.transform.position.y);
                            writer.WriteFloat(netObject.Value.transform.position.z);

                            writer.WriteFloat(netObject.Value.transform.rotation.eulerAngles.x);
                            writer.WriteFloat(netObject.Value.transform.rotation.eulerAngles.y);
                            writer.WriteFloat(netObject.Value.transform.rotation.eulerAngles.z);

                            if (netObject.Value.observers.Contains(client.Key))
                                netObject.Value.WriteFormattedSyncedVarData(writer);
                        }
                    }
                    InternalMessageHandler.Send(client.Key, "MLAPI_ADD_OBJECTS", "MLAPI_INTERNAL", writer, null);
                }
            }
        }

        internal static GameObject SpawnPrefabIndexClient(int networkedPrefabId, uint networkId, uint owner, Vector3 position, Quaternion rotation, BitReader reader = null)
        {
            if (!netManager.NetworkConfig.NetworkPrefabNames.ContainsKey(networkedPrefabId))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot spawn the object, invalid prefabIndex");
                return null;
            }

            GameObject go = MonoBehaviour.Instantiate(netManager.NetworkConfig.NetworkedPrefabs[networkedPrefabId].prefab, position, rotation);
            NetworkedObject netObject = go.GetComponent<NetworkedObject>();
            if (netObject == null)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Please add a NetworkedObject component to the root of all spawnable objects");
                netObject = go.AddComponent<NetworkedObject>();
            }

            if (reader != null)
                netObject.SetFormattedSyncedVarData(reader);

            netObject.NetworkedPrefabName = netManager.NetworkConfig.NetworkPrefabNames[networkedPrefabId];
            netObject._isSpawned = true;
            netObject._isPooledObject = false;
            netObject.networkId = networkId;
            netObject.ownerClientId = owner;
            netObject.transform.position = position;
            netObject.transform.rotation = rotation;
            spawnedObjects.Add(netObject.NetworkId, netObject);
            netObject.InvokeBehaviourNetworkSpawn();
            return go;
        }

        internal static void SpawnPrefabIndexServer(NetworkedObject netObject, uint? clientOwnerId = null)
        {
            if (netObject.isSpawned)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Object already spawned");
                return;
            }
            else if (!netManager.isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only server can spawn objects");
                return;
            }
            else if (!netManager.NetworkConfig.NetworkPrefabIds.ContainsKey(netObject.NetworkedPrefabName))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The prefab name " + netObject.NetworkedPrefabName + " does not exist as a networkedPrefab");
                return;
            }
            else if (!netManager.NetworkConfig.HandleObjectSpawning)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkConfig is set to not handle object spawning");
                return;
            }
            uint netId = GetNetworkObjectId();
            netObject.networkId = netId;
            spawnedObjects.Add(netId, netObject);
            netObject._isSpawned = true;
            netObject.sceneObject = false;
            netObject.InvokeBehaviourNetworkSpawn();
            if (clientOwnerId != null)
            {
                netObject.ownerClientId = clientOwnerId.Value;
                NetworkingManager.singleton.connectedClients[clientOwnerId.Value].OwnedObjects.Add(netObject);
            }
            foreach (var client in netManager.connectedClients)
            {
                using (BitWriter writer = BitWriter.Get())
                {
                    writer.WriteBool(false);
                    writer.WriteUInt(netObject.NetworkId);
                    writer.WriteUInt(netObject.OwnerClientId);
                    writer.WriteInt(netManager.NetworkConfig.NetworkPrefabIds[netObject.NetworkedPrefabName]);
                    writer.WriteBool(netObject.sceneObject == null ? true : netObject.sceneObject.Value);
                    writer.WriteBool(netObject.observers.Contains(client.Key));

                    writer.WriteFloat(netObject.transform.position.x);
                    writer.WriteFloat(netObject.transform.position.y);
                    writer.WriteFloat(netObject.transform.position.z);

                    writer.WriteFloat(netObject.transform.rotation.eulerAngles.x);
                    writer.WriteFloat(netObject.transform.rotation.eulerAngles.y);
                    writer.WriteFloat(netObject.transform.rotation.eulerAngles.z);

                    if (netObject.observers.Contains(client.Key))
                        netObject.WriteFormattedSyncedVarData(writer);

                    InternalMessageHandler.Send(client.Key, "MLAPI_ADD_OBJECT", "MLAPI_INTERNAL", writer, null);
                }
            }
        }

        internal static GameObject SpawnPlayerObject(uint clientId, uint networkId, Vector3 position, Quaternion rotation, BitReader reader = null)
        {
            if (string.IsNullOrEmpty(netManager.NetworkConfig.PlayerPrefabName) || !netManager.NetworkConfig.NetworkPrefabIds.ContainsKey(netManager.NetworkConfig.PlayerPrefabName))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("There is no player prefab in the NetworkConfig, or it's not registered at as a spawnable prefab");
                return null;
            }
            GameObject go = MonoBehaviour.Instantiate(netManager.NetworkConfig.NetworkedPrefabs[netManager.NetworkConfig.NetworkPrefabIds[netManager.NetworkConfig.PlayerPrefabName]].prefab, position, rotation);
            NetworkedObject netObject = go.GetComponent<NetworkedObject>();
            if (netObject == null)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Please add a NetworkedObject component to the root of the player prefab");
                netObject = go.AddComponent<NetworkedObject>();
            }

            if (NetworkingManager.singleton.isServer)
                netObject.networkId = GetNetworkObjectId();
            else
                netObject.networkId = networkId;

            if (reader != null)
                netObject.SetFormattedSyncedVarData(reader);

            netObject._isPooledObject = false;
            netObject.ownerClientId = clientId;
            netObject._isPlayerObject = true;
            netObject._isSpawned = true;
            netObject.sceneObject = false;
            netManager.connectedClients[clientId].PlayerObject = go;
            spawnedObjects.Add(netObject.NetworkId, netObject);
            netObject.InvokeBehaviourNetworkSpawn();
            return go;
        }

        internal static void OnDestroyObject(uint networkId, bool destroyGameObject)
        {
            if (!spawnedObjects.ContainsKey(networkId) || (netManager != null && !netManager.NetworkConfig.HandleObjectSpawning))
                return;
            if (spawnedObjects[networkId].OwnerClientId != NetworkingManager.singleton.NetworkConfig.NetworkTransport.InvalidDummyId && !spawnedObjects[networkId].isPlayerObject)
            {
                //Someone owns it.
                NetworkingManager.singleton.connectedClients[spawnedObjects[networkId].OwnerClientId].OwnedObjects.RemoveAll(x => x.NetworkId == networkId);
            }
            GameObject go = spawnedObjects[networkId].gameObject;
            if (netManager != null && netManager.isServer)
            {
                releasedNetworkObjectIds.Push(networkId);
                if (spawnedObjects[networkId] != null)
                {
                    using (BitWriter writer = BitWriter.Get())
                    {
                        writer.WriteUInt(networkId);

                        InternalMessageHandler.Send("MLAPI_DESTROY_OBJECT", "MLAPI_INTERNAL", writer, null);
                    }
                }
            }
            if (destroyGameObject && go != null)
                MonoBehaviour.Destroy(go);
            spawnedObjects.Remove(networkId);
        }
    }
}
