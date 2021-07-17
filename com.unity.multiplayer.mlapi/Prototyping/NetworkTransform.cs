using System;
using MLAPI.NetworkVariable;
using MLAPI.Serialization;
using MLAPI.Transports;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("TransformAuthority"), Tooltip("Defines who can update this transform")]
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

        private readonly NetworkVariable<NetworkState> m_NetworkState = new NetworkVariable<NetworkState>(new NetworkState());

        /// <summary>
        /// Does this instance (client or server) has authority to update transform?
        /// </summary>
        public bool CanUpdateTransform =>
            Authority == NetworkAuthority.Client && IsClient && IsOwner ||
            Authority == NetworkAuthority.Server && IsServer ||
            Authority == NetworkAuthority.Shared;

        private bool IsNetworkStateDirty
        {
            get
            {
                bool isDirty = false;

                isDirty |= m_NetworkState.Value.InLocalSpace != InLocalSpace;
                if (InLocalSpace)
                {
                    isDirty |= m_NetworkState.Value.Position != transform.localPosition;
                    isDirty |= m_NetworkState.Value.Rotation != transform.localRotation;
                    isDirty |= m_NetworkState.Value.Scale != transform.localScale;
                }
                else
                {
                    isDirty |= m_NetworkState.Value.Position != transform.position;
                    isDirty |= m_NetworkState.Value.Rotation != transform.rotation;
                    isDirty |= m_NetworkState.Value.Scale != transform.lossyScale;
                }

                return isDirty;
            }
        }

        private void UpdateNetworkState()
        {
            m_NetworkState.Value.InLocalSpace = InLocalSpace;
            if (InLocalSpace)
            {
                m_NetworkState.Value.Position = transform.localPosition;
                m_NetworkState.Value.Rotation = transform.localRotation;
                m_NetworkState.Value.Scale = transform.localScale;
            }
            else
            {
                m_NetworkState.Value.Position = transform.position;
                m_NetworkState.Value.Rotation = transform.rotation;
                m_NetworkState.Value.Scale = transform.lossyScale;
            }

            m_NetworkState.SetDirty(true);
        }

        private void ApplyNetworkState(NetworkState netState)
        {
            InLocalSpace = netState.InLocalSpace;
            if (InLocalSpace)
            {
                transform.localPosition = netState.Position;
                transform.localRotation = netState.Rotation;
                transform.localScale = netState.Scale;
            }
            else
            {
                transform.position = netState.Position;
                transform.rotation = netState.Rotation;
                // transform.lossyScale = netState.Scale;
                transform.localScale = Vector3.one;
                var lossyScale = transform.lossyScale;
                transform.localScale = new Vector3(netState.Scale.x / lossyScale.x, netState.Scale.y / lossyScale.y, netState.Scale.z / lossyScale.z);
            }
        }

        private void OnNetworkStateChanged(NetworkState oldState, NetworkState newState)
        {
            if (!NetworkObject.IsSpawned)
            {
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
            UpdateNetVarPerms();

            m_NetworkState.Settings.SendNetworkChannel = Channel;
            m_NetworkState.Settings.SendTickrate = FixedSendsPerSecond;

            m_NetworkState.OnValueChanged += OnNetworkStateChanged;
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

            if (IsNetworkStateDirty)
            {
                if (CanUpdateTransform)
                {
                    UpdateNetworkState();
                }
                else
                {
                    ApplyNetworkState(m_NetworkState.Value);
                }
            }
        }

        /// <summary>
        /// Updates the NetworkTransform's authority model at runtime
        /// </summary>
        public void SetAuthority(NetworkAuthority authority)
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
