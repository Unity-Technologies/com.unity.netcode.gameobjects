using System.Collections.Generic;
using System.IO;
using MLAPI.Data;
using MLAPI.Internal;
using MLAPI.Logging;
using MLAPI.NetworkedVar;
using MLAPI.Serialization;
using UnityEngine;

namespace MLAPI.Components
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

        internal static readonly Dictionary<uint, PendingSpawnObject> PendingSpawnObjects = new Dictionary<uint, PendingSpawnObject>();
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
            for (int i = NetworkingManager.singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.Count - 1; i > -1; i--)
            {
                if (NetworkingManager.singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects[i].NetworkId == netId)
                    NetworkingManager.singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAt(i);
            }
			netObject.OwnerClientId = NetworkingManager.singleton.ServerClientId;

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt32Packed(netId);
                    writer.WriteUInt32Packed(netObject.OwnerClientId);

                    InternalMessageHandler.Send(MLAPIConstants.MLAPI_CHANGE_OWNER, "MLAPI_INTERNAL", stream);
                }
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
            for (int i = NetworkingManager.singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.Count - 1; i > -1; i--)
            {
                if (NetworkingManager.singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects[i].NetworkId == netId)
                    NetworkingManager.singleton.ConnectedClients[netObject.OwnerClientId].OwnedObjects.RemoveAt(i);
            }
            NetworkingManager.singleton.ConnectedClients[clientId].OwnedObjects.Add(netObject);
            netObject.OwnerClientId = clientId;

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt32Packed(netId);
                    writer.WriteUInt32Packed(clientId);

                    InternalMessageHandler.Send(MLAPIConstants.MLAPI_CHANGE_OWNER, "MLAPI_INTERNAL", stream);
                }
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

        internal static NetworkedObject CreateSpawnedObject(int networkedPrefabId, uint networkId, uint owner, bool playerObject, uint sceneSpawnedInIndex, bool OnlySpawnInSceneOriginalySpawnedAt, Vector3 position, Quaternion rotation, bool isActive, Stream stream, bool readPayload, bool readNetworkedVar)
        {
            if (!netManager.NetworkConfig.NetworkPrefabNames.ContainsKey(networkedPrefabId))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot spawn the object, invalid prefabIndex: " + networkedPrefabId);
                return null;
            }

            //Delayed spawning
            if (OnlySpawnInSceneOriginalySpawnedAt && sceneSpawnedInIndex != NetworkSceneManager.CurrentActiveSceneIndex)
            {
                GameObject prefab = netManager.NetworkConfig.NetworkedPrefabs[networkedPrefabId].prefab;
                bool prefabActive = prefab.activeSelf;
                prefab.SetActive(false);
                GameObject go = MonoBehaviour.Instantiate(prefab, position, rotation);
                prefab.SetActive(prefabActive);

                NetworkedObject netObject = go.GetComponent<NetworkedObject>();
                if (netObject == null)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Please add a NetworkedObject component to the root of all spawnable objects");
                    netObject = go.AddComponent<NetworkedObject>();
                }

                netObject.NetworkedPrefabName = netManager.NetworkConfig.NetworkPrefabNames[networkedPrefabId];
                netObject.isSpawned = false;
                netObject.isPooledObject = false;

                if (netManager.isServer) netObject.NetworkId = GetNetworkObjectId();
                else netObject.NetworkId = networkId;

                netObject.sceneObject = false;
                netObject.OwnerClientId = owner;
                netObject.isPlayerObject = playerObject;
                netObject.transform.position = position;
                netObject.transform.rotation = rotation;
                netObject.OnlySpawnInSceneOriginalySpawnedAt = OnlySpawnInSceneOriginalySpawnedAt;
                netObject.sceneSpawnedInIndex = sceneSpawnedInIndex;

                Dictionary<ushort, List<INetworkedVar>> dummyNetworkedVars = new Dictionary<ushort, List<INetworkedVar>>();
                List<NetworkedBehaviour> networkedBehaviours = new List<NetworkedBehaviour>(netObject.GetComponentsInChildren<NetworkedBehaviour>());
                for (ushort i = 0; i < networkedBehaviours.Count; i++)
                {
                    dummyNetworkedVars.Add(i, networkedBehaviours[i].getDummyNetworkedVars());
                }

                PendingSpawnObject pso = new PendingSpawnObject()
                {
                    netObject = netObject,
                    dummyNetworkedVars = dummyNetworkedVars,
                    sceneSpawnedInIndex = sceneSpawnedInIndex,
                    playerObject = playerObject,
                    owner = owner,
                    isActive = isActive
                };
                pso.SetNetworkedVarData(stream);

                if (readPayload)
                {
                    int length = (int)(stream.Length - stream.Position);
                    byte[] payloadData = new byte[length];
                    stream.Read(payloadData, 0, length);
                    pso.payload = payloadData;
                }
                else
                {
                    pso.payload = new byte[0];
                }

                PendingSpawnObjects.Add(netObject.NetworkId, pso);

                return netObject;
            }

            //Normal spawning
            { 
                GameObject go = MonoBehaviour.Instantiate(netManager.NetworkConfig.NetworkedPrefabs[networkedPrefabId].prefab, position, rotation);
                NetworkedObject netObject = go.GetComponent<NetworkedObject>();
                if (netObject == null)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Please add a NetworkedObject component to the root of all spawnable objects");
                    netObject = go.AddComponent<NetworkedObject>();
                }

                if (readNetworkedVar) netObject.SetNetworkedVarData(stream);

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
                netObject.OnlySpawnInSceneOriginalySpawnedAt = OnlySpawnInSceneOriginalySpawnedAt;
                netObject.sceneSpawnedInIndex = sceneSpawnedInIndex;

                SpawnedObjects.Add(netObject.NetworkId, netObject);
                SpawnedObjectsList.Add(netObject);
                if (playerObject) NetworkingManager.singleton.ConnectedClients[owner].PlayerObject = netObject;
                netObject.InvokeBehaviourNetworkSpawn(readPayload ? stream : null);
                netObject.gameObject.SetActive(isActive);
                return netObject;
            }
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
        internal static void SpawnPlayerObject(NetworkedObject netObject, uint clientId, Stream payload = null)
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
            netObject.sceneSpawnedInIndex = NetworkSceneManager.CurrentActiveSceneIndex;
            netObject.isPlayerObject = true;
            netManager.ConnectedClients[clientId].PlayerObject = netObject;

            if (payload == null) netObject.InvokeBehaviourNetworkSpawn(null);
            else netObject.InvokeBehaviourNetworkSpawn(payload);

            foreach (var client in netManager.ConnectedClients)
            {
                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteBool(true);
                        writer.WriteUInt32Packed(netObject.NetworkId);
                        writer.WriteUInt32Packed(netObject.OwnerClientId);
                        writer.WriteInt32Packed(netManager.NetworkConfig.NetworkPrefabIds[netObject.NetworkedPrefabName]);
                        writer.WriteBool(netObject.sceneObject == null ? true : netObject.sceneObject.Value);

                        writer.WriteBool(netObject.OnlySpawnInSceneOriginalySpawnedAt);
                        writer.WriteUInt32Packed(netObject.sceneSpawnedInIndex);

                        writer.WriteSinglePacked(netObject.transform.position.x);
                        writer.WriteSinglePacked(netObject.transform.position.y);
                        writer.WriteSinglePacked(netObject.transform.position.z);

                        writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.x);
                        writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.y);
                        writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.z);

                        writer.WriteBool(payload != null);

                        netObject.WriteNetworkedVarData(stream, client.Key);

                        if (payload != null) stream.CopyFrom(payload);

                        InternalMessageHandler.Send(client.Key, MLAPIConstants.MLAPI_ADD_OBJECT, "MLAPI_INTERNAL", stream);
                    }
                }
            }
        }


        internal static void SpawnObject(NetworkedObject netObject, uint? clientOwnerId = null, Stream payload = null, bool destroyOnSceneChange = false)
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
            netObject.sceneObject = destroyOnSceneChange;
            netObject.sceneSpawnedInIndex = NetworkSceneManager.CurrentActiveSceneIndex;

            if (clientOwnerId != null)
            {
                netObject.OwnerClientId = clientOwnerId.Value;
                NetworkingManager.singleton.ConnectedClients[clientOwnerId.Value].OwnedObjects.Add(netObject);
            }

            if (payload == null) netObject.InvokeBehaviourNetworkSpawn(null);
            else netObject.InvokeBehaviourNetworkSpawn(payload);    

            foreach (var client in netManager.ConnectedClients)
            {
                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteBool(false);
                        writer.WriteUInt32Packed(netObject.NetworkId);
                        writer.WriteUInt32Packed(netObject.OwnerClientId);
                        writer.WriteInt32Packed(netManager.NetworkConfig.NetworkPrefabIds[netObject.NetworkedPrefabName]);
                        writer.WriteBool(netObject.sceneObject == null ? true : netObject.sceneObject.Value);

                        writer.WriteBool(netObject.OnlySpawnInSceneOriginalySpawnedAt);
                        writer.WriteUInt32Packed(netObject.sceneSpawnedInIndex);

                        writer.WriteSinglePacked(netObject.transform.position.x);
                        writer.WriteSinglePacked(netObject.transform.position.y);
                        writer.WriteSinglePacked(netObject.transform.position.z);

                        writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.x);
                        writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.y);
                        writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.z);

                        writer.WriteBool(payload != null);

                        netObject.WriteNetworkedVarData(stream, client.Key);

                        if (payload != null) stream.CopyFrom(payload);

                        InternalMessageHandler.Send(client.Key, MLAPIConstants.MLAPI_ADD_OBJECT, "MLAPI_INTERNAL", stream);
                    }
                }
            }
        }

        internal static void OnDestroyObject(uint networkId, bool destroyGameObject)
        {
            if((netManager != null && !netManager.NetworkConfig.HandleObjectSpawning))
                return;

            //Removal of pending object
            if (PendingSpawnObjects.ContainsKey(networkId))
            {

                if (!PendingSpawnObjects[networkId].netObject.isOwnedByServer && !PendingSpawnObjects[networkId].netObject.isPlayerObject &&
                netManager.ConnectedClients.ContainsKey(PendingSpawnObjects[networkId].netObject.OwnerClientId))
                {
                    //Someone owns it.
                    for (int i = NetworkingManager.singleton.ConnectedClients[PendingSpawnObjects[networkId].netObject.OwnerClientId].OwnedObjects.Count - 1; i > -1; i--)
                    {
                        if (NetworkingManager.singleton.ConnectedClients[PendingSpawnObjects[networkId].netObject.OwnerClientId].OwnedObjects[i].NetworkId == networkId)
                            NetworkingManager.singleton.ConnectedClients[PendingSpawnObjects[networkId].netObject.OwnerClientId].OwnedObjects.RemoveAt(i);
                    }
                }

                GameObject pendingGameObject = PendingSpawnObjects[networkId].netObject.gameObject;
                if (destroyGameObject && pendingGameObject != null)
                    MonoBehaviour.Destroy(pendingGameObject);
                PendingSpawnObjects.Remove(networkId);
            }

            //Removal of spawned object
            if (!SpawnedObjects.ContainsKey(networkId))
                return;
			if (!SpawnedObjects[networkId].isOwnedByServer && !SpawnedObjects[networkId].isPlayerObject && 
			    netManager.ConnectedClients.ContainsKey(SpawnedObjects[networkId].OwnerClientId))
            {
                //Someone owns it.
                for (int i = NetworkingManager.singleton.ConnectedClients[SpawnedObjects[networkId].OwnerClientId].OwnedObjects.Count - 1; i > -1; i--)
                {
                    if (NetworkingManager.singleton.ConnectedClients[SpawnedObjects[networkId].OwnerClientId].OwnedObjects[i].NetworkId == networkId)
                        NetworkingManager.singleton.ConnectedClients[SpawnedObjects[networkId].OwnerClientId].OwnedObjects.RemoveAt(i);
                }
            }
            SpawnedObjects[networkId].isSpawned = false;

            if (netManager != null && netManager.isServer)
            {
                releasedNetworkObjectIds.Push(networkId);
                if (SpawnedObjects[networkId] != null)
                {
                    using (PooledBitStream stream = PooledBitStream.Get())
                    {
                        using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                        {
                            writer.WriteUInt32Packed(networkId);

                            InternalMessageHandler.Send(MLAPIConstants.MLAPI_DESTROY_OBJECT, "MLAPI_INTERNAL", stream);
                        }
                    }
                }
            }

            GameObject go = SpawnedObjects[networkId].gameObject;
            if (destroyGameObject && go != null)
                MonoBehaviour.Destroy(go);
            SpawnedObjects.Remove(networkId);
            for (int i = SpawnedObjectsList.Count - 1; i > -1; i--)
            {
                if (SpawnedObjectsList[i].NetworkId == networkId)
                    SpawnedObjectsList.RemoveAt(i);
            }
        }


        internal static List<NetworkedObject> GetPendingSpawnObjectsList()
        {
            List<NetworkedObject> list = new List<NetworkedObject>();
            foreach (var pendingSpawnObject in PendingSpawnObjects.Values)
            {
                list.Add(pendingSpawnObject.netObject);
            }
            return list;
        }

        internal static void spawnPendingObjectsForScene(uint sceneIndex)
        {
            List<uint> keysToRemove = new List<uint>();

            foreach (var pendingSpawnObject in PendingSpawnObjects)
            {
                if (pendingSpawnObject.Value.sceneSpawnedInIndex == sceneIndex)
                {
                    pendingSpawnObject.Value.netObject.gameObject.SetActive(true);

                    using (PooledBitStream streamA = PooledBitStream.Get())
                    {
                        pendingSpawnObject.Value.WriteNetworkedVarData(streamA, NetworkingManager.singleton.LocalClientId);
                        using (Serialization.BitStream streamB = new Serialization.BitStream(streamA.GetBuffer()))
                        {
                            pendingSpawnObject.Value.netObject.SetNetworkedVarData(streamB);
                        }
                    }

                    SpawnedObjects.Add(pendingSpawnObject.Key, pendingSpawnObject.Value.netObject);
                    SpawnedObjectsList.Add(pendingSpawnObject.Value.netObject);

                    if (pendingSpawnObject.Value.playerObject) NetworkingManager.singleton.ConnectedClients[pendingSpawnObject.Value.owner].PlayerObject = pendingSpawnObject.Value.netObject;

                    if (pendingSpawnObject.Value.payload.Length > 0)
                    {
                        using (Serialization.BitStream stream = new Serialization.BitStream(pendingSpawnObject.Value.payload))
                        {
                            pendingSpawnObject.Value.netObject.InvokeBehaviourNetworkSpawn(stream);
                        }
                    }
                    else
                    {
                        pendingSpawnObject.Value.netObject.InvokeBehaviourNetworkSpawn(null);
                    }

                    pendingSpawnObject.Value.netObject.gameObject.SetActive(pendingSpawnObject.Value.isActive);

                    keysToRemove.Add(pendingSpawnObject.Key);
                }
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                PendingSpawnObjects.Remove(keysToRemove[i]);
            }
        }
    }



    internal class PendingSpawnObject
    {
        internal NetworkedObject netObject;
        internal Dictionary<ushort, List<INetworkedVar>> dummyNetworkedVars;
        internal uint sceneSpawnedInIndex;
        internal byte[] payload;
        internal bool playerObject;
        internal uint owner;
        internal bool isActive;

        internal void HandleNetworkedVarDeltas(ushort orderIndex, Stream stream, uint clientId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                for (int i = 0; i < dummyNetworkedVars[orderIndex].Count; i++)
                {
                    if (!reader.ReadBool())
                        continue;

                    if (NetworkingManager.singleton.isServer && !dummyNetworkedVars[orderIndex][i].CanClientWrite(clientId))
                    {
                        //This client wrote somewhere they are not allowed. This is critical
                        //We can't just skip this field. Because we don't actually know how to dummy read
                        //That is, we don't know how many bytes to skip. Because the interface doesn't have a 
                        //Read that gives us the value. Only a Read that applies the value straight away
                        //A dummy read COULD be added to the interface for this situation, but it's just being too nice.
                        //This is after all a developer fault. A critical error should be fine.
                        // - TwoTen
                        if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Client wrote to NetworkedVar without permission. No more variables can be read. This is critical");
                        return;
                    }
                    dummyNetworkedVars[orderIndex][i].ReadDelta(stream);
                }
            }
        }
        internal void HandleNetworkedVarUpdate(ushort orderIndex, Stream stream, uint clientId)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                for (int i = 0; i < dummyNetworkedVars[orderIndex].Count; i++)
                {
                    if (!reader.ReadBool())
                        continue;

                    if (NetworkingManager.singleton.isServer && !dummyNetworkedVars[orderIndex][i].CanClientWrite(clientId))
                    {
                        //This client wrote somewhere they are not allowed. This is critical
                        //We can't just skip this field. Because we don't actually know how to dummy read
                        //That is, we don't know how many bytes to skip. Because the interface doesn't have a 
                        //Read that gives us the value. Only a Read that applies the value straight away
                        //A dummy read COULD be added to the interface for this situation, but it's just being too nice.
                        //This is after all a developer fault. A critical error should be fine.
                        // - TwoTen
                        if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError("Client wrote to NetworkedVar without permission. No more variables can be read. This is critical");
                        return;
                    }
                    dummyNetworkedVars[orderIndex][i].ReadField(stream);
                }
            }
        }


        internal void WriteNetworkedVarData(Stream stream, uint clientId)
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                foreach (var NetworkedVarsList in dummyNetworkedVars.Values)
                {
                    for (int j = 0; j < NetworkedVarsList.Count; j++)
                    {
                        bool canClientRead = NetworkedVarsList[j].CanClientRead(clientId);
                        writer.WriteBool(canClientRead);
                        if (canClientRead) NetworkedVarsList[j].WriteField(stream);
                    }
                }
            }
        }

        internal void SetNetworkedVarData(Stream stream)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                foreach (var NetworkedVarsList in dummyNetworkedVars.Values)
                {
                    if (NetworkedVarsList.Count == 0)
                        continue;
                    for (int j = 0; j < NetworkedVarsList.Count; j++)
                    {
                        if (reader.ReadBool()) NetworkedVarsList[j].ReadField(stream);
                    }
                }
            }
        }
    }
}
