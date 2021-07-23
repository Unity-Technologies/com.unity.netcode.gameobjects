using MLAPI;
using UnityEngine;

public class MoveInCircle : NetworkBehaviour
{
    [SerializeField]
    private float m_MoveSpeed = 5;

    [SerializeField]
    private float m_RotationSpeed = 30;

    private Vector3 oldPosition;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();//
        NetworkManager.NetworkTickSystem.Tick += NetworkTickUpdate;
    }

    void NetworkTickUpdate() // doesn't work with Update?
    {
        if (NetworkManager.Singleton.IsServer)
        {
            oldPosition = transform.position;
            transform.position = transform.position + transform.forward * (m_MoveSpeed * NetworkManager.LocalTime.FixedDeltaTime);
            Debug.Log($"ewqqwe {(transform.position - oldPosition).magnitude}");
            transform.Rotate(0, m_RotationSpeed * NetworkManager.LocalTime.FixedDeltaTime, 0);
        }
    }
}
