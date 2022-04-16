using UnityEngine;
using Unity.Netcode;

public class NetworkObjectLabel : NetworkBehaviour
{
    private TextMesh m_ObjectLabel;
    private MeshRenderer m_Renderer;


    public void LabelVisibility(bool isVisible = true)
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
        LabelVisibility();
    }

    public override void OnDestroy()
    {
        LabelVisibility(false);
        base.OnDestroy();
    }

    private void OnDisable()
    {
        LabelVisibility(false);
    }

    public override void OnNetworkSpawn()
    {
        if (m_ObjectLabel == null)
        {
            m_ObjectLabel = GetComponent<TextMesh>();
        }

        m_ObjectLabel.text = NetworkObject.NetworkObjectId.ToString();

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
