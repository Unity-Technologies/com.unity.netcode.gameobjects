using System;
using DefaultNamespace;
using MLAPI.NetworkVariable;
using MLAPI.Serialization;
using MLAPI.Timing;
using MLAPI.Transports;
using UnityEngine;

namespace MLAPI.Prototyping
{
    /// <summary>
    /// A prototype component for syncing transforms
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkTransform")]
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

            public int SentTick;

            public NetworkState(NetworkState copy)
            {
                InLocalSpace = copy.InLocalSpace;
                Position = copy.Position;
                Rotation = copy.Rotation;
                Scale = copy.Scale;
                SentTick = copy.SentTick;
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
                serializer.Serialize(ref SentTick);
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

        private int previousTickSam;
        private Vector3 previousPosSam;
        private void UpdateNetworkState()
        {
            m_NetworkState.Value.InLocalSpace = InLocalSpace;
            // m_NetworkState.Value.SentTime = Time.realtimeSinceStartup;
            m_NetworkState.Value.SentTick = NetworkManager.Singleton.LocalTime.Tick;
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

            Debug.Log($"sam asdf distance {Math.Round((m_NetworkState.Value.Position - previousPosSam).magnitude, 2)} tick diff {m_NetworkState.Value.SentTick - previousTickSam} sam");
            previousTickSam = m_NetworkState.Value.SentTick;
            previousPosSam = m_NetworkState.Value.Position;

            m_NetworkState.SetDirty(true);
        }

        private void ApplyNetworkState(NetworkState netState)
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
            Debug.Log($"diff tick sam {(newState.SentTick - oldState.SentTick, 2)}");
            // oldTick = NetworkManager.Singleton.ServerTime.Tick;
            var sentTime = new NetworkTime(NetworkManager.Singleton.ServerTime.TickRate, newState.SentTick);
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

            var currentTime = new NetworkTime(NetworkManager.Singleton.ServerTime.TickRate, m_NetworkState.Value.SentTick);
            PositionInterpolator.Reset(m_Transform.position, currentTime);
            RotationInterpolator.Reset(m_Transform.rotation, currentTime);

            UpdateNetVarPerms();

            m_NetworkState.Settings.SendNetworkChannel = Channel;
            m_NetworkState.Settings.SendTickrate = FixedSendsPerSecond;

            m_NetworkState.OnValueChanged += OnNetworkStateChanged;
        }

        public override void OnNetworkSpawn()
        {
            m_PrevNetworkState = null;
            if (enabled) // todo Luke fix your UX
            {
                NetworkManager.NetworkTickSystem.Tick += NetworkTickUpdate;
            }

            PositionInterpolator.OnNetworkSpawn();
            RotationInterpolator.OnNetworkSpawn();
        }

        private void OnDestroy()
        {
            m_NetworkState.OnValueChanged -= OnNetworkStateChanged;
        }

        private void NetworkTickUpdate()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (CanUpdateTransform) //&& IsNetworkStateDirty(m_NetworkState.Value))
            {
                UpdateNetworkState();
            }
            else
            {
                if (IsNetworkStateDirty(m_PrevNetworkState))
                {
                    Debug.LogWarning("A local change without authority detected, revert back to latest network state!");
                    ApplyNetworkState(m_NetworkState.Value);
                }

                PositionInterpolator.NetworkTickUpdate(NetworkManager.ServerTime.FixedDeltaTime);
                RotationInterpolator.NetworkTickUpdate(NetworkManager.ServerTime.FixedDeltaTime);
            }
        }

        private int debugOldTime = 0;

        private void Update()
        {
            // Debug.Log("gaga "+Math.Round(Time.time - (float)NetworkManager.Singleton.ServerTime.Time, 2));

            // Debug.Log($"sam {Math.Round(NetworkManager.Singleton.ServerTime.Time - debugOldTime, 2)}");
            // debugOldTime = NetworkManager.Singleton.ServerTime.Time;
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (!CanUpdateTransform)
            {
                PositionInterpolator.Update(Time.deltaTime);
                RotationInterpolator.Update(Time.deltaTime);
                ApplyNetworkState(m_NetworkState.Value);
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
