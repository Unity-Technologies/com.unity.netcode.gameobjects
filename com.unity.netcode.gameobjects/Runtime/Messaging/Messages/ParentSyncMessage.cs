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

        // These additional properties are used to synchronize clients with the current
        // local position and rotation only when WorldPositionStays is false.  This allows
        // a user to not be required to use a NetworkTransform for NetworkObjects that rarely
        // change their default local space position and rotation (i.e. an item that is picked
        // up and parented under a player).  Upon de-parenting (dropped), the user might want
        // to re-apply the original local space position and rotation upon the object being
        // picked up again.  These values are set by the NetworkObject's transform when being
        // serialized and sets the NetworkObject's transform when being deserialized.
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
