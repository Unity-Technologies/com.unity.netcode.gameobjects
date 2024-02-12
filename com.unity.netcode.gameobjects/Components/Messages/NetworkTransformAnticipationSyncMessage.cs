using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode
{
    internal struct NetworkTransformAnticipationSyncMessage : INetworkMessage
    {
        public int Version => 0;
        public ulong NetworkObjectId;
        public int NetworkBehaviourId;

        private AnticipatedNetworkTransform m_ReceiverNetworkTransform;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            BytePacker.WriteValueBitPacked(writer, NetworkObjectId);
            BytePacker.WriteValueBitPacked(writer, NetworkBehaviourId);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = context.SystemOwner as NetworkManager;
            if (networkManager == null)
            {
                Debug.LogError($"[{nameof(NetworkTransformMessage)}] System owner context was not of type {nameof(NetworkManager)}!");
                return false;
            }
            ByteUnpacker.ReadValueBitPacked(reader, out NetworkObjectId);
            if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(NetworkObjectId))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnSpawn, NetworkObjectId, reader, ref context);
                return false;
            }
            // Get the behaviour index
            ByteUnpacker.ReadValueBitPacked(reader, out NetworkBehaviourId);

            var networkObject = networkManager.SpawnManager.SpawnedObjects[NetworkObjectId];

            // Get the target NetworkTransform
            m_ReceiverNetworkTransform = networkObject.ChildNetworkBehaviours[NetworkBehaviourId] as AnticipatedNetworkTransform;

            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            if (m_ReceiverNetworkTransform == null)
            {
                Debug.LogError($"[{nameof(NetworkTransformAnticipationSyncMessage)}][Dropped] Reciever {nameof(AnticipatedNetworkTransform)} was not set!");
                return;
            }

            m_ReceiverNetworkTransform.SetLastAuthorityUpdateTick();
        }
    }
}