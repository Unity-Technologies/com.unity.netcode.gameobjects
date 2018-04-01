using System.Collections.Generic;
using MLAPI.Data;

namespace MLAPI.NetworkingManagerComponents.Core
{
    internal static class ClientIdManager
    {
        internal static int clientIdCounter;
        // Use a queue instead of stack to (hopefully) reduce the chance of a clientId being re taken to quickly.
        internal static Queue<int> releasedClientIds;
        internal static Dictionary<int, ClientIdKey> clientIdToKey;
        internal static Dictionary<ClientIdKey, int> keyToClientId;

        internal static int GetClientId(int connectionId, int hostId)
        {
            int clientId;
            if (releasedClientIds.Count > 0)
            {
                clientId = releasedClientIds.Dequeue();   
            }
            else
            {
                clientId = clientIdCounter;
                clientIdCounter++;
            }
            clientIdToKey.Add(clientId, new ClientIdKey(hostId, connectionId));
            keyToClientId.Add(new ClientIdKey(hostId, connectionId), clientId);
            return clientId;
        }

        internal static void ReleaseClientId(int clientId)
        {
            ClientIdKey key = clientIdToKey[clientId];
            if (clientIdToKey.ContainsKey(clientId))
                clientIdToKey.Remove(clientId);
            if (keyToClientId.ContainsKey(key))
                keyToClientId.Remove(key);
            
            releasedClientIds.Enqueue(clientId);
        }
    }
}
