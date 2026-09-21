# apiupdaterproject — agent notes

Background for anyone changing this project or the relocation metadata it tests. `README.md` covers
what it is and how to run it; this file covers why it is built this way and what will bite you.

## Orientation

* This is a standalone Unity project at the repo root. It is not part of `testproject` or
  `minimalproject`, and the package does not reference it.
* It validates one thing end to end: that a project written against the **NGO 2.x** API is migrated
  automatically by Unity's API updater when the package is upgraded to **3.x**. Two relocations are
  covered — the editor namespaces, and the runtime timing types.
* **The mechanism it tests does not live here.** The `[MovedFrom]` attributes are on the real types in
  `com.unity.netcode.gameobjects/Editor/**` and `com.unity.netcode.gameobjects/Runtime/Timing/**`.
  This project only consumes them.
* **Do not "fix" the sources under `Assets/Editor` or `Assets/Runtime`.** They are deliberately
  written against the 2.x API and are the input to the test. A helpful cleanup there silently guts it.
* The two editor blocks of `EXPECTED_MOVES` in `run_upgrade_test.py` are frozen: they enumerate the
  public editor API of `develop-2.0.0`, which is released and cannot change. A block only needs
  extending if a public type is relocated again within 3.x — as the timing types were.
* CI runs it on demand only — comment `/ci apiupdater` on a PR. See `.yamato/api-updater-test.yml`.
* **Opening this project locally mutates it.** Unity rewrites `ProjectVersion.txt` to whatever editor
  opened it, and the package manager can add builtin modules to `Packages/manifest.json` that only
  exist in that editor — `com.unity.modules.smartstrings` from a 6000.7 alpha broke the 6000.6 CI job
  exactly this way. Check `git diff` on those two files before committing, and keep the manifest to
  modules that exist in the editor the job downloads (`validation_editors.default`).
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

## The timing move is a namespace-only relocation, and that is a different case

Every row measured above was a namespace **and** assembly move. The timing types keep their assembly
(`Unity.Netcode.Runtime`), so they carry `[MovedFrom(true, "Unity.Netcode", null, null)]` — a null
`sourceAssembly`, which `MovedFromAttributeData.Set` records as `assemblyHasChanged = false`.

Two things about that are worth knowing before trusting it:

* **The null form is the documented one.** The attribute's own comment states that any null string is
  read as "has not changed" and its value is taken from the decorated type, and there is a
  single-argument `MovedFromAttribute(string sourceNamespace)` constructor that does exactly
  `Set(true, ns, null, null)`. Passing the real assembly name instead would set `assemblyHasChanged`
  for a change that did not happen.
* **It has not been measured here.** The table above has no namespace-only row. `Assets/Runtime` plus
  the timing block in `EXPECTED_MOVES` is what settles it; a `/ci apiupdater` run is the proof.

Do not reach for `AffectsAPIUpdater` to reason about this. It reads
`!classHasChanged && !assemblyHasChanged`, which would make it false for the editor move — and the
editor move demonstrably works, so whatever that property gates, it is not script rewriting.

### Open form: a namespace alias whose target survives

`Assets/Runtime/DeprecatedTimingUsage.cs` contains `using TimeNs = Unity.Netcode;` used as
`TimeNs.NetworkTickSystem`. This is **not** the same case as the editor project's
`using Cfg = Unity.Netcode.Editor.Configuration;`: there the alias target itself stopped resolving and
was rewritten in place, whereas `Unity.Netcode` still exists and still holds `NetworkBehaviour` and
the rest. So the alias target cannot be rewritten and the use site has to be. The assertions do not
depend on this form — `NetworkTickSystem` is also referenced by simple name and fully qualified — so
if the updater leaves that one line alone the run still passes. Read the rewritten source rather than
assuming it was handled.

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
