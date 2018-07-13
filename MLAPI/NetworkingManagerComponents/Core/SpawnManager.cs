using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents.Core
{
    /// <summary>
    /// Class that handles object spawning
    /// </summary>
    public static class SpawnManager
    {
        /// <summary>
        /// The currently spawned objects
        /// </summary>
        public static readonly Dictionary<uint, NetworkedObject> SpawnedObjects = new Dictionary<uint, NetworkedObject>();
        /// <summary>
        /// A list of the spawned objects
        /// </summary>
        public static readonly List<NetworkedObject> SpawnedObjectsList = new List<NetworkedObject>();

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

        private static NetworkingManager netManager => NetworkingManager.singleton;

        /// <summary>
        /// Returns the local player object or null if one does not exist
        /// </summary>
        /// <returns>The local player object or null if one does not exist</returns>
        public static NetworkedObject GetLocalPlayerObject()
        {
            if (!NetworkingManager.singleton.ConnectedClients.ContainsKey(NetworkingManager.singleton.LocalClientId)) return null;
            return NetworkingManager.singleton.ConnectedClients[NetworkingManager.singleton.LocalClientId].PlayerObject;
        }

        /// <summary>
        /// Returns the player object with a given clientId or null if one does not exist
        /// </summary>
        /// <returns>The player object with a given clientId or null if one does not exist</returns>
        public static NetworkedObject GetPlayerObject(uint clientId)
        {
            if (!NetworkingManager.singleton.ConnectedClients.ContainsKey(clientId)) return null;
            return NetworkingManager.singleton.ConnectedClients[clientId].PlayerObject;
        }

        internal static void RemoveOwnership(uint netId)
        {
            if (!netManager.isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("You can only remove ownership from Server");
                return;
            }
            NetworkedObject netObject = SpawnManager.SpawnedObjects[netId];
            NetworkingManager.singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAll(x => x.NetworkId == netId);
            netObject.OwnerClientId = NetworkingManager.singleton.NetworkConfig.NetworkTransport.InvalidDummyId;

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteUInt(netId);
                writer.WriteUInt(netObject.OwnerClientId);

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
            NetworkedObject netObject = SpawnManager.SpawnedObjects[netId];
            NetworkingManager.singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAll(x => x.NetworkId == netId);
            NetworkingManager.singleton.ConnectedClients[clientId].OwnedObjects.Add(netObject);
            netObject.OwnerClientId = clientId;

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                writer.WriteUInt(netId);
                writer.WriteUInt(clientId);

                InternalMessageHandler.Send("MLAPI_CHANGE_OWNER", "MLAPI_INTERNAL", writer, null);
            }
        }

        internal static void DestroyNonSceneObjects()
        {
            if (SpawnedObjects != null)
            {
                foreach (KeyValuePair<uint, NetworkedObject> netObject in SpawnedObjects)
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
                    netObjects[i].InvokeBehaviourNetworkSpawn(null);
                    netObjects[i].sceneObject = true;
                }
            }
        }
        /*
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
        */

        internal static NetworkedObject CreateSpawnedObject(int networkedPrefabId, uint networkId, uint owner, bool playerObject, Vector3 position, Quaternion rotation, BitReaderDeprecated reader, bool readPayload, bool readNetworkedVar)
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

            if (readNetworkedVar) netObject.SetNetworkedVarData(reader);

            netObject.NetworkedPrefabName = netManager.NetworkConfig.NetworkPrefabNames[networkedPrefabId];
            netObject.isSpawned = true;
            netObject.isPooledObject = false;

            if (netManager.isServer) netObject.NetworkId = GetNetworkObjectId();
            else netObject.NetworkId = networkId;

            netObject.sceneObject = false;
            netObject.OwnerClientId = owner;
            netObject.isPlayerObject = playerObject;
            netObject.transform.position = position;
            netObject.transform.rotation = rotation;
            SpawnedObjects.Add(netObject.NetworkId, netObject);
            SpawnedObjectsList.Add(netObject);
            if (playerObject) NetworkingManager.singleton.ConnectedClients[owner].PlayerObject = netObject;
            netObject.InvokeBehaviourNetworkSpawn(readPayload ? reader : null);
            return netObject;
        }

        internal static void UnSpawnObject(NetworkedObject netObject)
        {
            if (!netObject.isSpawned)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot unspawn objects that are not spawned");
                return;
            }
            else if (!netManager.isServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only server can unspawn objects");
                return;
            }
            else if (!netManager.NetworkConfig.HandleObjectSpawning)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("NetworkConfig is set to not handle object spawning");
                return;
            }

            OnDestroyObject(netObject.NetworkId, false);
        }

        //Server only
        internal static void SpawnPlayerObject(NetworkedObject netObject, uint clientId, BitWriterDeprecated payload = null)
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
            else if (netManager.ConnectedClients[clientId].PlayerObject != null)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Client already have a player object");
                return;
            }
            uint netId = GetNetworkObjectId();
            netObject.NetworkId = netId;
            SpawnedObjects.Add(netId, netObject);
            SpawnedObjectsList.Add(netObject);
            netObject.isSpawned = true;
            netObject.sceneObject = false;
            netObject.isPlayerObject = true;
            netManager.ConnectedClients[clientId].PlayerObject = netObject;

            if (payload == null) netObject.InvokeBehaviourNetworkSpawn(null);
            else using (BitReaderDeprecated payloadReader = BitReaderDeprecated.Get(payload.Finalize())) netObject.InvokeBehaviourNetworkSpawn(payloadReader);

            foreach (var client in netManager.ConnectedClients)
            {
                using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
                {
                    writer.WriteBool(true);
                    writer.WriteUInt(netObject.NetworkId);
                    writer.WriteUInt(netObject.OwnerClientId);
                    writer.WriteInt(netManager.NetworkConfig.NetworkPrefabIds[netObject.NetworkedPrefabName]);
                    writer.WriteBool(netObject.sceneObject == null ? true : netObject.sceneObject.Value);

                    writer.WriteFloat(netObject.transform.position.x);
                    writer.WriteFloat(netObject.transform.position.y);
                    writer.WriteFloat(netObject.transform.position.z);

                    writer.WriteFloat(netObject.transform.rotation.eulerAngles.x);
                    writer.WriteFloat(netObject.transform.rotation.eulerAngles.y);
                    writer.WriteFloat(netObject.transform.rotation.eulerAngles.z);

                    writer.WriteBool(payload != null);

                    netObject.WriteNetworkedVarData(writer, client.Key);

                    if (payload != null) writer.WriteWriter(payload);

                    InternalMessageHandler.Send(client.Key, "MLAPI_ADD_OBJECT", "MLAPI_INTERNAL", writer, null);
                }
            }
        }

        internal static void SpawnObject(NetworkedObject netObject, uint? clientOwnerId = null, BitWriterDeprecated payload = null)
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
            netObject.NetworkId = netId;
            SpawnedObjects.Add(netId, netObject);
            SpawnedObjectsList.Add(netObject);
            netObject.isSpawned = true;
            netObject.sceneObject = false;

            if (clientOwnerId != null)
            {
                netObject.OwnerClientId = clientOwnerId.Value;
                NetworkingManager.singleton.ConnectedClients[clientOwnerId.Value].OwnedObjects.Add(netObject);
            }

            if (payload == null) netObject.InvokeBehaviourNetworkSpawn(null);
            else using (BitReaderDeprecated payloadReader = BitReaderDeprecated.Get(payload.Finalize())) netObject.InvokeBehaviourNetworkSpawn(payloadReader);    

            foreach (var client in netManager.ConnectedClients)
            {
                using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
                {
                    writer.WriteBool(false);
                    writer.WriteUInt(netObject.NetworkId);
                    writer.WriteUInt(netObject.OwnerClientId);
                    writer.WriteInt(netManager.NetworkConfig.NetworkPrefabIds[netObject.NetworkedPrefabName]);
                    writer.WriteBool(netObject.sceneObject == null ? true : netObject.sceneObject.Value);

                    writer.WriteFloat(netObject.transform.position.x);
                    writer.WriteFloat(netObject.transform.position.y);
                    writer.WriteFloat(netObject.transform.position.z);

                    writer.WriteFloat(netObject.transform.rotation.eulerAngles.x);
                    writer.WriteFloat(netObject.transform.rotation.eulerAngles.y);
                    writer.WriteFloat(netObject.transform.rotation.eulerAngles.z);

                    writer.WriteBool(payload != null);

                    netObject.WriteNetworkedVarData(writer, client.Key);

                    if (payload != null) writer.WriteWriter(payload);

                    InternalMessageHandler.Send(client.Key, "MLAPI_ADD_OBJECT", "MLAPI_INTERNAL", writer, null);
                }
            }
        }

        internal static void OnDestroyObject(uint networkId, bool destroyGameObject)
        {
            if (!SpawnedObjects.ContainsKey(networkId) || (netManager != null && !netManager.NetworkConfig.HandleObjectSpawning))
                return;
            if (SpawnedObjects[networkId].OwnerClientId != NetworkingManager.singleton.NetworkConfig.NetworkTransport.InvalidDummyId && 
                !SpawnedObjects[networkId].isPlayerObject && netManager.ConnectedClients.ContainsKey(SpawnedObjects[networkId].OwnerClientId))
            {
                //Someone owns it.
                netManager.ConnectedClients[SpawnedObjects[networkId].OwnerClientId].OwnedObjects.RemoveAll(x => x.NetworkId == networkId);
            }
            SpawnedObjects[networkId].isSpawned = false;

            if (netManager != null && netManager.isServer)
            {
                releasedNetworkObjectIds.Push(networkId);
                if (SpawnedObjects[networkId] != null)
                {
                    using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
                    {
                        writer.WriteUInt(networkId);

                        InternalMessageHandler.Send("MLAPI_DESTROY_OBJECT", "MLAPI_INTERNAL", writer, null);
                    }
                }
            }

            GameObject go = SpawnedObjects[networkId].gameObject;
            if (destroyGameObject && go != null)
                MonoBehaviour.Destroy(go);
            SpawnedObjects.Remove(networkId);
            SpawnedObjectsList.RemoveAll(x => x.NetworkId == networkId);
        }
    }
}
