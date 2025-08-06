# Optimizing Boss Room performance

Optimization is critical for networked and non-networked games. However, you have more limitations and constraints to consider when building a networked game. For example, networked games must account for latency, packet loss, reliability, and bandwidth limitations.

One of the more critical optimizations of a networked game is how much bandwidth it consumes. The following issues can occur with networked games that have high bandwidth usage:

* The game instance drops packets, which can occur at the transport or NIC (network interface card) layers
* Latency induced discrepancies between players depend upon their network capabilities.
  * A player with a latent and/or lower bandwidth capable connection can often lead to a less than nominal experience than players with a low latency and/or higher bandwidth capable connections.

As such, a good optimization goal for a netcode enabled game is to keep the bandwidth consumption reduced where possible. Netcode enabled games that have been optimized for bandwidth usage can help deliver a smoother player experience and reduce the overall network requirements for both servers and clients.

The following sections cover a handful of the most impactful optimization techniques used in the Boss Room sample, including

* [RPCs vs. NetworkVariables](#rpcs-vs-networkvariables)
* [NetworkTransform configuration](#networktransform-configuration)
* [Pooling](#pooling)
* [Unity Transport properties configuration](#utp-properties-configuration)
* [NetworkManager tick rate tweaking](#utp-properties-configuration)

## RPCs vs. NetworkVariables {#rpcs-vs-networkvariables}

Remote procedure calls (RPCs) and networked variables are the two primary ways to synchronize data between the server and clients. See [RPCs vs. NetworkVariable](../../learn/rpcvnetvar.md) to learn more about the differences between these two approaches.

You can optimize bandwidth usage by carefully deciding when to use RPCs versus NetworkVariables. As a general rule, you should:

* Use RPCs for temporary events with information that's only useful for a moment (when it's first received).
* Use NetworkVariables for persistent states with information that's useful for more extended periods (for example, long-term state syncing).

RPCs require less bandwidth than NetworkVariables, so using RPCs for temporary events for which the client doesn't need to subscribe to updates saves bandwidth.

For example, consider the Archer's and the Mage's attacks. The Archer's projectile is a slow, aimed attack that takes time to reach its target and only registers a hit when colliding with another character or object. In this case, creating a NetworkObject for the Archer's projectile and replicating its position with a NetworkTransform component (which uses NetworkVariables internally) makes sense to ensure the arrow stays in sync over multiple seconds.

However, the Mage's projectile is much faster and always seeks its target to hit it. In the case of the Mage's projectile, it does not matter if all clients see the projectile in the same place simultaneously; they only need to see it launch and hit its target. As a result, you can save bandwidth by using an RPC to trigger the creation of the visual effect on the clients and have each client interpret that event without data from the server. In this case, the server only needs to send data when the Mage triggers the ability instead of spawning a new NetworkObject that has the visual effects (VFX) and synchronizing the motion of the VFX as it heads towards its target over each network tick.

For more examples, see [RPCs vs. NetworkVariables Examples](../../learn/rpcnetvarexamples.md).

## NetworkTransform configuration {#networktransform-configuration}

The NetworkTransform component handles the synchronization of a NetworkObject's Transform. By default, the NetworkTransform component synchronizes every part of the transform at every tick if a change bigger than the specified [threshold](../../components/networktransform.md) occurs. However, you can configure it to only synchronize the necessary data by omitting particular axeis of the position, rotation, or scale vectors. See [Restricting synchronization](../../components/networktransform.md).

You can also increase the thresholds to reduce the frequency of updates if you don't mind reducing the accuracy and responsiveness of the replicated Transform.

Before optimization, the Boss Room sample contained a lot of unnecessary data. This is due to NetworkTransform synchronizing every axis' position, rotation, and scale information by default. So, we restricted every NetworkTransform only to synchronize the required data to reduce bandwidth usage.

Since the characters evolve on a plane, we only synchronize their position's x and z components and their rotation about the y-axis.

Additionally, with the changes introduced in [Netcode for GameObjects v1.4.0](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/releases/tag/ngo%2F1.4.0), we were able to further reduce the bandwidth cost associated for some prefabs that utilized NetworkTransform. The synchronization payload was reduced by 5 bytes for the Character and the Arrow prefab inside Boss Room, for example, by enabling "Use Half Float Precision" on their respective NetworkTransforms.

See [NetworkTransform](../../components/networktransform.md) for more information on the NetworkTransform component.

## Pooling {#pooling}

Object pooling is a creational design pattern that pre-instantiates all required objects before gameplay. It removes the need to create new objects or destroy old ones while the game runs. It creates a set amount of GameObjects before the game's runtime and inactivates or activates the required GameObjects, effectively recycling the GameObject and never destroying it.

You can use object pooling to optimize both networked and non-networked games by lowering the burden placed on the CPU and reudcing expensive memory allocations by pooling GameObjects before gameplay starts instead of having to rapidly create and destroy them.

See [Object pooling](../../advanced-topics/object-pooling.md).

## Unity Transport properties configuration {#utp-properties-configuration}

The [Unity Transport package](../../../transport/about.md) has properties you can configure to meet a game's needs. You can find these properties in the Inspector of the UnityTransport script.

The Boss Room sample shows how to adapt some Unity Transport properties to fit its specific requirements. Most of the Unity Transport property configuration remains unchanged. However, we changed the following property values:

* [Disconnect Timeout](#disconnect-timeout)
* [Max Connect Attempts](#max-connect-attempts)
* [Connect Timeout](#connect-timeout)
* [Max Packet Queue Size](#max-packet-queue-size)

### Disconnect Timeout {#disconnect-timeout}

The [Disconnect Timeout property](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.Transports.UTP.UnityTransport.html#Unity_Netcode_Transports_UTP_UnityTransport_DisconnectTimeoutMS) controls how long the server (and the clients) wait before disconnecting. The Boss Room sample uses a Disconnect Timeout value of 10 seconds to prevent the server and the clients from hanging onto a connection for too long.

### Max Connect Attempts {#max-connect-attempts}

The [Max Connect Attempts property](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.Transports.UTP.UnityTransport.html#Unity_Netcode_Transports_UTP_UnityTransport_MaxConnectAttempts) controls the times a client tries to connect before declaring a connection failure. The Boss Room sample uses a Max Connect Attempts value of 10 to prevent clients from waiting too long before declaring a connection failure.

### Connect Timeout {#connect-timeout}

The [Connect Timeout property](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.Transports.UTP.UnityTransport.html#Unity_Netcode_Transports_UTP_UnityTransport_ConnectTimeoutMS) controls the number of times clients attempt to connect per second. The Boss Room sample uses a Connect Timeout value of 1 second, meaning that clients try to connect once per second. Having the Connect Timeout set to 1 second and the Max Connect Attempts set to 10 means that clients fail to connect after 10 seconds of waiting.

### Max Packet Queue Size {#max-packet-queue-size}

The [Max Packet Queue Size property](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.Transports.UTP.UnityTransport.html#Unity_Netcode_Transports_UTP_UnityTransport_MaxPacketQueueSize) defines the maximum number of packets that can be sent and received during a single frame.

The impact of surpassing the Max Packet Queue Size threshold varies depending on the packet direction (sending or receiving).

* If a client or the server tries to **send** more packets than the Max Packet Queue Size value during a single frame, Unity Transport buffers the extra packets inside a send queue until the next frame update.
* If a client or the server **receives** more packets than the Max Packet Queue Size value during a single frame, the operating system buffers the extra packets.

Setting the Max Packet Queue Size parameter too low can introduce some [jitter](../../learn/lagandpacketloss.md) during frames where the number of packets sent or received is too high.  On the other hand, setting the Max Packet Queue Size parameter too high would use more memory than necessary.

:::note
**Note**: Each unit in the Max Packet Queue Size parameter uses roughly 4 KB of memory. This value is based on twice the maximum size of a packet, which Unity Transport defines internally (it isn't exposed to Netcode users since this parameter governs the sizes of both the send and receive queues).
:::

The Boss Room sample uses a Max Packet Queue Size property value of 256. The reasoning behind choosing 256 is specific to how Boss Room uses Unity Transport.

In the Boss Room sample, we found that the Max Packet Queue Size default value was too low, so we increased it. However, it didn't make sense to increase the Max Packet Queue Size too much because Boss Room only uses reliable channels for its NetworkVariable updates, RPCs, and custom messages. Reliable channels only support 32 in-flight packets per connection.

As a result, the maximum number of reliable packets sent or received in a single frame is the number of players (minus the host) multiplied by 32 (the maximum number of in-flight packets). For example, a game of eight players would have a maximum of 224 (seven times 32) reliable in-flight packets. To allow some leeway for the internal traffic from Unity Transport itself (such as resend/ACKs of reliable packets and heartbeats), we chose a slightly higher value of 256.

## NetworkManager tick rate configuration {#networkmanager-tick-rate-configuration}

Netcode's [NetworkManager](../../components/networkmanager.md) provides some configuration options, one of which is the [tick rate](../../advanced-topics/networktime-ticks.md). The tick rate configuration option determines the frequency at which network ticks occur. The ideal tick rate value relies on balancing smoothness, accuracy, and bandwidth usage.

Lowering the tick rate reduces the frequency of NetworkVariable update messages (because they're sent at each tick). However, since it reduces the frequency of updates, it also reduces the smoothness of gameplay for the clients. You can reduce the impact of lower tick rates by using interpolation to provide smoothness, such as in the NetworkTransform. However, because there are fewer updates, the interpolation will be less accurate because it has less information.

In the Boss Room sample, we found an ideal tick rate value by manually testing different values using the same scenarios. To define the scenarios, we looked at where we used NetworkVariables and which most often needed to be updated to provide smooth gameplay. We identified NetworkTransforms as the most sensitive use of NetworkVariables, so we defined our scenarios around players moving, launching arrows, and tossing items. We compared video captures of these scenarios and determined that the smallest tick rate value that gave clients smooth and accurate gameplay was 30 ticks per second.

## Further potential optimizations {#further-potential-optimizations}

Even with all the optimizations above, it's possible to further reduce the bandwidth requirements of the Boss Room sample with additional optimizations.

As a vertical slice of a small-scale co-op game, the bandwidth usage of the Boss Room sample is already pretty low. Still, the following techniques can reduce it further or improve other performance considerations, such as responsiveness.

### Unreliable RPCs vs. reliable RPCs {#unreliable-rpcs-vs-reliable-rpcs}

You can use unreliable RPCs when possible instead of only using reliable RPCs. The Boss Room sample uses only reliable messages for RPCs, which means that if a client doesn't receive an RPC sent by the server, the server will resend it until the client receives and acknowledges it (and vice versa).

Reliable messages aren't necessary for all RPCs, though. For example, it wouldn't pose that much of an issue if a client (having network issues) doesn't receive the RPC triggering the visual effect of a single Imp's attack. This is because the client would receive the impact of the Imp's attack (such as position updates and hit points) after recovering from the issue. As a result, there's no need to slow down the server and, by extension, the whole game to ensure that this client properly receives this specific message. An unreliable RPC would increase the game's responsiveness without much downside in such a case.

### Sent data size {#sent-data-size}

You can reduce the size of sent data by using smaller primitive types or encoding values into bits and bytes.

One method of encoding values into smaller primitive types is quantization. Quantization involves mapping input values from a large set to output values in a smaller set. Common examples of quantization include rounding and truncating.

Take the following example. It shows how quantization can considerably reduce bandwidth in scenarios where high precision isn't essential.

Let's say you're designing a game where you have positional values between 0 and 1000, and you don't need values more precise than a decimeter. Positional values typically use 3x32 bits (for the three floats that comprise a Vector3). However, since, in your game, precision isn't essential past the size of a decimeter, you can serialize the positional values by multiplying by 10, then only encode the integer part of the product. Doing so gives you the precision you need with fewer bits. Instead of 32 bits per axis, you use 14, which means that the total number of bits you need is 48 instead of the usual 96. When you receive the data, you can deserialize it by dividing the value by 10 to obtain the float value.

See [Quantization](https://en.wikipedia.org/wiki/Quantization_(signal_processing)) and [Snapshot compression](https://gafferongames.com/post/snapshot_compression/#optimizing-position).

### Pathfinding optimizations {#pathfinding-optimizations}

There are multiple ways to implement pathfinding in a networked game, each with tradeoffs between bandwidth usage, CPU costs, determinism, and position synchronization. The most common pathfinding techniques include:

* [Calculating the paths and moving everything on the server.](#technique-1)
* [Calculating the paths on the server, then sending them to the clients.](#technique-1)
* [Calculating the paths on the client and sending the movement directions to the server.](#technique-1)

#### Calculating the paths and moving everything on the server {#technique-1}

The first technique involves calculating the paths, moving everything on the server, then synchronizing the positions with the clients. This technique is easy to implement and ensures that clients have the latest positions. However, it relies on sending position updates every frame, which uses a lot of bandwidth and doesn't scale well if a game has many characters moving around.

| PROS                                       | CONS                                            |
|--------------------------------------------|-------------------------------------------------|
| Easy to implement                          | Uses a lot of bandwidth                         |
| Ensures clients have the latest positions  | Doesn't scale well with many moving characters  |

#### Calculating the paths on the server, then sending them to the clients {#technique-2}

The second technique involves calculating the paths on the server, then sending the paths to the clients and allowing them to simulate the movements themselves. It works for both deterministic and non-deterministic pathfinding by sending key position updates. Calculating the paths on the server reduces bandwidth usage, but implementing this technique involves a lot of hidden complexity.

An issue with this technique is that, since clients receive the whole paths of characters, it can lead to cheating because clients know where other players will be in the future. Clients knowing the future position of other players can introduce a big issue in competitive games like MOBA multiplayer online battle arena (MOBA) and real-time strategy (RTS) games.

| PROS                                                            | CONS                                                                                   |
|-----------------------------------------------------------------|----------------------------------------------------------------------------------------|
| Reduces bandwidth usage                                         | Implementation involves a lot of hidden complexity                                     |
| Works for both deterministic and non-deterministic pathfinding  | Increases the likelihood of cheating by knowing the future positions of other players  |

A variant of this technique is to send only the destination and let the clients calculate the paths (instead of sending the whole paths). Offloading path calculation to the clients saves even more bandwidth but adds CPU costs to clients and makes determinism more difficult.

#### Calculating the paths on the client and sending the movement directions to the server {#technique-3}

The third technique involves calculating the paths on the clients, then sending the movement directions to the server. After receiving the client movement directions, the server synchronizes the positions across the clients. This technique is easy to implement and saves server CPU usage but uses much more bandwidth for constant synchronization. It also only works for player-controlled entities.

| PROS                      | CONS                                       |
|---------------------------|--------------------------------------------|
| Easy to implement         | Increases bandwidth usage                  |
| Reduces server CPU usage  | Only works for player-controlled entities  |

#### Pathfinding in Boss Room {#pathfinding-in-boss-room}

The Boss Room sample uses the first technique: it calculates the paths and moves everything on the server to keep the positions synchronized with the clients. This technique works well with the Boss Room sample because it doesn't have too many characters moving simultaneously.

The second technique wasn't an option because some actions, like the Archer's attacks, require aiming. A position that can become desynchronized between a client and the server can negatively impact gameplay.

The third technique wasn't ideal either because most of the entities in the Boss Room sample aren't player-controlled.

## Dedicated game server (DGS) optimizations {#dgs-optimizations}

See [DGS game changes](../../../tools/porting-to-dgs/game-changes.md) and [Optimizing server builds](../../../tools/porting-to-dgs/optimizing-server-builds.md).
