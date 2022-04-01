#if COM_UNITY_MODULES_PHYSICS
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// NetworkRigidbody allows for the use of <see cref="Rigidbody"/> on network objects. By controlling the kinematic
    /// mode of the <see cref="Rigidbody"/> and disabling it on all peers but the authoritative one.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkTransform))]
    public class NetworkRigidbody : NetworkBehaviour
    {
        /// <summary>
        /// Determines if we are server (true) or owner (false) authoritative
        /// <see cref="NetworkTransform.IsServerAuthoritative"/>
        /// </summary>
        private bool m_IsServerAuthoritative;

        private Rigidbody m_Rigidbody;
        private NetworkTransform m_NetworkTransform;

        private bool m_OriginalKinematic;
        private RigidbodyInterpolation m_OriginalInterpolation;

        // Used to cache the authority state of this Rigidbody during the last frame
        private bool m_IsAuthority;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            // Store off the original kinematic state when instantiated
            m_OriginalKinematic = m_Rigidbody.isKinematic;

            // Turn off physics for the rigid body until spawned, otherwise
            // clients can run fixed update before the first full
            // NetworkTransform update
            m_Rigidbody.isKinematic = true;

            m_NetworkTransform = GetComponent<NetworkTransform>();
            m_IsServerAuthoritative = m_NetworkTransform.IsServerAuthoritative();
        }

        /// <summary>
        /// For owner authoritative (i.e. ClientNetworkTransform)
        /// we adjust our authority when we gain ownership
        /// </summary>
        public override void OnGainedOwnership()
        {
            UpdateOwnershipAuthority();
        }

        /// <summary>
        /// For owner authoritative(i.e. ClientNetworkTransform)
        /// we adjust our authority when we have lost ownership
        /// </summary>
        public override void OnLostOwnership()
        {
            UpdateOwnershipAuthority();
        }

        /// <summary>
        /// Sets the authority differently depending upon
        /// whether it is server or owner authoritative
        /// </summary>
        private void UpdateOwnershipAuthority()
        {
            if (m_IsServerAuthoritative)
            {
                m_IsAuthority = NetworkManager.IsServer;
            }
            else
            {
                m_IsAuthority = IsOwner;
            }
        }

        // Puts the Rigidbody in a kinematic non-interpolated mode on everyone but the server.
        private void UpdateRigidbodyKinematicMode()
        {
            if (m_IsAuthority == false)
            {
                m_Rigidbody.isKinematic = true;

                m_OriginalInterpolation = m_Rigidbody.interpolation;
                // Set interpolation to none, the NetworkTransform component interpolates the position of the object.
                m_Rigidbody.interpolation = RigidbodyInterpolation.None;
            }
            else
            {
                // Resets the Rigidbody back to it's non replication only state. Happens on shutdown and when authority is lost
                m_Rigidbody.isKinematic = m_OriginalKinematic;
                m_Rigidbody.interpolation = m_OriginalInterpolation;
            }
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            m_OriginalInterpolation = m_Rigidbody.interpolation;
            UpdateOwnershipAuthority();
            UpdateRigidbodyKinematicMode();
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            UpdateRigidbodyKinematicMode();
        }
    }
}
#endif // COM_UNITY_MODULES_PHYSICS
