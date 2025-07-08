# Improving performance with client-side interpolation

As outlined in [Understanding latency](lagandpacketloss.md), latency and jitter can negatively affect gameplay experience for users. In addition to managing [tick and update rates](ticks-and-update-rates.md), you can also use client-side interpolation to improve perceived latency for users.

## Without latency mitigation

Without any kind of compensation for latency, the client performs two simple operations:

* Sends inputs from the client to the server
* Receives states from the server and renders them

This is a conservative approach that never shows an incorrect user state, but exposes users directly to the effects of latency. Regardless of the client's potential frame rate, the game will only ever run at the speed the server (with its limiting [network factors](lagandpacketloss.md#network-latency)) is capable of, which can severely reduce frame rates and produce perceivable input lag. It also produces an uneven game speed, as jitter in the network causes states to arrive and be rendered at inconsistent intervals.

In addition to reduced responsiveness, this approach can also directly interfere with gameplay in certain genres such as first-person shooters. Because the information from the server is always slightly behind, due to network latency, players are forced to aim ahead of their target to compensate.

## With client-side interpolation

When using client-side interpolation, all clients intentionally run slightly behind the server, giving them time to transition smoothly between state updates and conceal the effects of latency from users. To implement client-side interpolation, use the [NetworkTransform](../components/networktransform.md) component.

In a standard client-server [topology](../terms-concepts/network-topologies.md), clients are able to render a state that's approximately half the [round-trip time (RTT)](lagandpacketloss.md#round-trip-time-and-pings) behind the server. When using client-side interpolation, a further intentional delay is added so that by the time the client is rendering state _n_, it's already received state _n+1_, which allows the client to always smoothly transition from one state to another. This is effectively increasing latency, but in a consistent, client-universal way that can be used to reduce the negative impacts of unpredictable network jitter on gameplay.

The delay added to enable client-side interpolation is called the interpolation period, and should always be less than the interval the server waits between sending packets to clients, to avoid stutter.

## Boss Room example

In the [Boss Room sample](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/), NetworkTransform components are used for client-side interpolation. Since transforms are updated in FixedUpdate, however, there is also some server-side interpolation that is needed on the host to smooth out movement. See [PhysicsProjectile](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Gameplay/GameplayObjects/Projectiles/PhysicsProjectile.cs) and [PositionLerper](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Utils/PositionLerper.cs) for an example of how it's done.
