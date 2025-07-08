# RPC

Any process can communicate with any other process by sending a remote procedure call (RPC). As of Netcode for GameObjects version 1.8.0, the `Rpc` attribute encompasses server to client RPCs, client to server RPCs, and client to client RPCs.

![](../../images/sequence_diagrams/RPCs/ServerRPCs.png)

![](../../images/sequence_diagrams/RPCs/ClientRPCs.png)

## Declaring an RPC

Declare an RPC by marking a method with the `[Rpc]` attribute and including the `Rpc` suffix in the method name. RPCs have a number of possible targets that can be declared at both runtime and compile time, but a default must be passed to the `[Rpc]` attribute. For example, to create an RPC that will be executed on a server, declare it like this:

```csharp
[Rpc(SendTo.Server)]
public void PingRpc(int pingCount) { /* ... */ }

[Rpc(SendTo.NotServer)]
void PongRpc(int pingCount, string message) { /* ... */ }
```

## Invoking an RPC

Invoke an RPC at [compile time](#compile-time-targets) or [runtime](#runtime-targets) by invoking the function directly with parameters:

```csharp
[Rpc(SendTo.Server)]
public void PingRpc(int pingCount)
{
    // Server -> Clients because PongRpc sends to NotServer
    // Note: This will send to all clients.
    // Sending to the specific client that requested the pong will be discussed in the next section.
    PongRpc(pingCount, "PONG!");
}

[Rpc(SendTo.NotServer)]
void PongRpc(int pingCount, string message)
{
    Debug.Log($"Received pong from server for ping {pingCount} and message {message}");
}

void Update()
{
    if (IsClient && Input.GetKeyDown(KeyCode.P))
    {
        // Client -> Server because PingRpc sends to Server
        PingRpc();
    }
}
```

You must mark client RPC methods with the `[Rpc]` attribute and use the `Rpc` method suffix. Failing to do so produces an error message.

```csharp
// Error: Invalid, missing 'Rpc' suffix in the method name
[Rpc(SendTo.Everyone)]
void Pong(int somenumber, string sometext) { /* ... */ }
```

The `[Rpc]` attribute and matching `...Rpc` suffix in the method name ensure context clarity in scripts that invoke them and are used by Netcode during the ILPostProcessor pass. The ILPostProcessor pass replaces all call-site locations where the RPC method is invoked with additional code, specifically generated for the RPC, to ensure that the RPC message is generated and sent to the appropriate targets rather than just locally invoking the method.

```csharp
Pong(somenumber, sometext); // Is this a local function or a remote function?

PongRpc(somenumber, sometext); // This is clearly an remote function
```

## RPC targets

### Compile-time targets

There are several default compile-time targets you can choose from. Some of these are only available in [client-server contexts](../../terms-concepts/client-server.md).

While client-to-client RPCs are supported, it's important to note that there are no direct connections between clients. When sending a client-to-client RPC, the RPC is sent to the server that acts as a proxy and forwards the same RPC message to all destination clients. If the targeted destination clients includes the local client, the local client will still execute the RPC immediately (or on the next frame locally, if deferred).

| Target                   | Description                                              |
| ---------------------------- | --- |
| `SendTo.Server`            | Send to the server, regardless of ownership. Executes locally if invoked on the server. |
| `SendTo.NotServer`        |Client-server only| Send to everyone _but_ the server, filtered to the current observer list. Won't send to a server running in host mode - it's still treated as a server. If you want to send to servers when they are host, but not when they are dedicated server, use `SendTo.ClientsAndHost`.<br /><br />Executes locally if invoked on a client. Doesn't execute locally if invoked on a server running in host mode. |
| `SendTo.Owner`             |Send to the NetworkObject's current owner. Executes locally if the local process is the owner. |
| `SendTo.NotOwner`          |Send to everyone but the current owner, filtered to the current observer list. Executes locally if the local process is not the owner.<br/><br/>In host mode, the host receives this message twice: once on the host client, once on the host server. |
| `SendTo.Me`                |Execute this RPC locally.<br/><br/>Normally this is no different from a standard function call.<br/><br/>Using the `DeferLocal` parameter of the attribute or the `LocalDeferMode` override in `RpcSendParams`, this can allow an RPC to be processed on localhost with a one-frame delay as if it were sent over the network. |
| `SendTo.NotMe`             |Send this RPC to everyone but the local machine, filtered to the current observer list. |
| `SendTo.Everyone`          |Send this RPC to everyone, filtered to the current observer list. Executes locally. |
| `SendTo.ClientsAndHost`    |Send this RPC to all clients, including the host, if a host exists. If the server is running in host mode, this is the same as `SendTo.Everyone`. If the server is running in dedicated server mode, this is the same as `SendTo.NotServer`. |
| `SendTo.SpecifiedInParams` |This RPC cannot be sent without passing in a target in `RpcSendParams`. |

### Runtime targets

By default, most targets are hard-coded at compile time: owner always sends to owner, server always sends to server, and so on. There are two cases where you can to specify a different set of targets at runtime:

- If the target is `SendTo.SpecifiedInParams`, you must pass a target in at runtime to send the RPC.
- If the `Rpc` parameter is declared with `AllowTargetOverride = true`, then you can optionally pass a target in at runtime to send the RPC to a different target than the compile-time default.

To send to a different target, you must define an additional parameter of type `RpcParameters` as the *last* parameter in your RPC method, and then pass your runtime targets in with that parameter. This gets special handling in the ILPostProcessor pass: the last parameter isn't serialized and sent across the network. Instead, it's read on the sending side to check for overrides to its default sending behavior using the `RpcParameters.Send` field (while `RpcParameters.Receive` will be ignored), and on the receiving side, is be populated with `RpcParameters.Receive` to provide information about the receiving context (while `RpcParameters.Send` is left uninitialized).

`RpcParameters` can be implicitly constructed from runtime send targets, making it simple to pass these targets in as parameters.

#### Built-in targets

`NetworkManager` and `NetworkBehaviour` both provide reference to a field called `RpcTarget`, which provides references to the various override targets you can pass in. The list mirrors the list of targets provided in the `SendTo` enum, and has the same behavior as described in the table above.

The `RpcTarget` object is shared by all `NetworkBehaviours` attached to a given `NetworkManager`, so you can get access to this via any `NetworkBehaviour` or via the `NetworkManager` object itself.

```csharp
public class SomeNetworkBehaviour : NetworkBehaviour
{
    [Rpc(SendTo.Server, AllowTargetOverride = true)]
    public void SomeRpc(int serializedParameter1, float serializedParameter2, RpcParams rpcParams)
    {

    }

    // Sends SomeRpc() to the owner instead of the server
    public void SendSomeRpcToOwner(int param1, float param2)
    {
        SomeRpc(param1, param2, RpcTarget.Owner);
    }
}

public class SomeOtherClass
{
    public SomeNetworkBehaviour Behaviour;

    public SendSomeRpcToOwnerOnBehaviour(int param1, float param2)
    {
        // Since this method is not within a NetworkBehaviour, RpcTarget.Owner must be accessed through
        // the Behaviour object via Behaviour.RpcTarget.Owner.
        Behaviour.SomeRpc(param1, param2, Behaviour.RpcTarget.Owner);
    }
}
```

#### Custom targets

If you need to send to a specific client ID or list of client IDs that are not covered by any of the existing named targets, you can use the following methods:

```csharp
// Send to a specific single client ID.
public BaseRpcTarget Single(ulong clientId, RpcTargetUse use) { /* ... */ }

// Send to everyone EXCEPT a specific single client ID.
public BaseRpcTarget Not(ulong excludedClientId, RpcTargetUse use) { /* ... */ }

// Sends to a group of client IDs.
public BaseRpcTarget Group(NativeArray<ulong> clientIds, RpcTargetUse use) { /* ... */ }
public BaseRpcTarget Group(NativeList<ulong> clientIds, RpcTargetUse use) { /* ... */ }
public BaseRpcTarget Group(ulong[] clientIds, RpcTargetUse use) { /* ... */ }
public BaseRpcTarget Group<T>(T clientIds, RpcTargetUse use) where T : IEnumerable<ulong>
{ /* ... */ }

// Sends to everyone EXCEPT a group of client IDs.
public BaseRpcTarget Not(NativeArray<ulong> excludedClientIds, RpcTargetUse use) { /* ... */ }
public BaseRpcTarget Not(NativeList<ulong> excludedClientIds, RpcTargetUse use) { /* ... */ }
public BaseRpcTarget Not(ulong[] excludedClientIds, RpcTargetUse use) { /* ... */ }
public BaseRpcTarget Not<T>(T excludedClientIds, RpcTargetUse use) where T : IEnumerable<ulong>
{ /* ... */ }
```

Each of these includes an `RpcTargetUse` parameter. This parameter controls the use of a particular performance optimization in situations where it can be used. Because `BaseRpcTarget` is a managed type, allocating a new one is expensive, as it puts pressure on the garbage collector.

If you're only creating the `BaseRpcTarget` instance for one-time use to pass it to an RPC and then discard it, `RpcTargetUse.Temp` can be used to avoid these allocations. It does this by making use of a persistent cached `BaseRpcTarget` object that it populates with new data. The downside of this is that, the next time you call any of these methods, that data may be overwritten - so if you are creating a target that you plan to use for a longer duration of time, `RpcTargetUse.Persistent` will tell the code to allocate a new one for you that it will never overwrite.

In general, in cases where you are using `RpcTargetUse.Temp` with the group functions, it's recommended to also use the `NativeArray` or `NativeList` versions. `NativeArray` and `NativeList` can be allocated with `Allocator.Temp` very efficiently with no pressure on the garbage collector; on the other hand, the `ulong[]` and `IEnumerable<ulong>` will add garbage collection pressure (even if the `IEnumerable<ulong>` version is invoked using a `struct` type, it will still cause a boxing allocation), so it's recommended to avoid using those frequently.

#### Completing the ping example

As noted in the comments above, the earlier [ping/pong example](#invoking-an-rpc) sends its pong to all connected clients instead of just the one that requested it. To complete this example, we can use `RpcParameters` along with `SendTo.SpecifiedInParams` to make the example work as you might expect a ping to work:

```csharp
[Rpc(SendTo.Server)]
public void PingRpc(int pingCount, RpcParams rpcParams)
{
    // Here we use RpcParams for incoming purposes - fetching the sender ID the RPC came from
    // That sender ID can be passed in to the PongRpc to send this back to that client and ONLY that client

    PongRpc(pingCount, "PONG!", RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
}

[Rpc(SendTo.SpecifiedInParams)]
void PongRpc(int pingCount, string message, RpcParams rpcParams)
{
    // We do not use rpcParams within this method's body, but that is okay!
    // The params passed in are used by the generated code to ensure that this sends only
    // to the one client it should go to

    Debug.Log($"Received pong from server for ping {pingCount} and message {message}");
}

void Update()
{
    if (IsClient && Input.GetKeyDown(KeyCode.P))
    {
        PingRpc();
    }
}
```

## Other RPC parameters

There are a few other parameters that can be passed to either the `Rpc` attribute at compile-time or the `RpcSendParams` object at runtime.

### `RpcAttribute` parameters

| Parameter               | Description                                                  |
| ----------------------- | ------------------------------------------------------------ |
| `Delivery`            | Controls whether the delivery is reliable (default) or unreliable.<br /><br />Options: `RpcDelivery.Reliable` or `RpcDelivery.Unreliable`<br />Default: `RpcDelivery.Reliable` |
| `RequireOwnership`    | If `true`, this RPC throws an exception if invoked by a player that does not own the object. This is in effect for server-to-client, client-to-server, and client-to-client RPCs - i.e., a server-to-client RPC will still fail if the server is not the object's owner.<br /><br />Default: `false` |
| `DeferLocal`          | If `true`, RPCs that execute locally will be deferred until the start of the next frame, as if they had been sent over the network. (They will not actually be sent over the network, but will be treated as if they were.) This is useful for mutually recursive RPCs on hosts, where sending back and forth between the server and the "host client" will cause a stack overflow if each RPC is executed instantly; simulating the flow of RPCs between remote client and server enables this flow to work the same in both contexts.<br /><br />Default: `false` |
| `AllowTargetOverride` | By default, any `SendTo` value other than `SendTo.SpecifiedInParams` is a hard-coded value that cannot be changed. Setting this to `true` allows you to provide an alternate target at runtime, while using the `SendTo` value as a fallback if no runtime value is provided. |

### `RpcSendParams` parameters

| Parameter          | Description                                                  |
| ------------------ | ------------------------------------------------------------ |
| `Target`         | Runtime override destination for the RPC. (See above for more details.) Populating this value will throw an exception unless either the `SendTo` value for the RPC is `SendTo.SpecifiedInParams`, or `AllowTargetOverride` is `true`.<br /><br />Default: `null` |
| `LocalDeferMode` | Overrides the `DeferLocal` value. `DeferLocalMode.Defer` causes this particular invocation of this RPC to be deferred until the next frame even if `DeferLocal` is `false`, while `DeferLocalMode.SendImmediate` causes the RPC to be executed immediately on the local machine even if `DeferLocal` is `true`. `DeferLocalMode.Default` does whatever the `DeferLocal` value in the attribute is configured to do.<br /><br />Options: `DeferLocalMode.Default`, `DeferLocalMode.Defer`, `DeferLocalMode.SendImmediate`<br />Default: `DeferLocalMode.Default` |

## Additional resources

* [RPC parameters](rpc-params.md)
* [Boss Room RPC Examples](../../learn/bossroom/bossroom-actions.md)
