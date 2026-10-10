#if UNIFIED_NETCODE
using NUnit.Framework;
using UnityEngine;

namespace Unity.Netcode.GameObjects.EditorTests
{
    /// <summary>
    /// Validates the <see cref="NetcodeConfig"/> values NGO drives in hybrid mode.
    /// </summary>
    internal class HybridNetcodeDefaultsTests
    {
        // Stands in for a value the user chose. Far enough from SnapshotPacketSize that a partial apply cannot
        // look like a pass.
        private const int k_UserPacketSize = 9000;

        private NetcodeConfig m_Config;

        [SetUp]
        public void SetUp()
        {
            m_Config = ScriptableObject.CreateInstance<NetcodeConfig>();
            m_Config.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Config);
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

        [Test]
        public void ApplySnapshotPacketSizeOverridesTheNetcodeForEntitiesDefault()
        {
            // Zero is N4E's default and means one MTU, which round-robins well below the tick rate at the instance
            // counts NGO projects run.
            m_Config.GhostSendSystemData.DefaultSnapshotPacketSize = 0;

            Assert.IsTrue(HybridNetcodeDefaults.ApplySnapshotPacketSize(m_Config), "The first apply should report a change.");
            Assert.AreEqual(HybridNetcodeDefaults.SnapshotPacketSize, m_Config.GhostSendSystemData.DefaultSnapshotPacketSize, "DefaultSnapshotPacketSize should be the NGO value.");

            Assert.IsFalse(HybridNetcodeDefaults.ApplySnapshotPacketSize(m_Config), "Re-applying an unchanged config should report no change.");
        }

        [Test]
        public void ApplySnapshotPacketSizeLeavesTheTickRatesAlone()
        {
            HybridNetcodeDefaults.ApplyTickRate(m_Config, 60);
            m_Config.GhostSendSystemData.DefaultSnapshotPacketSize = k_UserPacketSize;

            HybridNetcodeDefaults.ApplySnapshotPacketSize(m_Config);
            Assert.AreEqual(60, m_Config.ClientServerTickRate.SimulationTickRate, "The snapshot size pass should leave SimulationTickRate alone.");
            Assert.AreEqual(60, m_Config.ClientServerTickRate.NetworkTickRate, "The snapshot size pass should leave NetworkTickRate alone.");
        }

        [Test]
        public void IsMissingRequiredDetectsBinaryWorlds()
        {
            m_Config.HostWorldModeSelection = NetcodeConfig.HostWorldMode.SingleWorld;
            Assert.IsFalse(HybridNetcodeDefaults.IsMissingRequired(m_Config, out _), "A single world config should be valid for hybrid mode.");

            m_Config.HostWorldModeSelection = NetcodeConfig.HostWorldMode.BinaryWorlds;
            Assert.IsTrue(HybridNetcodeDefaults.IsMissingRequired(m_Config, out var reason), "Binary worlds should be reported as invalid.");
            Assert.That(reason, Does.Contain(nameof(NetcodeConfig.HostWorldModeSelection)), "The reason should name the setting that is wrong.");
        }

        /// <summary>
        /// NGO no longer requires the bootstrap setting, because <c>UnifiedBootstrap</c> never delegates to the
        /// Netcode for Entities bootstrap that reads it.
        /// </summary>
        [Test]
        public void AutomaticBootstrapIsNotRequiredToBeDisabled()
        {
            m_Config.HostWorldModeSelection = NetcodeConfig.HostWorldMode.SingleWorld;
            m_Config.EnableClientServerBootstrap = NetcodeConfig.AutomaticBootstrapSetting.EnableAutomaticBootstrap;

            Assert.IsFalse(HybridNetcodeDefaults.IsMissingRequired(m_Config, out _), "Automatic bootstrapping should no longer block hybrid mode.");
        }

        [Test]
        public void SnapshotPacketSizeIsOptOutThroughNetworkConfig()
        {
            Assert.IsTrue(new NetworkConfig().AutoConfigureSnapshotSize, "The snapshot size should be configured by default, with an opt out.");
        }
    }
}
#endif
