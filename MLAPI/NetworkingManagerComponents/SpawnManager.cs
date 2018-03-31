using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents
{
    internal static class SpawnManager
    {
        internal static Dictionary<uint, NetworkedObject> spawnedObjects;
        internal static Stack<uint> releasedNetworkObjectIds;
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
            NetworkedObject netObject = SpawnManager.spawnedObjects[netId];
            NetworkingManager.singleton.connectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAll(x => x.NetworkId == netId);
            netObject.ownerClientId = -2;
            using (MemoryStream stream = new MemoryStream(8))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(netId);
                    writer.Write(-2);
                }
                netManager.Send("MLAPI_CHANGE_OWNER", "MLAPI_INTERNAL", stream.GetBuffer());
            }
        }

        internal static void ChangeOwnership(uint netId, int clientId)
        {
            NetworkedObject netObject = SpawnManager.spawnedObjects[netId];
            NetworkingManager.singleton.connectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAll(x => x.NetworkId == netId);
            NetworkingManager.singleton.connectedClients[clientId].OwnedObjects.Add(netObject);
            netObject.ownerClientId = clientId;
            using (MemoryStream stream = new MemoryStream(8))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(netId);
                    writer.Write(clientId);
                }
                netManager.Send("MLAPI_CHANGE_OWNER", "MLAPI_INTERNAL", stream.GetBuffer());
            }
        }

        internal static GameObject SpawnObject(int spawnablePrefabIndex, uint networkId, int ownerId)
        {
            GameObject go = MonoBehaviour.Instantiate(netManager.SpawnablePrefabs[spawnablePrefabIndex]);
            NetworkedObject netObject = go.GetComponent<NetworkedObject>();
            if (netObject == null)
            {
                Debug.LogWarning("MLAPI: Please add a NetworkedObject component to the root of all spawnable objects");
                netObject = go.AddComponent<NetworkedObject>();
            }
            netObject.spawnablePrefabIndex = spawnablePrefabIndex;
            if (netManager.isServer)
            {
                netObject.networkId = GetNetworkObjectId();
            }
            else
            {
                netObject.networkId = networkId;
            }
            netObject.ownerClientId = ownerId;

            spawnedObjects.Add(netObject.NetworkId, netObject);
            netObject.InvokeBehaviourNetworkSpawn();
            return go;
        }

        internal static GameObject SpawnPlayerObject(int clientId, uint networkId)
        {
            GameObject go = MonoBehaviour.Instantiate(netManager.DefaultPlayerPrefab);
            NetworkedObject netObject = go.GetComponent<NetworkedObject>();
            if (netObject == null)
            {
                Debug.LogWarning("MLAPI: Please add a NetworkedObject component to the root of the player prefab");
                netObject = go.AddComponent<NetworkedObject>();
            }
            netObject.ownerClientId = clientId;
            if (NetworkingManager.singleton.isServer)
            {
                netObject.networkId = GetNetworkObjectId();
            }
            else
            {
                netObject.networkId = networkId;
            }
            netObject._isPlayerObject = true;
            netManager.connectedClients[clientId].PlayerObject = go;
            spawnedObjects.Add(netObject.NetworkId, netObject);
            netObject.InvokeBehaviourNetworkSpawn();
            return go;
        }

        internal static void OnDestroyObject(uint networkId, bool destroyGameObject)
        {
            if (!spawnedObjects.ContainsKey(networkId) || (netManager != null && !netManager.NetworkConfig.HandleObjectSpawning))
                return;
            if (spawnedObjects[networkId].OwnerClientId > -2 && !spawnedObjects[networkId].isPlayerObject)
            {
                //Someone owns it.
                NetworkingManager.singleton.connectedClients[spawnedObjects[networkId].OwnerClientId].OwnedObjects.RemoveAll(x => x.NetworkId == networkId);
            }
            GameObject go = spawnedObjects[networkId].gameObject;
            if (netManager != null && netManager.isServer)
            {
                releasedNetworkObjectIds.Push(networkId);
                if (spawnedObjects[networkId] != null && !spawnedObjects[networkId].ServerOnly)
                {
                    using (MemoryStream stream = new MemoryStream(4))
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {
                            writer.Write(networkId);
                        }
                        //If we are host, send to everyone except ourselves. Otherwise, send to all
                        if (netManager != null && netManager.isHost)
                            netManager.Send("MLAPI_DESTROY_OBJECT", "MLAPI_INTERNAL", stream.GetBuffer(), -1);
                        else
                            netManager.Send("MLAPI_DESTROY_OBJECT", "MLAPI_INTERNAL", stream.GetBuffer());
                    }
                }
            }
            if (destroyGameObject && go != null)
                MonoBehaviour.Destroy(go);
            spawnedObjects.Remove(networkId);
        }

        internal static void OnSpawnObject(NetworkedObject netObject, int? clientOwnerId = null)
        {
            if (netObject.isSpawned)
            {
                Debug.LogWarning("MLAPI: Object already spawned");
                return;
            }
            else if (!netManager.isServer)
            {
                Debug.LogWarning("MLAPI: Only server can spawn objects");
                return;
            }
            else if (netObject.SpawnablePrefabIndex == -1)
            {
                Debug.LogWarning("MLAPI: Invalid prefab index");
                return;
            }
            else if (netObject.ServerOnly)
            {
                Debug.LogWarning("MLAPI: Server only objects does not have to be spawned");
                return;
            }
            else if (netManager.NetworkConfig.HandleObjectSpawning)
            {
                Debug.LogWarning("MLAPI: NetworkingConfiguration is set to not handle object spawning");
                return;
            }
            uint netId = GetNetworkObjectId();
            spawnedObjects.Add(netId, netObject);
            netObject.isSpawned = true;
            if (clientOwnerId != null)
            {
                netObject.ownerClientId = clientOwnerId.Value;
                NetworkingManager.singleton.connectedClients[clientOwnerId.Value].OwnedObjects.Add(netObject);
            }
            using (MemoryStream stream = new MemoryStream(13))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(false);
                    writer.Write(netObject.NetworkId);
                    writer.Write(netObject.OwnerClientId);
                    writer.Write(netObject.SpawnablePrefabIndex);
                }
                //If we are host, send to everyone except ourselves. Otherwise, send to all
                if (netManager.isHost)
                    netManager.Send("MLAPI_ADD_OBJECT", "MLAPI_INTERNAL", stream.GetBuffer(), -1);
                else
                    netManager.Send("MLAPI_ADD_OBJECT", "MLAPI_INTERNAL", stream.GetBuffer());
            }
        }
    }
}
