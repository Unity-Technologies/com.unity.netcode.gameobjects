using UnityEngine;

namespace MLAPI
{
    //TODO
    //Will be used for objects which will be spawned automatically across clients
    public class NetworkedObject : MonoBehaviour
    {
        public uint NetworkId;
        public int OwnerClientId = -1;
        public int SpawnablePrefabId;
        internal bool IsPlayerObject = false;
        public bool isLocalPlayer
        {
            get
            {
                return OwnerClientId == NetworkingManager.singleton.MyClientId;
            }
        }

        private void OnDestroy()
        {
            NetworkingManager.OnDestroyObject(NetworkId);
        }
    }
}
