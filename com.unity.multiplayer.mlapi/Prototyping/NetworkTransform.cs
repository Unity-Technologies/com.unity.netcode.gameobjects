using System;
using UnityEngine;

namespace Unity.Netcode.Prototyping
{
    /// <summary>
    /// A prototype component for syncing transforms
    /// </summary>
    [AddComponentMenu("Netcode/" + nameof(NetworkTransform))]
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

            public void NetworkSerialize(NetworkSerializer serializer)
            {
                serializer.Serialize(ref InLocalSpace);
                serializer.Serialize(ref Position);
                serializer.Serialize(ref Rotation);
                serializer.Serialize(ref Scale);
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
        public NetworkChannel Channel = NetworkChannel.NetworkVariable;

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

        private void UpdateNetworkState()
        {
            m_NetworkState.Value.InLocalSpace = InLocalSpace;
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

        private void ApplyNetworkState(NetworkState netState)
        {
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

            ApplyNetworkState(newState);
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

            UpdateNetVarPerms();

            m_NetworkState.Settings.SendNetworkChannel = Channel;
            m_NetworkState.Settings.SendTickrate = FixedSendsPerSecond;

            m_NetworkState.OnValueChanged += OnNetworkStateChanged;
        }

        public override void OnNetworkSpawn()
        {
            m_PrevNetworkState = null;
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

            if (CanUpdateTransform && IsNetworkStateDirty(m_NetworkState.Value))
            {
                UpdateNetworkState();
            }
            else
            {
                if (IsNetworkStateDirty(m_PrevNetworkState))
                {
                    Debug.LogWarning("A local change without authority detected, revert back to latest network state!");
                }

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
            throw new NotImplementedException(); // TODO MTT-769
        }
    }
}
