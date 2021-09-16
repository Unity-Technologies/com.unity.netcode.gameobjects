# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Additional documentation and release notes are available at [Multiplayer Documentation](https://docs-multiplayer.unity3d.com).

## [Unreleased]

### Added
- Enhanced `NetworkSceneManager` implementation with additive scene loading capabilities (#1080, #955, #913)
  - `NetworkSceneManager.OnSceneEvent` provides improved scene event notificaitons  
- Enhanced `NetworkTransform` implementation with per axis/component based and threshold based state replication (#1042, #1055, #1061, #1084, #1101)
- Implemented `NetworkPrefabHandler` that provides support for object pooling and NetworkPrefab overrides (#1073, #1004, #977, #905,#749, #727)
- Implemented auto `NetworkObject` transform parent synchronization at runtime over the network (#855)
- Adopted Unity C# Coding Standards in the codebase with `.editorconfig` ruleset (#666, #670)
- When a client tries to spawn a `NetworkObject` an exception is thrown to indicate unsupported behavior. (#981)
- Added a `NetworkTime` and `NetworkTickSystem` which allows for improved control over time and ticks. (#845)
- Added a `OnNetworkDespawn` function to `NetworkObject` which gets called when a `NetworkObject` gets despawned and can be overriden. (#865)
- Added `SnapshotSystem`. This internal system will allow variables and (de)spawn commands to be sent in blocks. Will leverage unreliable messages and offer efficient transfer with eventual consistency. Off by default for now and only implemented for spawns and despawns.
- NetworkBehaviour and NetworkObject NetworkManager instance can now be overriden (#762)
- Added metrics reporting for the new network profiler if the Multiplayer Tools package is present (#1104, #1089, #1096, #1086, #1072, #1058, #960, #897, #891, #878)

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
- Scene registration in `NetworkManager` is now replaced by Build Setttings → Scenes in Build List (#1080)
- `NetworkSceneManager.SwitchScene` has been replaced by `NetworkSceneManager.LoadScene` (#955)
  - Improved newly joined client synchronization
- `GlobalObjectIdHash` replaced `PrefabHash` and `PrefabHashGenerator` for stability and consistency (#698)
- `NetworkStart` has been renamed to `OnNetworkSpawn`. (#865)
- Network variable cleanup - eliminated shared mode, variables are server-authoritative
- FindObject(s)OfType now filters NetworkManagers (#763)
- BufferManager now uses internal NetworkManager (#746)
- InternalMessageSender is no longer static (#739)
- NetworkSceneManager is no longer static (#738)
- CustomMessageManager is no longer static (#737)
- InternalMessageHandler is no longer static (#706)
- BufferManager is no longer static (#705)
- SpawnManager is no longer static (#696)

### Deprecated

- something

### Removed

- Removed ILPP backend for 2019.4, minimum required version is 2020.3+ (#895)
- `NetworkManager.NetworkConfig` had the following properties removed: (#1080)
  - Scene Registrations no longer exists
  - Allow Runtime Scene Changes was no longer needed and was removed
- Removed the NetworkObject.Spawn payload parameter (#1005)
- Removed `ProfilerCounter`, the original MLAPI network profiler, and the built-in network profiler module (2020.3). A replacement can now be found in the Multiplayer Tools package. (#1048)
- Removed NetworkSet, NetworkDictionary

### Fixed
- Fixed parenting not being preserved during scene transitions (#1148)
- Fixed `NetworkObject.OwnerClientId` property changing before `NetworkBehaviour.OnGainedOwnership()` callback (#1092)
- Fixed `NetworkBehaviourILPP` to iterate over all types in an assembly (#803)
- Fixed cross-asmdef RPC ILPP by importing types into external assemblies (#678)
- Fixed `NetworkManager` shutdown when quitting the application or switching scenes. Now NetworkManager shutdowns correctly and despawns existing NetworkObjects (#1011)
- Fixed `specified cast is not valid.' on NetworkWriter` (#951)
- Fixed `Clients are receiving data from objects not visible to them` (#915)
- Fixed project compilation when targeting WebGL (#724)
- Fixed NetworkObject editor exception when NetworkManager field is null (#797)
- Fixed NetworkVar implementations not using the Time of their owner NetworkManager (#788)
- Fixed NetworkSceneManager not using the correct ServerId to send responses (#787)
- Fixed NetworkNavMeshAgent not using the correct NetworkManager (#786)
- Fixed NetworkAnimator not using the correct NetworkManager (#785)
- Fixed NetworkObject editor not using the correct NetworkManager (#784)
- Fixed NetworkBehaviour not using the correct owner NetworkManager (#783)
- Fixed NetworkTransform not using the correct NetworkManager (#766)
- Fixed NetworkManager doesn't destroy itself on multi instance (#765)
- Fixed RpcQueueProcessor not using the correct instance to invoke (#747)
- Fixed UNET transport channel UI (#679)
- Fixed: Only one PlayerPrefab can be selected in NetworkManager editor (#676)
- Fixed connection approval not being triggered for host (#675)
- Fixed multiple connection requests being able to get processed while pending approval (#653)

### Security

- something

### TODO


- [49997da9] (2021-08-31) Valere Plantevin / fix: Missing end profiling sample (#1118)
- [af1ce68d] (2021-08-31) Jesse Olmer / chore: support standalone mode for netcode runtimetests (#1115)
- [93c8db00] (2021-08-24) Jesse Olmer / chore!: Remove unsupported UNET Relay behavior (MTT-1000) (#1081)
- [cc7a7d5c] (2021-08-19) Sam Bellomo / test: adding more details to multiprocess readme (#1050)
- [0d956059] (2021-08-19) Luke Stampfli / fix: networkmanager destroy on app quit (#1011)
- [d30f6170] (2021-08-18) Luke Stampfli / test: Add unit tests for NetworkTime properties (#1053)
- [5deae108] (2021-08-12) Jaedyn Draper / fix: Disabling fixedupdate portion of SpawnRpcDespawn test because it's failing for known reasons that will be fixed in the IMessage refactor. (#1049)
- [40a6aec0] (2021-08-09) Jaedyn Draper / fix: corrected NetworkVariable WriteField/WriteDelta/ReadField/ReadDelta dropping the last byte if unaligned. (#1008)
- [d30f6170] (2021-08-18) Luke Stampfli / test: Add unit tests for NetworkTimeField/WriteDelta/ReadField/ReadDelta dropping the last byte if unaligned. (#1008)
- [0ea502b0] (2021-08-03) Philipp Deschain / Replacing community NetworkManagerHUD with a simpler implementation (#993)
- [cbe74c2e] (2021-07-30) Luke Stampfli / fix: Client throw exception when destroying spawned object (invalid operation) (#981)
- [c25821d2] (2021-07-27) Jaedyn Draper / fix: Fixes for a few things discovered from the message ordering refactor: (#985)
- [4fad5915] (2021-07-27) Phil Deschain / fix: Network animator server authority fixes (#972)
- [82e1c33e] (2021-07-26) Luke Stampfli / fix: Remove assert which is currently broken (#984)
- [b9ffc1f1] (2021-07-23) Jaedyn Draper / feat: Message Ordering (#948)
- [fb1555f3] (2021-07-21) Luke Stampfli / feat!: Network Time (RFC #14) (#845)
- [13e2b7f1] (2021-07-13) Sam Bellomo / test: multiprocess tests part 6: fixing issues runnings all tests together (#957)
- [bf296660] (2021-07-13) Sam Bellomo / docs: Perf tests part 5. Adding documentation and instructions (#952)
- [6c8efd66] (2021-07-12) Sam Bellomo / test: Perf tests part 4. Adding example of performance test with spawning x network objects at once (#925)
- [089c2065] (2021-07-12) Luke Stampfli / test: Correctly teardown OnNetworkSpawn/Despawn tests.
- [725a77a9] (2021-07-12) Sam Bellomo / test: Perf tests part 3. Adding ExecuteStepInContext for better test readability (#924)
- [833f1faf] (2021-07-09) Sam Bellomo / test: Perf tests part 2. Adding Test Coordinator and base test class (#923)
- [d08b84ac] (2021-07-08) Sam Bellomo / test: Perf tests part 1. Basis for multiprocess tests process orchestration.  (#922)
- [be0ca068] (2021-07-06) Phil Deschain / feat: network animator Trigger parameter support (#872)
- [7ed627c6] (2021-06-30) Sam Bellomo / fix: reducing log level for noisy log and adding details for developer log (#926)
- [4679474b] (2021-06-30) Sam Bellomo / feat: users can set authority on network transform programmatically (#868)
- [e122376f] (2021-06-29) Sam Bellomo / refactor: move NetworkBehaviour update to a separate non-static class (#917)
- [0855557e] (2021-06-29) Sam Bellomo / test: add utils for multi instance tests (#914)
- [9a47c661] (2021-06-29) Sam Bellomo / test: downgrading testproject to 2020.3.12f1 (#927)
- [f14de778] (2021-06-28) James / fix: Empty prefab removal (#919)
- [459c041c] (2021-06-14) Luke Stampfli / feat!: OnNetworkSpawn / OnNetworkDespawn (#865)
- [b4a3f663] (2021-06-08) Sam Bellomo / docs: adding more info to help debug on network transform error message (#892)
- [3e96cf95] (2021-06-08) Jean-Sébastien Fauteux / feat: Add RPC Name Lookup Table Provided by NetworkBehaviourILPP (#875)
- [3495445e] (2021-06-03) Jesse Olmer / fix: update package version to 0.2.0 because of unity minversion change (#881)
- [0444c039] (2021-06-02) Jesse Olmer / fix: Update package patch version to allow package registry re-publish
- [fa15fc6f] (2021-06-01) Jesse Olmer / docs: Fix typo in changelog version title
- [bd223cfb] (2021-06-01) Lori Krell / docs: Hotfix Changelog for 0.1.1 and manual update (#873)
- [39b56366] (2021-06-01) Jesse Olmer / fix: Update package patch version to allow package registry re-publish
- [4b15869f] (2021-05-21) Sam Bellomo / fix: Adding exception for silent failure for clients getting other player's object #844Merge pull request #844 from Unity-Technologies/feature/adding-exception-for-client-side-player-object-get
- [63436440] (2021-05-21) Samuel Bellomo / Merge branch 'develop' into feature/adding-exception-for-client-side-player-object-get
- [7561c341] (2021-05-21) Samuel Bellomo / adding null check and spacing fix
- [e2b17b10] (2021-05-21) Samuel Bellomo / some cleanup
- [3566ea04] (2021-05-20) Samuel Bellomo / fixing a few issues when connecting and disconnecting additional clients Adding separate tests in SpawnManagerTests. Added Teardown
- [b3c155b5] (2021-05-20) Samuel Bellomo / Merge branch 'develop' into feature/adding-exception-for-client-side-player-object-get
- [d783a4e0] (2021-05-20) Samuel Bellomo / adding more tests
- [e2fd839c] (2021-05-19) Samuel Bellomo / Adding tests for that exception Adding the possibility to have multiple clients in MultiInstanceHelpers Updating exception check to make sure to use local networkmanager (so it works with tests)
- [d11e22be] (2021-05-19) Sam Bellomo / feat: NetworkTransform now uses NetworkVariables instead of RPCs (#826)
- [ad8ae404] (2021-05-18) Samuel Bellomo / Adding proper exception for invalid case. This is so users don't have silent failures calling this client side expecting to see other player objects. This solves issue https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/issues/581
- [e409b373] (2021-05-18) Andrew Spiering / fix: Fixing utp license file for legal (#843)
- [11b2d250] (2021-05-10) Luke Stampfli / refactor: reduce dictionary lookups (#584)
- [e864e8eb] (2021-05-03) Phil Deschain / feat: OnAllClientsReady (#755)
- [b7357f85] (2021-04-19) Andrew Spiering / ci: Enabling MLAPI UTP Transport package (#736)
- [c0386eb3] (2021-04-15) Jesse Olmer / Merge branch 'master' into develop
- [68482608] (2021-04-15) Cosmin / docs: Remove MIT license hyperlink in the main README.md (#732)
- [644c71a1] (2021-04-14) Jesse Olmer / Merge branch 'master' into develop
- [0f7d388b] (2021-04-14) Jesse Olmer / Merge tag '0.1.0' into develop
- [6984f6d6] (2021-04-14) will-mearns / docs: add "expected outcome" section to bug report template (#728)
- [f6cdc679] (2021-04-08) Jesse Olmer / chore: codeowners for transport, scene mgmt, and docs (#712)

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
