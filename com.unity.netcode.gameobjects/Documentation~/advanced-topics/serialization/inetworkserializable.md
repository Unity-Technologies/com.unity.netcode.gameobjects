# INetworkSerializable

You can use the `INetworkSerializable` interface to define custom serializable types.

```csharp
struct MyComplexStruct : INetworkSerializable
{
    public Vector3 Position;
    public Quaternion Rotation;

    // INetworkSerializable
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Rotation);
    }
    // ~INetworkSerializable
}
```

Types implementing `INetworkSerializable` are supported by `NetworkSerializer`, `RPC` s and `NetworkVariable` s.

```csharp

[Rpc(SendTo.Server)]
void MyServerRpc(MyComplexStruct myStruct) { /* ... */ }

void Update()
{
    if (Input.GetKeyDown(KeyCode.P))
    {
        MyServerRpc(
            new MyComplexStruct
            {
                Position = transform.position,
                Rotation = transform.rotation
            }); // Client -> Server
    }
}
```

## Nested serial types

Nested serial types will be `null` unless you initialize following one of these methods:

* Manually before calling `SerializeValue` if `serializer.IsReader` (or something like that).

* Initialize in the default constructor.

This is by design. You may see the values as null until initialized. The serializer isn't deserializing them, the `null` value is applied before it can be serialized.

## Conditional Serialization

As you have more control over serialization of a struct, you might implement conditional serialization at runtime.

The following example explores a more advanced use case.

### Example: Move

```csharp

public struct MyMoveStruct : INetworkSerializable
{
    public Vector3 Position;
    public Quaternion Rotation;

    public bool SyncVelocity;
    public Vector3 LinearVelocity;
    public Vector3 AngularVelocity;

    void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Position & Rotation
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Rotation);

        // LinearVelocity & AngularVelocity
        serializer.SerializeValue(ref SyncVelocity);
        if (SyncVelocity)
        {
            serializer.SerializeValue(ref LinearVelocity);
            serializer.SerializeValue(ref AngularVelocity);
        }
    }
}
```

**Reading:**

* (De)serialize `Position` back from the stream.

* (De)serialize `Rotation` back from the stream.

* (De)serialize `SyncVelocity` back from the stream.

* Check if `SyncVelocity` is set to true, if so:

  * (De)serialize `LinearVelocity` back from the stream.

  * (De)serialize `AngularVelocity` back from the stream.

**Writing:**

* Serialize `Position` into the stream.

* Serialize `Rotation` into the stream.

* Serialize `SyncVelocity` into the stream.

* Check if `SyncVelocity` is set to true, if so:

  * Serialize `LinearVelocity` into the stream.

  * Serialize `AngularVelocity` into the stream.

* If the `SyncVelocity` flag is set to true, serialize both the `LinearVelocity` and `AngularVelocity` into the stream.

* When the `SyncVelocity` flag is set to `false`, leave `LinearVelocity` and `AngularVelocity` with default values.

### Recursive Nested Serialization

It's possible to recursively serialize nested members with `INetworkSerializable` interface down in the hierarchy tree.

Review the following example:

```csharp

public struct MyStructA : INetworkSerializable
{
    public Vector3 Position;
    public Quaternion Rotation;

    void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Rotation);
    }
}

public struct MyStructB : INetworkSerializable
{
    public int SomeNumber;
    public string SomeText;
    public MyStructA StructA;

    void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SomeNumber);
        serializer.SerializeValue(ref SomeText);
        StructA.NetworkSerialize(serializer);
    }
}
```

If you were to serialize `MyStructA` alone, it would serialize `Position` and `Rotation` into the stream using `NetworkSerializer`.

However, if you were to serialize `MyStructB`, it would serialize `SomeNumber` and `SomeText` into the stream, then serialize `StructA` by calling `MyStructA` 's `void NetworkSerialize(NetworkSerializer)` method, which serializes `Position` and `Rotation` into the same stream.

> [!NOTE]
> Technically, there is no hard-limit on the number of `INetworkSerializable` fields you can serialize down the tree hierarchy. In practice, consider memory and bandwidth boundaries for best performance.

> [!NOTE]
> You can conditionally serialize in recursive nested serialization scenario and make use of both features.

While you can have nested `INetworkSerializable` implementations (an `INetworkSerializable` implementation with `INetworkSerializable` implementations as properties) like demonstrated in the example above, you can't have derived children of an `INetworkSerializable` implementation. <br/>
**Unsupported Example**

```csharp
/// This isn't supported.
public struct MyStructB : MyStructA
{
    public int SomeNumber;
    public string SomeText;

    void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SomeNumber);
        serializer.SerializeValue(ref SomeText);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Rotation);
    }
}
```
