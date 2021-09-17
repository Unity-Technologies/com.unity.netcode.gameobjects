using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField]
    private float m_Speed = 20.0f;
    [SerializeField]
    private float m_RotSpeed = 5.0f;

    public static Dictionary<ulong, PlayerMovement> Players = new Dictionary<ulong, PlayerMovement>();

    private void Start()
    {
        if (IsLocalPlayer)
        {
            var temp = transform.position;
            temp.y = 0.5f;
            transform.position = temp;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Players[OwnerClientId] = this; // todo should really have a NetworkStop for unregistering this...
    }

    private void FixedUpdate()
    {
        if (IsLocalPlayer)
        {
            transform.position += Input.GetAxis("Vertical") * m_Speed * Time.fixedDeltaTime * transform.forward;
            transform.rotation = Quaternion.Euler(0, Input.GetAxis("Horizontal") * 90 * m_RotSpeed * Time.fixedDeltaTime, 0) * transform.rotation;
        }
    }
}
