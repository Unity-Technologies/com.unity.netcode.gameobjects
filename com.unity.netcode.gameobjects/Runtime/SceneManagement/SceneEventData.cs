using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEngine.SceneManagement;


namespace Unity.Netcode
{
    /// <summary>
    /// The different types of scene events communicated between a server and client. <br/>
    /// Used by <see cref="NetworkSceneManager"/> for <see cref="SceneEventMessage"/> messages.<br/>
    /// <em>Note: This is only when <see cref="NetworkConfig.EnableSceneManagement"/> is enabled.</em><br/>
    /// See also: <br/>
    /// <see cref="SceneEvent"/>
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
        /// <summary>
        /// Synchronizes clients when the active scene has changed
        /// See: <see cref="NetworkObject.ActiveSceneSynchronization"/>
        /// </summary>
        ActiveSceneChanged,
        /// <summary>
        /// Synchronizes clients when one or more NetworkObjects are migrated into a new scene
        /// See: <see cref="NetworkObject.SceneMigrationSynchronization"/>
        /// </summary>
        ObjectSceneChanged,
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

        internal uint ActiveSceneHash;
        internal uint SceneHash;
        internal NetworkSceneHandle SceneHandle;

        // Used by the client during synchronization
        internal uint ClientSceneHash;
        internal NetworkSceneHandle NetworkSceneHandle;

        /// Only used for <see cref="SceneEventType.Synchronize"/> scene events, this assures permissions when writing
        /// NetworkVariable information.  If that process changes, then we need to update this
        /// In distributed authority mode this is used to route messages to the appropriate destination client
        internal ulong TargetClientId;
        /// Only used with a DAHost
        internal ulong SenderClientId;

        private Dictionary<uint, List<NetworkObject>> m_SceneNetworkObjects;
        private Dictionary<uint, long> m_SceneNetworkObjectDataOffsets;

        /// <summary>
        /// Client or Server Side:
        /// Client side: Generates a list of all NetworkObjects by their NetworkObjectId that was spawned during th synchronization process
        /// Server side: Compares list from client to make sure client didn't drop a message about a NetworkObject being despawned while it
        /// was synchronizing (if so server will send another message back to the client informing the client of NetworkObjects to remove)
        /// spawned during an initial synchronization.
        /// </summary>
        private readonly List<NetworkObject> m_NetworkObjectsSync = new List<NetworkObject>();

        private readonly List<NetworkObject> m_DespawnedInSceneObjectsSync = new List<NetworkObject>();

        /// <summary>
        /// Server Side Re-Synchronization:
        /// If there happens to be NetworkObjects in the final Event_Sync_Complete message that are no longer spawned,
        /// the server will compile a list and send back an Event_ReSync message to the client.
        /// </summary>
        private readonly List<ulong> m_NetworkObjectsToBeRemoved = new List<ulong>();

        private bool m_HasInternalBuffer;
        private FastBufferReader m_InternalBuffer;

        private readonly NetworkManager m_NetworkManager;

        internal List<ulong> ClientsCompleted;
        internal List<ulong> ClientsTimedOut;

        internal Queue<uint> ScenesToSynchronize;
        internal Queue<NetworkSceneHandle> SceneHandlesToSynchronize;

        internal LoadSceneMode ClientSynchronizationMode;


