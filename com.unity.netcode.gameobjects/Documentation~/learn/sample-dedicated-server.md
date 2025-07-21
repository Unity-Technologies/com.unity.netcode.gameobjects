# Dedicated game server sample

## Dedicated Game Server sample

[The Dedicated Game Server sample](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/tree/main/Experimental/DedicatedGameServer) project demonstrates how the dedicated server model works and the tools that you can use to test multiplayer in the editor.

This project uses the following packages and services:

* [Dedicated Server](https://docs.unity3d.com/Packages/com.unity.dedicated-server@1.0/manual/index.html)
* [Netcode For GameObjects](https://docs-multiplayer.unity3d.com/netcode/current/about/)
* [Unity Transport](https://docs-multiplayer.unity3d.com/transport/current/about/)
* [Multiplayer Play Mode](https://docs-multiplayer.unity3d.com/mppm/current/about/)
* [Game Server Hosting](https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/welcome-to-multiplay)
* [Matchmaker](https://docs.unity.com/ugs/en-us/manual/matchmaker/manual/matchmaker-overview)


### Sample features

This section describes how the following features are configured to use the dedicated server:

- [Dedicated Game Server sample](#dedicated-game-server-sample)
  - [Sample features](#sample-features)
  - [Project settings](#project-settings)
    - [Strip generic components from the server](#strip-generic-components-from-the-server)
    - [Split scripts across the client, server, or network](#split-scripts-across-the-client-server-or-network)
    - [Synchronize animations between clients](#synchronize-animations-between-clients)
      - [Automatically synchronized animations](#automatically-synchronized-animations)
      - [Manually synchronized animations](#manually-synchronized-animations)
    - [Navigation](#navigation)
    - [Use multiplayer roles to control game logic](#use-multiplayer-roles-to-control-game-logic)
- [Integrate with Unity Gaming Services (UGS)](#integrate-with-unity-gaming-services-ugs)
- [Set default command line arguments](#set-default-command-line-arguments)

### Project settings

In this sample some GameObjects and components exist on the clients, some exist on the server, and some exist on both. This section explains how this sample strips generic GameObjects and components from the client or the server.

#### Strip generic components from the server

This project automatically strips the rendering, UI, and Audio components. To learn which settings automatically strip components, perform the following actions:

* Open the Project Settings window (menu: **Edit** > **Project Settings**).
* Select **Multiplayer Roles**.
* Select **Content Selection**.

For more information, refer to [Automatically remove components from a Multiplayer Role](https://docs.unity3d.com/Packages/com.unity.dedicated-server@1.0/manual/automatic-selection.html).

This sample strips other components and GameObjects from the server manually. To learn how to do this, refer to [Control which GameObjects and components exist on the client or the server](https://docs.unity3d.com/Packages/com.unity.dedicated-server@1.0/manual/content-selection.html).

#### Split scripts across the client, server, or network

This sample splits the logic of the Player Character and the AI Character into separate scripts so that you can use the Content Selection window to run each character separately on the client, server and network. For example, the `PlayerCharacter` script logic is split into the following scripts:
* Client Player Character. This script only exists on the clients.
* Server Player Character.This script only exists on the server.
* Networked Player Character: This script inherits from `NetworkBehaviour`.It synchronizes data and sends messages between the server and clients. This script exists on both clients and the server.

These scripts separate the logic of each player which means you can strip the components that each script uses. For example, Client Player Character is the only script that uses the Player Character’s `CharacterController` component, so you can safely strip the `CharacterController` component from the server. To learn where the scripts in this sample exist, do the following:
1. Select a Player Character GameObject.
2. Open the GameObject’s Inspector window.
3. Refer to the script component’s [Multiplayer role icon](https://docs.unity3d.com/Packages/com.unity.dedicated-server@1.0/manual/mutliplayer-roles-icons.html).

The logic for scripts that contain a small class, like the doors and switches in this sample scene, exist in a single script that inherits from ``NetworkBehaviour``.

#### Synchronize animations between clients

This sample handles synchronizes animations between clients in the following ways:

* Automatically synchronized animations. This process uses the NetworkAnimator component.
* Manually synchronized animations. Use this process to strip animators from the server.

##### Automatically synchronized animations

The Animator component and the NetworkAnimator script synchronize animations across the clients and servers. The server or the owner of the animations, for example a PlayerCharacter script that uses a `ClientNetworkAnimator`, handles and automatically synchronizes animations between clients. This is necessary for animations that drive logic on the server. For example, in this sample the door animation (door_boss_ani) is synchronized automatically, but its game state determines whether to enable or disable its colliders and navmesh obstacles.

##### Manually synchronized animations

The Animator components on the AICharacter prefab in this sample are stripped from the server to reduce the build size which reduces the bandwidth and CPU resources that the server uses. This means that the NetworkAnimator doesn’t automatically synchronize animations between clients and requires manually synchronized animations.

Each client uses data and events that are synchronized between them to drive the animation. The server handles their movement, and synchronizes their speed and transform values. Clients use that information to drive the character’s animation.

`ServerAICharacter.cs`:

```
void Update()
        {
            if (!m_NavMeshAgent.pathPending && m_NavMeshAgent.remainingDistance &lt; k_ReachDist)
            {
                GotoNextPoint();
            }
            m_NetworkedAICharacter.Speed = m_NavMeshAgent.velocity.magnitude;
        }
```

`ClientAICharacter.cs`:

```
void Update()
        {
            if (m_NetworkedAICharacter.IsSpawned)
            {
                m_Animator.SetFloat(k_AnimIdSpeed, m_NetworkedAICharacter.Speed);
            }
        }
```

#### Navigation

The AI characters in this sample use the navigation system. The AI characters exist on the server which means their navigation data and components are not required on clients and can be stripped. This includes the Navmesh and the NavMeshAgent, NavMeshModifier, and NavMeshObstacle components.

Other GameObjects specific to servers are also stripped from clients, like the player spawn locations and the patrols.

#### Use multiplayer roles to control game logic

This sample assigns a [Multiplayer Role](https://docs.unity3d.com/Packages/com.unity.dedicated-server@1.0/manual/multiplayer-roles.html) to move Clients and Servers to different scenes when the application starts.

```
/// <summary>
        /// Initializes the application's network-related behaviour according to the configuration. Servers load the main
        /// game scene and automatically start. Clients load the metagame scene and, if autoconnect is set to true, attempt
        /// to connect to a server automatically based on the IP and port passed through the configuration or the command
        /// line arguments.
        /// <summary>

        void InitializeNetworkLogic()
        {
            var commandLineArgumentsParser = new CommandLineArgumentsParser();
            ushort listeningPort = (ushort) commandLineArgumentsParser.Port;
            switch (MultiplayerRolesManager.ActiveMultiplayerRoleMask)
            {
                case MultiplayerRoleFlags.Server:
                    //lock framerate on dedicated servers
                    Application.targetFrameRate = commandLineArgumentsParser.TargetFramerate;
                    QualitySettings.vSyncCount = 0;
                    m_ConnectionManager.StartServerIP(k_DefaultServerListenAddress, listeningPort);
                    break;
                case MultiplayerRoleFlags.Client:
                {
                    SceneManager.LoadScene("MetagameScene");
                    if (AutoConnectOnStartup)
                    {
                        m_ConnectionManager.StartClient(k_DefaultClientAutoConnectServerAddress, listeningPort);
                    }
                    break;
                }
                case MultiplayerRoleFlags.ClientAndServer:
                    throw new ArgumentOutOfRangeException("MultiplayerRole", "ClientAndServer is an invalid multiplayer role in this sample. Please select the Client or Server role.");
            }
        }
```

The Multiplayer Role also defines how the game handles connection events:

```
void OnConnectionEvent(ConnectionEvent evt)
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server)
            {
                switch (evt.status)
                {
                    case ConnectStatus.GenericDisconnect:
                    case ConnectStatus.ServerEndedSession:
                    case ConnectStatus.StartServerFailed:
                        // If server ends networked session or fails to start, quit the application
                        Quit();
                        break;
                    case ConnectStatus.Success:
                        // If server successfully starts, load game scene
                        NetworkManager.Singleton.SceneManager.LoadScene("GameScene01", LoadSceneMode.Single);
                        break;
                }
            }
            else
            {
                switch (evt.status)
                {
                    case ConnectStatus.GenericDisconnect:
                    case ConnectStatus.UserRequestedDisconnect:
                    case ConnectStatus.ServerEndedSession:
                        // If client is disconnected, return to metagame scene
                        SceneManager.LoadScene("MetagameScene");
                        break;
                }
            }
        }
```

## Integrate with Unity Gaming Services (UGS)

This sample uses the following Unity Gaming Services (UGS):

* The [Game Server Hosting](https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/welcome) service hosts the dedicated server builds in the cloud.
* The [Matchmaker](https://docs.unity.com/ugs/en-us/manual/matchmaker/manual/matchmaker-overview) service allows clients to connect to those servers.

## Set default command line arguments

Some of the parameters in this sample are set in command line arguments, for example, the port that the servers listen on and the target framerate of the server which apply in a build. When you test a project in the editor, Unity uses a fallback value instead.

This allows the Game Server Hosting service to specify which port a server uses when it is allocated. This sample uses the `DedicatedServer.Arguments` static class to read those values. It uses the [CLI Arguments Defaults](https://docs.unity3d.com/Packages/com.unity.dedicated-server@1.0/manual/cli-arguments.html) feature of the [Dedicated Server package](https://docs.unity3d.com/Packages/com.unity.dedicated-server@1.0/manual/index.html) to provide default values for those arguments.
