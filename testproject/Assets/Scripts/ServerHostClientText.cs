using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ServerHostClientText : NetworkBehaviour
{
    [SerializeField]
    private Text m_DisplayText;

    private void Start()
    {
        if (m_DisplayText != null)
        {
            m_DisplayText.text = string.Empty;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (m_DisplayText != null)
        {
            if (NetworkManager.IsServer )
            {
                m_DisplayText.text = NetworkManager.IsHost ? "Host" : "Server";
            }
            else if (NetworkManager.IsClient)
            {
                m_DisplayText.text = "Client";
            }
        }
    }
}
