#if UNIFIED_NETCODE
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Measurement harness (not a pass/fail behaviour test) used to size
    /// <see cref="NetCode.GhostSendSystemData.DefaultSnapshotPacketSize"/> for hybrid (NGO + N4E) mode.
    /// Spawns N hybrid ghosts, keeps every one of them dirty on every tick, and reads the N4E client-side
    /// snapshot metrics singleton for a fixed sample window. Results are emitted as "PKTSZ|" log lines.
    /// </summary>
    [TestFixture(HostOrServer.UnifiedHost)]
    [Explicit("Measurement harness, not a regression test. The 24 auto-expanded cases take ~162s, so it only runs when selected by name: -testFilter \".*UnifiedSnapshotPacketSizeMeasurement.*\"")]
    internal class UnifiedSnapshotPacketSizeMeasurement : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        // Delta-compression baselines need several snapshots to settle; the first ones are much larger.
        private const int k_WarmupSnapshots = 30;
        private const int k_SampleSnapshots = 100;
        private const int k_SpawnsPerFrame = 100;
        private const float k_SpawnTimeout = 240.0f;
        private const float k_SampleTimeout = 240.0f;

        private GameObject m_Prefab;
        private Transform[] m_Instances;
        private NetCode.GhostObject[] m_Ghosts;
        private float[] m_Phases;
        private int m_Frame;

        public UnifiedSnapshotPacketSizeMeasurement(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override bool OnSetVerboseDebug()
        {
            return false;
        }

        protected override IEnumerator OnSetup()
        {
            m_Instances = null;
            m_Ghosts = null;
            m_Phases = null;
            m_Frame = 0;
            // UnifiedHost sets m_AllPrefabsAsHybrid, so this yields a NetworkObject + GhostObject + NetworkObjectBridge prefab.
            m_Prefab = CreateNetworkObjectPrefab("PktSizeGhost");
            return base.OnSetup();
        }

        /// <summary>
        /// Every instance orbits on its own phase so that no chunk is ever unchanged. N4E static-optimizes
        /// unchanged chunks, so leaving these still would measure nothing.
        /// </summary>
        private void MoveAll()
        {
            if (m_Instances == null)
            {
                return;
            }
            m_Frame++;
            var time = m_Frame * 0.01f;
            for (int i = 0; i < m_Instances.Length; i++)
            {
                var instance = m_Instances[i];
                if (instance == null)
                {
                    continue;
                }
                var angle = time + m_Phases[i];
                var radius = 20.0f + (i % 17);
                var position = new Vector3(radius * Mathf.Cos(angle), (i % 32) * 0.5f, radius * Mathf.Sin(angle));
                var rotation = Quaternion.Euler(0.0f, angle * Mathf.Rad2Deg, 0.0f);
                instance.SetLocalPositionAndRotation(position, rotation);
                // On a single-world host the GameObject transform is also written by the presentation-time smoothing
                // system, so drive the authoritative LocalTransform directly as well.
                var ghost = m_Ghosts[i];
                if (ghost != null)
                {
                    ghost.Position = position;
                    ghost.Rotation = rotation;
                }
            }
        }

        private static Entity CreateMetricsSingleton(EntityManager entityManager)
        {
            var typeList = new NativeArray<ComponentType>(8, Allocator.Temp);
            typeList[0] = ComponentType.ReadWrite<NetCode.GhostMetricsMonitor>();
            typeList[1] = ComponentType.ReadWrite<NetCode.NetworkMetrics>();
            typeList[2] = ComponentType.ReadWrite<NetCode.SnapshotMetrics>();
            typeList[3] = ComponentType.ReadWrite<NetCode.GhostNames>();
            typeList[4] = ComponentType.ReadWrite<NetCode.GhostMetrics>();
            typeList[5] = ComponentType.ReadWrite<NetCode.GhostSerializationMetrics>();
            typeList[6] = ComponentType.ReadWrite<NetCode.PredictionErrorNames>();
            typeList[7] = ComponentType.ReadWrite<NetCode.PredictionErrorMetrics>();
            var singleton = entityManager.CreateEntity(entityManager.CreateArchetype(typeList));
            typeList.Dispose();
            entityManager.SetName(singleton, (FixedString64Bytes)"MetricsMonitor");
            return singleton;
        }

        private static double Mean(List<uint> values)
        {
            double total = 0;
            for (int i = 0; i < values.Count; i++)
            {
                total += values[i];
            }
            return values.Count == 0 ? 0 : total / values.Count;
        }

        private static uint Percentile(List<uint> values, double fraction)
        {
            if (values.Count == 0)
            {
                return 0;
            }
            var sorted = new List<uint>(values);
            sorted.Sort();
            var index = (int)Math.Round(fraction * (sorted.Count - 1));
            return sorted[Mathf.Clamp(index, 0, sorted.Count - 1)];
        }

        [UnityTest]
        public IEnumerator MeasureSnapshotSize(
            [Values(0, 4000, 8000, 15000)] int packetSize,
            [Values(250, 500, 1000, 2000, 2500, 3000)] int objectCount)
        {
            var hostWorld = m_ServerNetworkManager.NetcodeWorld;
            var clientWorld = m_ClientNetworkManagers[0].NetcodeWorld;
            Assert.IsNotNull(hostWorld, "Host has no NetcodeWorld!");
            Assert.IsNotNull(clientWorld, "Client has no NetcodeWorld!");

            var sendDataQuery = hostWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetCode.GhostSendSystemData>());
            var sendData = sendDataQuery.GetSingleton<NetCode.GhostSendSystemData>();
            sendData.DefaultSnapshotPacketSize = packetSize;
            sendDataQuery.SetSingleton(sendData);

            var tickRate = 30;
            var tickRateQuery = hostWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetCode.ClientServerTickRate>());
            if (tickRateQuery.CalculateEntityCount() == 1)
            {
                var configured = tickRateQuery.GetSingleton<NetCode.ClientServerTickRate>();
                tickRate = configured.NetworkTickRate > 0 ? configured.NetworkTickRate : Mathf.Max(1, configured.SimulationTickRate);
            }

            CreateMetricsSingleton(clientWorld.EntityManager);
            var snapshotMetricsQuery = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetCode.SnapshotMetrics>());

            var clientSpawnManager = m_ClientNetworkManagers[0].SpawnManager;
            var preSpawnCount = clientSpawnManager.SpawnedObjects.Count;

            m_Instances = new Transform[objectCount];
            m_Ghosts = new NetCode.GhostObject[objectCount];
            m_Phases = new float[objectCount];
            var random = new System.Random(12345);
            for (int i = 0; i < objectCount; i++)
            {
                m_Phases[i] = (float)(random.NextDouble() * Mathf.PI * 2.0f);
                var spawned = SpawnObject(m_Prefab, m_ServerNetworkManager);
                m_Instances[i] = spawned.transform;
                m_Ghosts[i] = spawned.GetComponent<NetCode.GhostObject>();
                if ((i + 1) % k_SpawnsPerFrame == 0)
                {
                    MoveAll();
                    yield return null;
                }
            }

            var deadline = Time.realtimeSinceStartup + k_SpawnTimeout;
            while ((clientSpawnManager.SpawnedObjects.Count - preSpawnCount) < objectCount && Time.realtimeSinceStartup < deadline)
            {
                MoveAll();
                yield return null;
            }
            var spawnedOnClient = clientSpawnManager.SpawnedObjects.Count - preSpawnCount;

            var sizes = new List<uint>(k_SampleSnapshots);
            var counts = new List<uint>(k_SampleSnapshots);
            uint lastSnapshotTick = 0;
            var snapshotsSeen = 0;
            deadline = Time.realtimeSinceStartup + k_SampleTimeout;
            while (snapshotsSeen < (k_WarmupSnapshots + k_SampleSnapshots) && Time.realtimeSinceStartup < deadline)
            {
                MoveAll();
                yield return null;

                if (snapshotMetricsQuery.CalculateEntityCount() != 1)
                {
                    continue;
                }
                var metrics = snapshotMetricsQuery.GetSingleton<NetCode.SnapshotMetrics>();
                if (metrics.SnapshotTick == 0 || metrics.SnapshotTick == lastSnapshotTick)
                {
                    continue;
                }
                lastSnapshotTick = metrics.SnapshotTick;
                snapshotsSeen++;
                if (snapshotsSeen > k_WarmupSnapshots)
                {
                    sizes.Add(metrics.TotalSizeInBits);
                    counts.Add(metrics.TotalGhostCount);
                }
            }

            // Sanity check that the ghosts really did move (a static ghost measures nothing useful).
            var hostNetworkObject = m_Instances[0].GetComponent<NetworkObject>();
            if (clientSpawnManager.SpawnedObjects.TryGetValue(hostNetworkObject.NetworkObjectId, out var clientClone))
            {
                Debug.Log($"PKTDIAG|hostGO={m_Instances[0].position}|hostGhost={m_Ghosts[0].Position}|client={clientClone.transform.position}|frames={m_Frame}");
            }

            // The unfragmented default is driver derived; approximate with the configured MaxMessageSize for the cap check.
            var effectiveCapBytes = packetSize > 0 ? packetSize : 1400;
            var capHits = 0;
            var incomplete = 0;
            for (int i = 0; i < sizes.Count; i++)
            {
                if ((sizes[i] / 8.0) >= (effectiveCapBytes * 0.95))
                {
                    capHits++;
                }
                if (counts[i] < objectCount)
                {
                    incomplete++;
                }
            }

            var meanBits = Mean(sizes);
            var meanGhosts = Mean(counts);
            var capHitFraction = sizes.Count == 0 ? 0.0 : (double)capHits / sizes.Count;
            var incompleteFraction = sizes.Count == 0 ? 0.0 : (double)incomplete / sizes.Count;
            var bytesPerGhost = meanGhosts <= 0 ? 0.0 : (meanBits / 8.0) / meanGhosts;
            var effectiveHz = objectCount <= 0 ? 0.0 : (meanGhosts / objectCount) * tickRate;

            Debug.Log($"PKTSZ|{packetSize}|{objectCount}|{spawnedOnClient}|{sizes.Count}|{meanBits:F1}|{Percentile(sizes, 0.95)}|" +
                $"{Percentile(sizes, 1.0)}|{meanGhosts:F1}|{Percentile(counts, 0.95)}|{bytesPerGhost:F3}|{capHitFraction:F3}|{incompleteFraction:F3}|{effectiveHz:F2}|{tickRate}");

            Assert.AreEqual(objectCount, spawnedOnClient, $"Client only spawned {spawnedOnClient} of {objectCount} hybrid ghosts!");
            Assert.AreEqual(k_SampleSnapshots, sizes.Count, $"Only collected {sizes.Count} of {k_SampleSnapshots} snapshot samples!");
        }

        protected override IEnumerator OnTearDown()
        {
            m_Instances = null;
            m_Ghosts = null;
            m_Phases = null;
            return base.OnTearDown();
        }
    }
}
#endif
