using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// A component for syncing transforms
    /// NetworkTransform will read the underlying transform and replicate it to clients.
    /// The replicated value will be automatically be interpolated (if active) and applied to the underlying GameObject's transform
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Netcode/" + nameof(NetworkTransform))]
    [DefaultExecutionOrder(100000)] // this is needed to catch the update time after the transform was updated by user scripts
    public class NetworkTransform : NetworkBehaviour
    {
        public const float PositionThresholdDefault = .001f;
        public const float RotAngleThresholdDefault = .01f;
        public const float ScaleThresholdDefault = .01f;
        public delegate (Vector3 pos, Quaternion rotOut, Vector3 scale) OnClientRequestChangeDelegate(Vector3 pos, Quaternion rot, Vector3 scale);
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

            public bool InLocalSpace
            {
                get => (m_Bitset & (1 << k_InLocalSpaceBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_InLocalSpaceBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_InLocalSpaceBit)); }
                }
            }

            // Position
            public bool HasPositionX
            {
                get => (m_Bitset & (1 << k_PositionXBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_PositionXBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_PositionXBit)); }
                }
            }

            public bool HasPositionY
            {
                get => (m_Bitset & (1 << k_PositionYBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_PositionYBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_PositionYBit)); }
                }
            }

            public bool HasPositionZ
            {
                get => (m_Bitset & (1 << k_PositionZBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_PositionZBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_PositionZBit)); }
                }
            }

            // RotAngles
            public bool HasRotAngleX
            {
                get => (m_Bitset & (1 << k_RotAngleXBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_RotAngleXBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_RotAngleXBit)); }
                }
            }

            public bool HasRotAngleY
            {
                get => (m_Bitset & (1 << k_RotAngleYBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_RotAngleYBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_RotAngleYBit)); }
                }
            }

            public bool HasRotAngleZ
            {
                get => (m_Bitset & (1 << k_RotAngleZBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_RotAngleZBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_RotAngleZBit)); }
                }
            }

            // Scale
            public bool HasScaleX
            {
                get => (m_Bitset & (1 << k_ScaleXBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_ScaleXBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_ScaleXBit)); }
                }
            }

            public bool HasScaleY
            {
                get => (m_Bitset & (1 << k_ScaleYBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_ScaleYBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_ScaleYBit)); }
                }
            }

            public bool HasScaleZ
            {
                get => (m_Bitset & (1 << k_ScaleZBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_ScaleZBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_ScaleZBit)); }
                }
            }

            public bool IsTeleportingNextFrame
            {
                get => (m_Bitset & (1 << k_TeleportingBit)) != 0;
                set
                {
                    if (value) { m_Bitset = (ushort)(m_Bitset | (1 << k_TeleportingBit)); }
                    else { m_Bitset = (ushort)(m_Bitset & ~(1 << k_TeleportingBit)); }
                }
            }

            public float PositionX, PositionY, PositionZ;
            public float RotAngleX, RotAngleY, RotAngleZ;
            public float ScaleX, ScaleY, ScaleZ;
            public double SentTime;

            public Vector3 Position
            {
                get { return new Vector3(PositionX, PositionY, PositionZ); }
                set
                {
                    PositionX = value.x;
                    PositionY = value.y;
                    PositionZ = value.z;
                }
            }

            public Vector3 Rotation
            {
                get { return new Vector3(RotAngleX, RotAngleY, RotAngleZ); }
                set
                {
                    RotAngleX = value.x;
                    RotAngleY = value.y;
                    RotAngleZ = value.z;
                }
            }

            public Vector3 Scale
            {
                get { return new Vector3(ScaleX, ScaleY, ScaleZ); }
                set
                {
                    ScaleX = value.x;
                    ScaleY = value.y;
                    ScaleZ = value.z;
                }
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
            }
        }

        public bool SyncPositionX = true, SyncPositionY = true, SyncPositionZ = true;
        public bool SyncRotAngleX = true, SyncRotAngleY = true, SyncRotAngleZ = true;
        public bool SyncScaleX = true, SyncScaleY = true, SyncScaleZ = true;

        public float PositionThreshold = PositionThresholdDefault;
        public float RotAngleThreshold = RotAngleThresholdDefault;
        public float ScaleThreshold = ScaleThresholdDefault;

        /// <summary>
        /// Sets whether this transform should sync in local space or in world space.
        /// This is important to set since reparenting this transform could have issues,
        /// if using world position (depending on who gets synced first: the parent or the child)
        /// Having a child always at position 0,0,0 for example will have less possibilities of desync than when using world positions
        /// </summary>
        [Tooltip("Sets whether this transform should sync in local space or in world space")]
        public bool InLocalSpace = false;

        public bool Interpolate = true;

        /// <summary>
        /// Used to determine who can write to this transform. Server only for this transform.
        /// Changing this value alone in a child implementation will not allow you to create a NetworkTransform which can be written to by clients. See the ClientNetworkTransform Sample
        /// in the package samples for how to implement a NetworkTransform with client write support.
        /// If using different values, please use RPCs to write to the server. Netcode doesn't support client side network variable writing
        /// </summary>
        // This is public to make sure that users don't depend on this IsClient && IsOwner check in their code. If this logic changes in the future, we can make it invisible here
        public bool CanCommitToTransform;
        protected bool m_CachedIsServer;
        protected NetworkManager m_CachedNetworkManager;

        private readonly NetworkVariable<NetworkTransformState> m_ReplicatedNetworkState = new NetworkVariable<NetworkTransformState>(new NetworkTransformState());

        private NetworkTransformState m_LocalAuthoritativeNetworkState;

        private NetworkTransformState m_PrevNetworkState;

        private const int k_DebugDrawLineTime = 10;

        private bool m_HasSentLastValue = false; // used to send one last value, so clients can make the difference between lost replication data (clients extrapolate) and no more data to send.


        private BufferedLinearInterpolator<float> m_PositionXInterpolator; // = new BufferedLinearInterpolatorFloat();
        private BufferedLinearInterpolator<float> m_PositionYInterpolator; // = new BufferedLinearInterpolatorFloat();
        private BufferedLinearInterpolator<float> m_PositionZInterpolator; // = new BufferedLinearInterpolatorFloat();
        private BufferedLinearInterpolator<Quaternion> m_RotationInterpolator; // = new BufferedLinearInterpolatorQuaternion(); // rotation is a single Quaternion since each euler axis will affect the quaternion's final value
        private BufferedLinearInterpolator<float> m_ScaleXInterpolator; // = new BufferedLinearInterpolatorFloat();
        private BufferedLinearInterpolator<float> m_ScaleYInterpolator; // = new BufferedLinearInterpolatorFloat();
        private BufferedLinearInterpolator<float> m_ScaleZInterpolator; // = new BufferedLinearInterpolatorFloat();
        private readonly List<BufferedLinearInterpolator<float>> m_AllFloatInterpolators = new List<BufferedLinearInterpolator<float>>(6);

        private Transform m_Transform; // cache the transform component to reduce unnecessary bounce between managed and native
        private int m_LastSentTick;
        private NetworkTransformState m_LastSentState;

        /// <summary>
        /// Tries updating the server authoritative transform, only if allowed.
        /// If this called server side, this will commit directly.
        /// If no update is needed, nothing will be sent. This method should still be called every update, it'll self manage when it should and shouldn't send
        /// </summary>
        /// <param name="transformToCommit"></param>
        /// <param name="dirtyTime"></param>
        protected void TryCommitTransformToServer(Transform transformToCommit, double dirtyTime)
        {
            var isDirty = ApplyTransformToNetworkState(ref m_LocalAuthoritativeNetworkState, dirtyTime, transformToCommit);
            TryCommit(isDirty);
        }

        private void TryCommitValuesToServer(Vector3 position, Vector3 rotation, Vector3 scale, double dirtyTime)
        {
            var isDirty = ApplyTransformToNetworkStateWithInfo(ref m_LocalAuthoritativeNetworkState, dirtyTime, position, rotation, scale);

            TryCommit(isDirty.isDirty);
        }

        private void TryCommit(bool isDirty)
        {
            void Send(NetworkTransformState stateToSend)
            {
                if (m_CachedIsServer)
                {
                    // server RPC takes a few frames to execute server side, we want this to execute immediately
                    CommitLocallyAndReplicate(stateToSend);
                }
                else
                {
                    CommitTransformServerRpc(stateToSend);
                }
            }

            // if dirty, send
            // if not dirty anymore, but hasn't sent last value for limiting extrapolation, still set isDirty
            // if not dirty and has already sent last value, don't do anything
            // extrapolation works by using last two values. if it doesn't receive anything anymore, it'll continue to extrapolate.
            // This is great in case there's message loss, not so great if we just don't have new values to send.
            // the following will send one last "copied" value so unclamped interpolation tries to extrapolate between two identical values, effectively
            // making it immobile.
            if (isDirty)
            {
                Send(m_LocalAuthoritativeNetworkState);
                m_HasSentLastValue = false;
                m_LastSentTick = m_CachedNetworkManager.LocalTime.Tick;
                m_LastSentState = m_LocalAuthoritativeNetworkState;
            }
            else if (!m_HasSentLastValue && m_CachedNetworkManager.LocalTime.Tick >= m_LastSentTick + 1) // check for state.IsDirty since update can happen more than once per tick. No need for client, RPCs will just queue up
            {
                m_LastSentState.SentTime = m_CachedNetworkManager.LocalTime.Time; // time 1+ tick later
                Send(m_LastSentState);
                m_HasSentLastValue = true;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void CommitTransformServerRpc(NetworkTransformState networkState, ServerRpcParams serverParams = default)
        {
            if (serverParams.Receive.SenderClientId == OwnerClientId) // RPC call when not authorized to write could happen during the RTT interval during which a server's ownership change hasn't reached the client yet
            {
                CommitLocallyAndReplicate(networkState);
            }
        }

        private void CommitLocallyAndReplicate(NetworkTransformState networkState)
        {
            m_ReplicatedNetworkState.Value = networkState;
            AddInterpolatedState(networkState);
        }

        private void ResetInterpolatedStateToCurrentAuthoritativeState()
        {
            var serverTime = NetworkManager.ServerTime.Time;
            m_PositionXInterpolator.ResetTo(m_LocalAuthoritativeNetworkState.PositionX, serverTime);
            m_PositionYInterpolator.ResetTo(m_LocalAuthoritativeNetworkState.PositionY, serverTime);
            m_PositionZInterpolator.ResetTo(m_LocalAuthoritativeNetworkState.PositionZ, serverTime);

            m_RotationInterpolator.ResetTo(Quaternion.Euler(m_LocalAuthoritativeNetworkState.Rotation), serverTime);

            m_ScaleXInterpolator.ResetTo(m_LocalAuthoritativeNetworkState.ScaleX, serverTime);
            m_ScaleYInterpolator.ResetTo(m_LocalAuthoritativeNetworkState.ScaleY, serverTime);
            m_ScaleZInterpolator.ResetTo(m_LocalAuthoritativeNetworkState.ScaleZ, serverTime);
        }

        // updates `NetworkState` properties if they need to and returns a `bool` indicating whether or not there was any changes made
        // returned boolean would be useful to change encapsulating `NetworkVariable<NetworkState>`'s dirty state, e.g. ReplNetworkState.SetDirty(isDirty);
        internal bool ApplyTransformToNetworkState(ref NetworkTransformState networkState, double dirtyTime, Transform transformToUse)
        {
            return ApplyTransformToNetworkStateWithInfo(ref networkState, dirtyTime, transformToUse).isDirty;
        }

        private (bool isDirty, bool isPositionDirty, bool isRotationDirty, bool isScaleDirty) ApplyTransformToNetworkStateWithInfo(ref NetworkTransformState networkState, double dirtyTime, Transform transformToUse)
        {
            var position = InLocalSpace ? transformToUse.localPosition : transformToUse.position;
            var rotAngles = InLocalSpace ? transformToUse.localEulerAngles : transformToUse.eulerAngles;
            var scale = transformToUse.localScale;
            return ApplyTransformToNetworkStateWithInfo(ref networkState, dirtyTime, position, rotAngles, scale);
        }

        private (bool isDirty, bool isPositionDirty, bool isRotationDirty, bool isScaleDirty) ApplyTransformToNetworkStateWithInfo(ref NetworkTransformState networkState, double dirtyTime, Vector3 position, Vector3 rotAngles, Vector3 scale)
        {
            var isDirty = false;
            var isPositionDirty = false;
            var isRotationDirty = false;
            var isScaleDirty = false;

            // hasPositionZ set to false when it should be true?

            if (InLocalSpace != networkState.InLocalSpace)
            {
                networkState.InLocalSpace = InLocalSpace;
                isDirty = true;
            }

            // we assume that if x, y or z are dirty then we'll have to send all 3 anyway, so for efficiency
            //  we skip doing the (quite expensive) Math.Approximately() and check against PositionThreshold
            //  this still is overly costly and could use more improvements.
            //
            // (ditto for scale components)
            if (SyncPositionX &&
                Mathf.Abs(networkState.PositionX - position.x) > PositionThreshold)
            {
                networkState.PositionX = position.x;
                networkState.HasPositionX = true;
                isPositionDirty = true;
            }

            if (SyncPositionY &&
                Mathf.Abs(networkState.PositionY - position.y) > PositionThreshold)
            {
                networkState.PositionY = position.y;
                networkState.HasPositionY = true;
                isPositionDirty = true;
            }

            if (SyncPositionZ &&
                Mathf.Abs(networkState.PositionZ - position.z) > PositionThreshold)
            {
                networkState.PositionZ = position.z;
                networkState.HasPositionZ = true;
                isPositionDirty = true;
            }

            if (SyncRotAngleX &&
                Mathf.Abs(networkState.RotAngleX - rotAngles.x) > RotAngleThreshold)
            {
                networkState.RotAngleX = rotAngles.x;
                networkState.HasRotAngleX = true;
                isRotationDirty = true;
            }

            if (SyncRotAngleY &&
                Mathf.Abs(networkState.RotAngleY - rotAngles.y) > RotAngleThreshold)
            {
                networkState.RotAngleY = rotAngles.y;
                networkState.HasRotAngleY = true;
                isRotationDirty = true;
            }

            if (SyncRotAngleZ &&
                Mathf.Abs(networkState.RotAngleZ - rotAngles.z) > RotAngleThreshold)
            {
                networkState.RotAngleZ = rotAngles.z;
                networkState.HasRotAngleZ = true;
                isRotationDirty = true;
            }

            if (SyncScaleX &&
                Mathf.Abs(networkState.ScaleX - scale.x) > ScaleThreshold)
            {
                networkState.ScaleX = scale.x;
                networkState.HasScaleX = true;
                isScaleDirty = true;
            }

            if (SyncScaleY &&
                Mathf.Abs(networkState.ScaleY - scale.y) > ScaleThreshold)
            {
                networkState.ScaleY = scale.y;
                networkState.HasScaleY = true;
                isScaleDirty = true;
            }

            if (SyncScaleZ &&
                Mathf.Abs(networkState.ScaleZ - scale.z) > ScaleThreshold)
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

            return (isDirty, isPositionDirty, isRotationDirty, isScaleDirty);
        }

        private void ApplyInterpolatedNetworkStateToTransform(NetworkTransformState networkState, Transform transformToUpdate)
        {
            m_PrevNetworkState = networkState;

            var interpolatedPosition = InLocalSpace ? transformToUpdate.localPosition : transformToUpdate.position;

            // todo: we should store network state w/ quats vs. euler angles
            var interpolatedRotAngles = InLocalSpace ? transformToUpdate.localEulerAngles : transformToUpdate.eulerAngles;
            var interpolatedScale = transformToUpdate.localScale;

            // InLocalSpace Read
            InLocalSpace = networkState.InLocalSpace;
            // Position Read
            if (SyncPositionX)
            {
                interpolatedPosition.x = networkState.IsTeleportingNextFrame || !Interpolate ? networkState.Position.x : m_PositionXInterpolator.GetInterpolatedValue();
            }

            if (SyncPositionY)
            {
                interpolatedPosition.y = networkState.IsTeleportingNextFrame || !Interpolate ? networkState.Position.y : m_PositionYInterpolator.GetInterpolatedValue();
            }

            if (SyncPositionZ)
            {
                interpolatedPosition.z = networkState.IsTeleportingNextFrame || !Interpolate ? networkState.Position.z : m_PositionZInterpolator.GetInterpolatedValue();
            }

            // again, we should be using quats here
            if (SyncRotAngleX || SyncRotAngleY || SyncRotAngleZ)
            {
                var eulerAngles = m_RotationInterpolator.GetInterpolatedValue().eulerAngles;
                if (SyncRotAngleX)
                {
                    interpolatedRotAngles.x = networkState.IsTeleportingNextFrame || !Interpolate ? networkState.Rotation.x : eulerAngles.x;
                }

                if (SyncRotAngleY)
                {
                    interpolatedRotAngles.y = networkState.IsTeleportingNextFrame || !Interpolate ? networkState.Rotation.y : eulerAngles.y;
                }

                if (SyncRotAngleZ)
                {
                    interpolatedRotAngles.z = networkState.IsTeleportingNextFrame || !Interpolate ? networkState.Rotation.z : eulerAngles.z;
                }
            }

            // Scale Read
            if (SyncScaleX)
            {
                interpolatedScale.x = networkState.IsTeleportingNextFrame || !Interpolate ? networkState.Scale.x : m_ScaleXInterpolator.GetInterpolatedValue();
            }

            if (SyncScaleY)
            {
                interpolatedScale.y = networkState.IsTeleportingNextFrame || !Interpolate ? networkState.Scale.y : m_ScaleYInterpolator.GetInterpolatedValue();
            }

            if (SyncScaleZ)
            {
                interpolatedScale.z = networkState.IsTeleportingNextFrame || !Interpolate ? networkState.Scale.z : m_ScaleZInterpolator.GetInterpolatedValue();
            }

            // Position Apply
            if (SyncPositionX || SyncPositionY || SyncPositionZ)
            {
                if (InLocalSpace)
                {
                    transformToUpdate.localPosition = interpolatedPosition;
                }
                else
                {
                    transformToUpdate.position = interpolatedPosition;
                }

                m_PrevNetworkState.Position = interpolatedPosition;
            }

            // RotAngles Apply
            if (SyncRotAngleX || SyncRotAngleY || SyncRotAngleZ)
            {
                if (InLocalSpace)
                {
                    transformToUpdate.localRotation = Quaternion.Euler(interpolatedRotAngles);
                }
                else
                {
                    transformToUpdate.rotation = Quaternion.Euler(interpolatedRotAngles);
                }

                m_PrevNetworkState.Rotation = interpolatedRotAngles;
            }

            // Scale Apply
            if (SyncScaleX || SyncScaleY || SyncScaleZ)
            {
                transformToUpdate.localScale = interpolatedScale;
                m_PrevNetworkState.Scale = interpolatedScale;
            }
        }

        private void AddInterpolatedState(NetworkTransformState newState)
        {
            var sentTime = newState.SentTime;

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

            m_RotationInterpolator.AddMeasurement(Quaternion.Euler(newState.Rotation), sentTime);

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
        }

        private void OnNetworkStateChanged(NetworkTransformState oldState, NetworkTransformState newState)
        {
            if (!NetworkObject.IsSpawned)
            {
                // todo MTT-849 should never happen but yet it does! maybe revisit/dig after NetVar updates and snapshot system lands?
                return;
            }

            if (CanCommitToTransform)
            {
                // we're the authority, we ignore incoming changes
                return;
            }

            Debug.DrawLine(newState.Position, newState.Position + Vector3.up + Vector3.left, Color.green, 10, false);

            AddInterpolatedState(newState);

            if (m_CachedNetworkManager.LogLevel == LogLevel.Developer)
            {
                var pos = new Vector3(newState.PositionX, newState.PositionY, newState.PositionZ);
                Debug.DrawLine(pos, pos + Vector3.up + Vector3.left * Random.Range(0.5f, 2f), Color.green, k_DebugDrawLineTime, false);
            }
        }

        private void Awake()
        {
            // we only want to create our interpolators during Awake so that, when pooled, we do not create tons
            //  of gc thrash each time objects wink out and are re-used
            m_PositionXInterpolator = new BufferedLinearInterpolatorFloat();
            m_PositionYInterpolator = new BufferedLinearInterpolatorFloat();
            m_PositionZInterpolator = new BufferedLinearInterpolatorFloat();
            m_RotationInterpolator = new BufferedLinearInterpolatorQuaternion(); // rotation is a single Quaternion since each euler axis will affect the quaternion's final value
            m_ScaleXInterpolator = new BufferedLinearInterpolatorFloat();
            m_ScaleYInterpolator = new BufferedLinearInterpolatorFloat();
            m_ScaleZInterpolator = new BufferedLinearInterpolatorFloat();

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

        public override void OnNetworkSpawn()
        {
            // must set up m_Transform in OnNetworkSpawn because it's possible an object spawns but is disabled
            //  and thus awake won't be called.
            // TODO: investigate further on not sending data for something that is not enabled
            m_Transform = transform;
            m_ReplicatedNetworkState.OnValueChanged += OnNetworkStateChanged;

            CanCommitToTransform = IsServer;
            m_CachedIsServer = IsServer;
            m_CachedNetworkManager = NetworkManager;

            if (CanCommitToTransform)
            {
                TryCommitTransformToServer(m_Transform, m_CachedNetworkManager.LocalTime.Time);
            }
            m_LocalAuthoritativeNetworkState = m_ReplicatedNetworkState.Value;

            // crucial we do this to reset the interpolators so that recycled objects when using a pool will
            //  not have leftover interpolator state from the previous object
            Initialize();
        }

        public override void OnNetworkDespawn()
        {
            m_ReplicatedNetworkState.OnValueChanged -= OnNetworkStateChanged;
        }

        public override void OnGainedOwnership()
        {
            Initialize();
        }

        public override void OnLostOwnership()
        {
            Initialize();
        }

        private void Initialize()
        {
            ResetInterpolatedStateToCurrentAuthoritativeState(); // useful for late joining

            if (CanCommitToTransform)
            {
                m_ReplicatedNetworkState.SetDirty(true);
            }
            else
            {
                ApplyInterpolatedNetworkStateToTransform(m_ReplicatedNetworkState.Value, m_Transform);
            }
        }

        #region state set

        /// <summary>
        /// Directly sets a state on the authoritative transform.
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
            if (!IsOwner)
            {
                throw new Exception("Trying to set a state on a not owned transform");
            }

            if (m_CachedNetworkManager && !(m_CachedNetworkManager.IsConnectedClient || m_CachedNetworkManager.IsListening))
            {
                return;
            }

            Vector3 pos = posIn == null ? transform.position : (Vector3)posIn;
            Quaternion rot = rotIn == null ? transform.rotation : (Quaternion)rotIn;
            Vector3 scale = scaleIn == null ? transform.localScale : (Vector3)scaleIn;

            if (!CanCommitToTransform)
            {
                if (!m_CachedIsServer)
                {
                    SetStateServerRpc(pos, rot, scale, shouldGhostsInterpolate);
                }
            }
            else
            {
                m_Transform.position = pos;
                m_Transform.rotation = rot;
                m_Transform.localScale = scale;
                m_LocalAuthoritativeNetworkState.IsTeleportingNextFrame = shouldGhostsInterpolate;
            }
        }

        [ServerRpc]
        private void SetStateServerRpc(Vector3 pos, Quaternion rot, Vector3 scale, bool shouldTeleport)
        {
            // server has received this RPC request to move change transform.  Give the server a chance to modify or
            //  even reject the move
            if (OnClientRequestChange != null)
            {
                (pos, rot, scale) = OnClientRequestChange(pos, rot, scale);
            }
            m_Transform.position = pos;
            m_Transform.rotation = rot;
            m_Transform.localScale = scale;
            m_LocalAuthoritativeNetworkState.IsTeleportingNextFrame = shouldTeleport;
        }
        #endregion

        // todo this is currently in update, to be able to catch any transform changes. A FixedUpdate mode could be added to be less intense, but it'd be
        // conditional to users only making transform update changes in FixedUpdate.
        protected virtual void Update()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (CanCommitToTransform)
            {
                if (m_CachedIsServer)
                {
                    TryCommitTransformToServer(m_Transform, m_CachedNetworkManager.LocalTime.Time);
                }

                m_PrevNetworkState = m_LocalAuthoritativeNetworkState;
            }

            // apply interpolated value
            if (m_CachedNetworkManager.IsConnectedClient || m_CachedNetworkManager.IsListening)
            {
                // eventually, we could hoist this calculation so that it happens once for all objects, not once per object
                var cachedDeltaTime = Time.deltaTime;
                var serverTime = NetworkManager.ServerTime;
                var cachedServerTime = serverTime.Time;
                var cachedRenderTime = serverTime.TimeTicksAgo(1).Time;

                foreach (var interpolator in m_AllFloatInterpolators)
                {
                    interpolator.Update(cachedDeltaTime, cachedRenderTime, cachedServerTime);
                }

                m_RotationInterpolator.Update(cachedDeltaTime, cachedRenderTime, cachedServerTime);

                if (!CanCommitToTransform)
                {
#if NGO_TRANSFORM_DEBUG
                    if (m_CachedNetworkManager.LogLevel == LogLevel.Developer)
                    {
                        // TODO: This should be a component gizmo - not some debug draw based on log level
                        var interpolatedPosition = new Vector3(m_PositionXInterpolator.GetInterpolatedValue(), m_PositionYInterpolator.GetInterpolatedValue(), m_PositionZInterpolator.GetInterpolatedValue());
                        Debug.DrawLine(interpolatedPosition, interpolatedPosition + Vector3.up, Color.magenta, k_DebugDrawLineTime, false);

                        // try to update previously consumed NetworkState
                        // if we have any changes, that means made some updates locally
                        // we apply the latest ReplNetworkState again to revert our changes
                        var oldStateDirtyInfo = ApplyTransformToNetworkStateWithInfo(ref m_PrevNetworkState, 0, m_Transform);

                        // there are several bugs in this code, as we the message is dumped out under odd circumstances
                        //  For Matt, it would trigger when an object's rotation was perturbed by colliding with another
                        //  object vs. explicitly rotating it
                        if (oldStateDirtyInfo.isPositionDirty || oldStateDirtyInfo.isScaleDirty || (oldStateDirtyInfo.isRotationDirty && SyncRotAngleX && SyncRotAngleY && SyncRotAngleZ))
                        {
                            // ignoring rotation dirty since quaternions will mess with euler angles, making this impossible to determine if the change to a single axis comes
                            // from an unauthorized transform change or euler to quaternion conversion artifacts.
                            var dirtyField = oldStateDirtyInfo.isPositionDirty ? "position" : oldStateDirtyInfo.isRotationDirty ? "rotation" : "scale";
                            Debug.LogWarning($"A local change to {dirtyField} without authority detected, reverting back to latest interpolated network state!", this);
                        }
                    }
#endif

                    // Apply updated interpolated value
                    ApplyInterpolatedNetworkStateToTransform(m_ReplicatedNetworkState.Value, m_Transform);
                }
            }

            m_LocalAuthoritativeNetworkState.IsTeleportingNextFrame = false;
        }

        /// <summary>
        /// Teleports the transform to the given values without interpolating
        /// </summary>
        public void Teleport(Vector3 newPosition, Quaternion newRotation, Vector3 newScale)
        {
            if (!CanCommitToTransform)
            {
                throw new Exception("Teleport not allowed");
            }

            var newRotationEuler = newRotation.eulerAngles;
            var stateToSend = m_LocalAuthoritativeNetworkState;
            stateToSend.IsTeleportingNextFrame = true;
            stateToSend.Position = newPosition;
            stateToSend.Rotation = newRotationEuler;
            stateToSend.Scale = newScale;
            ApplyInterpolatedNetworkStateToTransform(stateToSend, transform);
            // set teleport flag in state to signal to ghosts not to interpolate
            m_LocalAuthoritativeNetworkState.IsTeleportingNextFrame = true;
            // check server side
            TryCommitValuesToServer(newPosition, newRotationEuler, newScale, m_CachedNetworkManager.LocalTime.Time);
            m_LocalAuthoritativeNetworkState.IsTeleportingNextFrame = false;
        }
    }
}
