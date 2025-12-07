# Install Netcode for GameObjects

Follow the instructions on this page to set up Netcode for GameObjects in your Unity project.

##  Prerequisites

Before you begin, you need the following:

- An active Unity account with a valid license.
- A supported version of Unity. Check [Netcode for GameObjects' requirements](#netcode-installation-requirements) for the specific version details.
- An existing Unity project. If you're new to Unity, you can refer to the [get started](./tutorials/get-started-with-ngo.md) section for guidance.

### Compatibility

- Unity Editor version 2021.3 or later
- Mono and IL2CPP [scripting backends](https://docs.unity3d.com/Manual/scripting-backends.html)

### Supported platforms

- Windows, MacOS, and Linux
- iOS and Android
- XR platforms on Windows, Android, and iOS
- Most [**closed platforms**](https://unity.com/platform-installation), such as consoles. Contact the [development team](https://discord.com/channels/449263083769036810/563033158480691211) for more information about specific closed platforms.
- WebGL (requires Netcode for GameObjects 1.2.0+ and [Unity Transport](https://docs-multiplayer.unity3d.com/transport/current/about/) 2.0.0+)

> [!NOTE]
> When working with closed platforms like consoles (PlayStation, Xbox, Nintendo Switch), there may be specific policies and considerations. Refer to your console's documentation for more information.

## Install Netcode for GameObjects with the Package Manager

1. From the Unity Editor, select **Window** > **Package Manager**.
1. From the Package Manager, select **Add** ![add symbol](images/add.png) > **Add package by name…**
1. Type (or copy and paste) `com.unity.netcode.gameobjects` into the package name field, then select **Add**.

### For Unity Editor versions 2020.3 LTS or earlier

1. From the Unity Editor, select **Window** > **Package Manager**.
1. From the Package Manager, select **Add** ![add symbol](images/add.png) > **Add package by git URL…**
1. Type (or copy and paste) `https://github.com/Unity-Technologies/com.unity.netcode.gameobjects` into the git URL field, then select **Add**.

## Next Steps

After installing Netcode for GameObjects, see the following content to continue your journey:

* Use the [Get started with Netcode for GameObjects tutorial](./tutorials/get-started-with-ngo.md) to create a project, test your installation, and learn how to use the basic features of Netcode for GameObjects.
