using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class EnableDisableSceneNetworkObjectComponent : NetworkBehaviour
{
    [SerializeField]
    private MeshRenderer m_MyMeshRenderer;

    [SerializeField]
    private BoxCollider m_MyBoxCollider;

    [SerializeField]
    private Button m_ActivateObjectButton;

    private bool m_CurrentActiveState;


    private void Start()
    {
        //For this example, hide the button until NetworkStart is invoked.
        if (m_ActivateObjectButton)
        {
            m_ActivateObjectButton.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        //For this example, the server controls whether the mesh is visible and can collide or not
        if (IsServer && IsHost)
        {
            if (m_ActivateObjectButton)
            {
                m_ActivateObjectButton.gameObject.SetActive(true);
            }
        }
        base.OnNetworkSpawn();
    }

    public void ButtonActivateToggle()
    {
        m_CurrentActiveState = !m_CurrentActiveState;
        StartActivation(m_CurrentActiveState);
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
    }


    [ClientRpc]
    private void ActivateMeshObjectClientRpc(bool isActive)
    {
        Activate(isActive);
    }
}
