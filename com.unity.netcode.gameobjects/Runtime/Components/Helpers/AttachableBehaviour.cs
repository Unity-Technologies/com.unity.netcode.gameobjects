using System;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Handles parenting the <see cref="GameObject"/> that is a child under a root network prefab
    /// without having to use the same <see cref="NetworkObject"/> parenting rules.
    /// </summary>
    public class AttachableBehaviour : NetworkBehaviour
    {
        /// <summary>
        /// Invoked just prior to the parent being applied.
        /// </summary>
        /// <remarks>
        /// The <see cref="NetworkBehaviour"/> parameter passed into a susbcriber callback will be either null or a valid. <br />
        /// When null, the parent is being unapplied/removed from the <see cref="GameObject"/> this <see cref="AttachableBehaviour"/> instance is attached to.
        /// </remarks>
        public event Action<NetworkBehaviour> ParentIsBeingApplied;

        private NetworkVariable<NetworkBehaviourReference> m_AppliedParent = new NetworkVariable<NetworkBehaviourReference>(new NetworkBehaviourReference(null));
        private GameObject m_DefaultParent;
        private Vector3 m_OriginalLocalPosition;
        private Quaternion m_OriginalLocalRotation;

        /// <summary>
        /// Will be true when this <see href="AttachableBehaviour"/> instance has a parent applied to it.<br />
        /// Will be false when this <see href="AttachableBehaviour"/> instance does not have a parent applied to it.<br />
        /// </summary>
        public bool ParentIsApplied { get; private set; }

        /// <inheritdoc/>
        protected virtual void Awake()
        {
            m_DefaultParent = transform.parent == null ? gameObject : transform.parent.gameObject;
            m_OriginalLocalPosition = transform.localPosition;
            m_OriginalLocalRotation = transform.localRotation;
        }

        /// <inheritdoc/>
        protected override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            if (HasAuthority)
            {
                m_AppliedParent.Value = new NetworkBehaviourReference(null);
            }
            UpdateParent();
            m_AppliedParent.OnValueChanged += OnAppliedParentChanged;
        }

        /// <inheritdoc/>
        public override void OnNetworkDespawn()
        {
            ResetToDefault();
            base.OnNetworkDespawn();
        }

        private void OnAppliedParentChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
        {
            UpdateParent();
        }

        private void UpdateParent()
        {
            var parent = (NetworkBehaviour)null;
            if (m_AppliedParent.Value.TryGet(out parent))
            {
                ParentIsApplied = true;
                transform.SetParent(parent.gameObject.transform, false);
            }
            else
            {
                ParentIsApplied = false;
                ResetToDefault();
            }

            OnParentUpdated(parent);
        }

        private void ResetToDefault()
        {
            if (m_DefaultParent != null)
            {
                transform.SetParent(m_DefaultParent.transform, false);
            }
            transform.localPosition = m_OriginalLocalPosition;
            transform.localRotation = m_OriginalLocalRotation;
        }

        /// <summary>
        /// Invoked after the parent has been applied.<br />
        /// </summary>
        /// <remarks>
        /// The <param name="parent"/> can be either null or a valid <see cref="NetworkBehaviour"/>. <br />
        /// When null, the parent is being unapplied/removed from the <see cref="GameObject"/> this <see cref="AttachableBehaviour"/> instance is attached to.
        /// </remarks>
        /// <param name="parent">The <see cref="NetworkBehaviour"/> that is applied or null if it is no longer applied.</param>
        protected virtual void OnParentUpdated(NetworkBehaviour parent)
        {

        }

        /// <summary>
        /// Invoked just prior to the parent being applied. <br />
        /// This is a good time to handle disabling or enabling <see cref="Object"/>s using an <see cref="ObjectController"/>.
        /// </summary>
        /// <remarks>
        /// The <param name="parent"/> can be either null or a valid <see cref="NetworkBehaviour"/>. <br />
        /// When null, the parent is being unapplied/removed from the <see cref="GameObject"/> this <see cref="AttachableBehaviour"/> instance is attached to.
        /// </remarks>
        /// <param name="parent">The <see cref="NetworkBehaviour"/> that is applied or null if it is no longer applied.</param>
        protected virtual void OnParentBeingApplied(NetworkBehaviour parent)
        {

        }

        /// <summary>
        /// Applies a parent to a nested <see cref="NetworkBehaviour"/> and all <see cref="GameObject"/> children
        /// of the nested <see cref="NetworkBehaviour"/>.
        /// </summary>
        /// <param name="parent">The <see cref="NetworkBehaviour"/> to be applied or null to reparent under its original <see cref="GameObject"/> when spawned.</param>
        public void ApplyParent(NetworkBehaviour parent)
        {
            if (!IsSpawned)
            {
                Debug.LogError($"[{name}][Not Spawned] Can only have a parent applied when it is spawned!");
                return;
            }

            if (!HasAuthority)
            {
                Debug.LogError($"[{name}][Not Authority] Client-{NetworkManager.LocalClientId} is not the authority!");
                return;
            }
            // Notify any subscriptions
            ParentIsBeingApplied?.Invoke(parent);

            // Invoke for any overrides
            OnParentBeingApplied(parent);

            // Once everything has been notified that we are applying a parent...apply the parent.
            m_AppliedParent.Value = new NetworkBehaviourReference(parent);
        }
    }
}
