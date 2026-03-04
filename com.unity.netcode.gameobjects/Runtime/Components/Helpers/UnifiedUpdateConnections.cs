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

        internal float ConnectedTime;

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

        private Dictionary<int, NetcodeConnection> m_NewConnections = new Dictionary<int, NetcodeConnection>();

        protected override void OnUpdate()
        {
            var isServer = World.IsServer();
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
                // TODO-Unified: For new connections, we have a delay before the N4E in-game state for the client to provide time for the NGO side of the client to synchronize.
                // Note: Once both are using the same transport we should be able to get the transport id and determine the NGO assigned client-id and at that point once the
                // client has signaled that it has synchronized (or has been sent the synchronization data) we finalize the in-game connection state (or something along those lines).
                if (!m_NewConnections.ContainsKey(networkId.Value))
                {
                    var delayTime = 0.0f;// isServer ? 0.2f : 0.1f;
                    var newConnection = new NetcodeConnection { World = World, Entity = entity, NetworkId = networkId.Value, ConnectedTime = UnityEngine.Time.realtimeSinceStartup + delayTime};
                    m_NewConnections.Add(networkId.Value, newConnection);
                }
            }

            // If we have any pending connections
            if (m_NewConnections.Count > 0)
            {
                foreach (var entry in m_NewConnections)
                {
                    // Check if the delay time has passed.
                    if (entry.Value.ConnectedTime < UnityEngine.Time.realtimeSinceStartup)
                    {
                        // Set the connection in-game
                        commandBuffer.AddComponent<NetworkStreamInGame>(entry.Value.Entity);
                        commandBuffer.AddComponent(entry.Value.Entity, default(ConnectionState));
                        NetworkManager.OnNetCodeConnect?.Invoke(entry.Value);
                        m_TempConnections.Add(entry.Value);
                    }
                }
                // Remove any connections that have "gone in-game".
                foreach (var connection in m_TempConnections)
                {
                    m_NewConnections.Remove(connection.NetworkId);
                }
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
