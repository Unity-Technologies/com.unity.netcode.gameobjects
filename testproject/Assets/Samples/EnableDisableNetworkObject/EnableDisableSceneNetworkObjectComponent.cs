using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EnableDisableSceneNetworkObjectComponent : NetworkBehaviour
{
    [SerializeField]
    private MeshRenderer m_MyMeshRenderer;

    [SerializeField]
    private BoxCollider m_MyBoxCollider;

    [SerializeField]
    private Button m_ActivateObjectButton;

    private Text m_ButtonText;

    private bool m_CurrentActiveState = true;

    private void Start()
    {
        if (m_ActivateObjectButton != null)
        {
            m_ButtonText = m_ActivateObjectButton.GetComponentInChildren<Text>();
            if (m_ButtonText != null)
            {
                m_ButtonText.text = "Hide";
            }

            m_ActivateObjectButton.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            m_ActivateObjectButton.gameObject.SetActive(true);
        }
        base.OnNetworkSpawn();
    }

    public void ButtonActivateToggle()
    {
        if (NetworkManager != null && NetworkManager.IsListening && IsServer)
        {
            m_CurrentActiveState = !m_CurrentActiveState;
            StartActivation(m_CurrentActiveState);
        }
    }

    public void StartActivation(bool isActive)
    {
        if (IsServer)
        {
            ActivateMeshObjectClientRpc(isActive);
            Activate(isActive);
        }
    }

    private void Activate(bool isActive)
    {
        if (m_MyMeshRenderer)
        {
            m_MyMeshRenderer.enabled = isActive;
        }

        if (m_MyBoxCollider)
        {
            m_MyBoxCollider.enabled = isActive;
        }

        if (m_ButtonText != null)
        {
            if (isActive)
            {
                m_ButtonText.text = "Hide";
            }
            else
            {
                m_ButtonText.text = "Show";
            }
        }
    }


    [ClientRpc]
    private void ActivateMeshObjectClientRpc(bool isActive)
    {
        Activate(isActive);
    }
}
