using System;
using UnityEngine;
using Unity.Netcode;

public class MoveInCircle : NetworkBehaviour
{
    [SerializeField]
    private float m_MoveSpeed = 5;

    [SerializeField]
    private float m_RotationSpeed = 30;

    public bool runServerOnly;
    public bool runInUpdate;

    private Vector3 debug_oldPosition;

    // public override void OnNetworkSpawn()
    // {
    //     base.OnNetworkSpawn();//
    //     NetworkManager.NetworkTickSystem.Tick += NetworkTickUpdate;
    // }

    private float lastTime;

    void FixedUpdate()
    {
        if (runInUpdate) return;
        Tick(Time.fixedDeltaTime);
    }

    void Tick(float deltaTime)
    {
        if (NetworkManager.Singleton.IsServer || !runServerOnly)
        {
            debug_oldPosition = transform.position;
            transform.position = transform.position + transform.forward * (m_MoveSpeed * deltaTime);
            // Debug.Log($"ewqqwe {Math.Round((transform.position - debug_oldPosition).magnitude, 2)} time diff {Math.Round(Time.time - lastTime, 2)}");
            lastTime = Time.time;
            transform.Rotate(0, m_RotationSpeed * deltaTime, 0);
        }
    }

    private void Update()
    {
        if (!runInUpdate) return;
        Tick(Time.deltaTime);
    }
}
