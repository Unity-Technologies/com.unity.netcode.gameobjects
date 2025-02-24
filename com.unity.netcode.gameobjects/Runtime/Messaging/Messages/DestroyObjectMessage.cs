using System.Linq;
using System.Runtime.CompilerServices;

namespace Unity.Netcode
{
    internal struct DestroyObjectMessage : INetworkMessage, INetworkSerializeByMemcpy
    {
        private const int k_OptimizeDestroyObjectMessage = 1;
        public int Version => k_OptimizeDestroyObjectMessage;

        private const string k_Name = "DestroyObjectMessage";

        public ulong NetworkObjectId;

        /// <summary>
        /// Used to communicate whether to destroy the associated game object.
        /// Should be false if the object is InScenePlaced and true otherwise
        /// </summary>
        public bool DestroyGameObject;
        private byte m_DestroyFlags;

        internal int DeferredDespawnTick;
        // Temporary until we make this a list
        internal ulong TargetClientId;

        internal bool IsDistributedAuthority;

        private const byte k_ClientTargetedDestroy = 0x01;
        private const byte k_DeferredDespawn = 0x02;

        internal bool IsTargetedDestroy
        {
            get => GetFlag(k_ClientTargetedDestroy);

            set => SetFlag(value, k_ClientTargetedDestroy);
        }

        private bool IsDeferredDespawn
        {
            get => GetFlag(k_DeferredDespawn);

            set => SetFlag(value, k_DeferredDespawn);
        }

        private bool GetFlag(int flag)
        {
            return (m_DestroyFlags & flag) != 0;
        }

        private void SetFlag(bool set, byte flag)
        {
            if (set) { m_DestroyFlags = (byte)(m_DestroyFlags | flag); }
            else { m_DestroyFlags = (byte)(m_DestroyFlags & ~flag); }
        }

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            // Set deferred despawn flag
            IsDeferredDespawn = DeferredDespawnTick > 0;

            BytePacker.WriteValueBitPacked(writer, NetworkObjectId);

            if (IsDistributedAuthority)
            {
                writer.WriteByteSafe(m_DestroyFlags);

                if (IsTargetedDestroy)
                {
                    BytePacker.WriteValueBitPacked(writer, TargetClientId);
                }

                if (targetVersion < k_OptimizeDestroyObjectMessage || IsDeferredDespawn)
                {
                    BytePacker.WriteValueBitPacked(writer, DeferredDespawnTick);
                }
            }

            if (targetVersion < k_OptimizeDestroyObjectMessage)
            {
                writer.WriteValueSafe(DestroyGameObject);
            }
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }

            ByteUnpacker.ReadValueBitPacked(reader, out NetworkObjectId);
            if (networkManager.DistributedAuthorityMode)
            {
                reader.ReadByteSafe(out m_DestroyFlags);
                if (IsTargetedDestroy)
                {
                    ByteUnpacker.ReadValueBitPacked(reader, out TargetClientId);
                }

                if (receivedMessageVersion < k_OptimizeDestroyObjectMessage || IsDeferredDespawn)
                {
                    ByteUnpacker.ReadValueBitPacked(reader, out DeferredDespawnTick);
                }
            }

            if (receivedMessageVersion < k_OptimizeDestroyObjectMessage)
            {
                reader.ReadValueSafe(out DestroyGameObject);
            }

            if (networkManager.SpawnManager.SpawnedObjects.ContainsKey(NetworkObjectId))
            {
                return true;
            }

            // Client-Server mode we always defer where in distributed authority mode we only defer if it is not a targeted destroy
            if (!networkManager.DistributedAuthorityMode || (networkManager.DistributedAuthorityMode && !IsTargetedDestroy))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnSpawn, NetworkObjectId, reader, ref context, k_Name);
            }
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            networkManager.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjectId, out var networkObject);

            // The DAHost needs to forward despawn messages to the other clients
            if (networkManager.DAHost)
            {
                HandleDAHostForwardMessage(context.SenderId, ref networkManager, networkObject);

                // DAHost adds the object to the queue only if it is not a targeted destroy, or it is and the target is the DAHost client.
                if (networkObject && DeferredDespawnTick > 0 && (!IsTargetedDestroy || (IsTargetedDestroy && TargetClientId == 0)))
                {
                    HandleDeferredDespawn(ref networkManager, ref networkObject);
                    return;
                }
            }

            // If this NetworkObject does not exist on this instance then exit early
            if (!networkObject)
            {
                if (networkManager.LogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogWarning($"[{nameof(DestroyObjectMessage)}] Received destroy object message for NetworkObjectId ({NetworkObjectId}) on Client-{networkManager.LocalClientId}, but that {nameof(NetworkObject)} does not exist!");
                }
                return;
            }

            if (networkManager.DistributedAuthorityMode)
            {
                // If we are deferring the despawn, then add it to the deferred despawn queue
                // If DAHost has reached this point, it is not valid to add to the queue
                if (DeferredDespawnTick > 0 && !networkManager.DAHost)
                {
                    HandleDeferredDespawn(ref networkManager, ref networkObject);
                    return;
                }

                // If this is targeted and we are not the target, then just update our local observers for this object
                if (IsTargetedDestroy && TargetClientId != networkManager.LocalClientId)
                {
                    networkObject.Observers.Remove(TargetClientId);
                    return;
                }
            }

            // Otherwise just despawn the NetworkObject right now
            networkManager.SpawnManager.OnDespawnNonAuthorityObject(networkObject);
            networkManager.NetworkMetrics.TrackObjectDestroyReceived(context.SenderId, networkObject, context.MessageSize);
        }

        /// <summary>
        /// Handles forwarding the <see cref="DestroyObjectMessage"/> when acting as the DA Host
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleDAHostForwardMessage(ulong senderId, ref NetworkManager networkManager, NetworkObject networkObject)
        {
            var message = new DestroyObjectMessage
            {
                NetworkObjectId = NetworkObjectId,
                DestroyGameObject = DestroyGameObject,
                IsDistributedAuthority = true,
                IsTargetedDestroy = IsTargetedDestroy,
                TargetClientId = TargetClientId, // Just always populate this value whether we write it or not
                DeferredDespawnTick = DeferredDespawnTick,
            };
            var ownerClientId = networkObject == null ? senderId : networkObject.OwnerClientId;
            var clientIds = networkObject == null ? networkManager.ConnectedClientsIds.ToList() : networkObject.Observers.ToList();

            foreach (var clientId in clientIds)
            {
                if (clientId != networkManager.LocalClientId && clientId != ownerClientId)
                {
                    networkManager.ConnectionManager.SendMessage(ref message, NetworkDelivery.ReliableSequenced, clientId);
                }
            }
        }

        /// <summary>
        /// Handles adding to the deferred despawn queue when the <see cref="DestroyObjectMessage"/> indicates a deferred despawn
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleDeferredDespawn(ref NetworkManager networkManager, ref NetworkObject networkObject)
        {
            networkObject.DeferredDespawnTick = DeferredDespawnTick;
            var hasCallback = networkObject.OnDeferredDespawnComplete != null;
            networkManager.SpawnManager.DeferDespawnNetworkObject(NetworkObjectId, DeferredDespawnTick, hasCallback);
        }
    }
}
