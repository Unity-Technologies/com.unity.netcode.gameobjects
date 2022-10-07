using System.Collections.Generic;
using System;
using System.Linq;
using Unity.Collections;
using UnityEngine.SceneManagement;

namespace Unity.Netcode
{
    /// <summary>
    /// The different types of scene events communicated between a server and client. <br/>
    /// Used by <see cref="NetworkSceneManager"/> for <see cref="SceneEventMessage"/> messages.<br/>
    /// <em>Note: This is only when <see cref="NetworkConfig.EnableSceneManagement"/> is enabled.</em><br/>
    /// See also: <br/>
    /// <seealso cref="SceneEvent"/>
    /// </summary>
    public enum SceneEventType : byte
    {
        /// <summary>
        /// Load a scene<br/>
        /// <b>Invocation:</b> Server Side<br/>
        /// <b>Message Flow:</b> Server to client<br/>
        /// <b>Event Notification:</b> Both server and client are notified a load scene event started
        /// </summary>
        Load,
        /// <summary>
        /// Unload a scene<br/>
        /// <b>Invocation:</b> Server Side<br/>
        /// <b>Message Flow:</b> Server to client<br/>
        /// <b>Event Notification:</b> Both server and client are notified an unload scene event started.
        /// </summary>
        Unload,
        /// <summary>
        /// Synchronizes current game session state for newly approved clients<br/>
        /// <b>Invocation:</b> Server Side<br/>
        /// <b>Message Flow:</b> Server to client<br/>
        /// <b>Event Notification:</b> Server and Client receives a local notification (<em>server receives the ClientId being synchronized</em>).
        /// </summary>
        Synchronize,
        /// <summary>
        /// Game session re-synchronization of NetworkObjects that were destroyed during a <see cref="Synchronize"/> event<br/>
        /// <b>Invocation:</b> Server Side<br/>
        /// <b>Message Flow:</b> Server to client<br/>
        /// <b>Event Notification:</b> Both server and client receive a local notification<br/>
        /// </summary>
        ReSynchronize,
        /// <summary>
        /// All clients have finished loading a scene<br/>
        /// <b>Invocation:</b> Server Side<br/>
        /// <b>Message Flow:</b> Server to Client<br/>
        /// <b>Event Notification:</b> Both server and client receive a local notification containing the clients that finished
        /// as well as the clients that timed out(<em>if any</em>).
        /// </summary>
        LoadEventCompleted,
        /// <summary>
        /// All clients have unloaded a scene<br/>
        /// <b>Invocation:</b> Server Side<br/>
        /// <b>Message Flow:</b> Server to Client<br/>
        /// <b>Event Notification:</b> Both server and client receive a local notification containing the clients that finished
        /// as well as the clients that timed out(<em>if any</em>).
        /// </summary>
        UnloadEventCompleted,
        /// <summary>
        /// A client has finished loading a scene<br/>
        /// <b>Invocation:</b> Client Side<br/>
        /// <b>Message Flow:</b> Client to Server<br/>
        /// <b>Event Notification:</b> Both server and client receive a local notification.
        /// </summary>
        LoadComplete,
        /// <summary>
        /// A client has finished unloading a scene<br/>
        /// <b>Invocation:</b> Client Side<br/>
        /// <b>Message Flow:</b> Client to Server<br/>
        /// <b>Event Notification:</b> Both server and client receive a local notification.
        /// </summary>
        UnloadComplete,
        /// <summary>
        /// A client has finished synchronizing from a <see cref="Synchronize"/> event<br/>
        /// <b>Invocation:</b> Client Side<br/>
        /// <b>Message Flow:</b> Client to Server<br/>
        /// <b>Event Notification:</b> Both server and client receive a local notification.
        /// </summary>
        SynchronizeComplete,
    }

    /// <summary>
    /// Used by <see cref="NetworkSceneManager"/> for <see cref="SceneEventMessage"/> messages
    /// <em>Note: This is only when <see cref="NetworkConfig.EnableSceneManagement"/> is enabled.</em><br/>
    /// See also: <seealso cref="SceneEvent"/>
    /// </summary>
    internal class SceneEventData : IDisposable
    {
        internal SceneEventType SceneEventType;
        internal LoadSceneMode LoadSceneMode;
        internal ForceNetworkSerializeByMemcpy<Guid> SceneEventProgressId;
        internal uint SceneEventId;


        internal uint SceneHash;
        internal int SceneHandle;

        // Used by the client during synchronization
        internal uint ClientSceneHash;
        internal int NetworkSceneHandle;

