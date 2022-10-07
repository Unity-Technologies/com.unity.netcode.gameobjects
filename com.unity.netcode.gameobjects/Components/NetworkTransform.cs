using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// A component for syncing transforms.
    /// NetworkTransform will read the underlying transform and replicate it to clients.
    /// The replicated value will be automatically be interpolated (if active) and applied to the underlying GameObject's transform.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Netcode/Network Transform")]
    [DefaultExecutionOrder(100000)] // this is needed to catch the update time after the transform was updated by user scripts
    public class NetworkTransform : NetworkBehaviour
    {
        /// <summary>
        /// The default position change threshold value.
        /// Any changes above this threshold will be replicated.
        /// </summary>
        public const float PositionThresholdDefault = 0.001f;

        /// <summary>
        /// The default rotation angle change threshold value.
        /// Any changes above this threshold will be replicated.
        /// </summary>
        public const float RotAngleThresholdDefault = 0.01f;

        /// <summary>
        /// The default scale change threshold value.
        /// Any changes above this threshold will be replicated.
        /// </summary>
        public const float ScaleThresholdDefault = 0.01f;

        /// <summary>
        /// The handler delegate type that takes client requested changes and returns resulting changes handled by the server.
        /// </summary>
        /// <param name="pos">The position requested by the client.</param>
        /// <param name="rot">The rotation requested by the client.</param>
        /// <param name="scale">The scale requested by the client.</param>
        /// <returns>The resulting position, rotation and scale changes after handling.</returns>
        public delegate (Vector3 pos, Quaternion rotOut, Vector3 scale) OnClientRequestChangeDelegate(Vector3 pos, Quaternion rot, Vector3 scale);

        /// <summary>
        /// The handler that gets invoked when server receives a change from a client.
        /// This handler would be useful for server to modify pos/rot/scale before applying client's request.
        /// </summary>
        public OnClientRequestChangeDelegate OnClientRequestChange;

        internal struct NetworkTransformState : INetworkSerializable
        {
            private const int k_InLocalSpaceBit = 0;
            private const int k_PositionXBit = 1;
            private const int k_PositionYBit = 2;
            private const int k_PositionZBit = 3;
            private const int k_RotAngleXBit = 4;
            private const int k_RotAngleYBit = 5;
            private const int k_RotAngleZBit = 6;
            private const int k_ScaleXBit = 7;
            private const int k_ScaleYBit = 8;
            private const int k_ScaleZBit = 9;
            private const int k_TeleportingBit = 10;
            // 11-15: <unused>

            private ushort m_Bitset;

            internal bool InLocalSpace
            {
                get => (m_Bitset & (1 << k_InLocalSpaceBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_InLocalSpaceBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_InLocalSpaceBit)); }
                }
            }

            // Position
            internal bool HasPositionX
            {
                get => (m_Bitset & (1 << k_PositionXBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_PositionXBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_PositionXBit)); }
                }
            }

            internal bool HasPositionY
            {
                get => (m_Bitset & (1 << k_PositionYBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_PositionYBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_PositionYBit)); }
                }
            }

            internal bool HasPositionZ
            {
                get => (m_Bitset & (1 << k_PositionZBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_PositionZBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_PositionZBit)); }
                }
            }

            internal bool HasPositionChange
            {
                get
                {
                    return HasPositionX | HasPositionY | HasPositionZ;
                }
            }

            // RotAngles
            internal bool HasRotAngleX
            {
                get => (m_Bitset & (1 << k_RotAngleXBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_RotAngleXBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_RotAngleXBit)); }
                }
            }

            internal bool HasRotAngleY
            {
                get => (m_Bitset & (1 << k_RotAngleYBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_RotAngleYBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_RotAngleYBit)); }
                }
            }

            internal bool HasRotAngleZ
            {
                get => (m_Bitset & (1 << k_RotAngleZBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_RotAngleZBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_RotAngleZBit)); }
                }
            }

            internal bool HasRotAngleChange
            {
                get
                {
                    return HasRotAngleX | HasRotAngleY | HasRotAngleZ;
                }
            }


            // Scale
            internal bool HasScaleX
            {
                get => (m_Bitset & (1 << k_ScaleXBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_ScaleXBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_ScaleXBit)); }
                }
            }

            internal bool HasScaleY
            {
                get => (m_Bitset & (1 << k_ScaleYBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_ScaleYBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_ScaleYBit)); }
                }
            }

            internal bool HasScaleZ
            {
                get => (m_Bitset & (1 << k_ScaleZBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_ScaleZBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_ScaleZBit)); }
                }
            }

            internal bool HasScaleChange
            {
                get
                {
                    return HasScaleX | HasScaleY | HasScaleZ;
                }
            }

            internal bool IsTeleportingNextFrame
            {
                get => (m_Bitset & (1 << k_TeleportingBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_TeleportingBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_TeleportingBit)); }
                }
            }

            internal float PositionX, PositionY, PositionZ;
            internal float RotAngleX, RotAngleY, RotAngleZ;
            internal float ScaleX, ScaleY, ScaleZ;
            internal double SentTime;

            // Authoritative and non-authoritative sides use this to determine if a NetworkTransformState is
            // dirty or not.
            internal bool IsDirty;

            // Non-Authoritative side uses this for ending extrapolation of the last applied state
            internal int EndExtrapolationTick;

            /// <summary>
            /// This will reset the NetworkTransform BitSet
            /// </summary>
            internal void ClearBitSetForNextTick()
            {
                // We need to preserve the local space settings for the current state
                m_Bitset &= (ushort)(m_Bitset & (1 << k_InLocalSpaceBit));
                IsDirty = false;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref SentTime);
                // InLocalSpace + HasXXX Bits
                serializer.SerializeValue(ref m_Bitset);
                // Position Values
                if (HasPositionX)
                {
                    serializer.SerializeValue(ref PositionX);
                }

                if (HasPositionY)
                {
                    serializer.SerializeValue(ref PositionY);
                }

                if (HasPositionZ)
                {
                    serializer.SerializeValue(ref PositionZ);
                }

                // RotAngle Values
                if (HasRotAngleX)
                {
                    serializer.SerializeValue(ref RotAngleX);
                }

                if (HasRotAngleY)
                {
                    serializer.SerializeValue(ref RotAngleY);
                }

                if (HasRotAngleZ)
                {
                    serializer.SerializeValue(ref RotAngleZ);
                }

                // Scale Values
                if (HasScaleX)
                {
                    serializer.SerializeValue(ref ScaleX);
                }

                if (HasScaleY)
                {
                    serializer.SerializeValue(ref ScaleY);
                }

                if (HasScaleZ)
                {
                    serializer.SerializeValue(ref ScaleZ);
                }

                // Only if we are receiving state
                if (serializer.IsReader)
                {
                    // Go ahead and mark the local state dirty or not dirty as well
                    /// <see cref="TryCommitTransformToServer"/>
                    IsDirty = HasPositionChange || HasRotAngleChange || HasScaleChange;
                }
            }
        }

        /// <summary>
        /// Whether or not x component of position will be replicated
        /// </summary>
        public bool SyncPositionX = true;
        /// <summary>
        /// Whether or not y component of position will be replicated
        /// </summary>
        public bool SyncPositionY = true;
        /// <summary>
        /// Whether or not z component of position will be replicated
        /// </summary>
        public bool SyncPositionZ = true;

        private bool SynchronizePosition
        {
            get
            {
                return SyncPositionX || SyncPositionY || SyncPositionZ;
            }
        }

        /// <summary>
        /// Whether or not x component of rotation will be replicated
        /// </summary>
        public bool SyncRotAngleX = true;
        /// <summary>
        /// Whether or not y component of rotation will be replicated
        /// </summary>
        public bool SyncRotAngleY = true;
        /// <summary>
        /// Whether or not z component of rotation will be replicated
        /// </summary>
        public bool SyncRotAngleZ = true;

        private bool SynchronizeRotation
        {
            get
            {
                return SyncRotAngleX || SyncRotAngleY || SyncRotAngleZ;
            }
        }

        /// <summary>
        /// Whether or not x component of scale will be replicated
        /// </summary>
        public bool SyncScaleX = true;
        /// <summary>
        /// Whether or not y component of scale will be replicated
        /// </summary>
        public bool SyncScaleY = true;
        /// <summary>
        /// Whether or not z component of scale will be replicated
        /// </summary>
        public bool SyncScaleZ = true;


        private bool SynchronizeScale
        {
            get
            {
                return SyncScaleX || SyncScaleY || SyncScaleZ;
            }
        }

        /// <summary>
        /// The current position threshold value
        /// Any changes to the position that exceeds the current threshold value will be replicated
        /// </summary>
        public float PositionThreshold = PositionThresholdDefault;

        /// <summary>
        /// The current rotation threshold value
        /// Any changes to the rotation that exceeds the current threshold value will be replicated
        /// Minimum Value: 0.001
        /// Maximum Value: 360.0
        /// </summary>
        [Range(0.001f, 360.0f)]
        public float RotAngleThreshold = RotAngleThresholdDefault;

        /// <summary>
        /// The current scale threshold value
        /// Any changes to the scale that exceeds the current threshold value will be replicated
        /// </summary>
        public float ScaleThreshold = ScaleThresholdDefault;

        /// <summary>
        /// Sets whether the transform should be treated as local (true) or world (false) space.
        /// </summary>
        /// <remarks>
        /// This should only be changed by the authoritative side during runtime. Non-authoritative
        /// changes will be overridden upon the next state update.
        /// </remarks>
        [Tooltip("Sets whether this transform should sync in local space or in world space")]
        public bool InLocalSpace = false;

        /// <summary>
        /// When enabled (default) interpolation is applied and when disabled no interpolation is applied
        /// </summary>
        public bool Interpolate = true;

        /// <summary>
        /// Used to determine who can write to this transform. Server only for this transform.
        /// Changing this value alone in a child implementation will not allow you to create a NetworkTransform which can be written to by clients. See the ClientNetworkTransform Sample
        /// in the package samples for how to implement a NetworkTransform with client write support.
        /// If using different values, please use RPCs to write to the server. Netcode doesn't support client side network variable writing
        /// </summary>
        public bool CanCommitToTransform { get; protected set; }

        /// <summary>
        /// Internally used by <see cref="NetworkTransform"/> to keep track of whether this <see cref="NetworkBehaviour"/> derived class instance
        /// was instantiated on the server side or not.
        /// </summary>
        protected bool m_CachedIsServer;

        /// <summary>
        /// Internally used by <see cref="NetworkTransform"/> to keep track of the <see cref="NetworkManager"/> instance assigned to this
        /// this <see cref="NetworkBehaviour"/> derived class instance.
        /// </summary>
        protected NetworkManager m_CachedNetworkManager;

        /// <summary>
        /// We have two internal NetworkVariables.
        /// One for server authoritative and one for "client/owner" authoritative.
        /// </summary>
        private readonly NetworkVariable<NetworkTransformState> m_ReplicatedNetworkStateServer = new NetworkVariable<NetworkTransformState>(new NetworkTransformState(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<NetworkTransformState> m_ReplicatedNetworkStateOwner = new NetworkVariable<NetworkTransformState>(new NetworkTransformState(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        internal NetworkVariable<NetworkTransformState> ReplicatedNetworkState
        {
            get
            {
                if (!IsServerAuthoritative())
                {
                    return m_ReplicatedNetworkStateOwner;
                }

                return m_ReplicatedNetworkStateServer;
            }
        }

        // Used by both authoritative and non-authoritative instances.
        // This represents the most recent local authoritative state.
        private NetworkTransformState m_LocalAuthoritativeNetworkState;

        private ClientRpcParams m_ClientRpcParams = new ClientRpcParams() { Send = new ClientRpcSendParams() };
        private List<ulong> m_ClientIds = new List<ulong>() { 0 };

        private BufferedLinearInterpolator<float> m_PositionXInterpolator;
        private BufferedLinearInterpolator<float> m_PositionYInterpolator;
        private BufferedLinearInterpolator<float> m_PositionZInterpolator;
        private BufferedLinearInterpolator<Quaternion> m_RotationInterpolator; // rotation is a single Quaternion since each Euler axis will affect the quaternion's final value
        private BufferedLinearInterpolator<float> m_ScaleXInterpolator;
        private BufferedLinearInterpolator<float> m_ScaleYInterpolator;
        private BufferedLinearInterpolator<float> m_ScaleZInterpolator;
        private readonly List<BufferedLinearInterpolator<float>> m_AllFloatInterpolators = new List<BufferedLinearInterpolator<float>>(6);

        // Used by integration test
        private NetworkTransformState m_LastSentState;

        internal NetworkTransformState GetLastSentState()
        {
            return m_LastSentState;
        }

        /// <summary>
        /// This will try to send/commit the current transform delta states (if any)
        /// </summary>
        /// <remarks>
        /// Only client owners or the server should invoke this method
        /// </remarks>
        /// <param name="transformToCommit">the transform to be committed</param>
        /// <param name="dirtyTime">time it was marked dirty</param>
        protected void TryCommitTransformToServer(Transform transformToCommit, double dirtyTime)
        {
            // Only client owners or the server should invoke this method
            if (!IsOwner && !m_CachedIsServer)
            {
                NetworkLog.LogError($"Non-owner instance, {name}, is trying to commit a transform!");
                return;
            }

            // If we are authority, update the authoritative state
            if (CanCommitToTransform)
            {
                UpdateAuthoritativeState(transform);
            }
            else // Non-Authority
            {
                var position = InLocalSpace ? transformToCommit.localPosition : transformToCommit.position;
                var rotation = InLocalSpace ? transformToCommit.localRotation : transformToCommit.rotation;
                // We are an owner requesting to update our state
                if (!m_CachedIsServer)
                {
                    SetStateServerRpc(position, rotation, transformToCommit.localScale, false);
                }
                else // Server is always authoritative (including owner authoritative)
                {
                    SetStateClientRpc(position, rotation, transformToCommit.localScale, false);
                }
            }
        }

        /// <summary>
        /// Authoritative side only
        /// If there are any transform delta states, this method will synchronize the
        /// state with all non-authority instances.
        /// </summary>
        private void TryCommitTransform(Transform transformToCommit, double dirtyTime)
        {
            if (!CanCommitToTransform && !IsOwner)
            {
                NetworkLog.LogError($"[{name}] is trying to commit the transform without authority!");
                return;
            }

            // If the transform has deltas (returns dirty) then...
            if (ApplyTransformToNetworkState(ref m_LocalAuthoritativeNetworkState, dirtyTime, transformToCommit))
            {
                // ...commit the state
                ReplicatedNetworkState.Value = m_LocalAuthoritativeNetworkState;
            }
        }

        /// <summary>
        /// Initializes the interpolators with the current transform values
        /// </summary>
        private void ResetInterpolatedStateToCurrentAuthoritativeState()
        {
            var serverTime = NetworkManager.ServerTime.Time;
            var position = InLocalSpace ? transform.localPosition : transform.position;
            m_PositionXInterpolator.ResetTo(position.x, serverTime);
            m_PositionYInterpolator.ResetTo(position.y, serverTime);
            m_PositionZInterpolator.ResetTo(position.z, serverTime);

            var rotation = InLocalSpace ? transform.localRotation : transform.rotation;
            m_RotationInterpolator.ResetTo(rotation, serverTime);

            var scale = transform.localScale;
            m_ScaleXInterpolator.ResetTo(scale.x, serverTime);
            m_ScaleYInterpolator.ResetTo(scale.y, serverTime);
            m_ScaleZInterpolator.ResetTo(scale.z, serverTime);
        }

        /// <summary>
        /// Used for integration testing:
        /// Will apply the transform to the LocalAuthoritativeNetworkState and get detailed dirty information returned
        /// in the <see cref="NetworkTransformState"/> returned.
        /// </summary>
        /// <param name="transform">transform to apply</param>
        /// <returns>NetworkTransformState</returns>
        internal NetworkTransformState ApplyLocalNetworkState(Transform transform)
        {
            // Since we never commit these changes, we need to simulate that any changes were committed previously and the bitset
            // value would already be reset prior to having the state applied
            m_LocalAuthoritativeNetworkState.ClearBitSetForNextTick();

            // Now check the transform for any threshold value changes
            ApplyTransformToNetworkStateWithInfo(ref m_LocalAuthoritativeNetworkState, m_CachedNetworkManager.LocalTime.Time, transform);

            // Return the entire state to be used by the integration test
            return m_LocalAuthoritativeNetworkState;
        }

        /// <summary>
        /// Used for integration testing
        /// </summary>
        internal bool ApplyTransformToNetworkState(ref NetworkTransformState networkState, double dirtyTime, Transform transformToUse)
        {
            return ApplyTransformToNetworkStateWithInfo(ref networkState, dirtyTime, transformToUse);
        }

        /// <summary>
        /// Applies the transform to the <see cref="NetworkTransformState"/> specified.
        /// </summary>
        private bool ApplyTransformToNetworkStateWithInfo(ref NetworkTransformState networkState, double dirtyTime, Transform transformToUse)
        {
            var isDirty = false;
            var isPositionDirty = false;
            var isRotationDirty = false;
            var isScaleDirty = false;

            var position = InLocalSpace ? transformToUse.localPosition : transformToUse.position;
            var rotAngles = InLocalSpace ? transformToUse.localEulerAngles : transformToUse.eulerAngles;
            var scale = transformToUse.localScale;

            if (InLocalSpace != networkState.InLocalSpace)
            {
                networkState.InLocalSpace = InLocalSpace;
                isDirty = true;
            }

            if (SyncPositionX && (Mathf.Abs(networkState.PositionX - position.x) >= PositionThreshold || networkState.IsTeleportingNextFrame))
            {
                networkState.PositionX = position.x;
                networkState.HasPositionX = true;
                isPositionDirty = true;
            }

            if (SyncPositionY && (Mathf.Abs(networkState.PositionY - position.y) >= PositionThreshold || networkState.IsTeleportingNextFrame))
            {
                networkState.PositionY = position.y;
                networkState.HasPositionY = true;
                isPositionDirty = true;
            }

            if (SyncPositionZ && (Mathf.Abs(networkState.PositionZ - position.z) >= PositionThreshold || networkState.IsTeleportingNextFrame))
            {
                networkState.PositionZ = position.z;
                networkState.HasPositionZ = true;
                isPositionDirty = true;
            }

            if (SyncRotAngleX && (Mathf.Abs(Mathf.DeltaAngle(networkState.RotAngleX, rotAngles.x)) >= RotAngleThreshold || networkState.IsTeleportingNextFrame))
            {
                networkState.RotAngleX = rotAngles.x;
                networkState.HasRotAngleX = true;
                isRotationDirty = true;
            }

            if (SyncRotAngleY && (Mathf.Abs(Mathf.DeltaAngle(networkState.RotAngleY, rotAngles.y)) >= RotAngleThreshold || networkState.IsTeleportingNextFrame))
            {
                networkState.RotAngleY = rotAngles.y;
                networkState.HasRotAngleY = true;
                isRotationDirty = true;
            }

            if (SyncRotAngleZ && (Mathf.Abs(Mathf.DeltaAngle(networkState.RotAngleZ, rotAngles.z)) >= RotAngleThreshold || networkState.IsTeleportingNextFrame))
            {
                networkState.RotAngleZ = rotAngles.z;
                networkState.HasRotAngleZ = true;
                isRotationDirty = true;
            }

            if (SyncScaleX && (Mathf.Abs(networkState.ScaleX - scale.x) >= ScaleThreshold || networkState.IsTeleportingNextFrame))
            {
                networkState.ScaleX = scale.x;
                networkState.HasScaleX = true;
                isScaleDirty = true;
            }

            if (SyncScaleY && (Mathf.Abs(networkState.ScaleY - scale.y) >= ScaleThreshold || networkState.IsTeleportingNextFrame))
            {
                networkState.ScaleY = scale.y;
                networkState.HasScaleY = true;
                isScaleDirty = true;
            }

            if (SyncScaleZ && (Mathf.Abs(networkState.ScaleZ - scale.z) >= ScaleThreshold || networkState.IsTeleportingNextFrame))
            {
                networkState.ScaleZ = scale.z;
                networkState.HasScaleZ = true;
                isScaleDirty = true;
            }

            isDirty |= isPositionDirty || isRotationDirty || isScaleDirty;

            if (isDirty)
            {
                networkState.SentTime = dirtyTime;
            }

            /// We need to set this in order to know when we can reset our local authority state <see cref="Update"/>
            /// If our state is already dirty or we just found deltas (i.e. isDirty == true)
            networkState.IsDirty |= isDirty;
            return isDirty;
        }

        /// <summary>
        /// Applies the authoritative state to the transform
        /// </summary>
        private void ApplyAuthoritativeState()
        {
            var networkState = ReplicatedNetworkState.Value;
            var adjustedPosition = networkState.InLocalSpace ? transform.localPosition : transform.position;

            // TODO: We should store network state w/ quats vs. euler angles
            var adjustedRotAngles = networkState.InLocalSpace ? transform.localEulerAngles : transform.eulerAngles;
            var adjustedScale = transform.localScale;

            // InLocalSpace Read:
            InLocalSpace = networkState.InLocalSpace;

            // NOTE ABOUT INTERPOLATING AND THE CODE BELOW:
            // We always apply the interpolated state for any axis we are synchronizing even when the state has no deltas
            // to assure we fully interpolate to our target even after we stop extrapolating 1 tick later.
            var useInterpolatedValue = !networkState.IsTeleportingNextFrame && Interpolate;
            if (useInterpolatedValue)
            {
                if (SyncPositionX) { adjustedPosition.x = m_PositionXInterpolator.GetInterpolatedValue(); }
                if (SyncPositionY) { adjustedPosition.y = m_PositionYInterpolator.GetInterpolatedValue(); }
                if (SyncPositionZ) { adjustedPosition.z = m_PositionZInterpolator.GetInterpolatedValue(); }

                if (SyncScaleX) { adjustedScale.x = m_ScaleXInterpolator.GetInterpolatedValue(); }
                if (SyncScaleY) { adjustedScale.y = m_ScaleYInterpolator.GetInterpolatedValue(); }
                if (SyncScaleZ) { adjustedScale.z = m_ScaleZInterpolator.GetInterpolatedValue(); }

                if (SynchronizeRotation)
                {
                    var interpolatedEulerAngles = m_RotationInterpolator.GetInterpolatedValue().eulerAngles;
                    if (SyncRotAngleX) { adjustedRotAngles.x = interpolatedEulerAngles.x; }
                    if (SyncRotAngleY) { adjustedRotAngles.y = interpolatedEulerAngles.y; }
                    if (SyncRotAngleZ) { adjustedRotAngles.z = interpolatedEulerAngles.z; }
                }
            }
            else
            {
                if (networkState.HasPositionX) { adjustedPosition.x = networkState.PositionX; }
                if (networkState.HasPositionY) { adjustedPosition.y = networkState.PositionY; }
                if (networkState.HasPositionZ) { adjustedPosition.z = networkState.PositionZ; }

                if (networkState.HasScaleX) { adjustedScale.x = networkState.ScaleX; }
                if (networkState.HasScaleY) { adjustedScale.y = networkState.ScaleY; }
                if (networkState.HasScaleZ) { adjustedScale.z = networkState.ScaleZ; }

                if (networkState.HasRotAngleX) { adjustedRotAngles.x = networkState.RotAngleX; }
                if (networkState.HasRotAngleY) { adjustedRotAngles.y = networkState.RotAngleY; }
                if (networkState.HasRotAngleZ) { adjustedRotAngles.z = networkState.RotAngleZ; }
            }

            // NOTE: The below conditional checks for applying axial values are required in order to
            // prevent the non-authoritative side from making adjustments when interpolation is off.

            // TODO: Determine if we want to enforce, frame by frame, the non-authoritative transform values.
            // We would want save the position, rotation, and scale (each individually) after applying each
            // authoritative transform state received. Otherwise, the non-authoritative side could make
            // changes to an axial value (if interpolation is turned off) until authority sends an update for
            // that same axial value. When interpolation is on, the state's values being synchronized are
            // always applied each frame.

            // Apply the new position if it has changed or we are interpolating and synchronizing position
            if (networkState.HasPositionChange || (useInterpolatedValue && SynchronizePosition))
            {
                if (InLocalSpace)
                {
                    transform.localPosition = adjustedPosition;
                }
                else
                {
                    transform.position = adjustedPosition;
                }
            }

            // Apply the new rotation if it has changed or we are interpolating and synchronizing rotation
            if (networkState.HasRotAngleChange || (useInterpolatedValue && SynchronizeRotation))
            {
                if (InLocalSpace)
                {
                    transform.localRotation = Quaternion.Euler(adjustedRotAngles);
                }
                else
                {
                    transform.rotation = Quaternion.Euler(adjustedRotAngles);
                }
            }

            // Apply the new scale if it has changed or we are interpolating and synchronizing scale
            if (networkState.HasScaleChange || (useInterpolatedValue && SynchronizeScale))
            {
                transform.localScale = adjustedScale;
            }
        }

        /// <summary>
        /// Only non-authoritative instances should invoke this
        /// </summary>
        private void AddInterpolatedState(NetworkTransformState newState)
        {
            var sentTime = newState.SentTime;
            var currentPosition = newState.InLocalSpace ? transform.localPosition : transform.position;
            var currentRotation = newState.InLocalSpace ? transform.localRotation : transform.rotation;
            var currentEulerAngles = currentRotation.eulerAngles;

            // When there is a change in interpolation or if teleporting, we reset
            if ((newState.InLocalSpace != InLocalSpace) || newState.IsTeleportingNextFrame)
            {
                InLocalSpace = newState.InLocalSpace;
                var currentScale = transform.localScale;

                // we should clear our float interpolators
                foreach (var interpolator in m_AllFloatInterpolators)
                {
                    interpolator.Clear();
                }

                // we should clear our quaternion interpolator
                m_RotationInterpolator.Clear();

                // Adjust based on which axis changed
                if (newState.HasPositionX)
                {
                    m_PositionXInterpolator.ResetTo(newState.PositionX, sentTime);
                    currentPosition.x = newState.PositionX;
                }

                if (newState.HasPositionY)
                {
                    m_PositionYInterpolator.ResetTo(newState.PositionY, sentTime);
                    currentPosition.y = newState.PositionY;
                }

                if (newState.HasPositionZ)
                {
                    m_PositionZInterpolator.ResetTo(newState.PositionZ, sentTime);
                    currentPosition.z = newState.PositionZ;
                }

                // Apply the position
                if (newState.InLocalSpace)
                {
                    transform.localPosition = currentPosition;
                }
                else
                {
                    transform.position = currentPosition;
                }

                // Adjust based on which axis changed
                if (newState.HasScaleX)
                {
                    m_ScaleXInterpolator.ResetTo(newState.ScaleX, sentTime);
                    currentScale.x = newState.ScaleX;
                }

                if (newState.HasScaleY)
                {
                    m_ScaleYInterpolator.ResetTo(newState.ScaleY, sentTime);
                    currentScale.y = newState.ScaleY;
                }

                if (newState.HasScaleZ)
                {
                    m_ScaleZInterpolator.ResetTo(newState.ScaleZ, sentTime);
                    currentScale.z = newState.ScaleZ;
                }

                // Apply the adjusted scale
                transform.localScale = currentScale;

                // Adjust based on which axis changed
                if (newState.HasRotAngleX)
                {
                    currentEulerAngles.x = newState.RotAngleX;
                }

                if (newState.HasRotAngleY)
                {
                    currentEulerAngles.y = newState.RotAngleY;
                }

                if (newState.HasRotAngleZ)
                {
                    currentEulerAngles.z = newState.RotAngleZ;
                }

                // Apply the rotation
                currentRotation.eulerAngles = currentEulerAngles;
                transform.rotation = currentRotation;

                // Reset the rotation interpolator
                m_RotationInterpolator.ResetTo(currentRotation, sentTime);
                return;
            }

            // Apply axial changes from the new state
            if (newState.HasPositionX)
            {
                m_PositionXInterpolator.AddMeasurement(newState.PositionX, sentTime);
            }

            if (newState.HasPositionY)
            {
                m_PositionYInterpolator.AddMeasurement(newState.PositionY, sentTime);
            }

            if (newState.HasPositionZ)
            {
                m_PositionZInterpolator.AddMeasurement(newState.PositionZ, sentTime);
            }

            if (newState.HasScaleX)
            {
                m_ScaleXInterpolator.AddMeasurement(newState.ScaleX, sentTime);
            }

            if (newState.HasScaleY)
            {
                m_ScaleYInterpolator.AddMeasurement(newState.ScaleY, sentTime);
            }

            if (newState.HasScaleZ)
            {
                m_ScaleZInterpolator.AddMeasurement(newState.ScaleZ, sentTime);
            }

            // With rotation, we check if there are any changes first and
            // if so then apply the changes to the current Euler rotation
            // values.
            if (newState.HasRotAngleChange)
            {
                if (newState.HasRotAngleX)
                {
                    currentEulerAngles.x = newState.RotAngleX;
                }

                if (newState.HasRotAngleY)
                {
                    currentEulerAngles.y = newState.RotAngleY;
                }

                if (newState.HasRotAngleZ)
                {
                    currentEulerAngles.z = newState.RotAngleZ;
                }

                currentRotation.eulerAngles = currentEulerAngles;

                m_RotationInterpolator.AddMeasurement(currentRotation, sentTime);
            }
        }

        /// <summary>
        /// Only non-authoritative instances should invoke this method
        /// </summary>
        private void OnNetworkStateChanged(NetworkTransformState oldState, NetworkTransformState newState)
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (CanCommitToTransform)
            {
                // we're the authority, we ignore incoming changes
                return;
            }

            if (Interpolate)
            {
                // Add measurements for the new state's deltas
                AddInterpolatedState(newState);
            }
        }

        /// <summary>
        /// Will set the maximum interpolation boundary for the interpolators of this <see cref="NetworkTransform"/> instance.
        /// This value roughly translates to the maximum value of 't' in <see cref="Mathf.Lerp(float, float, float)"/> and
        /// <see cref="Mathf.LerpUnclamped(float, float, float)"/> for all transform elements being monitored by
        /// <see cref="NetworkTransform"/> (i.e. Position, Rotation, and Scale)
        /// </summary>
        /// <param name="maxInterpolationBound">Maximum time boundary that can be used in a frame when interpolating between two values</param>
        public void SetMaxInterpolationBound(float maxInterpolationBound)
        {
            m_PositionXInterpolator.MaxInterpolationBound = maxInterpolationBound;
            m_PositionYInterpolator.MaxInterpolationBound = maxInterpolationBound;
            m_PositionZInterpolator.MaxInterpolationBound = maxInterpolationBound;
            m_RotationInterpolator.MaxInterpolationBound = maxInterpolationBound;
            m_ScaleXInterpolator.MaxInterpolationBound = maxInterpolationBound;
            m_ScaleYInterpolator.MaxInterpolationBound = maxInterpolationBound;
            m_ScaleZInterpolator.MaxInterpolationBound = maxInterpolationBound;
        }

        /// <summary>
        /// Create interpolators when first instantiated to avoid memory allocations if the
        /// associated NetworkObject persists (i.e. despawned but not destroyed or pools)
        /// </summary>
        private void Awake()
        {
            // Rotation is a single Quaternion since each Euler axis will affect the quaternion's final value
            m_RotationInterpolator = new BufferedLinearInterpolatorQuaternion();

            // All other interpolators are BufferedLinearInterpolatorFloats
            m_PositionXInterpolator = new BufferedLinearInterpolatorFloat();
            m_PositionYInterpolator = new BufferedLinearInterpolatorFloat();
            m_PositionZInterpolator = new BufferedLinearInterpolatorFloat();
            m_ScaleXInterpolator = new BufferedLinearInterpolatorFloat();
            m_ScaleYInterpolator = new BufferedLinearInterpolatorFloat();
            m_ScaleZInterpolator = new BufferedLinearInterpolatorFloat();

            // Used to quickly iteration over the BufferedLinearInterpolatorFloat
            // instances
            if (m_AllFloatInterpolators.Count == 0)
            {
                m_AllFloatInterpolators.Add(m_PositionXInterpolator);
                m_AllFloatInterpolators.Add(m_PositionYInterpolator);
                m_AllFloatInterpolators.Add(m_PositionZInterpolator);
                m_AllFloatInterpolators.Add(m_ScaleXInterpolator);
                m_AllFloatInterpolators.Add(m_ScaleYInterpolator);
                m_AllFloatInterpolators.Add(m_ScaleZInterpolator);
            }
        }

        /// <inheritdoc/>
        public override void OnNetworkSpawn()
        {
            m_CachedIsServer = IsServer;
            m_CachedNetworkManager = NetworkManager;

            Initialize();

            // This assures the initial spawning of the object synchronizes all connected clients
            // with the current transform values. This should not be placed within Initialize since
            // that can be invoked when ownership changes.
            if (CanCommitToTransform)
            {
                var currentPosition = InLocalSpace ? transform.localPosition : transform.position;
                var currentRotation = InLocalSpace ? transform.localRotation : transform.rotation;
                // Teleport to current position
                SetStateInternal(currentPosition, currentRotation, transform.localScale, true);

                // Force the state update to be sent
                TryCommitTransform(transform, m_CachedNetworkManager.LocalTime.Time);
            }
        }

        /// <inheritdoc/>
        public override void OnNetworkDespawn()
        {
            ReplicatedNetworkState.OnValueChanged -= OnNetworkStateChanged;
        }

        /// <inheritdoc/>
        public override void OnDestroy()
        {
            base.OnDestroy();
            m_ReplicatedNetworkStateServer.Dispose();
            m_ReplicatedNetworkStateOwner.Dispose();
        }

        /// <inheritdoc/>
        public override void OnGainedOwnership()
        {
            Initialize();
        }

        /// <inheritdoc/>
        public override void OnLostOwnership()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes NetworkTransform when spawned and ownership changes.
        /// </summary>
        private void Initialize()
        {
            if (!IsSpawned)
            {
                return;
            }

            CanCommitToTransform = IsServerAuthoritative() ? IsServer : IsOwner;
            var replicatedState = ReplicatedNetworkState;
            m_LocalAuthoritativeNetworkState = replicatedState.Value;

            if (CanCommitToTransform)
            {
                replicatedState.OnValueChanged -= OnNetworkStateChanged;
            }
            else
            {
                replicatedState.OnValueChanged += OnNetworkStateChanged;

                // In case we are late joining
                ResetInterpolatedStateToCurrentAuthoritativeState();
            }
        }

        /// <summary>
        /// Directly sets a state on the authoritative transform.
        /// Owner clients can directly set the state on a server authoritative transform
        /// This will override any changes made previously to the transform
        /// This isn't resistant to network jitter. Server side changes due to this method won't be interpolated.
        /// The parameters are broken up into pos / rot / scale on purpose so that the caller can perturb
        ///  just the desired one(s)
        /// </summary>
        /// <param name="posIn"></param> new position to move to.  Can be null
        /// <param name="rotIn"></param> new rotation to rotate to.  Can be null
        /// <param name="scaleIn">new scale to scale to. Can be null</param>
        /// <param name="shouldGhostsInterpolate">Should other clients interpolate this change or not. True by default</param>
        /// new scale to scale to.  Can be null
        /// <exception cref="Exception"></exception>
        public void SetState(Vector3? posIn = null, Quaternion? rotIn = null, Vector3? scaleIn = null, bool shouldGhostsInterpolate = true)
        {
            if (!IsSpawned)
            {
                return;
            }

            // Only the server or owner can invoke this method
            if (!IsOwner && !m_CachedIsServer)
            {
                throw new Exception("Non-owner client instance cannot set the state of the NetworkTransform!");
            }

            Vector3 pos = posIn == null ? InLocalSpace ? transform.localPosition : transform.position : posIn.Value;
            Quaternion rot = rotIn == null ? InLocalSpace ? transform.localRotation : transform.rotation : rotIn.Value;
            Vector3 scale = scaleIn == null ? transform.localScale : scaleIn.Value;

            if (!CanCommitToTransform)
            {
                // Preserving the ability for owner authoritative mode to accept state changes from server
                if (m_CachedIsServer)
                {
                    m_ClientIds[0] = OwnerClientId;
                    m_ClientRpcParams.Send.TargetClientIds = m_ClientIds;
                    SetStateClientRpc(pos, rot, scale, !shouldGhostsInterpolate, m_ClientRpcParams);
                }
                else // Preserving the ability for server authoritative mode to accept state changes from owner
                {
                    SetStateServerRpc(pos, rot, scale, !shouldGhostsInterpolate);
                }
                return;
            }

            SetStateInternal(pos, rot, scale, !shouldGhostsInterpolate);
        }

        /// <summary>
        /// Authoritative only method
        /// Sets the internal state (teleporting or just set state) of the authoritative
        /// transform directly.
        /// </summary>
        private void SetStateInternal(Vector3 pos, Quaternion rot, Vector3 scale, bool shouldTeleport)
        {
            if (InLocalSpace)
            {
                transform.localPosition = pos;
                transform.localRotation = rot;
            }
            else
            {
                transform.SetPositionAndRotation(pos, rot);
            }
            transform.localScale = scale;
            m_LocalAuthoritativeNetworkState.IsTeleportingNextFrame = shouldTeleport;

            TryCommitTransform(transform, m_CachedNetworkManager.LocalTime.Time);
        }

        /// <summary>
        /// Invoked by <see cref="SetState"/>, allows a non-owner server to update the transform state
        /// </summary>
        /// <remarks>
        /// Continued support for client-driven server authority model
        /// </remarks>
        [ClientRpc]
        private void SetStateClientRpc(Vector3 pos, Quaternion rot, Vector3 scale, bool shouldTeleport, ClientRpcParams clientRpcParams = default)
        {
            // Server dictated state is always applied
            SetStateInternal(pos, rot, scale, shouldTeleport);
        }

        /// <summary>
        /// Invoked by <see cref="SetState"/>, allows an owner-client update the transform state
        /// </summary>
        /// <remarks>
        /// Continued support for client-driven server authority model
        /// </remarks>
        [ServerRpc]
        private void SetStateServerRpc(Vector3 pos, Quaternion rot, Vector3 scale, bool shouldTeleport)
        {
            // server has received this RPC request to move change transform. give the server a chance to modify or even reject the move
            if (OnClientRequestChange != null)
            {
                (pos, rot, scale) = OnClientRequestChange(pos, rot, scale);
            }
            SetStateInternal(pos, rot, scale, shouldTeleport);
        }

        /// <summary>
        /// Will update the authoritative transform state if any deltas are detected.
        /// This will also reset the m_LocalAuthoritativeNetworkState if it is still dirty
        /// but the replicated network state is not.
        /// </summary>
        /// <param name="transformSource">transform to be updated</param>
        private void UpdateAuthoritativeState(Transform transformSource)
        {
            // If our replicated state is not dirty and our local authority state is dirty, clear it.
            if (!ReplicatedNetworkState.IsDirty() && m_LocalAuthoritativeNetworkState.IsDirty)
            {
                m_LastSentState = m_LocalAuthoritativeNetworkState;
                // Now clear our bitset and prepare for next network tick state update
                m_LocalAuthoritativeNetworkState.ClearBitSetForNextTick();
            }

            TryCommitTransform(transformSource, m_CachedNetworkManager.LocalTime.Time);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// If you override this method, be sure that:
        /// - Non-owners always invoke this base class method when using interpolation.
        /// - Authority can opt to use <see cref="TryCommitTransformToServer"/> in place of invoking this base class method.
        /// - Non-authority owners can use <see cref="TryCommitTransformToServer"/> but should still invoke the this base class method when using interpolation.
        /// </remarks>
        protected virtual void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            // If we are authority, update the authoritative state
            if (CanCommitToTransform)
            {
                UpdateAuthoritativeState(transform);
            }
            else // Non-Authority
            {
                if (Interpolate)
                {
                    var serverTime = NetworkManager.ServerTime;
                    var cachedDeltaTime = Time.deltaTime;
                    var cachedServerTime = serverTime.Time;
                    var cachedRenderTime = serverTime.TimeTicksAgo(1).Time;
                    foreach (var interpolator in m_AllFloatInterpolators)
                    {
                        interpolator.Update(cachedDeltaTime, cachedRenderTime, cachedServerTime);
                    }

                    m_RotationInterpolator.Update(cachedDeltaTime, cachedRenderTime, cachedServerTime);
                }

                // Apply the current authoritative state
                ApplyAuthoritativeState();
            }
        }

        /// <summary>
        /// Teleport the transform to the given values without interpolating
        /// </summary>
        /// <param name="newPosition"></param> new position to move to.
        /// <param name="newRotation"></param> new rotation to rotate to.
        /// <param name="newScale">new scale to scale to.</param>
        /// <exception cref="Exception"></exception>
        public void Teleport(Vector3 newPosition, Quaternion newRotation, Vector3 newScale)
        {
            if (!CanCommitToTransform)
            {
                throw new Exception("Teleporting on non-authoritative side is not allowed!");
            }

            // Teleporting now is as simple as setting the internal state and passing the teleport flag
            SetStateInternal(newPosition, newRotation, newScale, true);
        }

        /// <summary>
        /// Override this method and return false to switch to owner authoritative mode
        /// </summary>
        /// <returns>(<see cref="true"/> or <see cref="false"/>) where when false it runs as owner-client authoritative</returns>
        protected virtual bool OnIsServerAuthoritative()
        {
            return true;
        }

        /// <summary>
        /// Used by <see cref="NetworkRigidbody"/> to determines if this is server or owner authoritative.
        /// </summary>
        internal bool IsServerAuthoritative()
        {
            return OnIsServerAuthoritative();
        }
    }
}
