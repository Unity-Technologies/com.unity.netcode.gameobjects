#if UNIFIED_NETCODE
#if !UNIFIED_NETCODE_7_0_0
using NetcodeConfig = Unity.NetCode.NetCodeConfig;
#endif

namespace Unity.Netcode
{
    /// <summary>
    /// This handles applying two <see cref="NetcodeConfig"/> settings NGO requires and an optional recommended snapshot size when running in hybrid mode.
    /// </summary>
    /// <remarks>
    /// This intentionally does not modify the <see cref="NetcodeConfig"/> asset to preserve the default settings and/or any user adjustments other than the ones required.
    /// </remarks>
    internal static class HybridNetcodeDefaults
    {
        // A hybrid ghost costs ~4.87 bytes per snapshot, so this carries ~840 of them at the full tick rate, which
        // covers the 200-1000 moving instances projects typically run. The default MTU size will round-robin at around ~230 instances.
        internal const int SnapshotPacketSize = 4096;

        /// <summary>
        /// Assures N4E's tick rate matches NGO's <see cref="NetworkConfig.TickRate"/> setting.
        /// </summary>
        /// <remarks>
        /// Not cosmetic: NGO's own send queues are flushed by a system in N4E's SimulationSystemGroup, which steps at
        /// SimulationTickRate, so a rate below <see cref="NetworkConfig.TickRate"/> starves NGO's outbound traffic.
        /// </remarks>
        /// <param name="config">The configuration to determine if NGO's tick rate should be applied.</param>
        /// <param name="tickRate">The owning <see cref="NetworkManager"/>'s configured tick rate.</param>
        /// <returns>True if anything changed.</returns>
        internal static bool ApplyTickRate(NetcodeConfig config, uint tickRate)
        {
            var rate = (int)tickRate;
            if (config.ClientServerTickRate.SimulationTickRate == rate && config.ClientServerTickRate.NetworkTickRate == rate)
            {
                return false;
            }

            // Both are written because you can set NetworkTickRate to 0 which N4E would then use SimulationTickRate. 
            // Keeping both locked in at NGO's tick rate (runtime only) assures there can be no deviation.
            config.ClientServerTickRate.SimulationTickRate = rate;
            config.ClientServerTickRate.NetworkTickRate = rate;
            return true;
        }

        /// <summary>
        /// Sets N4E's snapshot packet size to <see cref="SnapshotPacketSize"/>.
        /// </summary>
        /// <param name="config">The config to correct.</param>
        /// <returns>True if anything changed.</returns>
        internal static bool ApplySnapshotPacketSize(NetcodeConfig config)
        {
            if (config.GhostSendSystemData.DefaultSnapshotPacketSize == SnapshotPacketSize)
            {
                return false;
            }

            config.GhostSendSystemData.DefaultSnapshotPacketSize = SnapshotPacketSize;
            return true;
        }

        /// <summary>
        /// Determines if the NetcodeConfig about to be used to start a session meets the required settings.
        /// </summary>
        /// <param name="config">The config to check.</param>
        /// <param name="reason">Populated with a user-facing description of what is wrong.</param>
        /// <returns>True when <paramref name="config"/> is not configured correctly.</returns>
        internal static bool IsMissingRequired(NetcodeConfig config, out string reason)
        {
            if (config.HostWorldModeSelection != NetcodeConfig.HostWorldMode.SingleWorld)
            {
                reason = $"{nameof(NetcodeConfig.HostWorldModeSelection)} must be {nameof(NetcodeConfig.HostWorldMode.SingleWorld)} but is {config.HostWorldModeSelection}";
                return true;
            }

            reason = null;
            return false;
        }
    }
}
#endif
