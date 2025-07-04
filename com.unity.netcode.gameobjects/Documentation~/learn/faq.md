# Frequently asked questions

The FAQ provides immediate answers for questions collected from the Community on developing games with Multiplayer, including Netcode for GameObjects (Netcode), Transport, and more.

## Unity Netcode for GameObjects (Netcode)

<div id="faq">

### Does Netcode have a Public Roadmap?

See the [Multiplayer Networking Public Roadmap](https://unity.com/roadmap/unity-platform/multiplayer-networking) to review, suggest, and vote on features for all Multiplayer Networking, Netcode, and documentation.

### Implement a Dedicated Server vs a Listen Server, would I need to make changes to the codebase?

Netcode works with both a Listen Server and a Dedicated Server model. You just have to be careful that you're using things like `IsServer` and `IsHost` correctly.

### Does Netcode have any Matchmaking functionality?

You would have to implement matchmaking using third party matchmaking services. Netcode has no matchmaking functionality.

### What are recommendations for getting a game connecting over the internet?

The Multiplayer Technology Team recommend the following:

1. Upload your server build to a cloud provider or another server machine and run it there.
2. Host your game locally and open ports in your router.

   It should work just like over LAN you just have to connect to the public IP itself. Make sure to open UDP on the router. Many forward ports tutorials are available to review, including this option https://portforward.com/.

3. Use a Relay.

   [Unity Relay](https://docs.unity.com/relay/introduction.html) offers a way for game developers to securely offer increased connectivity between players by using a join code style workflow without needing to invest in a third-party solution. The Relay service enables developers to connect players together using a listen server. Instead of using dedicated game servers (DGS), the Relay service provides connectivity through a universal Relay server acting as a proxy.

   Another option is to use something like the `SteamP2PTransport`, which will work without the need of setting up any servers if you release your game on Steam.

### Is it good for add Spawnable object into NetworkConfig after start host?

Yes, but you need to ensure that all your clients add the same object to their configurations as well. You can't only add it on the host.

### How do I join from two devices on the same network?

The client need to use the local IPv4 address of your server. On Windows, you can find that address by opening the Command Prompt and typing `ipconfig`. Search for the `IPv4 Address` value on the server's machine.

Your client needs an input field to input the server's IP. If you just need something for prototyping, you can use [NetworkManagerHud](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/com.community.netcode.extensions/Runtime/NetworkManagerHud). You should attach this code to the NetworkManager GameObject.

### What's the best method for spawning?

Spawning can always be done on the host/server. If you want to give a client control over spawning objects, you need to implement a server RPC that you call on the client to spawn the object.

### What are best practices for handing physics with Netcode?

Networked physics is complicated, with many different ways to handle them. Currently, physics can be a little difficult to handle with Netcode and the built-in `NetworkTransform`.

The Multiplayer Technology Team recommends the following:

- Simulate the physics on the server and transforms on the client.
- Remove the Rigidbody component from all your client objects and have just the server drive the position of the physics object. This should help reduce stutter on the client.

### How do you implement on Steam?

The Steam transport should be quite straightforward to use. Just add it to your project and set the `ConnectToSteamID` in the transport on the client to connect to the host that's all you need.

</div>

## Boss Room and Bitesize Samples

<div id="faq">

### Why do I get path too long errors with Boss Room on Windows?

Using Windows' built-in extracting tool may generate an `Error 0x80010135: Path too long` error window which can invalidate the extraction process.

As a workaround, shorten the zip file to a single character (for example "c.zip") and move it to the shortest path on your computer (such as in root `C:\`) and retry. If that solution fails, another workaround is to extract the downloaded zip file using an application like 7zip.

### Why do I get unidentified developer errors with Boss Room?

If you try to run a build on OSX and receive a warning dialog mentioning an "unidentified developer," you might need to override your security settings for this application:

1. In the Finder on your Mac, locate the application you want to open.

> [!NOTE]
> Don't use Launchpad, it doesn't allow you to access the shortcut menu.

1. Control-click the app icon, then choose **Open** from the shortcut menu.
1. Select **Open**.

The app is saved as an exception to your security settings. You can open it in the future by double-clicking it just as you can any registered app.

See [Apple Support](https://support.apple.com/guide/mac-help/open-a-mac-app-from-an-unidentified-developer-mh40616/mac) for details.

### Why is there an `InitBootStrap` scene as the startup scene for Boss Room and Bitesize Samples?

The initial reason is that in Netcode the `NetworkManager` is a singleton class. The Bitesize Samples Team initially created it in the main menu, but when the host was leaving the in-game/networked scene, the Network Manager was getting destroyed, which led to not being able to host a game again without restarting the game instance.

The Bootstrap scene ensures that the NetworkManager and other singletons are initialized first and will be there when you get back to main menu.

</div>
