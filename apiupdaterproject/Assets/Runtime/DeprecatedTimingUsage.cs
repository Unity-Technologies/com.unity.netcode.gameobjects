// Update only if the relocated runtime timing API changes. Deliberately written against the
// pre-move namespace: this file is the test's input, not code to clean up. See ../../README.md.
//
// Each of the three types is named at least once in fully qualified form, so a --collision-stub run
// can tell "was not rewritten" apart from "was never referenced".
#pragma warning disable 169 // Ignore field is never used warnings

using System;
using System.Collections.Generic;
using Unity.Netcode;
using TimeNs = Unity.Netcode;
using TimeValue = Unity.Netcode.NetworkTime;

namespace ApiUpdaterProject
{
    // Unity.Netcode -> Unity.Netcode.GameObjects.Timing
    internal class DeprecatedTimingUsage
    {
        // using directive plus simple name
        private NetworkTime m_SimpleName;
        private NetworkTimeSystem m_TimeSystem;
        private NetworkTickSystem m_TickSystem;

        // Fully qualified
        private Unity.Netcode.NetworkTime m_TimeFullyQualified;
        private Unity.Netcode.NetworkTimeSystem m_TimeSystemFullyQualified;
        private Unity.Netcode.NetworkTickSystem m_TickSystemFullyQualified;

        // Through a namespace alias, and through a type alias
        private TimeNs.NetworkTickSystem m_ThroughNamespaceAlias;
        private TimeValue m_ThroughTypeAlias;

        // As a generic type argument
        private List<NetworkTime> m_AsGenericArgument;

        // typeof
        private Type TimeSystemType => typeof(NetworkTimeSystem);

        // Constructor call, and as a return type
        private NetworkTime Construct(uint tickRate) => new NetworkTime(tickRate, 0d);

        // As a parameter type
        private static double TickOf(NetworkTime time) => time.TickWithPartial;
    }
}
