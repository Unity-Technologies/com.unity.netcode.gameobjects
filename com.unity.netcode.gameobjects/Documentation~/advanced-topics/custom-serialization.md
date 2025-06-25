# Custom serialization

Netcode uses a default serialization pipeline when using `RPC`s, `NetworkVariable`s, or any other Netcode-related tasks that require serialization. The serialization pipeline looks like this:

``
Custom Types => Built In Types => INetworkSerializable
``

That is, when Netcode first gets hold of a type, it will check for any custom types that the user have registered for serialization, after that it will check if it's a built in type, such as a Vector3, float etc. These are handled by default. If not, it will check if the type inherits `INetworkSerializable`, if it does, it will call it's write methods.

By default, any type that satisfies the `unmanaged` generic constraint can be automatically serialized as RPC parameters. This includes all basic types (bool, byte, int, float, enum, etc) as well as any structs that has only these basic types.

With this flow, you can provide support for serializing any unsupported types, and with the API provided, it can even be done with types that you haven't defined yourself, those who are behind a 3rd party wall, such as .NET types. However, the way custom serialization is implemented for RPCs and NetworkVariables is slightly different.

### Serialize a type in a Remote Procedure Call (RPC)

> [!NOTE]
> From versioln 1.7.0 Remote Procedure Calls (RPCs) can also use the Network Variable flow, but NetworkVariables can't use the RPC flow. The RPC flow is more efficient when RPCs serialize the type. Unity selects the RPC flow if you implement both the RPC and Network variable flows. When a type is used by both NetworkVariables and RPCs you can use the NetworkVariable flow to lower maintenance requirements.

To register a custom type, or override an already handled type, you need to create extension methods for `FastBufferReader.ReadValueSafe()` and `FastBufferWriter.WriteValueSafe()`:

```csharp
// Tells the Netcode how to serialize and deserialize Url in the future.
// The class name doesn't matter here.
public static class SerializationExtensions
{
    public static void ReadValueSafe(this FastBufferReader reader, out Url url)
    {
        reader.ReadValueSafe(out string val);
        url = new Url(val);
    }

    public static void WriteValueSafe(this FastBufferWriter writer, in Url url)
    {
        writer.WriteValueSafe(url.Value);
    }
}
```

The code generation for RPCs will automatically pick up and use these functions, and they'll become available via `FastBufferWriter` and `FastBufferReader` directly.

You can also optionally use the same method to add support for `BufferSerializer<TReaderWriter>.SerializeValue()`, if you wish, which will make this type readily available within [`INetworkSerializable`](/advanced-topics/serialization/inetworkserializable.md) types:

```csharp
// The class name doesn't matter here.
public static class SerializationExtensions
{  
    public static void SerializeValue<TReaderWriter>(this BufferSerializer<TReaderWriter> serializer, ref Url url) where TReaderWriter: IReaderWriter
    {
        if (serializer.IsReader)
        {
            url = new Url();
        }
        serializer.SerializeValue(ref url.Value);
    }
}
```

Additionally, you can also add extensions for `FastBufferReader.ReadValue()`, `FastBufferWriter.WriteValue()`, and `BufferSerializer<TReaderWriter>.SerializeValuePreChecked()` to provide more optimal implementations for manual serialization using `FastBufferReader.TryBeginRead()`, `FastBufferWriter.TryBeginWrite()`, and `BufferSerializer<TReaderWriter>.PreCheck()`, respectively. However, none of these will be used for serializing RPCs - only `ReadValueSafe` and `WriteValueSafe` are used.

### For NetworkVariable

`NetworkVariable` goes through a slightly different pipeline than `RPC`s and relies on a different process for determining how to serialize its types. As a result, making a custom type available to the `RPC` pipeline doesn't automatically make it available to the `NetworkVariable` pipeline, and vice-versa. The same method can be used for both, but currently, `NetworkVariable` requires an additional runtime step to make it aware of the methods.

To add custom serialization support in `NetworkVariable`, follow the steps from the "For RPCs" section to write extension methods for `FastBufferReader` and `FastBufferWriter`; then, somewhere in your application startup (before any `NetworkVariable`s using the affected types will be serialized) add the following:

```csharp
UserNetworkVariableSerialization<Url>.WriteValue = SerializationExtensions.WriteValueSafe;
UserNetworkVariableSerialization<Url>.ReadValue = SerializationExtensions.ReadValueSafe;    
```

You can also use lambda expressions here:

```csharp
UserNetworkVariableSerialization<Url>.WriteValue = (FastBufferWriter writer, in Url url) =>
{
    writer.WriteValueSafe(url.Value);
};

UserNetworkVariableSerialization<Url>.ReadValue = (FastBufferReader reader, out Url url)
{
    reader.ReadValueSafe(out string val);
    url = new Url(val);
};
```

When you create an extension method in `NetworkVariable<T>` you need to implement the following values:

- `WriteValue`
- `ReadValue`
- `DuplicateValue`

`DuplicateValue` returns a complete deep copy of the value that `NetworkVariable<T>` compares to a previous value to check whether or not that values has changed. This avoids reserializing it over the network every frame when it hasn't changed.
