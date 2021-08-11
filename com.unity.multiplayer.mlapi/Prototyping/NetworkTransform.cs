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

        internal class NetworkState : INetworkSerializable
        {
            internal const int InLocalSpaceBit = 0;
            internal const int PositionXBit = 1;
            internal const int PositionYBit = 2;
            internal const int PositionZBit = 3;
            internal const int RotationXBit = 4;
            internal const int RotationYBit = 5;
            internal const int RotationZBit = 6;
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
            // Rotation
            public bool HasRotationX
            {
                get => (Bitset & (1 << RotationXBit)) != 0;
                set => Bitset |= (ushort)((value ? 1 : 0) << RotationXBit);
            }
            public bool HasRotationY
            {
                get => (Bitset & (1 << RotationYBit)) != 0;
                set => Bitset |= (ushort)((value ? 1 : 0) << RotationYBit);
            }
            public bool HasRotationZ
            {
                get => (Bitset & (1 << RotationZBit)) != 0;
                set => Bitset |= (ushort)((value ? 1 : 0) << RotationZBit);
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
            public float RotationX, RotationY, RotationZ;
            public float ScaleX, ScaleY, ScaleZ;

            public void NetworkSerialize(NetworkSerializer serializer)
            {
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
                // Rotation Values
                if (HasRotationX)
                {
                    serializer.Serialize(ref RotationX);
                }
                if (HasRotationY)
                {
                    serializer.Serialize(ref RotationY);
                }
                if (HasRotationZ)
                {
                    serializer.Serialize(ref RotationZ);
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

        public bool SyncPositionX = true, SyncPositionY = true, SyncPositionZ = true;
        public bool SyncRotationX = true, SyncRotationY = true, SyncRotationZ = true;
        public bool SyncScaleX = true, SyncScaleY = true, SyncScaleZ = true;

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

            var position = InLocalSpace ? m_Transform.localPosition : m_Transform.position;
            var rotation = InLocalSpace ? m_Transform.localRotation : m_Transform.rotation;
            var scale = InLocalSpace ? m_Transform.localScale : m_Transform.lossyScale;

            return
                // InLocalSpace Check
                (networkState.InLocalSpace != InLocalSpace) ||
                // Position Check
                (SyncPositionX && !Mathf.Approximately(position.x, networkState.PositionX)) ||
                (SyncPositionY && !Mathf.Approximately(position.y, networkState.PositionY)) ||
                (SyncPositionZ && !Mathf.Approximately(position.z, networkState.PositionZ)) ||
                // Rotation Check
                (SyncRotationX && !Mathf.Approximately(rotation.x, networkState.RotationX)) ||
                (SyncRotationY && !Mathf.Approximately(rotation.y, networkState.RotationY)) ||
                (SyncRotationZ && !Mathf.Approximately(rotation.z, networkState.RotationZ)) ||
                // Scale Check
                (SyncScaleX && !Mathf.Approximately(scale.x, networkState.ScaleX)) ||
                (SyncScaleY && !Mathf.Approximately(scale.y, networkState.ScaleY)) ||
                (SyncScaleZ && !Mathf.Approximately(scale.z, networkState.ScaleZ));
        }

        private void UpdateNetworkState()
        {
            var position = InLocalSpace ? m_Transform.localPosition : m_Transform.position;
            var rotation = InLocalSpace ? m_Transform.localRotation : m_Transform.rotation;
            var scale = InLocalSpace ? m_Transform.localScale : m_Transform.lossyScale;

            // InLocalSpace Bit
            m_NetworkState.Value.InLocalSpace = InLocalSpace;
            // Position Bits
            (m_NetworkState.Value.HasPositionX, m_NetworkState.Value.HasPositionY, m_NetworkState.Value.HasPositionZ) =
                (SyncPositionX, SyncPositionY, SyncPositionZ);
            // Rotation Bits
            (m_NetworkState.Value.HasRotationX, m_NetworkState.Value.HasRotationY, m_NetworkState.Value.HasRotationZ) =
                (SyncRotationX, SyncRotationY, SyncRotationZ);
            // Scale Bits
            (m_NetworkState.Value.HasScaleX, m_NetworkState.Value.HasScaleY, m_NetworkState.Value.HasScaleZ) =
                (SyncScaleX, SyncScaleY, SyncScaleZ);

            // Position Values
            (m_NetworkState.Value.PositionX, m_NetworkState.Value.PositionY, m_NetworkState.Value.PositionZ) =
                (position.x, position.y, position.z);
            // Rotation Values
            (m_NetworkState.Value.RotationX, m_NetworkState.Value.RotationY, m_NetworkState.Value.RotationZ) =
                (rotation.x, rotation.y, rotation.z);
            // Scale Values
            (m_NetworkState.Value.ScaleX, m_NetworkState.Value.ScaleY, m_NetworkState.Value.ScaleZ) =
                (scale.x, scale.y, scale.z);

            m_NetworkState.SetDirty(true);
        }

        // TODO: temporary! the function body below probably needs to be rewritten
        // (e.g. rotation/quaternion computation looks unreliable, needs to be properly vetted/tested)
        private void ApplyNetworkState(NetworkState networkState)
        {
            m_PrevNetworkState = networkState;

            var position = InLocalSpace ? m_Transform.localPosition : m_Transform.position;
            var rotation = InLocalSpace ? m_Transform.localRotation : m_Transform.rotation;
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
            // Rotation Read
            if (networkState.HasRotationX)
            {
                rotation.x = networkState.RotationX;
            }
            if (networkState.HasRotationY)
            {
                rotation.y = networkState.RotationY;
            }
            if (networkState.HasRotationZ)
            {
                rotation.z = networkState.RotationZ;
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
            // Rotation Apply
            if (networkState.HasRotationX || networkState.HasRotationY || networkState.HasRotationZ)
            {
                // numerical precision issues can make the remainder very slightly negative.
                // In this case, use 0 for w as, otherwise, w would be NaN.
                var remainder = 1f - Mathf.Pow(rotation.x, 2) - Mathf.Pow(rotation.y, 2) - Mathf.Pow(rotation.z, 2);
                var computedW = (remainder > 0f) ? Mathf.Sqrt(remainder) : 0.0f;
                var quaternion = new Quaternion(rotation.x, rotation.y, rotation.z, computedW);
                if (InLocalSpace)
                {
                    m_Transform.localRotation = quaternion;
                }
                else
                {
                    m_Transform.rotation = quaternion;
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
