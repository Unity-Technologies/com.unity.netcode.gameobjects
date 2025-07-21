# RPC params

You can configure the `Rpc`,  `ServerRpc` and `ClientRpc` methods in the following ways:
* Use the `[Rpc]`, `[ServerRpc]` and `[ClientRpc]` attributes at compile-time.
* Use `ServerRpcParams` and `ClientRpcParams` at runtime.

Developers can put, `RpcParams`, `ServerRpcParams`, or `ClientRpcParams` as the last parameter (optionally, as appropriate for the type of RPC). The `RpcParams`, `ServerRpcParams`, and `ClientRpcParams` types contain both send and receive properties that are populated relative to the context of usage.


## Rpc Params

See the following for `Rpc` params:

```csharp
// Both Rpc methods below are fine, `ServerRpcParams` is completely optional

[Rpc(SendTo.Server)]
void AbcdServerRpc(int somenumber) { /* ... */ }

[Rpc(SendTo.Server)]
void XyzwServerRpc(int somenumber, RpcParams rpcParams = default) { /* ... */ }
```

For those accustomed to the legacy `ServerRpc` and `ClientRpc` parameters, there are a few differences with `Rpc` params:

- `RpcParams` defines implicit conversion operators for `BaseRpcTarget`  and `LocalDeferMode`:
  - `BaseRpcTarget`: Used to change the destination of an RPC at runtime.
  - `LocalDeferMode`: Used to change how an RPC is handled locally.
    - `Defer`: Invokes the RPC locally on the next frame.
    - `SendImmediate` (default): Immediately invokes the RPC (at the current call stack) locally.

- Changing destination of an RPC is done using the properties and methods of `RpcTarget` (each `NetworkBehaviour` contains a reference to the shared `RpcTarget` object, as does `NetworkManager`.) This allows conveniently selecting various common targets (Server, NotServer, Owner, NotOwner, etc), as well as custom client lists using `RpcTarget.Single()` (to send to one client ID), `RpcTarget.Group()` (to send to multiple client IDs),  and`RpcTarget.Not()` (to send to everyone except for the specified client ID or list of client IDs)
- `RpcParams` do not allow overriding the destination at runtime unless either the default target is `SendTo.SpecifiedInParams` or `AllowTargetOverride = true` is passed to the attribute.

```csharp
[Rpc(SendTo.Server)]
void ServerRpc(RpcParams rpcParams = default) { /* ... */ }

[Rpc(SendTo.SpecifiedInParams)]
void RuntimeSpecifiedRpc(RpcParams rpcParams = default) { /* ... */ }

[Rpc(SendTo.Server, AllowTargetOverride = true)]
void OverridableServerRpc(RpcParams rpcParams = default) { /* ... */ }

// Throws an exception: SendTo.Server cannot be overriden for ServerRpc
ServerRpc(RpcTarget.NotServer);
ServerRpc(RpcTarget.Single(someClientId, RpcTargetUse.Temp));

// Works
RuntimeSpecifiedRpc(RpcTarget.NotServer);
RuntimeSpecifiedRpc(RpcTarget.Single(someClientId, RpcTargetUse.Temp));

// Works
OverridableServerRpc(RpcTarget.NotServer);
OverridableServerRpc(RpcTarget.Single(someClientId, RpcTargetUse.Temp));
```

> [!NOTE]
> For various reasons, `BaseRpcTarget` is not intended to be extended by user code. If you have a custom use case that requires a special target, we recommend creating a wrapper method that returns `RpcTarget.Single()` or `RpcTarget.Group()`, which should be able to satisfy most if not all of your needs.

## ServerRpc Params

> [!NOTE]
> ClientRpc and ServerRpc are legacy features of Netcode for GameObjects, and have been supplanted by the universal Rpc attribute. This documentation is provided for legacy use, but we recommend all current projects use [Rpc](rpc.md) instead.

Refer to the following for `ServerRpc` params:

``` csharp
// Both ServerRpc methods below are fine, `ServerRpcParams` is completely optional

[ServerRpc]
void AbcdServerRpc(int somenumber) { /* ... */ }

[ServerRpc]
void XyzwServerRpc(int somenumber, ServerRpcParams serverRpcParams = default) { /* ... */ }
```

[ServerRpcParams Documentation](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.ServerRpcParams.html)

## ClientRpc Params

> [!NOTE]
> ClientRpc and ServerRpc are legacy features of Netcode for GameObjects, and have been supplanted by the universal Rpc attribute. This documentation is provided for legacy use, but we recommend all current projects use [Rpc](rpc.md) instead.

See the following for `ClientRpc` params:

```csharp
// Both ClientRpc methods below are fine, `ClientRpcParams` is completely optional

[ClientRpc]
void AbcdClientRpc(int framekey) { /* ... */ }

[ClientRpc]
void XyzwClientRpc(int framekey, ClientRpcParams clientRpcParams = default) { /* ... */ }
```

[ClientRpcParams Documentation](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.ClientRpcParams.html)

> [!NOTE]
> `ClientRpcSendParams`'s `TargetClientIds` property is a `ulong[]` which means everytime you try to specify a subset of target clients or even a single client target, you will have to allocate a `new ulong[]`. This pattern can quickly lead into lots of heap allocations and pressure GC which would cause GC spikes at runtime. We suggest developers cache their `ulong[]` variables or use an array pool to cycle `ulong[]` instances so that it would cause less heap allocations.
