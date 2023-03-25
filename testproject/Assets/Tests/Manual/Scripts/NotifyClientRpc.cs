using Unity.Netcode;
using UnityEngine;

namespace TestProject.ManualTests
{
    public class NotifyClientRpc : NetworkBehaviour
    {
        private static NotifyClientRpc s_Singleton;

        private void OnEnable()
        {
            if (s_Singleton != null && s_Singleton != this)
            {
                DestroyImmediate(gameObject);
                return;
            }
            DontDestroyOnLoad(this);
        }
        // Start is called before the first frame update
        private void Start()
        {
            NetworkObject.DestroyWithScene = false;
        }


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (s_Singleton == null)
            {
                s_Singleton = this;
            }

            if (IsServer && NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            }
            NetworkObject.DestroyWithScene = false;
        }

        [ClientRpc]
        private void SendRpcOnLoadClientRpc(string sceneName)
        {
            Debug.Log($"Client loaded {sceneName}, we can send RPCs now!");
        }

        [ClientRpc]
        private void SendRpcOnSynchClientRpc()
        {
            Debug.Log($"Client finished synchronizing, we can send RPCs now!");
        }

        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            if (NetworkManager != null && NetworkManager.IsServer)
            {
                switch (sceneEvent.SceneEventType)
                {
                    case SceneEventType.LoadComplete:
                        {
                            SendRpcOnLoadClientRpc(sceneEvent.SceneName);
                            break;
                        }
                    case SceneEventType.SynchronizeComplete:
                        {
                            SendRpcOnSynchClientRpc();
                            break;
                        }
                }
            }
        }
    }
}
