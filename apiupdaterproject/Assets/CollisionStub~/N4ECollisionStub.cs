// Stands in for a second package occupying Unity.Netcode.NetworkTimeSystem, which is what Netcode
// for Entities does as of 6.7.0: its casing correction moved 204 files into Unity.Netcode, and it
// sub-namespaced NetworkTime into Unity.Netcode.NetcodeTime but left NetworkTimeSystem behind.
//
// Only that one name collides. NetworkTime and NetworkTickSystem deliberately are not declared here,
// so a --collision-stub run asserts both halves of the finding in one pass: those two migrate, and
// the one whose old name still resolves cannot.
//
// Inert until run_upgrade_test.py --collision-stub copies this folder into place. Unity does not
// import a directory whose name ends in '~'.
namespace Unity.Netcode
{
    public class NetworkTimeSystem
    {
        public uint EffectiveInputLatencyTicks;
    }
}
