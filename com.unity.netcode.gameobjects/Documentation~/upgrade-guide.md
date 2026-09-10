# Upgrade from 2.x to 3.x

Update your project for the Unity Editor, assembly definition, dependency, and API changes that Netcode for GameObjects 3.x introduces.

Version 3.x of Netcode for GameObjects raises the minimum Unity Editor version, renames the Editor assembly definitions, and turns several obsolete API warnings into compile errors. Refer to the following sections for the changes that affect your project and the steps to take for each one.

> [!WARNING]
> The API updater modifies your files in place. Commit or back up your work before you update your project.

## Prerequisites

Before you upgrade, make sure that your project meets the following requirements:

- Unity Editor version 6.7 or later. If your project uses an earlier Editor version, either stay on version 2.x or upgrade the Editor first.
- Target platforms that support Burst and Entities.

Version 3.x depends on the Netcode for Entities package (`com.unity.netcode`), which in turn depends on the Entities packages (`com.unity.entities`, `com.unity.collections`, `com.unity.burst`, and `com.unity.mathematics`). Installing version 3.x adds these packages to your project. As a result, your project contains more packages, and its build times increase.

## Upgrade your project to version 3.x

To upgrade an existing project from version 2.x to version 3.x, follow these steps:

1. Back up your project, or commit your work to source control.
1. Upgrade your project to Unity Editor version 6.7 or later.
1. From the Unity Editor, select **Window** > **Package Manager**.
1. From the **Package Manager** window, select **Netcode for GameObjects** in the list of packages.
1. Select version 3.x, then select **Update**.
1. Wait for the API updater to finish, then open the **Console** window to review the remaining compile errors.
1. Resolve the remaining compile errors using the sections that follow.

After the API updater finishes and you resolve the compile errors, your project compiles against version 3.x. If the API updater doesn't resolve every reference, refer to [Continue an incomplete API update](#continue-an-incomplete-api-update).

### Continue an incomplete API update

The API updater doesn't always complete its work in one pass. If the update is incomplete, trigger compilation again so that the API updater can continue to apply its changes.

