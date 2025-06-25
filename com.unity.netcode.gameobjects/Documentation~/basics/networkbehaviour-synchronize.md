# NetworkBehaviour synchronization

[`NetworkBehaviour`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.html) is an abstract class that derives from [`MonoBehaviour`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html) and is primarily used to create unique netcode or game logic. To replicate any netcode-aware properties or send and receive RPCs, a [GameObject](https://docs.unity3d.com/Manual/GameObjects.html) must have a [NetworkObject](networkobject.md) component and at least one `NetworkBehaviour` component.

You can use `NetworkBehaviour`s to synchronize settings before, during, and after spawning NetworkObjects.

For more information about spawning and despawning `NetworkBehaviour`s, refer to the [NetworkBehaviour spawning and despawning page](networkbehaviour.md).

## Pre-spawn, spawn, post-spawn and synchronization

The NetworkObject spawn process can become complicated when there are multiple `NetworkBehaviour` components attached to the same GameObject. Additionally, there can be times where you want to be able to handle pre- and post-spawn oriented tasks.

- Pre-spawn example: Instantiating a `NetworkVariable` with owner write permissions and assigning a value to that `NetworkVariable` on the server or host side.
- Spawn example: Applying a local value or setting that may be used during post spawn by another local `NetworkBehaviour` component.
- Post-spawn example: Accessing a `NetworkVariable` or other property that is set during the spawn process.

Below are the three virtual methods you can override within a `NetworkBehaviour`-derived class:

Method                       | Scope                    | Use case                                               | Context
---------------------------- | ------------------------ | ------------------------------------------------------ | -------------
OnNetworkPreSpawn            | NetworkObject            | Pre-spawn initialization                               | Client and server
OnNetworkSpawn               | NetworkObject            | During spawn initialization                            | Client and server
OnNetworkPostSpawn           | NetworkObject            | Post-spawn actions                                     | Client and server
OnNetworkSessionSynchronized | All NetworkObjects       | New client finished synchronizing                      | Client-side only
OnInSceneObjectsSpawned      | In-scene NetworkObjects  | New client finished synchronizing or a scene is loaded | Client and server

In addition to the methods above, there are two special case convenience methods:

- `OnNetworkSessionSynchronized`: When scene management is enabled and a new client joins a session, the client starts synchronizing with the network session. During this period of time the client might need to load additional scenes as well as instantiate and spawn NetworkObjects. When a client has finished loading all scenes and all NetworkObjects are spawned, this method gets invoked on all `NetworkBehaviour`s associated with any spawned NetworkObjects. This can be useful if you want to write scripts that might require access to other spawned NetworkObjects and/or their `NetworkBehaviour` components. When this method is invoked, you are assured everything is spawned and ready to be accessed and/or to have messages sent from them. Remember that this is invoked on clients and is not invoked on a server or host.
- `OnInSceneObjectsSpawned`: Sometimes you might want to have the same kind of assurance that any in-scene placed NetworkObjects have been spawned prior to a specific set of scripts being invoked. This method is invoked on in-scene placed NetworkObjects when:
    - A server or host first starts up after all in-scene placed NetworkObjects in the currently loaded scene(s) have been spawned.
    - A client finishes synchronizing.
    - On the server and client side after a scene has been loaded and all newly instantiated in-scene placed NetworkObjects have been spawned.

### Pre-spawn synchronization with `OnSynchronize`

There can be scenarios where you need to include additional configuration data or use a `NetworkBehaviour` to configure some non-netcode related component (or the like) before a `NetworkObject` is spawned. This can be particularly critical if you want specific settings applied before `NetworkBehaviour.OnNetworkSpawn` is invoked. When a client is synchronizing with an existing network session, this can become problematic as messaging requires a client to be fully synchronized before you know "it is safe" to send the message, and even if you send a message there is the latency involved in the whole process that might not be convenient and can require additional specialized code to account for this.

`NetworkBehaviour.OnSynchronize` allows you to write and read custom serialized data during the NetworkObject serialization process.  

There are two cases where NetworkObject synchronization occurs:

- When dynamically spawning a NetworkObject.
- When a client is being synchronized after connection approval
  - that is, Full synchronization of the NetworkObjects and scenes.

> [!NOTE]
> If you aren't familiar with the [`INetworkSerializable` interface](../advanced-topics/serialization/inetworkserializable.md), then you might read up on that before proceeding, because `NetworkBehaviour.OnSynchronize` follows a similar usage pattern.

#### Order of operations when dynamically spawning

The following provides you with an outline of the order of operations that occur during NetworkObject serialization when dynamically spawned.

Server-side:

- `GameObject` with `NetworkObject` component is instantiated.
- The `NetworkObject` is spawned.
  - For each associated `NetworkBehaviour` component, `NetworkBehaviour.OnNetworkSpawn` is invoked.
- The `CreateObjectMessage` is generated.
  - `NetworkObject` state is serialized.
  - `NetworkVariable` state is serialized.
  - `NetworkBehaviour.OnSynchronize` is invoked for each `NetworkBehaviour` component.
    - If this method isn't overridden then nothing is written to the serialization buffer.
- The `CreateObjectMessage` is sent to all clients that are observers of the `NetworkObject`.


Client-side:
- The `CreateObjectMessage` is received.
  - `GameObject` with `NetworkObject` component is instantiated.
  - `NetworkVariable` state is deserialized and applied.
  - `NetworkBehaviour.OnSynchronize` is invoked for each `NetworkBehaviour` component.
    - If this method isn't overridden then nothing is read from the serialization buffer.
- The `NetworkObject` is spawned.
  - For each associated `NetworkBehaviour` component, `NetworkBehaviour.OnNetworkSpawn` is invoked.

#### Order of operations during full (late-join) client synchronization

Server-side:
- The `SceneEventMessage` of type `SceneEventType.Synchronize` is created.
  - All spawned `NetworkObjects` that are visible to the client, already instantiated, and spawned are serialized.
    - `NetworkObject` state is serialized.
    - `NetworkVariable` state is serialized.
    - `NetworkBehaviour.OnSynchronize` is invoked for each `NetworkBehaviour` component.
      - If this method isn't overridden then nothing is written to the serialization buffer.
- The `SceneEventMessage` is sent to the client.

Client-side:
- The `SceneEventMessage` of type `SceneEventType.Synchronize` is received.
- Scene information is deserialized and scenes are loaded (if not already).
  - In-scene placed `NetworkObject`s are instantiated when a scene is loaded.
- All `NetworkObject` oriented synchronization information is deserialized.
  - Dynamically spawned `NetworkObject`s are instantiated and state is synchronized.
  - For each `NetworkObject` instance:
    - `NetworkVariable` state is deserialized and applied.
    - `NetworkBehaviour.OnSynchronize` is invoked.
      - If this method isn't overridden then nothing is read from the serialization buffer.
    - The `NetworkObject` is spawned.
      - For each associated `NetworkBehaviour` component, `NetworkBehaviour.OnNetworkSpawn` is invoked.

### `OnSynchronize` example

Now that you understand the general concept behind `NetworkBehaviour.OnSynchronize`, the question you might have is "when would you use such a thing"? `NetworkVariable`s can be useful to synchronize state, but they also are only updated every network tick and you might have some form of state that needs to be updated when it happens and not several frames later, so you decide to use RPCs. However, this becomes an issue when you want to synchronize late-joining clients as there is no way to synchronize late-joining clients based on RPC activity over the duration of a network session. This is one of many possible reasons one might want to use `NetworkBehaviour.OnSynchronize`.

The following example uses `NetworkBehaviour.OnSynchronize` to synchronize connecting (to-be-synchronized) clients and also uses an RPC to synchronize changes in state for already synchronized and connected clients:

```csharp
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Simple RPC driven state that shows one
/// form of NetworkBehaviour.OnSynchronize usage
/// </summary>
public class SimpleRpcState : NetworkBehaviour
{
    private bool m_ToggleState;

    /// <summary>
    /// Late joining clients will be synchronized
    /// to the most current m_ToggleState
    /// </summary>
    protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer)
    {
        serializer.SerializeValue(ref m_ToggleState);
        base.OnSynchronize(ref serializer);
    }

    public void ToggleState(bool stateIsSet)
    {
        m_ToggleState = stateIsSet;
    }

    /// <summary>
    /// Synchronizes connected clients with the
    /// server-side m_ToggleState
    /// </summary>
    /// <param name="stateIsSet"></param>
    [Rpc(SendTo.ClientsAndHost)]
    private void ToggleStateClientRpc(bool stateIsSet)
    {
        m_ToggleState = stateIsSet;
    }
}
```
> [!NOTE]
> `NetworkBehaviour.OnSynchronize` is only invoked on the server side during the write part of serialization and only invoked on the client side during the read part of serialization. When running a host, `NetworkBehaviour.OnSynchronize` is still only invoked once (server-side) during the write part of serialization.

### Debugging `OnSynchronize` serialization

If your serialization code has a bug and throws an exception, then `NetworkBehaviour.OnSynchronize` has additional safety checking to handle a graceful recovery without completely breaking the rest of the synchronization serialization pipeline.  

#### When writing

If user-code throws an exception during `NetworkBehaviour.OnSynchronize`, it catches the exception and if:
- **LogLevel = Normal**: A warning message that includes the name of the `NetworkBehaviour` that threw an exception while writing will be logged and that part of the serialization for the given `NetworkBehaviour` is skipped.
- **LogLevel = Developer**: It provides the same warning message as well as it logs an error with the exception message and stack trace.

After generating the log message(s), it rewinds the serialization stream to the point just before it invoked `NetworkBehaviour.OnSynchronize` and will continue serializing. Any data written before the exception occurred will be overwritten or dropped depending upon whether there are more `NetworkBehaviour` components to be serialized.

#### When reading

For exceptions this follows the exact same message logging pattern described above when writing. The distinct difference is that after it logs one or more messages to the console, it skips over only the serialization data written by the server-side when `NetworkBehaviour.OnSynchronize` was invoked and continues the deserialization process for any remaining `NetworkBehaviour` components.

However, there is an additional check to assure that the total expected bytes to read were actually read from the buffer. If the total number of bytes read does not equal the expected number of bytes to be read it will log a warning that includes the name of the `NetworkBehaviour` in question, the total bytes read, the expected bytes to be read, and lets you know this `NetworkBehaviour` is being skipped.

> [!NOTE]
> When using `NetworkBehaviour.OnSynchronize` you should be aware that you are increasing the synchronization payload size per instance. If you have 30 instances that each write 100 bytes of information you will have increased the total full client synchronization size by 3000 bytes.

## Serializing `NetworkBehaviour`s

`NetworkBehaviour`s require the use of specialized [`NetworkBehaviourReference`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviourReference.html) structures to be serialized and used with RPCs and `NetworkVariable`s.
