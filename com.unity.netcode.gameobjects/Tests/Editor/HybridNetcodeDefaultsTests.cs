#if UNIFIED_NETCODE
using NUnit.Framework;
using Unity.NetCode;
using Unity.Netcode.GameObjects.Editor.Configuration;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.GameObjects.EditorTests
{
    /// <summary>
    /// Validates the <see cref="NetCodeConfig"/> values NGO applies in hybrid mode.
    /// </summary>
    internal class HybridNetcodeDefaultsTests
    {
        // Stands in for a value the user chose. Far enough from SnapshotPacketSize that a partial apply cannot
        // look like a pass.
        private const int k_UserPacketSize = 9000;

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

        [Test]
        public void ApplyRecommendedReportsNoChangeWhenConfigAlreadyMatches()
        {
            Assert.IsTrue(HybridNetcodeDefaults.ApplyRecommended(m_Config, HybridNetcodeDefaults.DefaultTickRate), "The first apply should report a change.");
            Assert.IsFalse(HybridNetcodeDefaults.ApplyRecommended(m_Config, HybridNetcodeDefaults.DefaultTickRate), "Applying an already matching config should report no change.");
        }

        [Test]
        public void ApplyRequiredAdjustsBothSettings()
        {
            m_Config.EnableClientServerBootstrap = NetCodeConfig.AutomaticBootstrapSetting.EnableAutomaticBootstrap;
            m_Config.HostWorldModeSelection = NetCodeConfig.HostWorldMode.BinaryWorlds;

            Assert.IsTrue(HybridNetcodeDefaults.ApplyRequired(m_Config), "The first apply should report a change.");
            Assert.AreEqual(NetCodeConfig.AutomaticBootstrapSetting.DisableAutomaticBootstrap, m_Config.EnableClientServerBootstrap, "Automatic bootstrapping should be disabled.");
            Assert.AreEqual(NetCodeConfig.HostWorldMode.SingleWorld, m_Config.HostWorldModeSelection, "Hybrid mode should use a single world.");

            Assert.IsFalse(HybridNetcodeDefaults.ApplyRequired(m_Config), "Applying an already correct config should report no change.");
        }

        [Test]
        public void IsMissingRequiredDetectsEachViolation()
        {
            HybridNetcodeDefaults.ApplyRequired(m_Config);
            Assert.IsFalse(HybridNetcodeDefaults.IsMissingRequired(m_Config, out _), "An adjusted config should be valid for hybrid mode.");

            m_Config.HostWorldModeSelection = NetCodeConfig.HostWorldMode.BinaryWorlds;
            Assert.IsTrue(HybridNetcodeDefaults.IsMissingRequired(m_Config, out var worldReason), "Binary worlds should be reported as invalid.");
            Assert.That(worldReason, Does.Contain(nameof(NetCodeConfig.HostWorldModeSelection)), "The reason should name the setting that is wrong.");

            m_Config.HostWorldModeSelection = NetCodeConfig.HostWorldMode.SingleWorld;
            m_Config.EnableClientServerBootstrap = NetCodeConfig.AutomaticBootstrapSetting.EnableAutomaticBootstrap;
            Assert.IsTrue(HybridNetcodeDefaults.IsMissingRequired(m_Config, out var bootstrapReason), "Automatic bootstrapping should be reported as invalid.");
            Assert.That(bootstrapReason, Does.Contain(nameof(NetCodeConfig.EnableClientServerBootstrap)), "The reason should name the setting that is wrong.");
        }

        [TestCase(30u)]
        [TestCase(60u)]
        public void ApplyTickRateLocksSimulationAndNetworkRates(uint tickRate)
        {
            Assert.IsTrue(HybridNetcodeDefaults.ApplyTickRate(m_Config, tickRate), "The first apply should report a change.");
            Assert.AreEqual((int)tickRate, m_Config.ClientServerTickRate.SimulationTickRate, "SimulationTickRate should be the requested rate.");
            Assert.AreEqual((int)tickRate, m_Config.ClientServerTickRate.NetworkTickRate, "NetworkTickRate should track SimulationTickRate.");

            Assert.IsFalse(HybridNetcodeDefaults.ApplyTickRate(m_Config, tickRate), "Re-applying the same rate should report no change.");
        }

        /// <summary>
        /// The editor writes <see cref="HybridNetcodeDefaults.DefaultTickRate"/>.<br />
        /// <see cref="NetworkManager"/> adjusts it at start-up for a project running at any other rate.<br />
        /// That pass leaves the tuned values alone.<br />
        /// </summary>
        [Test]
        public void TickRateOnlyPassAdjustsTheRateAndLeavesTheTunedValuesAlone()
        {
            const uint managerTickRate = 60;

            HybridNetcodeDefaults.ApplyRecommended(m_Config, HybridNetcodeDefaults.DefaultTickRate);

            Assert.IsTrue(HybridNetcodeDefaults.ApplyTickRate(m_Config, managerTickRate), "The tick rate pass should report a change.");
            Assert.AreEqual((int)managerTickRate, m_Config.ClientServerTickRate.SimulationTickRate, "SimulationTickRate should follow the NetworkManager.");
            Assert.AreEqual((int)managerTickRate, m_Config.ClientServerTickRate.NetworkTickRate, "NetworkTickRate should follow the NetworkManager.");
            Assert.AreEqual(HybridNetcodeDefaults.SnapshotPacketSize, m_Config.GhostSendSystemData.DefaultSnapshotPacketSize, "A tick rate pass should leave DefaultSnapshotPacketSize alone.");
            Assert.AreEqual(HybridNetcodeDefaults.InterpolationTimeMS, m_Config.ClientTickRate.InterpolationTimeMS, "A tick rate pass should leave InterpolationTimeMS alone.");
            Assert.AreEqual(HybridNetcodeDefaults.InterpolationTimeScaleMax, m_Config.ClientTickRate.InterpolationTimeScaleMax, "A tick rate pass should leave InterpolationTimeScaleMax alone.");
        }

        [Test]
        public void ApplyRecommendedProducesTheTunedValues()
        {
            Assert.IsTrue(HybridNetcodeDefaults.ApplyRecommended(m_Config, HybridNetcodeDefaults.DefaultTickRate), "The first apply should report a change.");

            Assert.AreEqual(HybridNetcodeDefaults.SnapshotPacketSize, m_Config.GhostSendSystemData.DefaultSnapshotPacketSize, "DefaultSnapshotPacketSize should be the tuned value.");
            Assert.AreEqual(HybridNetcodeDefaults.PercentReservedForDespawn, m_Config.GhostSendSystemData.PercentReservedForDespawnMessages, "PercentReservedForDespawnMessages should be the tuned value.");
            Assert.AreEqual(HybridNetcodeDefaults.InterpolationTimeMS, m_Config.ClientTickRate.InterpolationTimeMS, "InterpolationTimeMS should be the tuned value.");
            Assert.AreEqual(0u, m_Config.ClientTickRate.InterpolationTimeNetTicks, "The net tick form wins over the millisecond form, so it is cleared.");
            Assert.AreEqual(HybridNetcodeDefaults.InterpolationTimeScaleMin, m_Config.ClientTickRate.InterpolationTimeScaleMin, "InterpolationTimeScaleMin should be the tuned value.");
            Assert.AreEqual(HybridNetcodeDefaults.InterpolationTimeScaleMax, m_Config.ClientTickRate.InterpolationTimeScaleMax, "InterpolationTimeScaleMax should be the tuned value.");
            Assert.AreEqual(HybridNetcodeDefaults.ClientQueueCapacity, m_Config.ClientSendQueueCapacity, "ClientSendQueueCapacity should be the tuned value.");
            Assert.AreEqual(HybridNetcodeDefaults.ClientQueueCapacity, m_Config.ClientReceiveQueueCapacity, "ClientReceiveQueueCapacity should be the tuned value.");

            Assert.IsFalse(HybridNetcodeDefaults.ApplyRecommended(m_Config, HybridNetcodeDefaults.DefaultTickRate), "Re-applying an unchanged config should report no change.");
        }

        /// <summary>
        /// Why the millisecond form is used rather than <see cref="ClientTickRate.InterpolationTimeNetTicks"/>.
        /// </summary>
        /// <remarks>
        /// N4E rounds the millisecond value up to whole network ticks.<br />
        /// It holds at least the configured wall clock buffer at any tick rate.<br />
        /// </remarks>
        /// <param name="tickRate">The tick rate to resolve the buffer against.</param>
        [TestCase(30u)]
        [TestCase(60u)]
        public void InterpolationBufferHoldsAtLeastFiftyMillisecondsAtAnyTickRate(uint tickRate)
        {
            HybridNetcodeDefaults.ApplyRecommended(m_Config, tickRate);

            var bufferMs = m_Config.ClientTickRate.CalculateInterpolationBufferTimeInMs(in m_Config.ClientServerTickRate);
            Assert.GreaterOrEqual(bufferMs, HybridNetcodeDefaults.InterpolationTimeMS, "The interpolation buffer should hold the configured wall clock time.");
        }

        [Test]
        public void NetTickFormWouldRegressTheBufferAtHigherTickRates()
        {
            // If this stops being true, the millisecond form and its extra rounding are no longer buying anything.
            HybridNetcodeDefaults.ApplyTickRate(m_Config, 60);
            m_Config.ClientTickRate = new ClientTickRate
            {
                InterpolationTimeNetTicks = 2,
                InterpolationTimeMS = 0,
            };

            var bufferMs = m_Config.ClientTickRate.CalculateInterpolationBufferTimeInMs(in m_Config.ClientServerTickRate);
            Assert.Less(bufferMs, HybridNetcodeDefaults.InterpolationTimeMS, "The net tick form should fall short of the millisecond form at 60Hz.");
        }

        /// <summary>
        /// The editor writes <see cref="HybridNetcodeDefaults.DefaultTickRate"/> because no
        /// <see cref="NetworkManager"/> is loaded to read the rate from.
        /// </summary>
        [Test]
        public void DefaultTickRateMatchesTheNetworkConfigDefault()
        {
            Assert.AreEqual(new NetworkConfig().TickRate, HybridNetcodeDefaults.DefaultTickRate, "DefaultTickRate should track the NetworkConfig.TickRate default.");
        }

        /// <summary>
        /// Once the marker is recorded nothing writes the config again.<br />
        /// Only the Project Settings button overrides it.<br />
        /// </summary>
        [Test]
        public void ApplyDefaultsIsAOneShotUnlessItIsForced()
        {
            var config = HybridNetcodeConfigApplier.ResolveGlobalConfig();
            Assert.IsNotNull(config, "This project should have a NetCodeConfig to adjust.");

            var settings = NetcodeForGameObjectsProjectSettings.instance;
            var restoreOptIn = settings.EnableUnifiedNetcodeApi;
            var restoreVersion = settings.HybridDefaultsVersion;
            var restoreConfig = EditorJsonUtility.ToJson(config);
            try
            {
                settings.EnableUnifiedNetcodeApi = true;
                settings.HybridDefaultsVersion = HybridNetcodeDefaults.Version;
                config.GhostSendSystemData.DefaultSnapshotPacketSize = k_UserPacketSize;

                HybridNetcodeConfigApplier.ApplyDefaults(false);
                Assert.AreEqual(k_UserPacketSize, config.GhostSendSystemData.DefaultSnapshotPacketSize, "A recorded marker should stop the defaults from being written a second time.");

                HybridNetcodeConfigApplier.ApplyDefaults(true);
                Assert.AreEqual(HybridNetcodeDefaults.SnapshotPacketSize, config.GhostSendSystemData.DefaultSnapshotPacketSize, "The Project Settings button should re-apply regardless of the marker.");
            }
            finally
            {
                Restore(config, restoreConfig);
                settings.EnableUnifiedNetcodeApi = restoreOptIn;
                settings.HybridDefaultsVersion = restoreVersion;
                settings.SaveSettings();
            }
        }

        /// <summary>
        /// Puts the project's own <see cref="NetCodeConfig"/> back the way the test found it.
        /// </summary>
        /// <remarks>
        /// Serialized rather than field by field because the applier writes across three nested structures.
        /// </remarks>
        /// <param name="config">The project config the test mutated.</param>
        /// <param name="serializedConfig">Its state before the test ran.</param>
        private static void Restore(NetCodeConfig config, string serializedConfig)
        {
            EditorJsonUtility.FromJsonOverwrite(serializedConfig, config);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
        }
    }
}
#endif