If you experience issues with the API updater, report the issue through the [Unity bug submission process](https://unity3d.com/unity/qa/bug-reporting).

## Update Editor assembly definition references

Version 3.x renames the Editor assembly definitions to the `Unity.Netcode.GameObjects.*` form:

| **Original name** | **New name** |
| --- | --- |
| `Unity.Netcode.Editor` | `Unity.Netcode.GameObjects.Editor` |
| `Unity.Netcode.Editor.CodeGen` | `Unity.Netcode.GameObjects.Editor.CodeGen` |
| `Unity.Netcode.PackageChecker.Editor` | `Unity.Netcode.GameObjects.PackageChecker.Editor` |
| `Unity.Netcode.Editor.Tests` | `Unity.Netcode.GameObjects.Editor.Tests` |

If your own Editor assemblies reference any of these assembly definitions by name, update the reference to the new name. The API updater migrates references to the moved Editor namespace types for you.

Version 3.x doesn't change the runtime assembly definition (`Unity.Netcode.Runtime`) or the runtime `Unity.Netcode` namespace. Runtime scripts that only use `using Unity.Netcode;` need no changes for this rename.

## Replace obsolete APIs that now raise compile errors

Version 2.x marks several APIs with `[Obsolete]` and raises a warning for each one. Version 3.x raises a compile error instead. Version 3.x doesn't remove the members, so the error message points you to the replacement in each case.

### APIs with replacements

Replace each of the following APIs with its listed replacement:

| **Original API** | **Replacement** |
| --- | --- |
| `NetworkObject.IsSceneObject` | `NetworkObject.InScenePlaced` |
| `[ServerRpc(RequireOwnership = true)]` | `[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]` |
| `[ServerRpc(RequireOwnership = false)]` | `[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]` |
| `ConnectionAddressData.ServerEndPoint` (`ParseNetworkEndpoint`) | `NetworkEndpoint.Parse` on the `Address` field |
| `NetworkBehaviourEditor.GetRootParentTransform(Transform)` | `transform.root` |

The `RequireOwnership` field applies to `ServerRpc` and the other remote procedure call (RPC) attributes. Use `InvokePermission` in its place. For more information, refer to [RPCs](advanced-topics/message-system/rpc.md).

### Replace command-line argument lookups

Version 3.x raises a compile error for both `CommandLineOptions.Instance` and the `CommandLineOptions.GetArg` instance method, so a typical `Instance.GetArg` call needs a full rewrite rather than a rename. Use the static `CommandLineOptions.TryGetArg` method, which reports whether it found the argument and returns the value through an `out` parameter:

```csharp
// Version 2.x
var value = CommandLineOptions.Instance.GetArg("--myArg");
if (value != null)
{
    // Use value.
}

// Version 3.x
if (CommandLineOptions.TryGetArg("--myArg", out var value))
{
    // Use value.
}
```

### APIs without replacements

Remove your use of each of the following APIs:

- `NetworkObject.SetSceneObjectStatus(bool)`: version 3.x computes the in-scene status during the build, so remove these calls.
- `UnityTransport.InitialMaxSendQueueSize`: version 3.x determines the maximum send queue size dynamically, so remove your use of this constant. You can still set `MaxSendQueueSize` using C# scripts if you need to.
- `UnityTransport.DebugSimulator` and `UnityTransport.SetDebugSimulatorParameters(...)`: version 3.x no longer supports these members, and they have no effect. Use the [Network Simulator tool](https://docs.unity3d.com/Packages/com.unity.multiplayer.tools@latest?subfolder=/manual/network-simulator) from the Multiplayer Tools package instead.
- `NotListeningException`: version 3.x no longer uses this exception.
- The obsolete list and property on `BufferedLinearInterpolator`, the obsolete property on `NetworkList`, and the obsolete method on `NetworkSpawnManager`: version 3.x no longer supports these members.

### Remove directives that disable obsolete warnings

If you disabled the version 2.x warnings with `#pragma warning disable CS0618`, remove that directive and apply the replacements in the previous sections. You can't disable the error form (`CS0619`) with `#pragma warning disable`.

## Behavior changes that don't require code edits

The following changes don't require code edits, but they change runtime behavior. Review them if your project depends on the behavior of version 2.x.

### Position synchronization uses a higher resolution

In version 3.x, `NetworkTransform.UseHalfFloatPrecision` synchronizes position at a resolution of approximately 1 mm, regardless of how far a GameObject has traveled. In version 2.x, the resolution might degrade to approximately 3 cm. The change doesn't increase bandwidth, but a project that uses `NetworkTransform.UseUnreliableDeltas` sends full-precision position updates more often. For more information, refer to [NetworkTransform](components/helper/networktransform.md).

### Interpolation smoothing is frame rate independent

In version 2.x, the `Lerp` and `SmoothDampening` interpolation types smooth by different amounts at different frame rates, because Netcode for GameObjects applies smoothing per frame instead of over time. Version 3.x applies smoothing over time, so results at 60 frames per second (fps) stay the same as in version 2.x, and results at other frame rates differ.

## Netcode for Entities integration

> [!NOTE]
> This integration is experimental, so it's not ready for production use. The features and documentation for this integration might change before it's verified for release.

Version 3.x introduces the foundations of the unified GameObject layer for Netcode for Entities, known as the hybrid prefab concept. Version 3.x doesn't enable this integration by default for standard GameObject workflows. Standard `NetworkBehaviour`, `NetworkVariable`, and RPC workflows work the same as in version 2.x.

## Additional resources

- [Install Netcode for GameObjects](install.md)
- [Netcode for Entities](https://docs.unity3d.com/Packages/com.unity.netcode@latest)
- [NetworkObject](components/core/networkobject.md)
- [NetworkTransform](components/helper/networktransform.md)
- [RPCs](advanced-topics/message-system/rpc.md)
