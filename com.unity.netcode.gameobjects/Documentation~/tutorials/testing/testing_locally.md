# Testing multiplayer games locally

Testing a multiplayer game presents unique challenges:

- You need to run multiple instances of the game to test multiplayer scenarios.
- You also need to iterate quickly on custom code and asset changes and validate work in a multiplayer scenario.
- You need to be able to debug work in a multiplayer scenario using Editor tools.

There are several different ways you can test multiplayer games locally:

- Using [player builds](#player-builds) to validate work on a target distribution platform, although player builds can be slow for local iteration.
- Using the [Multiplayer Play Mode package](#multiplayer-play-mode) to simulate up to four players simultaneously on the same development device.
- Using third-party tools such as [ParrelSync](https://github.com/VeriorPies/ParrelSync).

## Player builds

Player builds are best used to verify that your game works on target platforms or with a wider group of testers. Start by building an executable to distribute.

1. Navigate to **File** > **Build Settings** in the menu bar.
1. Click **Build**.

### Local iteration using player builds

After the executable build completes, you can distribute it to testers and launch several instances of the built executable to both host and join a game. You can also run the build alongside the Editor that produced it, which can be useful during iterative development.

Though functional, this approach can be somewhat slow for the purposes of local iteration. You can use [Multiplayer Play Mode](#multiplayer-play-mode) to iterate locally at speed.

> [!NOTE]
> To run multiple instances of the same app, you need to use the command line on MacOS. Run `open -n YourAppName.app`.

## Multiplayer Play Mode

Multiplayer Play Mode is a Unity package you can use to simulate up to four players simultaneously on the same development device while using the same source assets on disk. It allows you to reduce project build times, run your game locally, and test the server-client relationship, all from within the Unity Editor.  

For more details, refer to the [Multiplayer Play Mode documentation](https://docs-multiplayer.unity3d.com/mppm/current/about/).
