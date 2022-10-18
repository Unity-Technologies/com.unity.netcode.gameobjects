
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Additional documentation and release notes are available at [Multiplayer Documentation](https://docs-multiplayer.unity3d.com).

## [1.1.0] - 2022-10-21

### Added

- Added `NetworkManager.IsApproved` flag that is set to `true` a client has been approved.(#2261)
- `UnityTransport` now provides a way to set the Relay server data directly from the `RelayServerData` structure (provided by the Unity Transport package) throuh its `SetRelayServerData` method. This allows making use of the new APIs in UTP 1.3 that simplify integration of the Relay SDK. (#2235)
- IPv6 is now supported for direct connections when using `UnityTransport`. (#2232)
- Added WebSocket support when using UTP 2.0 with `UseWebSockets` property in the `UnityTransport` component of the `NetworkManager` allowing to pick WebSockets for communication. When building for WebGL, this selection happens automatically. (#2201)
- Added position, rotation, and scale to the `ParentSyncMessage` which provides users the ability to specify the final values on the server-side when `OnNetworkObjectParentChanged` is invoked just before the message is created (when the `Transform` values are applied to the message). (#2146)
- Added `NetworkObject.TryRemoveParent` method for convenience purposes opposed to having to cast null to either `GameObject` or `NetworkObject`. (#2146)

### Changed

- Updated `UnityTransport` dependency on `com.unity.transport` to 1.3.0. (#2231)
- The send queues of `UnityTransport` are now dynamically-sized. This means that there shouldn't be any need anymore to tweak the 'Max Send Queue Size' value. In fact, this field is now removed from the inspector and will not be serialized anymore. It is still possible to set it manually using the `MaxSendQueueSize` property, but it is not recommended to do so aside from some specific needs (e.g. limiting the amount of memory used by the send queues in very constrained environments). (#2212)
- As a consequence of the above change, the `UnityTransport.InitialMaxSendQueueSize` field is now deprecated. There is no default value anymore since send queues are dynamically-sized. (#2212)
- The debug simulator in `UnityTransport` is now non-deterministic. Its random number generator used to be seeded with a constant value, leading to the same pattern of packet drops, delays, and jitter in every run. (#2196)
- `NetworkVariable<>` now supports managed `INetworkSerializable` types, as well as other managed types with serialization/deserialization delegates registered to `UserNetworkVariableSerialization<T>.WriteValue` and `UserNetworkVariableSerialization<T>.ReadValue` (#2219)
- `NetworkVariable<>` and `BufferSerializer<BufferSerializerReader>` now deserialize `INetworkSerializable` types in-place, rather than constructing new ones. (#2219)

### Fixed

- Fixed `NetworkManager.ApprovalTimeout` will not timeout due to slower client synchronization times as it now uses the added `NetworkManager.IsApproved` flag to determined if the client has been approved or not.(#2261)
- Fixed issue caused when changing ownership of objects hidden to some clients (#2242)
- Fixed issue where an in-scene placed NetworkObject would not invoke NetworkBehaviour.OnNetworkSpawn if the GameObject was disabled when it was despawned. (#2239)
- Fixed issue where clients were not rebuilding the `NetworkConfig` hash value for each unique connection request. (#2226)
- Fixed the issue where player objects were not taking the `DontDestroyWithOwner` property into consideration when a client disconnected. (#2225)
- Fixed issue where `SceneEventProgress` would not complete if a client late joins while it is still in progress. (#2222)
- Fixed issue where `SceneEventProgress` would not complete if a client disconnects. (#2222)
- Fixed issues with detecting if a `SceneEventProgress` has timed out. (#2222)
- Fixed issue #1924 where `UnityTransport` would fail to restart after a first failure (even if what caused the initial failure was addressed). (#2220)
- Fixed issue where `NetworkTransform.SetStateServerRpc` and `NetworkTransform.SetStateClientRpc` were not honoring local vs world space settings when applying the position and rotation. (#2203)
- Fixed ILPP `TypeLoadException` on WebGL on MacOS Editor and potentially other platforms. (#2199)
- Implicit conversion of NetworkObjectReference to GameObject will now return null instead of throwing an exception if the referenced object could not be found (i.e., was already despawned) (#2158)
- Fixed warning resulting from a stray NetworkAnimator.meta file (#2153)
- Fixed Connection Approval Timeout not working client side. (#2164)
- Fixed issue where the `WorldPositionStays` parenting parameter was not being synchronized with clients. (#2146)
- Fixed issue where parented in-scene placed `NetworkObject`s would fail for late joining clients. (#2146)
- Fixed issue where scale was not being synchronized which caused issues with nested parenting and scale when `WorldPositionStays` was true. (#2146)
- Fixed issue with `NetworkTransform.ApplyTransformToNetworkStateWithInfo` where it was not honoring axis sync settings when `NetworkTransformState.IsTeleportingNextFrame` was true. (#2146)
- Fixed issue with `NetworkTransform.TryCommitTransformToServer` where it was not honoring the `InLocalSpace` setting. (#2146)
- Fixed ClientRpcs always reporting in the profiler view as going to all clients, even when limited to a subset of clients by `ClientRpcParams`. (#2144)
- Fixed RPC codegen failing to choose the correct extension methods for `FastBufferReader` and `FastBufferWriter` when the parameters were a generic type (i.e., List<int>) and extensions for multiple instantiations of that type have been defined (i.e., List<int> and List<string>) (#2142)
- Fixed the issue where running a server (i.e. not host) the second player would not receive updates (unless a third player joined). (#2127)
- Fixed issue where late-joining client transition synchronization could fail when more than one transition was occurring.(#2127)
- Fixed throwing an exception in `OnNetworkUpdate` causing other `OnNetworkUpdate` calls to not be executed. (#1739)
- Fixed synchronization when Time.timeScale is set to 0. This changes timing update to use unscaled deltatime. Now network updates rate are independent from the local time scale. (#2171)
- Fixed not sending all NetworkVariables to all clients when a client connects to a server. (#1987)
- Fixed IsOwner/IsOwnedByServer being wrong on the server after calling RemoveOwnership (#2211)

## [1.0.2] - 2022-09-12

### Fixed

- Fixed issue where `NetworkTransform` was not honoring the InLocalSpace property on the authority side during OnNetworkSpawn. (#2170)
- Fixed issue where `NetworkTransform` was not ending extrapolation for the previous state causing non-authoritative instances to become out of synch. (#2170)
- Fixed issue where `NetworkTransform` was not continuing to interpolate for the remainder of the associated tick period. (#2170)
- Fixed issue during `NetworkTransform.OnNetworkSpawn` for non-authoritative instances where it was initializing interpolators with the replicated network state which now only contains the transform deltas that occurred during a network tick and not the entire transform state. (#2170)

## [1.0.1] - 2022-08-23

### Changed

- Changed version to 1.0.1. (#2131)
- Updated dependency on `com.unity.transport` to 1.2.0. (#2129)
- When using `UnityTransport`, _reliable_ payloads are now allowed to exceed the configured 'Max Payload Size'. Unreliable payloads remain bounded by this setting. (#2081)
- Performance improvements for cases with large number of NetworkObjects, by not iterating over all unchanged NetworkObjects

### Fixed

- Fixed an issue where reading/writing more than 8 bits at a time with BitReader/BitWriter would write/read from the wrong place, returning and incorrect result. (#2130)
- Fixed issue with the internal `NetworkTransformState.m_Bitset` flag not getting cleared upon the next tick advancement. (#2110)
- Fixed interpolation issue with `NetworkTransform.Teleport`. (#2110)
- Fixed issue where the authoritative side was interpolating its transform. (#2110)
- Fixed Owner-written NetworkVariable infinitely write themselves (#2109)
- Fixed NetworkList issue that showed when inserting at the very end of a NetworkList (#2099)
- Fixed issue where a client owner of a `NetworkVariable` with both owner read and write permissions would not update the server side when changed. (#2097)
- Fixed issue when attempting to spawn a parent `GameObject`, with `NetworkObject` component attached, that has one or more child `GameObject`s, that are inactive in the hierarchy, with `NetworkBehaviour` components it will no longer attempt to spawn the associated `NetworkBehaviour`(s) or invoke ownership changed notifications but will log a warning message. (#2096)
- Fixed an issue where destroying a NetworkBehaviour would not deregister it from the parent NetworkObject, leading to exceptions when the parent was later destroyed. (#2091)
- Fixed issue where `NetworkObject.NetworkHide` was despawning and destroying, as opposed to only despawning, in-scene placed `NetworkObject`s. (#2086)
- Fixed `NetworkAnimator` synchronizing transitions twice due to it detecting the change in animation state once a transition is started by a trigger. (#2084)
- Fixed issue where `NetworkAnimator` would not synchronize a looping animation for late joining clients if it was at the very end of its loop. (#2076)
- Fixed issue where `NetworkAnimator` was not removing its subscription from `OnClientConnectedCallback` when despawned during the shutdown sequence. (#2074)
- Fixed IsServer and IsClient being set to false before object despawn during the shutdown sequence. (#2074)
- Fixed NetworkList Value event on the server. PreviousValue is now set correctly when a new value is set through property setter. (#2067)
- Fixed NetworkLists not populating on client. NetworkList now uses the most recent list as opposed to the list at the end of previous frame, when sending full updates to dynamically spawned NetworkObject. The difference in behaviour is required as scene management spawns those objects at a different time in the frame, relative to updates. (#2062)

## [1.0.0] - 2022-06-27

### Changed

- Changed version to 1.0.0. (#2046)

## [1.0.0-pre.10] - 2022-06-21

### Added

- Added a new `OnTransportFailure` callback to `NetworkManager`. This callback is invoked when the manager's `NetworkTransport` encounters an unrecoverable error. Transport failures also cause the `NetworkManager` to shut down. Currently, this is only used by `UnityTransport` to signal a timeout of its connection to the Unity Relay servers. (#1994)
- Added `NetworkEvent.TransportFailure`, which can be used by implementations of `NetworkTransport` to signal to `NetworkManager` that an unrecoverable error was encountered. (#1994)
- Added test to ensure a warning occurs when nesting NetworkObjects in a NetworkPrefab (#1969)
- Added `NetworkManager.RemoveNetworkPrefab(...)` to remove a prefab from the prefabs list (#1950)

### Changed

- Updated `UnityTransport` dependency on `com.unity.transport` to 1.1.0. (#2025)
- (API Breaking) `ConnectionApprovalCallback` is no longer an `event` and will not allow more than 1 handler registered at a time. Also, `ConnectionApprovalCallback` is now an `Action<>` taking a `ConnectionApprovalRequest` and a `ConnectionApprovalResponse` that the client code must fill (#1972) (#2002)

### Removed

### Fixed
- Fixed issue where dynamically spawned `NetworkObject`s could throw an exception if the scene of origin handle was zero (0) and the `NetworkObject` was already spawned. (#2017)
- Fixed issue where `NetworkObject.Observers` was not being cleared when despawned. (#2009)
- Fixed `NetworkAnimator` could not run in the server authoritative mode. (#2003)
- Fixed issue where late joining clients would get a soft synchronization error if any in-scene placed NetworkObjects were parented under another `NetworkObject`. (#1985)
- Fixed issue where `NetworkBehaviourReference` would throw a type cast exception if using `NetworkBehaviourReference.TryGet` and the component type was not found. (#1984)
- Fixed `NetworkSceneManager` was not sending scene event notifications for the currently active scene and any additively loaded scenes when loading a new scene in `LoadSceneMode.Single` mode. (#1975)
- Fixed issue where one or more clients disconnecting during a scene event would cause `LoadEventCompleted` or `UnloadEventCompleted` to wait until the `NetworkConfig.LoadSceneTimeOut` period before being triggered. (#1973)
- Fixed issues when multiple `ConnectionApprovalCallback`s were registered (#1972)
- Fixed a regression in serialization support: `FixedString`, `Vector2Int`, and `Vector3Int` types can now be used in NetworkVariables and RPCs again without requiring a `ForceNetworkSerializeByMemcpy<>` wrapper. (#1961)
- Fixed generic types that inherit from NetworkBehaviour causing crashes at compile time. (#1976)
- Fixed endless dialog boxes when adding a `NetworkBehaviour` to a `NetworkManager` or vice-versa. (#1947)
- Fixed `NetworkAnimator` issue where it was only synchronizing parameters if the layer or state changed or was transitioning between states. (#1946)
- Fixed `NetworkAnimator` issue where when it did detect a parameter had changed it would send all parameters as opposed to only the parameters that changed. (#1946)
- Fixed `NetworkAnimator` issue where it was not always disposing the `NativeArray` that is allocated when spawned. (#1946)
- Fixed `NetworkAnimator` issue where it was not taking the animation speed or state speed multiplier into consideration. (#1946)
- Fixed `NetworkAnimator` issue where it was not properly synchronizing late joining clients if they joined while `Animator` was transitioning between states. (#1946)
- Fixed `NetworkAnimator` issue where the server was not relaying changes to non-owner clients when a client was the owner. (#1946)
- Fixed issue where the `PacketLoss` metric for tools would return the packet loss over a connection lifetime instead of a single frame. (#2004)

## [1.0.0-pre.9] - 2022-05-10

### Fixed

- Fixed Hosting again after failing to host now works correctly (#1938)
- Fixed NetworkManager to cleanup connected client lists after stopping (#1945)
- Fixed NetworkHide followed by NetworkShow on the same frame works correctly (#1940)

## [1.0.0-pre.8] - 2022-04-27

### Changed

- `unmanaged` structs are no longer universally accepted as RPC parameters because some structs (i.e., structs with pointers in them, such as `NativeList<T>`) can't be supported by the default memcpy struct serializer. Structs that are intended to be serialized across the network must add `INetworkSerializeByMemcpy` to the interface list (i.e., `struct Foo : INetworkSerializeByMemcpy`). This interface is empty and just serves to mark the struct as compatible with memcpy serialization. For external structs you can't edit, you can pass them to RPCs by wrapping them in `ForceNetworkSerializeByMemcpy<T>`. (#1901)
- Changed requirement to register in-scene placed NetworkObjects with `NetworkManager` in order to respawn them.  In-scene placed NetworkObjects are now automatically tracked during runtime and no longer need to be registered as a NetworkPrefab.  (#1898)

### Removed

- Removed `SIPTransport` (#1870)
- Removed `ClientNetworkTransform` from the package samples and moved to Boss Room's Utilities package which can be found [here](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/com.unity.multiplayer.samples.coop/Utilities/Net/ClientAuthority/ClientNetworkTransform.cs) (#1912)

### Fixed
- Fixed issue where `NetworkSceneManager` did not synchronize despawned in-scene placed NetworkObjects. (#1898)
- Fixed `NetworkTransform` generating false positive rotation delta checks when rolling over between 0 and 360 degrees. (#1890)
- Fixed client throwing an exception if it has messages in the outbound queue when processing the `NetworkEvent.Disconnect` event and is using UTP. (#1884)
- Fixed issue during client synchronization if 'ValidateSceneBeforeLoading' returned false it would halt the client synchronization process resulting in a client that was approved but not synchronized or fully connected with the server. (#1883)
- Fixed an issue where UNetTransport.StartServer would return success even if the underlying transport failed to start (#854)
- Passing generic types to RPCs no longer causes a native crash (#1901)
- Fixed a compile failure when compiling against com.unity.nuget.mono-cecil >= 1.11.4 (#1920)
- Fixed an issue where calling `Shutdown` on a `NetworkManager` that was already shut down would cause an immediate shutdown the next time it was started (basically the fix makes `Shutdown` idempotent). (#1877)

## [1.0.0-pre.7] - 2022-04-06

### Added

- Added editor only check prior to entering into play mode if the currently open and active scene is in the build list and if not displays a dialog box asking the user if they would like to automatically add it prior to entering into play mode. (#1828)
- Added `UnityTransport` implementation and `com.unity.transport` package dependency (#1823)
- Added `NetworkVariableWritePermission` to `NetworkVariableBase` and implemented `Owner` client writable netvars. (#1762)
- `UnityTransport` settings can now be set programmatically. (#1845)
- `FastBufferWriter` and Reader IsInitialized property. (#1859)
- Prefabs can now be added to the network at **runtime** (i.e., from an addressable asset). If `ForceSamePrefabs` is false, this can happen after a connection has been formed. (#1882)
- When `ForceSamePrefabs` is false, a configurable delay (default 1 second, configurable via `NetworkConfig.SpawnTimeout`) has been introduced to gracefully handle race conditions where a spawn call has been received for an object whose prefab is still being loaded. (#1882)

### Changed

- Changed `NetcodeIntegrationTestHelpers` to use `UnityTransport` (#1870)
- Updated `UnityTransport` dependency on `com.unity.transport` to 1.0.0 (#1849)

### Removed

- Removed `SnapshotSystem` (#1852)
- Removed `com.unity.modules.animation`, `com.unity.modules.physics` and `com.unity.modules.physics2d` dependencies from the package (#1812)
- Removed `com.unity.collections` dependency from the package (#1849)

### Fixed

- Fixed in-scene placed NetworkObjects not being found/ignored after a client disconnects and then reconnects. (#1850)
- Fixed issue where `UnityTransport` send queues were not flushed when calling `DisconnectLocalClient` or `DisconnectRemoteClient`. (#1847)
- Fixed NetworkBehaviour dependency verification check for an existing NetworkObject not searching from root parent transform relative GameObject. (#1841)
- Fixed issue where entries were not being removed from the NetworkSpawnManager.OwnershipToObjectsTable. (#1838)
- Fixed ClientRpcs would always send to all connected clients by default as opposed to only sending to the NetworkObject's Observers list by default. (#1836)
- Fixed clarity for NetworkSceneManager client side notification when it receives a scene hash value that does not exist in its local hash table. (#1828)
- Fixed client throws a key not found exception when it times out using UNet or UTP. (#1821)
- Fixed network variable updates are no longer limited to 32,768 bytes when NetworkConfig.EnsureNetworkVariableLengthSafety is enabled. The limits are now determined by what the transport can send in a message. (#1811)
- Fixed in-scene NetworkObjects get destroyed if a client fails to connect and shuts down the NetworkManager. (#1809)
- Fixed user never being notified in the editor that a NetworkBehaviour requires a NetworkObject to function properly. (#1808)
- Fixed PlayerObjects and dynamically spawned NetworkObjects not being added to the NetworkClient's OwnedObjects (#1801)
- Fixed issue where NetworkManager would continue starting even if the NetworkTransport selected failed. (#1780)
- Fixed issue when spawning new player if an already existing player exists it does not remove IsPlayer from the previous player (#1779)
- Fixed lack of notification that NetworkManager and NetworkObject cannot be added to the same GameObject with in-editor notifications (#1777)
- Fixed parenting warning printing for false positives (#1855)

## [1.0.0-pre.6] - 2022-03-02

### Added
- NetworkAnimator now properly synchrhonizes all animation layers as well as runtime-adjusted weighting between them (#1765)
- Added first set of tests for NetworkAnimator - parameter syncing, trigger set / reset, override network animator (#1735)

### Fixed
- Fixed an issue where sometimes the first client to connect to the server could see messages from the server as coming from itself. (#1683)
- Fixed an issue where clients seemed to be able to send messages to ClientId 1, but these messages would actually still go to the server (id 0) instead of that client. (#1683)
- Improved clarity of error messaging when a client attempts to send a message to a destination other than the server, which isn't allowed. (#1683)
- Disallowed async keyword in RPCs (#1681)
- Fixed an issue where Alpha release versions of Unity (version 2022.2.0a5 and later) will not compile due to the UNet Transport no longer existing (#1678)
- Fixed messages larger than 64k being written with incorrectly truncated message size in header (#1686) (credit: @kaen)
- Fixed overloading RPC methods causing collisions and failing on IL2CPP targets. (#1694)
- Fixed spawn flow to propagate `IsSceneObject` down to children NetworkObjects, decouple implicit relationship between object spawning & `IsSceneObject` flag (#1685)
- Fixed error when serializing ConnectionApprovalMessage with scene management disabled when one or more objects is hidden via the CheckObjectVisibility delegate (#1720)
- Fixed CheckObjectVisibility delegate not being properly invoked for connecting clients when Scene Management is enabled. (#1680)
- Fixed NetworkList to properly call INetworkSerializable's NetworkSerialize() method (#1682)
- Fixed NetworkVariables containing more than 1300 bytes of data (such as large NetworkLists) no longer cause an OverflowException (the limit on data size is now whatever limit the chosen transport imposes on fragmented NetworkDelivery mechanisms) (#1725)
- Fixed ServerRpcParams and ClientRpcParams must be the last parameter of an RPC in order to function properly. Added a compile-time check to ensure this is the case and trigger an error if they're placed elsewhere (#1721)
- Fixed FastBufferReader being created with a length of 1 if provided an input of length 0 (#1724)
- Fixed The NetworkConfig's checksum hash includes the NetworkTick so that clients with a different tickrate than the server are identified and not allowed to connect (#1728)
- Fixed OwnedObjects not being properly modified when using ChangeOwnership (#1731)
- Improved performance in NetworkAnimator (#1735)
- Removed the "always sync" network animator (aka "autosend") parameters (#1746)
- Fixed in-scene placed NetworkObjects not respawning after shutting down the NetworkManager and then starting it back up again (#1769)

## [1.0.0-pre.5] - 2022-01-26

### Added

- Added `PreviousValue` in `NetworkListEvent`, when `Value` has changed (#1528)

### Changed

- NetworkManager's GameObject is no longer allowed to be nested under one or more GameObject(s).(#1484)
- NetworkManager DontDestroy property was removed and now NetworkManager always is migrated into the DontDestroyOnLoad scene. (#1484)'

### Fixed

- Fixed network tick value sometimes being duplicated or skipped. (#1614)
- Fixed The ClientNetworkTransform sample script to allow for owner changes at runtime. (#1606)
- Fixed When the LogLevel is set to developer NetworkBehaviour generates warning messages when it should not (#1631)
- Fixed NetworkTransport Initialize now can receive the associated NetworkManager instance to avoid using NetworkManager.Singleton in transport layer (#1677)
- Fixed a bug where NetworkList.Contains value was inverted (#1363)

## [1.0.0-pre.4] - 2021-01-04

### Added

- Added `com.unity.modules.physics` and `com.unity.modules.physics2d` package dependencies (#1565)

### Removed

- Removed `com.unity.modules.ai` package dependency (#1565)
- Removed `FixedQueue`, `StreamExtensions`, `TypeExtensions` (#1398)

### Fixed
- Fixed in-scene NetworkObjects that are moved into the DDOL scene not getting restored to their original active state (enabled/disabled) after a full scene transition (#1354)
- Fixed invalid IL code being generated when using `this` instead of `this ref` for the FastBufferReader/FastBufferWriter parameter of an extension method. (#1393)
- Fixed an issue where if you are running as a server (not host) the LoadEventCompleted and UnloadEventCompleted events would fire early by the NetworkSceneManager (#1379)
- Fixed a runtime error when sending an array of an INetworkSerializable type that's implemented as a struct (#1402)
- NetworkConfig will no longer throw an OverflowException in GetConfig() when ForceSamePrefabs is enabled and the number of prefabs causes the config blob size to exceed 1300 bytes. (#1385)
- Fixed NetworkVariable not calling NetworkSerialize on INetworkSerializable types (#1383)
- Fixed NullReferenceException on ImportReferences call in NetworkBehaviourILPP (#1434)
- Fixed NetworkObjects not being despawned before they are destroyed during shutdown for client, host, and server instances. (#1390)
- Fixed KeyNotFound exception when removing ownership of a newly spawned NetworkObject that is already owned by the server. (#1500)
- Fixed NetworkManager.LocalClient not being set when starting as a host. (#1511)
- Fixed a few memory leak cases when shutting down NetworkManager during Incoming Message Queue processing. (#1323)
- Fixed network tick value sometimes being duplicated or skipped. (#1614)

### Changed
- The SDK no longer limits message size to 64k. (The transport may still impose its own limits, but the SDK no longer does.) (#1384)
- Updated com.unity.collections to 1.1.0 (#1451)
- NetworkManager's GameObject is no longer allowed to be nested under one or more GameObject(s).(#1484)
- NetworkManager DontDestroy property was removed and now NetworkManager always is migrated into the DontDestroyOnLoad scene. (#1484)

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
