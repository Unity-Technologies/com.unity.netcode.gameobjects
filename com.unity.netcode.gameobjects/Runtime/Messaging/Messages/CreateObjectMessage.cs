using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode
{
    internal struct CreateObjectMessage : INetworkMessage
    {
        public int Version => 0;

        public NetworkObject.SceneObject ObjectInfo;
        private FastBufferReader m_ReceivedNetworkVariableData;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            // Debug.Log($"CreateObjectMessage Serialize [v={targetVersion}] [H={ObjectInfo.Hash}] [id={ObjectInfo.NetworkObjectId}]");
            ObjectInfo.Serialize(writer);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }

            ObjectInfo.Deserialize(reader);
            if (!networkManager.NetworkConfig.ForceSamePrefabs && !networkManager.SpawnManager.HasPrefab(ObjectInfo))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnAddPrefab, ObjectInfo.Hash, reader, ref context);
                return false;
            }
            m_ReceivedNetworkVariableData = reader;
            // Debug.Log($"CreateObjectMessage Deserialize [v={receivedMessageVersion}] [H={ObjectInfo.Hash}] [id={ObjectInfo.NetworkObjectId}]");
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            // If a client receives a create object message and it is still synchronizing, then defer the object creation until it has finished synchronizing
            if (networkManager.SceneManager.ShouldDeferCreateObject())
            {
                networkManager.SceneManager.DeferCreateObject(context.SenderId, context.MessageSize, ObjectInfo, m_ReceivedNetworkVariableData);
            }
            else
            {
                CreateObject(ref networkManager, context.SenderId, context.MessageSize, ObjectInfo, m_ReceivedNetworkVariableData);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CreateObject(ref NetworkManager networkManager, ulong senderId, uint messageSize, NetworkObject.SceneObject sceneObject, FastBufferReader networkVariableData)
        {
            try
            {
                var networkObject = NetworkObject.AddSceneObject(sceneObject, networkVariableData, networkManager);
                networkManager.NetworkMetrics.TrackObjectSpawnReceived(senderId, networkObject, messageSize);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }
    }
}
