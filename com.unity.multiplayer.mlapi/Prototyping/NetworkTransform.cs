using System;
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
        private NetworkChannel Channel = NetworkChannel.PositionUpdate;

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

        protected virtual IInterpolator<Vector3> PositionInterpolator { get; set; }
        protected virtual IInterpolator<Quaternion> RotationInterpolator { get; set; }
        protected virtual IInterpolator<Vector3> ScaleInterpolator { get; set; }

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

        private readonly NetworkVariable<NetworkState> m_NetworkState = new NetworkVariable<NetworkState>(new NetworkState());
        private NetworkState m_PrevNetworkState;

        /// <summary>
        /// Does this instance (client or server) has authority to update transform?
        /// </summary>
        public bool CanUpdateTransform =>
            Authority == NetworkAuthority.Client && IsClient && IsOwner ||
            Authority == NetworkAuthority.Server && IsServer ||
            Authority == NetworkAuthority.Shared;

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

            if (Authority == NetworkAuthority.Client && IsClient && IsOwner)
            {
                // todo MTT-768 this shouldn't happen anymore with new tick system (tick written will be higher than tick read, so netvar wouldn't change in that case)
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
            m_Transform = transform;
            PositionInterpolator = new BufferedLinearInterpolatorVector3();
            RotationInterpolator = new BufferedLinearInterpolatorQuaternion();
            ScaleInterpolator = new BufferedLinearInterpolatorVector3();

            UpdateNetVarPerms();

            m_NetworkState.Settings.SendNetworkChannel = Channel;
            m_NetworkState.Settings.SendTickrate = FixedSendsPerSecond;

            m_NetworkState.OnValueChanged += OnNetworkStateChanged;
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
            m_NetworkState.OnValueChanged -= OnNetworkStateChanged;

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
                    SendNetworkStateToGhosts(NetworkManager.LocalTime.Time);
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
