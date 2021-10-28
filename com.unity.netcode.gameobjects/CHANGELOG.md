# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Additional documentation and release notes are available at [Multiplayer Documentation](https://docs-multiplayer.unity3d.com).

## [1.0.0-pre.3] - 2021-10-22

### Added

- ResetTrigger function to NetworkAnimator (#1327)

### Fixed 

- Overflow exception when syncing Animator state. (#1327)
- Added `try`/`catch` around RPC calls, preventing exception from causing further RPC calls to fail (#1329)
- Fixed an issue where ServerClientId and LocalClientId could have the same value, causing potential confusion, and also fixed an issue with the UNet where the server could be identified with two different values, one of which might be the same as LocalClientId, and the other of which would not.(#1368)
- IL2CPP would not properly compile (#1359)

## [1.0.0-pre.2] - 2021-10-19


### Added

- Associated Known Issues for the 1.0.0-pre.1 release in the changelog

### Changed

- Updated label for `1.0.0-pre.1` changelog section

## [1.0.0-pre.1] - 2021-10-19

### Added

- Added `ClientNetworkTransform` sample to the SDK package (#1168)
- Added `Bootstrap` sample to the SDK package (#1140)
- Enhanced `NetworkSceneManager` implementation with additive scene loading capabilities (#1080, #955, #913)
  - `NetworkSceneManager.OnSceneEvent` provides improved scene event notificaitons  
- Enhanced `NetworkTransform` implementation with per axis/component based and threshold based state replication (#1042, #1055, #1061, #1084, #1101)
- Added a jitter-resistent `BufferedLinearInterpolator<T>` for `NetworkTransform` (#1060)
- Implemented `NetworkPrefabHandler` that provides support for object pooling and `NetworkPrefab` overrides (#1073, #1004, #977, #905,#749, #727)
- Implemented auto `NetworkObject` transform parent synchronization at runtime over the network (#855)
- Adopted Unity C# Coding Standards in the codebase with `.editorconfig` ruleset (#666, #670)
- When a client tries to spawn a `NetworkObject` an exception is thrown to indicate unsupported behavior. (#981)
- Added a `NetworkTime` and `NetworkTickSystem` which allows for improved control over time and ticks. (#845)
- Added a `OnNetworkDespawn` function to `NetworkObject` which gets called when a `NetworkObject` gets despawned and can be overriden. (#865)
- Added `SnapshotSystem` that would allow variables and spawn/despawn messages to be sent in blocks (#805, #852, #862, #963, #1012, #1013, #1021, #1040, #1062, #1064, #1083, #1091, #1111, #1129, #1166, #1192)
  - Disabled by default for now, except spawn/despawn messages
  - Will leverage unreliable messages with eventual consistency
- `NetworkBehaviour` and `NetworkObject`'s `NetworkManager` instances can now be overriden (#762)
- Added metrics reporting for the new network profiler if the Multiplayer Tools package is present (#1104, #1089, #1096, #1086, #1072, #1058, #960, #897, #891, #878)
- `NetworkBehaviour.IsSpawned` a quick (and stable) way to determine if the associated NetworkObject is spawned (#1190)
- Added `NetworkRigidbody` and `NetworkRigidbody2D` components to support networking `Rigidbody` and `Rigidbody2D` components (#1202, #1175)
- Added `NetworkObjectReference` and `NetworkBehaviourReference` structs which allow to sending `NetworkObject/Behaviours` over RPCs/`NetworkVariable`s (#1173)
- Added `NetworkAnimator` component to support networking `Animator` component (#1281, #872)

### Changed

- Bumped minimum Unity version, renamed package as "Unity Netcode for GameObjects", replaced `MLAPI` namespace and its variants with `Unity.Netcode` namespace and per asm-def variants (#1007, #1009, #1015, #1017, #1019, #1025, #1026, #1065)
  - Minimum Unity version:
    - 2019.4 → 2020.3+
  - Package rename:
    - Display name: `MLAPI Networking Library` → `Netcode for GameObjects`
    - Name: `com.unity.multiplayer.mlapi` → `com.unity.netcode.gameobjects`
    - Updated package description
  - All `MLAPI.x` namespaces are replaced with `Unity.Netcode`
    - `MLAPI.Messaging` → `Unity.Netcode`
    - `MLAPI.Connection` → `Unity.Netcode`
    - `MLAPI.Logging` → `Unity.Netcode`
    - `MLAPI.SceneManagement` → `Unity.Netcode`
    - and other `MLAPI.x` variants to `Unity.Netcode`
  - All assembly definitions are renamed with `Unity.Netcode.x` variants
    - `Unity.Multiplayer.MLAPI.Runtime` → `Unity.Netcode.Runtime`
    - `Unity.Multiplayer.MLAPI.Editor` → `Unity.Netcode.Editor`
    - and other `Unity.Multiplayer.MLAPI.x` variants to `Unity.Netcode.x` variants
- Renamed `Prototyping` namespace and assembly definition to `Components` (#1145)
- Changed `NetworkObject.Despawn(bool destroy)` API to default to `destroy = true` for better usability (#1217)
- Scene registration in `NetworkManager` is now replaced by Build Setttings → Scenes in Build List (#1080)
- `NetworkSceneManager.SwitchScene` has been replaced by `NetworkSceneManager.LoadScene` (#955)
- `NetworkManager, NetworkConfig, and NetworkSceneManager` scene registration replaced with scenes in build list (#1080)
- `GlobalObjectIdHash` replaced `PrefabHash` and `PrefabHashGenerator` for stability and consistency (#698)
- `NetworkStart` has been renamed to `OnNetworkSpawn`. (#865)
- Network variable cleanup - eliminated shared mode, variables are server-authoritative (#1059, #1074)
- `NetworkManager` and other systems are no longer singletons/statics (#696, #705, #706, #737, #738, #739, #746, #747, #763, #765, #766, #783, #784, #785, #786, #787, #788)
- Changed `INetworkSerializable.NetworkSerialize` method signature to use `BufferSerializer<T>` instead of `NetworkSerializer` (#1187)
- Changed `CustomMessagingManager`'s methods to use `FastBufferWriter` and `FastBufferReader` instead of `Stream` (#1187)
- Reduced internal runtime allocations by removing LINQ calls and replacing managed lists/arrays with native collections (#1196)

### Removed

- Removed `NetworkNavMeshAgent` (#1150)
- Removed `NetworkDictionary`, `NetworkSet` (#1149)
- Removed `NetworkVariableSettings` (#1097)
- Removed predefined `NetworkVariable<T>` types (#1093)
    - Removed `NetworkVariableBool`, `NetworkVariableByte`, `NetworkVariableSByte`, `NetworkVariableUShort`, `NetworkVariableShort`, `NetworkVariableUInt`, `NetworkVariableInt`, `NetworkVariableULong`, `NetworkVariableLong`, `NetworkVariableFloat`, `NetworkVariableDouble`, `NetworkVariableVector2`, `NetworkVariableVector3`, `NetworkVariableVector4`, `NetworkVariableColor`, `NetworkVariableColor32`, `NetworkVariableRay`, `NetworkVariableQuaternion`
- Removed `NetworkChannel` and `MultiplexTransportAdapter` (#1133)
- Removed ILPP backend for 2019.4, minimum required version is 2020.3+ (#895)
- `NetworkManager.NetworkConfig` had the following properties removed: (#1080)
  - Scene Registrations no longer exists
  - Allow Runtime Scene Changes was no longer needed and was removed
- Removed the NetworkObject.Spawn payload parameter (#1005)
- Removed `ProfilerCounter`, the original MLAPI network profiler, and the built-in network profiler module (2020.3). A replacement can now be found in the Multiplayer Tools package. (#1048)
- Removed UNet RelayTransport and related relay functionality in UNetTransport (#1081)
- Removed `UpdateStage` parameter from `ServerRpcSendParams` and `ClientRpcSendParams` (#1187)
- Removed `NetworkBuffer`, `NetworkWriter`, `NetworkReader`, `NetworkSerializer`, `PooledNetworkBuffer`, `PooledNetworkWriter`, and `PooledNetworkReader` (#1187)
- Removed `EnableNetworkVariable` in `NetworkConfig`, it is always enabled now (#1179)
- Removed `NetworkTransform`'s FixedSendsPerSecond, AssumeSyncedSends, InterpolateServer, ExtrapolatePosition, MaxSendsToExtrapolate, Channel, EnableNonProvokedResendChecks, DistanceSendrate (#1060) (#826) (#1042, #1055, #1061, #1084, #1101)
- Removed `NetworkManager`'s `StopServer()`, `StopClient()` and `StopHost()` methods and replaced with single `NetworkManager.Shutdown()` method for all (#1108)

### Fixed

- Fixed ServerRpc ownership check to `Debug.LogError` instead of `Debug.LogWarning` (#1126)
- Fixed `NetworkObject.OwnerClientId` property changing before `NetworkBehaviour.OnGainedOwnership()` callback (#1092)
- Fixed `NetworkBehaviourILPP` to iterate over all types in an assembly (#803)
- Fixed cross-asmdef RPC ILPP by importing types into external assemblies (#678)
- Fixed `NetworkManager` shutdown when quitting the application or switching scenes (#1011)
  - Now `NetworkManager` shutdowns correctly and despawns existing `NetworkObject`s
- Fixed Only one `PlayerPrefab` can be selected on `NetworkManager` inspector UI in the editor (#676)
- Fixed connection approval not being triggered for host (#675)
- Fixed various situations where messages could be processed in an invalid order, resulting in errors (#948, #1187, #1218)
- Fixed `NetworkVariable`s being default-initialized on the client instead of being initialized with the desired value (#1266)
- Improved runtime performance and reduced GC pressure (#1187)
- Fixed #915 - clients are receiving data from objects not visible to them (#1099)
- Fixed `NetworkTransform`'s "late join" issues, `NetworkTransform` now uses `NetworkVariable`s instead of RPCs (#826)
- Throw an exception for silent failure when a client tries to get another player's `PlayerObject`, it is now only allowed on the server-side (#844)

### Known Issues

- `NetworkVariable` does not serialize `INetworkSerializable` types through their `NetworkSerialize` implementation
- `NetworkObjects` marked as `DontDestroyOnLoad` are disabled during some network scene transitions
- `NetworkTransform` interpolates from the origin when switching Local Space synchronization
- Exceptions thrown in `OnNetworkSpawn` user code for an object will prevent the callback in other objects
- Cannot send an array of `INetworkSerializable` in RPCs
- ILPP generation fails with special characters in project path

## [0.2.0] - 2021-06-03

WIP version increment to pass package validation checks. Changelog & final version number TBD.

## [0.1.1] - 2021-06-01

This is hotfix v0.1.1 for the initial experimental Unity MLAPI Package.

### Changed

- Fixed issue with the Unity Registry package version missing some fixes from the v0.1.0 release.

## [0.1.0] - 2021-03-23

This is the initial experimental Unity MLAPI Package, v0.1.0.

### Added

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

### Changed

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

### Fixed

- [GitHub 460](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/460): Fixed an issue for RPC where the host-server was not receiving RPCs from the host-client and vice versa without the loopback flag set in `NetworkingManager`. 
- Fixed an issue where data in the Profiler was incorrectly aggregated and drawn, which caused the profiler data to increment indefinitely instead of resetting each frame.
- Fixed an issue the client soft-synced causing PlayMode client-only scene transition issues, caused when running the client in the editor and the host as a release build. Users may have encountered a soft sync of `NetworkedInstanceId` issues in the `SpawnManager.ClientCollectSoftSyncSceneObjectSweep` method.
- [GitHub 458](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/458): Fixed serialization issues in `NetworkList` and `NetworkDictionary` when running in Server mode.
- [GitHub 498](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/498): Fixed numerical precision issues to prevent not a number (NaN) quaternions.
- [GitHub 438](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/438): Fixed booleans by reaching or writing bytes instead of bits.
- [GitHub 519](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/519): Fixed an issue where calling `Shutdown()` before making `NetworkManager.Singleton = null` is null on `NetworkManager.OnDestroy()`.

### Removed

With a new release of MLAPI in Unity, some features have been removed:

- SyncVars have been removed from MLAPI. Use `NetworkVariable`s in place of this functionality. <!-- MTT54 -->
- [GitHub 527](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/527): Lag compensation systems and `TrackedObject` have moved to the new [MLAPI Community Contributions](https://github.com/Unity-Technologies/mlapi-community-contributions/tree/master/com.mlapi.contrib.extensions) repo.
- [GitHub 509](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/509): Encryption has been removed from MLAPI. The `Encryption` option in `NetworkConfig` on the `NetworkingManager` is not available in this release. This change will not block game creation or running. A current replacement for this functionality is not available, and may be developed in future releases. See the following changes:
    - Removed `SecuritySendFlags` from all APIs.
    - Removed encryption, cryptography, and certificate configurations from APIs including `NetworkManager` and `NetworkConfig`.
    - Removed "hail handshake", including `NetworkManager` implementation and `NetworkConstants` entries.
    - Modified `RpcQueue` and `RpcBatcher` internals to remove encryption and authentication from reading and writing.
- Removed the previous MLAPI Profiler editor window from Unity versions 2020.2 and later.
- Removed previous MLAPI Convenience and Performance RPC APIs with the new standard RPC API. See [RFC #1](https://github.com/Unity-Technologies/com.unity.multiplayer.rfcs/blob/master/text/0001-std-rpc-api.md) for details.
- [GitHub 520](https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/520): Removed the MLAPI Installer.

### Known Issues

- `NetworkNavMeshAgent` does not synchronize mesh data, Agent Size, Steering, Obstacle Avoidance, or Path Finding settings. It only synchronizes the destination and velocity, not the path to the destination.
- For `RPC`, methods with a `ClientRpc` or `ServerRpc` suffix which are not marked with [ServerRpc] or [ClientRpc] will cause a compiler error.
- For `NetworkAnimator`, Animator Overrides are not supported. Triggers do not work.
- For `NetworkVariable`, the `NetworkDictionary` `List` and `Set` must use the `reliableSequenced` channel.
- `NetworkObjects`s are supported but when spawning a prefab with nested child network objects you have to manually call spawn on them
- `NetworkTransform` have the following issues:
  - Replicated objects may have jitter. 
  - The owner is always authoritative about the object's position.
  - Scale is not synchronized.
- Connection Approval is not called on the host client.
- For `NamedMessages`, always use `NetworkBuffer` as the underlying stream for sending named and unnamed messages.
- For `NetworkManager`, connection management is limited. Use `IsServer`, `IsClient`, `IsConnectedClient`, or other code to check if MLAPI connected correctly.

## [0.0.1-preview.1] - 2020-12-20

This was an internally-only-used version of the Unity MLAPI Package
