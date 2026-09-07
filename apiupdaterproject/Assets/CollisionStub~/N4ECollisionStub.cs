// Stands in for a second installed package that declares Unity.Netcode.NetworkTimeSystem, so the
// upgrade test can assert what the API updater does when a relocated type's old name still resolves
// somewhere else. The updater is driven by resolution failure, so such a name is never rewritten.
//
// Deliberately not tied to any particular package's current layout: this asserts a property of the
// updater, and it stays worth testing whether or not another SDK happens to occupy the name today.
//
// Only this one name is occupied. NetworkTime and NetworkTickSystem are absent, so a --collision-stub
// run proves both halves at once - those two migrate, and this one cannot.
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
