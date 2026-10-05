#if UNIFIED_NETCODE
using Unity.Mathematics;
#if !UNIFIED_NETCODE_7_0_0
using Unity.NetCode;
#endif
using Unity.Transforms;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// TODO-UNIFIED: Needs further peer review and exploring alternate ways of handling this.
    /// This is a component that is added to the root of all N4E-spawned hybrid prefab instances. It is used to link
    /// <see cref="NetworkObject.SerializedObject"/> the N4E-spawned hybrid prefab instances to the incoming <see cref="CreateObjectMessage"/>
    /// specific to the N4E-spawned hybrid prefab instance that has the matching <see cref="NetworkObjectId"/>.
    /// </summary>

    [DefaultExecutionOrder(GhostObject.ExecutionOrder + 1)]
    // Internal: GhostBehaviour is only public when NETCODE_GAMEOBJECT_BRIDGE_EXPERIMENTAL is defined, and a public
    // type cannot derive from an internal one.
    internal partial class NetworkObjectBridge : GhostBehaviour
    {
#if UNITY_EDITOR
        private void Reset()
        {
            // Start users with just interpolation (they can adjust this if they want prediction)
            // to make the initial transition less problematic for users.
            // Only set when the bridge is first added, so a user's prediction setting is kept.
            var ghostAdapter = GetComponent<GhostObject>();
            if (ghostAdapter != null)
            {
                ghostAdapter.SupportedGhostModes = GhostModeMask.Interpolated;
            }
        }

        private void OnValidate()
        {
            hideFlags = HideFlags.HideInInspector;

            var ghostAdapter = GetComponent<GhostObject>();
            if (ghostAdapter == null)
            {
                return;
            }

#if COM_UNITY_MODULES_PHYSICS
            var rigidBody = GetComponent<Rigidbody>();
            var ghostRigidBody = GetComponent<GhostRigidbody>();
            if (rigidBody != null)
            {
                // This must be enabled when replicating the rigid body.

                ghostAdapter.SingleWorldHostInterpolationSmoothing = SingleWorldHostInterpolationMode.Interpolate;
                // TODO: Currently, this is added only if you enable replication of the rigid body.
                // There is a bug where if you don't add this component it doesn't synchronize the transform.
                // Remove this once the issue is resolved.
                if (ghostRigidBody == null)
                {
                    gameObject.AddComponent<GhostRigidbody>();
                }
            }
#endif
#if COM_UNITY_MODULES_PHYSICS2D
            // TODO: Fill out a similar script as above but for the 2D version
#endif
        }
#endif

        /// <summary>
        /// This is used to link <see cref="NetworkObject.SerializedObject"/> data to
        /// N4E-spawned hybrid prefab instances.
        /// </summary>
        internal GhostField<ulong> NetworkObjectId = new GhostField<ulong>();

        /// <summary>
        /// Currently, NGO provides the parenting event handling via <see cref="ParentSyncMessage"/>.
        /// Once <see cref="GhostField{InternalTypeT}"/> can provide a form of event notification that
        /// the value has changed, we can then invert this flow such that the change in the parent value
        /// drives the event.
        /// </summary>
        /// <param name="scale">We use NGO scale, delivered via ParentSyncMessage, that is applied to this
        /// instance's entity's PostTransformMatrix.</param>
        internal void HybridParentUpdate(Vector3 scale)
        {
            var current = Ghost.GetPositionAndRotation();
            //Debug.Log($"---- Current LT: {current.Position} | {current.Rotation.eulerAngles}");
            //Debug.Log($"---- New LT: {transform.localPosition} | {transform.localRotation}");
            Ghost.ApplyPostTransformMatrixScale(scale);
        }

        internal void ApplyScale(Vector3 scale)
        {
            Ghost.ApplyPostTransformMatrixScale(scale);
        }

        /// <summary>
        /// Keeps the ghost's owner in step with the <see cref="NetworkObject"/> owner, so an owner-predicted
        /// ghost is predicted by the NGO owner.
        /// </summary>
        /// <remarks>Only the server can assign a ghost owner; N4E replicates it to clients.</remarks>
        /// <param name="networkManager">The server's <see cref="NetworkManager"/>.</param>
        /// <param name="ownerClientId">The new NGO owner.</param>
        internal void UpdateGhostOwner(NetworkManager networkManager, ulong ownerClientId)
        {
            if (!networkManager.IsServer || !Ghost.HasOwner)
            {
                return;
            }

            // Owned by the server with no local client means no client owns the ghost.
            var ownerNetworkId = default(NetworkId);
            if (ownerClientId == networkManager.LocalClientId)
            {
                if (networkManager.IsHost && networkManager.NetcodeWorld != null)
                {
                    ownerNetworkId = networkManager.NetcodeWorld.LocalConnection.NetworkId;
                }
            }
            else
            {
                // The unified transport id is the client's N4E network id.
                var (transportId, found) = networkManager.ConnectionManager.ClientIdToTransportId(ownerClientId);
                if (found)
                {
                    ownerNetworkId = new NetworkId { Value = (int)transportId };
                }
            }
            Ghost.OwnerNetworkId = ownerNetworkId;
        }
    }

#if UNIFIED_NETCODE_7_0_0
    /// <summary>
    /// Stands in for N4E's <c>GhostObject.ApplyPostTransformMatrixScale</c>, which 7.0.0 removed along with the
    /// non-uniform scale rework that gave the GameObject-to-entity transform sync ownership of the
    /// <see cref="PostTransformMatrix"/>. 6.7.0 still has the method, so this is only compiled against 7.0.0.
    /// Remove it once N4E exposes a supported way to push scale to a ghost.
    /// </summary>
    internal static class GhostObjectScaleExtensions
    {
        /// <summary>
        /// A ghost that replicates 3D scale stores it in its <see cref="PostTransformMatrix"/> and holds
        /// <see cref="LocalTransform.Scale"/> at 1, because consumers multiply the two. A ghost authored with
        /// <c>UseUniformScale</c> has no matrix - and cannot gain one at runtime, since the component is only in the
        /// replicated set when the prefab is registered - so only the uniform scale can be applied there.
        /// </summary>
        internal static void ApplyPostTransformMatrixScale(this GhostObject ghost, Vector3 scale)
        {
            var entityManager = ghost.World.EntityManager;
            var entity = ghost.Entity;
            var localTransform = entityManager.GetComponentData<LocalTransform>(entity);

            if (entityManager.HasComponent<PostTransformMatrix>(entity))
            {
                entityManager.SetComponentData(entity, new PostTransformMatrix { Value = float4x4.Scale(scale) });
                localTransform.Scale = 1f;
            }
            else
            {
                if (!Mathf.Approximately(scale.x, scale.y) || !Mathf.Approximately(scale.y, scale.z))
                {
                    Debug.LogWarning($"[{nameof(NetworkObjectBridge)}] Non-uniform scale {scale} cannot be replicated by a ghost authored for uniform scale; applying {scale.x} to all axes.", ghost);
                }
                localTransform.Scale = scale.x;
            }

            entityManager.SetComponentData(entity, localTransform);
        }
    }
#endif
}
#endif
