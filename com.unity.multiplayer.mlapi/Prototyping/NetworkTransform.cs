using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Netcode.Prototyping
{
    /// <summary>
    /// A prototype component for syncing transforms
    /// </summary>
    [AddComponentMenu("Netcode/" + nameof(NetworkTransform))]
    [DefaultExecutionOrder(1000000)]
    public class NetworkTransform : NetworkBehaviour
    {
        /// <summary>
        /// Server authority allows only the server to update this transform
        /// Client authority allows only the owner client to update this transform
        /// Shared authority allows everyone to update this transform
        /// </summary>
        public enum NetworkAuthority
        {
            Server = 0,
            Client,
            Shared
        }

        private class NetworkState : INetworkSerializable
        {
            public bool InLocalSpace;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;

            public double SentTime;

            public NetworkState(NetworkState copy)
            {
                InLocalSpace = copy.InLocalSpace;
                Position = copy.Position;
                Rotation = copy.Rotation;
                Scale = copy.Scale;
                SentTime = copy.SentTime;
            }

            public NetworkState()
            {
            }

            public void NetworkSerialize(NetworkSerializer serializer)
            {
                serializer.Serialize(ref InLocalSpace);
                serializer.Serialize(ref Position);
                serializer.Serialize(ref Rotation);
                serializer.Serialize(ref Scale);
                serializer.Serialize(ref SentTime);
            }
        }

        /// <summary>
        /// TODO this will need refactoring
        /// Specifies who can update this transform
        /// </summary>
        [Tooltip("Defines who can update this transform")]
        public NetworkAuthority Authority = NetworkAuthority.Server;

        /// <summary>
        /// The network channel to use send updates
        /// </summary>
        [Tooltip("The network channel to use send updates")]
        private NetworkChannel Channel = NetworkChannel.SyncChannel;
        // private NetworkChannel Channel = NetworkChannel.PositionUpdate;

        /// <summary>
        /// Sets whether this transform should sync in local space or in world space.
        /// This is important to set since reparenting this transform could have issues,
        /// if using world position (depending on who gets synced first: the parent or the child)
        /// Having a child always at position 0,0,0 for example will have less possibilities of desync than when using world positions
        /// </summary>
        [Tooltip("Sets whether this transform should sync in local space or in world space")]
        public bool InLocalSpace = false;

        /// <summary>
        /// The base amount of sends per seconds to use when range is disabled
        /// </summary>
        [SerializeField, Range(0, 120), Tooltip("The base amount of sends per seconds to use when range is disabled")]
        public float FixedSendsPerSecond = 30f;

        [SerializeField]
        private InterpolatorFactory<Vector3> m_PositionInterpolatorFactory;

        [SerializeField]
        private InterpolatorFactory<Quaternion> m_RotationInterpolatorFactory;

        protected virtual IInterpolator<Vector3> PositionInterpolator { get; set; }

        protected virtual IInterpolator<Quaternion> RotationInterpolator { get; set; }
        // public IInterpolator<Vector3> PositionInterpolator = new BufferedLinearInterpolatorVector3(ScriptableObject.CreateInstance<BufferedLinearInterpolatorVector3Factory>()); // todo tmp, use default value instead

        private Transform m_Transform; // cache the transform component to reduce unnecessary bounce between managed and native
        private readonly NetworkVariable<NetworkState> m_NetworkState = new NetworkVariable<NetworkState>(new NetworkState());
        private NetworkState m_PrevNetworkState;

        /// <summary>
        /// Does this instance (client or server) has authority to update transform?
        /// </summary>
        public bool CanUpdateTransform =>
            Authority == NetworkAuthority.Client && IsClient && IsOwner ||
            Authority == NetworkAuthority.Server && IsServer ||
            Authority == NetworkAuthority.Shared;

        private bool IsNetworkStateDirty(NetworkState networkState)
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
                isDirty |= networkState.Scale != m_Transform.localScale;
            }
            else
            {
                isDirty |= networkState.Position != PositionInterpolator.GetInterpolatedValue();
                isDirty |= networkState.Rotation != RotationInterpolator.GetInterpolatedValue();
                isDirty |= networkState.Scale != m_Transform.lossyScale;
            }

            return isDirty;
        }

        private bool IsTransformDirty()
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

        private double previousTimeSam;
        private Vector3 previousPosSam;
        private void SendNetworkStateToGhosts(double dirtyTime)
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
            Debug.DrawLine(m_NetworkState.Value.Position, m_NetworkState.Value.Position + Vector3.up * 100f * (float) (m_NetworkState.Value.SentTime - previousTimeSam), Color.yellow, 10, false);
            Debug.Log($"sam asdf distance {Math.Round((m_NetworkState.Value.Position - previousPosSam).magnitude, 2)} tick diff {m_NetworkState.Value.SentTime - previousTimeSam} sam");
            previousTimeSam = m_NetworkState.Value.SentTime;
            previousPosSam = m_NetworkState.Value.Position;

            m_NetworkState.SetDirty(true);
        }

        private void ApplyNetworkStateFromAuthority(NetworkState netState)
        {
            netState = new NetworkState(netState);
            netState.Position = PositionInterpolator.GetInterpolatedValue();
            netState.Rotation = RotationInterpolator.GetInterpolatedValue();

            InLocalSpace = netState.InLocalSpace;
            if (InLocalSpace)
            {
                m_Transform.localPosition = netState.Position;
                m_Transform.localRotation = netState.Rotation;
                m_Transform.localScale = netState.Scale;
            }
            else
            {
                m_Transform.position = netState.Position;
                m_Transform.rotation = netState.Rotation;
                m_Transform.localScale = Vector3.one;
                var lossyScale = m_Transform.lossyScale;
                m_Transform.localScale = new Vector3(netState.Scale.x / lossyScale.x, netState.Scale.y / lossyScale.y, netState.Scale.z / lossyScale.z);
            }

            m_PrevNetworkState = netState;
        }


        private int oldTick;
        private NetworkState debug_previousStateChanged;
        private void OnNetworkStateChanged(NetworkState oldState, NetworkState newState)
        {
            if (!NetworkObject.IsSpawned)
            {
                // todo MTT-849 should never happen but yet it does! maybe revisit/dig after NetVar updates and snapshot system lands?
                return;
            }

            if (Authority == NetworkAuthority.Client && IsClient && IsOwner)
            {
                // todo MTT-768 this shouldn't happen anymore with new tick system (tick written will be higher than tick read, so netvar wouldn't change in that case)
                return;
            }

            // todo check teleport flag
            // if (newState.Teleporting)
            // {
            //     PositionInterpolator.Reset(newState.Position, new NetworkTime(NetworkManager.Singleton.ServerTime.TickRate, newState.SentTick));
            // }

            // PositionInterpolator.AddMeasurement(newState.Position, NetworkManager.Singleton.ServerTime.Time);

            Debug.Log($"distance sam {Math.Round((newState.Position - oldState.Position).magnitude, 2)}");
            Debug.Log($"diff tick sam {(newState.SentTime - oldState.SentTime, 2)}");
            // oldTick = NetworkManager.Singleton.ServerTime.Tick;
            // Debug.DrawLine(newState.Position, newState.Position + Vector3.down + Vector3.left, Color.yellow, 10, false);
            debug_previousStateChanged = newState;
            var sentTime = new NetworkTime(NetworkManager.Singleton.ServerTime.TickRate, newState.SentTime);
            PositionInterpolator.AddMeasurement(newState.Position, sentTime);
            RotationInterpolator.AddMeasurement(newState.Rotation, sentTime);
        }

        private void UpdateNetVarPerms()
        {
            switch (Authority)
            {
                default:
                    throw new NotImplementedException($"Authority: {Authority} is not handled");
                case NetworkAuthority.Server:
                    m_NetworkState.Settings.WritePermission = NetworkVariablePermission.ServerOnly;
                    break;
                case NetworkAuthority.Client:
                    m_NetworkState.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                    break;
                case NetworkAuthority.Shared:
                    m_NetworkState.Settings.WritePermission = NetworkVariablePermission.Everyone;
                    break;
            }
        }

        private void Awake()
        {
            //debug, remove me
            // Time.maximumDeltaTime = 999f;

            m_Transform = transform;
            var defaultBufferTime = 0.1f;
            if (m_PositionInterpolatorFactory == null)
            {
                PositionInterpolator = new BufferedLinearInterpolatorVector3(new BufferedLinearInterpolatorSettings {InterpolationTime = defaultBufferTime});
            }
            else
            {
                PositionInterpolator = m_PositionInterpolatorFactory.CreateInterpolator();
            }

            if (m_RotationInterpolatorFactory == null)
            {
                RotationInterpolator = new BufferedLinearInterpolatorQuaternion(new BufferedLinearInterpolatorSettings {InterpolationTime = defaultBufferTime});
            }
            else
            {
                RotationInterpolator = m_RotationInterpolatorFactory.CreateInterpolator();
            }

            UpdateNetVarPerms();

            m_NetworkState.Settings.SendNetworkChannel = Channel;
            m_NetworkState.Settings.SendTickrate = FixedSendsPerSecond;

            m_NetworkState.OnValueChanged += OnNetworkStateChanged;
        }

        public override void OnNetworkSpawn()
        {
            m_PrevNetworkState = null;
            // if (enabled) // todo Luke fix your UX
            // {
            //     NetworkManager.NetworkTickSystem.Tick += NetworkTickUpdate;
            // }

            var currentTime = new NetworkTime(NetworkManager.Singleton.ServerTime.TickRate, m_NetworkState.Value.SentTime);
            PositionInterpolator.Reset(m_Transform.position, currentTime);
            RotationInterpolator.Reset(m_Transform.rotation, currentTime);

            PositionInterpolator.OnNetworkSpawn();
            RotationInterpolator.OnNetworkSpawn();
        }

        private void OnDestroy()
        {
            m_NetworkState.OnValueChanged -= OnNetworkStateChanged;
        }

        private void FixedUpdate()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }
            // if (CanUpdateTransform)
            // {
            //     if (IsTransformDirty())
            //     {
            //         // check for time there was a change to the transform
            //         m_DirtyTime = NetworkManager.LocalTime.Time;
            //     }
            //
            //     UpdateNetworkState();
            // }
            // else
            {
                if (IsNetworkStateDirty(m_PrevNetworkState))
                {
                    Debug.LogWarning("A local change without authority detected, revert back to latest network state!");
                    ApplyNetworkStateFromAuthority(m_NetworkState.Value);
                }

                PositionInterpolator.FixedUpdate(NetworkManager.ServerTime.FixedDeltaTime);
                RotationInterpolator.FixedUpdate(NetworkManager.ServerTime.FixedDeltaTime);
            }
        }

        private int debugOldTime = 0;

        private void Update()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (CanUpdateTransform)
            {
                if (IsTransformDirty())
                {
                    // check for time there was a change to the transform
                    SendNetworkStateToGhosts(NetworkManager.LocalTime.Time);
                }
            }

            if (!CanUpdateTransform)
            {
                // Debug.Log("gaga "+Math.Round(Time.time - (float)NetworkManager.Singleton.ServerTime.Time, 2));

                // Debug.Log($"sam {Math.Round(NetworkManager.Singleton.ServerTime.Time - debugOldTime, 2)}");
                // debugOldTime = NetworkManager.Singleton.ServerTime.Time;

                PositionInterpolator.Update(Time.deltaTime);
                RotationInterpolator.Update(Time.deltaTime);
                ApplyNetworkStateFromAuthority(m_NetworkState.Value);
            }
        }

        /// <summary>
        /// Updates the NetworkTransform's authority model at runtime
        /// </summary>
        internal void SetAuthority(NetworkAuthority authority)
        {
            Authority = authority;
            UpdateNetVarPerms();
            // todo this should be synced with the other side.
            // let's wait for a more final solution before adding more code here
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
