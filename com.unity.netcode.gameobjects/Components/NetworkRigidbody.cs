using System;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// NetworkRigidbody allows for the use of <see cref="Rigidbody"/> on network objects.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkTransform))]
    public class NetworkRigidbody : NetworkBehaviour
    {
        private NetworkTransform m_NetworkTransform;
        private Rigidbody m_Rigidbody;

        private bool m_OriginalKinematic;

        // Used to cache the authority state of this rigidbody during the last frame
        private bool m_IsAuthority;

        /// <summary>
        /// Gets a bool value indicating whether this <see cref="NetworkRigidbody"/> on this peer currently holds authority.
        /// </summary>
        internal bool HasAuthority => NetworkManager.IsServer; // TODO update this once we support owner authoritative NetworkTransform.

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_NetworkTransform = GetComponent<NetworkTransform>();
        }

        // Currently commented out because it is not needed as authority currently can't change at runtime.
        // private void FixedUpdate()
        // {
        //     if (NetworkManager.IsListening)
        //     {
        //         if (HasAuthority != m_IsAuthority)
        //         {
        //             m_IsAuthority = HasAuthority;
        //             UpdateRigidbodyKinematicMode();
        //         }
        //     }
        // }

        // Puts the rigidbody in a kinematic non-interpolated mode on everyone but the server.
        public void UpdateRigidbodyKinematicMode()
        {
            if (m_IsAuthority == false)
            {
                m_OriginalKinematic = m_Rigidbody.isKinematic;
                m_Rigidbody.isKinematic = true;
            }
            else
            {
                // Resets the rigidbody back to it's non replication only state. Happens on shutdown and when authority is lost
                m_Rigidbody.isKinematic = m_OriginalKinematic;
            }
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            m_IsAuthority = HasAuthority;
            UpdateRigidbodyKinematicMode();
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            m_IsAuthority = false;
            UpdateRigidbodyKinematicMode();
        }
    }
}
