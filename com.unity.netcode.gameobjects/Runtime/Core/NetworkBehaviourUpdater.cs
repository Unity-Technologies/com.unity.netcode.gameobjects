using System.Collections.Generic;
using Unity.Profiling;

namespace Unity.Netcode
{
    /// <summary>
    /// An helper class that helps NetworkManager update NetworkBehaviours and replicate them down to connected clients.
    /// </summary>
    public class NetworkBehaviourUpdater
    {
        private HashSet<NetworkObject> m_DirtyNetworkObjects = new HashSet<NetworkObject>();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private ProfilerMarker m_NetworkBehaviourUpdate = new ProfilerMarker($"{nameof(NetworkBehaviour)}.{nameof(NetworkBehaviourUpdate)}");
#endif

        internal void AddForUpdate(NetworkObject networkObject)
        {
            m_DirtyNetworkObjects.Add(networkObject);
        }

        internal void NetworkBehaviourUpdate(NetworkManager networkManager)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_NetworkBehaviourUpdate.Begin();
#endif
            try
            {
                if (networkManager.IsServer)
                {
                    foreach (var dirtyObj in m_DirtyNetworkObjects)
                    {
                        for (int i = 0; i < networkManager.ConnectedClientsList.Count; i++)
                        {
                            var client = networkManager.ConnectedClientsList[i];
                            if (networkManager.IsHost && client.ClientId == networkManager.LocalClientId)
                            {
                                continue;
                            }

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

                    // Now, reset all the no-longer-dirty variables
                    foreach (var dirtyObj in m_DirtyNetworkObjects)
                    {
                        for (int k = 0; k < dirtyObj.ChildNetworkBehaviours.Count; k++)
                        {
                            dirtyObj.ChildNetworkBehaviours[k].PostNetworkVariableWrite();
                        }
                    }
                    // This is not strictly needed for runtime, but it fails tests, otherwise.
                    foreach (var networkObject in m_DirtyNetworkObjects)
                    {
                        networkObject.MarkVariablesDirty(false);
                    }
                    m_DirtyNetworkObjects.Clear();
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
                                sobj.ChildNetworkBehaviours[k].VariableUpdate(NetworkManager.ServerClientId);
                            }
                        }
                    }

                    // Now, reset all the no-longer-dirty variables
                    foreach (var sobj in m_DirtyNetworkObjects)
                    {
                        for (int k = 0; k < sobj.ChildNetworkBehaviours.Count; k++)
                        {
                            sobj.ChildNetworkBehaviours[k].PostNetworkVariableWrite();
                        }
                        // This is not strictly needed for runtime, but it fails tests, otherwise.
                        sobj.MarkVariablesDirty(false);
                    }
                    m_DirtyNetworkObjects.Clear();
                }
            }
            finally
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                m_NetworkBehaviourUpdate.End();
#endif
            }
        }

    }
}
