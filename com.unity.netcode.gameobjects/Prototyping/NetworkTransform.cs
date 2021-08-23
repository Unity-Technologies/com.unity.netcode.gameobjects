using System;
using UnityEngine;

namespace Unity.Netcode.Prototyping
{
    /// <summary>
    /// A prototype component for syncing transforms
    /// </summary>
    [AddComponentMenu("Netcode/" + nameof(NetworkTransform))]
    [DefaultExecutionOrder(1000000)] // this is needed to catch the update time after the transform was updated by user scripts
    public class NetworkTransform : NetworkBehaviour
    {
        internal struct NetworkState : INetworkSerializable
        {
            internal const int InLocalSpaceBit = 0;
            internal const int PositionXBit = 1;
            internal const int PositionYBit = 2;
            internal const int PositionZBit = 3;
            internal const int RotAngleXBit = 4;
            internal const int RotAngleYBit = 5;
            internal const int RotAngleZBit = 6;
            internal const int ScaleXBit = 7;
            internal const int ScaleYBit = 8;
            internal const int ScaleZBit = 9;
            // 10-15: <unused>
            public ushort Bitset;

            public bool InLocalSpace
            {
                get => (Bitset & (1 << InLocalSpaceBit)) != 0;
                set => Bitset |= (ushort)((value ? 1 : 0) << InLocalSpaceBit);
            }
            // Position
            public bool HasPositionX
            {
                get => (Bitset & (1 << PositionXBit)) != 0;
                set => Bitset |= (ushort)((value ? 1 : 0) << PositionXBit);
            }
            public bool HasPositionY
            {
                get => (Bitset & (1 << PositionYBit)) != 0;
                set => Bitset |= (ushort)((value ? 1 : 0) << PositionYBit);
            }
            public bool HasPositionZ
            {
                get => (Bitset & (1 << PositionZBit)) != 0;
                set => Bitset |= (ushort)((value ? 1 : 0) << PositionZBit);
            }
            // RotAngles
            public bool HasRotAngleX
            {
                get => (Bitset & (1 << RotAngleXBit)) != 0;
                set => Bitset |= (ushort)((value ? 1 : 0) << RotAngleXBit);
            }
            public bool HasRotAngleY
            {
                get => (Bitset & (1 << RotAngleYBit)) != 0;
                set => Bitset |= (ushort)((value ? 1 : 0) << RotAngleYBit);
            }
            public bool HasRotAngleZ
            {
                get => (Bitset & (1 << RotAngleZBit)) != 0;
                set => Bitset |= (ushort)((value ? 1 : 0) << RotAngleZBit);
            }
            // Scale
            public bool HasScaleX
            {
                get => (Bitset & (1 << ScaleXBit)) != 0;
                set => Bitset |= (ushort)((value ? 1 : 0) << ScaleXBit);
            }
            public bool HasScaleY
            {
                get => (Bitset & (1 << ScaleYBit)) != 0;
                set => Bitset |= (ushort)((value ? 1 : 0) << ScaleYBit);
            }
            public bool HasScaleZ
            {
                get => (Bitset & (1 << ScaleZBit)) != 0;
                set => Bitset |= (ushort)((value ? 1 : 0) << ScaleZBit);
            }

            public float PositionX, PositionY, PositionZ;
            public float RotAngleX, RotAngleY, RotAngleZ;
            public float ScaleX, ScaleY, ScaleZ;
			public double SentTime;

