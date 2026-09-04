// Stands in for a second package occupying Unity.Netcode.NetworkTime and
// Unity.Netcode.NetworkTimeSystem, which is what Netcode for Entities does once the casing of its
// Unity.NetCode namespace is corrected.
//
// Only those two names collide. NetworkTickSystem deliberately is not declared here, so a
// --collision-stub run asserts both halves of the finding in one pass: the updater migrates
// NetworkTickSystem, and it cannot migrate the two whose old names still resolve.
//
// Inert until run_upgrade_test.py --collision-stub copies this folder into place. Unity does not
// import a directory whose name ends in '~'.
namespace Unity.Netcode
{
    public struct NetworkTime
    {
        public int ServerTick;
    }

    public class NetworkTimeSystem
    {
        public uint EffectiveInputLatencyTicks;
    }
}
