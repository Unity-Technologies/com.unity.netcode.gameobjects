using UnityEngine;

namespace Unity.Netcode
{
    internal struct ParentSyncMessage : INetworkMessage
    {
        public int Version => 0;

        public ulong NetworkObjectId;

        private byte m_BitField;

        public bool WorldPositionStays
        {
            get => ByteUtility.GetBit(m_BitField, 0);
            set => ByteUtility.SetBit(ref m_BitField, 0, value);
        }

        //If(Metadata.IsReparented)
        public bool IsLatestParentSet
        {
            get => ByteUtility.GetBit(m_BitField, 1);
            set => ByteUtility.SetBit(ref m_BitField, 1, value);
        }

        //If(IsLatestParentSet)
        public ulong? LatestParent;

        // Is set when the parent should be removed (similar to IsReparented functionality but only for removing the parent)
        public bool RemoveParent
        {
            get => ByteUtility.GetBit(m_BitField, 2);
            set => ByteUtility.SetBit(ref m_BitField, 2, value);
        }

        // These additional properties are used to synchronize clients with the current position,
        // rotation, and scale after parenting/de-parenting (world/local space relative). This
        // allows users to control the final child's transform values without having to have a
        // NetworkTransform component on the child. (i.e. picking something up)
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            BytePacker.WriteValueBitPacked(writer, NetworkObjectId);
            writer.WriteValueSafe(m_BitField);
            if (!RemoveParent)
            {
                if (IsLatestParentSet)
                {
                    BytePacker.WriteValueBitPacked(writer, LatestParent.Value);
                }
            }

            // Whether parenting or removing a parent, we always update the position, rotation, and scale
            writer.WriteValueSafe(Position);
            writer.WriteValueSafe(Rotation);
            writer.WriteValueSafe(Scale);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }

            ByteUnpacker.ReadValueBitPacked(reader, out NetworkObjectId);
            reader.ReadValueSafe(out m_BitField);
            if (!RemoveParent)
            {
                if (IsLatestParentSet)
                {
                    ByteUnpacker.ReadValueBitPacked(reader, out ulong latestParent);
                    LatestParent = latestParent;
                }
            }

            // Whether parenting or removing a parent, we always update the position, rotation, and scale
            reader.ReadValueSafe(out Position);
            reader.ReadValueSafe(out Rotation);
            reader.ReadValueSafe(out Scale);

            if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(NetworkObjectId))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnSpawn, NetworkObjectId, reader, ref context);
                return false;
            }

            // If the target parent does not exist, then defer this message until it does.
            if (LatestParent.HasValue && !networkManager.SpawnManager.SpawnedObjects.ContainsKey(LatestParent.Value))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnSpawn, LatestParent.Value, reader, ref context);
                return false;
            }

            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            var networkObject = networkManager.SpawnManager.SpawnedObjects[NetworkObjectId];
            networkObject.SetNetworkParenting(LatestParent, WorldPositionStays);
            networkObject.ApplyNetworkParenting(RemoveParent);


            // This check is primarily for client-server network topologies when the motion model is owner authoritative:
            // When SyncOwnerTransformWhenParented is enabled, then always apply the transform values.
            // When SyncOwnerTransformWhenParented is disabled, then only synchronize the transform on non-owner instances.
            if (networkObject.SyncOwnerTransformWhenParented || (!networkObject.SyncOwnerTransformWhenParented && !networkObject.IsOwner))
            {
                // We set all of the transform values after parenting as they are
                // the values of the server-side post-parenting transform values
                if (!WorldPositionStays)
                {
                    networkObject.transform.localPosition = Position;
                    networkObject.transform.localRotation = Rotation;
                }
                else
                {
                    networkObject.transform.position = Position;
                    networkObject.transform.rotation = Rotation;
                }
            }
            networkObject.transform.localScale = Scale;

            // If client side parenting is enabled and this is the server instance, then notify the rest of the connected clients that parenting has taken place.
            if (networkObject.AllowOwnerToParent && context.SenderId == networkObject.OwnerClientId && networkManager.IsServer)
            {
                var size = 0;
                var message = this;
                foreach (var client in networkManager.ConnectedClients)
                {
                    if (client.Value.ClientId == networkObject.OwnerClientId || client.Value.ClientId == networkManager.LocalClientId || !networkObject.IsNetworkVisibleTo(client.Value.ClientId))
                    {
                        continue;
                    }
                    size = networkManager.ConnectionManager.SendMessage(ref message, NetworkDelivery.ReliableSequenced, client.Value.ClientId);
                    networkManager.NetworkMetrics.TrackOwnershipChangeSent(client.Key, networkObject, size);
                }
            }
        }
    }
}
