# apiupdaterproject — agent notes

Background for anyone changing this project or the relocation metadata it tests. `README.md` covers
what it is and how to run it; this file covers why it is built this way and what will bite you.

## Orientation

* This is a standalone Unity project at the repo root. It is not part of `testproject` or
  `minimalproject`, and the package does not reference it.
* It validates one thing end to end: that a project written against the **NGO 2.x** editor API is
  migrated automatically by Unity's API updater when the package is upgraded to **3.x**.
* **The mechanism it tests does not live here.** The `[MovedFrom]` attributes are on the real types in
  `com.unity.netcode.gameobjects/Editor/**`. This project only consumes them.
* **Do not "fix" the sources under `Assets/Editor`.** They are deliberately written against the 2.x
  API and are the input to the test. A helpful cleanup there silently guts it.
* The expected-type list in the run scripts is frozen: it enumerates the public editor API of
  `develop-2.0.0`, which is released and cannot change. It only needs extending if a public editor
  type is relocated again within 3.x.
* CI runs it on demand only — comment `/ci apiupdater` on a PR. See `.yamato/api-updater-test.yml`.
* Verified beyond this project: a real sample project upgraded 7 of its own scripts automatically,
  including a `NetcodeEditorBase<T>` subclass.

## Why not `[Obsolete(... (UnityUpgradable))]` skeletons

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

## Measured behaviour

Probed against 6000.7.0a5 with throwaway types, for a namespace + assembly move:

| Mechanism / `(UnityUpgradable)` target form | Non-generic type | Generic type |
| --- | --- | --- |
| `[Asm] Ns.Type` | rewritten, fully qualified | name replaced, namespace dropped |
| `[Asm] Ns.Type<TT>` | n/a | name replaced, namespace dropped (a no-op when the name is unchanged) |
| ``[Asm] Ns.Type`1`` | n/a | backtick emitted into the source verbatim |
| `* [Asm] Ns.Type<TT>` | n/a | not rewritten |
| `[MovedFrom(true, oldNs, oldAsm, null)]` | rewritten, fully qualified | rewritten, fully qualified |

Reference forms `MovedFrom` was confirmed to handle, via `Assets/Editor/DeprecatedApiUsage.cs` and
`Assets/Editor/DeprecatedApiUsageQualified.cs`: `using` + simple name, fully qualified name, namespace
alias, type alias, base type, `typeof`, and generic type argument. The dead
`using Unity.Netcode.Editor;` directives are removed and namespace aliases are rewritten in place
rather than expanded at each use.

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
