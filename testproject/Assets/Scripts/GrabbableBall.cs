using UnityEngine;
using MLAPI;
using MLAPI.NetworkVariable;
using MLAPI.Messaging;

public class GrabbableBall : NetworkBehaviour
{
    public float GrabDistance = 5.0f;

    private Rigidbody m_Rigidbody;
    private Material m_Material;

    private NetworkVariable<bool> m_IsGrabbed = new NetworkVariable<bool>();
    private Transform m_CachedParent = null;

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

                m_IsGrabbed.Value = true;

                UpdateParentClientRpc(NetworkObject.OwnerClientId, false);
            }
        }
    }

    [ServerRpc]
    private void ReleaseServerRpc()
    {
        if (m_IsGrabbed.Value)
        {
            NetworkObject.RemoveOwnership();

            m_IsGrabbed.Value = false;

            UpdateParentClientRpc(NetworkObject.OwnerClientId, true);
        }
    }

    [ClientRpc]
    private void UpdateParentClientRpc(ulong ownerClientId, bool isFree)
    {
        var playerObject = PlayerMovement.Players[ownerClientId].NetworkObject;

        if (isFree || playerObject == null)
        {
            transform.parent = m_CachedParent;
        }
        else
        {
            m_CachedParent = transform.parent;
            transform.parent = playerObject.transform;
            transform.localPosition = Vector3.up * (1 / playerObject.transform.localScale.y);
        }

        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.rotation = Quaternion.identity;
    }
}
