# Reliability

## Introduction
RPCs **are reliable by default**.  This means they're guaranteed to be received and executed on the remote side. However, sometimes developers might want to opt-out reliability, which is often the case for non-critical events such as particle effects, sounds effects etc.

> [!NOTE]
>Packet reliability can be a two edged sword (pro and con):
>- **The Pro**: Under bad network conditions a reliable packet is guaranteed to be received by the target remote side.
>- **The Con**: Because the sender will continue trying to send a reliable packet if it does not get a "packet received" response, under bad network conditions too many reliable packets being sent can cause additional bandwidth overhead.

### Reliable and Unreliable RPC Examples:
Reliability configuration can be specified for `Rpc` methods at compile-time:

```csharp

[Rpc(SendTo.Server)]
void MyReliableServerRpc() { /* ... */ }

[Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
void MyUnreliableServerRpc() { /* ... */ }
```

Reliable RPCs will be received on the remote end in the same order they are sent, but this in-order guarantee only applies to RPCs on the same `NetworkObject`. Different `NetworkObjects` might have reliable RPCs called but executed in different order compared to each other. To put more simply, **in-order reliable RPC execution is guaranteed per `NetworkObject` basis only**.  If you determine an RPC is being updated often (that is, several times per second), it _might_ be better suited as an unreliable RPC.    

> [!NOTE]
> When testing unreliable RPCs on a local network, the chance of an unreliable packet being dropped is reduced greatly (sometimes never).  As such, you might want to use [`UnityTransport`'s Simulator Pipeline](https://docs-multiplayer.unity3d.com/transport/current/pipelines#simulator-pipeline) to simulate poor network conditions to better determine how dropped unreliable RPC messages impacts your project.

> [!NOTE]
> Sometimes you might find out that you are sending parameters that are both critical and non-critical.  Under this scenario you might benefit by splitting the RPC into two or more RPCs with the less critical parameters being sent unreliably (possibly at the same or higher frequency) while the more critical parameters are sent reliably (possibly at the same or lower frequency).

### Additional RPC Facts
- An RPC call made without an active connection won't be automatically added to the send queue and will be dropped/ignored.
- Both reliable and unreliable RPC calls have to be made when there is an active network connection established between a client and the server. _The caveat to this rule is if you are running a host since a host is both a server and a client:_
    - _A host can send RPC messages from the "host-server" to the "host-client" and vice versa._
- Reliable RPC calls made during connection will be dropped if the sender disconnects before the RPC is sent.
