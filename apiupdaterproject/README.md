# API updater upgrade-path project

A small Unity project whose only job is to prove that an **NGO 2.x** project's editor scripts are
migrated automatically when the package is upgraded to **NGO 3.x**.

NGO 3.0 renamed the editor assembly and its namespaces:

| 2.x | 3.x |
| --- | --- |
| `Unity.Netcode.Editor` (assembly) | `Unity.Netcode.GameObjects.Editor` |
| `Unity.Netcode.Editor` (namespace) | `Unity.Netcode.GameObjects.Editor` |
| `Unity.Netcode.Editor.Configuration` | `Unity.Netcode.GameObjects.Editor.Configuration` |
| `Unity.Netcode.Editor.CodeGen` | `Unity.Netcode.GameObjects.Editor.CodeGen` |
| `Unity.Netcode.PackageChecker.Editor` | `Unity.Netcode.GameObjects.PackageChecker.Editor` |

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

### Why not `[Obsolete(... (UnityUpgradable))]` skeletons

That is the other mechanism for this, and it was measured first: a second assembly declaring an empty
skeleton of each 2.x type under the old namespace, each carrying
`[Obsolete("... (UnityUpgradable) -> [asm] ns.Type", true)]`. It works for every non-generic type, but

* it **cannot** relocate a generic type — a target carrying a type argument list is treated as a
  same-namespace *rename*, so the namespace and assembly are dropped (see the table below), which
  left `NetcodeEditorBase<TT>` needing `MovedFrom` anyway;
* it costs a second assembly and a hand-maintained parallel API surface that has to track the real
  one's `#if` guards and eventually be deleted;
* it leaves the stale `using` directives and expands namespace aliases at the reference site, where
  `MovedFrom` removes the dead usings and rewrites aliases in place.

What it buys, and `MovedFrom` does not, is a better error when the user *declines* the update:
`'NetworkManagerEditor' is obsolete: ... Use Unity.Netcode.GameObjects.Editor.NetworkManagerEditor
instead` rather than a bare CS0246. It is also the only route for member-level redirects (a renamed
method, a changed signature) and for type *renames*, which `MovedFrom` explicitly does not support.
Neither applies to this change — it is a pure relocation.

## Running it

```powershell
.\run-upgrade-test.ps1 -UnityExe "C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe"
```

The script imports the project in batch mode with `-accept-apiupdate`, then asserts that every 2.x
type reference under `Assets/Editor` was rewritten and that none survived. It restores the 2.x
sources when it finishes, so it can be re-run; pass `-KeepUpdatedSources` to inspect exactly what the
updater produced (`git diff` then shows the rewrite). `-Clean` purges `Library` first for a cold
import.

`-UnityExe` may be omitted if `UNITY_EDITOR_PATH` is set or if the hub has the version named in
`ProjectSettings/ProjectVersion.txt`.

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

## Measured behaviour

Probed against 6000.7.0a5 with throwaway types, for a namespace + assembly move:

| Mechanism / `(UnityUpgradable)` target form | Non-generic type | Generic type |
| --- | --- | --- |
| `[Asm] Ns.Type` | rewritten, fully qualified | name replaced, namespace dropped |
| `[Asm] Ns.Type<TT>` | n/a | name replaced, namespace dropped (a no-op when the name is unchanged) |
| `[Asm] Ns.Type`1` | n/a | backtick emitted into the source verbatim |
| `* [Asm] Ns.Type<TT>` | n/a | not rewritten |
| `[MovedFrom(true, oldNs, oldAsm, null)]` | rewritten, fully qualified | rewritten, fully qualified |

Reference forms `MovedFrom` was confirmed to handle, via this project's two source files: `using` +
simple name, fully qualified name, namespace alias, type alias, base type, `typeof`, and generic type
argument. The dead `using Unity.Netcode.Editor;` directives are removed and namespace aliases are
rewritten in place rather than expanded at each use.

## Known gap: assembly definition references

The updater rewrites C# source only; it does not touch `.asmdef` files.

References made **by GUID** — the Unity default — keep working untouched. A GUID reference resolves
to whichever `.asmdef` *asset* carries that GUID, independent of the `name` field inside it, and
`Editor/Unity.Netcode.Editor.asmdef` kept both its path and its GUID through the rename. So a 2.x
project referencing it by GUID silently ends up referencing `Unity.Netcode.GameObjects.Editor`.

References made **by name** (`"Unity.Netcode.Editor"`) no longer resolve and have to be repointed at
`Unity.Netcode.GameObjects.Editor` by hand. The same applies to the other renamed assemblies:
`Unity.Netcode.Editor.CodeGen` and `Unity.Netcode.PackageChecker.Editor`. Nothing can be done about
this from the package side — reviving the old assembly name is not an option, because
`Unity.Netcode.Editor` differs from N4E's `Unity.NetCode.Editor` only by the case of one letter and
the two `Library/ScriptAssemblies/*.dll` filenames collide when both packages are installed. Removing
that collision is what the 3.0 rename is for.
