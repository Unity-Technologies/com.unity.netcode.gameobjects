using Unity.Netcode;
using UnityEngine;

public class OnDisconnectNotify : MonoBehaviour
{
    private NetworkManager m_NetworkManager;
    // Start is called before the first frame update
    void Start()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
        m_NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
        m_NetworkManager.OnConnectionEvent += OnConnectionEvent;
    }

    private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData eventData)
    {
        NetworkManagerHelper.Instance.LogMessage($"[{Time.realtimeSinceStartup}] Connection event {eventData.EventType} for Client-{eventData.ClientId}.");
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        NetworkManagerHelper.Instance.LogMessage($"[{Time.realtimeSinceStartup}] Connected event invoked for Client-{clientId}.");
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        NetworkManagerHelper.Instance.LogMessage($"[{Time.realtimeSinceStartup}] Disconnected event invoked for Client-{clientId}.");
    }

    private void OnDestroy()
    {
        m_NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
        m_NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        m_NetworkManager.OnConnectionEvent -= OnConnectionEvent;
    }
}
