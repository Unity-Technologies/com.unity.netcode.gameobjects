using System.Collections.Generic;
using System.IO;
using MLAPI.Configuration;
using MLAPI.Data;
using MLAPI.Internal;
using MLAPI.Logging;
using MLAPI.NetworkedVar;
using MLAPI.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        internal static ulong GetPrefabHash(string prefabName)
        {
            HashSize mode = NetworkingManager.singleton.NetworkConfig.PrefabHashSize;

            if (mode == HashSize.VarIntTwoBytes)
                return prefabName.GetStableHash16();
            if (mode == HashSize.VarIntFourBytes)
                return prefabName.GetStableHash32();
            if (mode == HashSize.VarIntEightBytes)
                return prefabName.GetStableHash64();

            return 0;
        }

        /// <summary>
        /// Gets the prefab index of a given prefab hash
        /// </summary>
        /// <param name="hash">The hash of the prefab</param>
        /// <returns>The index of the prefab</returns>
        public static int GetNetworkedPrefabIndexOfHash(ulong hash)
        {
            for (int i = 0; i < NetworkingManager.singleton.NetworkConfig.NetworkedPrefabs.Count; i++)
            {
                if (NetworkingManager.singleton.NetworkConfig.NetworkedPrefabs[i].hash == hash)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Gets the prefab index of a given prefab name
        /// </summary>
        /// <param name="name">The name of the prefab</param>
        /// <returns>The index of the prefab</returns>
        public static int GetNetworkedPrefabIndexOfName(string name)
        {
            for (int i = 0; i < NetworkingManager.singleton.NetworkConfig.NetworkedPrefabs.Count; i++)
            {
                if (NetworkingManager.singleton.NetworkConfig.NetworkedPrefabs[i].name == name)
                    return i;
            }

            return -1;
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

                    InternalMessageHandler.Send(MLAPIConstants.MLAPI_CHANGE_OWNER, "MLAPI_INTERNAL", stream, SecuritySendFlags.None);
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

                    InternalMessageHandler.Send(MLAPIConstants.MLAPI_CHANGE_OWNER, "MLAPI_INTERNAL", stream, SecuritySendFlags.None);
                }
            }
        }

        internal static void DestroyNonSceneObjects()
        {
            if (SpawnedObjects != null)
            {
                foreach (KeyValuePair<uint, NetworkedObject> netObject in SpawnedObjects)
                {
                    if (netObject.Value.destroyWithScene != null && netObject.Value.destroyWithScene.Value == false)
                        MonoBehaviour.Destroy(netObject.Value.gameObject);
                }
            }
        }

        internal static void DestroySceneObjects()
        {
            NetworkedObject[] netObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();
            for (int i = 0; i < netObjects.Length; i++)
            {
                if (netObjects[i].destroyWithScene == null || netObjects[i].destroyWithScene.Value == true)
                    MonoBehaviour.Destroy(netObjects[i].gameObject);
            }
        }

        internal static void MarkSceneObjects()
        {
            NetworkedObject[] netObjects = MonoBehaviour.FindObjectsOfType<NetworkedObject>();
            for (int i = 0; i < netObjects.Length; i++)
            {
                if (netObjects[i].destroyWithScene == null)
                {
                    netObjects[i].InvokeBehaviourNetworkSpawn(null);
                    netObjects[i].destroyWithScene = true;
                }
            }
        }

        internal static NetworkedObject CreateSpawnedObject(int networkedPrefabId, uint networkId, uint owner, bool playerObject, uint sceneSpawnedInIndex, bool sceneDelayedSpawn, bool destroyWithScene, Vector3? position, Quaternion? rotation, bool isActive, Stream stream, bool readPayload, int payloadLength, bool readNetworkedVar)
        {
            if (networkedPrefabId >= netManager.NetworkConfig.NetworkedPrefabs.Count || networkedPrefabId < 0)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Cannot spawn the object, invalid prefabIndex: " + networkedPrefabId);
                return null;
            }

            //Delayed spawning
            if (sceneDelayedSpawn && sceneSpawnedInIndex != NetworkSceneManager.CurrentActiveSceneIndex)
            {
                GameObject prefab = netManager.NetworkConfig.NetworkedPrefabs[networkedPrefabId].prefab;
                bool prefabActive = prefab.activeSelf;
                prefab.SetActive(false);
                GameObject go = (position == null && rotation == null) ? MonoBehaviour.Instantiate(prefab) : MonoBehaviour.Instantiate(prefab, position.GetValueOrDefault(Vector3.zero), rotation.GetValueOrDefault(Quaternion.identity));
                prefab.SetActive(prefabActive);

                //Appearantly some wierd behavior when switching scenes can occur that destroys this object even though the scene is
                //not destroyed, therefor we set it to DontDestroyOnLoad here, to prevent that problem.
                MonoBehaviour.DontDestroyOnLoad(go);

                NetworkedObject netObject = go.GetComponent<NetworkedObject>();
                if (netObject == null)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Please add a NetworkedObject component to the root of all spawnable objects");
                    netObject = go.AddComponent<NetworkedObject>();
                }

                netObject.NetworkedPrefabName = netManager.NetworkConfig.NetworkedPrefabs[networkedPrefabId].name;
                netObject.isSpawned = false;
                netObject.isPooledObject = false;

                if (netManager.isServer) netObject.NetworkId = GetNetworkObjectId();
                else netObject.NetworkId = networkId;

                netObject.destroyWithScene = destroyWithScene;
                netObject.OwnerClientId = owner;
                netObject.isPlayerObject = playerObject;
                netObject.SceneDelayedSpawn = sceneDelayedSpawn;
                netObject.sceneSpawnedInIndex = sceneSpawnedInIndex;

                Dictionary<ushort, List<INetworkedVar>> dummyNetworkedVars = new Dictionary<ushort, List<INetworkedVar>>();
                List<NetworkedBehaviour> networkedBehaviours = new List<NetworkedBehaviour>(netObject.GetComponentsInChildren<NetworkedBehaviour>());
                for (ushort i = 0; i < networkedBehaviours.Count; i++)
                {
                    dummyNetworkedVars.Add(i, networkedBehaviours[i].GetDummyNetworkedVars());
                }

                PendingSpawnObject pso = new PendingSpawnObject()
                {
                    netObject = netObject,
                    dummyNetworkedVars = dummyNetworkedVars,
                    sceneSpawnedInIndex = sceneSpawnedInIndex,
                    playerObject = playerObject,
                    owner = owner,
                    isActive = isActive,
                    payload = null
                };
                PendingSpawnObjects.Add(netObject.NetworkId, pso);

                pso.SetNetworkedVarData(stream);
                if (readPayload)
                {
                    MLAPI.Serialization.BitStream payloadStream = new MLAPI.Serialization.BitStream();
                    payloadStream.CopyUnreadFrom(stream, payloadLength);
                    stream.Position += payloadLength;
                    pso.payload = payloadStream;
                }

                return netObject;
            }

            //Normal spawning
            { 
                GameObject prefab = netManager.NetworkConfig.NetworkedPrefabs[networkedPrefabId].prefab;
                GameObject go = (position == null && rotation == null) ? MonoBehaviour.Instantiate(prefab) : MonoBehaviour.Instantiate(prefab, position.GetValueOrDefault(Vector3.zero), rotation.GetValueOrDefault(Quaternion.identity));

                NetworkedObject netObject = go.GetComponent<NetworkedObject>();
                if (netObject == null)
                {
                    if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Please add a NetworkedObject component to the root of all spawnable objects");
                    netObject = go.AddComponent<NetworkedObject>();
                }

                if (readNetworkedVar) netObject.SetNetworkedVarData(stream);

                netObject.NetworkedPrefabName = netManager.NetworkConfig.NetworkedPrefabs[networkedPrefabId].name;
                netObject.isSpawned = true;
                netObject.isPooledObject = false;

                if (netManager.isServer) netObject.NetworkId = GetNetworkObjectId();
                else netObject.NetworkId = networkId;

                netObject.destroyWithScene = destroyWithScene;
                netObject.OwnerClientId = owner;
                netObject.isPlayerObject = playerObject;
                netObject.SceneDelayedSpawn = sceneDelayedSpawn;
                netObject.sceneSpawnedInIndex = sceneSpawnedInIndex;

                SpawnedObjects.Add(netObject.NetworkId, netObject);
                SpawnedObjectsList.Add(netObject);
                if (playerObject) NetworkingManager.singleton.ConnectedClients[owner].PlayerObject = netObject;

                if (readPayload)
                {
                    using (PooledBitStream payloadStream = PooledBitStream.Get())
                    {
                        payloadStream.CopyUnreadFrom(stream, payloadLength);
                        stream.Position += payloadLength;
                        netObject.InvokeBehaviourNetworkSpawn(payloadStream);
                    }
                }
                else
                {
                    netObject.InvokeBehaviourNetworkSpawn(null);
                }
               
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
            else if (SpawnManager.GetNetworkedPrefabIndexOfName(netObject.NetworkedPrefabName) == -1)
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
            netObject.destroyWithScene = false;
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
                        writer.WriteUInt64Packed(netObject.NetworkedPrefabHash);

                        writer.WriteBool(netObject.destroyWithScene == null ? true : netObject.destroyWithScene.Value);
                        writer.WriteBool(netObject.SceneDelayedSpawn);
                        writer.WriteUInt32Packed(netObject.sceneSpawnedInIndex);

                        writer.WriteSinglePacked(netObject.transform.position.x);
                        writer.WriteSinglePacked(netObject.transform.position.y);
                        writer.WriteSinglePacked(netObject.transform.position.z);

                        writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.x);
                        writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.y);
                        writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.z);

                        writer.WriteBool(payload != null);
                        if (payload != null)
                        {
                            writer.WriteInt32Packed((int)payload.Length);
                        }

                        netObject.WriteNetworkedVarData(stream, client.Key);

                        if (payload != null) stream.CopyFrom(payload);

                        InternalMessageHandler.Send(client.Key, MLAPIConstants.MLAPI_ADD_OBJECT, "MLAPI_INTERNAL", stream, SecuritySendFlags.None);
                    }
                }
            }
        }


        internal static void SpawnObject(NetworkedObject netObject, uint? clientOwnerId = null, Stream payload = null, bool destroyWithScene = false)
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
            else if (SpawnManager.GetNetworkedPrefabIndexOfName(netObject.NetworkedPrefabName) == -1)
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
            netObject.destroyWithScene = destroyWithScene;
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
                        writer.WriteUInt64Packed(netObject.NetworkedPrefabHash);

                        writer.WriteBool(netObject.destroyWithScene == null ? true : netObject.destroyWithScene.Value);
                        writer.WriteBool(netObject.SceneDelayedSpawn);
                        writer.WriteUInt32Packed(netObject.sceneSpawnedInIndex);

                        writer.WriteSinglePacked(netObject.transform.position.x);
                        writer.WriteSinglePacked(netObject.transform.position.y);
                        writer.WriteSinglePacked(netObject.transform.position.z);

                        writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.x);
                        writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.y);
                        writer.WriteSinglePacked(netObject.transform.rotation.eulerAngles.z);

                        writer.WriteBool(payload != null);
                        if (payload != null)
                        {
                            writer.WriteInt32Packed((int)payload.Length);
                        }

                        netObject.WriteNetworkedVarData(stream, client.Key);

                        if (payload != null) stream.CopyFrom(payload);

                        InternalMessageHandler.Send(client.Key, MLAPIConstants.MLAPI_ADD_OBJECT, "MLAPI_INTERNAL", stream, SecuritySendFlags.None);
                    }
                }
            }
        }

        internal static void OnDestroyObject(uint networkId, bool destroyGameObject)
        {
            if((netManager == null || !netManager.NetworkConfig.HandleObjectSpawning))
                return;

            //Removal of pending object
            //Even though pending objects is marked with DontDestroyOnLoad, the OnDestroy method is invoked on pending objects. They are however not
            //destroyed (probably a unity bug for having an gameobject spawned as inactive). Therefore we only actual remove it from the list if 
            //destroyGameObject is set to true, meaning MLAPI decided to destroy it, not unity.
            if (destroyGameObject == true && PendingSpawnObjects.ContainsKey(networkId))
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
                if (pendingGameObject != null)
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

                            InternalMessageHandler.Send(MLAPIConstants.MLAPI_DESTROY_OBJECT, "MLAPI_INTERNAL", stream, SecuritySendFlags.None);
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

        internal static void SpawnPendingObjectsForScene(uint sceneIndex)
        {
            List<uint> keysToRemove = new List<uint>();

            foreach (var pendingSpawnObject in PendingSpawnObjects)
            {
                if (pendingSpawnObject.Value.sceneSpawnedInIndex == sceneIndex)
                {
                    //Move the pending object away from the DontDestroyOnLoad scene and back into the active scene.
                    SceneManager.MoveGameObjectToScene(pendingSpawnObject.Value.netObject.gameObject, SceneManager.GetActiveScene());

                    pendingSpawnObject.Value.netObject.gameObject.SetActive(true);
                    pendingSpawnObject.Value.netObject.isSpawned = true;

                    using (PooledBitStream stream = PooledBitStream.Get())
                    {
                        pendingSpawnObject.Value.WriteNetworkedVarData(stream, NetworkingManager.singleton.LocalClientId);
                        stream.Position = 0;
                        pendingSpawnObject.Value.netObject.SetNetworkedVarData(stream);
                    }

                    SpawnedObjects.Add(pendingSpawnObject.Key, pendingSpawnObject.Value.netObject);
                    SpawnedObjectsList.Add(pendingSpawnObject.Value.netObject);

                    if (pendingSpawnObject.Value.playerObject) NetworkingManager.singleton.ConnectedClients[pendingSpawnObject.Value.owner].PlayerObject = pendingSpawnObject.Value.netObject;

                    pendingSpawnObject.Value.netObject.InvokeBehaviourNetworkSpawn(pendingSpawnObject.Value.payload);
                    if(pendingSpawnObject.Value.payload != null)
                    {
                        pendingSpawnObject.Value.payload.Dispose();
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
        internal Stream payload = null;
        internal bool playerObject;
        internal uint owner;
        internal bool isActive;

        internal List<INetworkedVar> GetDummyNetworkedVarListAtOrderIndex(ushort orderIndex)
        {
            return dummyNetworkedVars[orderIndex];
        }

        internal void WriteNetworkedVarData(Stream stream, uint clientId)
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                foreach (var NetworkedVarsList in dummyNetworkedVars.Values)
                {
                    NetworkedBehaviour.WriteNetworkedVarData(NetworkedVarsList, writer, stream, clientId);
                }
            }
        }

        internal void SetNetworkedVarData(Stream stream)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                foreach (var NetworkedVarsList in dummyNetworkedVars.Values)
                {
                    NetworkedBehaviour.SetNetworkedVarData(NetworkedVarsList, reader, stream);
                }
            }
        }
    }
}
