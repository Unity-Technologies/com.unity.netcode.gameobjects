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
        public bool SyncRotAngleX = true, SyncRotAngleY = true, SyncRotAngleZ = true;
        public bool SyncScaleX = true, SyncScaleY = true, SyncScaleZ = true;

        /// <summary>
        /// The base amount of sends per seconds to use when range is disabled
        /// </summary>
        [SerializeField, Range(0, 120), Tooltip("The base amount of sends per seconds to use when range is disabled")]
        public float FixedSendsPerSecond = 30f;

        private Transform m_Transform; // cache the transform component to reduce unnecessary bounce between managed and native
        internal readonly NetworkVariable<NetworkState> ReplNetworkState = new NetworkVariable<NetworkState>(new NetworkState());
        internal NetworkState PrevNetworkState;

        /// <summary>
        /// Does this instance (client or server) has authority to update transform?
        /// </summary>
        public bool CanUpdateTransform =>
            Authority == NetworkAuthority.Client && IsClient && IsOwner ||
            Authority == NetworkAuthority.Server && IsServer ||
            Authority == NetworkAuthority.Shared;

        internal bool IsNetworkStateDirty(NetworkState networkState)
        {
            if (networkState == null)
            {
                return false;
            }

            var position = InLocalSpace ? m_Transform.localPosition : m_Transform.position;
            var rotAngles = InLocalSpace ? m_Transform.localEulerAngles : m_Transform.eulerAngles;
            var scale = InLocalSpace ? m_Transform.localScale : m_Transform.lossyScale;

            return
                // InLocalSpace Check
                (InLocalSpace != networkState.InLocalSpace) ||
                // Position Check
                (SyncPositionX && !Mathf.Approximately(position.x, networkState.PositionX)) ||
                (SyncPositionY && !Mathf.Approximately(position.y, networkState.PositionY)) ||
                (SyncPositionZ && !Mathf.Approximately(position.z, networkState.PositionZ)) ||
                // RotAngles Check
                (SyncRotAngleX && !Mathf.Approximately(rotAngles.x, networkState.RotAngleX)) ||
                (SyncRotAngleY && !Mathf.Approximately(rotAngles.y, networkState.RotAngleY)) ||
                (SyncRotAngleZ && !Mathf.Approximately(rotAngles.z, networkState.RotAngleZ)) ||
                // Scale Check
                (SyncScaleX && !Mathf.Approximately(scale.x, networkState.ScaleX)) ||
                (SyncScaleY && !Mathf.Approximately(scale.y, networkState.ScaleY)) ||
                (SyncScaleZ && !Mathf.Approximately(scale.z, networkState.ScaleZ));
        }

        internal void UpdateNetworkState()
        {
            var position = InLocalSpace ? m_Transform.localPosition : m_Transform.position;
            var rotAngles = InLocalSpace ? m_Transform.localEulerAngles : m_Transform.eulerAngles;
            var scale = InLocalSpace ? m_Transform.localScale : m_Transform.lossyScale;

            // InLocalSpace Bit
            ReplNetworkState.Value.InLocalSpace = InLocalSpace;
            // Position Bits
            (ReplNetworkState.Value.HasPositionX, ReplNetworkState.Value.HasPositionY, ReplNetworkState.Value.HasPositionZ) =
                (SyncPositionX, SyncPositionY, SyncPositionZ);
            // RotAngle Bits
            (ReplNetworkState.Value.HasRotAngleX, ReplNetworkState.Value.HasRotAngleY, ReplNetworkState.Value.HasRotAngleZ) =
                (SyncRotAngleX, SyncRotAngleY, SyncRotAngleZ);
            // Scale Bits
            (ReplNetworkState.Value.HasScaleX, ReplNetworkState.Value.HasScaleY, ReplNetworkState.Value.HasScaleZ) =
                (SyncScaleX, SyncScaleY, SyncScaleZ);

            // Position Values
            (ReplNetworkState.Value.PositionX, ReplNetworkState.Value.PositionY, ReplNetworkState.Value.PositionZ) =
                (position.x, position.y, position.z);
            // RotAngle Values
            (ReplNetworkState.Value.RotAngleX, ReplNetworkState.Value.RotAngleY, ReplNetworkState.Value.RotAngleZ) =
                (rotAngles.x, rotAngles.y, rotAngles.z);
            // Scale Values
            (ReplNetworkState.Value.ScaleX, ReplNetworkState.Value.ScaleY, ReplNetworkState.Value.ScaleZ) =
                (scale.x, scale.y, scale.z);

            ReplNetworkState.SetDirty(true);
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
                    ReplNetworkState.Settings.WritePermission = NetworkVariableWritePermission.ServerOnly;
                    break;
                case NetworkAuthority.Client:
                    ReplNetworkState.Settings.WritePermission = NetworkVariableWritePermission.OwnerOnly;
                    break;
            }
        }

        private void Awake()
        {
            m_Transform = transform;

            UpdateNetVarPerms();

            ReplNetworkState.Settings.SendNetworkChannel = Channel;
            ReplNetworkState.Settings.SendTickrate = FixedSendsPerSecond;

            ReplNetworkState.OnValueChanged += OnNetworkStateChanged;
        }

        public override void OnNetworkSpawn()
        {
            PrevNetworkState = null;
        }

        private void OnDestroy()
        {
            ReplNetworkState.OnValueChanged -= OnNetworkStateChanged;
        }

        private void FixedUpdate()
        {
            if (!NetworkObject.IsSpawned)
            {
                return;
            }

            if (CanUpdateTransform && IsNetworkStateDirty(ReplNetworkState.Value))
            {
                UpdateNetworkState();
            }
            else
            {
                if (IsNetworkStateDirty(PrevNetworkState))
                {
                    Debug.LogWarning("A local change without authority detected, revert back to latest network state!");
                }

                ApplyNetworkState(ReplNetworkState.Value);
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
