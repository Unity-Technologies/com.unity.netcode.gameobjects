using System;
using MLAPI;
using MLAPI.Transports;
using UnityEngine;

namespace MLAPI.Prototyping
{
    [RequireComponent(typeof(NetworkingManager))]
    [DisallowMultipleComponent]
    public class NetworkingManagerHud : MonoBehaviour
    {
        NetworkingManager m_NetworkingManager;

        Transport m_Transport;

        GUIStyle m_LabelTextStyle;

        // This is needed to make the port field more convenient. GUILayout.TextField is very limited and we want to be able to clear the field entirely so we can't cache this as ushort.
        string m_PortString;

        public Vector2 DrawOffset = new Vector2(10, 10);

        public Color LabelColor = Color.black;

        void Awake()
        {
            // Only cache networking manager but not transport here because transport could change anytime.
            m_NetworkingManager = GetComponent<NetworkingManager>();
            m_LabelTextStyle = new GUIStyle(GUIStyle.none);
        }

        void OnGUI()
        {
            m_LabelTextStyle.normal.textColor = LabelColor;

            m_Transport = m_NetworkingManager.NetworkConfig.NetworkTransport;

            if (m_PortString == null)
            {
                m_PortString = m_Transport.NetworkPort.ToString();
            }

            GUILayout.BeginArea(new Rect(DrawOffset, new Vector2(200, 200)));

            if (m_NetworkingManager.IsRunning)
            {
                DrawStatusGUI();
            }
            else
            {
                DrawConnectGUI();
            }

            GUILayout.EndArea();
        }

        void DrawConnectGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Address", m_LabelTextStyle);
            GUILayout.Label("Port", m_LabelTextStyle);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            m_Transport.NetworkAddress = GUILayout.TextField(m_Transport.NetworkAddress);
            m_PortString = GUILayout.TextField(m_PortString);
            if (ushort.TryParse(m_PortString, out ushort port))
            {
                m_Transport.NetworkPort = port;
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Host (Server + Client)"))
            {
                m_NetworkingManager.StartHost();
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Server"))
            {
                m_NetworkingManager.StartServer();
            }

            if (GUILayout.Button("Client"))
            {
                m_NetworkingManager.StartClient();
            }

            GUILayout.EndHorizontal();
        }

        void DrawStatusGUI()
        {
            if (m_NetworkingManager.IsServer)
            {
                var mode = m_NetworkingManager.IsHost ? "Host" : "Server";
                GUILayout.Label($"{mode} active on port: {m_Transport.NetworkPort.ToString()}", m_LabelTextStyle);
            }
            else
            {
                if (m_NetworkingManager.IsConnectedClient)
                {
                    GUILayout.Label($"Client connected {m_Transport.NetworkAddress}:{m_Transport.NetworkPort.ToString()}", m_LabelTextStyle);
                }
            }

            GUILayout.Label($"Transport: {m_Transport.GetType().Name}", m_LabelTextStyle);

            if (GUILayout.Button("Stop"))
            {
                m_NetworkingManager.Stop();
            }
        }
    }
}
