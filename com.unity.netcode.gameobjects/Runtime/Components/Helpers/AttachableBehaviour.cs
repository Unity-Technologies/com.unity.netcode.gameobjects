using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
    /// upon the <see cref="AttachableBehaviour"/> instance's current state.<br />
    /// <see cref="AttachableNode"/> invocation order:
    /// - When attaching, the <see cref="AttachableNode"/>'s <see cref="AttachableNode.OnAttached(AttachableBehaviour)"/> is invoked just before the <see cref="OnAttachStateChanged"/> is invoked with the <see cref="AttachState.Attached"/> state.<br />
    /// - When detaching, the <see cref="AttachableNode"/>'s <see cref="AttachableNode.OnDetached(AttachableBehaviour)"/> is invoked right after the <see cref="OnAttachStateChanged"/> is invoked with the <see cref="AttachState.Detached"/> notification.<br />
    /// </remarks>
    public class AttachableBehaviour : NetworkBehaviour
    {
        [Serializable]
        internal class ComponentControllerEntry
        {
            // Ignoring the naming convention in order to auto-assign element names
#pragma warning disable IDE1006
            /// <summary>
            /// Used for naming each element entry.
            /// </summary>
            [HideInInspector]
            public string name;
#pragma warning restore IDE1006


#if UNITY_EDITOR
            internal void OnValidate()
            {
                if (!HasInitialized)
                {
                    AutoTrigger = TriggerTypes.OnAttach | TriggerTypes.OnDetach;
                    HasInitialized = true;
                }
                name = ComponentController != null ? ComponentController.GetComponentNameFormatted(ComponentController) : "Component Controller";
            }
#endif

            [Flags]
            public enum TriggerTypes : byte
            {
                Nothing,
                OnAttach,
                OnDetach,
            }

            public TriggerTypes AutoTrigger;
            public bool EnableOnAttach = true;
            public ComponentController ComponentController;

            [HideInInspector]
            [SerializeField]
            internal bool HasInitialized;
        }

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
            if (ComponentControllers == null)
            {
                return;
            }
            foreach (var componentController in ComponentControllers)
            {
                componentController?.OnValidate();
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
        /// Flags to determine if the <see cref="AttachableBehaviour"/> will automatically detach.
        /// </summary>
        [Flags]
        public enum AutoDetachTypes
        {
            /// <summary>
            /// Disables auto detach.
            /// </summary>
            None,
            /// <summary>
            /// Detach on ownership change.
            /// </summary>
            OnOwnershipChange,
            /// <summary>
            /// Detach on despawn.
            /// </summary>
            OnDespawn,
            /// <summary>
            /// Detach on destroy.
            /// </summary>
            OnAttachNodeDestroy,
        }

        /// <summary>
        /// Determines if this <see cref="AttachableBehaviour"/> will automatically detach on all instances if it has one of the <see cref="AutoDetachTypes"/> flags.
        /// </summary>
        public AutoDetachTypes AutoDetach = AutoDetachTypes.OnDespawn | AutoDetachTypes.OnOwnershipChange | AutoDetachTypes.OnAttachNodeDestroy;

        [SerializeField]
        internal List<ComponentControllerEntry> ComponentControllers;

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
        internal AttachableNode InternalAttachableNode => m_AttachableNode;

        private NetworkBehaviourReference m_AttachedNodeReference = new NetworkBehaviourReference(null);
        private Vector3 m_OriginalLocalPosition;
        private Quaternion m_OriginalLocalRotation;

        /// <inheritdoc/>
        protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer)
        {
            // Example of how to synchronize late joining clients when using an RPC to update
            // a local property's state.
            serializer.SerializeValue(ref m_AttachedNodeReference);
            base.OnSynchronize(ref serializer);
        }

        /// <summary>
        /// Override this method in place of Awake. This method is invoked during Awake.
        /// </summary>
        /// <remarks>
        /// The <see cref="AttachableBehaviour"/>'s Awake method is protected to assure it initializes itself at this point in time.
        /// </remarks>
        protected virtual void OnAwake()
        {
        }

        /// <summary>
        /// If you create a custom <see cref="AttachableBehaviour"/> and override this method, you must invoke
        /// this base instance of <see cref="Awake"/>.
        /// </summary>
        protected virtual void Awake()
        {
            m_DefaultParent = transform.parent == null ? gameObject : transform.parent.gameObject;
            m_OriginalLocalPosition = transform.localPosition;
            m_OriginalLocalRotation = transform.localRotation;
            m_AttachState = AttachState.Detached;
            m_AttachableNode = null;
            OnAwake();
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

        internal void ForceDetach()
        {
            if (m_AttachState == AttachState.Detached || m_AttachState == AttachState.Detaching)
            {
                return;
            }

            ForceComponentChange(false, true);

            InternalDetach();
            // Notify of the changed attached state
            NotifyAttachedStateChanged(m_AttachState, m_AttachableNode);

            m_AttachedNodeReference = new NetworkBehaviourReference(null);

            // When detaching, we want to make our final action
            // the invocation of the AttachableNode's Detach method.
            if (m_AttachableNode)
            {
                m_AttachableNode.Detach(this);
                m_AttachableNode = null;
            }
        }

        /// <inheritdoc/>
        public override void OnNetworkPreDespawn()
        {
            if (AutoDetach.HasFlag(AutoDetachTypes.OnDespawn))
            {
                ForceDetach();
            }
            base.OnNetworkDespawn();
        }

        /// <summary>
        /// This will apply the final attach or detatch state based on the current value of <see cref="m_AttachedNodeReference"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAttachedState()
        {
            // Process the NetworkBehaviourReference to get the new AttachableNode or null.
            // If null, then isAttaching will always be false.
            var isAttaching = m_AttachedNodeReference.TryGet(out AttachableNode attachableNode, NetworkManager);

            // Exit early if we are already in the correct attached state and the incoming
            // AttachableNode reference is the same as the local AttachableNode property.
            if (attachableNode == m_AttachableNode &&
                ((isAttaching && m_AttachState == AttachState.Attached) ||
                (!isAttaching && m_AttachState == AttachState.Detached)))
            {
                return;
            }

            // If we are attached to some other AttachableNode, then detach from that before attaching to a new one.
            if (isAttaching && m_AttachableNode != null && m_AttachState == AttachState.Attached)
            {
                // Run through the same process without being triggerd by a NetVar update.
                NotifyAttachedStateChanged(AttachState.Detaching, m_AttachableNode);
                InternalDetach();
                NotifyAttachedStateChanged(AttachState.Detached, m_AttachableNode);

                m_AttachableNode.Detach(this);
                m_AttachableNode = null;
            }

            // Change the state to attaching or detaching
            NotifyAttachedStateChanged(isAttaching ? AttachState.Attaching : AttachState.Detaching, isAttaching ? attachableNode : m_AttachableNode);

            ForceComponentChange(isAttaching, false);
            if (isAttaching)
            {
                InternalAttach(attachableNode);
            }
            else
            {
                InternalDetach();
            }

            // Notify of the changed attached state
            NotifyAttachedStateChanged(m_AttachState, m_AttachableNode);

            // When detaching, we want to make our final action
            // the invocation of the AttachableNode's Detach method.
            if (!isAttaching && m_AttachableNode)
            {
                m_AttachableNode.Detach(this);
                m_AttachableNode = null;
            }
        }

        /// <summary>
        /// For customized/derived <see cref="AttachableBehaviour"/>s, override this method to receive notifications
        /// when the <see cref="AttachState"/> has changed.
        /// </summary>
        /// <param name="attachState">The new <see cref="AttachState"/>.</param>
        /// <param name="attachableNode">The <see cref="AttachableNode"/> being attached to or from. Will be null when completely detached.</param>
        protected virtual void OnAttachStateChanged(AttachState attachState, AttachableNode attachableNode)
        {

        }

        /// <summary>
        /// Update the attached state.
        /// </summary>
        private void NotifyAttachedStateChanged(AttachState attachState, AttachableNode attachableNode)
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

        /// <inheritdoc/>
        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            if (AutoDetach.HasFlag(AutoDetachTypes.OnOwnershipChange))
            {
                ForceDetach();
            }
            base.OnOwnershipChanged(previous, current);
        }

        internal void ForceComponentChange(bool isAttaching, bool forcedChange)
        {
            var triggerType = isAttaching ? ComponentControllerEntry.TriggerTypes.OnAttach : ComponentControllerEntry.TriggerTypes.OnDetach;

            foreach (var componentControllerEntry in ComponentControllers)
            {
                if (componentControllerEntry.AutoTrigger.HasFlag(triggerType))
                {
                    componentControllerEntry.ComponentController.ForceChangeEnabled(componentControllerEntry.EnableOnAttach ? isAttaching : !isAttaching, forcedChange);
                }
            }
        }

        /// <summary>
        /// Internal attach method that just handles changing state, parenting, and sending the <see cref="AttachableNode"/> a
        /// notification that an <see cref="AttachableBehaviour"/> has attached.
        /// </summary>
        internal void InternalAttach(AttachableNode attachableNode)
        {
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
        /// <param name="attachableNode">The <see cref="AttachableNode"/> to attach this instance to.</param>
        public void Attach(AttachableNode attachableNode)
        {
            if (!IsSpawned)
            {
                NetworkLog.LogError($"[{name}][Attach][Not Spawned] Cannot attach before being spawned!");
                return;
            }

            if (!OnHasAuthority())
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

            ChangeReference(new NetworkBehaviourReference(attachableNode));
        }

        /// <summary>
        /// Internal detach method that just handles changing state, parenting, and sending the <see cref="AttachableNode"/> a
        /// notification that an <see cref="AttachableBehaviour"/> has detached.
        /// </summary>
        internal void InternalDetach()
        {
            if (m_AttachableNode)
            {
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
            if (!gameObject)
            {
                return;
            }
            if (!IsSpawned)
            {
                NetworkLog.LogError($"[{name}][Detach][Not Spawned] Cannot detach if not spawned!");
                return;
            }

            if (!OnHasAuthority())
            {
                NetworkLog.LogError($"[{name}][Detach][Not Authority] Client-{NetworkManager.LocalClientId} is not the authority!");
                return;
            }

            if (m_AttachState == AttachState.Detached || m_AttachState == AttachState.Detaching || m_AttachableNode == null)
            {
                // Check for the unlikely scenario that an instance has mismatch between the state and assigned attachable node.
                if (!m_AttachableNode)
                {
                    NetworkLog.LogError($"[{name}][Detach] Invalid state detected! {name}'s state is still {m_AttachState} but has no {nameof(AttachableNode)} assigned!");
                    // Developer only notification for the most likely scenario where this method is invoked but the instance is not attached to anything.
                    if (NetworkManager && NetworkManager.LogLevel <= LogLevel.Developer)
                    {
                        NetworkLog.LogWarning($"[{name}][Detach] Cannot detach! {name} is not attached to anything!");
                    }
                }
                else
                {
                    // If we have the attachable node set and we are not in the middle of detaching, then log an error and note
                    // this could potentially occur if inoked more than once for the same instance in the same frame.
                    NetworkLog.LogError($"[{name}][Detach] Invalid state detected! {name} is still referencing {nameof(AttachableNode)} {m_AttachableNode.name}! Could {nameof(Detach)} be getting invoked more than once for the same instance?");
                }
                return;
            }

            ChangeReference(new NetworkBehaviourReference(null));
        }

        /// <summary>
        /// Override this method to change how the instance determines the authority.<br />
        /// The default is to use the <see cref="NetworkObject.HasAuthority"/> method.
        /// </summary>
        /// <remarks>
        /// Useful when using a <see cref="NetworkTopologyTypes.ClientServer"/> network topology and you would like
        /// to have the owner be the authority of this <see cref="ComponentController"/> instance.
        /// </remarks>
        /// <returns>true = has authoriy | false = does not have authority</returns>
        protected virtual bool OnHasAuthority()
        {
            return HasAuthority;
        }

        private void ChangeReference(NetworkBehaviourReference networkBehaviourReference)
        {
            // Update the attached node reference to the new attachable node.
            m_AttachedNodeReference = networkBehaviourReference;
            UpdateAttachedState();

            if (OnHasAuthority())
            {
                // Send notification of the change in this property's state.
                UpdateAttachStateRpc(m_AttachedNodeReference);
            }
        }

        [Rpc(SendTo.NotMe)]
        private void UpdateAttachStateRpc(NetworkBehaviourReference attachedNodeReference, RpcParams rpcParams = default)
        {
            ChangeReference(attachedNodeReference);
        }

        /// <summary>
        /// Notification that the <see cref="AttachableNode"/> is being destroyed
        /// </summary>
        internal void OnAttachNodeDestroy()
        {
            // If this instance should force a detach on destroy
            if (AutoDetach.HasFlag(AutoDetachTypes.OnAttachNodeDestroy))
            {
                // Force a detach
                ForceDetach();
            }
        }
    }
}
