# INetworkSerializeByMemcpy

The `INetworkSerializeByMemcpy` interface is used to mark an unmanaged struct type as being trivially serializable over the network by directly copying the whole struct, byte-for-byte, as it appears in memory, into and out of the buffer. This can offer some benefits for performance compared to serializing one field at a time, especially if the struct has many fields in it, but it may be less efficient from a bandwidth-usage perspective, as fields will often be padded for memory alignment and you won't be able to "pack" any of the fields to optimize for space usage.

The interface itself has no methods in it - it's an empty interface that satisfies a constraint on methods that perform this type of serialization, primarily there to ensure that memcpy serialization isn't performed by accident on structs that don't support it.

```csharp
public struct MyStruct : INetworkSerializeByMemcpy
{
    public int A;
    public int B;
    public float C;
    public bool D;
}
```

If you have a type you wish to serialize that you know is compatible with this method of serialization, but don't have access to modify the struct to add this interface, you can wrap your values in `ForceNetworkSerializeByMemcpy` to enable it to be serialized this way. This works in both `RPC`s and `NetworkVariables`, as well as in other contexts such as `BufferSerializer<>`, `FastBufferReader`, and `FastBufferWriter`.

```csharp
public NetworkVariable<ForceNetworkSerializeByMemcpy<Guid>> GuidVar;
```

> [!NOTE]
> Take care with using `INetworkSerializeByMemcpy`, and especially `ForceNetworkSerializeByMemcpy`, because not all unmanaged structs are actually compatible with this type of serialization. Anything that includes pointer types (including Native Collections like `NativeArray<>`) won't function correctly when serialized this way, and will likely cause memory corruption or crashes on the receiving side.
