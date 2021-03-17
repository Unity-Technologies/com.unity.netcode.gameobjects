# Changelog
This file documents all notable changes to this package. Additional documentation and release notes are available at [Multiplayer Documentation](https://docs-multiplayer.unity3d.com).

## [0.1.0] - 2021-03-23

This is the initial experimental Unity MLAPI Package, v0.1.0.

### New Features

- Refactored a new standard for Remote Procedure Call (RPC) in MLAPI which provides increased performance, significantly reduced boilerplate code, and extensibility for future-proofed code. MLAPI RPC includes `ServerRpc` and `ClientRpc` to execute logic on the server and client-side. This provides a single performant unified RPC solution, replacing MLAPI Convenience and Performance RPC (see [here](#removed-features)).
- Added standarized serialization types, including built-in and custom serialization flows. See [RFC #2](https://github.com/Unity-Technologies/com.unity.multiplayer.rfcs/blob/master/text/0002-serializable-types.md) for details.
- `INetworkSerializable` interface replaces `IBitWritable`.
- Added `NetworkSerializer`..., which is the main aggregator that implements serialization code for built-in supported types and holds `NetworkReader` and `NetworkWriter` instances internally.
- Added a Network Update Loop infrastructure that aids Netcode systems to update (such as RPC queue and transport) outside of the standard `MonoBehaviour` event cycle. See [RFC #8](https://github.com/Unity-Technologies/com.unity.multiplayer.rfcs/blob/master/text/0008-network-update-loop.md) and the following details:

  - It uses Unity's [low-level Player Loop API](https://docs.unity3d.com/ScriptReference/LowLevel.PlayerLoop.html) and allows for registering `INetworkUpdateSystem`s with `NetworkUpdate` methods to be executed at specific `NetworkUpdateStage`s, which may also be before or after `MonoBehaviour`-driven game logic execution.
  - You will typically interact with `NetworkUpdateLoop` for registration and `INetworkUpdateSystem` for implementation.
  - `NetworkVariable`s are now tick-based using the `NetworkTickSystem`, tracking time through network interactions and syncs.

- Added message batching to handle consecutive RPC requests sent to the same client. `RpcBatcher` sends batches based on requests from the `RpcQueueProcessing`, by batch size threshold or immediately.
- [GitHub 494](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/494): Added a constraint to allow one `NetworkObject` per `GameObject`, set through the `DisallowMultipleComponent` attribute.
- Integrated MLAPI with the Unity Profiler for versions 2020.2 and later:

  - Added new profiler modules for MLAPI that report important network data.
  - Attached the profiler to a remote player to view network data over the wire.

- A test project is available for building and experimenting with MLAPI features. This project is available in the MLAPI GitHub [testproject folder](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/tree/release/0.1.0/testproject). 
- Added a [MLAPI Community Contributions](https://github.com/Unity-Technologies/mlapi-community-contributions/tree/master/com.mlapi.contrib.extensions) new GitHub repository to accept extensions from the MLAPI community. Current extensions include moved MLAPI features for lag compensation (useful for Server Authoritative actions) and `TrackedObject`.

### Changes

- [GitHub 520](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/520): MLAPI now uses the Unity Package Manager for installation management.
- Added functionality and usability to `NetworkVariable`, previously called `NetworkVar`. Updates enhance options and fully replace the need for `SyncedVar`s. 
- [GitHub 507](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/507): Reimplemented `NetworkAnimator`, which synchronizes animation states for networked objects. 
- GitHub [444](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/444) and [455](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/455): Channels are now represented as bytes instead of strings.

For users of previous versions of MLAPI, this release renames APIs due to refactoring. All obsolete marked APIs have been removed as per [GitHub 513](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/513) and [GitHub 514](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/514).

| Previous MLAPI Versions | V 0.1.0 Name |
| -- | -- |
| `NetworkingManager` | `NetworkManager` |
| `NetworkedObject` | `NetworkObject` |
| `NetworkedBehaviour` | `NetworkBehaviour` |
| `NetworkedClient` | `NetworkClient` |
| `NetworkedPrefab` | `NetworkPrefab` |
| `NetworkedVar` | `NetworkVariable` |
| `NetworkedTransform` | `NetworkTransform` |
| `NetworkedAnimator` | `NetworkAnimator` |
| `NetworkedAnimatorEditor` | `NetworkAnimatorEditor` |
| `NetworkedNavMeshAgent` | `NetworkNavMeshAgent` |
| `SpawnManager` | `NetworkSpawnManager` |
| `BitStream` | `NetworkBuffer` |
| `BitReader` | `NetworkReader` |
| `BitWriter` | `NetworkWriter` |
| `NetEventType` | `NetworkEventType` |
| `ChannelType` | `NetworkDelivery` |
| `Channel` | `NetworkChannel` |
| `Transport` | `NetworkTransport` |
| `NetworkedDictionary` | `NetworkDictionary` |
| `NetworkedList` | `NetworkList` |
| `NetworkedSet` | `NetworkSet` |
| `MLAPIConstants` | `NetworkConstants` |
| `UnetTransport` | `UNetTransport` |

### Fixes

- [GitHub 460](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/460): Fixed an issue for RPC where the host-server was not receiving RPCs from the host-client and vice versa without the loopback flag set in `NetworkingManager`. 
- Fixed an issue where data in the Profiler was incorrectly aggregated and drawn, which caused the profiler data to increment indefinitely instead of resetting each frame.
- Fixed an issue the client soft-synced causing PlayMode client-only scene transition issues, caused when running the client in the editor and the host as a release build. Users may have encountered a soft sync of `NetworkedInstanceId` issues in the `SpawnManager.ClientCollectSoftSyncSceneObjectSweep` method.
- [GitHub 458](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/458): Fixed serialization issues in `NetworkList` and `NetworkDictionary` when running in Server mode.
- [GitHub 498](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/498): Fixed numerical precision issues to prevent not a number (NaN) quaternions.
- [GitHub 438](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/438): Fixed booleans by reaching or writing bytes instead of bits.
- [GitHub 519](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/519): Fixed an issue where calling `Shutdown()` before making `NetworkManager.Singleton = null` is null on `NetworkManager.OnDestroy()`.

### Removed features

With a new release of MLAPI in Unity, some features have been removed:

* SyncVars have been removed from MLAPI. Use `NetworkVariable`s in place of this functionality. <!-- MTT54 -->
* [GitHub 527](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/527): Lag compensation systems and `TrackedObject` have moved to the new [MLAPI Community Contributions](https://github.com/Unity-Technologies/mlapi-community-contributions/tree/master/com.mlapi.contrib.extensions) repo.
* [GitHub 509](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/509): Encryption has been removed from MLAPI. The `Encryption` option in `NetworkConfig` on the `NetworkingManager` is not available in this release. This change will not block game creation or running. A current replacement for this functionality is not available, and may be developed in future releases. See the following changes:

    * Removed `SecuritySendFlags` from all APIs.
    * Removed encryption, cryptography, and certificate configurations from APIs including `NetworkManager` and `NetworkConfig`.
    * Removed "hail handshake", including `NetworkManager` implementation and `NetworkConstants` entries.
    * Modified `RpcQueue` and `RpcBatcher` internals to remove encryption and authentication from reading and writing.

* Removed the previous MLAPI Profiler editor window from Unity versions 2020.2 and later.
* Removed previous MLAPI Convenience and Performance RPC APIs with the new standard RPC API. See [RFC #1](https://github.com/Unity-Technologies/com.unity.multiplayer.rfcs/blob/master/text/0001-std-rpc-api.md) for details.
* [GitHub 520](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/520): Removed the MLAPI Installer.

## Known issues

* `NetworkNavMeshAgent` does not synchronize mesh data, Agent Size, Steering, Obstacle Avoidance, or Path Finding settings. It only synchronizes the destination and velocity, not the path to the destination.
* For `RPC`, methods with a `ClientRpc` or `ServerRpc` suffix which are not marked with [ServerRpc] or [ClientRpc] will cause a compiler error.
* For `NetworkAnimator`, Animator Overrides are not supported. Triggers do not work.
* For `NetworkVariable`, the `NetworkDictionary` `List` and `Set` must use the `reliableSequenced` channel.
* `NetworkObjects`s are supported but when spawning a prefab with nested child network objects you have to manually call spawn on them
* `NetworkTransform` have the following issues:
  * Replicated objects may have jitter. 
  * The owner is always authoritative about the object's position.
  * Scale is not synchronized.
* Connection Approval is not called on the host client.
* For `NamedMessages`, always use `NetworkBuffer` as the underlying stream for sending named and unnamed messages.
* For `NetworkManager`, connection management is limited. Use `IsServer`, `IsClient`, `IsConnectedClient`, or other code to check if MLAPI connected correctly.

## [0.0.1-preview.1] - 2020-12-20
This was an internally-only-used version of the Unity MLAPI Package
