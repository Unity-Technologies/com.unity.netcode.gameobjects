using Unity.Netcode;
using UnityEngine;

public class NetworkObjectLabel : NetworkBehaviour
{
    public static bool GlobalVisibility = true;
    private TextMesh m_ObjectLabel;
    private MeshRenderer m_Renderer;

    private bool m_IsLabelVisible = true;

    public void SetLabelVisibility(bool isVisiable)
    {
        m_IsLabelVisible = isVisiable;

        ShowHideLabel(m_IsLabelVisible);
    }


    private void ShowHideLabel(bool isVisible)
    {
        if (m_Renderer == null)
        {
            m_Renderer = GetComponent<MeshRenderer>();
        }

        if (m_Renderer != null)
        {
            m_Renderer.enabled = isVisible;
        }
    }

    private void OnEnable()
    {
        if (m_IsLabelVisible)
        {
            ShowHideLabel(GlobalVisibility);
        }
    }

    private void OnDisable()
    {
        if (m_IsLabelVisible)
        {
            ShowHideLabel(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (m_ObjectLabel == null)
        {
            m_ObjectLabel = GetComponent<TextMesh>();
        }

        m_ObjectLabel.text = NetworkObject.NetworkObjectId.ToString();
        SetLabelVisibility(GlobalVisibility);

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if (m_ObjectLabel == null)
        {
            m_ObjectLabel = GetComponent<TextMesh>();
        }

        m_ObjectLabel.text = "None";

        base.OnNetworkDespawn();
    }
}
