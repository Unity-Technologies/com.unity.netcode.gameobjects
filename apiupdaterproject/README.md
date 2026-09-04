# API updater upgrade-path project

This project validates that an **NGO 2.x** project's scripts are migrated automatically when upgrading to **NGO 3.x**.


NGO 3.0 renamed the editor assembly and its namespaces:

| 2.x | 3.x |
| --- | --- |
| `Unity.Netcode.Editor` (assembly) | `Unity.Netcode.GameObjects.Editor` |
| `Unity.Netcode.Editor` (namespace) | `Unity.Netcode.GameObjects.Editor` |
| `Unity.Netcode.Editor.Configuration` | `Unity.Netcode.GameObjects.Editor.Configuration` |
| `Unity.Netcode.Editor.CodeGen` | `Unity.Netcode.GameObjects.Editor.CodeGen` |
| `Unity.Netcode.PackageChecker.Editor` | `Unity.Netcode.GameObjects.PackageChecker.Editor` |

It also relocated the runtime timing types out of the root namespace, so that correcting the casing
of Netcode for Entities' `Unity.NetCode` namespace does not collide with them:

| 2.x | 3.x |
| --- | --- |
| `Unity.Netcode.NetworkTime` | `Unity.Netcode.GameObjects.Timing.NetworkTime` |
| `Unity.Netcode.NetworkTimeSystem` | `Unity.Netcode.GameObjects.Timing.NetworkTimeSystem` |
| `Unity.Netcode.NetworkTickSystem` | `Unity.Netcode.GameObjects.Timing.NetworkTickSystem` |

The **assembly is unchanged** for the timing move — only the namespace — so those three carry a null
`sourceAssembly`, which the attribute reads as "unchanged".


### NGO v2.x.x Unity.Netcode.Editor changes

If there is a need to add new API to NGO v2.x.x, the above table should be updated and the DeprecatedApiUsage.cs
file or the DeprecatedApiUsageQualified.cs files are updated to reflect the added API.

## Contents

| Path | What it covers |
| --- | --- |
| `Assets/Editor/DeprecatedApiUsage.cs` | Every public 2.x editor type through `using` + simple name |
| `Assets/Editor/DeprecatedApiUsageQualified.cs` | Fully qualified names, namespace alias, type alias, base type, `typeof`, generic |
| `Assets/Runtime/DeprecatedTimingUsage.cs` | The three relocated timing types, in every reference form plus a constructor call |
| `Assets/UpgradeProbeBehaviour.cs` | The `MonoBehaviour` used as the `NetcodeEditorBase<TT>` type argument |
| `Assets/CollisionStub~/` | An assembly occupying the two colliding timing names. Inert — Unity does not import a folder whose name ends in `~` — until `--collision-stub` copies it in |

`UpgradeProbeBehaviour` exists so the test does not name NGO's `NetworkManager`: com.unity.transport
6.6.0 — the builtin on some 6000.6 editors — ships a `Unity.Netcode.NetworkManager` of its own in
`Unity.Networking.Transport.NetcodeInterop`, which makes any `NetworkManager` reference from an
auto-referencing assembly like `Assembly-CSharp-Editor` CS0433-ambiguous. The type argument is
incidental to what is being measured, so a local `MonoBehaviour` keeps the test independent of the
resolved transport version.


## Running it locally

`run_upgrade_test.py` imports the project in batch mode with `-accept-apiupdate`, then asserts that
every 2.x type reference under `Assets/Editor` and `Assets/Runtime` was rewritten and that none
survived. It restores the 2.x sources when it finishes, so it can be re-run. Windows, macOS and Linux.

```sh
python run_upgrade_test.py --unity <editor> --clean --keep-updated-sources
```

| Option | |
| --- | --- |
| `--unity` | Omit it if `UNITY_EDITOR_PATH` is set, or if the hub has the version named in `ProjectSettings/ProjectVersion.txt`. |
| `--clean` | Purges `Library` and `Temp` first for a cold import. |
| `--keep-updated-sources` | Leaves the rewritten sources in place so `git diff` shows exactly what the updater produced. |
| `--collision-stub` | Adds an assembly occupying `Unity.Netcode.NetworkTime` and `NetworkTimeSystem`, then **inverts** the expectation for those two. See below. |

### The `--collision-stub` run

This is the regression test for the reason the timing move exists. With the stub installed, a 2.x
reference to `NetworkTime` still resolves — to the stub — so it never fails to resolve, never reaches
the `MovedFrom` data, and cannot be migrated. `NetworkTickSystem` is deliberately **not** in the stub,
so the same run asserts that one still migrates. A pass therefore proves both halves:

| Type | Expected under `--collision-stub` |
| --- | --- |
| `Unity.Netcode.NetworkTime` | **not** rewritten |
| `Unity.Netcode.NetworkTimeSystem` | **not** rewritten |
| `Unity.Netcode.NetworkTickSystem` | rewritten |
| every editor type | rewritten |

If a future change ever makes the two blocked rows pass as "rewritten", the mechanism has changed and
the one-sided-move conclusion needs revisiting.

Default hub locations, if you need to pass `--unity` explicitly — note that on macOS the binary is
inside the `.app` bundle rather than beside it:

| | |
| --- | --- |
| Windows | `C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe` |
| macOS | `/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity` |
| Linux | `$HOME/Unity/Hub/Editor/<version>/Editor/Unity` |


## How the migration works

Every relocated public editor type carries

```csharp
[MovedFrom(true, "Unity.Netcode.Editor", "Unity.Netcode.Editor", null)]
```

(`"Unity.Netcode.Editor.Configuration"` as the source namespace for the two types that were in it).
The arguments are `autoUpdateAPI, sourceNamespace, sourceAssembly, sourceClassName` — a null class
name means the type name itself did not change.

The three relocated timing types carry

```csharp
[MovedFrom(true, "Unity.Netcode", null, null)]
```

with `sourceAssembly` null because `Unity.Netcode.Runtime` keeps its name: any null argument is read
as "this did not change", and its value is taken from the decorated type.

A 2.x reference no longer resolves, so the compiler reports CS0246/CS0234. Unity's `ScriptUpdater`
consults the `MovedFrom` data extracted from the referenced assemblies, matches the old
namespace/assembly, and rewrites the reference. Nothing extra ships: no skeleton assembly, no
duplicate API surface.

`MovedFrom` is consulted **only** for references that fail to resolve. That is why the old namespace
must not be kept alive by anything — a type that still resolves never reaches the MovedFrom path.
