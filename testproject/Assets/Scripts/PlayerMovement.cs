using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;


public class PlayerMovement : NetworkTransform
{
    public float Speed = 4.0f;

    public float RotSpeed = 1.0f;

    private Rigidbody m_Rigidbody;

    /// Used by <see cref="GrabbableBall"/>
    public static Dictionary<ulong, PlayerMovement> Players = new Dictionary<ulong, PlayerMovement>();

    /// <summary>
    /// Make this PlayerMovement-NetworkTransform component
    /// Owner Authoritative
    /// </summary>
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }

    private bool m_IsTeleporting;
    public bool IsTeleporting
    {
        get
        {
            return m_IsTeleporting;
        }
    }

    private float m_TickFrequency;
    private float m_DelayInputForTeleport;
    private Quaternion m_PreviousRotation;
    private RigidbodyInterpolation m_OrginalRigidbodyInterpolation;

    public void Telporting(Vector3 destination)
    {
        if (IsSpawned && IsOwner && !m_IsTeleporting)
        {
            m_IsTeleporting = true;

            // With rigid bodies, if you already know you are using standard
            // transform interpolation (not NetworkTransform) on the authoritative
            // side then you need to set it to Kinematic, preserve the current rigid
            // body interpolation value, and then finally set interpolation to none.
            m_Rigidbody.isKinematic = true;
            m_OrginalRigidbodyInterpolation = m_Rigidbody.interpolation;
            m_Rigidbody.interpolation = RigidbodyInterpolation.None;

            // We want to provide a few network ticks to pass in time before restoring
            // the rigid body back to its settings prior to being teleported.
            m_DelayInputForTeleport = Time.realtimeSinceStartup + (3f * m_TickFrequency);

            // Since the player-cube is a cube, when colliding with something it could
            // cause the cube to rotate based on the surface being collided against
            // and the facing of the cube. This prevents rotation from being changed
            // due to colliding with a side wall (and then teleported)
            transform.rotation = m_PreviousRotation;

            // Now teleport
            Teleport(destination, transform.rotation, transform.localScale);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            var temp = transform.position;
            temp.y = 0.5f;
            transform.position = temp;
            m_Rigidbody = GetComponent<Rigidbody>();
            m_TickFrequency = 1.0f / NetworkManager.NetworkTickSystem.TickRate;
        }

        /// Used by <see cref="GrabbableBall"/>
        Players[OwnerClientId] = this;

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if (Players.ContainsKey(OwnerClientId))
        {
            Players.Remove(OwnerClientId);
        }
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// LateUpdate is being used to check for the end of
    /// a player's teleporting cycle.
    /// </summary>
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

            /// We store this to handle a collision rotation issue: <see cref="Teleport(Vector3, Quaternion, Vector3)"/>
            m_PreviousRotation = transform.rotation;
        }
    }
}
