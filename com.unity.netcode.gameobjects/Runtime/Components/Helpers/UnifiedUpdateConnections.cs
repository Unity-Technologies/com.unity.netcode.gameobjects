#if UNIFIED_NETCODE
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Unity.Netcode.Components
{

    public struct NetcodeConnection
    {
        internal World World;
        internal Entity Entity;
        public int NetworkId;

        public bool IsServer => World.IsServer();
        public void GoInGame()
        {
            World.EntityManager.AddComponentData(Entity, default(NetworkStreamInGame));
        }
        public void SendMessage<T>(T message) where T : unmanaged, IRpcCommand
        {
            var req = World.EntityManager.CreateEntity();
            World.EntityManager.AddComponentData(req, new SendRpcCommandRequest { TargetConnection = Entity });
            World.EntityManager.AddComponentData(req, message);
        }
    }

    internal partial class UnifiedUpdateConnections : SystemBase
    {
        private List<NetcodeConnection> m_TempConnections = new List<NetcodeConnection>();
        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (networkId, connectionState, entity) in SystemAPI.Query<NetworkId, ConnectionState>().WithNone<NetworkStreamConnection>().WithEntityAccess())
            {
                commandBuffer.RemoveComponent<ConnectionState>(entity);
                m_TempConnections.Add(new NetcodeConnection { World = World, Entity = entity, NetworkId = networkId.Value });
            }
            foreach (var con in m_TempConnections)
            {
                NetworkManager.OnNetCodeDisconnect?.Invoke(con);
            }

            m_TempConnections.Clear();

            foreach (var (networkId, entity) in SystemAPI.Query<NetworkId>().WithAll<NetworkStreamConnection>().WithNone<NetworkStreamInGame>().WithEntityAccess())
            {
                commandBuffer.AddComponent<NetworkStreamInGame>(entity);
                commandBuffer.AddComponent(entity, default(ConnectionState));
                m_TempConnections.Add(new NetcodeConnection { World = World, Entity = entity, NetworkId = networkId.Value });
            }

            foreach (var con in m_TempConnections)
            {
                NetworkManager.OnNetCodeConnect?.Invoke(con);
            }

            m_TempConnections.Clear();

            commandBuffer.Playback(EntityManager);
        }

        protected override void OnDestroy()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (networkId, entity) in SystemAPI.Query<NetworkId>().WithEntityAccess())
            {
                commandBuffer.RemoveComponent<ConnectionState>(entity);
                // TODO: maybe disconnect reason?
                m_TempConnections.Add(new NetcodeConnection { World = World, Entity = entity, NetworkId = networkId.Value });
            }
            foreach (var con in m_TempConnections)
            {
                NetworkManager.OnNetCodeDisconnect?.Invoke(con);
            }
            commandBuffer.Playback(EntityManager);
            base.OnDestroy();
        }
    }
}
#endif