        /// Only used for <see cref="SceneEventType.Synchronize"/> scene events, this assures permissions when writing
        /// NetworkVariable information.  If that process changes, then we need to update this
        internal ulong TargetClientId;

        private Dictionary<uint, List<NetworkObject>> m_SceneNetworkObjects;
        private Dictionary<uint, long> m_SceneNetworkObjectDataOffsets;

        /// <summary>
        /// Client or Server Side:
        /// Client side: Generates a list of all NetworkObjects by their NetworkObjectId that was spawned during th synchronization process
        /// Server side: Compares list from client to make sure client didn't drop a message about a NetworkObject being despawned while it
        /// was synchronizing (if so server will send another message back to the client informing the client of NetworkObjects to remove)
        /// spawned during an initial synchronization.
        /// </summary>
        private List<NetworkObject> m_NetworkObjectsSync = new List<NetworkObject>();

        private List<NetworkObject> m_DespawnedInSceneObjectsSync = new List<NetworkObject>();
        private Dictionary<int, List<uint>> m_DespawnedInSceneObjects = new Dictionary<int, List<uint>>();

        /// <summary>
        /// Server Side Re-Synchronization:
        /// If there happens to be NetworkObjects in the final Event_Sync_Complete message that are no longer spawned,
        /// the server will compile a list and send back an Event_ReSync message to the client.
        /// </summary>
        private List<ulong> m_NetworkObjectsToBeRemoved = new List<ulong>();

        private bool m_HasInternalBuffer;
        internal FastBufferReader InternalBuffer;

        private NetworkManager m_NetworkManager;

        internal List<ulong> ClientsCompleted;
        internal List<ulong> ClientsTimedOut;

        internal Queue<uint> ScenesToSynchronize;
        internal Queue<uint> SceneHandlesToSynchronize;


        /// <summary>
        /// Server Side:
        /// Add a scene and its handle to the list of scenes the client should load before synchronizing
        /// Since scene handles are not the same per instance, the client builds a server scene handle to
        /// client scene handle lookup table.
        /// Why include the scene handle? In order to support loading of the same additive scene more than once
        /// we must distinguish which scene we are talking about when the server tells the client to unload a scene.
        /// The server will always communicate its local relative scene's handle and the client will determine its
        /// local relative handle from the table being built.
        /// Look for <see cref="NetworkSceneManager.m_ServerSceneHandleToClientSceneHandle"/> usage to see where
        /// entries are being added to or removed from the table
        /// </summary>
        /// <param name="sceneIndex"></param>
        /// <param name="sceneHandle"></param>
        internal void AddSceneToSynchronize(uint sceneHash, int sceneHandle)
        {
            ScenesToSynchronize.Enqueue(sceneHash);
            SceneHandlesToSynchronize.Enqueue((uint)sceneHandle);
        }

        /// <summary>
        /// Client Side:
        /// Gets the next scene hash to be loaded for approval and/or late joining
        /// </summary>
        /// <returns></returns>
        internal uint GetNextSceneSynchronizationHash()
        {
            return ScenesToSynchronize.Dequeue();
        }

        /// <summary>
        /// Client Side:
        /// Gets the next scene handle to be loaded for approval and/or late joining
        /// </summary>
        /// <returns></returns>
        internal int GetNextSceneSynchronizationHandle()
        {
            return (int)SceneHandlesToSynchronize.Dequeue();
        }

        /// <summary>
        /// Client Side:
        /// Determines if all scenes have been processed during the synchronization process
        /// </summary>
        /// <returns>true/false</returns>
        internal bool IsDoneWithSynchronization()
        {
            if (ScenesToSynchronize.Count == 0 && SceneHandlesToSynchronize.Count == 0)
            {
                return true;
            }
            else if (ScenesToSynchronize.Count != SceneHandlesToSynchronize.Count)
            {
                // This should never happen, but in the event it does...
                throw new Exception($"[{nameof(SceneEventData)}-Internal Mismatch Error] {nameof(ScenesToSynchronize)} count != {nameof(SceneHandlesToSynchronize)} count!");
            }
            return false;
        }

        /// <summary>
        /// Server Side:
        /// Called just before the synchronization process
        /// </summary>
        internal void InitializeForSynch()
        {
            if (m_SceneNetworkObjects == null)
            {
                m_SceneNetworkObjects = new Dictionary<uint, List<NetworkObject>>();
            }
            else
            {
                m_SceneNetworkObjects.Clear();
            }

            if (ScenesToSynchronize == null)
            {
                ScenesToSynchronize = new Queue<uint>();
            }
            else
            {
                ScenesToSynchronize.Clear();
            }

            if (SceneHandlesToSynchronize == null)
            {
                SceneHandlesToSynchronize = new Queue<uint>();
            }
            else
            {
                SceneHandlesToSynchronize.Clear();
            }
        }

