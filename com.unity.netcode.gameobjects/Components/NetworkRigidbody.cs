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
#if NGO_DAMODE
        public bool UseRigidBodyForMotion;
#endif

        private Rigidbody m_Rigidbody;
        private NetworkTransform m_NetworkTransform;
        private RigidbodyInterpolation m_OriginalInterpolation;

        // Used to cache the authority state of this Rigidbody during the last frame
        private bool m_IsAuthority;

        protected virtual void Awake()
        {
            m_NetworkTransform = GetComponent<NetworkTransform>();
            m_Rigidbody = GetComponent<Rigidbody>();
            SetupRigidbody();
        }

        /// <summary>
        /// If the current <see cref="NetworkTransform"/> has authority,
        /// then use the <see cref="RigidBody"/> interpolation strategy,
        /// if the <see cref="NetworkTransform"/> is handling interpolation,
        /// set interpolation to none on the <see cref="Rigidbody"/>
        /// <br/>
        /// Turn off physics for the rigid body until spawned, otherwise
        /// clients can run fixed update before the first
        /// full <see cref="NetworkTransform"/> update
        /// </summary>
        private void SetupRigidbody()
        {
            m_OriginalInterpolation = m_Rigidbody.interpolation;
#if NGO_DAMODE
            if (m_NetworkTransform != null)
            {
                m_NetworkTransform.RegisterRigidbody(this, m_Rigidbody);
            }
#endif
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

#if NGO_DAMODE
        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            if (NetworkManager.LocalClientId == current || NetworkManager.LocalClientId == previous)
            {
                UpdateOwnershipAuthority();
            }
            base.OnOwnershipChanged(previous, current);
        }
#endif

        /// <summary>
        /// Sets the authority differently depending upon
        /// whether it is server or owner authoritative
        /// </summary>
        internal void UpdateOwnershipAuthority()
        {
#if NGO_DAMODE
            if (NetworkManager.DistributedAuthorityMode)
            {
                // When in distributed authority mode, always use HasAuthority
                m_IsAuthority = HasAuthority;
            }
            else
#endif
            {
                if (m_NetworkTransform.IsServerAuthoritative())
                {
                    m_IsAuthority = NetworkManager.IsServer;
                }
                else
                {
                    m_IsAuthority = IsOwner;
                }
            }

            m_Rigidbody.isKinematic = !m_IsAuthority;

#if NGO_DAMODE
            if (UseRigidBodyForMotion)
            {
                if (m_Rigidbody.isKinematic)
                {
                    // Since we don't support kinematic extrapolation, if we are transitioning to kinematic mode
                    // and the user is using extrapolation on the authority side then we switch it to interpolation.
                    if (m_Rigidbody.interpolation == RigidbodyInterpolation.Extrapolate)
                    {
                        // Sleep until the next fixed update when switching
                        m_Rigidbody.Sleep();
                        m_Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                    }
                }
                else
                {
                    // Switch it back to the original interpolation if non-kinematic (doesn't require sleep).
                    if (m_Rigidbody.interpolation != m_OriginalInterpolation)
                    {
                        m_Rigidbody.interpolation = m_OriginalInterpolation;
                    }
                }
            }
            else
#endif
            {
                m_Rigidbody.interpolation = m_IsAuthority ? m_OriginalInterpolation : (m_NetworkTransform.Interpolate ? RigidbodyInterpolation.None : m_OriginalInterpolation);
            }
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            UpdateOwnershipAuthority();
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            //m_Rigidbody.interpolation = m_OriginalInterpolation;
            // Turn off physics for the rigid body until spawned, otherwise
            // non-owners can run fixed updates before the first full
            // NetworkTransform update and physics will be applied (i.e. gravity, etc)
            m_Rigidbody.isKinematic = true;
            m_Rigidbody.interpolation = m_OriginalInterpolation;
        }

#if NGO_DAMODE
        /// <summary>
        /// When using <see cref="NetworkRigidbody"/> with a <see cref="NetworkTransform"/> and <see cref="UseRigidBodyForMotion"/> is
        /// enabled, the <see cref="NetworkTransform"/> will update Kinematic instances using the <see cref="Rigidbody"/> move methods.
        /// This allows one to use <see cref="Rigidbody"/> interpolation on both the authoritative and nonauthoritative instances.
        /// </summary>
        /// <remarks>
        /// The <see cref="NetworkRigidbody"/> updates <see cref="NetworkTransform"/> during FixedUpdate to avoid the expense of having
        /// a FixedUpdate on <see cref="NetworkTransform"/> instances that do not have a <see cref="Rigidbody"/>.
        /// </remarks>
        private void FixedUpdate()
        {
            if (!IsSpawned || m_NetworkTransform == null || !UseRigidBodyForMotion)
            {
                return;
            }
            m_NetworkTransform.OnFixedUpdate();
        }
#endif
    }
}
#endif // COM_UNITY_MODULES_PHYSICS
