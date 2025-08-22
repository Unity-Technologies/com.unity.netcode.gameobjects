#  Create a command-line helper for testing

This page walks you through how to create a command-line helper that launches your project outside the Unity Editor to make testing builds easier.

Using a command-line helper script to launch multiple instances of a game build isn't the only way to test a multiplayer game. You can also use the Unity Editor or the [Multiplayer Play Mode package](https://docs-multiplayer.unity3d.com/mppm/current/about/).

## Create the command-line helper

1. Right-click the **Assets** folder in the **Projects** tab, then select **Create** > **Folder**.
2. Name the new folder **Scripts**.
3. Right-click the **Scripts** folder you created, then select **Create** > **C# Script**.
4. Name the script **NetworkCommandLine**.
5. Right-click on **NetworkManager** within the **Hierarchy** list, then select **Create Empty**.
6. Name the new GameObject **NetworkCommandLine**.
7. With the `NetworkCommandLine` GameObject selected, select **Add Component** from the **Inspector** tab.
8. For the new component, select **Scripts** > **Network Command Line** (the `NetworkCommandLine.cs` script you created earlier).
9. Double-click on the **NetworkCommandLine.cs** script from the **Project** tab to open it in a text editor.
10. Edit the `NetworkCommandLine.cs` script to match the following code snippet:

```csharp
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

11. Save the file, then return to the Unity Editor.
12. Open the Build Settings window by selecting **File** > **Build Settings**.
13. Select **Player Settings…**.
14. Beneath **Settings for PC, Mac, & Linux Standalone**, select **Resolution and Presentation** to open the section options.
15. From **Resolution** > **Fullscreen Mode**, change **Fullscreen Window** to **Windowed**.
16. Return to the Editor main window and save your scene.

## Test the command-line helper

Follow these instructions to test that the command-line helper script works.

1. Select **File** > **Build and Run**.
2. Create a new folder called `Build` inside your Hello World project folder.
3. **Save As** the binary `HelloWorld`.

Saving the project in this way causes the Unity Editor to build and launch the project in a new window. After it launches (and displays the plane), close the window you just launched.

### Test on Windows

To test on Windows:

1. Open the Command Prompt.
2. Use the following command to launch the server and the client. Make sure to replace the placeholder text within the angled brackets (`< >`) for all commands.
    * You might get a [UAC prompt](https://learn.microsoft.com/windows/security/identity-protection/user-account-control/how-user-account-control-works) requesting permission to run the executable. Allow the executable to run to continue.

Command to start the server:

```cmd
<Path to Project>\HelloWorld.exe -mode server
```

Command to start the client:

```cmd
<path to project>\HelloWorld.exe -mode client
```

To run these commands on a single line:

```cmd
<path to project>\HelloWorld.exe -mode server & <path to project>\HelloWorld.exe -mode client
```

Here's an example of what your command might look like when you replace the placeholder text in `< >`:

```cmd
<path to project>\HelloWorld.exe -mode server & <path to project>\HelloWorld.exe -mode client
```
There's no standard out stream on Windows by default, so you need to view the `Debug.log` file for the outputs. You can find the `Debug.log` files in:

```cmd
C:\Users\username\AppData\LocalLow\CompanyName\ProductName\Player.log
```

Where the `CompanyName` defaults to `DefaultCompany` for a new project and `ProductName` equals to the project's name.

You can also change the Windows commands to create a `log.txt` file in the same folder as your `HelloWorld` folder.

Change the commands as follows:

Server command:

```cmd
<Path to Project>\HelloWorld.exe -logfile log-server.txt -mode server
```

Client command:

```cmd
<Path to Project>\HelloWorld.exe  -logfile log-client.txt -mode client
```

Example (Running as a single command line):

```cmd
C:\Users\sarao>HelloWorld\Build\HelloWorld.exe -logfile log-server.txt -mode server & HelloWorld\Build\HelloWorld.exe -logfile log-client.txt -mode client
```

### Test on macOS

Use the following instructions if you're using macOS:

1. Open the Terminal app.
2. Use the following command to launch the server and the client. Make sure to replace the placeholder text within the angled brackets (`< >`) for all commands.

Command to start the server:

```shell
<Path to Project>/HelloWorld.app/Contents/MacOS/<Project Name> -mode server -logfile -
```

Command to start the client:

```shell
<Path to Project>/HelloWorld.app/Contents/MacOS/<Project Name> -mode client -logfile -
```

To run both as a single command:

```shell
<Path to Project>/HelloWorld.app/Contents/MacOS/<Project Name> -mode server -logfile - & <Path to Project>HelloWorld.app/Contents/MacOS/<Project Name> -mode client -logfile -
```
