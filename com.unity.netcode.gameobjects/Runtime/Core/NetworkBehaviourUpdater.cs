using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace Unity.Netcode
{
    /// <summary>
    /// An helper class that helps NetworkManager update NetworkBehaviours and replicate them down to connected clients.
    /// </summary>
    public class NetworkBehaviourUpdater
    {
        private NetworkManager m_NetworkManager;
        private NetworkConnectionManager m_ConnectionManager;

        /// <summary>
        /// Contains the current dirty <see cref="NetworkObject"/>s that are proccessed each new network tick.
        /// Under most cases, dirty <see cref="NetworkObject"/>s are fully processed on the next network tick.
        /// Under certain conditions, like user script invoking <see cref="NetworkVariableBase.SetUpdateTraits(NetworkVariableUpdateTraits)"/>
        /// to define <see cref="NetworkVariableUpdateTraits"/>, a <see cref="NetworkObject"/> can remain in the
        /// <see cref="m_DirtyNetworkObjects"/> list until the configured traits' conditions have been met.
        /// </summary>
        private HashSet<NetworkObject> m_DirtyNetworkObjects = new HashSet<NetworkObject>();

        /// <summary>
        /// Contains any dirty <see cref="NetworkObject"/>s that will be added to the <see cref="m_DirtyNetworkObjects"/>
        /// list on the next network tick (<see cref="OnNetworkTick"/>).
        /// </summary>
        private HashSet<NetworkObject> m_PendingDirtyNetworkObjects = new HashSet<NetworkObject>();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private ProfilerMarker m_NetworkBehaviourUpdate = new ProfilerMarker($"{nameof(NetworkBehaviour)}.{nameof(NetworkBehaviourUpdate)}");
#endif

        /// <summary>
        /// Adds a <see cref="NetworkObject"/> to the prending dirty list.
        /// The <see cref="m_PendingDirtyNetworkObjects"/> list is merged into the <see cref="m_DirtyNetworkObjects"/> list
        /// when processed.
        /// </summary>
        internal void AddForUpdate(NetworkObject networkObject)
        {
            // Since this is a HashSet, we don't need to worry about duplicate entries
            m_PendingDirtyNetworkObjects.Add(networkObject);
        }

        /// <summary>
        /// (Client-server network topology only)
        /// The server handles processing network variables the same way as a client
        /// with the primary difference being that the server sends updates to all
        /// observers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ProcessDirtyObjectServer(NetworkObject dirtyObj, bool forceSend)
        {
            foreach (var client in m_ConnectionManager.ConnectedClientsList)
            {
                if (m_NetworkManager.DistributedAuthorityMode || dirtyObj.IsNetworkVisibleTo(client.ClientId))
                {
                    // Sync just the variables for just the objects this client sees
                    for (int k = 0; k < dirtyObj.ChildNetworkBehaviours.Count; k++)
                    {
                        dirtyObj.ChildNetworkBehaviours[k].NetworkVariableUpdate(client.ClientId, forceSend);
                    }
                }
            }
        }

        /// <summary>
        /// Clients handle processing dirty objects relative to the client.
        /// The <see cref="NetworkVariableDeltaMessage"/> is client to server.
        /// With distributed authority live service sessions, this is sent to
        /// the CMB service.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ProcessDirtyObjectClient(NetworkObject dirtyObj, bool forceSend)
        {
            for (int k = 0; k < dirtyObj.ChildNetworkBehaviours.Count; k++)
            {
                dirtyObj.ChildNetworkBehaviours[k].NetworkVariableUpdate(NetworkManager.ServerClientId, forceSend);
            }
        }

        /// <summary>
        /// Handle house cleaning on the child <see cref="NetworkBehaviour"/>s.
        /// This includes some collections specific checks and updates.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PostProcessDirtyObject(NetworkObject dirtyObj)
        {
            for (int k = 0; k < dirtyObj.ChildNetworkBehaviours.Count; k++)
            {
                var behaviour = dirtyObj.ChildNetworkBehaviours[k];
                for (int i = 0; i < behaviour.NetworkVariableFields.Count; i++)
                {
                    // Set to true for NetworkVariable to ignore duplication of the
                    // "internal original value" for collections support.
                    behaviour.NetworkVariableFields[i].NetworkUpdaterCheck = true;
                    if (behaviour.NetworkVariableFields[i].IsDirty() &&
                        !behaviour.NetworkVariableIndexesToResetSet.Contains(i))
                    {
                        behaviour.NetworkVariableIndexesToResetSet.Add(i);
                        behaviour.NetworkVariableIndexesToReset.Add(i);
                    }
                    // Reset back to false when done
                    behaviour.NetworkVariableFields[i].NetworkUpdaterCheck = false;
                }
            }
        }

        /// <summary>
        /// Invokes <see cref="NetworkBehaviour.PostNetworkVariableWrite(bool)"/> on all child <see cref="NetworkBehaviour"/>s.
        /// </summary>
        /// <param name="dirtyObj"></param>
        /// <param name="forceSend">Refer to the <see cref="ProcessDirtyObject(NetworkObject, bool)"/> definition.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ResetDirtyObject(NetworkObject dirtyObj, bool forceSend)
        {
            foreach (var behaviour in dirtyObj.ChildNetworkBehaviours)
            {
                behaviour.PostNetworkVariableWrite(forceSend);
            }
        }

        /// <summary>
        /// Temporary work-around for assuring any pending dirty states are pushed out prior to showing the object
        /// TODO: We need to send all messages that are specific to a NetworkObject along with a NetworkObject event header
        /// and grouped together such that all directed messages will be processed after spawned.
        /// </summary>
        /// <param name="networkObject"></param>
        internal void ForceSendIfDirtyOnNetworkShow(NetworkObject networkObject)
        {
            // Exit early if no pending dirty NetworkVariables.
            if (!m_PendingDirtyNetworkObjects.Contains(networkObject) && !m_DirtyNetworkObjects.Contains(networkObject))
            {
                return;
            }

            ProcessDirtyObject(networkObject, true);

            // Remove it from the pending and queued dirty objects lists
            m_PendingDirtyNetworkObjects.Remove(networkObject);
            m_DirtyNetworkObjects.Remove(networkObject);
        }

        /// <summary>
        /// The primary "dirty" <see cref="NetworkObject"/> processor.
        /// Invokes:
        /// - <see cref="NetworkBehaviour.PreVariableUpdate"/> on all properties that derive from <see cref="NetworkVariableBase"/>.
        /// - <see cref="ProcessDirtyObjectServer(NetworkObject, bool)"/> (if the server).
        /// - <see cref="ProcessDirtyObjectClient(NetworkObject, bool)"/> (if the client).
        /// - <see cref="PostProcessDirtyObject"/> to handle the post processing of network variables.
        /// - <see cref="ResetDirtyObject"/> which cleans up and removes the <see cref="NetworkObject"/> from the dirty list.
        /// </summary>
        /// <param name="networkObject">The <see cref="NetworkObject"/> to process.</param>
        /// <param name="forceSend">When enabled, any dirty network variables will be added to a
        /// <see cref="NetworkVariableDeltaMessage"/> and added to the outbound queue.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ProcessDirtyObject(NetworkObject networkObject, bool forceSend)
        {
            // Only the server or the owner of the NetworkObject will send
            // delta state updates. Otherwise, if neither are true we exit early.
            if (!(m_NetworkManager.IsServer || networkObject.IsOwner))
            {
                return;
            }

            // Pre-variable update
            for (int k = 0; k < networkObject.ChildNetworkBehaviours.Count; k++)
            {
                networkObject.ChildNetworkBehaviours[k].PreVariableUpdate();
            }

            // Server sends updates to all clients where a client sends updates
            // to the server or DA service.
            if (m_NetworkManager.IsServer)
            {
                ProcessDirtyObjectServer(networkObject, forceSend);
            }
            else
            {
                ProcessDirtyObjectClient(networkObject, forceSend);
            }

            // Handle post processing and resetting of the NetworkObject
            PostProcessDirtyObject(networkObject);
            ResetDirtyObject(networkObject, forceSend);
        }

        /// <summary>
        /// Sends NetworkVariable deltas
        /// </summary>
        /// <param name="forceSend"> Refer to the <see cref="ProcessDirtyObject(NetworkObject, bool)"/> definition.</param>
        internal void NetworkBehaviourUpdate(bool forceSend = false)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_NetworkBehaviourUpdate.Begin();
#endif
            try
            {
                foreach (var dirtyNetworkObject in m_PendingDirtyNetworkObjects)
                {
                    m_DirtyNetworkObjects.Add(dirtyNetworkObject);
                }
                m_PendingDirtyNetworkObjects.Clear();

                // NetworkObject references can become null, when hidden or despawned. Once NUll, there is no point
                // trying to process them, even if they were previously marked as dirty.
                m_DirtyNetworkObjects.RemoveWhere((sobj) => sobj == null);

                foreach (var dirtyObj in m_DirtyNetworkObjects)
                {
                    ProcessDirtyObject(dirtyObj, forceSend);
                }

                m_DirtyNetworkObjects.Clear();
            }
            finally
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                m_NetworkBehaviourUpdate.End();
#endif
            }
        }

        internal void Initialize(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            m_ConnectionManager = networkManager.ConnectionManager;
            m_NetworkManager.NetworkTickSystem.Tick += OnNetworkTick;
        }

        internal void Shutdown()
        {
            m_NetworkManager.NetworkTickSystem.Tick -= OnNetworkTick;
        }

        /// <summary>
        /// Process any dirty <see cref="NetworkObject"/>s on each new
        /// network tick.
        /// </summary>
        private void OnNetworkTick()
        {
            NetworkBehaviourUpdate();
        }
    }
}
