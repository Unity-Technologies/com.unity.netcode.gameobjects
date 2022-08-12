using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;


public class PlayerMovement : NetworkTransform
{

    public float Speed = 4.0f;

    public float RotSpeed = 1.0f;

    private Rigidbody m_Rigidbody;
    public static Dictionary<ulong, PlayerMovement> Players = new Dictionary<ulong, PlayerMovement>();

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }

    private float m_DelayInputForTeleport;

    private bool m_IsTeleporting;
    public bool IsTeleporting
    {
        get
        {
            return m_IsTeleporting;
        }
    }

    private float m_TickFrequency;
    private Quaternion m_PreviousRotation;
    private RigidbodyInterpolation m_OrginalRigidbodyInterpolation;
    private Vector3 m_TeleportDestination;

    public void Telporting(Vector3 destination)
    {
        if (IsSpawned && IsOwner && !m_IsTeleporting)
        {
            m_IsTeleporting = true;
            m_TeleportDestination = destination;
            m_Rigidbody.isKinematic = true;
            m_OrginalRigidbodyInterpolation = m_Rigidbody.interpolation;
            m_Rigidbody.interpolation = RigidbodyInterpolation.None;
            // Since the player-cube is a cube, when colliding with something it could
            // cause the cube to rotate based on the surface being collided against
            // and the facing of the cube. This prevents rotation from being changed
            // due to colliding with a side wall (and then teleported)
            transform.rotation = m_PreviousRotation;
            m_DelayInputForTeleport = Time.realtimeSinceStartup + (3f * m_TickFrequency);
            // Now teleport
            Teleport(m_TeleportDestination, transform.rotation, transform.localScale);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            var temp = transform.position;
            temp.y = 0.5f;
            transform.position = temp;
        }
        m_Rigidbody = GetComponent<Rigidbody>();
        m_TickFrequency = 1.0f / NetworkManager.NetworkTickSystem.TickRate;
        Players[OwnerClientId] = this; // todo should really have a NetworkStop for unregistering this...

        base.OnNetworkSpawn();
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsOwner || !m_IsTeleporting)
        {
            return;
        }
        if (Time.realtimeSinceStartup >= m_DelayInputForTeleport)
        {
            m_IsTeleporting = false;
            m_Rigidbody.isKinematic = false;
            m_Rigidbody.interpolation = m_OrginalRigidbodyInterpolation;
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner)
        {
            return;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, transform.position + Input.GetAxis("Vertical") * Speed * transform.forward, Time.fixedDeltaTime);
            var rotation = transform.rotation;
            var euler = rotation.eulerAngles;
            euler.y += Input.GetAxis("Horizontal") * 90 * RotSpeed * Time.fixedDeltaTime;
            rotation.eulerAngles = euler;
            transform.rotation = rotation;
            // This allows us to rollback to the previous rotation for the teleport
            // sample. If we don't do this the box will collide with the wall and
            // will slightly rotate the box to align with the wall
            m_PreviousRotation = transform.rotation;
        }
    }
}
