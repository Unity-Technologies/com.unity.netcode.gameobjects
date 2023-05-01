using Unity.Netcode;
using UnityEngine;

public class GrabbableBall : NetworkBehaviour
{
    public float GrabDistance = 5.0f;

    private Rigidbody m_Rigidbody;
    private Material m_Material;

    private NetworkVariable<bool> m_IsGrabbed = new NetworkVariable<bool>();

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Material = GetComponent<Renderer>().material;
    }

    private void FixedUpdate()
    {
        if (NetworkManager == null)
        {
            return;
        }

        if (m_Rigidbody)
        {
            m_Rigidbody.isKinematic = !IsServer || m_IsGrabbed.Value;
        }
    }

    private void Update()
    {
        if (NetworkManager == null)
        {
            return;
        }

        var localPlayerObject = NetworkManager?.SpawnManager?.GetLocalPlayerObject();

        if (m_IsGrabbed.Value)
        {
            m_Material.color = Color.cyan;

            if (IsOwner && Input.GetKeyDown(KeyCode.F))
            {
                ReleaseServerRpc();
            }
        }
        else
        {
            m_Material.color = Color.white;

            if (localPlayerObject != null)
            {
                var distance = Vector3.Distance(transform.position, localPlayerObject.transform.position);
                if (distance <= GrabDistance)
                {
                    m_Material.color = Color.yellow;

                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        TryGrabServerRpc();
                    }
                }
            }
        }
    }

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        if (parentNetworkObject != null && (IsOwner || IsServer))
        {
            transform.localPosition = Vector3.up * 2;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryGrabServerRpc(ServerRpcParams serverRpcParams = default)
    {
        if (!m_IsGrabbed.Value)
        {
            var senderClientId = serverRpcParams.Receive.SenderClientId;
            var senderPlayerObject = PlayerMovement.Players[senderClientId].NetworkObject;
            if (senderPlayerObject != null)
            {
                NetworkObject.ChangeOwnership(senderClientId);

                transform.parent = senderPlayerObject.transform;

                m_IsGrabbed.Value = true;
            }
        }
    }

    [ServerRpc]
    private void ReleaseServerRpc()
    {
        if (m_IsGrabbed.Value)
        {
            NetworkObject.RemoveOwnership();

            transform.parent = null;

            m_IsGrabbed.Value = false;
        }
    }
}
