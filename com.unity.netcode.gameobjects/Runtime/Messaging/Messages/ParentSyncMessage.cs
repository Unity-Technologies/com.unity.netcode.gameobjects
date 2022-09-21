using UnityEngine;

namespace Unity.Netcode
{
    internal struct ParentSyncMessage : INetworkMessage
    {
        public ulong NetworkObjectId;

        public bool WorldPositionStays;

        //If(Metadata.IsReparented)
        public bool IsLatestParentSet;

        //If(IsLatestParentSet)
        public ulong? LatestParent;

        // These additional properties are used to synchronize clients with the current position
        // , rotation, and scale after parenting/de-parenting (world/local space relative). This
        // allows users to control the final child's transform values without having to have
        // a NetworkTransform component on the child. (i.e. picking something up)
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public void Serialize(FastBufferWriter writer)
        {
            BytePacker.WriteValuePacked(writer, NetworkObjectId);
            writer.WriteValueSafe(WorldPositionStays);
            writer.WriteValueSafe(IsLatestParentSet);

            if (IsLatestParentSet)
            {
                BytePacker.WriteValuePacked(writer, (ulong)LatestParent);
            }

            writer.WriteValueSafe(Position);
            writer.WriteValueSafe(Rotation);
            writer.WriteValueSafe(Scale);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }

            ByteUnpacker.ReadValuePacked(reader, out NetworkObjectId);
            reader.ReadValueSafe(out WorldPositionStays);
            reader.ReadValueSafe(out IsLatestParentSet);

            if (IsLatestParentSet)
            {
                ByteUnpacker.ReadValuePacked(reader, out ulong latestParent);
                LatestParent = latestParent;
            }

            reader.ReadValueSafe(out Position);
            reader.ReadValueSafe(out Rotation);
            reader.ReadValueSafe(out Scale);

            if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(NetworkObjectId))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredMessageManager.TriggerType.OnSpawn, NetworkObjectId, reader, ref context);
                return false;
            }
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            var networkObject = networkManager.SpawnManager.SpawnedObjects[NetworkObjectId];
            networkObject.SetNetworkParenting(LatestParent, WorldPositionStays);
            networkObject.ApplyNetworkParenting();
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
            networkObject.transform.localScale = Scale;
        }
    }
}