            public void NetworkSerialize(NetworkSerializer serializer)
            {
                serializer.Serialize(ref SentTime);
                // InLocalSpace + HasXXX Bits
                serializer.Serialize(ref Bitset);
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

        /// <summary>
        /// The network channel to use send updates
        /// </summary>
        [Tooltip("The network channel to use send updates")]
        private NetworkChannel Channel = NetworkChannel.PositionUpdate;

        /// <summary>
        /// Sets whether this transform should sync in local space or in world space.
        /// This is important to set since reparenting this transform could have issues,
        /// if using world position (depending on who gets synced first: the parent or the child)
        /// Having a child always at position 0,0,0 for example will have less possibilities of desync than when using world positions
        /// </summary>
        [Tooltip("Sets whether this transform should sync in local space or in world space")]
        public bool InLocalSpace = false;

        public bool SyncPositionX = true, SyncPositionY = true, SyncPositionZ = true;
        public bool SyncRotAngleX = true, SyncRotAngleY = true, SyncRotAngleZ = true;
        public bool SyncScaleX = true, SyncScaleY = true, SyncScaleZ = true;

        public float PositionThreshold, RotAngleThreshold, ScaleThreshold;

        /// <summary>
        /// The base amount of sends per seconds to use when range is disabled
        /// </summary>
        [SerializeField, Range(0, 120), Tooltip("The base amount of sends per seconds to use when range is disabled")]
        public float FixedSendsPerSecond = 30f;

        public virtual IInterpolator<Vector3> PositionInterpolator { get; set; }
        public virtual IInterpolator<Quaternion> RotationInterpolator { get; set; }
        public virtual IInterpolator<Vector3> ScaleInterpolator { get; set; }

        private Transform m_Transform; // cache the transform component to reduce unnecessary bounce between managed and native

        private Vector3 TransformPosition
        {
            get
            {
                if (InLocalSpace)
                {
                    return m_Transform.localPosition;
                }
                else
                {
                    return m_Transform.position;
                }
            }
        }

        private Quaternion TransformRotation
        {
            get
            {
                if (InLocalSpace)
                {
                    return m_Transform.localRotation;
                }
                else
                {
                    return m_Transform.rotation;
                }
            }
        }

        private Vector3 TransformScale
        {
            get
            {
                if (InLocalSpace)
                {
                    return m_Transform.localScale;
                }
                else
                {
                    return m_Transform.lossyScale;
                }
            }
        }

        internal readonly NetworkVariable<NetworkState> ReplNetworkState = new NetworkVariable<NetworkState>(new NetworkState());
        internal NetworkState PrevNetworkState;

        // updates `NetworkState` properties if they need to and returns a `bool` indicating whether or not there was any changes made
        // returned boolean would be useful to change encapsulating `NetworkVariable<NetworkState>`'s dirty state, e.g. ReplNetworkState.SetDirty(isDirty);
        internal bool UpdateNetworkState(ref NetworkState networkState)
        {
            var position = InLocalSpace ? m_Transform.localPosition : m_Transform.position;
            var rotAngles = InLocalSpace ? m_Transform.localEulerAngles : m_Transform.eulerAngles;
            var scale = InLocalSpace ? m_Transform.localScale : m_Transform.lossyScale;

            bool isDirty = false;

            if (InLocalSpace != networkState.InLocalSpace)
            {
                networkState.InLocalSpace = InLocalSpace;
                isDirty |= true;
            }

            if (SyncPositionX &&
                Mathf.Abs(networkState.PositionX - position.x) >= PositionThreshold &&
                !Mathf.Approximately(networkState.PositionX, position.x))
            {
                networkState.PositionX = position.x;
                networkState.HasPositionX = true;
                isDirty |= true;
            }

            if (SyncPositionY &&
                Mathf.Abs(networkState.PositionY - position.y) >= PositionThreshold &&
                !Mathf.Approximately(networkState.PositionY, position.y))
            {
                networkState.PositionY = position.y;
                networkState.HasPositionY = true;
                isDirty |= true;
            }

            if (SyncPositionZ &&
                Mathf.Abs(networkState.PositionZ - position.z) >= PositionThreshold &&
                !Mathf.Approximately(networkState.PositionZ, position.z))
            {
                networkState.PositionZ = position.z;
                networkState.HasPositionZ = true;
                isDirty |= true;
            }

            if (SyncRotAngleX &&
                Mathf.Abs(networkState.RotAngleX - rotAngles.x) >= RotAngleThreshold &&
                !Mathf.Approximately(networkState.RotAngleX, rotAngles.x))
            {
                networkState.RotAngleX = rotAngles.x;
                networkState.HasRotAngleX = true;
                isDirty |= true;
            }

            if (SyncRotAngleY &&
                Mathf.Abs(networkState.RotAngleY - rotAngles.y) >= RotAngleThreshold &&
                !Mathf.Approximately(networkState.RotAngleY, rotAngles.y))
            {
                networkState.RotAngleY = rotAngles.y;
                networkState.HasRotAngleY = true;
                isDirty |= true;
            }

            if (SyncRotAngleZ &&
                Mathf.Abs(networkState.RotAngleZ - rotAngles.z) >= RotAngleThreshold &&
                !Mathf.Approximately(networkState.RotAngleZ, rotAngles.z))
            {
                networkState.RotAngleZ = rotAngles.z;
                networkState.HasRotAngleZ = true;
                isDirty |= true;
            }

            if (SyncScaleX &&
                Mathf.Abs(networkState.ScaleX - scale.x) >= ScaleThreshold &&
                !Mathf.Approximately(networkState.ScaleX, scale.x))
            {
                networkState.ScaleX = scale.x;
                networkState.HasScaleX = true;
                isDirty |= true;
            }

            if (SyncScaleY &&
                Mathf.Abs(networkState.ScaleY - scale.y) >= ScaleThreshold &&
                !Mathf.Approximately(networkState.ScaleY, scale.y))
            {
                networkState.ScaleY = scale.y;
                networkState.HasScaleY = true;
                isDirty |= true;
            }

            if (SyncScaleZ &&
                Mathf.Abs(networkState.ScaleZ - scale.z) >= ScaleThreshold &&
                !Mathf.Approximately(networkState.ScaleZ, scale.z))
            {
                networkState.ScaleZ = scale.z;
                networkState.HasScaleZ = true;
                isDirty |= true;
            }

            return isDirty;
        }

        // TODO: temporary! the function body below probably needs to be rewritten later with interpolation in mind
        internal void ApplyNetworkState(NetworkState networkState)
        {
            PrevNetworkState = networkState;

            var position = InLocalSpace ? m_Transform.localPosition : m_Transform.position;
            var rotAngles = InLocalSpace ? m_Transform.localEulerAngles : m_Transform.eulerAngles;
            var scale = InLocalSpace ? m_Transform.localScale : m_Transform.lossyScale;

            // InLocalSpace Read
            InLocalSpace = networkState.InLocalSpace;
            // Position Read
            if (networkState.HasPositionX)
            {
                position.x = networkState.PositionX;
            }
            if (networkState.HasPositionY)
            {
                position.y = networkState.PositionY;
            }
            if (networkState.HasPositionZ)
            {
                position.z = networkState.PositionZ;
            }
            // RotAngles Read
            if (networkState.HasRotAngleX)
            {
                rotAngles.x = networkState.RotAngleX;
            }
            if (networkState.HasRotAngleY)
            {
                rotAngles.y = networkState.RotAngleY;
            }
            if (networkState.HasRotAngleZ)
            {
                rotAngles.z = networkState.RotAngleZ;
            }
            // Scale Read
            if (networkState.HasScaleX)
            {
                scale.x = networkState.ScaleX;
            }
            if (networkState.HasScaleY)
            {
                scale.y = networkState.ScaleY;
            }
            if (networkState.HasScaleZ)
            {
                scale.z = networkState.ScaleZ;
            }

            // Position Apply
            if (networkState.HasPositionX || networkState.HasPositionY || networkState.HasPositionZ)
            {
                if (InLocalSpace)
                {
                    m_Transform.localPosition = position;
                }
                else
                {
                    m_Transform.position = position;
                }
            }
            // RotAngles Apply
            if (networkState.HasRotAngleX || networkState.HasRotAngleY || networkState.HasRotAngleZ)
            {
                if (InLocalSpace)
                {
                    m_Transform.localEulerAngles = rotAngles;
                }
                else
                {
                    m_Transform.eulerAngles = rotAngles;
                }
            }
            // Scale Apply
            if (networkState.HasScaleX || networkState.HasScaleY || networkState.HasScaleZ)
            {
                if (InLocalSpace)
                {
                    m_Transform.localScale = scale;
                }
                else
                {
                    m_Transform.localScale = Vector3.one;
                    var lossyScale = m_Transform.lossyScale;
                    m_Transform.localScale = new Vector3(networkState.ScaleX / lossyScale.x, networkState.ScaleY / lossyScale.y, networkState.ScaleZ / lossyScale.z);
                }
            }
        }
        private bool IsGhostStateDirty(NetworkState networkState)
        {
            if (networkState == null)
            {
                return false;
            }

            bool isDirty = false;

            isDirty |= networkState.InLocalSpace != InLocalSpace;
            if (InLocalSpace)
            {
                isDirty |= networkState.Position != PositionInterpolator.GetInterpolatedValue();
                isDirty |= networkState.Rotation != RotationInterpolator.GetInterpolatedValue();
                isDirty |= networkState.Scale != ScaleInterpolator.GetInterpolatedValue();
            }
            else
            {
                isDirty |= networkState.Position != PositionInterpolator.GetInterpolatedValue();
                isDirty |= networkState.Rotation != RotationInterpolator.GetInterpolatedValue();
                isDirty |= networkState.Scale != ScaleInterpolator.GetInterpolatedValue();
            }

            return isDirty;
        }

        // Is the non-interpolated authoritative state dirty?
        private bool IsAuthoritativeTransformDirty()
        {
            bool isDirty = false;
            var networkState = m_NetworkState.Value;
            isDirty |= networkState.InLocalSpace != InLocalSpace;
            if (InLocalSpace)
            {
                isDirty |= networkState.Position != m_Transform.localPosition;
                isDirty |= networkState.Rotation != m_Transform.localRotation;
                isDirty |= networkState.Scale != m_Transform.localScale;
            }
            else
            {
                isDirty |= networkState.Position != m_Transform.position;
                isDirty |= networkState.Rotation != m_Transform.rotation;
                isDirty |= networkState.Scale != m_Transform.lossyScale;
            }

            return isDirty;
        }

        private void SetNetworkStateDirtyToGhosts(double dirtyTime)
        {
            m_NetworkState.Value.InLocalSpace = InLocalSpace;
            m_NetworkState.Value.SentTime = dirtyTime;
            if (InLocalSpace)
            {
                m_NetworkState.Value.Position = m_Transform.localPosition;
                m_NetworkState.Value.Rotation = m_Transform.localRotation;
                m_NetworkState.Value.Scale = m_Transform.localScale;
            }
            else
            {
                m_NetworkState.Value.Position = m_Transform.position;
                m_NetworkState.Value.Rotation = m_Transform.rotation;
                m_NetworkState.Value.Scale = m_Transform.lossyScale;
            }

            m_NetworkState.SetDirty(true);
        }

        private void ApplyInterpolatedStateFromAuthority(NetworkState netState)
        {
            InLocalSpace = netState.InLocalSpace;
            if (InLocalSpace)
            {
                m_Transform.localPosition = PositionInterpolator.GetInterpolatedValue();
                m_Transform.localRotation = RotationInterpolator.GetInterpolatedValue();
                m_Transform.localScale = ScaleInterpolator.GetInterpolatedValue();
            }
            else
            {
                m_Transform.position = PositionInterpolator.GetInterpolatedValue();
                m_Transform.rotation = RotationInterpolator.GetInterpolatedValue();
                m_Transform.localScale = Vector3.one;
                var lossyScale = m_Transform.lossyScale;
                m_Transform.localScale = new Vector3(ScaleInterpolator.GetInterpolatedValue().x / lossyScale.x, ScaleInterpolator.GetInterpolatedValue().y / lossyScale.y, ScaleInterpolator.GetInterpolatedValue().z / lossyScale.z);
            }

            m_PrevNetworkState = netState;
            m_PrevNetworkState.Position = PositionInterpolator.GetInterpolatedValue();
            m_PrevNetworkState.Rotation = RotationInterpolator.GetInterpolatedValue();
            m_PrevNetworkState.Scale = ScaleInterpolator.GetInterpolatedValue();
        }
        private void OnNetworkStateChanged(NetworkState oldState, NetworkState newState)
        {
            if (!NetworkObject.IsSpawned)
            {
                // todo MTT-849 should never happen but yet it does! maybe revisit/dig after NetVar updates and snapshot system lands?
                return;
            }

            // todo for teleport, check teleport flag
            // if (newState.Teleporting)
            // {
            //     PositionInterpolator.Reset(newState.Position, new NetworkTime(NetworkManager.Singleton.ServerTime.TickRate, newState.SentTick));
            // }

            var sentTime = new NetworkTime(NetworkManager.Singleton.ServerTime.TickRate, newState.SentTime);
            PositionInterpolator.AddMeasurement(newState.Position, sentTime);
            RotationInterpolator.AddMeasurement(newState.Rotation, sentTime);
            ScaleInterpolator.AddMeasurement(newState.Scale, sentTime);
        }

        private void Awake()
        {
            m_Transform = transform;
            PositionInterpolator = new BufferedLinearInterpolatorVector3(TransformPosition);
            RotationInterpolator = new BufferedLinearInterpolatorQuaternion(TransformRotation);
            ScaleInterpolator = new BufferedLinearInterpolatorVector3(TransformScale);

            ReplNetworkState.Settings.SendNetworkChannel = Channel;
            ReplNetworkState.Settings.SendTickrate = FixedSendsPerSecond;

            ReplNetworkState.OnValueChanged += OnNetworkStateChanged;
        }

        public void Start()
        {
            PositionInterpolator.Start();
            RotationInterpolator.Start();
            ScaleInterpolator.Start();
        }

        public void OnEnable()
        {
            PositionInterpolator.OnEnable();
            RotationInterpolator.OnEnable();
            ScaleInterpolator.OnEnable();
        }

        public override void OnNetworkSpawn()
        {
            m_PrevNetworkState = null;

            var currentTime = new NetworkTime(NetworkManager.Singleton.ServerTime.TickRate, m_NetworkState.Value.SentTime);

            PositionInterpolator.OnNetworkSpawn();
            RotationInterpolator.OnNetworkSpawn();
            ScaleInterpolator.OnNetworkSpawn();
        }
        private void OnDestroy()
        {
            ReplNetworkState.OnValueChanged -= OnNetworkStateChanged;

            PositionInterpolator.OnDestroy();
            RotationInterpolator.OnDestroy();
            ScaleInterpolator.OnDestroy();
        }
		
        private void FixedUpdate()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (IsServer)
            {
                ReplNetworkState.SetDirty(UpdateNetworkState(ref ReplNetworkState.ValueRef));
            }
            // try to update previously consumed NetworkState
            // if we have any changes, that means made some updates locally
            // we apply the latest ReplNetworkState again to revert our changes
            else if (UpdateNetworkState(ref PrevNetworkState))
            {
                ApplyNetworkState(ReplNetworkState.Value);
            }
        }

        private void FixedUpdate()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (!CanUpdateTransform)
            {
                if (IsGhostStateDirty(m_PrevNetworkState))
                {
                    Debug.LogWarning("A local change without authority detected, revert back to latest network state!", this);
                    ApplyInterpolatedStateFromAuthority(m_NetworkState.Value);
                }

                PositionInterpolator.FixedUpdate(NetworkManager.ServerTime.FixedDeltaTime);
                RotationInterpolator.FixedUpdate(NetworkManager.ServerTime.FixedDeltaTime);
                ScaleInterpolator.FixedUpdate(NetworkManager.ServerTime.FixedDeltaTime);
            }
        }

        private void Update()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (CanUpdateTransform)
            {
                if (IsAuthoritativeTransformDirty())
                {
                    // check for time there was a change to the transform
                    // this needs to be done in Update to catch that time change as soon as it happens.
                    SetNetworkStateDirtyToGhosts(NetworkManager.LocalTime.Time);
                }
            }
            else if (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening)
            {
                PositionInterpolator.Update(Time.deltaTime);
                RotationInterpolator.Update(Time.deltaTime);
                ScaleInterpolator.Update(Time.deltaTime);
                ApplyInterpolatedStateFromAuthority(m_NetworkState.Value);
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