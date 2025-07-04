# RPCs vs NetworkVariables examples

This page has examples of how the Small Coop Sample (Boss Room) uses `RPC`s and `NetworkVariable`s. It gives guidance on when to use `RPC`s versus `NetworkVariable`s in your own projects.

See the [RPC vs NetworkVariable](rpcvnetvar.md) tutorial for more information.

## RPCs for movement

Boss Room uses RPCs to send movement inputs.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Gameplay/UserInput/ClientInputSender.cs
```

Boss Room wants the full history of inputs sent, not just the latest value. There is no need for `NetworkVariable`s, you just want to blast your inputs to the server. Since Boss Room isn't a twitch shooter, it sends inputs as reliable `RPC`s without worrying about the latency an input loss would add.

## Arrow's GameObject vs Fireball's VFX

The archer's arrows use a standalone `GameObject` that's replicated over time. Since this object's movements are slow, the Boss Room development team decided to use state (via the `NetworkTransform`) to replicate the ability's status (in case a client connected while the arrow was flying).

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Gameplay/GameplayObjects/Projectiles/PhysicsProjectile.cs
```

Boss Room might have used an `RPC` instead (for the Mage's projectile attack). Since the Mage's projectile fires quickly, the player experience isn't affected by the few milliseconds where a newly connected client might miss the projectile. In fact, it helps Boss Room save on bandwidth when managing a replicated object. Instead, Boss Room sends a single RPC to trigger the FX client side.

```csharp reference

https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Gameplay/GameplayObjects/Projectiles/FXProjectile.cs

```

## Breakable state

Boss Room might have used a "break" `RPC` to set a breakable object as broken and play the appropriate visual effects. Applying the "replicate information when a player joins the game mid-game" rule of thumb, the Boss Room development team used `NetworkVariable`s instead. Boss Room uses the `OnValueChanged` callback on those values to play the visual effects (and an initial check when spawning the `NetworkBehaviour`).

```csharp reference

https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Gameplay/GameplayObjects/Breakable.cs#L59-L78

```

The visual changes:

```csharp reference

https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Gameplay/GameplayObjects/Breakable.cs#L146-L156

```

**Error when connecting after imps have died**: The following is a small gotcha the Boss Room development team encountered while developing Boss Room. Using `NetworkVariable`s isn't magical. If you use `OnValueChanged`, you still need to make sure you initialize your values when spawning for the first time. `OnValueChanged` isn't called when connecting for the first time, only for the next value changes.

![imp not appearing dead](../images/01_imp_not_appearing_dead.png)


```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Gameplay/GameplayObjects/Character/ServerAnimationHandler.cs#L23-L30
```

## Hit points

Boss Room syncs all character and object hit points through `NetworkVariable`s, making it easy to collect data.

If Boss Room synced this data through `RPC`s, Boss Room would need to keep a list of `RPC`s to send to connecting players to ensure they get the latest hit point values for each object. Keeping a list of `RPC`s for each object to send to those `RPC`s on connecting would be a maintainability nightmare. By using `NetworkVariable`s, Boss Room lets the SDK do the work.
