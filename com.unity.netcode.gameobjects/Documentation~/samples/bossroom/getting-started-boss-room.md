# Getting started with Boss Room

Boss Room is a fully functional co-op multiplayer RPG made with Unity Netcode. It's an educational sample designed to showcase typical netcode [patterns](bossroom-actions.md) often featured in similar multiplayer games.

> [!NOTE]
> Boss Room is compatible with the latest Unity Long Term Support (LTS) Editor version (currently [6000 LTS](https://unity.com/releases). Make sure to include standalone support for Windows/Mac in your installation.

Boss Room has been developed and tested on the following platforms:

* Windows
* Mac
* iOS
* Android

The minimum device specifications for Boss Room are:

* iPhone 6S
* Samsung Galaxy J2 Core

Join the multiplayer community on the Unity [Discord](https://discord.gg/mNgM2XRDpb) and [Forum](https://forum.unity.com/forums/multiplayer.26/) to connect and find support. Got feedback? Tell us what you think using our new [Feedback Form](#feedback-form).

### Contents and quick links

- [Boss Room Overview](#boss-room-overview)
- [Install the Boss Room project](#getting-the-project)
  - [Install Git LFS to clone locally](#installing-git-lfs-to-clone-locally)
  - [Direct download](#direct-download)
- [Registering the project with Unity Gaming Services (UGS)](#registering-the-project-with-unity-gaming-services-ugs)
- [Opening the project for the first time](#opening-the-project-for-the-first-time)
- [Exploring the project](#exploring-the-project)
- [Testing multiplayer](#testing-multiplayer)
  - [Local multiplayer setup](#local-multiplayer-setup)
  - [Multiplayer over Internet](#multiplayer-over-internet)
- [Index of resources](#index-of-resources)
  - [Gameplay](#gameplay)
  - [Game flow](#game-flow)
  - [Connectivity](#connectivity)
  - [Services](#services)
  - [Tools and utilities](#tools-and-utilities)
- [Troubleshooting](#troubleshooting)
  - [Bugs](#bugs)
- [Licence](#licence)
- [Other samples](#other-samples)

### Boss Room Overview

Boss Room is designed to be used in its entirety to help you explore the concepts and patterns behind a multiplayer game flow; such as character abilities, casting animations to hide latency, replicated objects, RPCs, and integration with the [Relay](https://unity.com/products/relay), [Lobby](https://unity.com/products/lobby), and [Authentication](https://unity.com/products/authentication) services.

You can use the project as a reference starting point for your own Unity game or use elements individually.

This repository also has a [Utilities](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/com.unity.multiplayer.samples.coop) package containing reusable sample scripts. You can install it using the following manifest file entry:

```json
"com.unity.multiplayer.samples.coop": "https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git?path=/Packages/com.unity.multiplayer.samples.coop"
```

For more information on the art in Boss Room, see [ART_NOTES.md](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Documentation/ART_NOTES.md).

### Install the Boss Room project

To install the Boss Room project, download or clone it from the [com.unity.multiplayer.samples.coop](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop) repository.

**Note**: If you install the project with Git, you must install Git LFS first.

#### Install Git LFS to clone locally

Boss Room uses Git Large Files Support (LFS) to handle all large assets required locally. See [Git LFS installation options](https://github.com/git-lfs/git-lfs/wiki/Installation) for Windows and Mac instructions. This step is only needed if cloning locally. You can also just download the project which will already include large files.

#### Direct download

You can download the latest version of Boss Room from our [Releases](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/releases) page. Doing so doesn't require you to use Git LFS.

Alternatively, you can click the green **Code** button on the main "Code" github page, then select **Download Zip**. This downloads the branch that you are currently viewing on Github.

:::note
**Note for Windows users**: Using Windows' built-in extraction tool may generate an `Error 0x80010135: Path too long` error window which can invalidate the extraction process. A workaround for this is to shorten the zip file to a single character (for example, "`c.zip`") and move it to the shortest path on your computer (most often right at `C:\`) and retry. If that solution fails, another workaround is to extract the downloaded zip file using [7zip](https://www.7-zip.org/).
:::

### Registering the project with Unity Gaming Services (UGS)

Boss Room leverages several services from UGS to ease connectivity between players. To use these services inside your project, you need to [create an organization](https://support.unity.com/hc/en-us/articles/208592876-How-do-I-create-a-new-Organization-) inside the Unity Dashboard, which will provide you with access to the [Relay](https://docs.unity.com/relay/get-started.html) and [Lobby](https://docs.unity.com/lobby/game-lobby-sample.html) services.

### Opening the project for the first time

Once you have downloaded the project, follow the steps below to get up and running:

1. Check that you have installed the most recent [LTS editor version](https://unity.com/releases/).
    1. Include standalone support for **Windows/Mac** in your Unity Editor installation.
2. Add the project to the Unity Hub by selecting the **Add** button and pointing it to the root folder of the downloaded project.
    1. Unity imports all the project assets the first time you open the project, which takes longer than usual.
3. Select the **Play** button. You can then host a new game or join an existing one using the in-game UI.

### Exploring the project

Boss Room is an eight-player co-op RPG game experience, where players collaborate to fight imps, and then a boss. Players can select between character classes that were designed to provide educational oriented implementations of characteristics commonly found in similar small-scale netcode games. Control model is click-to-move, with skills triggered by a mouse button or hotkey.

One of the eight clients acts as the host/server. That client will use a compositional approach so that its entities have both server and client components.

* The game is server-authoritative, with latency-masking animations.
* Position updates are carried out through NetworkTransform that sync position and rotation.
* Code is organized in domain-based assemblies.

### Testing multiplayer

To see the multiplayer functionality in action, you can:

* Connect with a friend over your local area network
* Connect with a friend over the internet
  * You can use UGS which helps remove the complexities of handling port forwarding
  * Otherwise, the host will need to setup port forwarding on their router

See [Testing multiplayer games locally](../../tutorials/testing/testing_locally.md) for more information.

#### Local multiplayer setup

First, build an executable by selecting **File** > **Build Profiles** > **Build**.

After you have the build, you can launch several instances of the build executable to host or join a game.

If you run several instances locally, you must use the **Change Profile** button to set different profiles for each instance for authentication purposes.

:::note
**Note**: If you're using Mac to run multiple instances of the same app, you need to use the command line. Run `open -n BossRoom.app`.
:::

#### Multiplayer over Internet

Use the following instructions to test Boss Room with multiple players over the Internet.

First, build an executable and distribute it to all players.

:::tip
**Tip**: it's possible to connect between multiple instances of the same executable OR between executables and the Unity Editor that you used to create the executable.
:::

Next, you need to set up a relay. Running the Boss Room over the internet currently requires setting up a relay:

* Boss Room provides an integration with [Unity Relay](../../relay/relay.md). You can find our Unity Relay setup guide [here](../../relay/relay.md).
* Alternatively you can use Port Forwarding. The [https://portforward.com/](https://portforward.com/) site has guides on how to enable port forwarding on a huge number of routers.
* Boss Room uses `UDP` and needs a `9998` external port to be open.
* Make sure your host's address listens on 0.0.0.0 (127.0.0.1 is for local development only).

### Index of resources

The Boss Room sample includes resources for gameplay, game flow, connectivity, Unity services, and other tools and utilities.

#### Gameplay

Boss Room includes the following gameplay resources:

* Action anticipation - AnticipateActionClient() in [Assets/Scripts/Gameplay/Action/Action.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/Action/Action.cs)
* Object spawning for long actions (archer arrow) - LaunchProjectile() in [Assets/Scripts/Gameplay/Action/ConcreteActions/LaunchProjectileAction.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/Action/ConcreteActions/LaunchProjectileAction.cs)
* Quick actions with RPCs (ex: mage bolt) - [Assets/Scripts/Gameplay/Action/ConcreteActions/FXProjectileTargetedAction.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/Action/ConcreteActions/FXProjectileTargetedAction.cs)
* Teleport - [Assets/Scripts/Gameplay/Action/ConcreteActions/DashAttackAction.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/Action/ConcreteActions/DashAttackAction.cs)
* Client side input tracking before an action (archer AOE) - OnStartClient() in [Assets/Scripts/Gameplay/Action/ConcreteActions/AOEAction.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/Action/ConcreteActions/AOEAction.cs)
* Time based action (charged shot) - [Assets/Scripts/Gameplay/Action/ConcreteActions/ChargedLaunchProjectileAction.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/Action/ConcreteActions/ChargedLaunchProjectileAction.cs)
* Object parenting to animation - [Assets/Scripts/Gameplay/Action/ConcreteActions/PickUpAction.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/Action/ConcreteActions/PickUpAction.cs)
* Physics object throwing (using NetworkRigidbody) - [Assets/Scripts/Gameplay/Action/ConcreteActions/TossAction.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/Action/ConcreteActions/TossAction.cs)
* NetworkAnimator usage - All actions, in particular [Assets/Scripts/Gameplay/Action/ConcreteActions/ChargedShieldAction.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/Action/ConcreteActions/ChargedShieldAction.cs)
* NetworkTransform local space - [Assets/Scripts/Gameplay/GameplayObjects/ServerDisplacerOnParentChange.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameplayObjects/ServerDisplacerOnParentChange.cs)
* Dynamic imp spawning with portals - [Assets/Scripts/Gameplay/GameplayObjects/ServerWaveSpawner.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameplayObjects/ServerWaveSpawner.cs)
* In scene placed dynamic objects (imps) - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/NetworkObjectSpawner.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/com.unity.multiplayer.samples.coop/Utilities/Net/NetworkObjectSpawner.cs)
* Static objects (non-destroyables like doors, switches, etc) - [Assets/Scripts/Gameplay/GameplayObjects/SwitchedDoor.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameplayObjects/SwitchedDoor.cs)
* State tracking with breakables, switch, doors
    * [Assets/Scripts/Gameplay/GameplayObjects/Breakable.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameplayObjects/Breakable.cs)
    * [Assets/Scripts/Gameplay/GameplayObjects/FloorSwitch.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameplayObjects/FloorSwitch.cs)
    * [Assets/Scripts/Gameplay/GameplayObjects/SwitchedDoor.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameplayObjects/SwitchedDoor.cs)
* NetworkVariable with Enum - [Assets/Scripts/Gameplay/GameState/NetworkPostGame.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameState/NetworkPostGame.cs)
* NetworkVariable with custom serialization (GUID) - [Assets/Scripts/Infrastructure/NetworkGuid.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Infrastructure/NetworkGuid.cs)
* NetworkVariable with fixed string - [Assets/Scripts/Utils/NetworkNameState.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Utils/NetworkNameState.cs)
* NetworkList with custom serialization (LobbyPlayerState) - [Assets/Scripts/Gameplay/GameState/NetworkCharSelection.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameState/NetworkCharSelection.cs)
* Persistent player (over multiple scenes) - [Assets/Scripts/Gameplay/GameplayObjects/PersistentPlayer.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameplayObjects/PersistentPlayer.cs)
* Character logic (including player's avatar) - [Assets/Scripts/Gameplay/GameplayObjects/Character/ \
Assets/Scripts/Gameplay/GameplayObjects/Character/ServerCharacter.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameplayObjects/Character)
* Character movements - [Assets/Scripts/Gameplay/GameplayObjects/Character/ServerCharacterMovement.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameplayObjects/Character/ServerCharacterMovement.cs)
* Client driven movements - Boss Room is server driven with anticipation animation. See [Client Driven bitesize](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/tree/main/Basic/ClientDriven) for client driven gameplay
* Player spawn - SpawnPlayer() in [Assets/Scripts/Gameplay/GameState/ServerBossRoomState.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameState/ServerBossRoomState.cs)

#### Game flow

Boss Room includes the following game flow resources:

* Application Controller - [Assets/Scripts/ApplicationLifecycle/ApplicationController.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/ApplicationLifecycle/ApplicationController.cs)
* Game flow state machine - All child classes in [Assets/Scripts/Gameplay/GameState/GameStateBehaviour.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameState/GameStateBehaviour.cs)
* Scene loading and progress sharing - [ackages/com.unity.multiplayer.samples.coop/Utilities/SceneManagement/](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/com.unity.multiplayer.samples.coop/Utilities/SceneManagement)
* Synced UI with character select - [Assets/Scripts/Gameplay/GameState/ClientCharSelectState.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameState/ClientCharSelectState.cs)
* In-game lobby (character selection) - [Assets/Scripts/Gameplay/GameState/NetworkCharSelection.cs \
Assets/Scripts/Gameplay/GameState/ServerCharSelectState.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameState/NetworkCharSelection.cs)
* Win state - [Assets/Scripts/Gameplay/GameState/PersistentGameState.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/GameState/PersistentGameState.cs)

#### Connectivity

Boss Room includes the following connectivity resources:

* Connection approval return value with custom messaging - WaitToDenyApproval() in [Assets/Scripts/ConnectionManagement/ConnectionState/HostingState.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/ConnectionManagement/ConnectionState/HostingState.cs)
* Connection state machine - [Assets/Scripts/ConnectionManagement/ConnectionManager.cs \
Assets/Scripts/ConnectionManagement/ConnectionState/](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/ConnectionManagement/ConnectionManager.cs)
* Session manager - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/SessionManager.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/com.unity.multiplayer.samples.coop/Utilities/Net/SessionManager.cs)
* RTT stats - [Assets/Scripts/Utils/NetworkOverlay/NetworkStats.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Utils/NetworkOverlay/NetworkStats.cs)

#### Services

Boss Room supports integration with Lobby, Relay, and Authentication (UAS). Boss Room includes the following service resources:

* Lobby and relay - host creation - CreateLobbyRequest() in [Assets/Scripts/Gameplay/UI/Lobby/LobbyUIMediator.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/UI/Lobby/LobbyUIMediator.cs)
* Lobby and relay - client join - JoinLobbyRequest() in [Assets/Scripts/Gameplay/UI/Lobby/LobbyUIMediator.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/UI/Lobby/LobbyUIMediator.cs)
* Relay Join - StartClientLobby() in [Assets/Scripts/ConnectionManagement/ConnectionState/OfflineState.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/ConnectionManagement/ConnectionState/OfflineState.cs)
* Relay Create - StartHostLobby() in [Assets/Scripts/ConnectionManagement/ConnectionState/OfflineState.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/ConnectionManagement/ConnectionState/OfflineState.cs)
* Authentication - EnsurePlayerIsAuthorized() in [Assets/Scripts/UnityServices/Auth/AuthenticationServiceFacade.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/UnityServices/Auth/AuthenticationServiceFacade.cs)

#### Tools and utilities

Boss Room includes the following tools and utilities:

* Networked message channel (inter-class and networked messaging) - [Assets/Scripts/Infrastructure/PubSub/NetworkedMessageChannel.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Infrastructure/PubSub/NetworkedMessageChannel.cs)
* Simple interpolation - [Assets/Scripts/Utils/PositionLerper.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Utils/PositionLerper.cs)
* Network Object Pooling - [Assets/Scripts/Infrastructure/NetworkObjectPool.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Infrastructure/NetworkObjectPool.cs)
* NetworkGuid - [Assets/Scripts/Infrastructure/NetworkGuid.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Infrastructure/NetworkGuid.cs)
* Netcode hooks - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/NetcodeHooks.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/com.unity.multiplayer.samples.coop/Utilities/Net/NetcodeHooks.cs)
* Spawner for in-scene objects - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/NetworkObjectSpawner.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/com.unity.multiplayer.samples.coop/Utilities/Net/NetworkObjectSpawner.cs)
* Session manager for reconnection - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/SessionManager.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/com.unity.multiplayer.samples.coop/Utilities/Net/SessionManager.cs)
* Relay utils - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/UnityRelayUtilities.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/com.unity.multiplayer.samples.coop/Utilities/Net/UnityRelayUtilities.cs)
* Client authority - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/ClientAuthority/ClientNetworkTransform.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/com.unity.multiplayer.samples.coop/Utilities/Net/ClientAuthority/ClientNetworkTransform.cs)
* Scene utils with synced loading screens - [Packages/com.unity.multiplayer.samples.coop/Utilities/SceneManagement/](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/com.unity.multiplayer.samples.coop/Utilities/SceneManagement)
* RNSM custom config - [Packages/com.unity.multiplayer.samples.coop/Utilities/Net/RNSM/CustomNetStatsMonitorConfiguration.asset](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/com.unity.multiplayer.samples.coop/Utilities/Net/RNSM/CustomNetStatsMonitorConfiguration.asset)
* NetworkSimulator usage through UI - [Assets/Scripts/Utils/NetworkSimulatorUIMediator.cs ](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Utils/NetworkSimulatorUIMediator.cs)
* ParrelSync - [Packages/manifest.json](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Packages/manifest.json)

### Troubleshooting

#### Bugs

- Report bugs in Boss Room using Github [issues](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/issues)
- Report Netcode for GameObjects bugs using Netcode for GameObjects Github [issues](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues)
- Report Unity bugs using the [Unity bug submission process](https://unity3d.com/unity/qa/bug-reporting).

### Licence

Boss Room is licenced under the Unity Companion Licence. See [LICENSE.md](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/LICENSE.md) for more legal information.

Visit the following links to learn more about Unity Netcode and Boss Room.

* [About Netcode for GameObjects](../../index.md)
* [Boss Room Actions Walkthrough](bossroom-actions.md)
* [NetworkObject Parenting inside Boss Room](networkobject-parenting.md)
* [NetworkRigidbody inside Boss Room](networkrigidbody.md)
* [Dynamically spawning NetworkObject in Boss Room](spawn-networkobjects.md)

### Other samples

The [Bitesize Samples](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize) repository is currently being expanded and has a collection of smaller samples and games showcasing sub-features of Netcode for GameObjects. You can review these samples with documentation to better understand our APIs and features.