        internal void AddSpawnedNetworkObjects()
        {
            m_NetworkObjectsSync.Clear();
            foreach (var sobj in m_NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (sobj.Observers.Contains(TargetClientId))
                {
                    m_NetworkObjectsSync.Add(sobj);
                }
            }

            // Sort by parents before children
            m_NetworkObjectsSync.Sort(SortParentedNetworkObjects);

            // Sort by INetworkPrefabInstanceHandler implementation before the
            // NetworkObjects spawned by the implementation
            m_NetworkObjectsSync.Sort(SortNetworkObjects);

            // This is useful to know what NetworkObjects a client is going to be synchronized with
            // as well as the order in which they will be deserialized
            if (m_NetworkManager.LogLevel == LogLevel.Developer)
            {
                var messageBuilder = new System.Text.StringBuilder(0xFFFF);
                messageBuilder.Append("[Server-Side Client-Synchronization] NetworkObject serialization order:");
                foreach (var networkObject in m_NetworkObjectsSync)
                {
                    messageBuilder.Append($"{networkObject.name}");
                }
                NetworkLog.LogInfo(messageBuilder.ToString());
            }
        }

        internal void AddDespawnedInSceneNetworkObjects()
        {
            m_DespawnedInSceneObjectsSync.Clear();
            // Find all active and non-active in-scene placed NetworkObjects
            var inSceneNetworkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>(includeInactive: true).Where((c) => c.NetworkManager == m_NetworkManager);
            foreach (var sobj in inSceneNetworkObjects)
            {
                if (sobj.IsSceneObject.HasValue && sobj.IsSceneObject.Value && !sobj.IsSpawned)
                {
                    m_DespawnedInSceneObjectsSync.Add(sobj);
                }
            }
        }

        /// <summary>
        /// Server Side:
        /// Used during the synchronization process to associate NetworkObjects with scenes
        /// </summary>
        /// <param name="sceneIndex"></param>
        /// <param name="networkObject"></param>
        internal void AddNetworkObjectForSynch(uint sceneIndex, NetworkObject networkObject)
        {
            if (!m_SceneNetworkObjects.ContainsKey(sceneIndex))
            {
                m_SceneNetworkObjects.Add(sceneIndex, new List<NetworkObject>());
            }

            m_SceneNetworkObjects[sceneIndex].Add(networkObject);
        }

        /// <summary>
        /// Client and Server:
        /// Determines if the scene event type was intended for the client ( or server )
        /// </summary>
        /// <returns>true (client should handle this message) false (server should handle this message)</returns>
        internal bool IsSceneEventClientSide()
        {
            switch (SceneEventType)
            {
                case SceneEventType.Load:
                case SceneEventType.Unload:
                case SceneEventType.Synchronize:
                case SceneEventType.ReSynchronize:
                case SceneEventType.LoadEventCompleted:
                case SceneEventType.UnloadEventCompleted:
                    {
                        return true;
                    }
            }
            return false;
        }

