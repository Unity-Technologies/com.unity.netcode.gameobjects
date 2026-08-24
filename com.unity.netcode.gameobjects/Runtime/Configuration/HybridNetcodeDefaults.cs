#if UNIFIED_NETCODE
using Unity.NetCode;

namespace Unity.Netcode
{
    /// <summary>
    /// The <see cref="NetCodeConfig"/> values NGO needs when running in hybrid mode (i.e. Netcode for Entities is
    /// installed and at least one registered network prefab carries a <see cref="GhostObject"/>).
    /// </summary>
    /// <remarks>
    /// This lives in the runtime assembly rather than the editor one because <see cref="NetCodeConfig.HostWorldModeSelection"/>
    /// is internal to Unity.NetCode, and Unity.Netcode.Runtime is the only NGO assembly it grants InternalsVisibleTo to.
    /// Nothing here touches the AssetDatabase; the editor-side applier drives all of it.
    /// </remarks>
    internal static class HybridNetcodeDefaults
    {
        /// <summary>
        /// Bump whenever <see cref="ApplyRecommended"/> changes so that an upgrading project re-applies exactly once.
        /// Persisted as NetcodeForGameObjectsProjectSettings.HybridDefaultsVersion.
        /// </summary>
        internal const int Version = 1;

        // Tuned against 2000 GenericPhysicsBallNGO instances in the ngo-examples project. A hybrid ghost costs ~4.87
        // bytes per snapshot, so 15000 carries ~3000 of them at the full tick rate. This is a cap and not a cost:
        // below that count it puts no more on the wire than the N4E default would.
        internal const int SnapshotPacketSize = 15000;

        // A ceiling on despawn bytes, not a reservation, so unused headroom is free. 0.2 is also N4E's clamp minimum.
        internal const float PercentReservedForDespawn = 0.2f;

        // Expressed in milliseconds rather than net ticks deliberately. N4E rounds this up to whole network ticks, so
        // it holds >= 50ms of interpolation buffer at any tick rate. The net-tick form does not: 2 net ticks is 66.7ms
        // at 30Hz but only 33.3ms at 60Hz, and 33.3ms is the buffer the stress test stuttered at.
        internal const uint InterpolationTimeMS = 50;

        internal const float InterpolationDelayMaxDeltaTicksFraction = 0.15f;
        internal const float InterpolationTimeScaleMin = 0.9f;
        internal const float InterpolationTimeScaleMax = 1.33f;

        // A full snapshot fragments into ~11 datagrams and each fragment consumes a queue slot.
        internal const int ClientQueueCapacity = 128;

        /// <summary>
        /// Applies the two settings hybrid mode cannot run without.
        /// </summary>
        /// <param name="config">The config to correct.</param>
        /// <returns>True if anything changed.</returns>
        internal static bool ApplyRequired(NetCodeConfig config)
        {
            var changed = false;

            // NetworkManager gates the world spin-up, so N4E must not bootstrap worlds on its own.
            if (config.EnableClientServerBootstrap != NetCodeConfig.AutomaticBootstrapSetting.DisableAutomaticBootstrap)
            {
                config.EnableClientServerBootstrap = NetCodeConfig.AutomaticBootstrapSetting.DisableAutomaticBootstrap;
                changed = true;
            }

            if (config.HostWorldModeSelection != NetCodeConfig.HostWorldMode.SingleWorld)
            {
                config.HostWorldModeSelection = NetCodeConfig.HostWorldMode.SingleWorld;
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Drives N4E's tick rates from <see cref="NetworkConfig.TickRate"/> so that ghost transform updates land on
        /// the same interval NGO uses for everything else.
        /// </summary>
        /// <param name="config">The config to correct.</param>
        /// <param name="tickRate">The owning <see cref="NetworkManager"/>'s configured tick rate.</param>
        /// <returns>True if anything changed.</returns>
        internal static bool ApplyTickRate(NetCodeConfig config, uint tickRate)
        {
            var rate = (int)tickRate;
            if (config.ClientServerTickRate.SimulationTickRate == rate && config.ClientServerTickRate.NetworkTickRate == rate)
            {
                return false;
            }

            // Both are written: leaving NetworkTickRate at 0 would track SimulationTickRate anyway, but writing it
            // keeps the two visibly locked in the inspector, which is the invariant InterpolationTimeMS relies on.
            config.ClientServerTickRate.SimulationTickRate = rate;
            config.ClientServerTickRate.NetworkTickRate = rate;
            return true;
        }

        /// <summary>
        /// Applies the full NGO-recommended set: <see cref="ApplyRequired"/>, <see cref="ApplyTickRate"/>, and the
        /// values tuned against the stress test.
        /// </summary>
        /// <param name="config">The config to correct.</param>
        /// <param name="tickRate">The owning <see cref="NetworkManager"/>'s configured tick rate.</param>
        /// <returns>True if anything changed.</returns>
        internal static bool ApplyRecommended(NetCodeConfig config, uint tickRate)
        {
            var changed = ApplyRequired(config);
            changed |= ApplyTickRate(config, tickRate);

            changed |= Set(ref config.GhostSendSystemData.DefaultSnapshotPacketSize, SnapshotPacketSize);
            changed |= Set(ref config.GhostSendSystemData.PercentReservedForDespawnMessages, PercentReservedForDespawn);

            // The net-tick form has to be cleared or it wins over the millisecond form.
            changed |= Set(ref config.ClientTickRate.InterpolationTimeNetTicks, 0u);
            changed |= Set(ref config.ClientTickRate.InterpolationTimeMS, InterpolationTimeMS);
            changed |= Set(ref config.ClientTickRate.InterpolationDelayMaxDeltaTicksFraction, InterpolationDelayMaxDeltaTicksFraction);
            changed |= Set(ref config.ClientTickRate.InterpolationTimeScaleMin, InterpolationTimeScaleMin);
            changed |= Set(ref config.ClientTickRate.InterpolationTimeScaleMax, InterpolationTimeScaleMax);

            changed |= Set(ref config.ClientSendQueueCapacity, ClientQueueCapacity);
            changed |= Set(ref config.ClientReceiveQueueCapacity, ClientQueueCapacity);

            return changed;
        }

        /// <summary>
        /// Reports the first required setting that is still wrong, for the runtime start-up check.
        /// </summary>
        /// <param name="config">The config to inspect.</param>
        /// <param name="reason">Populated with a user-facing description of what is wrong.</param>
        /// <returns>True when <paramref name="config"/> cannot support hybrid mode as-is.</returns>
        internal static bool IsMissingRequired(NetCodeConfig config, out string reason)
        {
            if (config.HostWorldModeSelection != NetCodeConfig.HostWorldMode.SingleWorld)
            {
                reason = $"{nameof(NetCodeConfig.HostWorldModeSelection)} must be {nameof(NetCodeConfig.HostWorldMode.SingleWorld)} but is {config.HostWorldModeSelection}";
                return true;
            }

            if (config.EnableClientServerBootstrap != NetCodeConfig.AutomaticBootstrapSetting.DisableAutomaticBootstrap)
            {
                reason = $"{nameof(NetCodeConfig.EnableClientServerBootstrap)} must be {nameof(NetCodeConfig.AutomaticBootstrapSetting.DisableAutomaticBootstrap)} because {nameof(NetworkManager)} owns world creation in hybrid mode";
                return true;
            }

            reason = null;
            return false;
        }

        private static bool Set<T>(ref T target, T value)
            where T : System.IEquatable<T>
        {
            if (target.Equals(value))
            {
                return false;
            }

            target = value;
            return true;
        }
    }
}
#endif
