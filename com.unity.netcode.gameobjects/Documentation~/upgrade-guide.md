# Upgrading from 2.x to 3.x

Netcode for GameObjects 3.x is a major release. This guide covers the breaking changes you may encounter when upgrading a project from the 2.x series to the 3.x series, and how to resolve each of them.

Before you begin, back up your project (or make sure it is committed to source control) so you can revert if needed.

## Editor and dependency requirements

- **Minimum Editor version.** Netcode for GameObjects 3.x requires Unity **6000.7** or later. Projects on earlier Editor versions must either stay on the 2.x series or upgrade the Editor before installing 3.x.
- **New dependency.** Netcode for GameObjects 3.x depends on the Netcode for Entities package (`com.unity.netcode`), which in turn brings in the Entities packages (`com.unity.entities`, `com.unity.collections`, `com.unity.burst`, `com.unity.mathematics`). Installing 3.x adds these packages to your project. This increases the project's package footprint and build times, and requires an Editor and target platforms that support Burst and Entities.

## Editor assembly definitions renamed

The Editor assembly definitions have been renamed to the `Unity.Netcode.GameObjects.*` form:

| Previous name | New name |
| --- | --- |
| `Unity.Netcode.Editor` | `Unity.Netcode.GameObjects.Editor` |
| `Unity.Netcode.Editor.CodeGen` | `Unity.Netcode.GameObjects.Editor.CodeGen` |
| `Unity.Netcode.Editor.PackageChecker` | `Unity.Netcode.GameObjects.PackageChecker.Editor` |
| `Unity.Netcode.Editor.Tests` | `Unity.Netcode.GameObjects.Editor.Tests` |

If your own Editor assemblies reference any of these by name in their assembly definition, update the reference to the new name. An API updater is included to migrate references to the moved Editor namespace types automatically; retrigger compilation if it does not resolve everything in one pass, and report any issues with the auto-update process.

The runtime assembly definition (`Unity.Netcode.Runtime`) and the runtime `Unity.Netcode` namespace are unchanged, so runtime scripts that only use `using Unity.Netcode;` do not need changes for this rename.

## Obsolete APIs now raise compile errors

A number of APIs that were already marked `[Obsolete]` with a warning in 2.x now raise a **compile error** in 3.x. The members have not been removed yet, so the error message points you to the replacement in each case. Update your code as follows.

### NetworkObject

- `NetworkObject.IsSceneObject` → use `NetworkObject.InScenePlaced`.
- `NetworkObject.SetSceneObjectStatus(bool)` → the in-scene status is now calculated during the build; remove these calls.

### RPCs

- The `RequireOwnership` field on `ServerRpc` / RPC attributes → use `InvokePermission` instead:
  - `[ServerRpc(RequireOwnership = true)]` → `[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]`
  - `[ServerRpc(RequireOwnership = false)]` → `[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]`

### UnityTransport

- `UnityTransport.InitialMaxSendQueueSize` → the max send queue size is now determined dynamically; remove use of this constant. You can still set `MaxSendQueueSize` programmatically if required.
- `ConnectionAddressData.ServerEndPoint` (`ParseNetworkEndpoint`) → use `NetworkEndpoint.Parse` on the `Address` field instead.
- `UnityTransport.DebugSimulator` and `UnityTransport.SetDebugSimulatorParameters(...)` → no longer supported and have no effect. Use the Network Simulator from the Multiplayer Tools package instead.

### Other APIs

- `CommandLineOptions.Instance` → replaced by `TryGetArg`.
- `NotListeningException` → no longer used.
- The obsolete list/property on `BufferedLinearInterpolator`, the obsolete property on `NetworkList`, and the obsolete method on `NetworkSpawnManager` are no longer usable; remove references to them.
- `NetworkBehaviourEditor.GetRootParentTransform(Transform)` → use `transform.root` instead.

If you were suppressing the previous warnings with `#pragma warning disable CS0618`, remove those suppressions and apply the replacements above, as the error form (CS0619) cannot be suppressed with a pragma.

## Behavioral changes

These changes do not require code edits, but they change runtime behavior. Review them if your project depends on the previous behavior.

- **`NetworkTransform.UseHalfFloatPrecision` resolution.** Position is now synchronized with a resolution of approximately 1mm regardless of how far an object has travelled (previously the resolution could degrade to approximately 3cm). This does not increase bandwidth, but projects using `NetworkTransform.UseUnreliableDeltas` will send full-precision position updates more often.
- **Interpolation smoothing is now frame-rate independent.** The `Lerp` and `SmoothDampening` interpolation types previously smoothed by different amounts at different frame rates because smoothing was applied per frame instead of over time. Results at 60fps are unchanged; results at other frame rates will differ from 2.x.

## Netcode for Entities integration (experimental)

Netcode for GameObjects 3.x introduces the foundations of the unified "GameObject layer for Netcode for Entities" work (the hybrid prefab concept). This integration is experimental in 3.x and is not enabled for standard GameObject workflows by default. Standard `NetworkBehaviour`, `NetworkVariable`, and RPC workflows are unaffected. If you are not using the Entities integration, no action is required beyond the dependency and Editor requirements described above.
