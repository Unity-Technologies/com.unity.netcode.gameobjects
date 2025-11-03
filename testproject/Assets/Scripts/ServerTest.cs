using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class ServerTest : MonoBehaviour
{
    private NetworkManager m_Network;
    private UnityTransport m_Transport;

    private void Awake()
    {
        m_Network = GetComponent<NetworkManager>();
        m_Transport = GetComponent<UnityTransport>();
    }

    private void Start()
    {
        ushort port = 8889;
        Debug.Log("Start Server Port " + port);          // <---- Start Server Port 8888
        m_Transport.SetConnectionData("0.0.0.0", port, forceOverrideCommandLineArgs: true);   // <----  Port set to 8888
        m_Network.StartServer();
        Debug.Log("Server listening on port: " + m_Transport.ConnectionData.Port); // <---- Shows      Server listening on port: 7777
    }
}
