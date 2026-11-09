#if UNIFIED_NETCODE
using Unity.Collections;
using Unity.Entities;
using Unity.Netcode.Components;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Gives a client the instance of a hybrid prefab from its <see cref="INetworkPrefabInstanceHandler"/> instead of letting N4E instantiate it.
    /// </summary>
    /// <remarks>
    /// The ghost arrives before NGO's spawn message, so the handler receives <see cref="NetworkManager.ServerClientId"/> as the owner.
    /// </remarks>
    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    [UpdateAfter(typeof(GhostUpdateSystem))]
    [UpdateBefore(typeof(GhostGameObjectSpawnSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    internal partial class HybridPrefabHandlerSpawnSystem : SystemBase
    {
        private EntityQuery m_PendingSpawnQuery;
        private EntityQuery m_HybridNetworkManagerQuery;

        protected override void OnCreate()
        {
            if (World.IsHost())
            {
                Enabled = false;
                return;
            }
            m_PendingSpawnQuery = GetEntityQuery(ComponentType.ReadOnly<PendingClientGameObjectSpawn>(), ComponentType.ReadOnly<GhostInstance>());
            m_HybridNetworkManagerQuery = GetEntityQuery(ComponentType.ReadOnly<HybridNetworkManager>());
            RequireForUpdate(m_PendingSpawnQuery);
            RequireForUpdate(m_HybridNetworkManagerQuery);
        }

        protected override void OnUpdate()
        {
            var networkManager = m_HybridNetworkManagerQuery.GetSingleton<HybridNetworkManager>().NetworkManager.Value;
            if (!networkManager || networkManager.ShutdownInProgress)
            {
                return;
            }

            using var pendingEntities = m_PendingSpawnQuery.ToEntityArray(Allocator.Temp);
            using var ghostInstances = m_PendingSpawnQuery.ToComponentDataArray<GhostInstance>(Allocator.Temp);
            var prefabs = EntityManager.GetBuffer<GhostCollectionPrefab>(SystemAPI.GetSingletonEntity<GhostCollection>()).ToNativeArray(Allocator.Temp);

            for (int i = 0; i < pendingEntities.Length; i++)
            {
                var entity = pendingEntities[i];
                var prefabEntity = prefabs[ghostInstances[i].ghostType].GhostPrefab;
                var prefabId = EntityManager.GetComponentData<GhostGameObjectLink>(prefabEntity).AssociatedGameObject;
                var prefab = Resources.EntityIdToObject(prefabId) as GameObject;
                if (!prefab || !prefab.TryGetComponent<NetworkObject>(out var prefabNetworkObject) || !networkManager.PrefabHandler.ContainsHandler(prefabNetworkObject.GlobalObjectIdHash))
                {
                    continue;
                }

                var localTransform = EntityManager.GetComponentData<LocalTransform>(entity);
                var networkObject = networkManager.PrefabHandler.HandleNetworkPrefabSpawn(prefabNetworkObject.GlobalObjectIdHash, NetworkManager.ServerClientId, localTransform.Position, localTransform.Rotation);
                if (!networkObject || !networkObject.HasGhost || networkObject.GhostObject.IsSpawned)
                {
                    Debug.LogError($"[{nameof(INetworkPrefabInstanceHandler)}] The handler for {prefab.name} did not return an unspawned instance of it. N4E instantiates this ghost instead.");
                    continue;
                }

                var gameObject = networkObject.gameObject;
                var link = new GhostGameObjectLink(gameObject.GetEntityId(), gameObject.transform.GetEntityId());
                // Released by GhostObject.Despawn in HybridPrefabHandlerDespawnSystem.
                var entityLink = GhostEntityMapping.AcquireEntityReferenceGameObject(link.AssociatedGameObject, link.AssociatedTransform, prefabId, World.Unmanaged, entity);
                EntityManager.SetComponentData(entity, link);
                EntityManager.RemoveComponent<PendingClientGameObjectSpawn>(entity);
                networkObject.GhostObject.ClientRuntimeInitialize(entityLink);

                // A reused instance does not run Start again, so it registers here.
                networkManager.SpawnManager.GhostSpawnManager.RegisterGhostBridge(networkObject.NetworkObjectBridge.NetworkObjectId.Value, networkObject);
            }
        }
    }

    /// <summary>
    /// Returns a client's hybrid instance to its <see cref="INetworkPrefabInstanceHandler"/> when N4E despawns its ghost, instead of letting N4E destroy it.
    /// </summary>
    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    [UpdateAfter(typeof(GhostDespawnSystem))]
    [UpdateAfter(typeof(PredictedGhostDespawnSystem))]
    [UpdateAfter(typeof(GhostReceiveSystem))]
    [UpdateBefore(typeof(GhostGameObjectDespawnManagedSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    internal partial class HybridPrefabHandlerDespawnSystem : SystemBase
    {
        private EntityQuery m_DespawnTrackingQuery;
        private EntityQuery m_HybridNetworkManagerQuery;

        protected override void OnCreate()
        {
            if (World.IsHost())
            {
                Enabled = false;
                return;
            }
            m_DespawnTrackingQuery = GetEntityQuery(typeof(GameObjectDespawnTracking));
            m_HybridNetworkManagerQuery = GetEntityQuery(ComponentType.ReadOnly<HybridNetworkManager>());
            RequireForUpdate(m_DespawnTrackingQuery);
            RequireForUpdate(m_HybridNetworkManagerQuery);
        }

        protected override void OnUpdate()
        {
            var networkManager = m_HybridNetworkManagerQuery.GetSingleton<HybridNetworkManager>().NetworkManager.Value;
            if (!networkManager)
            {
                return;
            }

            var trackingEntity = m_DespawnTrackingQuery.GetSingletonEntity();
            // Copied out, as GhostGameObjectDespawnManagedSystem does, because of a DynamicBuffer safety handle bug.
            var despawns = EntityManager.GetBuffer<GameObjectDespawnTracking>(trackingEntity, isReadOnly: true).ToNativeArray(Allocator.Temp);
            if (despawns.Length == 0)
            {
                return;
            }

            EntityManager.CompleteDependencyBeforeRW<SpawnedGhostEntityMap>();
            var spawnedGhostMap = SystemAPI.GetSingletonRW<SpawnedGhostEntityMap>().ValueRO.SpawnedGhostMapRW;
            var spawnManager = networkManager.SpawnManager;
            var handled = new NativeList<int>(despawns.Length, Allocator.Temp);

            for (int i = 0; i < despawns.Length; i++)
            {
                var despawn = despawns[i].oneDespawn;
                var gameObjectId = EntityManager.GetComponentData<GhostGameObjectLink>(despawn.entity).AssociatedGameObject;
                var gameObject = Resources.EntityIdToObject(gameObjectId) as GameObject;
                if (!gameObject || !gameObject.TryGetComponent<NetworkObject>(out var networkObject) || !networkObject.HasGhost || !networkManager.PrefabHandler.ContainsHandler(networkObject))
                {
                    continue;
                }

                if (spawnedGhostMap.TryGetValue(despawn.ghost, out var mapped) && mapped == despawn.entity)
                {
                    spawnedGhostMap.Remove(despawn.ghost);
                }

                var networkObjectId = networkObject.NetworkObjectId;
                // The ghost can despawn before NGO's destroy message arrives, the same case NetworkObject.OnDestroy handles.
                if (networkObject.IsSpawned && spawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var spawned) && spawned == networkObject)
                {
                    spawnManager.OnDespawnObject(networkObject, false);
                }
                spawnManager.GhostSpawnManager.MarkNetworkObjectAsDestroying(networkObjectId);

                NetworkSpawnManager.DespawnGhostForPrefabHandler(networkObject);
                networkManager.PrefabHandler.HandleNetworkPrefabDestroy(networkObject);
                handled.Add(i);
            }

            if (handled.Length == 0)
            {
                return;
            }
            var buffer = EntityManager.GetBuffer<GameObjectDespawnTracking>(trackingEntity);
            for (int i = handled.Length - 1; i >= 0; i--)
            {
                buffer.RemoveAt(handled[i]);
            }
        }
    }
}
#endif
