using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class MoveInCircle : NetworkBehaviour
{
    [SerializeField]
    private float m_MoveSpeed = 5;

    [SerializeField]
    private float m_RotationSpeed = 30;

    [SerializeField]
    private bool m_RunServerOnly;

    [SerializeField]
    private bool m_RunInUpdate;

    private NetworkTransform m_NetworkTransform;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        m_NetworkTransform = GetComponent<NetworkTransform>();
        //NetworkManager.Singleton.NetworkTimeSystem.ServerBufferSec = 0.15f;
    }

    private void FixedUpdate()
    {
        if (m_RunInUpdate)
        {
            return;
        }
        Tick(true);
    }

    private void Update()
    {
        if (!m_RunInUpdate)
        {
            return;
        }
        Tick(false);
    }

    private void Tick(bool isFixed)
    {
        if (m_NetworkTransform.CanCommitToTransform || !m_RunServerOnly)
        {
            var deltaTime = isFixed ? Time.fixedDeltaTime : Time.deltaTime;
            transform.position = transform.position + transform.forward * (m_MoveSpeed * deltaTime);
            transform.Rotate(0, m_RotationSpeed * deltaTime, 0);
            transform.localScale = ((Mathf.Sin(isFixed ? Time.fixedTime : Time.time) + 1) * Vector3.one);
        }
    }
}
