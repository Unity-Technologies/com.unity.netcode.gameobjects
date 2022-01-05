using UnityEngine;
using Unity.Netcode;

public class SimplePlayerPositioning : NetworkBehaviour
{
    private float m_CurrentOffset = 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer && !IsOwner)
        {
            m_CurrentOffset += 1.5f;
            transform.position += new Vector3(m_CurrentOffset, 0, 0);
        }
        base.OnNetworkSpawn();
    }
}
