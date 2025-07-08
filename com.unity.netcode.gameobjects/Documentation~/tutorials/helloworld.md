# Your Netcode "Hello World" Project

A "Hello World" program is a computer program that outputs or displays the message "Hello, World!". Normally it's  the first program written by people learning to code. it's also used as a sanity test to make sure that a computer language is correctly installed and that the operator understands how to use it.

This "Hello World" tutorial walks you through creating a project, installing Netcode for GameObjects (Netcode), and creating the basic components for your first networked game. it's also the base for the [Golden Path series](goldenpath_series/gp_intro.md).

## Create a new project in Unity

1. Open the **Unity Hub**.
1. Click **New**.
1. Select type '3D'
1. Name the project *Hello World*.
1. Select the location you want to save the project.

## Install Netcode

See the [install Netcode guide](../install.md) to install the Netcode package.

## Create the Basic Components

In this section we will create the basic building blocks of a multiplayer game.

### Creating Network Manager and selecting the Transport

In this section we add a Network Manager and add Unity Transport to our project. The [NetworkManager](../components/networkmanager.md) is the component that has all your project's netcode-related settings. Unity Transport is the transport layer that Netcode uses for communication between the server and the clients. See [here](../advanced-topics/transports.md) for more.

1. Right-click in the **Hierarchy** tab of the main Unity Window.
1. Select **Create Empty**.
1. Rename the `GameObject` **NetworkManager**.
2. Select **NetworkManager**.
3. Click **Add Component** in the Inspector Tab.
4. Select **Netcode** from the list shown.
5. Select `NetworkManager` Component from the list displayed.
6. Inside the `NetworkManager` component tab, locate the  `NetworkTransport` field.
7. Click "Select Transport".
8. Select `UnityTransport`.
9. Save your scene.

## Creating an object to spawn for each connected player

This section adds in a player object and spawns it for each connected player.

1. Right-click in the **Hierarchy** tab of the Unity Window to create a **3D Object > Capsule**
1. Rename it **Player**.
1. While **Player** is selected, add a **Netcode** > `NetworkObject` component in the Inspector Tab.
1. Click the **Assets** folder under the **Project** tab.
2. Right-click inside the **Assets** folder to **Create** > **Folder** and call it **Prefabs**.
3. Make **Player** a Prefab by dragging it to **Prefabs** folder you just created.
4. Delete **Player** from scene.

> [!NOTE]
> We remove the **Player** object from the scene because we assign this network Prefab to the `Player Prefab` property in the `NetworkManager` component. The library does not support defining a player object as an in-scene placed `NetworkObject`.


5. Select `NetworkManager`.
6. Inside the `NetworkManager` component tab, locate the  `Player Prefab` field.
7. Drag this player Prefab from above into this field.

> [!NOTE]
> When you drop the Prefab into the `Player Prefab` slot, you are telling the library that when a client connects to the game, automatically spawn this Prefab as the character for the connecting client. If you don't have any Prefab set as the `Player Prefab` no player object will be spawned.

1. Create a **3D Object->Plane**, centered at (0,0,0).
1. Save your scene

### Adding your scene to the build

> [!NOTE]
> When 'Enable Scene Management' is enabled for the NetworkManager (allowing the server to control which scenes should be loaded for the clients), we must ensure that the current scene has been added to the build, otherwise, we will be unable to enter play mode. This option is enabled by default.

1. Click **File** > **Build Settings**, in the upper-left corner of the Unity window
2. Click **Add Open Scenes**
3. Close the `Build Settings` window.

## Creating a command line helper

This command line helper will allow us to launch builds with a command line argument that will start a networking session, either as a server, host, or client. This can make testing builds easier.

1. Right-click the **Assets** folder and create a new folder by hovering over **Create** and selecting **Folder**. Name it **Scripts**.
2. Create a script called `NetworkCommandLine` by right-clicking on your **Scripts** folder, hovering over **Create** and selecting **C# Script**.
3. In the **Hierarchy** menu, right-click on the `NetworkManager` and choose **Create Empty**.

   This will create an  empty `GameObject` with `NetworkManager` as its parent.

4. Rename this child `GameObject` `NetworkCommandLine`.
5. With the new `NetworkCommandLine` object selected, click **Add Component** from the **Inspector** tab.
6. Select **Scripts** from the drop-down and click on the `NetworkCommandLine.cs` script you created earlier.
7. Open the `NetworkCommandLine.cs` script by double-clicking from the **Project** tab > **Assets** > **Scripts**. It will open in your text editor
8. Edit the `NetworkCommandLine.cs` script to match the following:

<details open>
<summary>Click to show/hide the Code.

</summary>

