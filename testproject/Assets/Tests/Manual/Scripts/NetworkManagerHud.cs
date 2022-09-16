using System;
using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    [RequireComponent(typeof(NetworkManager))]
    [DisallowMultipleComponent]
    public class NetworkManagerHud : MonoBehaviour
    {
        public NetworkManager NetworkManager;
        public Rect Dimensions = new Rect(10, 10, 300, 300);

        private void Start()
        {
            if (NetworkManager == null)
            {
                NetworkManager = GetComponent<NetworkManager>();
            }

            if (NetworkManager == null)
            {
                Debug.Log("Warning, using NetworkManager Singleton");
                NetworkManager = NetworkManager.Singleton;
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(Dimensions);

            if (!NetworkManager.IsClient && !NetworkManager.IsServer)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
            }

            GUILayout.EndArea();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 PixelToCameraClip(Camera camera, Vector3 screenPos)
            {
                screenPos.z = camera.nearClipPlane + 0.001f;
                screenPos.y = camera.pixelHeight - screenPos.y;
                return camera.ScreenToWorldPoint(screenPos);
            }

            Gizmos.color = Color.green;

            var cam = FindObjectOfType<Camera>();
            if (cam == null)
            {
                return;
            }

            Vector3 tl = PixelToCameraClip(cam, Dimensions.min);
            Vector3 tr = PixelToCameraClip(cam, new Vector3(Dimensions.xMax, Dimensions.yMin));
            Vector3 br = PixelToCameraClip(cam, Dimensions.max);
            Vector3 bl = PixelToCameraClip(cam, new Vector3(Dimensions.xMin, Dimensions.yMax));

            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
            Gizmos.DrawLine(bl, tl);
        }

        private void StartButtons()
        {
            if (GUILayout.Button("Host"))
            {
                NetworkManager.StartHost();
            }

            if (GUILayout.Button("Client"))
            {
                NetworkManager.StartClient();
            }

            if (GUILayout.Button("Server"))
            {
                NetworkManager.StartServer();
            }
        }

        private void StatusLabels()
        {
            var mode = NetworkManager.IsHost ? "Host" : NetworkManager.IsServer ? "Server" : "Client";

            GUILayout.Label($"Transport: {NetworkManager.NetworkConfig.NetworkTransport.GetType().Name}");
            GUILayout.Label($"Mode: {mode}");
        }
    }
}
