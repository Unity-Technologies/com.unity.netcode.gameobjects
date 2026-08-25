#if UNIFIED_NETCODE
using NUnit.Framework;
using Unity.NetCode;
using Unity.Netcode.GameObjects.Editor.Configuration;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.EditorTests
{
    /// <summary>
    /// Validates the NetCodeConfig values NGO applies in hybrid mode.
    /// </summary>
    internal class HybridNetcodeDefaultsTests
    {
        private NetCodeConfig m_Config;

        [SetUp]
        public void SetUp()
        {
            m_Config = ScriptableObject.CreateInstance<NetCodeConfig>();
            m_Config.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Config);
        }

        /// <summary>
        /// A config that already matches reports no change, which is why the applier records the version marker
        /// independently of whether anything was written.
        /// </summary>
        [Test]
        public void ApplyRecommendedReportsNoChangeWhenConfigAlreadyMatches()
        {
            Assert.IsTrue(HybridNetcodeDefaults.ApplyRecommended(m_Config, 30u), "Expected the first apply to report a change.");
            Assert.IsFalse(HybridNetcodeDefaults.ApplyRecommended(m_Config, 30u), "Applying an already matching config should report no change.");
        }

        [Test]
        public void ApplyRequiredCorrectsBothSettings()
        {
            m_Config.EnableClientServerBootstrap = NetCodeConfig.AutomaticBootstrapSetting.EnableAutomaticBootstrap;
            m_Config.HostWorldModeSelection = NetCodeConfig.HostWorldMode.BinaryWorlds;

            Assert.IsTrue(HybridNetcodeDefaults.ApplyRequired(m_Config), "Expected the first apply to report a change.");
            Assert.AreEqual(NetCodeConfig.AutomaticBootstrapSetting.DisableAutomaticBootstrap, m_Config.EnableClientServerBootstrap);
            Assert.AreEqual(NetCodeConfig.HostWorldMode.SingleWorld, m_Config.HostWorldModeSelection);

            Assert.IsFalse(HybridNetcodeDefaults.ApplyRequired(m_Config), "Applying an already correct config should report no change.");
        }

        [Test]
        public void IsMissingRequiredDetectsEachViolation()
        {
            HybridNetcodeDefaults.ApplyRequired(m_Config);
            Assert.IsFalse(HybridNetcodeDefaults.IsMissingRequired(m_Config, out _), "A corrected config should be valid for hybrid mode.");

            m_Config.HostWorldModeSelection = NetCodeConfig.HostWorldMode.BinaryWorlds;
            Assert.IsTrue(HybridNetcodeDefaults.IsMissingRequired(m_Config, out var worldReason));
            Assert.That(worldReason, Does.Contain(nameof(NetCodeConfig.HostWorldModeSelection)));

            m_Config.HostWorldModeSelection = NetCodeConfig.HostWorldMode.SingleWorld;
            m_Config.EnableClientServerBootstrap = NetCodeConfig.AutomaticBootstrapSetting.EnableAutomaticBootstrap;
            Assert.IsTrue(HybridNetcodeDefaults.IsMissingRequired(m_Config, out var bootstrapReason));
            Assert.That(bootstrapReason, Does.Contain(nameof(NetCodeConfig.EnableClientServerBootstrap)));
        }

        [TestCase(30u)]
        [TestCase(60u)]
        public void ApplyTickRateLocksSimulationAndNetworkRates(uint tickRate)
        {
            Assert.IsTrue(HybridNetcodeDefaults.ApplyTickRate(m_Config, tickRate));
            Assert.AreEqual((int)tickRate, m_Config.ClientServerTickRate.SimulationTickRate);
            Assert.AreEqual((int)tickRate, m_Config.ClientServerTickRate.NetworkTickRate, "NetworkTickRate must track SimulationTickRate; the interpolation buffer depends on it.");

            Assert.IsFalse(HybridNetcodeDefaults.ApplyTickRate(m_Config, tickRate));
        }

        /// <summary>
        /// The one-shot can run before the hybrid <see cref="NetworkManager"/>'s scene is open, in which case it writes
        /// N4E's tick rate rather than NGO's. Opening that scene runs a tick rate only pass, which has to correct the
        /// rate without disturbing the tuned values.
        /// </summary>
        [Test]
        public void TickRateOnlyPassCorrectsTheRateAndLeavesTheTunedValuesAlone()
        {
            const int n4eTickRate = 60;
            const uint ngoTickRate = 30;

            HybridNetcodeDefaults.ApplyRecommended(m_Config, n4eTickRate);
            Assume.That(m_Config.ClientServerTickRate.SimulationTickRate, Is.EqualTo(n4eTickRate), "The one-shot should have written N4E's tick rate.");

            Assert.IsTrue(HybridNetcodeDefaults.ApplyTickRate(m_Config, ngoTickRate));

            Assert.AreEqual((int)ngoTickRate, m_Config.ClientServerTickRate.SimulationTickRate);
            Assert.AreEqual((int)ngoTickRate, m_Config.ClientServerTickRate.NetworkTickRate);
            Assert.AreEqual(HybridNetcodeDefaults.SnapshotPacketSize, m_Config.GhostSendSystemData.DefaultSnapshotPacketSize, "A tick rate pass must not disturb the tuned values.");
            Assert.AreEqual(HybridNetcodeDefaults.InterpolationTimeMS, m_Config.ClientTickRate.InterpolationTimeMS);
            Assert.AreEqual(HybridNetcodeDefaults.InterpolationTimeScaleMax, m_Config.ClientTickRate.InterpolationTimeScaleMax);
        }

        [Test]
        public void ApplyRecommendedProducesTheTunedValues()
        {
            Assert.IsTrue(HybridNetcodeDefaults.ApplyRecommended(m_Config, 30));

            Assert.AreEqual(HybridNetcodeDefaults.SnapshotPacketSize, m_Config.GhostSendSystemData.DefaultSnapshotPacketSize);
            Assert.AreEqual(HybridNetcodeDefaults.PercentReservedForDespawn, m_Config.GhostSendSystemData.PercentReservedForDespawnMessages);
            Assert.AreEqual(HybridNetcodeDefaults.InterpolationTimeMS, m_Config.ClientTickRate.InterpolationTimeMS);
            Assert.AreEqual(0u, m_Config.ClientTickRate.InterpolationTimeNetTicks, "The net tick form wins over the millisecond form, so it has to be cleared.");
            Assert.AreEqual(HybridNetcodeDefaults.InterpolationTimeScaleMin, m_Config.ClientTickRate.InterpolationTimeScaleMin);
            Assert.AreEqual(HybridNetcodeDefaults.InterpolationTimeScaleMax, m_Config.ClientTickRate.InterpolationTimeScaleMax);
            Assert.AreEqual(HybridNetcodeDefaults.ClientQueueCapacity, m_Config.ClientSendQueueCapacity);
            Assert.AreEqual(HybridNetcodeDefaults.ClientQueueCapacity, m_Config.ClientReceiveQueueCapacity);

            Assert.IsFalse(HybridNetcodeDefaults.ApplyRecommended(m_Config, 30), "Re-applying an unchanged config should report no change.");
        }

        /// <summary>
        /// Why the millisecond form is used rather than <see cref="ClientTickRate.InterpolationTimeNetTicks"/>.
        /// </summary>
        /// <remarks>
        /// Netcode for Entities rounds the millisecond value up to whole network ticks, so it holds at least the
        /// configured wall clock buffer at any tick rate.
        /// </remarks>
        /// <param name="tickRate">The tick rate to resolve the buffer against.</param>
        [TestCase(30u)]
        [TestCase(60u)]
        public void InterpolationBufferHoldsAtLeastFiftyMillisecondsAtAnyTickRate(uint tickRate)
        {
            HybridNetcodeDefaults.ApplyRecommended(m_Config, tickRate);

            var bufferMs = m_Config.ClientTickRate.CalculateInterpolationBufferTimeInMs(in m_Config.ClientServerTickRate);
            Assert.GreaterOrEqual(bufferMs, HybridNetcodeDefaults.InterpolationTimeMS, $"Interpolation buffer collapsed to {bufferMs}ms at {tickRate}Hz.");
        }

        [Test]
        public void NetTickFormWouldRegressTheBufferAtHigherTickRates()
        {
            // Documents why the net tick form is not used. If this ever stops being true, the millisecond form and its
            // extra rounding are no longer buying anything.
            HybridNetcodeDefaults.ApplyTickRate(m_Config, 60);
            m_Config.ClientTickRate = new ClientTickRate
            {
                InterpolationTimeNetTicks = 2,
                InterpolationTimeMS = 0,
            };

            var bufferMs = m_Config.ClientTickRate.CalculateInterpolationBufferTimeInMs(in m_Config.ClientServerTickRate);
            Assert.Less(bufferMs, HybridNetcodeDefaults.InterpolationTimeMS);
        }

        [Test]
        public void IsHybridProjectOnlyDetectsPrefabsCarryingAGhost()
        {
            Assume.That(HybridNetcodeConfigApplier.IsHybridProject(), Is.False, "Another loaded NetworkManager already registers a ghost prefab.");

            var managerObject = new GameObject(nameof(IsHybridProjectOnlyDetectsPrefabsCarryingAGhost));
            var prefabObject = new GameObject("GhostPrefab");
            var prefabsList = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            try
            {
                var networkManager = managerObject.AddComponent<NetworkManager>();
                networkManager.NetworkConfig = new NetworkConfig();
                var networkObject = prefabObject.AddComponent<NetworkObject>();

                prefabsList.Add(new NetworkPrefab { Prefab = prefabObject });
                networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabsList);

                Assert.IsFalse(HybridNetcodeConfigApplier.IsHybridProject(), "A registered prefab without a GhostObject is not hybrid.");

                networkObject.HasGhost = true;
                Assert.IsTrue(HybridNetcodeConfigApplier.IsHybridProject());
            }
            finally
            {
                Object.DestroyImmediate(prefabsList);
                Object.DestroyImmediate(prefabObject);
                Object.DestroyImmediate(managerObject);
            }
        }

        /// <summary>
        /// Once the one-shot has been recorded, every later pass still drives the tick rate from the hybrid
        /// <see cref="NetworkManager"/>. This is what corrects the rate when the manager's scene opens after the
        /// defaults were already applied.
        /// </summary>
        [Test]
        public void ApplyDrivesTheTickRateAfterTheOneShotHasBeenRecorded()
        {
            const uint managerTickRate = 45;

            var config = HybridNetcodeConfigApplier.ResolveGlobalConfig();
            Assume.That(config, Is.Not.Null, "This project has no NetCodeConfig to adjust.");
            Assume.That(HybridNetcodeConfigApplier.IsHybridProject(), Is.False, "Another loaded NetworkManager already registers a ghost prefab.");

            var settings = NetcodeForGameObjectsProjectSettings.instance;
            var restoreVersion = settings.HybridDefaultsVersion;
            var restoreSimulation = config.ClientServerTickRate.SimulationTickRate;
            var restoreNetwork = config.ClientServerTickRate.NetworkTickRate;

            var managerObject = new GameObject(nameof(ApplyDrivesTheTickRateAfterTheOneShotHasBeenRecorded));
            var prefabObject = new GameObject("GhostPrefab");
            var prefabsList = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            try
            {
                var networkManager = managerObject.AddComponent<NetworkManager>();
                networkManager.NetworkConfig = new NetworkConfig { TickRate = managerTickRate, };
                prefabObject.AddComponent<NetworkObject>().HasGhost = true;
                prefabsList.Add(new NetworkPrefab { Prefab = prefabObject });
                networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabsList);

                // Past the one-shot, so this exercises the required plus tick rate path rather than ApplyRecommended.
                settings.HybridDefaultsVersion = HybridNetcodeDefaults.Version;
                config.ClientServerTickRate.SimulationTickRate = 60;
                config.ClientServerTickRate.NetworkTickRate = 60;

                HybridNetcodeConfigApplier.Apply(false);

                Assert.AreEqual((int)managerTickRate, config.ClientServerTickRate.SimulationTickRate, "The tick rate should have been driven from the hybrid NetworkManager.");
                Assert.AreEqual((int)managerTickRate, config.ClientServerTickRate.NetworkTickRate);
            }
            finally
            {
                config.ClientServerTickRate.SimulationTickRate = restoreSimulation;
                config.ClientServerTickRate.NetworkTickRate = restoreNetwork;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssetIfDirty(config);

                settings.HybridDefaultsVersion = restoreVersion;
                settings.SaveSettings();

                Object.DestroyImmediate(prefabsList);
                Object.DestroyImmediate(prefabObject);
                Object.DestroyImmediate(managerObject);
            }
        }
    }
}
#endif
