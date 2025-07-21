# 2D Space Shooter sample

The [2D Space Shooter Project](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/tree/master/Basic/2DSpaceShooter) provides examples of physics, player health, and status effects using Netcode for GameObjects (Netcode). Technical features include[NetworkVariable](../../basics/networkvariable.md) and [ObjectPooling](../../advanced-topics/object-pooling.md).

## Server Authoritative Physics Movement

The movement in 2DSpaceShooter is physics based. The player object is a dynamic rigidbody and can collide with other players or asteroids. Physics in multiplayer games can be hard to get right. For simplicity, 2DSpaceShooter runs all movement and physics just on the server-side.

The client sends inputs in the form of RPCs to the server. The server then uses those inputs to adjust the throttle and turn values of the player.

This method of running physics makes sure that there are no desyncs or other physics issues between the client and server, but it introduces more latency.

## Player Health

2DSpaceShooter uses `NetworkVariable`s to track the players health and energy. Both variables are server authoritative, only the host or server can make changes to them. The client draws the player's health bar simply by accessing the value of the `NetworkVariable`.

For example:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/2DSpaceShooter/Assets/Scripts/ShipControl.cs#L172-L175
```

## Power-ups and Status Effects

The 2DSpaceShooter sample has power-ups which apply different status effects to a player on collection. The core implementation of the power up effects in `ShipControl.cs` is simplistic.

The power-ups themselves are server authorative. On the server they check if a player has entered their trigger and then apply a timed status effect to that player and disappear.

The `ShipControl.cs` of the player object tracks each status effect. `NetworkVariable`s are used as duration timers to control the beginning and end of status effects. You can also use regular floats for timers. By using `NetworkVariable`s, the client can use this information to display different graphics based on active buffs to players.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/2DSpaceShooter/Assets/Scripts/ShipControl.cs#L431-L486
```

## NetworkObject Pooling

The `2DSpaceShooter` object creates many objects dynamically at runtime including bullets, asteroids, and pickups. 2DSpaceShooter uses object pooling to avoid performance issues of instantiating and destroying Unity Objects all the time and creating allocations in the process.

2DSpaceShooter uses the NetworkObjectPool script, which can be found in the Community Contributions Repository.

![pool img](../../images/bitesize/invader-networkobjectpool.png)

All of the runtime spawnable objects have been registered to the pool. On the client-side, this will cause Netcode to use an object from the pool instead of instantiating a new Object. When the NetworkObject is despawned, it will be automatically returned to the pool instead of getting destroyed.

Adding the `NetworkObjectPool` to the scene won't yet pool server objects because these must be manually created and then spawned by the user. Instead of instantiating objects, your code should take them from the pool.

Regular Netcode Spawn Code example:

```csharp
GameObject powerUp = Instantiate(m_PowerupPrefab);
powerUp.GetComponent<NetworkObject>().Spawn(null, true);
```

Pooled Netcode Spawn Code example:

```csharp
GameObject powerUp = m_ObjectPool.GetNetworkObject(m_PowerupPrefab);
powerUp.GetComponent<NetworkObject>().Spawn(null, true);
```

<!--  https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/2DSpaceShooter/Assets/Scripts/Spawner.cs#L153-L156 -->

:::tip
If you are using Unity 2021, you can use the built-in [Object Pooling API](https://docs.unity3d.com/2021.1/Documentation/ScriptReference/Pool.ObjectPool_1.html) instead to build your own object pools.
:::
