using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// A component for syncing transforms
    /// </summary>
    [AddComponentMenu("Netcode/" + nameof(NetworkTransform))]
    [DefaultExecutionOrder(1000)] // this is needed to catch the update time after the transform was updated by user scripts
    public class NetworkTransform : NetworkBehaviour
    {
        public struct NetworkTransformState : INetworkSerializable
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

            // 10-15: <unused>
            private ushort m_Bitset;

            public bool InLocalSpace
            {
                get => (m_Bitset & (1 << k_InLocalSpaceBit)) != 0;
                set
                {
                    if (value) m_Bitset = (ushort) (m_Bitset | (1 << k_InLocalSpaceBit));
                    else m_Bitset = (ushort) (m_Bitset & ~(1 << k_InLocalSpaceBit));
                }
            }

            // Position
            public bool HasPositionX
            {
                get => (m_Bitset & (1 << k_PositionXBit)) != 0;
                set
                {
                    if (value) m_Bitset = (ushort) (m_Bitset | (1 << k_PositionXBit));
                    else m_Bitset = (ushort) (m_Bitset & ~(1 << k_PositionXBit));
                }
            }

            public bool HasPositionY
            {
                get => (m_Bitset & (1 << k_PositionYBit)) != 0;
                set
                {
                    if (value) m_Bitset = (ushort) (m_Bitset | (1 << k_PositionYBit));
                    else m_Bitset = (ushort) (m_Bitset & ~(1 << k_PositionYBit));
                }
            }

            public bool HasPositionZ
            {
                get => (m_Bitset & (1 << k_PositionZBit)) != 0;
                set
                {
                    if (value) m_Bitset = (ushort) (m_Bitset | (1 << k_PositionZBit));
                    else m_Bitset = (ushort) (m_Bitset & ~(1 << k_PositionZBit));
                }
            }

            // RotAngles
            public bool HasRotAngleX
            {
                get => (m_Bitset & (1 << k_RotAngleXBit)) != 0;
                set
                {
                    if (value) m_Bitset = (ushort) (m_Bitset | (1 << k_RotAngleXBit));
                    else m_Bitset = (ushort) (m_Bitset & ~(1 << k_RotAngleXBit));
                }
            }

            public bool HasRotAngleY
            {
                get => (m_Bitset & (1 << k_RotAngleYBit)) != 0;
                set
                {
                    if (value) m_Bitset = (ushort) (m_Bitset | (1 << k_RotAngleYBit));
                    else m_Bitset = (ushort) (m_Bitset & ~(1 << k_RotAngleYBit));
                }
            }

            public bool HasRotAngleZ
            {
                get => (m_Bitset & (1 << k_RotAngleZBit)) != 0;
                set
                {
                    if (value) m_Bitset = (ushort) (m_Bitset | (1 << k_RotAngleZBit));
                    else m_Bitset = (ushort) (m_Bitset & ~(1 << k_RotAngleZBit));
                }
            }

            // Scale
            public bool HasScaleX
            {
                get => (m_Bitset & (1 << k_ScaleXBit)) != 0;
                set
                {
                    if (value) m_Bitset = (ushort) (m_Bitset | (1 << k_ScaleXBit));
                    else m_Bitset = (ushort) (m_Bitset & ~(1 << k_ScaleXBit));
                }
            }

            public bool HasScaleY
            {
                get => (m_Bitset & (1 << k_ScaleYBit)) != 0;
                set
                {
                    if (value) m_Bitset = (ushort) (m_Bitset | (1 << k_ScaleYBit));
                    else m_Bitset = (ushort) (m_Bitset & ~(1 << k_ScaleYBit));
                }
            }

            public bool HasScaleZ
            {
                get => (m_Bitset & (1 << k_ScaleZBit)) != 0;
                set
                {
                    if (value) m_Bitset = (ushort) (m_Bitset | (1 << k_ScaleZBit));
                    else m_Bitset = (ushort) (m_Bitset & ~(1 << k_ScaleZBit));
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

            public void NetworkSerialize(NetworkSerializer serializer)
            {
                serializer.Serialize(ref SentTime);
                // InLocalSpace + HasXXX Bits
                serializer.Serialize(ref m_Bitset);
                // Position Values
                if (HasPositionX)
                {
                    serializer.Serialize(ref PositionX);
                }

                if (HasPositionY)
                {
                    serializer.Serialize(ref PositionY);
                }

                if (HasPositionZ)
                {
                    serializer.Serialize(ref PositionZ);
                }

                // RotAngle Values
                if (HasRotAngleX)
                {
                    serializer.Serialize(ref RotAngleX);
                }

                if (HasRotAngleY)
                {
                    serializer.Serialize(ref RotAngleY);
                }

                if (HasRotAngleZ)
                {
                    serializer.Serialize(ref RotAngleZ);
                }

                // Scale Values
                if (HasScaleX)
                {
                    serializer.Serialize(ref ScaleX);
                }

                if (HasScaleY)
                {
                    serializer.Serialize(ref ScaleY);
                }

                if (HasScaleZ)
                {
                    serializer.Serialize(ref ScaleZ);
                }
            }
        }

        public bool SyncPositionX = true, SyncPositionY = true, SyncPositionZ = true;
        public bool SyncRotAngleX = true, SyncRotAngleY = true, SyncRotAngleZ = true;
        public bool SyncScaleX = true, SyncScaleY = true, SyncScaleZ = true;

        public float PositionThreshold, RotAngleThreshold, ScaleThreshold;

        /// <summary>
        /// Sets whether this transform should sync in local space or in world space.
        /// This is important to set since reparenting this transform could have issues,
        /// if using world position (depending on who gets synced first: the parent or the child)
        /// Having a child always at position 0,0,0 for example will have less possibilities of desync than when using world positions
        /// </summary>
        [Tooltip("Sets whether this transform should sync in local space or in world space")]
        public bool InLocalSpace = false;

        public bool Interpolate = true;

        protected virtual bool CanWriteToTransform => IsServer;

        private readonly NetworkVariable<NetworkTransformState> m_ReplicatedNetworkState = new NetworkVariable<NetworkTransformState>(new NetworkTransformState());

        protected NetworkTransformState m_LocalAuthoritativeNetworkState;

        private NetworkTransformState m_PrevNetworkState;

        private const int k_DebugDrawLineTime = 10;

        private bool m_HasSentLastValue = false; // used to send one last value, so clients can make the difference between lost replication data (clients extrapolate) and no more data to send.

        private BufferedLinearInterpolator<float> m_PositionXInterpolator = new BufferedLinearInterpolatorFloat();
        private BufferedLinearInterpolator<float> m_PositionYInterpolator = new BufferedLinearInterpolatorFloat();
        private BufferedLinearInterpolator<float> m_PositionZInterpolator = new BufferedLinearInterpolatorFloat();
        private BufferedLinearInterpolator<Quaternion> m_RotationInterpolator = new BufferedLinearInterpolatorQuaternion(); // rotation is a single Quaternion since each euler axis will affect the quaternion's final value
        private BufferedLinearInterpolator<float> m_ScaleXInterpolator = new BufferedLinearInterpolatorFloat();
        private BufferedLinearInterpolator<float> m_ScaleYInterpolator = new BufferedLinearInterpolatorFloat();
        private BufferedLinearInterpolator<float> m_ScaleZInterpolator = new BufferedLinearInterpolatorFloat();

        private readonly List<BufferedLinearInterpolator<float>> m_AllFloatInterpolators = new List<BufferedLinearInterpolator<float>>(6);

        private Transform m_Transform; // cache the transform component to reduce unnecessary bounce between managed and native

        public void ResetInterpolatedStateToCurrentNetworkState()
        {
            m_PositionXInterpolator.ResetTo(m_ReplicatedNetworkState.Value.PositionX);
            m_PositionYInterpolator.ResetTo(m_ReplicatedNetworkState.Value.PositionY);
            m_PositionZInterpolator.ResetTo(m_ReplicatedNetworkState.Value.PositionZ);

            m_RotationInterpolator.ResetTo(Quaternion.Euler(m_ReplicatedNetworkState.Value.Rotation));

            m_ScaleXInterpolator.ResetTo(m_ReplicatedNetworkState.Value.ScaleX);
            m_ScaleYInterpolator.ResetTo(m_ReplicatedNetworkState.Value.ScaleY);
            m_ScaleZInterpolator.ResetTo(m_ReplicatedNetworkState.Value.ScaleZ);
        }

        // updates `NetworkState` properties if they need to and returns a `bool` indicating whether or not there was any changes made
        // returned boolean would be useful to change encapsulating `NetworkVariable<NetworkState>`'s dirty state, e.g. ReplNetworkState.SetDirty(isDirty);
        protected bool UpdateNetworkStateWithTransform(ref NetworkTransformState networkState, double dirtyTime)
        {
            return UpdateNetworkStateCheckDirtyWithInfo(ref networkState, dirtyTime).isDirty;
        }

        private (bool isDirty, bool isPositionDirty, bool isRotationDirty, bool isScaleDirty) UpdateNetworkStateCheckDirtyWithInfo(ref NetworkTransformState networkState, double dirtyTime)
        {
            var position = InLocalSpace ? m_Transform.localPosition : m_Transform.position;
            var rotAngles = InLocalSpace ? m_Transform.localEulerAngles : m_Transform.eulerAngles;
            var scale = InLocalSpace ? m_Transform.localScale : m_Transform.lossyScale;

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

            if (SyncPositionX &&
                Mathf.Abs(networkState.PositionX - position.x) >= PositionThreshold &&
                !Mathf.Approximately(networkState.PositionX, position.x))
            {
                networkState.PositionX = position.x;
                networkState.HasPositionX = true;
                isPositionDirty = true;
            }

            if (SyncPositionY &&
                Mathf.Abs(networkState.PositionY - position.y) >= PositionThreshold &&
                !Mathf.Approximately(networkState.PositionY, position.y))
            {
                networkState.PositionY = position.y;
                networkState.HasPositionY = true;
                isPositionDirty = true;
            }

            if (SyncPositionZ &&
                Mathf.Abs(networkState.PositionZ - position.z) >= PositionThreshold &&
                !Mathf.Approximately(networkState.PositionZ, position.z))
            {
                networkState.PositionZ = position.z;
                networkState.HasPositionZ = true;
                isPositionDirty = true;
            }

            if (SyncRotAngleX &&
                Mathf.Abs(networkState.RotAngleX - rotAngles.x) >= RotAngleThreshold &&
                !Mathf.Approximately(networkState.RotAngleX, rotAngles.x))
            {
                networkState.RotAngleX = rotAngles.x;
                networkState.HasRotAngleX = true;
                isRotationDirty = true;
            }

            if (SyncRotAngleY &&
                Mathf.Abs(networkState.RotAngleY - rotAngles.y) >= RotAngleThreshold &&
                !Mathf.Approximately(networkState.RotAngleY, rotAngles.y))
            {
                networkState.RotAngleY = rotAngles.y;
                networkState.HasRotAngleY = true;
                isRotationDirty = true;
            }

            if (SyncRotAngleZ &&
                Mathf.Abs(networkState.RotAngleZ - rotAngles.z) >= RotAngleThreshold &&
                !Mathf.Approximately(networkState.RotAngleZ, rotAngles.z))
            {
                networkState.RotAngleZ = rotAngles.z;
                networkState.HasRotAngleZ = true;
                isRotationDirty = true;
            }

            if (SyncScaleX &&
                Mathf.Abs(networkState.ScaleX - scale.x) >= ScaleThreshold &&
                !Mathf.Approximately(networkState.ScaleX, scale.x))
            {
                networkState.ScaleX = scale.x;
                networkState.HasScaleX = true;
                isScaleDirty = true;
            }

            if (SyncScaleY &&
                Mathf.Abs(networkState.ScaleY - scale.y) >= ScaleThreshold &&
                !Mathf.Approximately(networkState.ScaleY, scale.y))
            {
                networkState.ScaleY = scale.y;
                networkState.HasScaleY = true;
                isScaleDirty = true;
            }

            if (SyncScaleZ &&
                Mathf.Abs(networkState.ScaleZ - scale.z) >= ScaleThreshold &&
                !Mathf.Approximately(networkState.ScaleZ, scale.z))
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

        private void ApplyNetworkStateFromAuthority(NetworkTransformState networkState)
        {
            m_PrevNetworkState = networkState;

            var interpolatedPosition = InLocalSpace ? m_Transform.localPosition : m_Transform.position;
            var interpolatedRotAngles = InLocalSpace ? m_Transform.localEulerAngles : m_Transform.eulerAngles;
            var interpolatedScale = InLocalSpace ? m_Transform.localScale : m_Transform.lossyScale;

            // InLocalSpace Read
            InLocalSpace = networkState.InLocalSpace;
            // Position Read
            if (SyncPositionX)
            {
                interpolatedPosition.x = Interpolate ? m_PositionXInterpolator.GetInterpolatedValue() : networkState.Position.x;
            }

            if (SyncPositionY)
            {
                interpolatedPosition.y = Interpolate ? m_PositionYInterpolator.GetInterpolatedValue() : networkState.Position.y;
            }

            if (SyncPositionZ)
            {
                interpolatedPosition.z = Interpolate ? m_PositionZInterpolator.GetInterpolatedValue() : networkState.Position.z;
            }

            if (SyncRotAngleX)
            {
                interpolatedRotAngles.x = Interpolate ? m_RotationInterpolator.GetInterpolatedValue().eulerAngles.x : networkState.Rotation.x;
            }

            if (SyncRotAngleY)
            {
                interpolatedRotAngles.y = Interpolate ? m_RotationInterpolator.GetInterpolatedValue().eulerAngles.y : networkState.Rotation.y;
            }

            if (SyncRotAngleZ)
            {
                interpolatedRotAngles.z = Interpolate ? m_RotationInterpolator.GetInterpolatedValue().eulerAngles.z : networkState.Rotation.z;
            }

            // Scale Read
            if (SyncScaleX)
            {
                interpolatedScale.x = Interpolate ? m_ScaleXInterpolator.GetInterpolatedValue() : networkState.Scale.x;
            }

            if (SyncScaleY)
            {
                interpolatedScale.y = Interpolate ? m_ScaleYInterpolator.GetInterpolatedValue() : networkState.Scale.y;
            }

            if (SyncScaleZ)
            {
                interpolatedScale.z = Interpolate ? m_ScaleZInterpolator.GetInterpolatedValue() : networkState.Scale.z;
            }

            // Position Apply
            if (SyncPositionX || SyncPositionY || SyncPositionZ)
            {
                if (InLocalSpace)
                {
                    m_Transform.localPosition = interpolatedPosition;
                }
                else
                {
                    m_Transform.position = interpolatedPosition;
                }

                m_PrevNetworkState.Position = interpolatedPosition;
            }

            // RotAngles Apply
            if (SyncRotAngleX || SyncRotAngleY || SyncRotAngleZ)
            {
                if (InLocalSpace)
                {
                    m_Transform.localRotation = Quaternion.Euler(interpolatedRotAngles);
                }
                else
                {
                    m_Transform.rotation = Quaternion.Euler(interpolatedRotAngles);
                }

                m_PrevNetworkState.Rotation = interpolatedRotAngles;
            }

            // Scale Apply
            if (SyncScaleX || SyncScaleY || SyncScaleZ)
            {
                if (InLocalSpace)
                {
                    m_Transform.localScale = interpolatedScale;
                }
                else
                {
                    m_Transform.localScale = Vector3.one;
                    var lossyScale = m_Transform.lossyScale;
                    // todo this conversion is messing with interpolation. local scale interpolates fine, lossy scale is jittery. must investigate. MTT-1208
                    m_Transform.localScale = new Vector3(interpolatedScale.x / lossyScale.x, interpolatedScale.y / lossyScale.y, interpolatedScale.z / lossyScale.z);
                }

                m_PrevNetworkState.Scale = interpolatedScale;
            }
        }

        protected void AddInterpolatedState(NetworkTransformState newState)
        {
            var sentTime = new NetworkTime(NetworkManager.Singleton.ServerTime.TickRate, newState.SentTime);

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

            if (CanWriteToTransform)
            {
                // we're the authority, we ignore incoming changes
                return;
            }

            AddInterpolatedState(newState);

            if (NetworkManager.Singleton.LogLevel == LogLevel.Developer)
            {
                var pos = new Vector3(newState.PositionX, newState.PositionY, newState.PositionZ);
                Debug.DrawLine(pos, pos + Vector3.up + Vector3.left * Random.Range(0.5f, 2f), Color.green, k_DebugDrawLineTime, false);
            }
        }

        private void Awake()
        {
            m_Transform = transform;

            if (m_AllFloatInterpolators.Count == 0)
            {
                m_AllFloatInterpolators.Add(m_PositionXInterpolator);
                m_AllFloatInterpolators.Add(m_PositionYInterpolator);
                m_AllFloatInterpolators.Add(m_PositionZInterpolator);
                m_AllFloatInterpolators.Add(m_ScaleXInterpolator);
                m_AllFloatInterpolators.Add(m_ScaleYInterpolator);
                m_AllFloatInterpolators.Add(m_ScaleZInterpolator);
            }

            // ReplNetworkState.NetworkVariableChannel = NetworkChannel.PositionUpdate; // todo figure this out, talk with Matt/Fatih, this should be unreliable

            // set initial value for spawn
            if (CanWriteToTransform)
            {
                var isDirty = UpdateNetworkStateWithTransform(ref m_LocalAuthoritativeNetworkState, NetworkManager.LocalTime.Time);
                SendToGhosts(m_LocalAuthoritativeNetworkState, isDirty);
            }

            m_ReplicatedNetworkState.OnValueChanged += OnNetworkStateChanged;
        }

        public override void OnNetworkSpawn()
        {
            Initialize();
        }

        public override void OnGainedOwnership()
        {
            Initialize();
        }

        private void Initialize()
        {
            ResetInterpolatedStateToCurrentNetworkState(); // useful for late joining

            if (CanWriteToTransform)
            {
                m_ReplicatedNetworkState.SetDirty(true);
            }
            else
            {
                ApplyNetworkStateFromAuthority(m_ReplicatedNetworkState.Value);
            }
        }

        private void OnDestroy()
        {
            m_ReplicatedNetworkState.OnValueChanged -= OnNetworkStateChanged;
        }

        protected void SendToGhosts(NetworkTransformState newState, bool isDirty, Action sendToGhosts = null)
        {
            if (CanWriteToTransform)
            {
                // if dirty, send
                // if not dirty anymore, but hasn't sent last value for extrapolation, still set isDirty
                // if not dirty and has already sent last value, don't do anything
                void Send()
                {
                    m_PrevNetworkState = newState;
                    m_ReplicatedNetworkState.Value = newState;
                    m_ReplicatedNetworkState.SetDirty(true);
                    AddInterpolatedState(newState);
                }

                if (isDirty)
                {
                    if (sendToGhosts != null)
                    {
                        sendToGhosts.Invoke();
                    }
                    else
                    {
                        Send();
                    }

                    m_HasSentLastValue = false;
                }
                else if (!m_HasSentLastValue && !m_ReplicatedNetworkState.IsDirty()) // check for state.IsDirty since update can happen more than once per tick
                {
                    newState.SentTime = NetworkManager.LocalTime.Time; // time one tick later
                    if (sendToGhosts != null)
                    {
                        sendToGhosts.Invoke();
                    }
                    else
                    {
                        Send();
                    }

                    m_HasSentLastValue = true;
                }
            }
        }

        // todo this is currently in update, to be able to catch any transform changes. A FixedUpdate mode could be added to be less intense, but it'd be
        // conditional to users only making transform update changes in FixedUpdate.
        protected virtual void Update()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (CanWriteToTransform && IsServer)
            {
                var isDirty = UpdateNetworkStateWithTransform(ref m_LocalAuthoritativeNetworkState, NetworkManager.LocalTime.Time);
                SendToGhosts(m_LocalAuthoritativeNetworkState, isDirty);
            }

            // apply interpolated value
            if ((NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening))
            {
                foreach (var interpolator in m_AllFloatInterpolators)
                {
                    interpolator.Update(Time.deltaTime);
                }

                m_RotationInterpolator.Update(Time.deltaTime);

                if (!CanWriteToTransform)
                {
                    if (NetworkManager.Singleton.LogLevel == LogLevel.Developer)
                    {
                        var interpolatedPosition = new Vector3(m_PositionXInterpolator.GetInterpolatedValue(), m_PositionYInterpolator.GetInterpolatedValue(), m_PositionZInterpolator.GetInterpolatedValue());
                        Debug.DrawLine(interpolatedPosition, interpolatedPosition + Vector3.up, Color.magenta, k_DebugDrawLineTime, false);
                    }

                    // try to update previously consumed NetworkState
                    // if we have any changes, that means made some updates locally
                    // we apply the latest ReplNetworkState again to revert our changes
                    var oldStateDirtyInfo = UpdateNetworkStateCheckDirtyWithInfo(ref m_PrevNetworkState, 0);
                    if (oldStateDirtyInfo.isPositionDirty || oldStateDirtyInfo.isScaleDirty || (oldStateDirtyInfo.isRotationDirty && SyncRotAngleX && SyncRotAngleY && SyncRotAngleZ))
                    {
                        // ignoring rotation dirty since quaternions will mess with euler angles, making this impossible to determine if the change to a single axis comes
                        // from an unauthorized transform change or euler to quaternion conversion artifacts.
                        var dirtyField = oldStateDirtyInfo.isPositionDirty ? "position" : oldStateDirtyInfo.isRotationDirty ? "rotation" : "scale";
                        Debug.LogWarning($"A local change to {dirtyField} without authority detected, reverting back to latest interpolated network state!", this);
                    }

                    // Apply updated interpolated value
                    ApplyNetworkStateFromAuthority(m_ReplicatedNetworkState.Value);
                }
            }
        }

        /// <summary>
        /// Teleports the transform to the given values without interpolating
        /// </summary>
        public void Teleport(Vector3 newPosition, Quaternion newRotation, Vector3 newScale)
        {
            // check server side
            // set teleport flag in state
            throw new NotImplementedException(); // TODO MTT-769
        }
    }
}