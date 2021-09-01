using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Unity.Netcode.Prototyping
{
    /// <summary>
    /// A prototype component for syncing transforms
    /// </summary>
    [AddComponentMenu("Netcode/" + nameof(NetworkTransform))]
// todo add a note in doc about this
    // todo have a way for this to be only server side? This way client side you can have scripts that depend on that position update that'll execute afterward
    [DefaultExecutionOrder(1000)] // this is needed to catch the update time after the transform was updated by user scripts
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
            internal const int LastSentBit = 10; // todo remove this

            // 11-15: <unused>
            public ushort Bitset;

            public bool InLocalSpace
            {
                get => (Bitset & (1 << InLocalSpaceBit)) != 0;
                set => Bitset |= (ushort) ((value ? 1 : 0) << InLocalSpaceBit);
            }

            // Position
            public bool HasPositionX
            {
                get => (Bitset & (1 << PositionXBit)) != 0;
                set => Bitset |= (ushort) ((value ? 1 : 0) << PositionXBit);
            }

            public bool HasPositionY
            {
                get => (Bitset & (1 << PositionYBit)) != 0;
                set => Bitset |= (ushort) ((value ? 1 : 0) << PositionYBit);
            }

            public bool HasPositionZ
            {
                get => (Bitset & (1 << PositionZBit)) != 0;
                set => Bitset |= (ushort) ((value ? 1 : 0) << PositionZBit);
            }

            // RotAngles
            public bool HasRotAngleX
            {
                get => (Bitset & (1 << RotAngleXBit)) != 0;
                set => Bitset |= (ushort) ((value ? 1 : 0) << RotAngleXBit);
            }

            public bool HasRotAngleY
            {
                get => (Bitset & (1 << RotAngleYBit)) != 0;
                set => Bitset |= (ushort) ((value ? 1 : 0) << RotAngleYBit);
            }

            public bool HasRotAngleZ
            {
                get => (Bitset & (1 << RotAngleZBit)) != 0;
                set => Bitset |= (ushort) ((value ? 1 : 0) << RotAngleZBit);
            }

            // Scale
            public bool HasScaleX
            {
                get => (Bitset & (1 << ScaleXBit)) != 0;
                set => Bitset |= (ushort) ((value ? 1 : 0) << ScaleXBit);
            }

            public bool HasScaleY
            {
                get => (Bitset & (1 << ScaleYBit)) != 0;
                set => Bitset |= (ushort) ((value ? 1 : 0) << ScaleYBit);
            }

            public bool HasScaleZ
            {
                get => (Bitset & (1 << ScaleZBit)) != 0;
                set => Bitset |= (ushort) ((value ? 1 : 0) << ScaleZBit);
            }

            public bool IsLastSent
            {
                get => (Bitset & (1 << LastSentBit)) != 0;
                set => Bitset |= (ushort) ((value ? 1 : 0) << LastSentBit);
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
                // InLocalSpace + HasXXX Bits + LastSent flag
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

        // todo: revisit after MTT-876
        public bool Interpolate = true;

        /// <summary>
        /// The base amount of sends per seconds to use when range is disabled
        /// </summary>
        [SerializeField, Range(0, 120), Tooltip("The base amount of sends per seconds to use when range is disabled")]
        public float FixedSendsPerSecond = 30f;

        public bool UseFixedUpdate = true;

        public virtual IInterpolator<float> PositionXInterpolator { get; set; }
        public virtual IInterpolator<float> PositionYInterpolator { get; set; }

        public virtual IInterpolator<float> PositionZInterpolator { get; set; }

        // public virtual IInterpolator<float> RotationXInterpolator { get; set; }
        // public virtual IInterpolator<float> RotationYInterpolator { get; set; }
        // public virtual IInterpolator<float> RotationZInterpolator { get; set; }
        public virtual IInterpolator<Quaternion> RotationInterpolator { get; set; }
        public virtual IInterpolator<float> ScaleXInterpolator { get; set; }
        public virtual IInterpolator<float> ScaleYInterpolator { get; set; }
        public virtual IInterpolator<float> ScaleZInterpolator { get; set; }

        public void InitializeInterpolator<T, U>() where T : IInterpolator<float>, new() where U : IInterpolator<Quaternion>, new()
        {
            PositionXInterpolator = new T();
            PositionYInterpolator = new T();
            PositionZInterpolator = new T();
            RotationInterpolator = new U();
            // RotationXInterpolator = new T();
            // RotationYInterpolator = new T();
            // RotationZInterpolator = new T();
            ScaleXInterpolator = new T();
            ScaleYInterpolator = new T();
            ScaleZInterpolator = new T();
        }

        public void SetCurrentInterpolatedState()
        {
            var tickRate = NetworkManager.Singleton.NetworkConfig.TickRate;
            PositionXInterpolator.AddMeasurement(ReplNetworkState.Value.PositionX, new NetworkTime(tickRate, 0.0));
            PositionYInterpolator.AddMeasurement(ReplNetworkState.Value.PositionY, new NetworkTime(tickRate, 0.0));
            PositionZInterpolator.AddMeasurement(ReplNetworkState.Value.PositionZ, new NetworkTime(tickRate, 0.0));
            RotationInterpolator.AddMeasurement(Quaternion.Euler(ReplNetworkState.Value.Rotation), new NetworkTime(tickRate, 0.0));
            // RotationXInterpolator.AddMeasurement(ReplNetworkState.Value.RotationX ulerAngles.x, new NetworkTime(tickRate, 0.0));
            // RotationYInterpolator.AddMeasurement(ReplNetworkState.Value.RotationX ulerAngles.y, new NetworkTime(tickRate, 0.0));
            // RotationZInterpolator.AddMeasurement(ReplNetworkState.Value.RotationX ulerAngles.z, new NetworkTime(tickRate, 0.0));
            ScaleXInterpolator.AddMeasurement(ReplNetworkState.Value.ScaleX, new NetworkTime(tickRate, 0.0));
            ScaleYInterpolator.AddMeasurement(ReplNetworkState.Value.ScaleY, new NetworkTime(tickRate, 0.0));
            ScaleZInterpolator.AddMeasurement(ReplNetworkState.Value.ScaleZ, new NetworkTime(tickRate, 0.0));
        }

        public IEnumerable<IInterpolator<float>> AllFloatInterpolators()
        {
            yield return PositionXInterpolator;
            yield return PositionYInterpolator;
            yield return PositionZInterpolator;
            // yield return RotationXInterpolator;
            // yield return RotationYInterpolator;
            // yield return RotationZInterpolator;
            yield return ScaleXInterpolator;
            yield return ScaleYInterpolator;
            yield return ScaleZInterpolator;
        }

        public IEnumerable<IInterpolator<Quaternion>> AllQuaternionInterpolators()
        {
            yield return RotationInterpolator;
        }

        private int k_debugDrawLineTime = 10;

        internal NetworkState LocalNetworkState;
		
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

        internal bool UpdateNetworkStateCheckDirty(ref NetworkState networkState, double dirtyTime)
        {
            return UpdateNetworkStateCheckDirtyWithInfo(ref networkState, dirtyTime).isDirty;
        }

        private (bool isDirty, bool isPositionDirty, bool isRotationDirty, bool isScaleDirty) UpdateNetworkStateCheckDirtyWithInfo(ref NetworkState networkState, double dirtyTime)
        {
            var position = InLocalSpace ? m_Transform.localPosition : m_Transform.position;
            var rotAngles = InLocalSpace ? m_Transform.localEulerAngles : m_Transform.eulerAngles;
            var scale = InLocalSpace ? m_Transform.localScale : m_Transform.lossyScale;

            bool isDirty = false;
            bool isPositionDirty = false;
            bool isRotationDirty = false;
            bool isScaleDirty = false;

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

            // if (isDirty)
            {
                networkState.SentTime = dirtyTime;
            }


            return (isDirty, isPositionDirty, isRotationDirty, isScaleDirty);
        }

        internal void ApplyNetworkStateFromAuthority(NetworkState networkState)
        {
            PrevNetworkState = networkState;

            var interpolatedPosition = InLocalSpace ? m_Transform.localPosition : m_Transform.position;
            var interpolatedRotAngles = InLocalSpace ? m_Transform.localEulerAngles : m_Transform.eulerAngles;
            var interpolatedScale = InLocalSpace ? m_Transform.localScale : m_Transform.lossyScale;

            // InLocalSpace Read
            InLocalSpace = networkState.InLocalSpace;
            // Position Read
            if (networkState.HasPositionX)
            {
                interpolatedPosition.x = PositionXInterpolator.GetInterpolatedValue();
            }

            if (networkState.HasPositionY)
            {
                interpolatedPosition.y = PositionYInterpolator.GetInterpolatedValue();
            }

            if (networkState.HasPositionZ)
            {
                interpolatedPosition.z = PositionZInterpolator.GetInterpolatedValue();
            }

            // RotAngles Read
            // if (networkState.HasRotAngleX)
            // {
            //     interpolatedRotAngles.x = RotationXInterpolator.GetInterpolatedValue();
            // }
            //
            // if (networkState.HasRotAngleY)
            // {
            //     interpolatedRotAngles.y = RotationYInterpolator.GetInterpolatedValue();
            // }
            //
            // if (networkState.HasRotAngleZ)
            // {
            //     interpolatedRotAngles.z = RotationZInterpolator.GetInterpolatedValue();
            // }

            if (networkState.HasRotAngleX)
            {
                interpolatedRotAngles.x = RotationInterpolator.GetInterpolatedValue().eulerAngles.x;
            }

            if (networkState.HasRotAngleY)
            {
                interpolatedRotAngles.y = RotationInterpolator.GetInterpolatedValue().eulerAngles.y;
            }

            if (networkState.HasRotAngleZ)
            {
                interpolatedRotAngles.z = RotationInterpolator.GetInterpolatedValue().eulerAngles.z;
            }

            // Scale Read
            if (networkState.HasScaleX)
            {
                interpolatedScale.x = ScaleXInterpolator.GetInterpolatedValue();
            }

            if (networkState.HasScaleY)
            {
                interpolatedScale.y = ScaleYInterpolator.GetInterpolatedValue();
            }

            if (networkState.HasScaleZ)
            {
                interpolatedScale.z = ScaleZInterpolator.GetInterpolatedValue();
            }

            PrevNetworkState = networkState;
            // Position Apply
            if (networkState.HasPositionX || networkState.HasPositionY || networkState.HasPositionZ)
            {
                if (InLocalSpace)
                {
                    m_Transform.localPosition = interpolatedPosition;
                }
                else
                {
                    m_Transform.position = interpolatedPosition;
                }

                PrevNetworkState.Position = interpolatedPosition;
            }

            // RotAngles Apply
            if (networkState.HasRotAngleX || networkState.HasRotAngleY || networkState.HasRotAngleZ)
            {
                if (InLocalSpace)
                {
                    m_Transform.localRotation = Quaternion.Euler(interpolatedRotAngles);
                }
                else
                {
                    m_Transform.rotation = Quaternion.Euler(interpolatedRotAngles);
                }

                PrevNetworkState.Rotation = interpolatedRotAngles;
            }

            // Scale Apply
            if (networkState.HasScaleX || networkState.HasScaleY || networkState.HasScaleZ)
            {
                if (InLocalSpace)
                {
                    m_Transform.localScale = interpolatedScale;
                }
                else
                {
                    m_Transform.localScale = Vector3.one;
                    var lossyScale = m_Transform.lossyScale;
                    m_Transform.localScale = new Vector3(networkState.ScaleX / lossyScale.x, networkState.ScaleY / lossyScale.y, networkState.ScaleZ / lossyScale.z);
                }

                PrevNetworkState.Scale = interpolatedScale;
            }
        }

        // Is the non-interpolated authoritative state dirty?
        private bool IsAuthoritativeTransformDirty()
        {
            bool isDirty = false;
            var networkState = ReplNetworkState.Value;
            isDirty |= networkState.InLocalSpace != InLocalSpace;
            if (InLocalSpace)
            {
                isDirty |= networkState.Position != m_Transform.localPosition;
                isDirty |= networkState.Rotation != m_Transform.localEulerAngles;
                isDirty |= networkState.Scale != m_Transform.localScale;
            }
            else
            {
                isDirty |= networkState.Position != m_Transform.position;
                isDirty |= networkState.Rotation != m_Transform.eulerAngles;
                isDirty |= networkState.Scale != m_Transform.lossyScale;
            }

            return isDirty;
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

            if (newState.IsLastSent)
            {
                Debug.Log("asdf");
            }

            if (newState.HasPositionX)
            {
                PositionXInterpolator.AddMeasurement(newState.Position.x, sentTime);
            }

            if (newState.HasPositionY)
            {
                PositionYInterpolator.AddMeasurement(newState.Position.y, sentTime);
            }

            if (newState.HasPositionZ)
            {
                PositionZInterpolator.AddMeasurement(newState.Position.z, sentTime);
            }


            // todo fix this
            // if (newState.HasRotAngleX)
            // {
            //     RotationXInterpolator.AddMeasurement(newState.RotAngleX, sentTime);
            // }
            //
            // if (newState.HasRotAngleY)
            // {
            //     RotationYInterpolator.AddMeasurement(newState.RotAngleY, sentTime);
            // }
            //
            // if (newState.HasRotAngleZ)
            // {
            //     RotationZInterpolator.AddMeasurement(newState.RotAngleZ, sentTime);
            // }
            RotationInterpolator.AddMeasurement(Quaternion.Euler(newState.Rotation), sentTime);

            if (newState.HasScaleX)
            {
                ScaleXInterpolator.AddMeasurement(newState.ScaleX, sentTime);
            }

            if (newState.HasScaleY)
            {
                ScaleYInterpolator.AddMeasurement(newState.ScaleY, sentTime);
            }

            if (newState.HasScaleZ)
            {
                ScaleZInterpolator.AddMeasurement(newState.ScaleZ, sentTime);
            }

            if (NetworkManager.Singleton.LogLevel == LogLevel.Developer)
            {
                var pos = new Vector3(newState.PositionX, newState.PositionY, newState.PositionZ);
                Debug.DrawLine(pos, pos + Vector3.up + Vector3.left * Random.Range(0.5f, 2f), Color.green, k_debugDrawLineTime, false);
            }
        }

        private void Awake()
        {
            m_Transform = transform;
            bool interpolatorAlreadySet = false;
            foreach (var interpolator in AllFloatInterpolators())
            {
                if (interpolator != null || RotationInterpolator != null)
                {
                    interpolatorAlreadySet = true;
                    break;
                }
            }
            if (!interpolatorAlreadySet)
            {
                InitializeInterpolator<BufferedLinearInterpolatorFloat, BufferedLinearInterpolatorQuaternion>();
            }

            foreach (var interpolator in AllFloatInterpolators())
            {
                interpolator.Awake();
                interpolator.UseFixedUpdate = UseFixedUpdate;
            }
            RotationInterpolator.Awake();
            RotationInterpolator.UseFixedUpdate = UseFixedUpdate;

            ReplNetworkState.Settings.SendNetworkChannel = NetworkChannel.PositionUpdate;
            ReplNetworkState.OnValueChanged += OnNetworkStateChanged;
        }

        public void Start()
        {
            foreach (var interpolator in AllFloatInterpolators())
            {
                interpolator.Start();
            }

            RotationInterpolator.Start();
        }

        public void OnEnable()
        {
            foreach (var interpolator in AllFloatInterpolators())
            {
                interpolator.OnEnable();
            }

            RotationInterpolator.OnEnable();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                SetCurrentInterpolatedState(); // useful for late joining

                foreach (var interpolator in AllFloatInterpolators())
                {
                    interpolator.OnNetworkSpawn();
                }

                RotationInterpolator.OnNetworkSpawn();

                ApplyNetworkStateFromAuthority(ReplNetworkState.Value);
            }
        }

        private void OnDestroy()
        {
            ReplNetworkState.OnValueChanged -= OnNetworkStateChanged;

            foreach (var interpolator in AllFloatInterpolators())
            {
                interpolator.OnDestroy();
            }

            RotationInterpolator.OnDestroy();

        }

        private void DoSendToOthers(double time)
        {
            // check for time there was a change to the transform
            // this needs to be done in Update to catch that time change as soon as it happens.
            /*
			todo
			                if (UpdateNetworkState(ref LocalNetworkState))
                {
                    // if updated (dirty), change NetVar, mark it dirty
                    ReplNetworkState.Value = LocalNetworkState;
                    ReplNetworkState.SetDirty(true);
                }
			*/
			
			var isDirty = UpdateNetworkStateCheckDirty(ref ReplNetworkState.ValueRef, time); // todo sam diff here is Fixedtime
            if (isDirty)
            {
                alreadySentLastValue = false;
            }
            else if (!alreadySentLastValue)
            {
                isDirty = true;
                alreadySentLastValue = true; // to send one more value after a transform moves, so that unclamped interpolation has two similar last values
                shouldSendLastValue = true;
            }
            ReplNetworkState.ValueRef.IsLastSent = shouldSendLastValue;

            ReplNetworkState.SetDirty(shouldSendLastValue || isDirty);
            if (ReplNetworkState.IsDirty())
            {
                Debug.DrawLine(ReplNetworkState.Value.Position, ReplNetworkState.Value.Position + Vector3.up, Color.magenta, 10, false);
            }
            shouldSendLastValue = false;
        }

        private void FixedUpdate()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (IsServer && UseFixedUpdate)
            {
                // try to update local NetworkState
                DoSendToOthers(NetworkManager.LocalTime.FixedTime);
            }

            // try to update previously consumed NetworkState
            // if we have any changes, that means made some updates locally
            // we apply the latest ReplNetworkState again to revert our changes
            if (!IsServer)
            {
                var oldStateDirtyInfo = UpdateNetworkStateCheckDirtyWithInfo(ref PrevNetworkState, 0);
                if (oldStateDirtyInfo.isDirty && !oldStateDirtyInfo.isRotationDirty)
                {
                    // ignoring rotation dirty since quaternions will mess with euler angles, making this impossible to determine if the change to a single axis comes
                    // from an unauthorized transform change or euler to quaternion conversion artifacts.
                    var dirtyField = oldStateDirtyInfo.isPositionDirty ? "position" : "scale";
                    Debug.LogWarning($"A local change to {dirtyField} without authority detected, reverting back to latest interpolated network state!", this);
                    ApplyNetworkStateFromAuthority(ReplNetworkState.Value);
                }

                foreach (var interpolator in AllFloatInterpolators())
                {
                    interpolator.FixedUpdate(NetworkManager.ServerTime.FixedDeltaTime);
                }

                RotationInterpolator.FixedUpdate(NetworkManager.ServerTime.FixedDeltaTime);
            }
        }

        private bool alreadySentLastValue = false;
        private bool shouldSendLastValue = false;


        private void Update()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (IsServer && !UseFixedUpdate)
            {
                DoSendToOthers(NetworkManager.LocalTime.Time);
            }
            if (!IsServer && (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening))
            {
                foreach (var interpolator in AllFloatInterpolators())
                {
                    interpolator.Update(Time.deltaTime);
                }

                RotationInterpolator.Update(Time.deltaTime);

                if (NetworkManager.Singleton.LogLevel == LogLevel.Developer)
                {
                    var interpolatedPosition = new Vector3(PositionXInterpolator.GetInterpolatedValue(), PositionYInterpolator.GetInterpolatedValue(), PositionZInterpolator.GetInterpolatedValue());
                    Debug.DrawLine(interpolatedPosition, interpolatedPosition + Vector3.up, Color.magenta, k_debugDrawLineTime, false);
                }

                ApplyNetworkStateFromAuthority(ReplNetworkState.Value);
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