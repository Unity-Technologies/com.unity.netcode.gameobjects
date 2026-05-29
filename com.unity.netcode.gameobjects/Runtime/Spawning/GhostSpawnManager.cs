#if UNIFIED_NETCODE
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Netcode.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Netcode
{
    /// <summary>
    /// Handles the management of ghost spawns during the synchronization process. This includes tracking pending NetworkObject spawns
    /// that are waiting for their associated ghost to be spawned before they can be fully deserialized.
    /// </summary>
    internal class GhostSpawnManager
    {
        private readonly NetworkManager m_NetworkManager;
        private readonly ContextualLogger m_Log;

        public GhostSpawnManager(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            m_Log = new ContextualLogger((Object)networkManager, networkManager);
        }

        private readonly Dictionary<ulong, NetworkObject> m_GhostsPendingSpawn = new();

        // TODO: We might want to make this a mock interface but temporary solution to validate
        // the need to assure we are registering with the right NetworkManager instance when testing (everything
        // will use the singleton during Awake and Start when we need to register).
        internal delegate void RegisterPendingGhostDelegateHandler(NetworkObject networkObject, ulong networkObjectId);

        internal static RegisterPendingGhostDelegateHandler RegisterPendingGhost;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RuntimeInitializeOnLoad() => RegisterPendingGhost = null;

        internal void RegisterGhostBridge(ulong networkObjectId, NetworkObject networkObject)
        {
            m_Log.Info(new Context(LogLevel.Developer, $"Registering {nameof(NetworkObject)} for ghost bridge").AddInfo(nameof(NetworkObject.NetworkObjectId), networkObjectId));

            // Set when running through integration tests in order to initially bypass the
            // normal registration. This is because at this point in the instantiation process,
            // NetworkObject's NetworkManager is pointing to the singleton which means all instances
            // (even if intended to be for a specific client) will end up registering with whichever
            // NetworkManager instance is being pointed to by the singleton.
            if (RegisterPendingGhost != null)
            {
                RegisterPendingGhost(networkObject, networkObjectId);
            }
            else if (!m_NetworkManager.IsServer)
            {
                RegisterGhostPendingSpawn(networkObject, networkObjectId);
            }
        }

        internal void RegisterGhostPendingSpawn(NetworkObject networkObject, ulong networkObjectId)
        {
            m_Log.Info(new Context(LogLevel.Developer, $"Registering {nameof(NetworkObject)} for ghost spawn").AddTag(networkObject.name).AddInfo(nameof(NetworkObject.NetworkObjectId), networkObjectId));

            if (!m_GhostsPendingSpawn.TryAdd(networkObjectId, networkObject))
            {
                m_Log.Error(new Context(LogLevel.Normal, $"{nameof(NetworkObject)} has already been registered as a pending ghost!").AddTag(networkObject.name).AddInfo(nameof(NetworkObject.NetworkObjectId), networkObjectId));
                return;
            }

            // TODO-REVIEW-BELOW: *** This is very likely no longer an issue with the new connection sequence ***
            // TODO-UNIFIED: We need a better way to preserve any hybrid instances pending NGO spawn.
            // Edge-Case scenario: During initial client synchronization (i.e. !m_NetworkManager.IsConnectedClient).
            //
            // Description: A client can receive snapshots before finishing the NGO synchronization process.
            // This is when an edge case scenario can happen where the initial NGO synchronization information
            // can include new scenes to load. If one of those scenes is configured to load in SingleMode, then
            // any instantiated ghosts pending synchronization would be instantiated in whatever the currently
            // active scene was when the client was processing the synchronization data. If the ghosts pending
            // synchronization are in the currently active scene when the new scene is loaded in SingleMode, then
            // they would be destroyed.
            //
            // Current Fix:
            // If the client is not yet synchronized, then any ghost pending spawn get migrated into the DDOL.
            //
            // Further review:
            // We need to make sure that we are migrating NetworkObjects into their assigned scene (if scene
            // management is enabled). Currently, we assume all instances were in the DDOL and just migrate
            // them into the currently active scene upon spawn.
            if (!m_NetworkManager.IsConnectedClient && !m_GhostsPendingSynchronization.ContainsKey(networkObjectId))
            {
                Object.DontDestroyOnLoad(networkObject.gameObject);
            }
            else // There is matching spawn data for this pending Ghost, process the pending spawn for this hybrid instance.
            {
                m_NetworkManager.DeferredMessageManager.ProcessTriggers(IDeferredNetworkMessageManager.TriggerType.OnGhostSpawned, networkObjectId);
                if (m_GhostsPendingSynchronization.ContainsKey(networkObjectId))
                {
                    ProcessGhostPendingSynchronization(networkObjectId);
                }
            }
        }

        internal bool TryGetGhostNetworkObjectForSpawn(NetworkObject.SerializedObject serializedObject, [NotNullWhen(true)] out NetworkObject networkObject)
        {
            var networkObjectId = serializedObject.NetworkObjectId;

            // TODO-UNIFIED: Get this working somehow (or if not possible prevent this from happening prior to getting to this point)
            if (serializedObject.HasInstantiationData)
            {
                m_Log.Error(new Context(LogLevel.Error, $"{nameof(NetworkObject)} Pre-spawn instantiation data does not work in this version!").AddInfo(nameof(NetworkObject.NetworkObjectId), networkObjectId));
            }
            if (!m_GhostsPendingSpawn.Remove(networkObjectId, out networkObject) || networkObject == null)
            {
                m_Log.Error(new Context(LogLevel.Error, $"Attempting to spawn {nameof(NetworkObject)} with no instance to spawn!").AddInfo(nameof(NetworkObject.NetworkObjectId), networkObjectId));
                return false;
            }

            // TODO-UNIFIED: We need a better way to preserve any hybrid instances pending NGO spawn.
            // NOTE: We might be able to use the NetworkSceneHandle to get the associated local scene handle to which we can use to get the targeted scene.
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(networkObject.gameObject, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            return true;
        }

        private bool m_GhostsArePendingSynchronization;
        private readonly Dictionary<ulong, PendingGhostSpawnEntry> m_GhostsPendingSynchronization = new();
        internal void RegisterGhostPendingSynchronization(PendingGhostSpawnEntry pendingGhostSpawnEntry)
        {
            var networkObjectId = pendingGhostSpawnEntry.SerializedObject.NetworkObjectId;
            m_Log.Info(new Context(LogLevel.Developer, $"Registering {nameof(NetworkObject.SerializedObject)} for pending synchronization").AddInfo(nameof(NetworkObject.NetworkObjectId), networkObjectId));

            m_GhostsPendingSynchronization.TryAdd(networkObjectId, pendingGhostSpawnEntry);
            m_GhostsArePendingSynchronization = true;
        }

        internal NetworkObject ProcessGhostPendingSynchronization(ulong networkObjectId, bool removeUponSpawn = true)
        {
            var ghostPendingSync = m_GhostsPendingSynchronization[networkObjectId];
            var serializedObject = ghostPendingSync.SerializedObject;
            var reader = ghostPendingSync.Buffer;
            if (removeUponSpawn)
            {
                m_GhostsPendingSynchronization.Remove(networkObjectId);
            }

            if (serializedObject.IsSceneObject)
            {
                m_NetworkManager.SceneManager.SetTheSceneBeingSynchronized(serializedObject.NetworkSceneHandle);
            }
            var networkObject = NetworkObject.Deserialize(serializedObject, reader, m_NetworkManager);

            // TODO-UNIFIED: How do we handle the "all in-scene placed objects are spawned notification"?
            //if (serializedObject.IsSceneObject)
            //{
            //    networkObject.InternalInSceneNetworkObjectsSpawned();
            //}

            // If removing, determine if we have any pending ghosts remaining and dispose of this ghost's pending synchronization buffer.
            // If not removing, then we will keep the buffer around until we do remove it (either via spawn or timeout).
            if (removeUponSpawn)
            {
                m_GhostsArePendingSynchronization = m_GhostsPendingSynchronization.Count > 0;
                ghostPendingSync.Dispose();
            }
            return networkObject;
        }


        private readonly HashSet<ulong> m_GhostSynchronizationPendingRemoval = new();

        internal void ProcessAllGhostsPendingSynchronization()
        {
            var spawnTimeout = m_NetworkManager.NetworkConfig.SpawnTimeout;
            if (!m_GhostsArePendingSynchronization)
            {
                return;
            }
            foreach (var ghost in m_GhostsPendingSynchronization)
            {
                var networkObjectId = ghost.Value.SerializedObject.NetworkObjectId;
                if (m_GhostsPendingSpawn.ContainsKey(networkObjectId))
                {
                    // Process it, but don't remove it as we handle that a little later
                    ProcessGhostPendingSynchronization(ghost.Value.SerializedObject.NetworkObjectId, false);
                    m_GhostSynchronizationPendingRemoval.Add(networkObjectId);
                }
                else
                if ((ghost.Value.RegistrationTime + spawnTimeout) < Time.realtimeSinceStartup)
                {
                    m_Log.Info(new Context(LogLevel.Developer, $"Registering {nameof(NetworkObject.SerializedObject)} for pending synchronization").AddInfo(nameof(NetworkObject.NetworkObjectId), networkObjectId));

                    // Timed out entries are removed too
                    m_GhostSynchronizationPendingRemoval.Add(ghost.Key);
                }
            }

            foreach (var networkObjectId in m_GhostSynchronizationPendingRemoval)
            {
                var entry = m_GhostsPendingSynchronization[networkObjectId];
                m_GhostsPendingSynchronization.Remove(networkObjectId);
                entry.Buffer.Dispose();
            }
            m_GhostSynchronizationPendingRemoval.Clear();
            m_GhostsArePendingSynchronization = m_GhostsPendingSynchronization.Count > 0;
        }

        internal void MarkNetworkObjectAsDestroying(ulong networkObjectId)
        {
            m_GhostsPendingSpawn.Remove(networkObjectId);
            m_GhostsPendingSynchronization.Remove(networkObjectId);
        }

        internal bool IsGhostPendingSpawn(ulong networkObjectId)
        {
            return m_GhostsPendingSpawn.ContainsKey(networkObjectId);
        }

        /// <summary>
        /// Used in scene managment synchronization.
        /// Verifies if this <see cref="NetworkObject.SerializedObject"/> should defer its spawn until the associated GhostObject is created.
        /// </summary>
        /// <param name="serializedObject">The object to check</param>
        /// <param name="internalBuffer"><see cref="SceneEventData"/>'s <see cref="SceneEventData.InternalBuffer"/> to save the buffer with data for the deferred spawn.</param>
        /// <returns>True if the serialized object should should wait for the associated ghost object; false otherwise</returns>
        internal bool ShouldDeferGhostSceneObject(NetworkObject.SerializedObject serializedObject, FastBufferReader internalBuffer)
        {
            if (!m_GhostsPendingSpawn.TryGetValue(serializedObject.NetworkObjectId, out var existingObject))
            {
                m_Log.Info(new Context(LogLevel.Developer, $"Deferring creation of InScenePlaced {nameof(NetworkObject)} to wait for Ghost.").AddTag("SynchronizeSceneNetworkObjects").AddInfo(nameof(NetworkObject.NetworkObjectId), serializedObject.NetworkObjectId));

                var newEntry = new PendingGhostSpawnEntry()
                {
                    RegistrationTime = Time.realtimeSinceStartup,
                    SerializedObject = serializedObject,
                    Buffer = new FastBufferReader(internalBuffer, Allocator.Persistent, serializedObject.SynchronizationDataSize, internalBuffer.Position)
                };

                RegisterGhostPendingSynchronization(newEntry);
                internalBuffer.Seek(internalBuffer.Position + serializedObject.SynchronizationDataSize);
                return true;
            }

            if (existingObject == null)
            {
                m_Log.Info(new Context(LogLevel.Developer, $"Dropping creation of InScenePlaced {nameof(NetworkObject)} as it has an entry but no longer exists!").AddTag("SynchronizeSceneNetworkObjects").AddInfo(nameof(NetworkObject.NetworkObjectId), serializedObject.NetworkObjectId));

                // If it no longer exists, then just remove the entry and skip it.
                internalBuffer.Seek(internalBuffer.Position + serializedObject.SynchronizationDataSize);
                m_GhostsPendingSpawn.Remove(serializedObject.NetworkObjectId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finalizer that ensures proper cleanup of manager resources
        /// </summary>
        ~GhostSpawnManager()
        {
            Shutdown();
        }

        internal void Shutdown()
        {
            m_GhostsPendingSpawn.Clear();
            m_GhostsPendingSynchronization.Clear();
        }
    }

    /// <summary>
    /// Used to store pending ghost spawns that are waiting for their associated (N4E) ghost to be spawned before they can be fully deserialized and
    /// spawned during the scene synchronization process. This is necessary because in unified mode we allow for NetworkObjects with ghost components
    /// to be synchronized during the scene synchronization process but we can't guarantee the order of messages that the client receives so we
    /// need to defer the deserialization of any NetworkObject that has a ghost component until we have received the message that the ghost has
    /// been spawned and we have an instance to deserialize this information into.
    /// </summary>
    internal struct PendingGhostSpawnEntry : IDisposable
    {
        public float RegistrationTime;
        public FastBufferReader Buffer;
        public NetworkObject.SerializedObject SerializedObject;

        public void Dispose()
        {
            Buffer.Dispose();
        }
    }
}
#endif
