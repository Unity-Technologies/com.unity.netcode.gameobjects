using MLAPI.Data;
using MLAPI.MonoBehaviours.Core;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents.Core
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
            netObject.ownerClientId = new NetId(0, 0, false, true).GetClientId();
            using (MemoryStream stream = new MemoryStream(8))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(netId);
                    writer.Write(netObject.ownerClientId);
                }
                InternalMessageHandler.Send("MLAPI_CHANGE_OWNER", "MLAPI_INTERNAL", stream.GetBuffer());
            }
        }

        internal static void ChangeOwnership(uint netId, uint clientId)
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
                InternalMessageHandler.Send("MLAPI_CHANGE_OWNER", "MLAPI_INTERNAL", stream.GetBuffer());
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
                    netObjects[i].sceneObject = true;
            }
        }

        internal static void FlushSceneObjects()
        {
            if (!NetworkingManager.singleton.isServer)
                return;

            List<NetworkedObject> sceneObjectsToSync = new List<NetworkedObject>();
            foreach (KeyValuePair<uint, NetworkedObject> pair in SpawnManager.spawnedObjects)
            {
                if (pair.Value.sceneObject == null || pair.Value.sceneObject == true)
                    sceneObjectsToSync.Add(pair.Value);
            }
            int sizeOfStream = 2 + (38 * sceneObjectsToSync.Count); //The two is the base size, it's a ushort containing the amount of objects. Each object takes 38 bytes
            using (MemoryStream stream = new MemoryStream(sizeOfStream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((ushort)sceneObjectsToSync.Count);
                    for (int i = 0; i < sceneObjectsToSync.Count; i++)
                    {
                        writer.Write(false); //isLocalPlayer
                        writer.Write(sceneObjectsToSync[i].NetworkId);
                        writer.Write(sceneObjectsToSync[i].OwnerClientId);
                        writer.Write(NetworkingManager.singleton.NetworkConfig.NetworkPrefabIds[sceneObjectsToSync[i].NetworkedPrefabName]);

                        writer.Write(sceneObjectsToSync[i].transform.position.x);
                        writer.Write(sceneObjectsToSync[i].transform.position.y);
                        writer.Write(sceneObjectsToSync[i].transform.position.z);

                        writer.Write(sceneObjectsToSync[i].transform.rotation.eulerAngles.x);
                        writer.Write(sceneObjectsToSync[i].transform.rotation.eulerAngles.y);
                        writer.Write(sceneObjectsToSync[i].transform.rotation.eulerAngles.z);
                    }
                }
                InternalMessageHandler.Send("MLAPI_ADD_OBJECTS", "MLAPI_INTERNAL", stream.GetBuffer());
            }
        }

        internal static GameObject SpawnPrefabIndexClient(int networkedPrefabId, uint networkId, uint owner, Vector3 position, Quaternion rotation)
        {
            if (!netManager.NetworkConfig.NetworkPrefabNames.ContainsKey(networkedPrefabId))
            {
                Debug.LogWarning("MLAPI: Cannot spawn the object, invalid prefabIndex");
                return null;
            }

            GameObject go = MonoBehaviour.Instantiate(netManager.NetworkConfig.NetworkedPrefabs[networkedPrefabId].prefab);
            NetworkedObject netObject = go.GetComponent<NetworkedObject>();
            if (netObject == null)
            {
                Debug.LogWarning("MLAPI: Please add a NetworkedObject component to the root of all spawnable objects");
                netObject = go.AddComponent<NetworkedObject>();
            }
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
                Debug.LogWarning("MLAPI: Object already spawned");
                return;
            }
            else if (!netManager.isServer)
            {
                Debug.LogWarning("MLAPI: Only server can spawn objects");
                return;
            }
            else if (!netManager.NetworkConfig.NetworkPrefabIds.ContainsKey(netObject.NetworkedPrefabName))
            {
                Debug.LogWarning("MLAPI: The prefab name " + netObject.NetworkedPrefabName + " does not exist as a networkedPrefab");
                return;
            }
            else if (!netManager.NetworkConfig.HandleObjectSpawning)
            {
                Debug.LogWarning("MLAPI: NetworkConfig is set to not handle object spawning");
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
            using (MemoryStream stream = new MemoryStream(37))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(false);
                    writer.Write(netObject.NetworkId);
                    writer.Write(netObject.OwnerClientId);
                    writer.Write(netManager.NetworkConfig.NetworkPrefabIds[netObject.NetworkedPrefabName]);

                    writer.Write(netObject.transform.position.x);
                    writer.Write(netObject.transform.position.y);
                    writer.Write(netObject.transform.position.z);

                    writer.Write(netObject.transform.rotation.eulerAngles.x);
                    writer.Write(netObject.transform.rotation.eulerAngles.y);
                    writer.Write(netObject.transform.rotation.eulerAngles.z);
                }

                InternalMessageHandler.Send("MLAPI_ADD_OBJECT", "MLAPI_INTERNAL", stream.GetBuffer());
            }
        }

        internal static GameObject SpawnPlayerObject(uint clientId, uint networkId, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrEmpty(netManager.NetworkConfig.PlayerPrefabName) || !netManager.NetworkConfig.NetworkPrefabIds.ContainsKey(netManager.NetworkConfig.PlayerPrefabName))
            {
                Debug.LogWarning("MLAPI: There is no player prefab in the NetworkConfig, or it's not registered at as a spawnable prefab");
                return null;
            }
            GameObject go = MonoBehaviour.Instantiate(netManager.NetworkConfig.NetworkedPrefabs[netManager.NetworkConfig.NetworkPrefabIds[netManager.NetworkConfig.PlayerPrefabName]].prefab, position, rotation);
            NetworkedObject netObject = go.GetComponent<NetworkedObject>();
            if (netObject == null)
            {
                Debug.LogWarning("MLAPI: Please add a NetworkedObject component to the root of the player prefab");
                netObject = go.AddComponent<NetworkedObject>();
            }

            if (NetworkingManager.singleton.isServer)
                netObject.networkId = GetNetworkObjectId();
            else
                netObject.networkId = networkId;

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
            if (!new NetId(spawnedObjects[networkId].OwnerClientId).IsInvalid() && !spawnedObjects[networkId].isPlayerObject)
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
                    using (MemoryStream stream = new MemoryStream(4))
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {
                            writer.Write(networkId);
                        }
                        InternalMessageHandler.Send("MLAPI_DESTROY_OBJECT", "MLAPI_INTERNAL", stream.GetBuffer());
                    }
                }
            }
            if (destroyGameObject && go != null)
                MonoBehaviour.Destroy(go);
            spawnedObjects.Remove(networkId);
        }
    }
}
