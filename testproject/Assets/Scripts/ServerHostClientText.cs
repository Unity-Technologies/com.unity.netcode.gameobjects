using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ServerHostClientText : NetworkBehaviour
{
    [SerializeField]
    private Text m_DisplayText;
    private Color m_Color;
    private Vector3 m_LocalPosition;

    private void Awake()
    {
        m_LocalPosition = transform.localPosition;
    }

    private void Start()
    {
        if (m_DisplayText != null)
        {
            m_DisplayText.text = string.Empty;
            m_Color = m_DisplayText.color;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (m_DisplayText != null)
        {
            if (NetworkManager.IsServer)
            {
                m_DisplayText.text = NetworkManager.IsHost ? "Host" : "Server";
            }
            else if (NetworkManager.IsClient)
            {
                m_DisplayText.text = "Client";
            }
        }
        transform.localPosition = m_LocalPosition;
    }

    private void OnGUI()
    {
        if (Application.isFocused)
        {
            m_DisplayText.color = m_Color;
        }
        else
        {
            m_DisplayText.color = Color.grey;
        }
    }
}
