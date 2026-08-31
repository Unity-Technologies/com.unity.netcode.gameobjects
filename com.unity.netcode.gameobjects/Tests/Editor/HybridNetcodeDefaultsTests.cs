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
        // Stands in for a value the user chose. Anything other than SnapshotPacketSize works; this is far enough from
        // it that a partial apply cannot look like a pass.
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
        /// The editor one-shot writes <see cref="HybridNetcodeDefaults.DefaultTickRate"/>, so a project running at any
        /// other rate is corrected by <see cref="NetworkManager"/> at start-up. That pass has to move the rate without
        /// disturbing the tuned values around it.
        /// </summary>
        [Test]
        public void TickRateOnlyPassCorrectsTheRateAndLeavesTheTunedValuesAlone()
        {
            const int writtenTickRate = 30;
            const uint managerTickRate = 60;

            HybridNetcodeDefaults.ApplyRecommended(m_Config, writtenTickRate);
            Assume.That(m_Config.ClientServerTickRate.SimulationTickRate, Is.EqualTo(writtenTickRate), "The one-shot should have written the default tick rate.");

            Assert.IsTrue(HybridNetcodeDefaults.ApplyTickRate(m_Config, managerTickRate));

            Assert.AreEqual((int)managerTickRate, m_Config.ClientServerTickRate.SimulationTickRate);
            Assert.AreEqual((int)managerTickRate, m_Config.ClientServerTickRate.NetworkTickRate);
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

        /// <summary>
        /// The editor writes <see cref="HybridNetcodeDefaults.DefaultTickRate"/> because it has no
        /// <see cref="NetworkManager"/> to read the real rate from. If the field default ever moves, this has to move
        /// with it, or every project that kept the default starts out of sync until its first session.
        /// </summary>
        [Test]
        public void DefaultTickRateMatchesTheNetworkConfigDefault()
        {
            Assert.AreEqual(new NetworkConfig().TickRate, HybridNetcodeDefaults.DefaultTickRate);
        }

        /// <summary>
        /// Nothing is written until the user opts into the experimental unified netcode API, and opting in is what
        /// triggers the one and only write.
        /// </summary>
        [Test]
        public void ApplyDefaultsWritesNothingUntilTheUnifiedApiIsEnabled()
        {
            Assume.That(HybridNetcodeConfigApplier.RequiresExperimentalOptIn, Is.True, "This project ships the unified API without an opt-in, so there is no gate to exercise.");

            var config = HybridNetcodeConfigApplier.ResolveGlobalConfig();
            Assume.That(config, Is.Not.Null, "This project has no NetCodeConfig to adjust.");

            var settings = NetcodeForGameObjectsProjectSettings.instance;
            var restoreOptIn = settings.EnableUnifiedNetcodeApi;
            var restoreVersion = settings.HybridDefaultsVersion;
            var restoreConfig = EditorJsonUtility.ToJson(config);
            try
            {
                settings.EnableUnifiedNetcodeApi = false;
                settings.HybridDefaultsVersion = 0;
                config.GhostSendSystemData.DefaultSnapshotPacketSize = k_UserPacketSize;

                HybridNetcodeConfigApplier.ApplyDefaults(false);
                Assert.AreEqual(k_UserPacketSize, config.GhostSendSystemData.DefaultSnapshotPacketSize, "Opting out has to leave the NetCodeConfig exactly as it is.");
                Assert.AreEqual(0, settings.HybridDefaultsVersion, "Nothing was applied, so nothing should have been recorded.");

                settings.EnableUnifiedNetcodeApi = true;
                HybridNetcodeConfigApplier.ApplyDefaults(false);
                Assert.AreEqual(HybridNetcodeDefaults.SnapshotPacketSize, config.GhostSendSystemData.DefaultSnapshotPacketSize, "Opting in should have written the defaults.");
                Assert.AreEqual(HybridNetcodeDefaults.Version, settings.HybridDefaultsVersion);
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
        /// Once the marker is recorded nothing writes the config again, which is what keeps a user's own edits from
        /// being reverted on the next domain reload. Only the Project Settings button overrides it.
        /// </summary>
        [Test]
        public void ApplyDefaultsIsAOneShotUnlessItIsForced()
        {
            var config = HybridNetcodeConfigApplier.ResolveGlobalConfig();
            Assume.That(config, Is.Not.Null, "This project has no NetCodeConfig to adjust.");

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
                Assert.AreEqual(k_UserPacketSize, config.GhostSendSystemData.DefaultSnapshotPacketSize, "A recorded marker has to stop the defaults from being written a second time.");

                HybridNetcodeConfigApplier.ApplyDefaults(true);
                Assert.AreEqual(HybridNetcodeDefaults.SnapshotPacketSize, config.GhostSendSystemData.DefaultSnapshotPacketSize, "The Project Settings button re-applies regardless of the marker.");
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
        /// Puts the project's own NetCodeConfig back the way the test found it. Serialized rather than field by field
        /// because the applier writes across three nested structures.
        /// </summary>
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
