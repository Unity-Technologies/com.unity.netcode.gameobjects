using MLAPI;
using MLAPI.Connection;
using MLAPI.Security;
using MLAPI.Spawning;

namespace MLAPI_Examples
{
    // Features example calls for things often needed for things like Game managers
    public class ManagerExamples : NetworkedBehaviour
    {
        public NetworkedObject GetPlayerGameObject(ulong clientId)
        {
            return SpawnManager.GetPlayerObject(clientId);
        }
        
        // Only runs on host and client
        public NetworkedObject GetLocalPlayerObject()
        {
            return SpawnManager.GetLocalPlayerObject();
        }
        
#if !DISABLE_CRYPTOGRAPHY
        // Only runs on server
        public byte[] GetAESKeyForClient(ulong clientId)
        {
            return CryptographyHelper.GetClientKey(clientId);
        }

        // Only runs on client
        public byte[] GetAESKeyForServer()
        {
            return CryptographyHelper.GetServerKey();
        }
#endif

        // Contains player object, owned objects, cryptography keys and more
        public NetworkedClient GetClient(ulong clientId)
        {
            return NetworkingManager.Singleton.ConnectedClients[clientId];
        }
    }
}