#if UNIFIED_NETCODE
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

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

        private Dictionary<int, NetcodeConnection> m_NewConnections = new Dictionary<int, NetcodeConnection>();
        
        protected override void OnUpdate()
        {
            var isServer = World.IsServer();
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
//            var networkManager = NetworkManager.Singleton;
            foreach (var networkManager in GameObject.FindObjectsByType<NetworkManager>())
            {
                foreach (var (networkId, connectionState, entity) in SystemAPI.Query<NetworkId, ConnectionState>()
                             .WithNone<NetworkStreamConnection>().WithEntityAccess())
                {
                    commandBuffer.RemoveComponent<ConnectionState>(entity);
                    m_TempConnections.Add(new NetcodeConnection
                        { World = World, Entity = entity, NetworkId = networkId.Value });
                }

                foreach (var con in m_TempConnections)
                {
                    NetworkManager.OnNetCodeDisconnect?.Invoke(con);
                }

                m_TempConnections.Clear();

                // TODO: We should figure out how to associate the N4E NetworkId with the NGO ClientId
                foreach (var (networkId, entity) in SystemAPI.Query<NetworkId>().WithAll<NetworkStreamConnection>()
                             .WithNone<NetworkStreamInGame>().WithEntityAccess())
                {
                    if (!m_NewConnections.ContainsKey(networkId.Value))
                    {
                        var newConnection = new NetcodeConnection
                            { World = World, Entity = entity, NetworkId = networkId.Value };
                        m_NewConnections.Add(networkId.Value, newConnection);
                    }
                }

                // If we have any pending connections
                if (m_NewConnections.Count > 0)
                {
                    foreach (var entry in m_NewConnections)
                    {
                        // Server: always connect
                        // Client: wait until we have synchronized before announcing we are ready to receive snapshots
                        if (networkManager.IsServer || (!networkManager.IsServer && networkManager.IsConnectedClient))
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

                // If the local NetworkManager is shutting down or no longer connected, then
                // make sure we have disconnected all known connections.
                if (networkManager.ShutdownInProgress || !networkManager.IsListening)
                {
                    foreach (var (networkId, entity) in SystemAPI.Query<NetworkId>().WithEntityAccess())
                    {
                        commandBuffer.RemoveComponent<ConnectionState>(entity);
                        NetworkManager.OnNetCodeDisconnect?.Invoke(new NetcodeConnection
                            { World = World, Entity = entity, NetworkId = networkId.Value });
                    }
                }
            }
            
            commandBuffer.Playback(EntityManager);
        }

        /// <summary>
        /// Always disconnect all known connections when being destroyed.
        /// </summary>
        protected override void OnDestroy()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (networkId, entity) in SystemAPI.Query<NetworkId>().WithEntityAccess())
            {
                commandBuffer.RemoveComponent<ConnectionState>(entity);
                NetworkManager.OnNetCodeDisconnect?.Invoke(new NetcodeConnection { World = World, Entity = entity, NetworkId = networkId.Value });
            }
            commandBuffer.Playback(EntityManager);
            base.OnDestroy();
        }
    }
}
#endif
