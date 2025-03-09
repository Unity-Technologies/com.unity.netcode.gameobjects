using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Identifier : NetworkBehaviour
{
    private Text m_DisplayText;


    private Color m_Color;
    private Color m_OriginalColor;
    private Color m_ColorAlpha;
    private Vector3 m_AnchoredPosition;

    public void SetColor(Color color)
    {
        m_Color = color;
        m_ColorAlpha = color;
        m_ColorAlpha.a = 0.35f;
    }

    private void Awake()
    {
        m_AnchoredPosition = (transform as RectTransform).anchoredPosition3D;
        m_DisplayText = GetComponent<Text>();
        m_OriginalColor = m_DisplayText.color;
        if (m_DisplayText != null)
        {
            m_DisplayText.text = string.Empty;
            SetColor(m_DisplayText.color);
        }
    }

    public override void OnNetworkSpawn()
    {
        SetColor(ClientColorExtension.GetClientColor(NetworkManager.LocalClientId));
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
        (transform as RectTransform).anchoredPosition3D = m_AnchoredPosition;
        UpdateTextColor();
    }

    protected override void OnInSceneObjectsSpawned()
    {

        base.OnInSceneObjectsSpawned();
    }

    public override void OnNetworkDespawn()
    {
        if (m_DisplayText != null)
        {
            m_DisplayText.text = string.Empty;
            m_DisplayText.color = m_OriginalColor;
        }
        base.OnNetworkDespawn();
    }

    private bool m_LastFocusedValue;
    public void UpdateTextColor()
    {
        if (m_LastFocusedValue)
        {
            m_DisplayText.color = m_Color;
        }
        else
        {
            m_DisplayText.color = m_ColorAlpha;
        }
    }

    private void OnGUI()
    {
        if (!IsSpawned || m_LastFocusedValue == Application.isFocused)
        {
            return;
        }

        m_LastFocusedValue = Application.isFocused;

        UpdateTextColor();
    }
}
