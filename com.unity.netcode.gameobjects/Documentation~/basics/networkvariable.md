# NetworkVariables

`NetworkVariable`s are a way of synchronizing properties between servers and clients in a persistent manner, unlike [RPCs](../advanced-topics/message-system/rpc.md) and [custom messages](../advanced-topics/message-system/custom-messages.md), which are one-off, point-in-time communications that aren't shared with any clients not connected at the time of sending.

`NetworkVariable` is a wrapper of the stored value of type `T`, so you need to use the `NetworkVariable.Value` property to access the actual value being synchronized. A `NetworkVariable` is synchronized with:

* New clients joining the game (also referred to as late-joining clients)
    * When the associated `NetworkObject` of a `NetworkBehaviour` with `NetworkVariable` properties is spawned, any `NetworkVariable`'s current state (`Value`) is automatically synchronized on the client side.
* Connected clients
    * When a `NetworkVariable` value changes, all connected clients that subscribed to the `NetworkVariable.OnValueChanged` event (prior to the value being changed) are notified of the change. Two parameters are passed to any `NetworkVariable.OnValueChanged` subscribed callback method:
        - First parameter (Previous): The previous value before the value was changed
        - Second parameter (Current): The newly changed `NetworkVariable.Value`

You can use `NetworkVariable` [permissions](#permissions) to control read and write access to `NetworkVariable`s. You can also create [custom `NetworkVariable`s](custom-networkvariables.md).

## Setting up NetworkVariables

### Defining a NetworkVariable

- A `NetworkVariable` property must be defined within a `NetworkBehaviour`-derived class attached to a `GameObject`.
    - The `GameObject` or a parent `GameObject` must also have a `NetworkObject` component attached to it.
- A `NetworkVariable`'s value can only be set:
    - When initializing the property (either when it's declared or within the Awake method).
    - While the associated `NetworkObject` is spawned (upon being spawned or any time while it's still spawned).

###  Initializing a NetworkVariable

When a client first connects, it's synchronized with the current value of the `NetworkVariable`. Typically, clients should register for `NetworkVariable.OnValueChanged` within the `OnNetworkSpawn` method. A `NetworkBehaviour`'s `Start` and `OnNetworkSpawn` methods are invoked based on the type of `NetworkObject` the `NetworkBehaviour` is associated with:   

- In-scene placed: Since the instantiation occurs via the scene loading mechanism(s), the `Start` method is invoked before `OnNetworkSpawn`.
- Dynamically spawned: Since `OnNetworkSpawn` is invoked immediately (that is, within the same relative call-stack) after instantiation, the `Start` method is invoked after `OnNetworkSpawn`.  

Typically, these methods are invoked at least one frame after the `NetworkObject` and associated `NetworkBehaviour` components are instantiated. The table below lists the event order for dynamically spawned and in-scene placed objects respectively.

Dynamically spawned | In-scene placed
------------------- | ---------------
Awake               | Awake
OnNetworkSpawn      | Start
Start               | OnNetworkSpawn

You should only set the value of a `NetworkVariable` when first initializing it or if it's spawned. It isn't recommended to set a `NetworkVariable` when the associated `NetworkObject` isn't spawned.

> [!NOTE]
> If you need to initialize other components or objects based on a `NetworkVariable`'s initial synchronized state, then you can use a common method that's invoked on the client side within the `NetworkVariable.OnValueChanged` callback (if assigned) and `NetworkBehaviour.OnNetworkSpawn` method.

## Supported types

`NetworkVariable` supports the following types:

* C# unmanaged [primitive types](../advanced-topics/serialization/cprimitives.md) (which are serialized by direct `memcpy` into/out of the buffer): `bool`, `byte`, `sbyte`, `char`, `decimal`, `double`, `float`, `int`, `uint`, `long`, `ulong`, `short`, and `ushort`.
* Unity unmanaged [built-in types](../advanced-topics/serialization/unity-primitives.md) (which are serialized by direct `memcpy` into/out of the buffer): `Vector2`, `Vector3`, `Vector2Int`, `Vector3Int`, `Vector4`, `Quaternion`, `Color`, `Color32`, `Ray`, `Ray2D`.
* Any [`enum`](../advanced-topics/serialization/enum-types.md) types (which are serialized by direct `memcpy` into/out of the buffer).
* Any type (managed or unmanaged) that implements [`INetworkSerializable`](../advanced-topics/serialization/inetworkserializable.md) (which are serialized by calling their `NetworkSerialize` method). **On the reading side, these values are deserialized in-place, meaning the existing instance is reused and any non-serialized values are left in their current state.**
* Any unmanaged struct type that implements [`INetworkSerializeByMemcpy`](../advanced-topics/serialization/inetworkserializebymemcpy.md) (which are serialized by direct `memcpy `of the entire struct into/out of the buffer).
* Unity [fixed string](../advanced-topics/serialization/fixedstrings.md) types: `FixedString32Bytes`, `FixedString64Bytes`, `FixedString128Bytes`, `FixedString512Bytes`, and `FixedString4096Bytes` (which are serialized intelligently, only sending the used part across the network and adjusting the length of the string on the other side to fit the received data).

For any types that don't fit within this list, including managed types and unmanaged types with pointers: you can provide delegates informing the serialization system of how to serialize and deserialize your values. For more information, refer to [Custom Serialization](../advanced-topics/custom-serialization.md). A limitation of custom serialization is that, unlike `INetworkSerializable` types, types using custom serialization aren't able to be read in-place, so managed types will, by necessity, incur a garbage collection allocation (which can cause performance issues) on every update.

### Managing garbage collection

Although `NetworkVariable` supports both managed and [unmanaged](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters#unmanaged-constraint) types, managed types come with additional overhead.

Netcode for GameObjects has been designed to minimize garbage collection allocations for managed `INetworkSerializable` types (for example, a new value is only allocated if the value changes from `null` to non-`null`). However, the ability of a type to be `null` adds additional overhead both in logic (checking for nulls before serializing) and bandwidth (every serialization carries an additional byte indicating whether the value is `null`).

Additionally, any type that has a managed type is itself a managed type - so a struct that has `int[]` is a managed type because `int[]` is a managed type.

Finally, while managed `INetworkSerializable` types are serialized in-place (and thus don't incur allocations for simple value updates), C# arrays and managed types serialized through custom serialization are **not** serialized in-place, and will incur an allocation on every update.

### Using collections with `NetworkVariable`s

You can use `NetworkVariable`s with both managed and unmanaged collections, but you need to call `NetworkVariable<T>.CheckDirtyState()` after making changes to a collection (or items in a collection) for those changes to be detected. Then the [`OnValueChanged`](#onvaluechanged-example) event will trigger, if subscribed locally, and by the end of the frame the rest of the clients and server will be synchronized with the detected change(s).

`NetworkVariable<T>.CheckDirtyState()` checks every item in a collection, including recursively nested collections, which can have a significant impact on performance if collections are large. If you're making multiple changes to a collection, you only need to call `NetworkVariable<T>.CheckDirtyState()` once after all changes are complete, rather than calling it after each change.

## Synchronization and notification example

The following client-server example shows how the initial `NetworkVariable` synchronization has already occurred by the time `OnNetworkSpawn` is invoked. It also shows how subscribing to `NetworkVariable.OnValueChanged` within `OnNetworkSpawn` provides notifications for any changes to `m_SomeValue.Value` that occur.

1. The server initializes the `NetworkVariable` when the associated `NetworkObject` is spawned.
1. The client confirms that the `NetworkVariable` is synchronized to the initial value set by the server and assigns a callback method to `NetworkVariable.OnValueChanged`.
1. Once spawned, the client is notified of any changes made to the `NetworkVariable`.

> [!NOTE]
> This example only tests the initial client synchronization of the value and when the value changes. If a second client joins, it will throw a warning about the `NetworkVariable.Value` not being the initial value. This example is intended for use with a single server or host and a single client.

 ```csharp
public class TestNetworkVariableSynchronization : NetworkBehaviour
{
    private NetworkVariable<int> m_SomeValue = new NetworkVariable<int>();
    private const int k_InitialValue = 1111;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            m_SomeValue.Value = k_InitialValue;
            NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }
        else
        {
            if (m_SomeValue.Value != k_InitialValue)
            {
                Debug.LogWarning($"NetworkVariable was {m_SomeValue.Value} upon being spawned" +
                    $" when it should have been {k_InitialValue}");
            }
            else
            {
                Debug.Log($"NetworkVariable is {m_SomeValue.Value} when spawned.");
            }
            m_SomeValue.OnValueChanged += OnSomeValueChanged;
        }
    }

    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        StartCoroutine(StartChangingNetworkVariable());
    }

    private void OnSomeValueChanged(int previous, int current)
    {
        Debug.Log($"Detected NetworkVariable Change: Previous: {previous} | Current: {current}");
    }

    private IEnumerator StartChangingNetworkVariable()
    {
        var count = 0;
        var updateFrequency = new WaitForSeconds(0.5f);
        while (count < 4)
        {
            m_SomeValue.Value += m_SomeValue.Value;
            yield return updateFrequency;
        }
        NetworkManager.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
    }
}
 ```

If you attach the above script to an in-scene placed `NetworkObject`, make a standalone build, run the standalone build as a host, and then connect to that host by entering Play Mode in the Editor, you can see (in the console output):
- The client side `NetworkVariable` value is the same as the server when `NetworkBehaviour.OnNetworkSpawn` is invoked.  
- The client detects any changes made to the `NetworkVariable` after the in-scene placed `NetworkObject` is spawned.  

This works the same way with dynamically spawned `NetworkObject`s.

## OnValueChanged example

The [synchronization and notification example](#synchronization-and-notification-example) highlights the differences between synchronizing a `NetworkVariable` with newly-joining clients and notifying connected clients when a `NetworkVariable` changes, but it doesn't provide any concrete example usage.

The `OnValueChanged` example shows a simple server-authoritative `NetworkVariable` being used to track the state of a door (that is, open or closed) using a non-ownership-based server RPC. With `RequireOwnership = false` any client can notify the server that it's performing an action on the door. Each time the door is used by a client, the `Door.ToggleServerRpc` is invoked and the server-side toggles the state of the door. When the `Door.State.Value` changes, all connected clients are synchronized to the (new) current `Value` and the `OnStateChanged` method is invoked locally on each client.

```csharp
public class Door : NetworkBehaviour
{
    public NetworkVariable<bool> State = new NetworkVariable<bool>();

    public override void OnNetworkSpawn()
    {
        State.OnValueChanged += OnStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        State.OnValueChanged -= OnStateChanged;
    }

    public void OnStateChanged(bool previous, bool current)
    {
        // note: `State.Value` will be equal to `current` here
        if (State.Value)
        {
            // door is open:
            //  - rotate door transform
            //  - play animations, sound etc.
        }
        else
        {
            // door is closed:
            //  - rotate door transform
            //  - play animations, sound etc.
        }
    }

    [Rpc(SendTo.Server)]
    public void ToggleServerRpc()
    {
        // this will cause a replication over the network
        // and ultimately invoke `OnValueChanged` on receivers
        State.Value = !State.Value;
    }
}
```

## Permissions

The `NetworkVariable` constructor can take up to three parameters:

```csharp
public NetworkVariable(T value = default,
NetworkVariableReadPermission readPerm = NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission writePerm = NetworkVariableWritePermission.Server);
```

By default, in a [client-server](../terms-concepts/client-server.md) context, the default permissions are:

- **Server**: Has read and write permissions
- **Clients:** Have read-only permissions.

The two types of permissions are defined within [NetworkVariablePermissions.cs](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/blob/release/1.0.0/com.unity.netcode.gameobjects/Runtime/NetworkVariable/NetworkVariablePermission.cs):

```csharp
    /// <summary>
    /// The permission types for reading a var
    /// </summary>
    public enum NetworkVariableReadPermission
    {
        /// <summary>
        /// Everyone can read
        /// </summary>
        Everyone,
        /// <summary>
        /// Only the owner and the server can read
        /// </summary>
        Owner,
    }

    /// <summary>
    ///  The permission types for writing a var
    /// </summary>
    public enum NetworkVariableWritePermission
    {
        /// <summary>
        /// Only the server can write
        /// </summary>
        Server,
        /// <summary>
        /// Only the owner can write
        /// </summary>
        Owner
    }
```
> [!NOTE]
> Owner writer permissions are owner-only.

### Read permissions

There are two options for reading a `NetworkVariable.Value`:

- **Everyone(default)**: both owners and non-owners of the `NetworkObject` can read the value.
    - This is useful for global states that all clients should be aware of, such as player scores, health, or any other state that all clients should know about.
    - We provided an example of maintaining a door's open or closed state using the everyone permission.
- **Owner**: only the owner of the `NetworkObject` and the server can read the value.
    - This is useful if your `NetworkVariable` represents something specific to the client's player that only the server and client should know about, such as a player's inventory or ammo count.

### Write permissions

There are two options for writing a `NetworkVariable.Value`:

- **Server(default)**: the server is the only one that can write the value.
    - This is useful for server-side specific states that all clients should be aware of but can't change, such as an NPC's status or some global world environment state (that is, is it night or day time).
- **Owner**: only the owner of the `NetworkObject` can write to the value.
    - This is useful if your `NetworkVariable` represents something specific to the client's player that only the owning client should be able to set, such as a player's skin or other cosmetics.

### Permissions example

The following example has a few different permissions configurations that you might use in a game while keeping track of a player's state. It also provides details of each `NetworkVariable`'s purpose and the reasoning behind each `NetworkVariable`'s read and write permission settings.

```csharp
public class PlayerState : NetworkBehaviour
{
    private const float k_DefaultHealth = 100.0f;
    /// <summary>
    /// Default Permissions: Everyone can read, server can only write
    /// Player health is typically something determined (updated/written to) on the server
    ///  side, but a value everyone should be synchronized with (that is, read permissions).
    /// </summary>
    public NetworkVariable<float> Health = new NetworkVariable<float>(k_DefaultHealth);

    /// <summary>
    /// Owner Read Permissions: Owner or server can read
    /// Owner Write Permissions: Only the Owner can write
    /// A player's ammo count is something that you might want, for convenience sake, the
    /// client-side to update locally. This might be because you are trying to reduce
    /// bandwidth consumption for the server and all non-owners/ players or you might be
    /// trying to incorporate a more client-side "hack resistant" design where non-owners
    /// are never synchronized with this value.
    /// </summary>
    public NetworkVariable<int> AmmoCount = new NetworkVariable<int>(default,
        NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Owner Write & Everyone Read Permissions:
    /// A player's model's skin selection index. You might have the option to allow players
    /// to select different skin materials as a way to further encourage a player's personal
    /// association with their player character.  It would make sense to make the permissions
    /// setting of the NetworkVariable such that the client can change the value, but everyone
    /// will be notified when it changes to visually reflect the new skin selection.
    /// </summary>
    public NetworkVariable<int> SkinSelectionIndex = new NetworkVariable<int>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Owner Read & Server Write Permissions:
    /// You might incorporate some form of reconnection logic that stores a player's state on
    /// the server side and can be used by the client to reconnect a player if disconnected
    /// unexpectedly.  In order for the client to let the server know it's the "same client"
    /// you might have implemented a keyed array (that is, Hashtable, Dictionary, etc, ) to track
    /// each connected client. The key value for each connected client would only be written to
    /// the each client's PlayerState.ReconnectionKey. Under this scenario, you only want the
    /// server to have write permissions and the owner (client) to be synchronized with this
    /// value (via owner only read permissions).
    /// </summary>
    public NetworkVariable<ulong> ReconnectionKey = new NetworkVariable<ulong>(default,
    NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
}
```

> [!NOTE]
> You might be wondering about our earlier door example and why we chose to use a server RPC for clients to notify the server that the door's open/closed state has changed. In that scenario, the owner of the door will most likely be owned by the server, just like non-player characters will almost always be owned by the server. In a server-owned scenario, using an RPC to handle updating a `NetworkVariable` is the proper choice above permissions for most cases.

## Complex types

Almost all of the examples on this page have been focused around numeric [value types](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-types). Netcode for GameObjects also supports complex types and can support both unmanaged types *and* managed types (although avoiding managed types where possible will improve your game's performance).

### Synchronizing complex types example

This example extends the previous `PlayerState` class to include some complex types to handle a weapon boosting game play mechanic. The example uses two complex values types:

- `WeaponBooster`: A power-up weapon booster that can only be applied by the client. This is a simple example of a complex type.
- `AreaWeaponBooster`: A second kind of weapon booster power-up that players can deploy at a specific location, and any team members within the radius of the `AreaWeaponBooster` have the weapon booster applied. This is an example of a nested complex type.

`WeaponBooster` only needs one `NetworkVariable` to handle synchronizing everyone with any currently active player-local `WeaponBooster`. However, the `AreaWeaponBooster` must consider what happens if there are eight team members that can, at any given moment, deploy an `AreaWeaponBooster`. It requires, at a minimum, a list of all deployed and currently active `AreaWeaponBooster`s.  This task uses a `NetworkList` instead of a `NetworkVariable`.

Below are the `PlayerState` additions along with the `WeaponBooster` structure (complex type):

```csharp
public class PlayerState : NetworkBehaviour
{
    // ^^^^^^^ including all code from previous example ^^^^^^^

    // The weapon booster currently applied to a player
    private NetworkVariable<WeaponBooster> PlayerWeaponBooster = new NetworkVariable<WeaponBooster>();

    /// <summary>
    /// A list of team members active "area weapon boosters" that can be applied if the local player
    /// is within their range.
    /// </summary>
    private NetworkList<AreaWeaponBooster> TeamAreaWeaponBoosters;

    void Awake()
    {
        //NetworkList can't be initialized at declaration time like NetworkVariable. It must be initialized in Awake instead.
        //If you do initialize at declaration, you will run into Memmory leak errors.
        TeamAreaWeaponBoosters = new NetworkList<AreaWeaponBooster>();
    }

    void Start()
    {
        /*At this point, the object hasn't been network spawned yet, so you're not allowed to edit network variables! */
        //list.Add(new AreaWeaponBooster());
    }

    void Update()
    {
        //This is just an example that shows how to add an element to the list after its initialization:
        if (!IsServer) { return; } //remember: only the server can edit the list
        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            TeamAreaWeaponBoosters.Add(new AreaWeaponBooster()));
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsClient)
        {
            TeamAreaWeaponBoosters.OnListChanged += OnClientListChanged;
        }
        if (IsServer)
        {
            TeamAreaWeaponBoosters.OnListChanged += OnServerListChanged;
            TeamAreaWeaponBoosters.Add(new AreaWeaponBooster()); //if you want to initialize the list with some default values, this is a good time to do so.
        }
    }

    void OnServerListChanged(NetworkListEvent<AreaWeaponBooster> changeEvent)
    {
        Debug.Log($"[S] The list changed and now has {TeamAreaWeaponBoosters.Count} elements");
    }

    void OnClientListChanged(NetworkListEvent<AreaWeaponBooster> changeEvent)
    {
        Debug.Log($"[C] The list changed and now has {TeamAreaWeaponBoosters.Count} elements");
    }
}

/// <summary>
/// Example: Complex Type
/// This is an example of how one might handle tracking any weapon booster currently applied
/// to a player.
/// </summary>
public struct WeaponBooster : INetworkSerializable, System.IEquatable<WeaponBooster>
{
    public float PowerAmplifier;
    public float Duration;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out PowerAmplifier);
            reader.ReadValueSafe(out Duration);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(PowerAmplifier);
            writer.WriteValueSafe(Duration);
        }
    }

    public bool Equals(WeaponBooster other)
    {
        return PowerAmplifier == other.PowerAmplifier && Duration == other.Duration;
    }
}
```

The example code above shows how a complex type implements `INetworkSerializable`. The second part of the example below shows that the `AreaWeaponBooster` includes a `WeaponBooster` property that would (for example) be applied to team members that are within the `AreaWeaponBoosters` radius:

```csharp
/// <summary>
/// Example: Nesting Complex Types
/// This example uses the previous WeaponBooster complex type to be a "container" for
/// the "weapon booster" information of an AreaWeaponBooster.  It then provides additional
/// information that would allow clients to easily determine, based on location and radius,
/// if it should add (for example) a special power up HUD symbol or special-FX to the local
/// player.
/// </summary>
public struct AreaWeaponBooster : INetworkSerializable, System.IEquatable<AreaWeaponBooster>
{
    public WeaponBooster ApplyWeaponBooster; // the nested complex type
    public float Radius;
    public Vector3 Location;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            // The complex type handles its own de-serialization
            serializer.SerializeValue(ref ApplyWeaponBooster);
            // Now de-serialize the non-complex type properties
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out Radius);
            reader.ReadValueSafe(out Location);
        }
        else
        {
            // The complex type handles its own serialization
            serializer.SerializeValue(ref ApplyWeaponBooster);
            // Now serialize the non-complex type properties
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(Radius);
            writer.WriteValueSafe(Location);
        }
    }

    public bool Equals(AreaWeaponBooster other)
    {
        return other.Equals(this) && Radius == other.Radius && Location == other.Location;
    }
}
```

Looking closely at the read and write segments of code within `AreaWeaponBooster.NetworkSerialize`, the nested complex type property `ApplyWeaponBooster` handles its own serialization and de-serialization. The `ApplyWeaponBooster`'s implemented `NetworkSerialize` method serializes and deserialized any `AreaWeaponBooster` type property. This design approach can help reduce code replication while providing a more modular foundation to build even more complex, nested types.

## Custom NetworkVariable Implementations

> [!NOTE] Disclaimer
> The `NetworkVariable` and `NetworkList` classes were created as `NetworkVariableBase` class implementation examples. While the `NetworkVariable<T>` class is considered production ready, you might run into scenarios where you have a more advanced implementation in mind. In this case, we encourage you to create your own custom implementation.

To create your own `NetworkVariableBase` derived container, you should:

- Create a class deriving from `NetworkVariableBase`.

- Assure the the following methods are overridden:
    - `void WriteField(FastBufferWriter writer)`
    - `void ReadField(FastBufferReader reader)`
    - `void WriteDelta(FastBufferWriter writer)`
    - `void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)`
- Depdending upon your custom `NetworkVariableBase` container, you might look at `NetworkVariable<T>` or `NetworkList` to see how those two examples were implemented.

<a name="network-variable-serialization"></a>

#### NetworkVariableSerialization&lt;T&gt;

The way you read and write network variables changes depending on the type you use.

* Known, non-generic types: Use `FastBufferReader.ReadValue` to read from and `FastBufferWriter.WriteValue` to write to the network variable value.
* Integer types:  This type gives you the option to use `BytePacker` and `ByteUnpacker` to compress the network variable value. This process can save bandwidth but adds CPU processing time.
* Generic types: Use serializers that Unity generates based on types discovered during a compile-time code generation process. This means you need to tell Unity's code generation algorithm which types to generate serializers for. To tell Unity which types to serialize, use the following methods:
    * Use `GenerateSerializationForTypeAttribute` to serialize hard-coded types.
    * Use `GenerateSerializationForGenericParameterAttribute` to serialize generic types.
    To learn how to use these methods, refer to [Network variable serialization](#network-variable-serialization).

##### Tell Unity to serialize a hard-coded type
The following code example uses `GenerateSerializationForTypeAttribute` to generate serialization for a specific hard-coded type:
```csharp
[GenerateSerializationForType(typeof(Foo))]
public class MyNetworkVariableTypeUsingFoo : NetworkVariableBase {}
```

You can call a type that you know the name of with the `FastBufferReader` or `FastBufferWriter` methods. These methods don't work for Generic types because the name of the type is unknown.
##### Tell Unity to serialize a generic type
The following code example uses `GenerateSerializationForGenericParameterAttribute` to generate serialization for a specific Generic parameter in your `NetworkVariable` type:
```csharp
[GenerateSerializationForGenericParameter(0)]
public class MyNetworkVariableType<T> : NetworkVariableBase {}
```

This attribute accepts an integer that indicates which parameter in the type to generate serialization for. This value is 0-indexed, which means that the first type is 0, the second type is 1, and so on.
The following code example places the attribute more than once on one class to generate serialization for multiple types, in this case,`TFirstType` and `TSecondType:

```csharp
[GenerateSerializationForGenericParameter(0)]
[GenerateSerializationForGenericParameter(1)]
public class MyNetworkVariableType<TFirstType, TSecondType> : NetworkVariableBase {}
```


The  `GenerateSerializationForGenericParameterAttribute` and `GenerateSerializationForTypeAttribute` attributes make Unity's code generation create the following methods:

```csharp
NetworkVariableSerialization<T>.Write(FastBufferWriter writer, ref T value);
NetworkVariableSerialization<T>.Read(FastBufferWriter writer, ref T value);
NetworkVariableSerialization<T>.Duplicate(in T value, ref T duplicatedValue);
NetworkVariableSerialization<T>.AreEqual(in T a, in T b);
```

For dynamically allocated types with a value that isn't `null` (for example, managed types and collections like NativeArray and NativeList) call `Read` to read the value in the existing object and write data into it directy (in-place). This avoids more allocations.

You can use `AreEqual` to determine if a value is different from the value that `Duplicate` cached. This avoids sending the same value multiple times. You can also use the previous value that `Duplicate` cached to calculate deltas to use in `ReadDelta` and `WriteDelta`.

The type you use must be serializable according to the "Supported Types" list above. Each type needs its own serializer instantiated, so this step tells the codegen which types to create serializers for.

> [!NOTE] Unity's code generator assumes that all `NetworkVariable` types exist as fields inside `NetworkBehaviour` types. This means that Unity only inspects fields inside `NetworkBehaviour` types to identify the types to create serializers for.

 ### Custom NetworkVariable Example

This example shows a custom `NetworkVariable` type to help you understand how you might implement such a type. In the current version of Netcode for GameObjects, this example is possible without using a custom `NetworkVariable` type; however, for more complex situations that aren't natively supported, this basic example should help inform you of how to approach the implementation:

Looking at the read and write segments of code within `AreaWeaponBooster.NetworkSerialize`, the nested complex type property `ApplyWeaponBooster` handles its own serialization and de-serialization. The `ApplyWeaponBooster`'s implemented `NetworkSerialize` method serializes and deserializes any `AreaWeaponBooster` type property. This design approach can help reduce code replication while providing a more modular foundation to build even more complex, nested types.

## Strings

While `NetworkVariable` does support managed `INetworkSerializable` types, strings aren't in the list of supported types. This is because strings in C# are immutable types, preventing them from being deserialized in-place, so every update to a `NetworkVariable<string>` would cause a Garbage Collected allocation to create the new string, which may lead to performance problems.

While it's technically possible to support strings using custom serialization through `UserNetworkVariableSerialization`, it isn't recommended to do so due to the performance implications that come with it. Instead, we recommend using one of the `Unity.Collections.FixedString` value types. In the below example, we used a `FixedString128Bytes` as the `NetworkVariable` value type. On the server side, it changes the string value each time you press the space bar on the server or host instance. Joining clients will be synchronized with the current value applied on the server side, and each time you hit the space bar on the server side, the client synchronizes with the changed string.

> [!NOTE]
> `NetworkVariable<T>` won't serialize the entire 128 bytes each time the `Value` is changed. Only the number of bytes that are actually used to store the string value will be sent, no matter which size of `FixedString` you use.

```csharp
public class TestFixedString : NetworkBehaviour
{
    /// Create your 128 byte fixed string NetworkVariable
    private NetworkVariable<FixedString128Bytes> m_TextString = new NetworkVariable<FixedString128Bytes>();

    private string[] m_Messages ={ "This is the first message.",
    "This is the second message (not like the first)",
    "This is the third message (but not the last)",
    "This is the fourth and last message (next will roll over to the first)"
    };

    private int m_MessageIndex = 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Assin the current value based on the current message index value
            m_TextString.Value = m_Messages[m_MessageIndex];
        }
        else
        {
            // Subscribe to the OnValueChanged event
            m_TextString.OnValueChanged += OnTextStringChanged;
            // Log the current value of the text string when the client connected
            Debug.Log($"Client-{NetworkManager.LocalClientId}'s TextString = {m_TextString.Value}");
        }
    }

    public override void OnNetworkDespawn()
    {
        m_TextString.OnValueChanged -= OnTextStringChanged;        
    }

    private void OnTextStringChanged(FixedString128Bytes previous, FixedString128Bytes current)
    {
        // Just log a notification when m_TextString changes
        Debug.Log($"Client-{NetworkManager.LocalClientId}'s TextString = {m_TextString.Value}");
    }

    private void LateUpdate()
    {
        if (!IsServer)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            m_MessageIndex++;
            m_MessageIndex %= m_Messages.Length;
            m_TextString.Value = m_Messages[m_MessageIndex];
            Debug.Log($"Server-{NetworkManager.LocalClientId}'s TextString = {m_TextString.Value}");
        }
    }
}
```


> [!NOTE]
> The above example uses a pre-set list of strings to cycle through for example purposes only.  If you have a predefined set of text strings as part of your actual design then you would not want to use a FixedString to handle synchronizing the changes to `m_TextString`.  Instead, you would want to use a `uint` for the type `T` where the `uint` was the index of the string message to apply to `m_TextString`.  
