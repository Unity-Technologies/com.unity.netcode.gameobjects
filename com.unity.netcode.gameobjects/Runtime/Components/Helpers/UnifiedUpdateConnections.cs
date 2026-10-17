#if UNIFIED_NETCODE
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

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

    /// <summary>
    /// This is used to track the <see cref="NetworkManager"/>, as a singleton, when running a hybrid session.
    /// </summary>
    internal struct HybridNetworkManager : IComponentData
    {
        public UnityObjectRef<NetworkManager> NetworkManager;
    }

    // Only exists in client and server worlds, and only updates in a world created for a hybrid session.
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    internal partial class UnifiedUpdateConnections : SystemBase
    {
        private List<NetcodeConnection> m_TempConnections = new List<NetcodeConnection>();

        private Dictionary<int, NetcodeConnection> m_NewConnections = new Dictionary<int, NetcodeConnection>();

        private EntityQuery m_HybridNetworkManagerQuery;

        protected override void OnCreate()
        {
            m_HybridNetworkManagerQuery = GetEntityQuery(ComponentType.ReadOnly<HybridNetworkManager>());
            RequireForUpdate(m_HybridNetworkManagerQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var networkManager = m_HybridNetworkManagerQuery.GetSingleton<HybridNetworkManager>().NetworkManager.Value;
            // A missing NetworkManager counts as shutting down: the world outlives it if ShutdownInternal throws before disposing the world.
            var isShuttingDown = !networkManager || networkManager.ShutdownInProgress || !networkManager.IsListening;
            foreach (var (networkId, connectionState, entity) in SystemAPI.Query<NetworkId, ConnectionState>().WithNone<NetworkStreamConnection>().WithEntityAccess())
            {
                commandBuffer.RemoveComponent<ConnectionState>(entity);
                m_TempConnections.Add(new NetcodeConnection
                {
                    World = World,
                    Entity = entity,
                    NetworkId = networkId.Value
                });
            }

            foreach (var con in m_TempConnections)
            {
                NetworkManager.OnNetCodeDisconnect?.Invoke(con);
            }

            m_TempConnections.Clear();

            // TODO: We should figure out how to associate the N4E NetworkId with the NGO ClientId
            if (!isShuttingDown)
            {
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
            }

            // If we have any pending connections
            if (!isShuttingDown && m_NewConnections.Count > 0)
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
            if (isShuttingDown)
            {
                // Only in-game connections have ConnectionState, and removing it here means each one is only reported once.
                foreach (var (networkId, entity) in SystemAPI.Query<NetworkId>().WithAll<ConnectionState>().WithEntityAccess())
                {
                    commandBuffer.RemoveComponent<ConnectionState>(entity);
                    NetworkManager.OnNetCodeDisconnect?.Invoke(new NetcodeConnection
                    { World = World, Entity = entity, NetworkId = networkId.Value });
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
