using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Attachable NetworkBehaviours<br/>
    /// This component handles the parenting synchronization of the <see cref="GameObject"/> that this component is attached to.<br />
    /// under another <see cref="NetworkBehaviour"/>'s <see cref="GameObject"/>.<br />
    /// The <see cref="GameObject"/> to be parented must have this component attached to it and must be nested on any child <see cref="GameObject"/> under the <see cref="NetworkObject"/>'s <see cref="GameObject"/>.<br />
    /// The <see cref="GameObject"/> target parent must have an <see cref="AttachableNode"/> component attached to it and must belong to a
    /// different <see cref="NetworkObject"/> than that of the <see cref="AttachableBehaviour"/>'s.
    /// </summary>
    /// <remarks>
    /// The term "attach" is used in place of parenting in order to distinguish between <see cref="NetworkObject"/> parenting and
    /// <see cref="AttachableBehaviour"/> parenting ("attaching" and "detaching").<br />
    /// This component can be used along with one or more <see cref="ComponentController"/> in order to enable or disable different components depending
    /// upon the <see cref="AttachableBehaviour"/> instance's current state.
    /// </remarks>
    public class AttachableBehaviour : NetworkBehaviour
    {
#if UNITY_EDITOR
        /// <inheritdoc/>
        /// <remarks>
        /// In the event an <see cref="AttachableBehaviour"/> is placed on the same <see cref="GameObject"/>
        /// as the <see cref="NetworkObject"/>, this will automatically create a child and add an
        /// <see cref="AttachableBehaviour"/> to that.
        /// </remarks>
        protected virtual void OnValidate()
        {
            var networkObject = gameObject.GetComponentInParent<NetworkObject>();
            if (!networkObject)
            {
                networkObject = gameObject.GetComponent<NetworkObject>();
            }
            if (networkObject && networkObject.gameObject == gameObject)
            {
                Debug.LogWarning($"[{name}][{nameof(AttachableBehaviour)}] Cannot be placed on the same {nameof(GameObject)} as the {nameof(NetworkObject)}!");
                // Wait for the next editor update to create a nested child and add the AttachableBehaviour
                EditorApplication.update += CreatedNestedChild;
            }
        }

        private void CreatedNestedChild()
        {
            EditorApplication.update -= CreatedNestedChild;
            var childGameObject = new GameObject($"{name}-Child");
            childGameObject.transform.parent = transform;
            childGameObject.AddComponent<AttachableBehaviour>();
            Debug.Log($"[{name}][Created Child] Adding {nameof(AttachableBehaviour)} to newly created child {childGameObject.name}.");
            DestroyImmediate(this);
        }
#endif

        /// <summary>
        /// Invoked when the <see cref="AttachState"/> of this instance has changed.
        /// </summary>
        public event Action<AttachState, AttachableNode> AttachStateChange;

        /// <summary>
        /// The various states of <see cref="AttachableBehaviour"/>.
        /// </summary>
        public enum AttachState
        {
            /// <summary>
            /// The <see cref="AttachableBehaviour"/> instance is not attached to anything.
            /// When not attached to anything, the instance will be parented under the original
            /// <see cref="GameObject"/>.
            /// </summary>
            Detached,
            /// <summary>
            /// The <see cref="AttachableBehaviour"/> instance is attaching to an <see cref="AttachableNode"/>.
            /// </summary>
            /// <remarks>
            /// One example usage:<br />
            /// When using an <see cref="AttachableBehaviour"/> with one or more <see cref="ComponentController"/> component(s),
            /// this would be a good time to enable or disable components.
            /// </remarks>
            Attaching,
            /// <summary>
            /// The <see cref="AttachableBehaviour"/> instance is attached to an <see cref="AttachableNode"/>.
            /// </summary>
            /// <remarks>
            /// This would be a good time to apply any additional local position or rotation values to this <see cref="AttachableBehaviour"/> instance.
            /// </remarks>
            Attached,
            /// <summary>
            /// The <see cref="AttachableBehaviour"/> instance is detaching from an <see cref="AttachableNode"/>.
            /// </summary>
            /// <remarks>
            /// One example usage:<br />
            /// When using an <see cref="AttachableBehaviour"/> with one or more <see cref="ComponentController"/> component(s),
            /// this would be a good time to enable or disable components.
            /// </remarks>
            Detaching
        }

        /// <summary>
        /// The current <see cref="AttachableBehaviour"/> instance's <see cref="AttachState"/>.
        /// </summary>
        protected AttachState m_AttachState { get; private set; }

        /// <summary>
        /// The original parent of this <see cref="AttachableBehaviour"/> instance.
        /// </summary>
        protected GameObject m_DefaultParent { get; private set; }

        /// <summary>
        /// If attached, attaching, or detaching this will be the <see cref="AttachableNode"/> this <see cref="AttachableBehaviour"/> instance is attached to.
        /// </summary>
        protected AttachableNode m_AttachableNode { get; private set; }

        private NetworkVariable<NetworkBehaviourReference> m_AttachedNodeReference = new NetworkVariable<NetworkBehaviourReference>(new NetworkBehaviourReference(null));
        private Vector3 m_OriginalLocalPosition;
        private Quaternion m_OriginalLocalRotation;

        /// <inheritdoc/>
        /// <remarks>
        /// If you create a custom <see cref="AttachableBehaviour"/> and override this method, you must invoke
        /// this base instance of <see cref="Awake"/>.
        /// </remarks>
        protected virtual void Awake()
        {
            m_DefaultParent = transform.parent == null ? gameObject : transform.parent.gameObject;
            m_OriginalLocalPosition = transform.localPosition;
            m_OriginalLocalRotation = transform.localRotation;
            m_AttachState = AttachState.Detached;
            m_AttachableNode = null;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// If you create a custom <see cref="AttachableBehaviour"/> and override this method, you must invoke
        /// this base instance of <see cref="OnNetworkPostSpawn"/>.
        /// </remarks>
        protected override void OnNetworkPostSpawn()
        {
            if (HasAuthority)
            {
                m_AttachedNodeReference.Value = new NetworkBehaviourReference(null);
            }
            m_AttachedNodeReference.OnValueChanged += OnAttachedNodeReferenceChanged;
            base.OnNetworkPostSpawn();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// If you create a custom <see cref="AttachableBehaviour"/> and override this method, you will want to
        /// invoke this base instance of <see cref="OnNetworkSessionSynchronized"/> if you want the current
        /// state to have been applied before executing the derived class's <see cref="OnNetworkSessionSynchronized"/>
        /// script.
        /// </remarks>
        protected override void OnNetworkSessionSynchronized()
        {
            UpdateAttachedState();
            base.OnNetworkSessionSynchronized();
        }

        /// <inheritdoc/>
        public override void OnNetworkDespawn()
        {
            m_AttachedNodeReference.OnValueChanged -= OnAttachedNodeReferenceChanged;
            InternalDetach();
            if (NetworkManager && !NetworkManager.ShutdownInProgress)
            {
                // Notify of the changed attached state
                UpdateAttachState(m_AttachState, m_AttachableNode);
            }
            base.OnNetworkDespawn();
        }

        private void OnAttachedNodeReferenceChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
        {
            UpdateAttachedState();
        }

        private void UpdateAttachedState()
        {
            var attachableNode = (AttachableNode)null;
            var shouldParent = m_AttachedNodeReference.Value.TryGet(out attachableNode, NetworkManager);
            var preState = shouldParent ? AttachState.Attaching : AttachState.Detaching;
            var preNode = shouldParent ? attachableNode : m_AttachableNode;
            shouldParent = shouldParent && attachableNode != null;

            if (shouldParent && m_AttachableNode != null && m_AttachState == AttachState.Attached)
            {
                // If we are attached to some other AttachableNode, then detach from that before attaching to a new one.
                if (m_AttachableNode != attachableNode)
                {
                    // Run through the same process without being triggerd by a NetVar update.
                    UpdateAttachState(AttachState.Detaching, m_AttachableNode);
                    m_AttachableNode.Detach(this);
                    transform.parent = null;
                    UpdateAttachState(AttachState.Detached, null);
                }
            }

            // Change the state to attaching or detaching
            UpdateAttachState(preState, preNode);

            if (shouldParent)
            {
                InternalAttach(attachableNode);
            }
            else
            {
                InternalDetach();
            }

            // Notify of the changed attached state
            UpdateAttachState(m_AttachState, m_AttachableNode);
        }

        /// <summary>
        /// For customized/derived <see cref="AttachableBehaviour"/>s, override this method to receive notifications
        /// when the <see cref="AttachState"/> has changed.
        /// </summary>
        /// <param name="attachState">the new <see cref="AttachState"/>.</param>
        /// <param name="attachableNode"></param>
        protected virtual void OnAttachStateChanged(AttachState attachState, AttachableNode attachableNode)
        {

        }

        /// <summary>
        /// Update the attached state.
        /// </summary>
        private void UpdateAttachState(AttachState attachState, AttachableNode attachableNode)
        {
            try
            {
                AttachStateChange?.Invoke(attachState, attachableNode);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            try
            {
                OnAttachStateChanged(attachState, attachableNode);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Internal attach method that just handles changing state, parenting, and sending the <see cref="AttachableNode"/> a
        /// notification that an <see cref="AttachableBehaviour"/> has attached.
        /// </summary>
        internal void InternalAttach(AttachableNode attachableNode)
        {
            if (attachableNode.NetworkManager != NetworkManager)
            {
                Debug.Log("Blam!");
            }
            m_AttachState = AttachState.Attached;
            m_AttachableNode = attachableNode;
            // Attachables are always local space relative
            transform.SetParent(m_AttachableNode.transform, false);
            m_AttachableNode.Attach(this);
        }

        /// <summary>
        /// Attaches the <see cref="GameObject"/> of this <see cref="AttachableBehaviour"/> instance to the <see cref="GameObject"/> of the <see cref="AttachableNode"/>.
        /// </summary>
        /// <remarks>
        /// This effectively applies a new parent to a nested <see cref="NetworkBehaviour"/> and all <see cref="GameObject"/> children
        /// of the nested <see cref="NetworkBehaviour"/>.<br />
        /// Both the <see cref="AttachableNode"/> and this <see cref="AttachableBehaviour"/> instances should be in the spawned state before this
        /// is invoked.
        /// </remarks>
        /// <param name="parent">The <see cref="NetworkBehaviour"/> to be applied or null to reparent under its original <see cref="GameObject"/> when spawned.</param>
        public void Attach(AttachableNode attachableNode)
        {
            if (!IsSpawned)
            {
                NetworkLog.LogError($"[{name}][Attach][Not Spawned] Cannot attach before being spawned!");
                return;
            }

            if (!HasAuthority)
            {
                NetworkLog.LogError($"[{name}][Attach][Not Authority] Client-{NetworkManager.LocalClientId} is not the authority!");
                return;
            }

            if (attachableNode.NetworkObject == NetworkObject)
            {
                NetworkLog.LogError($"[{name}][Attach] Cannot attach to the original {NetworkObject} instance!");
                return;
            }

            if (m_AttachableNode != null && m_AttachState == AttachState.Attached && m_AttachableNode == attachableNode)
            {
                NetworkLog.LogError($"[{name}][Attach] Cannot attach! {name} is already attached to {attachableNode.name}!");
                return;
            }

            // Update the attached node reference to the new attachable node.
            m_AttachedNodeReference.Value = new NetworkBehaviourReference(attachableNode);
        }

        /// <summary>
        /// Internal detach method that just handles changing state, parenting, and sending the <see cref="AttachableNode"/> a
        /// notification that an <see cref="AttachableBehaviour"/> has detached.
        /// </summary>
        internal void InternalDetach()
        {
            if (m_AttachableNode)
            {
                m_AttachableNode.Detach(this);
                m_AttachableNode = null;
                if (m_DefaultParent)
                {
                    // Set the original parent and origianl local position and rotation
                    transform.SetParent(m_DefaultParent.transform, false);
                    transform.localPosition = m_OriginalLocalPosition;
                    transform.localRotation = m_OriginalLocalRotation;
                }
                m_AttachState = AttachState.Detached;
            }
        }

        /// <summary>
        /// Invoke to detach from a <see cref="AttachableNode"/>.
        /// </summary>
        public void Detach()
        {
            if (!IsSpawned)
            {
                NetworkLog.LogError($"[{name}][Detach][Not Spawned] Cannot detach if not spawned!");
                return;
            }

            if (!HasAuthority)
            {
                NetworkLog.LogError($"[{name}][Detach][Not Authority] Client-{NetworkManager.LocalClientId} is not the authority!");
                return;
            }

            if (m_AttachState != AttachState.Attached || m_AttachableNode == null)
            {
                // Check for the unlikely scenario that an instance has mismatch between the state and assigned attachable node.
                if (!m_AttachableNode && m_AttachState == AttachState.Attached)
                {
                    NetworkLog.LogError($"[{name}][Detach] Invalid state detected! {name}'s state is still {m_AttachState} but has no {nameof(AttachableNode)} assigned!");
                }

                // Developer only notification for the most likely scenario where this method is invoked but the instance is not attached to anything.
                if (NetworkManager && NetworkManager.LogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogWarning($"[{name}][Detach] Cannot detach! {name} is not attached to anything!");
                }

                // If we have the attachable node set and we are not in the middle of detaching, then log an error and note
                // this could potentially occur if inoked more than once for the same instance in the same frame.
                if (m_AttachableNode && m_AttachState != AttachState.Detaching)
                {
                    NetworkLog.LogError($"[{name}][Detach] Invalid state detected! {name} is still referencing {nameof(AttachableNode)} {m_AttachableNode.name}! Could {nameof(AttachableBehaviour.Detach)} be getting invoked more than once for the same instance?");
                }
                return;
            }

            // Update the attached node reference to nothing-null.
            m_AttachedNodeReference.Value = new NetworkBehaviourReference(null);
        }
    }
}
