using UnityEngine;
using Unity.Netcode;

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

    private Vector3 m_DebugOldPosition;
    private float m_DebugLastTime;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
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
        if (NetworkManager.Singleton.IsServer || !m_RunServerOnly)
        {
            m_DebugOldPosition = transform.position;
            var deltaTime = isFixed ? Time.fixedDeltaTime : Time.deltaTime;
            transform.position = transform.position + transform.forward * (m_MoveSpeed * deltaTime);
            // Debug.Log($"ewqqwe {Math.Round((transform.position - debug_oldPosition).magnitude, 2)} time diff {Math.Round(Time.time - lastTime, 2)}");
            m_DebugLastTime = Time.time;
            transform.Rotate(0, m_RotationSpeed * deltaTime, 0);
            transform.localScale = ((Mathf.Sin(isFixed ? Time.fixedTime : Time.time)+1) * Vector3.one);
        }
    }
}
