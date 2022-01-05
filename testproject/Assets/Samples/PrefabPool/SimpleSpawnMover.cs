using UnityEngine;
using Unity.Netcode;

public class SimpleSpawnMover : NetworkBehaviour
{
    private Vector3 m_Direction;
    [SerializeField]
    private float m_Velocity;

    [SerializeField]
    private float m_DestroyTime;

    private float m_TimeToDespawn;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        float ang = Random.Range(0.0f, 2 * Mathf.PI);
        m_Direction = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
        m_TimeToDespawn = Time.realtimeSinceStartup + m_DestroyTime;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (IsSpawned)
        {
            if (m_TimeToDespawn < Time.realtimeSinceStartup)
            {
                NetworkObject.Despawn();
            }
            else
            {
                transform.position += m_Direction * (m_Velocity * Time.fixedDeltaTime);
            }
        }
    }
}
