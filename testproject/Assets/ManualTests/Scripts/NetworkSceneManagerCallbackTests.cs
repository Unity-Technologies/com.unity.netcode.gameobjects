using UnityEngine;

namespace MLAPI
{
    [AddComponentMenu("MLAPI/NetworkSceneManagerCallbackTests")]
    public class NetworkSceneManagerCallbackTests : NetworkBehaviour
    {
        public void StartHost()
        {
            NetworkManager.StartHost();
        }
        
        public override void NetworkStart()
        {
            if (IsServer)
            {
                NetworkManager.SceneManager.OnClientLoadedScene += (progress, clientId) =>
                {
                    Debug.Log("OnClientLoadedScene invoked on the host - Passed");
                };
            
                NetworkManager.SceneManager.OnAllClientsLoadedScene += (progress, timedOut) =>
                {
                    Debug.Log("OnAllClientsLoadedScene invoked on the host - Passed");
                };
            
                NetworkManager.SceneManager.SwitchScene("SceneWeAreSwitchingTo");
            }
        }
    }
}