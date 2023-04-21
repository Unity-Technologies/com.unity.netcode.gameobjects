using Unity.Netcode;
using UnityEngine;

namespace TestProject.ManualTests
{
    [RequireComponent(typeof(NetworkManager))]
    [DisallowMultipleComponent]
    public class NetworkManagerHud : MonoBehaviour
    {
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
            }

            GUILayout.EndArea();
        }

        private static void StartButtons()
        {
            if (GUILayout.Button("Host"))
            {
                NetworkManager.Singleton.StartHost();
            }

            if (GUILayout.Button("Client"))
            {
                NetworkManager.Singleton.StartClient();
            }

            if (GUILayout.Button("Server"))
            {
                NetworkManager.Singleton.StartServer();
            }
        }

        private static void StatusLabels()
        {
            var mode = NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

            GUILayout.Label($"Transport: {NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name}");
            GUILayout.Label($"Mode: {mode}");
        }
    }
}
