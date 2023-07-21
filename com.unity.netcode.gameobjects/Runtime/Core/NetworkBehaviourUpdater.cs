using System.Collections.Generic;
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
        private HashSet<NetworkObject> m_DirtyNetworkObjects = new HashSet<NetworkObject>();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private ProfilerMarker m_NetworkBehaviourUpdate = new ProfilerMarker($"{nameof(NetworkBehaviour)}.{nameof(NetworkBehaviourUpdate)}");
#endif

        internal void AddForUpdate(NetworkObject networkObject)
        {
            m_DirtyNetworkObjects.Add(networkObject);
        }

        internal void NetworkBehaviourUpdate()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_NetworkBehaviourUpdate.Begin();
#endif
            try
            {
                // NetworkObject references can become null, when hidden or despawned. Once NUll, there is no point
                // trying to process them, even if they were previously marked as dirty.
                m_DirtyNetworkObjects.RemoveWhere((sobj) => sobj == null);

                if (m_ConnectionManager.LocalClient.IsServer)
                {
                    foreach (var dirtyObj in m_DirtyNetworkObjects)
                    {
                        for (int k = 0; k < dirtyObj.ChildNetworkBehaviours.Count; k++)
                        {
                            dirtyObj.ChildNetworkBehaviours[k].PreVariableUpdate();
                        }

                        for (int i = 0; i < m_ConnectionManager.ConnectedClientsList.Count; i++)
                        {
                            var client = m_ConnectionManager.ConnectedClientsList[i];

                            if (dirtyObj.IsNetworkVisibleTo(client.ClientId))
                            {
                                // Sync just the variables for just the objects this client sees
                                for (int k = 0; k < dirtyObj.ChildNetworkBehaviours.Count; k++)
                                {
                                    dirtyObj.ChildNetworkBehaviours[k].VariableUpdate(client.ClientId);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // when client updates the server, it tells it about all its objects
                    foreach (var sobj in m_DirtyNetworkObjects)
                    {
                        if (sobj.IsOwner)
                        {
                            for (int k = 0; k < sobj.ChildNetworkBehaviours.Count; k++)
                            {
                                sobj.ChildNetworkBehaviours[k].PreVariableUpdate();
                            }
                            for (int k = 0; k < sobj.ChildNetworkBehaviours.Count; k++)
                            {
                                sobj.ChildNetworkBehaviours[k].VariableUpdate(NetworkManager.ServerClientId);
                            }
                        }
                    }
                }

                foreach (var dirtyObj in m_DirtyNetworkObjects)
                {
                    for (int k = 0; k < dirtyObj.ChildNetworkBehaviours.Count; k++)
                    {
                        var behaviour = dirtyObj.ChildNetworkBehaviours[k];
                        for (int i = 0; i < behaviour.NetworkVariableFields.Count; i++)
                        {
                            if (behaviour.NetworkVariableFields[i].IsDirty() &&
                                !behaviour.NetworkVariableIndexesToResetSet.Contains(i))
                            {
                                behaviour.NetworkVariableIndexesToResetSet.Add(i);
                                behaviour.NetworkVariableIndexesToReset.Add(i);
                            }
                        }
                    }
                }
                // Now, reset all the no-longer-dirty variables
                foreach (var dirtyobj in m_DirtyNetworkObjects)
                {
                    dirtyobj.PostNetworkVariableWrite();
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
            m_NetworkManager.NetworkTickSystem.Tick += NetworkBehaviourUpdater_Tick;
        }

        internal void Shutdown()
        {
            m_NetworkManager.NetworkTickSystem.Tick -= NetworkBehaviourUpdater_Tick;
        }

        // Order of operations requires NetworkVariable updates first then showing NetworkObjects
        private void NetworkBehaviourUpdater_Tick()
        {
            // First update NetworkVariables
            NetworkBehaviourUpdate();

            // Then show any NetworkObjects queued to be made visible/shown
            m_NetworkManager.SpawnManager.HandleNetworkObjectShow();
        }
    }
}
