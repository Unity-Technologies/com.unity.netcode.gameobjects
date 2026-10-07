#if UNIFIED_NETCODE
#if !UNIFIED_NETCODE_7_0_0
using NetcodeConfig = Unity.NetCode.NetCodeConfig;
#endif

namespace Unity.Netcode
{
    /// <summary>
    /// The <see cref="NetcodeConfig"/> values NGO drives when running in hybrid mode (i.e. Netcode for Entities is
    /// installed and a registered network prefab carries a <see cref="GhostObject"/>).
    /// </summary>
    /// <remarks>
    /// This lives in the runtime assembly rather than the editor one because <see cref="NetcodeConfig.HostWorldModeSelection"/>
    /// is internal to Netcode for Entities, and Unity.Netcode.Runtime is the only NGO assembly it grants InternalsVisibleTo to.
    /// Nothing is written to the asset on disk: <see cref="NetworkManager"/> writes the in-memory config just before
    /// world creation, and Netcode for Entities seeds its world singletons from it at that point.
    /// </remarks>
    internal static class HybridNetcodeDefaults
    {
        // A hybrid ghost costs ~4.87 bytes per snapshot, so this carries ~840 of them at the full tick rate, which
        // covers the 200-1000 moving instances projects typically run. A cap and not a cost: below that count the
        // snapshot never reaches it. N4E's own default is one MTU, which round-robins above ~230 ghosts.
        internal const int SnapshotPacketSize = 4096;

        /// <summary>
        /// Drives N4E's tick rates from <see cref="NetworkConfig.TickRate"/>.
        /// </summary>
        /// <remarks>
        /// Not cosmetic: NGO's own send queues are flushed by a system in N4E's SimulationSystemGroup, which steps at
        /// SimulationTickRate, so a rate below <see cref="NetworkConfig.TickRate"/> starves NGO's outbound traffic.
        /// </remarks>
        /// <param name="config">The config to correct.</param>
        /// <param name="tickRate">The owning <see cref="NetworkManager"/>'s configured tick rate.</param>
        /// <returns>True if anything changed.</returns>
        internal static bool ApplyTickRate(NetcodeConfig config, uint tickRate)
        {
            var rate = (int)tickRate;
            if (config.ClientServerTickRate.SimulationTickRate == rate && config.ClientServerTickRate.NetworkTickRate == rate)
            {
                return false;
            }

            // Both are written: leaving NetworkTickRate at 0 would track SimulationTickRate anyway, but writing it
            // keeps the two visibly locked in the inspector.
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
        /// Reports the first required setting that is still wrong, for the runtime start-up check.
        /// </summary>
        /// <param name="config">The config to inspect.</param>
        /// <param name="reason">Populated with a user-facing description of what is wrong.</param>
        /// <returns>True when <paramref name="config"/> cannot support hybrid mode as-is.</returns>
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
