#if COM_UNITY_MODULES_PHYSICS
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// NetworkRigidbodyBase is a unified <see cref="Rigidbody"/> and <see cref="Rigidbody2D"/> integration that helps to synchronize physics motion, collision, and interpolation
    /// when used with a <see cref="NetworkTransform"/>.
    /// </summary>
    /// <remarks>
    /// For a customizable netcode Rigidbody, create your own component from this class and use <see cref="Initialize(RigidbodyTypes, NetworkTransform, Rigidbody2D, Rigidbody)"/>
    /// during instantiation (i.e. invoked from within the Awake method). You can re-initialize after having initialized but only when the <see cref="NetworkObject"/> is not spawned.
    /// </remarks>
    public abstract class NetworkRigidbodyBase : NetworkBehaviour
    {
        /// <summary>
        /// When enabled, the associated <see cref="NetworkTransform"/> will use the Rigidbody/Rigidbody2D to apply and synchronize changes in position, rotation, and
        /// allows for the use of Rigidbody interpolation/extrapolation.
        /// </summary>
        /// <remarks>
        /// If <see cref="NetworkTransform.Interpolate"/> is enabled, non-authoritative instances can only use Rigidbody interpolation. If a network prefab is set to
        /// extrapolation and <see cref="NetworkTransform.Interpolate"/> is enabled, then non-authoritative instances will automatically be adjusted to use Rigidbody
        /// interpolation while the authoritative instance will still use extrapolation.
        /// </remarks>
        public bool UseRigidBodyForMotion;

        /// <summary>
        /// When enabled (default), automatically set the Kinematic state of the Rigidbody based on ownership.
        /// When disabled, Kinematic state needs to be set by external script(s).
        /// </summary>
        public bool AutoUpdateKinematicState = true;

        /// <summary>
        /// Primarily applies to the <see cref="AutoUpdateKinematicState"/> property when disabled but you still want
        /// the Rigidbody to be automatically set to Kinematic when despawned.
        /// </summary>
        public bool AutoSetKinematicOnDespawn = true;

        // Determines if this is a Rigidbody or Rigidbody2D implementation
        private bool m_IsRigidbody2D => RigidbodyType == RigidbodyTypes.Rigidbody2D;
        // Used to cache the authority state of this Rigidbody during the last frame
        private bool m_IsAuthority;
        private Rigidbody m_Rigidbody;
        private Rigidbody2D m_Rigidbody2D;
        private NetworkTransform m_NetworkTransform;
        private enum InterpolationTypes
        {
            None,
            Interpolate,
            Extrapolate
        }
        private InterpolationTypes m_OriginalInterpolation;

        /// <summary>
        /// Used to define the type of Rigidbody implemented.
        /// <see cref=""/>
        /// </summary>
        public enum RigidbodyTypes
        {
            Rigidbody,
            Rigidbody2D,
        }

        public RigidbodyTypes RigidbodyType { get; private set; }

        /// <summary>
        /// Initializes the networked Rigidbody based on the <see cref="RigidbodyTypes"/>
        /// passed in as a parameter.
        /// </summary>
        /// <remarks>
        /// Cannot be initialized while the associated <see cref="NetworkObject"/> is spawned.
        /// </remarks>
        /// <param name="rigidbodyType">type of rigid body being initialized</param>
        /// <param name="rigidbody2D">(optional) The <see cref="Rigidbody2D"/> to be used</param>
        /// <param name="rigidbody">(optional) The <see cref="Rigidbody"/> to be used</param>
        protected void Initialize(RigidbodyTypes rigidbodyType, NetworkTransform networkTransform = null, Rigidbody2D rigidbody2D = null, Rigidbody rigidbody = null)
        {
            // Don't initialize if already spawned
            if (IsSpawned)
            {
                Debug.LogError($"[{name}] Attempting to initialize while spawned is not allowed.");
                return;
            }
            RigidbodyType = rigidbodyType;
            m_Rigidbody2D = rigidbody2D;
            m_Rigidbody = rigidbody;
            m_NetworkTransform = networkTransform;

            if (m_IsRigidbody2D && m_Rigidbody2D == null)
            {
                m_Rigidbody2D = GetComponent<Rigidbody2D>();

            }
            else if (m_Rigidbody == null)
            {
                m_Rigidbody = GetComponent<Rigidbody>();
            }

            SetOriginalInterpolation();

            if (m_NetworkTransform == null)
            {
                m_NetworkTransform = GetComponent<NetworkTransform>();
            }

            if (m_NetworkTransform != null)
            {
                m_NetworkTransform.RegisterRigidbody(this);
            }
            else
            {
                throw new System.Exception($"[Missing {nameof(NetworkTransform)}] No {nameof(NetworkTransform)} is assigned or can be found during initialization!");
            }

            if (AutoUpdateKinematicState)
            {
                SetIsKinematic(true);
            }
        }

        /// <summary>
        /// Gets the position of the Rigidbody
        /// </summary>
        /// <returns><see cref="Vector3"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetPosition()
        {
            if (m_IsRigidbody2D)
            {
                return m_Rigidbody2D.position;
            }
            else
            {
                return m_Rigidbody.position;
            }
        }

        /// <summary>
        /// Gets the rotation of the Rigidbody
        /// </summary>
        /// <returns><see cref="Quaternion"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion GetRotation()
        {
            if (m_IsRigidbody2D)
            {
                var quaternion = Quaternion.identity;
                var angles = quaternion.eulerAngles;
                angles.z = m_Rigidbody2D.rotation;
                quaternion.eulerAngles = angles;
                return quaternion;
            }
            else
            {
                return m_Rigidbody.rotation;
            }
        }

        /// <summary>
        /// Moves the rigid body
        /// </summary>
        /// <param name="position">The <see cref="Vector3"/> position to move towards</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MovePosition(Vector3 position)
        {
            if (m_IsRigidbody2D)
            {
                m_Rigidbody2D.MovePosition(position);
            }
            else
            {
                m_Rigidbody.MovePosition(position);
            }
        }

        /// <summary>
        /// Directly applies a position (like teleporting)
        /// </summary>
        /// <param name="position"><see cref="Vector3"/> position to apply to the Rigidbody</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPosition(Vector3 position)
        {
            if (m_IsRigidbody2D)
            {
                m_Rigidbody2D.position = position;
            }
            else
            {
                m_Rigidbody.position = position;
            }
        }

        /// <summary>
        /// Applies the rotation and position of the <see cref="GameObject"/>'s <see cref="Transform"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyCurrentTransform()
        {
            if (m_IsRigidbody2D)
            {
                m_Rigidbody2D.position = transform.position;
                m_Rigidbody2D.rotation = transform.eulerAngles.z;
            }
            else
            {
                m_Rigidbody.position = transform.position;
                m_Rigidbody.rotation = transform.rotation;
            }
        }

        /// <summary>
        /// Rotatates the Rigidbody towards a specified rotation
        /// </summary>
        /// <param name="rotation">The rotation expressed as a <see cref="Quaternion"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveRotation(Quaternion rotation)
        {
            if (m_IsRigidbody2D)
            {
                m_Rigidbody2D.MoveRotation(rotation);
            }
            else
            {
                m_Rigidbody.MoveRotation(rotation);
            }
        }

        /// <summary>
        /// Applies a rotation to the Rigidbody
        /// </summary>
        /// <param name="rotation">The rotation to apply expressed as a <see cref="Quaternion"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRotation(Quaternion rotation)
        {
            if (m_IsRigidbody2D)
            {
                m_Rigidbody2D.rotation = rotation.eulerAngles.z;
            }
            else
            {
                m_Rigidbody.rotation = rotation;
            }
        }

        /// <summary>
        /// Sets the original interpolation of the Rigidbody while taking the Rigidbody type into consideration
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetOriginalInterpolation()
        {
            if (m_IsRigidbody2D)
            {
                switch (m_Rigidbody2D.interpolation)
                {
                    case RigidbodyInterpolation2D.None:
                        {
                            m_OriginalInterpolation = InterpolationTypes.None;
                            break;
                        }
                    case RigidbodyInterpolation2D.Interpolate:
                        {
                            m_OriginalInterpolation = InterpolationTypes.Interpolate;
                            break;
                        }
                    case RigidbodyInterpolation2D.Extrapolate:
                        {
                            m_OriginalInterpolation = InterpolationTypes.Extrapolate;
                            break;
                        }
                }
            }
            else
            {
                switch (m_Rigidbody.interpolation)
                {
                    case RigidbodyInterpolation.None:
                        {
                            m_OriginalInterpolation = InterpolationTypes.None;
                            break;
                        }
                    case RigidbodyInterpolation.Interpolate:
                        {
                            m_OriginalInterpolation = InterpolationTypes.Interpolate;
                            break;
                        }
                    case RigidbodyInterpolation.Extrapolate:
                        {
                            m_OriginalInterpolation = InterpolationTypes.Extrapolate;
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Wakes the Rigidbody if it is sleeping
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WakeIfSleeping()
        {
            if (m_IsRigidbody2D)
            {
                if (m_Rigidbody2D.IsSleeping())
                {
                    m_Rigidbody2D.WakeUp();
                }
            }
            else
            {
                if (m_Rigidbody.IsSleeping())
                {
                    m_Rigidbody.WakeUp();
                }
            }
        }

        /// <summary>
        /// Puts the Rigidbody to sleep
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SleepRigidbody()
        {
            if (m_IsRigidbody2D)
            {
                m_Rigidbody2D.Sleep();
            }
            else
            {
                m_Rigidbody.Sleep();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsKinematic()
        {
            if (m_IsRigidbody2D)
            {
                return m_Rigidbody2D.isKinematic;
            }
            else
            {
                return m_Rigidbody.isKinematic;
            }
        }

        /// <summary>
        /// Sets the kinematic state of the Rigidbody and handles updating the Rigidbody's
        /// interpolation setting based on the Kinematic state.
        /// </summary>
        /// <remarks>
        /// When using the Rigidbody for <see cref="NetworkTransform"/> motion, this automatically
        /// adjusts from extrapolation to interpolation if:
        /// - The Rigidbody was originally set to extrapolation
        /// - The NetworkTransform is set to interpolate
        /// When the two above conditions are true:
        /// - When switching from non-kinematic to kinematic this will automatically
        /// switch the Rigidbody from extrapolation to interpolate.
        /// - When switching from kinematic to non-kinematic this will automatically
        /// switch the Rigidbody from interpolation back to extrapolation.
        /// </remarks>
        /// <param name="isKinematic"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetIsKinematic(bool isKinematic)
        {
            if (m_IsRigidbody2D)
            {
                m_Rigidbody2D.isKinematic = isKinematic;
            }
            else
            {
                m_Rigidbody.isKinematic = isKinematic;
            }

            // If we are not spawned, then exit early
            if (!IsSpawned)
            {
                return;
            }

            if (UseRigidBodyForMotion)
            {
                // Only if the NetworkTransform is set to interpolate do we need to check for extrapolation
                if (m_NetworkTransform.Interpolate && m_OriginalInterpolation == InterpolationTypes.Extrapolate)
                {
                    if (IsKinematic())
                    {
                        // If not already set to interpolate then set the Rigidbody to interpolate 
                        if (m_Rigidbody.interpolation == RigidbodyInterpolation.Extrapolate)
                        {
                            // Sleep until the next fixed update when switching from extrapolation to interpolation
                            SleepRigidbody();
                            SetInterpolation(InterpolationTypes.Interpolate);
                        }
                    }
                    else
                    {
                        // Switch it back to the original interpolation if non-kinematic (doesn't require sleep).
                        SetInterpolation(m_OriginalInterpolation);
                    }
                }
            }
            else
            {
                SetInterpolation(m_IsAuthority ? m_OriginalInterpolation : (m_NetworkTransform.Interpolate ? InterpolationTypes.None : m_OriginalInterpolation));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetInterpolation(InterpolationTypes interpolationType)
        {
            switch (interpolationType)
            {
                case InterpolationTypes.None:
                    {
                        if (m_IsRigidbody2D)
                        {
                            m_Rigidbody2D.interpolation = RigidbodyInterpolation2D.None;
                        }
                        else
                        {
                            m_Rigidbody.interpolation = RigidbodyInterpolation.None;
                        }
                        break;
                    }
                case InterpolationTypes.Interpolate:
                    {
                        if (m_IsRigidbody2D)
                        {
                            m_Rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
                        }
                        else
                        {
                            m_Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                        }
                        break;
                    }
                case InterpolationTypes.Extrapolate:
                    {
                        if (m_IsRigidbody2D)
                        {
                            m_Rigidbody2D.interpolation = RigidbodyInterpolation2D.Extrapolate;
                        }
                        else
                        {
                            m_Rigidbody.interpolation = RigidbodyInterpolation.Extrapolate;
                        }
                        break;
                    }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetInterpolation()
        {
            SetInterpolation(m_OriginalInterpolation);
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            UpdateOwnershipAuthority();
            base.OnOwnershipChanged(previous, current);
        }

        /// <summary>
        /// Sets the authority based on whether it is server or owner authoritative
        /// </summary>
        /// <remarks>
        /// Distributed authority sessions will always be owner authoritative.
        /// </remarks>
        internal void UpdateOwnershipAuthority()
        {
            if (NetworkManager.DistributedAuthorityMode)
            {
                // When in distributed authority mode, always use HasAuthority
                m_IsAuthority = HasAuthority;
            }
            else
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

            if (AutoUpdateKinematicState)
            {
                SetIsKinematic(!m_IsAuthority);
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
            // If we are automatically handling the kinematic state...
            if (AutoUpdateKinematicState || AutoSetKinematicOnDespawn)
            {
                // Turn off physics for the rigid body until spawned, otherwise
                // non-owners can run fixed updates before the first full
                // NetworkTransform update and physics will be applied (i.e. gravity, etc)
                SetIsKinematic(true);
            }
            SetInterpolation(m_OriginalInterpolation);
        }

        /// <summary>
        /// When <see cref="UseRigidBodyForMotion"/> is enabled, the <see cref="NetworkTransform"/> will update Kinematic instances using
        /// the Rigidbody's move methods allowing Rigidbody interpolation settings to be taken into consideration by the physics simulation.
        /// </summary>
        /// <remarks>
        /// This will update the associated <see cref="NetworkTransform"/> during FixedUpdate which also avoids the added expense of adding
        /// a FixedUpdate to all <see cref="NetworkTransform"/> instances where some might not be using a Rigidbody.
        /// </remarks>
        private void FixedUpdate()
        {
            if (!IsSpawned || m_NetworkTransform == null || !UseRigidBodyForMotion)
            {
                return;
            }
            m_NetworkTransform.OnFixedUpdate();
        }
    }
}
#endif // COM_UNITY_MODULES_PHYSICS

