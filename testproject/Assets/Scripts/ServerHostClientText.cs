using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ServerHostClientText : NetworkBehaviour
{
    [SerializeField]
    private Text m_DisplayText;

    private Color m_Color;
    private Color m_ColorAlpha;
    private Vector3 m_LocalPosition;



    public void SetColor(Color color)
    {
        m_Color = color;
        m_ColorAlpha = color;
        m_ColorAlpha.a = 0.35f;
    }

    private void Awake()
    {
        m_LocalPosition = transform.localPosition;
    }

    private void Start()
    {
        Screen.SetResolution((int)(Screen.currentResolution.width * 0.40f), (int)(Screen.currentResolution.height * 0.40f), false);
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
        transform.localPosition = m_LocalPosition;
        //m_DisplayText.rectTransform.position = new Vector3(0.0f, 30.0f, 0.0f);
    }

    private void OnGUI()
    {
        if (!IsSpawned)
        {
            return;
        }
        if (Application.isFocused)
        {
            m_DisplayText.color = m_Color;
        }
        else
        {
            m_DisplayText.color = m_ColorAlpha;
        }
    }
}