``` csharp
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkCommandLine : MonoBehaviour
{
   private NetworkManager netManager;

   void Start()
   {
       netManager = GetComponentInParent<NetworkManager>();

       if (Application.isEditor) return;

       var args = GetCommandlineArgs();

       if (args.TryGetValue("-mode", out string mode))
       {
           switch (mode)
           {
               case "server":
                   netManager.StartServer();
                   break;
               case "host":
                   netManager.StartHost();
                   break;
               case "client":

                   netManager.StartClient();
                   break;
           }
       }
   }

   private Dictionary<string, string> GetCommandlineArgs()
   {
       Dictionary<string, string> argDictionary = new Dictionary<string, string>();

       var args = System.Environment.GetCommandLineArgs();

       for (int i = 0; i < args.Length; ++i)
       {
           var arg = args[i].ToLower();
           if (arg.StartsWith("-"))
           {
               var value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
               value = (value?.StartsWith("-") ?? false) ? null : value;

               argDictionary.Add(arg, value);
           }
       }
       return argDictionary;
   }
}
```

</details>

9. Paste the copied code into your code editor.
1. Save your changes. Your script will reload in the Unity Editor.
1. Back in the Editor, open **Edit** -> **Project Settings**
1. Select the **Player tab**.
1. Expand the **Resolution and Presentation**.
1. From **Resolution** > **Fullscreen Mode**, change `Fullscreen Window` to `Windowed`.
1. Back to the Editor main window, save your scene.


> [!NOTE]
> If you are using a Pro Unity license, you may want to disable the splash screen by unchecking **Splash Image** > **Splash Screen** > **Show Splash Screen**.

### Testing the command line helper

Now we will test that the command line helper script works.

1. Select **File** > **Build and Run**.
1. Create a new folder called `Build` inside your Hello World project folder.
1. **Save As** the binary `HelloWorld`.
1. Your project will build and launch in a new window, and you should see the plane.
1. Quit your app.
1. Now to launch from the command line.

#### Windows

For Windows you should do the following:


1. Open your Command Prompt.
1. Enter the following. Be sure to change the noted section `< >` of **both** commands to your project.

> [!NOTE]
> You may get a UAC prompt requesting permission for the binary to run you should allow it.

  Server:
  ```
  <Path to Project>\Build\HelloWorld.exe -mode server
  ```

  Client:
  ```
  <path to project>\Build\HelloWorld.exe -mode client
  ```

  To run these commands on a single line:
  ```
  HelloWorld\Build\HelloWorld.exe -mode server & HelloWorld\Build\HelloWorld.exe -mode client
  ```

  Example:
  ```
  C:\Users\sarao>HelloWorld\Build\HelloWorld.exe -mode server & HelloWorld\Build\HelloWorld.exe -mode client
  ```

On Windows, no standard out stream exists by default, so you will need to view the Debug.log file to see the outputs. You can find the Debug.log  files in:

`C:\Users\username\AppData\LocalLow\CompanyName\ProductName\Player.log`

Where the `CompanyName` should default to `DefaultCompany` for a new project and  `ProductName` should be equal to the project's name.

Alternatively you can modify the Windows commands to create a log.txt file in the same folder as your **HelloWorld** folder.

Modify the commands as follows:

  Server:
  ```
  <Path to Project>\Build\HelloWorld.exe -logfile log-server.txt -mode server
  ```

  Client:
  ```
  <Path to Project>\Build\HelloWorld.exe  -logfile log-client.txt -mode client
  ```

  Example (Running as a single command line):
  ```
  C:\Users\sarao>HelloWorld\Build\HelloWorld.exe -logfile -log-server.txt -mode server & HelloWorld\Build\HelloWorld.exe -logfile log-client.txt -mode client
  ```

#### MacOS

For Mac you should do the following:

1. Open Terminal.
2. Enter the following. Be sure to change the noted section `< >` of **both** commands to your project.

Server
```
<Path to Project>/Build/HelloWorld.app/Contents/MacOS/<Project Name> -mode server -logfile -
```

Client
```
<Path to Project>/Build/HelloWorld.app/Contents/MacOS/<Project Name> -mode client -logfile -
```

Run both as a single command:
```
<Path to Project>/Build/HelloWorld.app/Contents/MacOS/<Project Name> -mode server -logfile - & ; ~ <Path to Project>/Build/HelloWorld.app/Contents/MacOS/<Project Name> -mode client -logfile -
```

## Testing Hello World

### Route 1 - Editor
Now, to see if everything works as expected we can test starting a host in the editor. A host plays the role of a server and a client at the same time.

1. Click **Play**.
1. Click the **Start Host** button in the **NetworkManager** GameObject's inspector.
1. You should now see your scene with your Player Prefab spawned.

### Route 2 - Command Line
You can also use the command line helper to launch a server and one or more clients to connect to the server. You should see just the plane on the server until the first client connects. Then, a new Player Prefab will be spawned by Netcode for each connected client; however, they will be overlapped in the game view.

## Next Steps

See the following content to continue your journey using Netcode:

* Build on the Hello World project to continue learning about different features of Netcode with the [Golden Path series](goldenpath_series/gp_intro.md).
* Check out the educational samples to further explore Netcode and its abilities:
  * [Boss Room](../learn/bossroom/getting-started-boss-room.md)
  * [2D Spaceshooter Bitesize Sample](../samples/bitesize/bitesize-spaceshooter.md)
  * [Invaders Bitesize Sample](../samples/bitesize/bitesize-invaders.md)
  * [Client-Driven Bitesize Sample](../samples/bitesize/bitesize-clientdriven.md)