        /// <summary>
        /// Server Side:
        /// Sorts the NetworkObjects to assure proper instantiation order of operations for
        /// registered INetworkPrefabInstanceHandler implementations
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private int SortNetworkObjects(NetworkObject first, NetworkObject second)
        {
            var doesFirstHaveHandler = m_NetworkManager.PrefabHandler.ContainsHandler(first);
            var doesSecondHaveHandler = m_NetworkManager.PrefabHandler.ContainsHandler(second);
            if (doesFirstHaveHandler != doesSecondHaveHandler)
            {
                if (doesFirstHaveHandler)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            return 0;
        }

        /// <summary>
        /// Sorts the synchronization order of the NetworkObjects to be serialized
        /// by parents before children.
        /// </summary>
        /// <remarks>
        /// This only handles late joining players. Spawning and nesting several children
        /// dynamically is still handled by the orphaned child list when deserialized out of
        /// hierarchical order (i.e. Spawn parent and child dynamically, parent message is
        /// dropped and re-sent but child object is received and processed)
        /// </remarks>
        private int SortParentedNetworkObjects(NetworkObject first, NetworkObject second)
        {
            // If the first has a parent, move the first down
            if (first.transform.parent != null)
            {
                return 1;
            }
            else // If the second has a parent and the first does not, then move the first up
            if (second.transform.parent != null)
            {
                return -1;
            }
            return 0;
        }


        /// <summary>
        /// Client and Server Side:
        /// Serializes data based on the SceneEvent type (<see cref="SceneEventType"/>)
        /// </summary>
        /// <param name="writer"><see cref="FastBufferWriter"/> to write the scene event data</param>
        internal void Serialize(FastBufferWriter writer)
        {
            // Write the scene event type
            writer.WriteValueSafe(SceneEventType);

            // Write the scene loading mode
            writer.WriteValueSafe(LoadSceneMode);

            // Write the scene event progress Guid
            if (SceneEventType != SceneEventType.Synchronize)
            {
                writer.WriteValueSafe(SceneEventProgressId);
            }

            // Write the scene index and handle
            writer.WriteValueSafe(SceneHash);
            writer.WriteValueSafe(SceneHandle);

            switch (SceneEventType)
            {
                case SceneEventType.Synchronize:
                    {
                        WriteSceneSynchronizationData(writer);
                        break;
                    }
                case SceneEventType.Load:
                    {
                        SerializeScenePlacedObjects(writer);
                        break;
                    }
                case SceneEventType.SynchronizeComplete:
                    {
                        WriteClientSynchronizationResults(writer);
                        break;
                    }
                case SceneEventType.ReSynchronize:
                    {
                        WriteClientReSynchronizationData(writer);
                        break;
                    }
                case SceneEventType.LoadEventCompleted:
                case SceneEventType.UnloadEventCompleted:
                    {
                        WriteSceneEventProgressDone(writer);
                        break;
                    }
            }
        }

        /// <summary>
        /// Server Side:
        /// Called at the end of a <see cref="SceneEventType.Load"/> event once the scene is loaded and scene placed NetworkObjects
        /// have been locally spawned
        /// </summary>
        internal void WriteSceneSynchronizationData(FastBufferWriter writer)
        {
            // Write the scenes we want to load, in the order we want to load them
            writer.WriteValueSafe(ScenesToSynchronize.ToArray());
            writer.WriteValueSafe(SceneHandlesToSynchronize.ToArray());

            // Store our current position in the stream to come back and say how much data we have written
            var positionStart = writer.Position;

            // Size Place Holder -- Start
            // !!NOTE!!: Since this is a placeholder to be set after we know how much we have written,
            // for stream offset purposes this MUST not be a packed value!
            writer.WriteValueSafe((int)0);
            int totalBytes = 0;

            // Write the number of NetworkObjects we are serializing
            BytePacker.WriteValuePacked(writer, m_NetworkObjectsSync.Count);
            // Serialize all NetworkObjects that are spawned
            for (var i = 0; i < m_NetworkObjectsSync.Count; ++i)
            {
                var noStart = writer.Position;
                var sceneObject = m_NetworkObjectsSync[i].GetMessageSceneObject(TargetClientId);
                BytePacker.WriteValuePacked(writer, m_NetworkObjectsSync[i].GetSceneOriginHandle());
                sceneObject.Serialize(writer);
                var noStop = writer.Position;
                totalBytes += (int)(noStop - noStart);
            }

            // Write the number of despawned in-scene placed NetworkObjects
            writer.WriteValueSafe(m_DespawnedInSceneObjectsSync.Count);
            // Write the scene handle and GlobalObjectIdHash value
            for (var i = 0; i < m_DespawnedInSceneObjectsSync.Count; ++i)
            {
                var noStart = writer.Position;
                BytePacker.WriteValuePacked(writer, m_DespawnedInSceneObjectsSync[i].GetSceneOriginHandle());
                BytePacker.WriteValuePacked(writer, m_DespawnedInSceneObjectsSync[i].GlobalObjectIdHash);
                var noStop = writer.Position;
                totalBytes += (int)(noStop - noStart);
            }

            // Size Place Holder -- End
            var positionEnd = writer.Position;
            var bytesWritten = (uint)(positionEnd - (positionStart + sizeof(uint)));
            writer.Seek(positionStart);
            // Write the total size written to the stream by NetworkObjects being serialized
            writer.WriteValueSafe(bytesWritten);
            writer.Seek(positionEnd);
        }

        /// <summary>
        /// Server Side:
        /// Called at the end of a <see cref="SceneEventType.Load"/> event once the scene is loaded and scene placed NetworkObjects
        /// have been locally spawned
        /// Maximum number of objects that could theoretically be synchronized is 65536
        /// </summary>
        internal void SerializeScenePlacedObjects(FastBufferWriter writer)
        {
            var numberOfObjects = (ushort)0;
            var headPosition = writer.Position;

            // Write our count place holder (must not be packed!)
            writer.WriteValueSafe((ushort)0);

            foreach (var keyValuePairByGlobalObjectIdHash in m_NetworkManager.SceneManager.ScenePlacedObjects)
            {
                foreach (var keyValuePairBySceneHandle in keyValuePairByGlobalObjectIdHash.Value)
                {
                    if (keyValuePairBySceneHandle.Value.Observers.Contains(TargetClientId))
                    {
                        // Write our server relative scene handle for the NetworkObject being serialized
                        writer.WriteValueSafe(keyValuePairBySceneHandle.Key);
                        // Serialize the NetworkObject
                        var sceneObject = keyValuePairBySceneHandle.Value.GetMessageSceneObject(TargetClientId);
                        sceneObject.Serialize(writer);
                        numberOfObjects++;
                    }
                }
            }

            // Write the number of despawned in-scene placed NetworkObjects
            writer.WriteValueSafe(m_DespawnedInSceneObjectsSync.Count);
            // Write the scene handle and GlobalObjectIdHash value
            for (var i = 0; i < m_DespawnedInSceneObjectsSync.Count; ++i)
            {
                BytePacker.WriteValuePacked(writer, m_DespawnedInSceneObjectsSync[i].GetSceneOriginHandle());
                BytePacker.WriteValuePacked(writer, m_DespawnedInSceneObjectsSync[i].GlobalObjectIdHash);
            }

            var tailPosition = writer.Position;
            // Reposition to our count position to the head before we wrote our object count
            writer.Seek(headPosition);
            // Write number of NetworkObjects serialized (must not be packed!)
            writer.WriteValueSafe(numberOfObjects);
            // Set our position back to the tail
            writer.Seek(tailPosition);
        }

        /// <summary>
        /// Client and Server Side:
        /// Deserialize data based on the SceneEvent type.
        /// </summary>
        /// <param name="reader"></param>
        internal void Deserialize(FastBufferReader reader)
        {
            reader.ReadValueSafe(out SceneEventType);
            reader.ReadValueSafe(out LoadSceneMode);

            if (SceneEventType != SceneEventType.Synchronize)
            {
                reader.ReadValueSafe(out SceneEventProgressId);
            }

            reader.ReadValueSafe(out SceneHash);
            reader.ReadValueSafe(out SceneHandle);

            switch (SceneEventType)
            {
                case SceneEventType.Synchronize:
                    {
                        CopySceneSynchronizationData(reader);
                        break;
                    }
                case SceneEventType.SynchronizeComplete:
                    {
                        CheckClientSynchronizationResults(reader);
                        break;
                    }
                case SceneEventType.Load:
                    {
                        unsafe
                        {
                            // We store off the trailing in-scene placed serialized NetworkObject data to
                            // be processed once we are done loading.
                            m_HasInternalBuffer = true;
                            // We use Allocator.Persistent since scene loading could take longer than 4 frames
                            InternalBuffer = new FastBufferReader(reader.GetUnsafePtrAtCurrentPosition(), Allocator.Persistent, reader.Length - reader.Position);
                        }
                        break;
                    }
                case SceneEventType.ReSynchronize:
                    {
                        ReadClientReSynchronizationData(reader);
                        break;
                    }
                case SceneEventType.LoadEventCompleted:
                case SceneEventType.UnloadEventCompleted:
                    {
                        ReadSceneEventProgressDone(reader);
                        break;
                    }
            }
        }

        /// <summary>
        /// Client Side:
        /// Prepares for a scene synchronization event and copies the scene synchronization data
        /// into the internal buffer to be used throughout the synchronization process.
        /// </summary>
        /// <param name="reader"></param>
        internal void CopySceneSynchronizationData(FastBufferReader reader)
        {
            m_NetworkObjectsSync.Clear();
            reader.ReadValueSafe(out uint[] scenesToSynchronize);
            reader.ReadValueSafe(out uint[] sceneHandlesToSynchronize);
            ScenesToSynchronize = new Queue<uint>(scenesToSynchronize);
            SceneHandlesToSynchronize = new Queue<uint>(sceneHandlesToSynchronize);

            // is not packed!
            reader.ReadValueSafe(out int sizeToCopy);
            unsafe
            {
                if (!reader.TryBeginRead(sizeToCopy))
                {
                    throw new OverflowException("Not enough space in the buffer to read recorded synchronization data size.");
                }

                m_HasInternalBuffer = true;
                // We use Allocator.Persistent since scene synchronization will most likely take longer than 4 frames
                InternalBuffer = new FastBufferReader(reader.GetUnsafePtrAtCurrentPosition(), Allocator.Persistent, sizeToCopy);
            }
        }

        /// <summary>
        /// Client Side:
        /// This needs to occur at the end of a <see cref="SceneEventType.Load"/> event when the scene has finished loading
        /// Maximum number of objects that could theoretically be synchronized is 65536
        /// </summary>
        internal void DeserializeScenePlacedObjects()
        {
            try
            {
                // is not packed!
                InternalBuffer.ReadValueSafe(out ushort newObjectsCount);

                for (ushort i = 0; i < newObjectsCount; i++)
                {
                    InternalBuffer.ReadValueSafe(out int sceneHandle);
                    // Set our relative scene to the NetworkObject
                    m_NetworkManager.SceneManager.SetTheSceneBeingSynchronized(sceneHandle);

                    // Deserialize the NetworkObject
                    var sceneObject = new NetworkObject.SceneObject();
                    sceneObject.Deserialize(InternalBuffer);
                    NetworkObject.AddSceneObject(sceneObject, InternalBuffer, m_NetworkManager);
                }
                // Now deserialize the despawned in-scene placed NetworkObjects list (if any)
                DeserializeDespawnedInScenePlacedNetworkObjects();
            }
            finally
            {
                InternalBuffer.Dispose();
                m_HasInternalBuffer = false;
            }
        }

        /// <summary>
        /// Client Side:
        /// If there happens to be NetworkObjects in the final Event_Sync_Complete message that are no longer spawned,
        /// the server will compile a list and send back an Event_ReSync message to the client.  This is where the
        /// client handles any returned values by the server.
        /// </summary>
        /// <param name="reader"></param>
        internal void ReadClientReSynchronizationData(FastBufferReader reader)
        {
            reader.ReadValueSafe(out uint[] networkObjectsToRemove);

            if (networkObjectsToRemove.Length > 0)
            {
                var networkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>();
                var networkObjectIdToNetworkObject = new Dictionary<ulong, NetworkObject>();
                foreach (var networkObject in networkObjects)
                {
                    if (!networkObjectIdToNetworkObject.ContainsKey(networkObject.NetworkObjectId))
                    {
                        networkObjectIdToNetworkObject.Add(networkObject.NetworkObjectId, networkObject);
                    }
                }

                foreach (var networkObjectId in networkObjectsToRemove)
                {
                    if (networkObjectIdToNetworkObject.ContainsKey(networkObjectId))
                    {
                        var networkObject = networkObjectIdToNetworkObject[networkObjectId];
                        networkObjectIdToNetworkObject.Remove(networkObjectId);

                        networkObject.IsSpawned = false;
                        if (m_NetworkManager.PrefabHandler.ContainsHandler(networkObject))
                        {
                            if (m_NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
                            {
                                m_NetworkManager.SpawnManager.SpawnedObjects.Remove(networkObjectId);
                            }
                            if (m_NetworkManager.SpawnManager.SpawnedObjectsList.Contains(networkObject))
                            {
                                m_NetworkManager.SpawnManager.SpawnedObjectsList.Remove(networkObject);
                            }
                            NetworkManager.Singleton.PrefabHandler.HandleNetworkPrefabDestroy(networkObject);
                        }
                        else
                        {
                            UnityEngine.Object.DestroyImmediate(networkObject.gameObject);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Server Side:
        /// If there happens to be NetworkObjects in the final Event_Sync_Complete message that are no longer spawned,
        /// the server will compile a list and send back an Event_ReSync message to the client.
        /// </summary>
        /// <param name="writer"></param>
        internal void WriteClientReSynchronizationData(FastBufferWriter writer)
        {
            //Write how many objects need to be removed
            writer.WriteValueSafe(m_NetworkObjectsToBeRemoved.ToArray());
        }

        /// <summary>
        /// Server Side:
        /// Determines if the client needs to be slightly re-synchronized if during the deserialization
        /// process the server finds NetworkObjects that the client still thinks are spawned.
        /// </summary>
        /// <returns></returns>
        internal bool ClientNeedsReSynchronization()
        {
            return (m_NetworkObjectsToBeRemoved.Count > 0);
        }

        /// <summary>
        /// Server Side:
        /// Determines if the client needs to be re-synchronized if during the deserialization
        /// process the server finds NetworkObjects that the client still thinks are spawned but
        /// have since been despawned.
        /// </summary>
        /// <param name="reader"></param>
        internal void CheckClientSynchronizationResults(FastBufferReader reader)
        {
            m_NetworkObjectsToBeRemoved.Clear();
            reader.ReadValueSafe(out uint networkObjectIdCount);
            for (int i = 0; i < networkObjectIdCount; i++)
            {
                reader.ReadValueSafe(out uint networkObjectId);
                if (!m_NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
                {
                    m_NetworkObjectsToBeRemoved.Add(networkObjectId);
                }
            }
        }

        /// <summary>
        /// Client Side:
        /// During the deserialization process of the servers Event_Sync, the client builds a list of
        /// all NetworkObjectIds that were spawned.  Upon responding to the server with the Event_Sync_Complete
        /// this list is included for the server to review over and determine if the client needs a minor resynchronization
        /// of NetworkObjects that might have been despawned while the client was processing the Event_Sync.
        /// </summary>
        /// <param name="writer"></param>
        internal void WriteClientSynchronizationResults(FastBufferWriter writer)
        {
            //Write how many objects were spawned
            writer.WriteValueSafe((uint)m_NetworkObjectsSync.Count);
            foreach (var networkObject in m_NetworkObjectsSync)
            {
                writer.WriteValueSafe((uint)networkObject.NetworkObjectId);
            }
        }

        /// <summary>
        /// For synchronizing any despawned in-scene placed NetworkObjects that were
        /// despawned by the server during synchronization or scene loading
        /// </summary>
        private void DeserializeDespawnedInScenePlacedNetworkObjects()
        {
            // Process all de-spawned in-scene NetworkObjects for this network session
            m_DespawnedInSceneObjects.Clear();
            InternalBuffer.ReadValueSafe(out int despawnedObjectsCount);
            var sceneCache = new Dictionary<int, Dictionary<uint, NetworkObject>>();

            for (int i = 0; i < despawnedObjectsCount; i++)
            {
                // We just need to get the scene
                ByteUnpacker.ReadValuePacked(InternalBuffer, out int networkSceneHandle);
                ByteUnpacker.ReadValuePacked(InternalBuffer, out uint globalObjectIdHash);
                var sceneRelativeNetworkObjects = new Dictionary<uint, NetworkObject>();
                if (!sceneCache.ContainsKey(networkSceneHandle))
                {
                    if (m_NetworkManager.SceneManager.ServerSceneHandleToClientSceneHandle.ContainsKey(networkSceneHandle))
                    {
                        var localSceneHandle = m_NetworkManager.SceneManager.ServerSceneHandleToClientSceneHandle[networkSceneHandle];
                        if (m_NetworkManager.SceneManager.ScenesLoaded.ContainsKey(localSceneHandle))
                        {
                            var objectRelativeScene = m_NetworkManager.SceneManager.ScenesLoaded[localSceneHandle];

                            // Find all active and non-active in-scene placed NetworkObjects
                            var inSceneNetworkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>(includeInactive: true).Where((c) =>
                            c.GetSceneOriginHandle() == localSceneHandle && (c.IsSceneObject != false)).ToList();

                            foreach (var inSceneObject in inSceneNetworkObjects)
                            {
                                if (!sceneRelativeNetworkObjects.ContainsKey(inSceneObject.GlobalObjectIdHash))
                                {
                                    sceneRelativeNetworkObjects.Add(inSceneObject.GlobalObjectIdHash, inSceneObject);
                                }
                            }
                            // Add this to a cache so we don't have to run this potentially multiple times (nothing will spawn or despawn during this time
                            sceneCache.Add(networkSceneHandle, sceneRelativeNetworkObjects);
                        }
                        else
                        {
                            UnityEngine.Debug.LogError($"In-Scene NetworkObject GlobalObjectIdHash ({globalObjectIdHash}) cannot find its relative local scene handle {localSceneHandle}!");
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"In-Scene NetworkObject GlobalObjectIdHash ({globalObjectIdHash}) cannot find its relative NetworkSceneHandle {networkSceneHandle}!");
                    }
                }
                else // Use the cached NetworkObjects if they exist
                {
                    sceneRelativeNetworkObjects = sceneCache[networkSceneHandle];
                }

                // Now find the in-scene NetworkObject with the current GlobalObjectIdHash we are looking for
                if (sceneRelativeNetworkObjects.ContainsKey(globalObjectIdHash))
                {
                    // Since this is a NetworkObject that was never spawned, we just need to send a notification
                    // out that it was despawned so users can make adjustments
                    sceneRelativeNetworkObjects[globalObjectIdHash].InvokeBehaviourNetworkDespawn();
                    if (!m_NetworkManager.SceneManager.ScenePlacedObjects.ContainsKey(globalObjectIdHash))
                    {
                        m_NetworkManager.SceneManager.ScenePlacedObjects.Add(globalObjectIdHash, new Dictionary<int, NetworkObject>());
                    }

                    if (!m_NetworkManager.SceneManager.ScenePlacedObjects[globalObjectIdHash].ContainsKey(sceneRelativeNetworkObjects[globalObjectIdHash].GetSceneOriginHandle()))
                    {
                        m_NetworkManager.SceneManager.ScenePlacedObjects[globalObjectIdHash].Add(sceneRelativeNetworkObjects[globalObjectIdHash].GetSceneOriginHandle(), sceneRelativeNetworkObjects[globalObjectIdHash]);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError($"In-Scene NetworkObject GlobalObjectIdHash ({globalObjectIdHash}) could not be found!");
                }
            }
        }

        /// <summary>
        /// Client Side:
        /// During the processing of a server sent Event_Sync, this method will be called for each scene once
        /// it is finished loading.  The client will also build a list of NetworkObjects that it spawned during
        /// this process which will be used as part of the Event_Sync_Complete response.
        /// </summary>
        /// <param name="networkManager"></param>
        internal void SynchronizeSceneNetworkObjects(NetworkManager networkManager)
        {
            try
            {
                // Process all spawned NetworkObjects for this network session
                ByteUnpacker.ReadValuePacked(InternalBuffer, out int newObjectsCount);


                for (int i = 0; i < newObjectsCount; i++)
                {
                    // We want to make sure for each NetworkObject we have the appropriate scene selected as the scene that is
                    // currently being synchronized.  This assures in-scene placed NetworkObjects will use the right NetworkObject
                    // from the list of populated <see cref="NetworkSceneManager.ScenePlacedObjects"/>
                    ByteUnpacker.ReadValuePacked(InternalBuffer, out int handle);
                    m_NetworkManager.SceneManager.SetTheSceneBeingSynchronized(handle);

                    var sceneObject = new NetworkObject.SceneObject();
                    sceneObject.Deserialize(InternalBuffer);

                    var spawnedNetworkObject = NetworkObject.AddSceneObject(sceneObject, InternalBuffer, networkManager);
                    if (!m_NetworkObjectsSync.Contains(spawnedNetworkObject))
                    {
                        m_NetworkObjectsSync.Add(spawnedNetworkObject);
                    }
                }

                // Now deserialize the despawned in-scene placed NetworkObjects list (if any)
                DeserializeDespawnedInScenePlacedNetworkObjects();

            }
            finally
            {
                InternalBuffer.Dispose();
                m_HasInternalBuffer = false;
            }
        }

        /// <summary>
        /// Writes the all clients loaded or unloaded completed and timed out lists
        /// </summary>
        /// <param name="writer"></param>
        internal void WriteSceneEventProgressDone(FastBufferWriter writer)
        {
            writer.WriteValueSafe((ushort)ClientsCompleted.Count);
            foreach (var clientId in ClientsCompleted)
            {
                writer.WriteValueSafe(clientId);
            }

            writer.WriteValueSafe((ushort)ClientsTimedOut.Count);
            foreach (var clientId in ClientsTimedOut)
            {
                writer.WriteValueSafe(clientId);
            }
        }

        /// <summary>
        /// Reads the all clients loaded or unloaded completed and timed out lists
        /// </summary>
        /// <param name="reader"></param>
        internal void ReadSceneEventProgressDone(FastBufferReader reader)
        {
            reader.ReadValueSafe(out ushort completedCount);
            ClientsCompleted = new List<ulong>();
            for (int i = 0; i < completedCount; i++)
            {
                reader.ReadValueSafe(out ulong clientId);
                ClientsCompleted.Add(clientId);
            }

            reader.ReadValueSafe(out ushort timedOutCount);
            ClientsTimedOut = new List<ulong>();
            for (int i = 0; i < timedOutCount; i++)
            {
                reader.ReadValueSafe(out ulong clientId);
                ClientsTimedOut.Add(clientId);
            }
        }

        /// <summary>
        /// Used to release the pooled network buffer
        /// </summary>
        public void Dispose()
        {
            if (m_HasInternalBuffer)
            {
                InternalBuffer.Dispose();
                m_HasInternalBuffer = false;
            }
        }

        /// <summary>
        /// Constructor for SceneEventData
        /// </summary>
        internal SceneEventData(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            SceneEventId = XXHash.Hash32(Guid.NewGuid().ToString());
        }
    }
}
