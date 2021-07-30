using UnityEngine;
using MLAPI;

public class NetworkObjectLabel : NetworkBehaviour
{

    private TextMesh m_ObjectLabel;

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
