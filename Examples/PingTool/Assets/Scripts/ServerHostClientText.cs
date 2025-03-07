using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ServerHostClientText : NetworkBehaviour
{
    private Text m_DisplayText;

    private Color m_Color;
    private Color m_ColorAlpha;
    private Vector3 m_AnchoredPosition3D;

    public void SetColor(Color color)
    {
        m_Color = color;
        m_ColorAlpha = color;
        m_ColorAlpha.a = 0.35f;
    }

    private void Awake()
    {
        // Get the anchored 3D position
        m_AnchoredPosition3D = (transform as RectTransform).anchoredPosition3D;
        m_DisplayText = GetComponent<Text>();
    }

    private void Start()
    {
        if (m_DisplayText != null)
        {
            m_DisplayText.text = string.Empty;
            SetColor(m_DisplayText.color);
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
                m_DisplayText.text = $"Client-{NetworkManager.LocalClientId}";
            }
        }
        // Apply the AnchoredPosition3D to account for any adjustments in resolution/size
        (transform as RectTransform).anchoredPosition3D = m_AnchoredPosition3D;
    }

    public override void OnNetworkDespawn()
    {
        if (m_DisplayText != null)
        {
            m_DisplayText.text = string.Empty;
        }
        base.OnNetworkDespawn();
    }

    private bool m_LastFocusedValue;
    private void OnGUI()
    {
        if (!IsSpawned || m_LastFocusedValue == Application.isFocused)
        {
            return;
        }

        m_LastFocusedValue = Application.isFocused;

        if (m_LastFocusedValue)
        {
            m_DisplayText.color = m_Color;
        }
        else
        {
            m_DisplayText.color = m_ColorAlpha;
        }
    }
}
