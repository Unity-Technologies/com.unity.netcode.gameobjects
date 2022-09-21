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
    [AddComponentMenu("Netcode/Network Rigidbody")]
    public class NetworkRigidbody : NetworkBehaviour
    {
        /// <summary>
        /// Determines if we are server (true) or owner (false) authoritative
        /// </summary>
        private bool m_IsServerAuthoritative;

        private Rigidbody m_Rigidbody;
        private NetworkTransform m_NetworkTransform;
        private RigidbodyInterpolation m_OriginalInterpolation;

        // Used to cache the authority state of this Rigidbody during the last frame
        private bool m_IsAuthority;

        private void Awake()
        {
            m_NetworkTransform = GetComponent<NetworkTransform>();
            m_IsServerAuthoritative = m_NetworkTransform.IsServerAuthoritative();

            m_Rigidbody = GetComponent<Rigidbody>();
            m_OriginalInterpolation = m_Rigidbody.interpolation;

            // Set interpolation to none if NetworkTransform is handling interpolation, otherwise it sets it to the original value
            m_Rigidbody.interpolation = m_NetworkTransform.Interpolate ? RigidbodyInterpolation.None : m_OriginalInterpolation;

            // Turn off physics for the rigid body until spawned, otherwise
            // clients can run fixed update before the first full
            // NetworkTransform update
            m_Rigidbody.isKinematic = true;
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

            // If you have authority then you are not kinematic
            m_Rigidbody.isKinematic = !m_IsAuthority;

            // Set interpolation of the Rigidbody based on authority
            // With authority: let local transform handle interpolation
            // Without authority: let the NetworkTransform handle interpolation
            m_Rigidbody.interpolation = m_IsAuthority ? m_OriginalInterpolation : RigidbodyInterpolation.None;
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            UpdateOwnershipAuthority();
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            m_Rigidbody.interpolation = m_OriginalInterpolation;
            // Turn off physics for the rigid body until spawned, otherwise
            // non-owners can run fixed updates before the first full
            // NetworkTransform update and physics will be applied (i.e. gravity, etc)
            m_Rigidbody.isKinematic = true;
        }
    }
}
#endif // COM_UNITY_MODULES_PHYSICS
