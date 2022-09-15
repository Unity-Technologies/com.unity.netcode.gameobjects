using UnityEngine;

namespace Unity.Netcode
{
    internal struct ParentSyncMessage : INetworkMessage
    {
        public ulong NetworkObjectId;

        public bool IsReparented;

        public bool WorldPositionStays;

        //If(Metadata.IsReparented)
        public bool IsLatestParentSet;

        //If(IsLatestParentSet)
        public ulong? LatestParent;

        // Used to synchronize clients with the current local position and rotation
        // when WorldPositionStays is false
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;

        public void Serialize(FastBufferWriter writer)
        {
            writer.WriteValueSafe(NetworkObjectId);
            writer.WriteValueSafe(IsReparented);
            writer.WriteValueSafe(WorldPositionStays);

            // if the world position is not "staying" we want to write the current local position and
            // rotation of the NetworkObject before parenting (to preserve any changes made if there
            // is no NetworkTransform component attached to the child).
            if (!WorldPositionStays)
            {
                writer.WriteValueSafe(LocalPosition);
                writer.WriteValueSafe(LocalRotation);
            }

            if (IsReparented)
            {
                writer.WriteValueSafe(IsLatestParentSet);
                if (IsLatestParentSet)
                {
                    writer.WriteValueSafe((ulong)LatestParent);
                }
            }
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }

            reader.ReadValueSafe(out NetworkObjectId);
            reader.ReadValueSafe(out IsReparented);
            reader.ReadValueSafe(out WorldPositionStays);
            if (!WorldPositionStays)
            {
                reader.ReadValueSafe(out LocalPosition);
                reader.ReadValueSafe(out LocalRotation);
            }

            if (IsReparented)
            {

                reader.ReadValueSafe(out IsLatestParentSet);
                if (IsLatestParentSet)
                {
                    reader.ReadValueSafe(out ulong latestParent);
                    LatestParent = latestParent;
                }
            }

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
            networkObject.SetNetworkParenting(IsReparented, LatestParent, WorldPositionStays);

            // if the world position is not "staying" we want to set the local position and rotation
            // of the NetworkObject before parenting (to preserve any changes made if there is no
            // NetworkTransform component attached to the child)
            if (!WorldPositionStays)
            {
                networkObject.transform.localPosition = LocalPosition;
                networkObject.transform.localRotation = LocalRotation;
            }
            networkObject.ApplyNetworkParenting();
        }
    }
}