        /// <summary>
        /// Server Side:
        /// Add a scene and its handle to the list of scenes the client should load before synchronizing
        /// Since scene handles are not the same per instance, the client builds a server scene handle to
        /// client scene handle lookup table.
        /// Why include the scene handle? In order to support loading of the same additive scene more than once
        /// we must distinguish which scene we are talking about when the server tells the client to unload a scene.
        /// The server will always communicate its local relative scene's handle and the client will determine its
        /// local relative handle from the table being built.
        /// Look for <see cref="NetworkSceneManager.ServerSceneHandleToClientSceneHandle"/> usage to see where
        /// entries are being added to or removed from the table
        /// </summary>
        /// <param name="sceneHash"></param>
        /// <param name="sceneHandle"></param>
        internal void AddSceneToSynchronize(uint sceneHash, NetworkSceneHandle sceneHandle)
        {
            ScenesToSynchronize.Enqueue(sceneHash);
            SceneHandlesToSynchronize.Enqueue(sceneHandle);
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
        internal NetworkSceneHandle GetNextSceneSynchronizationHandle()
        {
            return SceneHandlesToSynchronize.Dequeue();
        }

        internal bool IsStartingSynchronization;
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
                SceneHandlesToSynchronize = new Queue<NetworkSceneHandle>();
            }
            else
            {
                SceneHandlesToSynchronize.Clear();
            }
            ForwardSynchronization = false;
        }

        /// <summary>
        /// Used with SortParentedNetworkObjects to sort the children of the root parent NetworkObject
        /// </summary>
        /// <param name="first">object to be sorted</param>
        /// <param name="second">object to be compared to for sorting the first object</param>
        /// <returns></returns>
        private int SortChildrenNetworkObjects(NetworkObject first, NetworkObject second)
        {
            var firstParent = first.GetCachedParent()?.GetComponent<NetworkObject>();
            // If the second is the first's parent then move the first down
            if (firstParent != null && firstParent == second)
            {
                return 1;
            }

            var secondParent = second.GetCachedParent()?.GetComponent<NetworkObject>();
            // If the first is the second's parent then move the first up
            if (secondParent != null && secondParent == first)
            {
                return -1;
            }

            // Otherwise, don't move the first at all
            return 0;
        }

        /// <summary>
        /// Sorts the synchronization order of the NetworkObjects to be serialized
        /// by parents before children order
        /// </summary>
        private void SortParentedNetworkObjects()
        {
            var networkObjectList = m_NetworkObjectsSync.ToList();
            foreach (var networkObject in networkObjectList)
            {
                // Find only the root parent NetworkObjects
                if (networkObject.transform.childCount > 0 && networkObject.transform.parent == null)
                {
                    // Get all child NetworkObjects of the root
                    var childNetworkObjects = networkObject.GetComponentsInChildren<NetworkObject>().ToList();

                    childNetworkObjects.Sort(SortChildrenNetworkObjects);

                    // Remove the root from the children list
                    childNetworkObjects.Remove(networkObject);

                    // Remove the root's children from the primary list
                    foreach (var childObject in childNetworkObjects)
                    {
                        m_NetworkObjectsSync.Remove(childObject);
                    }
                    // Insert or Add the sorted children list
                    var nextIndex = m_NetworkObjectsSync.IndexOf(networkObject) + 1;
                    if (nextIndex == m_NetworkObjectsSync.Count)
                    {
                        m_NetworkObjectsSync.AddRange(childNetworkObjects);
                    }
                    else
                    {
                        m_NetworkObjectsSync.InsertRange(nextIndex, childNetworkObjects);
                    }
                }
            }
        }

        internal void AddSpawnedNetworkObjects()
        {
            m_NetworkObjectsSync.Clear();
            // If distributed authority mode and sending to the service, then ignore observers
            var distributedAuthoritySendingToService = m_NetworkManager.DistributedAuthorityMode && TargetClientId == NetworkManager.ServerClientId;
            foreach (var sobj in m_NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                var spawnedObject = sobj;
                // Don't synchronize objects that have pending visibility as that will be sent as a CreateObjectMessage towards the end of the current frame
                if (TargetClientId != NetworkManager.ServerClientId && m_NetworkManager.SpawnManager.IsObjectVisibilityPending(TargetClientId, ref spawnedObject))
                {
                    continue;
                }
                if (sobj.Observers.Contains(TargetClientId) || distributedAuthoritySendingToService)
                {
                    m_NetworkObjectsSync.Add(sobj);
                }
            }
            SortObjectsToSync();
        }

        /// <summary>
        /// Used to order the object serialization for both synchronization and scene loading
        /// </summary>
        private void SortObjectsToSync()
        {
            // Sort by INetworkPrefabInstanceHandler implementation before the
            // NetworkObjects spawned by the implementation
            m_NetworkObjectsSync.Sort(SortNetworkObjects);

            // The last thing we sort is parents before children
            SortParentedNetworkObjects();

            // This is useful to know what NetworkObjects a client is going to be synchronized with
            // as well as the order in which they will be deserialized
            if (NetworkLog.Config.LogSerializationOrder && m_NetworkManager.LogLevel == LogLevel.Developer)
            {
                var messageBuilder = new StringBuilder(0xFFFF);
                messageBuilder.AppendLine("[Server-Side Client-Synchronization] NetworkObject serialization order:");
                foreach (var networkObject in m_NetworkObjectsSync)
                {
                    messageBuilder.AppendLine($"{networkObject.name}");
                }
                NetworkLog.LogInfo(messageBuilder.ToString());
            }
        }

        internal void AddDespawnedInSceneNetworkObjects()
        {
            m_DespawnedInSceneObjectsSync.Clear();
            // Find all active and non-active in-scene placed NetworkObjects
            foreach (var scene in m_NetworkManager.SceneManager.ScenesLoaded.Values)
            {
                // Ignore invalid scenes
                if (!scene.IsValid())
                {
                    continue;
                }
                foreach (var networkObject in FindObjects.FromSceneByType<NetworkObject>(scene, true))
                {
                    if (networkObject.InScenePlaced && networkObject.NetworkManagerOwner == m_NetworkManager && !networkObject.IsSpawned)
                    {
                        m_DespawnedInSceneObjectsSync.Add(networkObject);
                    }
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
            if (!m_SceneNetworkObjects.TryGetValue(sceneIndex, out var sceneNetworkObject))
            {
                sceneNetworkObject = new List<NetworkObject>();
                m_SceneNetworkObjects.Add(sceneIndex, sceneNetworkObject);
            }
            sceneNetworkObject.Add(networkObject);
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
                case SceneEventType.ActiveSceneChanged:
                case SceneEventType.ObjectSceneChanged:
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

        internal bool EnableSerializationLogs = false;

        private void LogArray(byte[] data, int start = 0, int stop = 0, StringBuilder builder = null)
        {
            var usingExternalBuilder = builder != null;
            if (!usingExternalBuilder)
            {
                builder = new StringBuilder();
            }

            if (stop == 0)
            {
                stop = data.Length;
            }

            builder.AppendLine($"[Start Data Dump][Start = {start}][Stop = {stop}] Size ({stop - start})");
            for (int i = start; i < stop; i++)
            {
                builder.Append($"{data[i]:X2} ");
            }
            builder.Append("\n");

            if (!usingExternalBuilder)
            {
                UnityEngine.Debug.Log(builder.ToString());
            }
        }

        internal bool ForwardSynchronization;

        /// <summary>
        /// Client and Server Side:
        /// Serializes data based on the SceneEvent type (<see cref="SceneEventType"/>)
        /// </summary>
        /// <param name="writer"><see cref="FastBufferWriter"/> to write the scene event data</param>
        internal void Serialize(FastBufferWriter writer)
        {
            // Write the scene event type
            writer.WriteValueSafe(SceneEventType);

            if (m_NetworkManager.DistributedAuthorityMode)
            {
                BytePacker.WriteValueBitPacked(writer, TargetClientId);
                BytePacker.WriteValueBitPacked(writer, SenderClientId);
            }

            if (SceneEventType == SceneEventType.ActiveSceneChanged)
            {
                writer.WriteValueSafe(ActiveSceneHash);
                return;
            }

            if (SceneEventType == SceneEventType.ObjectSceneChanged)
            {
                SerializeObjectsMovedIntoNewScene(writer);
                return;
            }

            // Write the scene loading mode
            writer.WriteValueSafe((byte)LoadSceneMode);

            // Write the scene event progress Guid
            if (SceneEventType != SceneEventType.Synchronize)
            {
                writer.WriteValueSafe(SceneEventProgressId);
            }
            else
            {
                writer.WriteValueSafe(ClientSynchronizationMode);
            }

            // Write the scene index and handle
            writer.WriteValueSafe(SceneHash);
            writer.WriteValueSafe(SceneHandle);

            switch (SceneEventType)
            {
                case SceneEventType.Synchronize:
                    {
                        writer.WriteValueSafe(ActiveSceneHash);

                        WriteSceneSynchronizationData(writer);

                        if (EnableSerializationLogs)
                        {
                            LogArray(writer.ToArray(), 0, writer.Length);
                        }
                        break;
                    }
                case SceneEventType.Load:
                    {
                        if (m_NetworkManager.DistributedAuthorityMode && IsForwarding && m_NetworkManager.DAHost)
                        {
                            CopyInternalBuffer(ref writer);
                        }
                        else
                        {
                            SerializeScenePlacedObjects(writer);
                        }
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

        private unsafe void CopyInternalBuffer(ref FastBufferWriter writer)
        {
            writer.WriteBytesSafe(m_InternalBuffer.GetUnsafePtrAtCurrentPosition(), m_InternalBuffer.Length);
        }

        /// <summary>
        /// Server Side:
        /// Called at the end of a <see cref="SceneEventType.Load"/> event once the scene is loaded and scene placed NetworkObjects
        /// have been locally spawned
        /// </summary>
        private void WriteSceneSynchronizationData(FastBufferWriter writer)
        {
            StringBuilder builder = null;
            if (EnableSerializationLogs)
            {
                builder = new StringBuilder();
                builder.AppendLine($"[Write][Synchronize-Start][WPos: {writer.Position}] Begin:");
            }
            // Write the scenes we want to load, in the order we want to load them
            writer.WriteValueSafe(ScenesToSynchronize.ToArray());
            writer.WriteValueSafe(SceneHandlesToSynchronize.ToArray());
            // Store our current position in the stream to come back and say how much data we have written
            var positionStart = writer.Position;

            if (m_NetworkManager.DistributedAuthorityMode && ForwardSynchronization && m_NetworkManager.DAHost)
            {
                writer.WriteValueSafe(m_InternalBufferSize);
                CopyInternalBuffer(ref writer);
                if (EnableSerializationLogs)
                {
                    LogArray(writer.ToArray(), positionStart);
                }
                return;
            }

            // Size Placeholder -- Start
            // !!NOTE!!: Since this is a placeholder to be set after we know how much we have written,
            // for stream offset purposes this MUST not be a packed value!
            writer.WriteValueSafe(0);

            // Write the number of NetworkObjects we are serializing
            writer.WriteValueSafe(m_NetworkObjectsSync.Count);
            if (EnableSerializationLogs)
            {
                builder.AppendLine($"[Synchronize Objects][positionStart: {positionStart}][WPos: {writer.Position}][NO-Count: {m_NetworkObjectsSync.Count}] Begin:");
            }
            var distributedAuthority = m_NetworkManager.DistributedAuthorityMode;

            // Serialize all NetworkObjects that are spawned
            foreach (var networkObject in m_NetworkObjectsSync)
            {
                var noStart = writer.Position;
                // In distributed authority mode, we send the currently known observers of each NetworkObject to the client being synchronized.
                var serializedObject = networkObject.SerializeSpawnedObject(TargetClientId, distributedAuthority);

                serializedObject.Serialize(writer);
                var noStop = writer.Position;
                if (EnableSerializationLogs)
                {
                    var offStart = noStart - (positionStart + sizeof(int));
                    var offStop = noStop - (positionStart + sizeof(int));
                    builder.AppendLine($"[Head: {offStart}][Tail: {offStop}][Size: {offStop - offStart}][{networkObject.name}][NID-{networkObject.NetworkObjectId}][Children: {networkObject.ChildNetworkBehaviours.Count}]");
                    LogArray(writer.ToArray(), noStart, noStop, builder);
                }
            }
            if (EnableSerializationLogs)
            {
                UnityEngine.Debug.Log(builder.ToString());
            }

            // Write the number of despawned in-scene placed NetworkObjects
            writer.WriteValueSafe(m_DespawnedInSceneObjectsSync.Count);
            // Write the scene handle and GlobalObjectIdHash value
            foreach (var despawnedInSceneObject in m_DespawnedInSceneObjectsSync)
            {
                writer.WriteValueSafe(despawnedInSceneObject.GetSceneOriginHandle());
                writer.WriteValueSafe(despawnedInSceneObject.GlobalObjectIdHash);
            }

            // Size Placeholder -- End
            var positionEnd = writer.Position;
            var bytesWritten = (uint)(positionEnd - (positionStart + sizeof(uint)));
            writer.Seek(positionStart);
            // Write the total size written to the stream by NetworkObjects being serialized
            writer.WriteValueSafe(bytesWritten);
            writer.Seek(positionEnd);
            if (EnableSerializationLogs)
            {
                LogArray(writer.ToArray(), positionStart);
            }
        }

        /// <summary>
        /// Server Side:
        /// Called at the end of a <see cref="SceneEventType.Load"/> event once the scene is loaded and scene placed NetworkObjects
        /// have been locally spawned
        /// Maximum number of objects that could theoretically be synchronized is 65536
        /// </summary>
        private void SerializeScenePlacedObjects(FastBufferWriter writer)
        {
            ushort numberOfObjects = 0;
            var headPosition = writer.Position;

            // Write our count placeholder (must not be packed!)
            writer.WriteValueSafe((ushort)0);
            var distributedAuthority = m_NetworkManager.DistributedAuthorityMode;
            // If distributed authority mode and sending to the service, then ignore observers
            var distributedAuthoritySendingToService = distributedAuthority && TargetClientId == NetworkManager.ServerClientId;

            // Clear our objects to sync and build a list of the in-scene placed NetworkObjects instantiated and spawned locally
            m_NetworkObjectsSync.Clear();
            foreach (var keyValuePairByGlobalObjectIdHash in m_NetworkManager.SceneManager.ScenePlacedObjects)
            {
                foreach (var keyValuePairBySceneHandle in keyValuePairByGlobalObjectIdHash.Value)
                {
                    if (keyValuePairBySceneHandle.Value.Observers.Contains(TargetClientId) || distributedAuthoritySendingToService)
                    {
                        m_NetworkObjectsSync.Add(keyValuePairBySceneHandle.Value);
                    }
                }
            }

            // Sort the objects to sync based on parenting hierarchy
            SortObjectsToSync();

            // Serialize the sorted objects to sync.
            foreach (var objectToSync in m_NetworkObjectsSync)
            {
                // Serialize the NetworkObject
                var serializedObject = objectToSync.SerializeSpawnedObject(TargetClientId, distributedAuthority);
                serializedObject.Serialize(writer);
                numberOfObjects++;
            }

            // Write the number of despawned in-scene placed NetworkObjects
            writer.WriteValueSafe(m_DespawnedInSceneObjectsSync.Count);
            // Write the scene handle and GlobalObjectIdHash value
            foreach (var despawnedInSceneObject in m_DespawnedInSceneObjectsSync)
            {
                writer.WriteValueSafe(despawnedInSceneObject.GetSceneOriginHandle());
                writer.WriteValueSafe(despawnedInSceneObject.GlobalObjectIdHash);
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
            if (m_NetworkManager.DistributedAuthorityMode)
            {
                ByteUnpacker.ReadValueBitPacked(reader, out TargetClientId);
                ByteUnpacker.ReadValueBitPacked(reader, out SenderClientId);
            }

            if (SceneEventType == SceneEventType.ActiveSceneChanged)
            {
                reader.ReadValueSafe(out ActiveSceneHash);
                return;
            }

            if (SceneEventType == SceneEventType.ObjectSceneChanged)
            {
                // Defer these scene event types if a client hasn't finished synchronizing
                if (!m_NetworkManager.IsConnectedClient)
                {
                    DeferObjectsMovedIntoNewScene(reader);
                }
                else
                {
                    DeserializeObjectsMovedIntoNewScene(reader);
                }
                return;
            }

            reader.ReadValueSafe(out byte loadSceneMode);
            LoadSceneMode = (LoadSceneMode)loadSceneMode;

            if (SceneEventType != SceneEventType.Synchronize)
            {
                reader.ReadValueSafe(out SceneEventProgressId);
            }
            else
            {
                reader.ReadValueSafe(out ClientSynchronizationMode);
            }

            reader.ReadValueSafe(out SceneHash);
            reader.ReadValueSafe(out SceneHandle);

            switch (SceneEventType)
            {
                case SceneEventType.Synchronize:
                    {
                        reader.ReadValueSafe(out ActiveSceneHash);
                        if (EnableSerializationLogs)
                        {
                            LogArray(reader.ToArray(), 0, reader.Length);
                        }
                        CopySceneSynchronizationData(reader);
                        IsStartingSynchronization = true;
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
                            m_InternalBuffer = new FastBufferReader(reader.GetUnsafePtrAtCurrentPosition(), Allocator.Persistent, reader.Length - reader.Position);
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

        private int m_InternalBufferSize;

        /// <summary>
        /// Client Side:
        /// Prepares for a scene synchronization event and copies the scene synchronization data
        /// into the internal buffer to be used throughout the synchronization process.
        /// </summary>
        /// <param name="reader"></param>
        private void CopySceneSynchronizationData(FastBufferReader reader)
        {
            m_NetworkObjectsSync.Clear();
            reader.ReadValueSafe(out uint[] scenesToSynchronize);
            reader.ReadValueSafe(out NetworkSceneHandle[] sceneHandlesToSynchronize);
            ScenesToSynchronize = new Queue<uint>(scenesToSynchronize);
            SceneHandlesToSynchronize = new Queue<NetworkSceneHandle>(sceneHandlesToSynchronize);


            // is not packed!
            reader.ReadValueSafe(out int sizeToCopy);
            m_InternalBufferSize = sizeToCopy;

            unsafe
            {
                if (!reader.TryBeginRead(sizeToCopy))
                {
                    throw new OverflowException("Not enough space in the buffer to read recorded synchronization data size.");
                }

                m_HasInternalBuffer = true;
                // We use Allocator.Persistent since scene synchronization will most likely take longer than 4 frames
                m_InternalBuffer = new FastBufferReader(reader.GetUnsafePtrAtCurrentPosition(), Allocator.Persistent, sizeToCopy);
                if (EnableSerializationLogs)
                {
                    LogArray(m_InternalBuffer.ToArray());
                }
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
                m_InternalBuffer.ReadValueSafe(out ushort newObjectsCount);
                var sceneObjects = new List<NetworkObject>();
                for (ushort i = 0; i < newObjectsCount; i++)
                {
                    var serializedObject = new NetworkObject.SerializedObject();
                    serializedObject.Deserialize(m_InternalBuffer);

                    if (serializedObject.IsSceneObject)
                    {
                        // Set our relative scene to the NetworkObject
                        m_NetworkManager.SceneManager.SetTheSceneBeingSynchronized(serializedObject.NetworkSceneHandle);
                    }

                    var networkObject = NetworkObject.DeserializeAndSpawnObject(serializedObject, m_InternalBuffer, m_NetworkManager);

                    if (serializedObject.IsSceneObject && networkObject != null)
                    {
                        sceneObjects.Add(networkObject);
                    }
                }
                // Now deserialize the despawned in-scene placed NetworkObjects list (if any)
                DeserializeDespawnedInScenePlacedNetworkObjects();

                // Notify all newly spawned in-scene placed NetworkObjects that all in-scene placed
                // NetworkObjects have been spawned.
                foreach (var networkObject in sceneObjects)
                {
                    networkObject.InternalInSceneNetworkObjectsSpawned();
                }
            }
            finally
            {
                m_InternalBuffer.Dispose();
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
        private void ReadClientReSynchronizationData(FastBufferReader reader)
        {
            reader.ReadValueSafe(out uint[] networkObjectsToRemove);

            if (networkObjectsToRemove.Length > 0)
            {
                var networkObjects = FindObjects.ByType<NetworkObject>(orderByIdentifier: true);
                var networkObjectIdToNetworkObject = new Dictionary<ulong, NetworkObject>();
                foreach (var networkObject in networkObjects)
                {
                    // If the NetworkObject isn't spawned then we don't need to destroy it
                    if (networkObject.IsSpawned)
                    {
                        networkObjectIdToNetworkObject.TryAdd(networkObject.NetworkObjectId, networkObject);
                    }
                }

                foreach (var networkObjectId in networkObjectsToRemove)
                {
                    if (networkObjectIdToNetworkObject.TryGetValue(networkObjectId, out var networkObject))
                    {
                        if (m_NetworkManager.LogLevel <= LogLevel.Developer)
                        {
                            NetworkLog.LogWarning($"[ReadClientReSynchronizationData][{networkObject.name}] Despawning and destroying {nameof(NetworkObject)}.");
                        }
                        m_NetworkManager.SpawnManager.OnDespawnObject(networkObject, true, true);
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
        private void WriteClientReSynchronizationData(FastBufferWriter writer)
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
            return m_NetworkObjectsToBeRemoved.Count > 0;
        }

        /// <summary>
        /// All clients:
        /// Determines if the client needs to be re-synchronized if during the deserialization
        /// process the server finds NetworkObjects that the client still thinks are spawned but
        /// have since been despawned.
        /// </summary>
        /// <param name="reader"></param>
        private void CheckClientSynchronizationResults(FastBufferReader reader)
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
        /// all NetworkObjectIds that were spawned. Upon responding to the server with the Event_Sync_Complete,
        /// this list is included for the server to review over and determine if the client needs a minor resynchronization
        /// of NetworkObjects that might have been despawned while the client was processing the Event_Sync.
        /// </summary>
        /// <param name="writer"></param>
        private void WriteClientSynchronizationResults(FastBufferWriter writer)
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
            m_InternalBuffer.ReadValueSafe(out int despawnedObjectsCount);
            var sceneCache = new Dictionary<NetworkSceneHandle, Dictionary<uint, NetworkObject>>();

            for (int i = 0; i < despawnedObjectsCount; i++)
            {
                // We just need to get the scene
                m_InternalBuffer.ReadValueSafe(out NetworkSceneHandle networkSceneHandle);
                m_InternalBuffer.ReadValueSafe(out uint globalObjectIdHash);

                // Check if we already have processed the objects in this scene
                if (!sceneCache.TryGetValue(networkSceneHandle, out var sceneRelativeNetworkObjects))
                {
                    // If we haven't already cached the objects in this scene, build the cache
                    sceneRelativeNetworkObjects = new Dictionary<uint, NetworkObject>();
                    if (m_NetworkManager.SceneManager.ServerSceneHandleToClientSceneHandle.TryGetValue(networkSceneHandle, out var localSceneHandle))
                    {
                        if (m_NetworkManager.SceneManager.ScenesLoaded.TryGetValue(localSceneHandle, out var objectRelativeScene))
                        {
                            foreach (var networkObject in FindObjects.FromSceneByType<NetworkObject>(objectRelativeScene, true))
                            {
                                if (networkObject.InScenePlaced)
                                {
                                    sceneRelativeNetworkObjects.TryAdd(networkObject.GlobalObjectIdHash, networkObject);
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

                // Now find the in-scene NetworkObject with the current GlobalObjectIdHash we are looking for
                if (sceneRelativeNetworkObjects.TryGetValue(globalObjectIdHash, out var despawnedObject))
                {
                    // Set the owner of this network object
                    despawnedObject.NetworkManagerOwner = m_NetworkManager;

                    // Since this is a NetworkObject that was never spawned, we just need to send a notification
                    // out that it was despawned so users can make adjustments
                    despawnedObject.InvokeBehaviourNetworkDespawn();

                    m_NetworkManager.SceneManager.ScenePlacedObjects.TryAdd(globalObjectIdHash, new Dictionary<NetworkSceneHandle, NetworkObject>());

                    m_NetworkManager.SceneManager.ScenePlacedObjects[globalObjectIdHash].TryAdd(despawnedObject.GetSceneOriginHandle(), despawnedObject);
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
            StringBuilder builder = null;
            if (EnableSerializationLogs)
            {
                builder = new StringBuilder();
            }

            try
            {
                // Process all spawned NetworkObjects for this network session
                m_InternalBuffer.ReadValueSafe(out int newObjectsCount);
                if (EnableSerializationLogs)
                {
                    builder.AppendLine($"[Read][Synchronize Objects][WPos: {m_InternalBuffer.Position}][NO-Count: {newObjectsCount}] Begin:");
                }

                for (int i = 0; i < newObjectsCount; i++)
                {
                    var noStart = m_InternalBuffer.Position;
                    var serializedObject = new NetworkObject.SerializedObject();
                    serializedObject.Deserialize(m_InternalBuffer);

#if UNIFIED_NETCODE
                    // This handles the case where a NetworkObject is serialized with a ghost component but the ghost isn't actually included in
                    // the spawn message and won't be spawned by the client until later in the N4E synchronization process. In this case, we need
                    // to defer the deserialization of the NetworkObject until the ghost is spawned and we have an instance to deserialize this
                    // information during synchronization.
                    if (serializedObject.HasGhost)
                    {
                        if (networkManager.SpawnManager.GhostSpawnManager.ShouldDeferGhostSceneObject(serializedObject, m_InternalBuffer))
                        {
                            continue;
                        }
                    }
#endif

                    // If the sceneObject is in-scene placed, then set the scene being synchronized
                    if (serializedObject.IsSceneObject)
                    {
                        m_NetworkManager.SceneManager.SetTheSceneBeingSynchronized(serializedObject.NetworkSceneHandle);
                    }
                    var spawnedNetworkObject = NetworkObject.DeserializeAndSpawnObject(serializedObject, m_InternalBuffer, networkManager);
                    if (spawnedNetworkObject == null)
                    {
                        continue;
                    }


                    if (EnableSerializationLogs)
                    {
                        var noStop = m_InternalBuffer.Position;
                        builder.AppendLine($"[Head: {noStart}][Tail: {noStop}][Size: {noStop - noStart}][{spawnedNetworkObject.name}][NID-{spawnedNetworkObject.NetworkObjectId}][Children: {spawnedNetworkObject.ChildNetworkBehaviours.Count}]");
                        LogArray(m_InternalBuffer.ToArray(), noStart, noStop, builder);
                    }
                    // If we failed to deserialize the NetworkObject then don't add null to the list
                    if (!m_NetworkObjectsSync.Contains(spawnedNetworkObject))
                    {
                        m_NetworkObjectsSync.Add(spawnedNetworkObject);
                    }
                }
                if (EnableSerializationLogs)
                {
                    UnityEngine.Debug.Log(builder.ToString());
                }

                // Notify that all in-scene placed NetworkObjects have been spawned
                foreach (var networkObject in m_NetworkObjectsSync)
                {
                    if (networkObject.IsSpawned && networkObject.InScenePlaced)
                    {
                        networkObject.InternalInSceneNetworkObjectsSpawned();
                    }
                }

                // Now deserialize the despawned in-scene placed NetworkObjects list (if any)
                DeserializeDespawnedInScenePlacedNetworkObjects();

            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                if (EnableSerializationLogs)
                {
                    UnityEngine.Debug.Log(builder.ToString());
                }
            }
            finally
            {
                m_InternalBuffer.Dispose();
                m_HasInternalBuffer = false;
            }
        }

        /// <summary>
        /// Writes the all clients loaded or unloaded completed and timed out lists
        /// </summary>
        /// <param name="writer"></param>
        private void WriteSceneEventProgressDone(FastBufferWriter writer)
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
        private void ReadSceneEventProgressDone(FastBufferReader reader)
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
        /// Serialize scene handles and associated NetworkObjects that were migrated
        /// into a new scene.
        /// </summary>
        internal bool IsForwarding;
        private ulong m_OwnerId;

        private void SerializeObjectsMovedIntoNewScene(FastBufferWriter writer)
        {
            var sceneManager = m_NetworkManager.SceneManager;
            var ownerId = m_NetworkManager.LocalClientId;
            if (IsForwarding)
            {
                ownerId = m_OwnerId;
            }

            // Write the owner identifier
            writer.WriteValueSafe(ownerId);

            // Write the number of scene handles
            writer.WriteValueSafe(sceneManager.ObjectsMigratedIntoNewScene.Count);
            foreach (var sceneHandleObjects in sceneManager.ObjectsMigratedIntoNewScene)
            {
                if (!sceneHandleObjects.Value.ContainsKey(ownerId))
                {
                    throw new Exception($"Trying to send object scene migration for Client-{ownerId} but the client has no entries to send!");
                }
                // Write the scene handle
                writer.WriteValueSafe(sceneHandleObjects.Key);
                // Write the number of NetworkObjectIds to expect
                writer.WriteValueSafe(sceneHandleObjects.Value[ownerId].Count);
                foreach (var networkObject in sceneHandleObjects.Value[ownerId])
                {
                    writer.WriteValueSafe(networkObject.NetworkObjectId);
                }
            }
        }

        /// <summary>
        /// Deserialize scene handles and associated NetworkObjects that need to
        /// be migrated into a new scene.
        /// </summary>
        private void DeserializeObjectsMovedIntoNewScene(FastBufferReader reader)
        {
            var sceneManager = m_NetworkManager.SceneManager;
            var spawnManager = m_NetworkManager.SpawnManager;

            reader.ReadValueSafe(out ulong ownerID);
            m_OwnerId = ownerID;
            reader.ReadValueSafe(out int numberOfScenes);

            for (int i = 0; i < numberOfScenes; i++)
            {
                reader.ReadValueSafe(out NetworkSceneHandle sceneHandle);
                if (!sceneManager.ObjectsMigratedIntoNewScene.TryGetValue(sceneHandle, out var migratedObjects))
                {
                    migratedObjects = new Dictionary<ulong, List<NetworkObject>>();
                    sceneManager.ObjectsMigratedIntoNewScene.Add(sceneHandle, migratedObjects);
                }

                migratedObjects.TryAdd(ownerID, new List<NetworkObject>());

                reader.ReadValueSafe(out int objectCount);
                for (int j = 0; j < objectCount; j++)
                {
                    reader.ReadValueSafe(out ulong networkObjectId);
                    if (!spawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
                    {
                        NetworkLog.LogError($"[Object Scene Migration] Trying to synchronize NetworkObjectId ({networkObjectId}) but it was not spawned or no longer exists!!");
                        continue;
                    }

                    // Add NetworkObject scene migration to ObjectsMigratedIntoNewScene dictionary that is processed
                    migratedObjects[ownerID].Add(networkObject);
                }
            }
        }

        /// <summary>
        /// While a client is synchronizing ObjectSceneChanged messages could be received.
        /// This defers any ObjectSceneChanged message processing to occur after the client
        /// has completed synchronization to assure the associated NetworkObjects being
        /// migrated to a new scene are instantiated and spawned.
        /// </summary>
        private void DeferObjectsMovedIntoNewScene(FastBufferReader reader)
        {
            var sceneManager = m_NetworkManager.SceneManager;

            reader.ReadValueSafe(out ulong ownerId);

            var deferredObjectsMovedEvent = new NetworkSceneManager.DeferredObjectsMovedEvent()
            {
                OwnerId = ownerId,
                ObjectsMigratedTable = new Dictionary<NetworkSceneHandle, List<ulong>>(),
            };

            reader.ReadValueSafe(out int numberOfScenes);
            for (int i = 0; i < numberOfScenes; i++)
            {
                reader.ReadValueSafe(out NetworkSceneHandle sceneHandle);
                var objectsMigrated = new List<ulong>();
                deferredObjectsMovedEvent.ObjectsMigratedTable.Add(sceneHandle, objectsMigrated);
                reader.ReadValueSafe(out int objectCount);
                for (int j = 0; j < objectCount; j++)
                {
                    reader.ReadValueSafe(out ulong networkObjectId);
                    objectsMigrated.Add(networkObjectId);
                }
            }
            sceneManager.DeferredObjectsMovedEvents.Add(deferredObjectsMovedEvent);
        }

        internal void ProcessDeferredObjectSceneChangedEvents()
        {
            var sceneManager = m_NetworkManager.SceneManager;
            var spawnManager = m_NetworkManager.SpawnManager;
            if (sceneManager.DeferredObjectsMovedEvents.Count == 0)
            {
                return;
            }
            foreach (var objectsMovedEvent in sceneManager.DeferredObjectsMovedEvents)
            {
                foreach (var keyEntry in objectsMovedEvent.ObjectsMigratedTable)
                {
                    if (!sceneManager.ObjectsMigratedIntoNewScene.TryGetValue(keyEntry.Key, out var migratedObjects))
                    {
                        migratedObjects = new Dictionary<ulong, List<NetworkObject>>();
                        sceneManager.ObjectsMigratedIntoNewScene.Add(keyEntry.Key, migratedObjects);
                    }

                    migratedObjects.TryAdd(objectsMovedEvent.OwnerId, new List<NetworkObject>());
                    var objectList = migratedObjects[objectsMovedEvent.OwnerId];

                    foreach (var objectId in keyEntry.Value)
                    {
                        if (!spawnManager.SpawnedObjects.TryGetValue(objectId, out var networkObject))
                        {
                            NetworkLog.LogWarning($"[Deferred][Object Scene Migration] Trying to synchronize NetworkObjectId ({objectId}) but it was not spawned or no longer exists!");
                            continue;
                        }

                        if (!objectList.Contains(networkObject))
                        {
                            objectList.Add(networkObject);
                        }
                    }
                }
                objectsMovedEvent.ObjectsMigratedTable.Clear();
            }
            sceneManager.DeferredObjectsMovedEvents.Clear();
        }

        /// <summary>
        /// Used to release the pooled network buffer
        /// </summary>
        public void Dispose()
        {
            if (m_HasInternalBuffer)
            {
                m_InternalBuffer.Dispose();
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
