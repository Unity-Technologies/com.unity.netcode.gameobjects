using UnityEngine;

namespace Unity.Netcode.Messages
{
    internal struct CreateObjectMessage : INetworkMessage
    {
        public NetworkObject.SceneObject ObjectInfo;
        
        public void Serialize(ref FastBufferWriter writer)
        {
            ObjectInfo.Serialize(ref writer);
        }

        public static void Receive(ref FastBufferReader reader, NetworkContext context)
        {
            var networkManager = (NetworkManager) context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return;
            }
            var message = new CreateObjectMessage();
            message.ObjectInfo.Deserialize(ref reader);
            message.Handle(context.SenderId, ref reader, networkManager);
        }

        public void Handle(ulong senderId, ref FastBufferReader reader, NetworkManager networkManager)
        {
            var networkObject = NetworkObject.AddSceneObject(ObjectInfo, ref reader, networkManager);
            networkManager.NetworkMetrics.TrackObjectSpawnReceived(senderId, networkObject.NetworkObjectId, networkObject.name, reader.Length);
        }
    }
}