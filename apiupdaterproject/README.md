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


### NGO v2.x.x Unity.Netcode.Editor changes

If there is a need to add new API to NGO v2.x.x, the above table should be updated and the DeprecatedApiUsage.cs
file or the DeprecatedApiUsageQualified.cs files are updated to reflect the added API.

## Contents

| Path | What it covers |
| --- | --- |
| `Assets/Editor/DeprecatedApiUsage.cs` | Every public 2.x editor type through `using` + simple name |
| `Assets/Editor/DeprecatedApiUsageQualified.cs` | Fully qualified names, namespace alias, type alias, base type, `typeof`, generic |
| `Assets/UpgradeProbeBehaviour.cs` | The `MonoBehaviour` used as the `NetcodeEditorBase<TT>` type argument |

`UpgradeProbeBehaviour` exists so the test does not name NGO's `NetworkManager`: com.unity.transport
6.6.0 — the builtin on some 6000.6 editors — ships a `Unity.Netcode.NetworkManager` of its own in
`Unity.Networking.Transport.NetcodeInterop`, which makes any `NetworkManager` reference from an
auto-referencing assembly like `Assembly-CSharp-Editor` CS0433-ambiguous. The type argument is
incidental to what is being measured, so a local `MonoBehaviour` keeps the test independent of the
resolved transport version.

**Do not "fix" the sources under `Assets/Editor`.** They are deliberately written against the 2.x API
— they are the input to the test.

## Running it locally

`run_upgrade_test.py` imports the project in batch mode with `-accept-apiupdate`, then asserts that
every 2.x type reference under `Assets/Editor` was rewritten and that none survived. It restores the
2.x sources when it finishes, so it can be re-run. Windows, macOS and Linux.

```sh
python run_upgrade_test.py --unity <editor> --clean --keep-updated-sources
```

| Option | |
| --- | --- |
| `--unity` | Omit it if `UNITY_EDITOR_PATH` is set, or if the hub has the version named in `ProjectSettings/ProjectVersion.txt`. |
| `--clean` | Purges `Library` and `Temp` first for a cold import. |
| `--keep-updated-sources` | Leaves the rewritten sources in place so `git diff` shows exactly what the updater produced. |

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

A 2.x reference no longer resolves, so the compiler reports CS0246/CS0234. Unity's `ScriptUpdater`
consults the `MovedFrom` data extracted from the referenced assemblies, matches the old
namespace/assembly, and rewrites the reference. Nothing extra ships: no skeleton assembly, no
duplicate API surface.

`MovedFrom` is consulted **only** for references that fail to resolve. That is why the old namespace
must not be kept alive by anything — a type that still resolves never reaches the MovedFrom path.
