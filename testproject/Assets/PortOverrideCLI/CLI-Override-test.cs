using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UIElements;

public class ServerTest : MonoBehaviour
{
    private NetworkManager m_Network;
    private UnityTransport m_Transport;

    public UIDocument Doc;
    public string First;
    public string Second;

    private void Awake()
    {
        m_Network = GetComponent<NetworkManager>();
        m_Transport = GetComponent<UnityTransport>();
    }

    private void Start()
    {
        ushort port = 8889;

        First = "Start Server Port " + port;
        Debug.Log(First);          // <---- Start Server Port 8888
        //m_Transport.SetConnectionData("0.0.0.0", port, forceOverrideCommandLineArgs: true);   // <----  Port set to true 8888 false what is set in cli
        m_Network.StartServer();
        Second = "Server listening on port: " + m_Transport.ConnectionData.Port;
        Debug.Log(Second); // <---- Shows      Server listening on port: 7777

        Doc.rootVisualElement.Q<Label>("1").text = First;
        Doc.rootVisualElement.Q<Label>("2").text = Second;
    }
}


