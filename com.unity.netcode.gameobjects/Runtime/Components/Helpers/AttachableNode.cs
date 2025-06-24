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
    /// A <see cref="List{T}"/> of the currently attached <see cref="AttachableBehaviour"/>s.
    /// </summary>
    protected readonly List<AttachableBehaviour> m_AttachedBehaviours = new List<AttachableBehaviour>();

    /// <inheritdoc/>
    /// <remarks>
    /// If the <see cref="NetworkObject"/> this <see cref="AttachableNode"/> belongs to is despawned,
    /// then any attached <see cref="AttachableBehaviour"/> will be detached during <see cref="OnNetworkDespawn"/>.
    /// </remarks>
    public override void OnNetworkDespawn()
    {
        for (int i = m_AttachedBehaviours.Count - 1; i > 0; i--)
        {
            m_AttachedBehaviours[i].InternalDetach();
        }
        base.OnNetworkDespawn();
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
