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
        // NOTE: Packing and unpacking all serialized properties helps to offset the
        // additional increased message size.
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public void Serialize(FastBufferWriter writer)
        {
            BytePacker.WriteValuePacked(writer, NetworkObjectId);
            BytePacker.WriteValuePacked(writer, WorldPositionStays);
            BytePacker.WriteValuePacked(writer, IsLatestParentSet);

            if (IsLatestParentSet)
            {
                BytePacker.WriteValuePacked(writer, (ulong)LatestParent);
            }

            BytePacker.WriteValuePacked(writer, Position);
            BytePacker.WriteValuePacked(writer, Rotation);
            BytePacker.WriteValuePacked(writer, Scale);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }
            ByteUnpacker.ReadValuePacked(reader, out NetworkObjectId);
            ByteUnpacker.ReadValuePacked(reader, out WorldPositionStays);
            ByteUnpacker.ReadValuePacked(reader, out IsLatestParentSet);

            if (IsLatestParentSet)
            {
                ByteUnpacker.ReadValuePacked(reader, out ulong latestParent);
                LatestParent = latestParent;
            }

            ByteUnpacker.ReadValuePacked(reader, out Position);
            ByteUnpacker.ReadValuePacked(reader, out Rotation);
            ByteUnpacker.ReadValuePacked(reader, out Scale);

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
            // We always set m_IsReparented to true, ApplyNetworkParenting will reset this
            // value if it is removing the parent.
            // TODO: Determine if m_IsReparented is still needed.
            networkObject.SetNetworkParenting(true, LatestParent, WorldPositionStays);
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
