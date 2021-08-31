# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Additional documentation and release notes are available at [Multiplayer Documentation](https://docs-multiplayer.unity3d.com).

## [Unreleased]

### Added

- Adopted Unity C# Coding Standards in the codebase with `.editorconfig` ruleset (#666 formatting, #670 naming rules)

### Changed

- something

### Deprecated

- something

### Removed

- something

### Fixed

- something

### Security

- something

### TODO

- [af1ce68d] (2021-08-31) Jesse Olmer / chore: support standalone mode for netcode runtimetests (#1115)
- [75609cd1] (2021-08-31) Benoit Doyon / feat: Change MetricNames for a more complex value type (#1109)
- [7fbc65cb] (2021-08-31) Josie Messa / feat: Track scene event metrics (#1089)
- [ff185d6a] (2021-08-31) Jeffrey Rainy / style: whitespace fixes (#1117)
- [c1ee3b62] (2021-08-30) Noel Stephens / feat: replace scene registration with scenes in build list (#1080)
- [b5f761cf] (2021-08-27) Jeffrey Rainy / fix: mtt-857 GitHub issue 915 (#1099)
- [ced41388] (2021-08-27) Noel Stephens / fix: NetworkSceneManager exception when DontDestroyOnLoad NetworkObjects are being synchronized (#1090)
- [f3851d6a] (2021-08-27) M. Fatih MAR / feat: NetworkTransform Custom Editor Inspector UI (#1101)
- [f8f53f3e] (2021-08-27) M. Fatih MAR / refactor: remove TempGlobalObjectIdHashOverride (#1105)
- [bef00ff6] (2021-08-27) JS Fauteux / fix: MTT-1124 Counters are now reported in sync with other metrics (#1096)
- [00164832] (2021-08-27) M. Fatih MAR / refactor: convert using var statements to using var declarations (#1100)
- [4dfc7601] (2021-08-27) becksebenius-unity / chore: updated all of the namespaces to match the tools package change (#1095)
- [15d5bef0] (2021-08-26) Matt Walsh / refactor!: remove network variable settings, network behaviour cleanup (#1097)
- [3796565a] (2021-08-26) Jeffrey Rainy / fix: mtt-1088 review. Safer handling of out-of-order or old messages (#1091)
- [90e4bbe9] (2021-08-26) M. Fatih MAR / refactor: assign auto-incremented `GlobalObjectIdHash` as a fallback in `MultiInstanceHelpers.MakeNetworkObjectTestPrefab()` + fix flaky tests exposed by this fix (#1094)
- [f733bec4] (2021-08-25) becksebenius-unity / feat: fulfilling interface for tools to find network objects from an id (#1086)
- [2017e0fd] (2021-08-25) Matt Walsh / chore!: remove netvar predefined types (#1093)
- [a7ffde6a] (2021-08-25) M. Fatih MAR / fix: change OwnerClientId before firing OnGainedOwnership() callback (#1092)
- [611678a2] (2021-08-25) Matt Walsh / feat!: network variables - client auth, permission cleanup, containers (#1074)
- [fbfcc94e] (2021-08-25) M. Fatih MAR / chore: expose `--verbosity` through `standards.py` (#1085)
- [4c166a64] (2021-08-24) M. Fatih MAR / test: NetworkTransformStateTests no longer uses ReplNetworkState (#1084)
- [904552fc] (2021-08-24) Noel Stephens / fix: networkmanager prefab validation and no scene management manual test (#1073)
- [152178b1] (2021-08-24) Jeffrey Rainy / feat: snapshot. MTT-1088 Snapshot acknowledgment gaps (#1083)
- [fd44d53a] (2021-08-24) Benoit Doyon / feat: Add a test to validate registration of metric types (#1072)
- [93c8db00] (2021-08-24) Jesse Olmer / chore!: Remove unsupported UNET Relay behavior (MTT-1000) (#1081)
- [94a4bf68] (2021-08-23) becksebenius-unity / fix: 2+ inheritance from network behaviour causes compilation exception (#1078) (#1079)
- [d6d4bd0f] (2021-08-23) M. Fatih MAR / test: add networkscenemanager additive scene loading tests (#1076)
- [89a92fbd] (2021-08-23) Noel Stephens / feat: additive scene loading and networkscenemanager refactoring (#955)
- [cc7a7d5c] (2021-08-19) Sam Bellomo / test: adding more details to multiprocess readme (#1050)
- [d4d15fad] (2021-08-19) M. Fatih MAR / refactor!: convert NetworkTransform.NetworkState to `struct` (#1061)
- [0d956059] (2021-08-19) Luke Stampfli / fix: networkmanager destroy on app quit (#1011)
- [46b55520] (2021-08-19) Jeffrey Rainy / feat: snapshot, using unreliable packets, now that the underlying foundation supports it. Only merge after https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/pull/1062 (#1064)
- [428d6e43] (2021-08-19) Jeffrey Rainy / feat: snapshot. Fully integrated despawn, mtt-1092, mtt-1056 (#1062)
- [e5575e02] (2021-08-19) M. Fatih MAR / fix: eliminate bad use-after-free(destroy) pattern (#1068)
- [0db6789a] (2021-08-18) M. Fatih MAR / chore: cleanup meta files for empty dirs (#1067)
- [8e9900b5] (2021-08-18) M. Fatih MAR / chore: minor MLAPI to Netcode rename (#1065)
- [43d4494c] (2021-08-18) becksebenius-unity / feat: report network behaviour name to the profiler (#1058)
- [19e6d3ca] (2021-08-18) Noel Stephens / fix: player movement (#1063)
- [d30f6170] (2021-08-18) Luke Stampfli / test: Add unit tests for NetworkTime properties (#1053)
- [e89f05db] (2021-08-17) M. Fatih MAR / chore: remove authority & netvar perms from NetworkTransform (#1059)
- [85f84fbc] (2021-08-17) M. Fatih MAR / feat: networktransform pos/rot/sca thresholds on state sync (#1055)
- [24cebfb2] (2021-08-17) becksebenius-unity / feat: expose network behaviour type name internally (#1057)
- [fffc11f2] (2021-08-16) becksebenius-unity / chore: remove all the old profiling code (#1048)
- [8a503a84] (2021-08-16) M. Fatih MAR / fix: if-guard `NetworkManager.__rpc_name_table` access (#1056)
- [5deae108] (2021-08-12) Jaedyn Draper / fix: Disabling fixedupdate portion of SpawnRpcDespawn test because it's failing for known reasons that will be fixed in the IMessage refactor. (#1049)
- [6b58eeb0] (2021-08-11) becksebenius-unity / feat: Implement metrics for the new network profiler (#960)
- [8d4b2349] (2021-08-12) M. Fatih MAR / chore!: change package name & asmdefs (#1026)
- [87f9ec96] (2021-08-11) M. Fatih MAR / feat: per axis networktransform state sync (+bitwise state comp) (#1042)
- [18de5721] (2021-08-10) Jeffrey Rainy / refactor: Snapshot prep. createObjects and destroyObjects dead code (#1040)
- [589882c0] (2021-08-10) Jeffrey Rainy / refactor: calling networkShow(NetworkObject) code in networkshow(List<NetworkObject>) (#1028)
- [01ad0c21] (2021-08-10) Jeffrey Rainy / feat: snapshot. MTT-685 MTT-822 (#1021)
- [dce2e54d] (2021-08-10) Jeffrey Rainy / test: adding a multi-instance test checking NetworkShow and NetworkHide on lists of objects (#1036)
- [40a6aec0] (2021-08-09) Jaedyn Draper / fix: corrected NetworkVariable WriteField/WriteDelta/ReadField/ReadDelta dropping the last byte if unaligned. (#1008)
- [d89e2f2f] (2021-08-05) M. Fatih MAR / chore: run standards check over solution files (#1027)
- [8919c1ed] (2021-08-05) M. Fatih MAR / chore: replace MLAPI with Netcode in Markdown files (#1025)
- [1da76b29] (2021-08-05) Matt Walsh / fix!: added plainly-callable Add() method to NetworkSet [MTT-1005] (#1022)
- [d71dd1b8] (2021-08-04) Jeffrey Rainy / fix: fixing incorrect merge done as part of commit 85842eeacb99f6ab53ceb78295ec91da5ead7c73 (#1023)
- [f881d338] (2021-08-05) M. Fatih MAR / chore: cleanup/upgrade serialized scenes (#1020)
- [95886c59] (2021-08-05) M. Fatih MAR / chore: replace MLAPI with Netcode in C# source files (#1019)
- [5b9f953b] (2021-08-04) Matt Walsh / test: add network collections, struct and class tests MTT-936 (#1000)
- [c7250a6c] (2021-08-04) M. Fatih MAR / test: add buildtests to test build pipeline on target platforms (#1018)
- [63be9085] (2021-08-04) M. Fatih MAR / chore: rename MLAPI types to Netcode (#1017)
- [838c6e5b] (2021-08-04) M. Fatih MAR / chore!: rename asmdefs, change top-level namespaces (#1015)
- [0ea502b0] (2021-08-03) Philipp Deschain / Replacing community NetworkManagerHUD with a simpler implementation (#993)
- [f5c51f25] (2021-08-03) Noel Stephens / test: network prefab pools and INetworkPrefabInstanceHandler (#1004)
- [f8a7ef01] (2021-08-03) M. Fatih MAR / fix: do not expose Runtime internals to TestProject.ManualTests asmdef (#1014)
- [6b78f322] (2021-08-03) Jeffrey Rainy / refactor: snapshot. merge preparation. Removing old acks, removing unused varia… (#1013)
- [9c73270c] (2021-08-03) M. Fatih MAR / chore!: per-asmdef namespaces instead of per-folder (#1009)
- [85842eea] (2021-08-03) Jeffrey Rainy / feat: snapshot. ground work, preparing depedencies. No impact on code behaviour (#1012)
- [bb2f0ef6] (2021-08-03) M. Fatih MAR / fix: rename Finalize to Cleanup on InternalCommandContext due to CS0465 compiler warning (#1010)
- [c8eb5fca] (2021-08-03) M. Fatih MAR / chore!: replace MLAPI namespace with Unity.Multiplayer.Netcode (#1007)
- [bba72a4b] (2021-08-02) Noel Stephens / refactor!: remove spawn payload technical debt (#1005)
- [cbe74c2e] (2021-07-30) Luke Stampfli / fix: Client throw exception when destroying spawned object (invalid operation) (#981)
- [1311327d] (2021-07-30) M. Fatih MAR / chore: standards check to run over all projects (#999)
- [d6b2a486] (2021-07-29) Noel Stephens / refactor!: NetworkPrefabHandler and INetworkPrefabInstanceHandler API & XmlDoc changes (#977)
- [4b900723] (2021-07-29) Benoit Doyon / fix: Fix named message stream copy to (#987)
- [c3f747bc] (2021-07-28) M. Fatih MAR / fix: NetworkWriter.WriteObjectPacked to throw exception with full type name (#992)
- [c25821d2] (2021-07-27) Jaedyn Draper / fix: Fixes for a few things discovered from the message ordering refactor: (#985)
- [4fad5915] (2021-07-27) Phil Deschain / fix: Network animator server authority fixes (#972)
- [82e1c33e] (2021-07-26) Luke Stampfli / fix: Remove assert which is currently broken (#984)
- [b9ffc1f1] (2021-07-23) Jaedyn Draper / feat: Message Ordering (#948)
- [34b161ae] (2021-07-22) M. Fatih MAR / feat: pack NetworkTransform into one state variable, prevent sync popping (MTT-818) (#964)
- [fb1555f3] (2021-07-21) Luke Stampfli / feat!: Network Time (RFC #14) (#845)
- [9ae52deb] (2021-07-21) Jeffrey Rainy / test: small fix to ManualNetworkVariableTest.cs, exposed during anoth… (#968)
- [fa2be6f1] (2021-07-19) Jeffrey Rainy / feat: snapshot. Adding RTT computation API. (#963)
- [bdd7d714] (2021-07-14) Jeffrey Rainy / feat: snapshot. Milestone 1b. Testproject "manual test" "scene transitioning" working with snapshot. Disabled by default. (#862)
- [13e2b7f1] (2021-07-13) Sam Bellomo / test: multiprocess tests part 6: fixing issues runnings all tests together (#957)
- [bf296660] (2021-07-13) Sam Bellomo / docs: Perf tests part 5. Adding documentation and instructions (#952)
- [6c8efd66] (2021-07-12) Sam Bellomo / test: Perf tests part 4. Adding example of performance test with spawning x network objects at once (#925)
- [089c2065] (2021-07-12) Luke Stampfli / test: Correctly teardown OnNetworkSpawn/Despawn tests.
- [725a77a9] (2021-07-12) Sam Bellomo / test: Perf tests part 3. Adding ExecuteStepInContext for better test readability (#924)
- [833f1faf] (2021-07-09) Sam Bellomo / test: Perf tests part 2. Adding Test Coordinator and base test class (#923)
- [b2209c2c] (2021-07-08) Jeffrey Rainy / fix: (MLAPI.Serialization) 'specified cast is not valid.' on NetworkW… (#951)
- [d08b84ac] (2021-07-08) Sam Bellomo / test: Perf tests part 1. Basis for multiprocess tests process orchestration.  (#922)
- [be0ca068] (2021-07-06) Phil Deschain / feat: network animator Trigger parameter support (#872)
- [2621a196] (2021-07-01) M. Fatih MAR / feat: log warning if detected child NetworkObjects under a NetworkPrefab (#938)
- [7ed627c6] (2021-06-30) Sam Bellomo / fix: reducing log level for noisy log and adding details for developer log (#926)
- [4679474b] (2021-06-30) Sam Bellomo / feat: users can set authority on network transform programmatically (#868)
- [e122376f] (2021-06-29) Sam Bellomo / refactor: move NetworkBehaviour update to a separate non-static class (#917)
- [0855557e] (2021-06-29) Sam Bellomo / test: add utils for multi instance tests (#914)
- [9a47c661] (2021-06-29) Sam Bellomo / test: downgrading testproject to 2020.3.12f1 (#927)
- [fae7e8ce] (2021-06-29) Noel Stephens / refactor: decouple PendingSoftSyncObjects from NetworkSpawnManager (#913)
- [27112250] (2021-06-29) Josie Messa / chore: Change function signature of OnDespawnObject to accept NetworkObject (#928)
- [f14de778] (2021-06-28) James / fix: Empty prefab removal (#919)
- [1a7c145e] (2021-06-18) M. Fatih MAR / feat: NetworkObject Parenting (#855)
- [32da2f9f] (2021-06-17) M. Fatih MAR / refactor: move RpcMethodId serialization from ILPP to Core (#910)
- [7e19026a] (2021-06-15) Noel Stephens / fix: NetworkPrefabs container's elements invalidated in the NetworkManager after relaunching Unity Project (#905)
- [459c041c] (2021-06-14) Luke Stampfli / feat!: OnNetworkSpawn / OnNetworkDespawn (#865)
- [376ed36b] (2021-06-10) Benoit Doyon / feat: Add missing XMLdoc comment (#897)
- [bfea822e] (2021-06-11) M. Fatih MAR / refactor: upgrade ILPP backend, drop 2019.4 support, rename types/fields (#895)
- [f70a75fa] (2021-06-10) M. Fatih MAR / fix: do not access/render runtime info if not playing in the editor (#898)
- [28e1d85c] (2021-06-09) Benoit Doyon / feat: Add name property for network variables (#891)
- [6d9d3172] (2021-06-09) M. Fatih MAR / chore: delete PhilTestResults.xml (#894)
- [6f5e7cad] (2021-06-09) M. Fatih MAR / feat: MultiInstanceHelpers to use fixed FrameRate by default (#893)
- [a0fa2a48] (2021-06-09) Albin Corén / test: General MultiInstanceHelper improvements (#885)
- [fa2109e7] (2021-06-08) Noel Stephens / refactor: isKinematic set to true for rigid bodies of non-authorized instances (#886)
- [b4a3f663] (2021-06-08) Sam Bellomo / docs: adding more info to help debug on network transform error message (#892)
- [3e96cf95] (2021-06-08) Jean-Sébastien Fauteux / feat: Add RPC Name Lookup Table Provided by NetworkBehaviourILPP (#875)
- [92cd9cc6] (2021-06-04) Noel Stephens / fix: remove OnClientConnectedCallback registration from StatsDisplay (#882)
- [59994781] (2021-06-04) Benoit Doyon / feat: Add profiling decorator pattern (#878)
- [ba93f729] (2021-06-03) Jeffrey Rainy / refactor: Removing dead code for NETWORK_VARIABLE_UPDATE (#880)
- [3495445e] (2021-06-03) Jesse Olmer / fix: update package version to 0.2.0 because of unity minversion change (#881)
- [0444c039] (2021-06-02) Jesse Olmer / fix: Update package patch version to allow package registry re-publish
- [fa15fc6f] (2021-06-01) Jesse Olmer / docs: Fix typo in changelog version title
- [bd223cfb] (2021-06-01) Lori Krell / docs: Hotfix Changelog for 0.1.1 and manual update (#873)
- [39b56366] (2021-06-01) Jesse Olmer / fix: Update package patch version to allow package registry re-publish
- [b07b12e3] (2021-05-27) Albin Corén / fix: WebGL compilation (#724)
- [a5c3aaed] (2021-05-27) Albin Corén / test: Added RPC test (#822)
- [22c8db80] (2021-05-24) Matt Walsh / refactor!: tick param removal (#853)
- [4b15869f] (2021-05-21) Sam Bellomo / fix: Adding exception for silent failure for clients getting other player's object #844Merge pull request #844 from Unity-Technologies/feature/adding-exception-for-client-side-player-object-get
- [0ebc1e6d] (2021-05-21) M. Fatih MAR / Merge branch 'develop' into feature/adding-exception-for-client-side-player-object-get
- [75de2e0b] (2021-05-21) Noel Stephens / test: verify that empty or null arrays of custom INetworkSerializable types can be sent and received (#851)
- [63436440] (2021-05-21) Samuel Bellomo / Merge branch 'develop' into feature/adding-exception-for-client-side-player-object-get
- [7561c341] (2021-05-21) Samuel Bellomo / adding null check and spacing fix
- [d796d787] (2021-05-21) Jeffrey Rainy / fix: snapshot system. Properly gating the SnapshotSystem from being used until is is ready and specifically enabled (#852)
- [e2b17b10] (2021-05-21) Samuel Bellomo / some cleanup
- [086a55a7] (2021-05-21) Noel Stephens / test:  verifies that a user can use INetworkSerializable with RPCs (#850)
- [3566ea04] (2021-05-20) Samuel Bellomo / fixing a few issues when connecting and disconnecting additional clients Adding separate tests in SpawnManagerTests. Added Teardown
- [b3c155b5] (2021-05-20) Samuel Bellomo / Merge branch 'develop' into feature/adding-exception-for-client-side-player-object-get
- [d783a4e0] (2021-05-20) Samuel Bellomo / adding more tests
- [e7d8c0a4] (2021-05-20) Noel Stephens / test: manual connection approval to fully automated connection approval testing (#839)
- [77013b45] (2021-05-20) Noel Stephens / test: manual rpc tests to automated fixed revision (#841)
- [34b1a91b] (2021-05-19) Noel Stephens / fix: CleanDiffedSceneObjects was not clearing PendingSoftSyncObjects  (#834)
- [e2fd839c] (2021-05-19) Samuel Bellomo / Adding tests for that exception Adding the possibility to have multiple clients in MultiInstanceHelpers Updating exception check to make sure to use local networkmanager (so it works with tests)
- [d11e22be] (2021-05-19) Sam Bellomo / feat: NetworkTransform now uses NetworkVariables instead of RPCs (#826)
- [c09fa57f] (2021-05-19) M. Fatih MAR / fix: gracefully handle exceptions in an RPC invoke (#846)
- [ad8ae404] (2021-05-18) Samuel Bellomo / Adding proper exception for invalid case. This is so users don't have silent failures calling this client side expecting to see other player objects. This solves issue https://github.com/Unity-Technologies/com.unity.multiplayer.mlapi/issues/581
- [e409b373] (2021-05-18) Andrew Spiering / fix: Fixing utp license file for legal (#843)
- [cf8d29c0] (2021-05-18) M. Fatih MAR / ci: enable standards check on UTP too (#837)
- [2774a2c1] (2021-05-18) M. Fatih MAR / refactor: NetworkBehaviour.NetworkObject no longer throws an exception (#838)
- [04479232] (2021-05-18) M. Fatih MAR / fix: revert #830 (#840)
- [31b41aed] (2021-05-17) Noel Stephens / test: converting the manual rpc tests over to an automated unit test (#830)
- [edcd5c7f] (2021-05-14) M. Fatih MAR / feat: grabbable ball script to utilize transform parenting (#827)
- [09dec32e] (2021-05-11) Noel Stephens / refactor: Minor updates for unique editor unit tests with updated comments (#801)
- [3afda532] (2021-05-11) Albin Corén / test: Added MultiInstanceHelper for multi-instance runtime tests (#817)
- [1808ea01] (2021-05-10) Noel Stephens / test: connection approval and invalid NetworkPrefabs within NetworkConfig.NetworkPrefabs (#825)
- [ba6fe039] (2021-05-10) Noel Stephens / fix: Handling empty network prefab entries during NetworkManager.Init (#818)
- [11b2d250] (2021-05-10) Luke Stampfli / refactor: reduce dictionary lookups (#584)
- [aa193a48] (2021-05-10) M. Fatih MAR / chore: update namespaces in testproject test scripts (#820)
- [1bea8671] (2021-05-10) M. Fatih MAR / fix: make manualtests asmdef a normal runtime asmdef (#821)
- [f1728f65] (2021-05-10) Jeffrey Rainy / perf: cache the behaviour id in NetworkBehaviour (#819)
- [f9ad5d4f] (2021-05-09) Albin Corén / feat: Added SIPTransport for multi instance testing (#806)
- [ddfc4989] (2021-05-09) M. Fatih MAR / feat: add GrabbableBall to SampleScene in testproject (#816)
- [2ade79b8] (2021-05-09) Noel Stephens / test: scene transitioning within testproject (#815)
- [df423caa] (2021-05-08) Noel Stephens / refactor: unit test scene transitioning (#814)
- [51940a66] (2021-05-08) Noel Stephens / refactor: remove game objects after test finishes (#813)
- [df687449] (2021-05-08) Noel Stephens / fix: Set singleton if needed and style update (#812)
- [3304a8d3] (2021-05-07) Jeffrey Rainy / feat: snapshot system milestone 1 (#805)
- [56b5244a] (2021-05-07) M. Fatih MAR / test: scaffold runtimetests, editortests and manualtests asmdefs in testproject (#810)
- [4916a930] (2021-05-07) Albin Corén / fix: Test transports are no longer shown to users in transport dropdown (#802)
- [f431d519] (2021-05-07) M. Fatih MAR / fix: run testproject tests on PR trigger (#809)
- [0a7bcb2a] (2021-05-05) M. Fatih MAR / fix: standards script to fail on any errors in a check run (#807)
- [a8f16aa7] (2021-05-05) M. Fatih MAR / fix: NetworkBehaviourILPP to iterate over all types in an AsmDef (#803)
- [2a77e533] (2021-05-05) Albin Corén / refactor: extract GlobalObjectIdHash generation from NetworkObject.OnValidate() (#798)
- [f1c86121] (2021-05-04) Albin Corén / fix: Fix editor NetworkManager field when NetworkManager is null (#797)
- [e864e8eb] (2021-05-03) Phil Deschain / feat: OnAllClientsReady (#755)
- [961b1f81] (2021-04-30) Noel Stephens / test: E1.P0.5 General Acceptance Tests (#781)
- [eb991d64] (2021-04-30) Albin Corén / fix: NetworkVar implementations now uses Time of their owner NetworkM… (#788)
- [bdb87742] (2021-04-30) Albin Corén / fix: NetworkSceneManager uses the correct ServerId to send responses (#787)
- [b49445a4] (2021-04-30) Albin Corén / fix: NetworkNavMeshAgent now uses owner NetworkManager (#786)
- [c424046b] (2021-04-29) Albin Corén / fix: NetworkAnimator now uses owner NetworkManager (#785)
- [8843e6c9] (2021-04-29) Albin Corén / fix: NetworkObject editor now uses owner NetworkManager (#784)
- [85377d1a] (2021-04-29) Albin Corén / fix: NetworkBehaviour editor now uses owner NetworkBehaviour (#783)
- [340324dc] (2021-04-29) Albin Corén / fix: NetworkTransform no longer uses NetworkManager.Singleton (#766)
- [3e1aef34] (2021-04-29) Albin Corén / fix: NetworkManager doesn't destroy itself on multi instance (#765)
- [ea307953] (2021-04-28) Noel Stephens / refactor: scene object serialization consolidation and improved handling (#756)
- [75e09775] (2021-04-27) Albin Corén / refactor: FindObject(s)OfType now filters NetworkManagers (#763)
- [50e2a44c] (2021-04-27) M. Fatih MAR / feat: implement temporary globalobjectidhash override (#776)
- [32909ae7] (2021-04-27) M. Fatih MAR / ci: upgrade standards check machine size to medium from small (#778)
- [ae70d511] (2021-04-27) Albin Corén / refactor!: NetworkBehaviour and NetworkObject NetworkManager instance can now be overriden (#762)
- [c876ad9e] (2021-04-27) M. Fatih MAR / chore: remove #region blocks to better comply with coding standards (#777)
- [cc3ab426] (2021-04-27) M. Fatih MAR / ci: coding standards check in yamato (#720)
- [ca676c3b] (2021-04-26) Noel Stephens / test: Companion Tests for MTT-640 and PR-749 (#754)
- [abc85557] (2021-04-26) M. Fatih MAR / fix: globalobjectidhash printing in editor, remove istestrun flag (#774)
- [9e127c10] (2021-04-23) Albin Corén / refactor: BufferManager now uses internal NetworkManager (#746)
- [6998fd47] (2021-04-22) Noel Stephens / feat!: Network Prefab Overrides, Inspector View, and the default Player Prefab (#749)
- [e2f74f87] (2021-04-23) M. Fatih MAR / chore: fix namespace for ProfilerTests (#761)
- [789c4055] (2021-04-22) M. Fatih MAR / fix: cleanup using directives in NetworkManager (#758)
- [cd92e897] (2021-04-22) Albin Corén / fix: RpcQueueProcessor now uses the correct instance to invoke (#747)
- [2c8f8263] (2021-04-22) M. Fatih MAR / fix: correct the math behind ranged single/double read/write methods in reader/writer impls (#760)
- [c21427e7] (2021-04-22) M. Fatih MAR / chore: update testproject/packages-lock.json (#759)
- [4beabfea] (2021-04-20) Albin Corén / refactor: InternalMessageSender is no longer static (#739)
- [4a130606] (2021-04-19) Albin Corén / refactor!: NetworkSceneManager is no longer static (#738)
- [b7357f85] (2021-04-19) Andrew Spiering / ci: Enabling MLAPI UTP Transport package (#736)
- [5770ce2c] (2021-04-19) M. Fatih MAR / chore: minor coding standards fixes (#750)
- [ac8e4623] (2021-04-16) Albin Corén / refactor: remove version control using (#748)
- [9dc01b00] (2021-04-16) Albin Corén / refactor!: CustomMessageManager is no longer static (#737)
- [54a45938] (2021-04-16) Albin Corén / refactor: InternalMessageHandler is no longer static (#706)
- [a9a6ec29] (2021-04-16) M. Fatih MAR / fix: do not override GlobalObjectIdHash in Editor (#744)
- [9aa543c9] (2021-04-15) M. Fatih MAR / fix: minor whitespace fixes according to standards (#743)
- [d534a20f] (2021-04-15) M. Fatih MAR / test: all networkprefabs attached to the networkmanager have to be unique (#742)
- [adef6bc3] (2021-04-15) M. Fatih MAR / test: all networkobjects in the scene have to be unique (#741)
- [c0386eb3] (2021-04-15) Jesse Olmer / Merge branch 'master' into develop
- [023b64ae] (2021-04-15) M. Fatih MAR / docs: remove license button from readme (#740)
- [68482608] (2021-04-15) Cosmin / docs: Remove MIT license hyperlink in the main README.md (#732)
- [644c71a1] (2021-04-14) Jesse Olmer / Merge branch 'master' into develop
- [0f7d388b] (2021-04-14) Jesse Olmer / Merge tag '0.1.0' into develop
- [6984f6d6] (2021-04-14) will-mearns / docs: add "expected outcome" section to bug report template (#728)
- [8219636e] (2021-04-13) Noel Stephens / feat!: NetworkPrefabHandler to replace UsePrefabSync and Custom Spawn/Destroy Handlers (#727)
- [24e65315] (2021-04-09) M. Fatih MAR / chore: delete testing asmdef from the testproject (#721)
- [2dda54ad] (2021-04-08) M. Fatih MAR / refactor!: GlobalObjectIdHash to become 32-bits instead of 64-bits (#718)
- [52e91a45] (2021-04-08) Jeffrey Rainy / chore: Move NetworkVariable traffic onto a reliable fragmented sequen… (#717)
- [186c31d5] (2021-04-08) M. Fatih MAR / feat!: GlobalObjectIdHash to replace PrefabHash and PrefabHashGenerator (#698)
- [f6cdc679] (2021-04-08) Jesse Olmer / chore: codeowners for transport, scene mgmt, and docs (#712)
- [13b406f2] (2021-04-08) M. Fatih MAR / fix: private member naming standard issue (#713)
- [27ac4e0b] (2021-04-07) M. Fatih MAR / chore: delete CODEOWNERS.meta (#711)
- [a52b0bf0] (2021-04-07) Jeffrey Rainy / test: Adding a first test to check the robustness of MLAPI to transpo… (#709)
- [d9969907] (2021-04-07) kvassall-unity / chore: add CODEOWNERS (#697)
- [050c4992] (2021-04-07) M. Fatih MAR / refactor: remove FormerlySerializedAs attribute from NetworkPrefabs field (#708)
- [e51d7e71] (2021-04-06) Albin Corén / refactor: BufferManager is no longer static (#705)
- [9f5f39b4] (2021-04-06) Albin Corén / refactor!: SpawnManager is no longer static (#696)
- [2976167d] (2021-04-01) M. Fatih MAR / feat!: GlobalObjectIdHash64 to replace NetworkInstanceId (#695)
- [06cc418b] (2021-04-01) M. Fatih MAR / chore: tweak testproject visuals, upgrade engine version (#694)
- [2571b8ce] (2021-03-31) DanZolnik / feat!: add client id to spawn handler delegate (#685)
- [ffbf84e3] (2021-03-31) Jeffrey Rainy / fix: more stable profiler stats. Averages over a full second. No memory allocations. (#692)
- [77790b74] (2021-03-31) M. Fatih MAR / refactor: make NetworkScenePostProcessor internal, drop MonoBehaviour, rename files (#689)
- [53af4595] (2021-03-31) M. Fatih MAR / refactor!: xxHash to replace FNV (#691)
- [bcf4dfcf] (2021-03-31) M. Fatih MAR / refactor: make NetworkObject's NetworkInstanceId property internal (#690)
- [9ae8b111] (2021-03-31) M. Fatih MAR / feat: add hash32/hash64 for string/byte[] APIs to xxHash (#688)
- [91f472d1] (2021-03-30) M. Fatih MAR / feat: move xxhash from editor into runtime asmdef (#687)
- [a867fdc0] (2021-03-30) M. Fatih MAR / feat: add script for standards check/fix and git pre-push hook/unhook (#682)
- [a46e46f7] (2021-03-30) M. Fatih MAR / refactor: use m_ServerRpcParams, rename RPC param names (#681)
- [ca47b1d3] (2021-03-30) M. Fatih MAR / chore: refactor/consolidate gitignore files (#680)
- [61df17d5] (2021-03-30) M. Fatih MAR / fix: cross-asmdef RPC ILPP (#678)
- [467ef5dc] (2021-03-30) Albin Corén / fix: UNET transport channel UI (#679)
- [350b9a23] (2021-03-29) Albin Corén / test: add NetworkObject and NetworkBehaviour EditorTests (#607)
- [91339ecc] (2021-03-29) Albin Corén / fix: do not allow multiple player prefabs on networkmanager being checked in the editor (#676)
- [db2d1713] (2021-03-29) Albin Corén / fix: fix approval flow not being triggered for host (#675)
- [ce477d79] (2021-03-29) Albin Corén / fix: prevent multiple connection requests being processed while pending approval (#653)
- [e9c826fc] (2021-03-27) Sean Stolberg / ci: streamline PR and nightly CI jobs (#671)
- [1e650bbb] (2021-03-25) kvassall-unity / Merge pull request #646 from Unity-Technologies/feature/initial_barebones_proflier_test
- [bee8bcaa] (2021-03-25) kvassall-unity / More PR feedback
- [93437ad1] (2021-03-25) kvassall-unity / More PR feedback
- [bcd35b96] (2021-03-24) kvassall-unity / Merge branch 'develop' into feature/initial_barebones_proflier_test
- [1700973a] (2021-03-24) Noel Stephens / test: sending multiple clients RPC messages in various combinations for RPC Queue validation purposes (#635)
- [537067cc] (2021-03-23) kvassall-unity / Cleaning up the tests some more
- [749a9459] (2021-03-22) kvassall-unity / Updating to make tests more explicit on the type of things we should be testing
- [fdeb073c] (2021-03-22) kvassall-unity / Hooking the Network manager up to the ProfilerNotifier
- [d549d4c6] (2021-03-19) kvassall-unity / Updating to have a more testable interface and have reusable code that MLAPI could use directly
- [551c3536] (2021-03-18) kvassall-unity / test: Building out a test to get some surface coverage of the mlapi profiler functionality
- [5a5fc055] (2021-03-23) Matt Walsh / Merge pull request #651 from Unity-Technologies/fix/destroyobjectspam
- [5e04e7e5] (2021-03-22) NoelStephensUnity / refactor: remove count check
- [2f9a8339] (2021-03-22) NoelStephensUnity / fix: destroy object spam
- [8a5ab8b7] (2021-03-16) Noel Stephens / test: update rpc queue unit tests  (#626)
- [e318357f] (2021-03-16) Noel Stephens / fix: remove NetworkManager.Singleton dependencies (#625)
- [22523476] (2021-03-16) Sean Stolberg / ci: Add code coverage that depends on the pack job (#633)
- [8619b338] (2021-03-16) Matt Walsh / Merge pull request #623 from Unity-Technologies/tmp/release-to-develop-mergeback
- [ef830abe] (2021-03-15) Matt Walsh / Merge remote-tracking branch 'origin/release/0.1.0' into tmp/release-to-develop-mergeback
- [c81cf517] (2021-03-12) Albin Corén / refactor: Made editor serialized paths strongly typed (#606)
- [23dbca11] (2021-03-12) Albin Corén /  test: Added regression test for BitWriter / BitReader bool size inconsistency (#561)
- [992354e0] (2021-03-11) kvassall-unity / fix: prevent OnPerformanceTickData from sending data on null (#586)
- [26ffe4bd] (2021-03-09) Matt Walsh / chore: merge release0.1.0 back to mainline (#575)
- [91cb7e66] (2021-03-08) Albin Corén / test: add BitStream and Arithmetic EditorTests (#505)
- [caff1f6b] (2021-03-04) Matt Walsh / revert: Add NetworkAddress and NetworkPort properties to Transport. (#512) (#530)

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
