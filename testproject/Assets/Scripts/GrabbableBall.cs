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

        if (IsOwner && m_IsGrabbed.Value && localPlayerObject != null)
        {
            transform.position = localPlayerObject.transform.position + Vector3.up;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryGrabServerRpc(ServerRpcParams serverRpcParams = default)
    {
        if (!m_IsGrabbed.Value)
        {
            var senderClientId = serverRpcParams.Receive.SenderClientId;
            var senderPlayerObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(senderClientId);
            if (senderPlayerObject != null)
            {
                NetworkObject.ChangeOwnership(senderClientId);

                transform.position = senderPlayerObject.transform.position + Vector3.up;
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.rotation = Quaternion.identity;
                // transform.parent = senderPlayerObject.transform;
                // transform.localPosition = Vector3.up*4;
                // transform.localScale = Vector3.one;

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
            // transform.parent = null;
            // transform.localScale = Vector3.one;
            m_IsGrabbed.Value = false;
        }
    }
}
