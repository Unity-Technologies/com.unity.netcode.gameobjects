using MLAPI.NetworkingManagerComponents;
using UnityEngine;

namespace MLAPI
{
    public class NetworkedObject : MonoBehaviour
    {
        [HideInInspector]
        public uint NetworkId;
        [HideInInspector]
        public int OwnerClientId = -1;
        [HideInInspector]
        public int SpawnablePrefabIndex;
        [HideInInspector]
        public bool isPlayerObject = false;
        public bool ServerOnly = false;
        public bool isLocalPlayer
        {
            get
            {
                return isPlayerObject && (OwnerClientId == NetworkingManager.singleton.MyClientId || (OwnerClientId == -1 && NetworkingManager.singleton.isHost));
            }
        }

        public bool IsOwner
        {
            get
            {
                return !isPlayerObject && (OwnerClientId == NetworkingManager.singleton.MyClientId || (OwnerClientId == -1 && NetworkingManager.singleton.isHost));
            }
        }

        private void OnDestroy()
        {
            SpawnManager.OnDestroyObject(NetworkId, false);
        }

        internal bool isSpawned = false;

        public void Spawn()
        {
            SpawnManager.OnSpawnObject(this);
        }

        public void SpawnWithOwnership(int clientId)
        {
            SpawnManager.OnSpawnObject(this, clientId);
        }
    }
}
