using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;


/// <summary>
/// This component is used in conjunction with <see cref="AttachableBehaviour"/> and is used to
/// denote a specific child <see cref="UnityEngine.GameObject"/> that an <see cref="AttachableBehaviour"/>
/// can attach itself to.
/// </summary>
/// <remarks>
/// Primarily, the <see cref="AttachableNode"/> can be used as it is or can be extended to perform additional
/// logical operations when something attaches to or detaches from the <see cref="AttachableNode"/> instance.
/// </remarks>
public class AttachableNode : NetworkBehaviour
{
    /// <summary>
    /// Returns true if the <see cref="AttachableNode"/> instance has one or more attached <see cref="AttachableBehaviour"/> components.
    /// </summary>
    public bool HasAttachments => m_AttachedBehaviours.Count > 0;

    /// <summary>
    /// When enabled, any attached <see cref="AttachableBehaviour"/>s will be automatically detached and re-parented under its original parent.
    /// </summary>
    public bool DetachOnDespawn = true;

    /// <summary>
    /// A <see cref="List{T}"/> of the currently attached <see cref="AttachableBehaviour"/>s.
    /// </summary>
    protected readonly List<AttachableBehaviour> m_AttachedBehaviours = new List<AttachableBehaviour>();

    /// <inheritdoc/>
    protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
    {
        m_AttachedBehaviours.Clear();
        base.OnNetworkPreSpawn(ref networkManager);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// When the ownership of an <see cref="AttachableNode"/> changes, it will find all currently attached <see cref="AttachableBehaviour"/> components
    /// that are registered as being attached to this instance.
    /// </remarks>
    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        // Clear any known behaviours on all instances (really only the previous owner should know about AttachedBehaviours
        m_AttachedBehaviours.Clear();
        if (current == NetworkManager.LocalClientId)
        {
            // Rebuild the list of AttachableBehaviours for the new owner
            var attachables = NetworkObject.transform.GetComponentsInChildren<AttachableBehaviour>();
            foreach (var attachable in attachables)
            {
                if (attachable.InternalAttachableNode == this)
                {
                    m_AttachedBehaviours.Add(attachable);
                }
            }
        }
        base.OnOwnershipChanged(previous, current);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// If the <see cref="NetworkObject"/> this <see cref="AttachableNode"/> belongs to is despawned,
    /// then any attached <see cref="AttachableBehaviour"/> will be detached during <see cref="OnNetworkDespawn"/>.
    /// </remarks>
    public override void OnNetworkPreDespawn()
    {
        if (IsSpawned && DetachOnDespawn)
        {
            for (int i = m_AttachedBehaviours.Count - 1; i >= 0; i--)
            {
                if (!m_AttachedBehaviours[i])
                {
                    continue;
                }
                // If we don't have authority but should detach on despawn,
                // then proceed to detach.
                if (!m_AttachedBehaviours[i].HasAuthority)
                {
                    m_AttachedBehaviours[i].ForceDetach();
                }
                else
                {
                    // Detach the normal way with authority
                    m_AttachedBehaviours[i].Detach();
                }
            }
        }
        base.OnNetworkPreDespawn();
    }

    internal override void InternalOnDestroy()
    {
        // Notify any attached behaviours that this node is being destroyed.
        for (int i = m_AttachedBehaviours.Count - 1; i >= 0; i--)
        {
            m_AttachedBehaviours[i]?.OnAttachNodeDestroy();
        }
        m_AttachedBehaviours.Clear();
        base.InternalOnDestroy();
    }

    /// <summary>
    /// Override this method to be notified when an <see cref="AttachableBehaviour"/> has attached to this node.
    /// </summary>
    /// <param name="attachableBehaviour">The <see cref="AttachableBehaviour"/> that has been attached.</param>
    protected virtual void OnAttached(AttachableBehaviour attachableBehaviour)
    {

    }

    internal void Attach(AttachableBehaviour attachableBehaviour)
    {
        if (m_AttachedBehaviours.Contains(attachableBehaviour))
        {
            NetworkLog.LogError($"[{nameof(AttachableNode)}][{name}][Attach] {nameof(AttachableBehaviour)} {attachableBehaviour.name} is already attached!");
            return;
        }
        m_AttachedBehaviours.Add(attachableBehaviour);
        OnAttached(attachableBehaviour);
    }

    /// <summary>
    /// Override this method to be notified when an <see cref="AttachableBehaviour"/> has detached from this node.
    /// </summary>
    /// <param name="attachableBehaviour">The <see cref="AttachableBehaviour"/> that has been detached.</param>
    protected virtual void OnDetached(AttachableBehaviour attachableBehaviour)
    {

    }

    internal void Detach(AttachableBehaviour attachableBehaviour)
    {
        if (!m_AttachedBehaviours.Contains(attachableBehaviour))
        {
            NetworkLog.LogError($"[{nameof(AttachableNode)}][{name}][Detach] {nameof(AttachableBehaviour)} {attachableBehaviour.name} is not attached!");
            return;
        }
        m_AttachedBehaviours.Remove(attachableBehaviour);
        OnDetached(attachableBehaviour);
    }
}
