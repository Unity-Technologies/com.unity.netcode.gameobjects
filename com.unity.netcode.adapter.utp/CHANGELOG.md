# Changelog

All notable changes to this package will be documented in this file. The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)

## [Unreleased]

### Added

### Changed

- Updated Burst package to 1.6.4. (#1615)

### Fixed

- Fixed issue where the server `NetworkEndPoint` would fail to be created when 'Server Listen Address' is empty. (#1636)
- Fixed issue with native collections not all being disposed of when destroying the component without shutting it down properly. This would result in errors in the console and memory leaks. (#1640)

## [1.0.0-pre.5] - 2022-01-26

### Added

- A new 'Server Listen Address' field under 'Connection Data' in the inspector has been added to specify the address a server should listen to, in case it differs from the main 'Address' field. The `SetConnectionData` method has been updated accordingly to take an optional parameter to specify that listen address. (#1605)
- Added new methods to set the relay server data: `SetHostRelayData` and `SetClientRelayData`. These are meant to be less error-prone than `SetRelayServerData` (which remains available). (#1609)

### Changed

- Updated Netcode for GameObjects dependency to 1.0.0-pre.5 (#1626)
- Updated Unity Transport package to 1.0.0-pre.12. (#1615)
- Rename the 'Send Queue Batch Size' property to 'Max Payload Size' to better reflect its usage. (#1584)
- Implicit conversions between `ConnectionAddressData` and `NetworkEndPoint` are now deprecated, since their semantics are no longer clear with the introduction of the new `ServerListenAddress` field (see above). (#1605)

### Fixed

- Lifted the limit of ~44KB for reliable payloads. Before the fix, attempting to send a payload larger than that with reliable delivery would silently fail. Note that it is still not recommended to send such large reliable payloads, since their delivery could take a few network round-trips. (#1596)
- Fixed a bug where NetworkList.contains value was inverted (#1363)

## [1.0.0-pre.4] - 2022-01-04

### Added

- Added new 'Max Send Queue Size' configuration field in the inspector. This controls the size of the send queue that is used to accumulate small sends together and also acts as an overflow queue when there are too many in-flight packets or when other internal queues are full. (#1491)

### Changed

- Updated Netcode for GameObjects dependency to 1.0.0-pre.4 (#1562)
- Removed 'Maximum Packet Size' configuration field in the inspector. This would cause confusion since the maximum packet size is in effect always the MTU (1400 bytes on most platforms). (#1403)
- Updated com.unity.transport to 1.0.0-pre.10 (#1501)
- All delivery methods now support fragmentation, meaning the 'Send Queue Batch Size' setting (which controls the maximum payload size) now applies to all delivery methods, not just reliable ones. (#1512)

### Fixed

- Fixed packet overflow errors when sending payloads too close to the MTU (was mostly visible when using Relay). (#1403)
- Don't throw an exception when the host disconnects (issue 1439 on GitHub). (#1441)
- Avoid "too many inflight packets" errors by queueing packets in a queue when the limit of inflight packets is reached in UTP. The size of this queue can be controlled with the 'Max Send Queue Size' configuration field. (#1491)

## [1.0.0-pre.3] - 2021-10-22

### Added 

- Exposed `m_HeartbeatTimeoutMS`, `m_ConnectTimeoutMS`, `m_MaxConnectAttempts`, and `m_DisconnectTimeoutMS` parameters. (#1314)

### Changed

- Updated Unity Transport package to 1.0.0-pre.7
- Updated Netcode for GameObjects dependency to 1.0.0-pre.3

### Fixed

- Fixed sends failing when send queue is filled or close to be filled. (#1317)
- Heartbeats API not working for Unity Transport when running in the editor or development builds. (#1314)

## [1.0.0-pre.2] - 2021-10-19

### Changed

- Updated Netcode for GameObjects dependency to 1.0.0-pre.2

## [1.0.0-pre.1] - 2021-10-19

### Added

- Support for Unity Relay (#887)
- New SetConnectionData function that takes in a NetworkEndpoint

### Changed 

- No longer use coroutines when connecting to relay
- Consolidated the Send/Recv queue properties as they always needed to be the same.
- Consolidated the Fragmentation/Queue size as they always needed to be the same.
- Updated Unity Transport package to 1.0.0-pre.6

### Fixed

- Fixed an issue where OnClientDisconnectCallback was not being called (#1243)
- Flush the UnityTransport send queue on shutdown (#1234)
- Exposed a way to set ip and port from code (#1208)
- Possible Editor crash when trying to read a batched packet where the size of the packet was larger than the max packet size.
- Removed the requirement that MaxPacketSize needs to be the same size as the batched/fragmentation buffer size.

## [0.0.1-preview.1] - 2020-12-20
This is the first release of Unity Transport for Netcode for Gameobjects
