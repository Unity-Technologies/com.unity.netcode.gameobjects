# Custom serialization

> [!NOTE]
> Read the [Serialization overview](./serialization/serialization-overview.md) page to understand the basics of serialization before learning how to customize serialization.

Netcode for GameObjects provide support for serializing any unsupported types, and with the API provided, it can even be done with types that you haven't defined yourself, those who are behind a 3rd party wall, such as .NET types. However, the way custom serialization is implemented for RPCs and NetworkVariables is slightly different.

Netcode for GameObjects supports custom serialization of unsupported types, including those you haven't defined yourself, such as third-party .NET types. You can also use custom serialization to override how an existing supported type is serialized.

Custom serialization is implemented slightly differently for RPCs and NetworkVariables. The examples on this page provide different ways to serialization a custom health struct.

[!code-cs[](../../Tests/Editor/DocumentationCodeSamples/Serialization/SerializationCustomization.cs#HealthStruct)]

## FastBufferReader and FastBufferWriter

[`FastBufferReader` and `FastBufferWriter`](./fastbufferwriter-fastbufferreader.md) are the main serialization tools in Netcode for GameObjects. To register serialization for a custom type, or override an already handled type, you need to create extension methods for `FastBufferReader.ReadValueSafe()` and `FastBufferWriter.WriteValueSafe()`. `FastBufferReader` and `FastBufferWriter` can read and write primitive types, and you can extend this functionality to serialize your custom type.

[!code-cs[](../../Tests/Editor/DocumentationCodeSamples/Serialization/SerializationCustomization.cs#FastBuffer)]

You may also need to add extensions for `FastBufferReader.ReadValue()`, `FastBufferWriter.WriteValue()` if you want to serialize without [bounds checking](./fastbufferwriter-fastbufferreader.md#bounds-checking)

## BufferSerializer

You can also add custom serialization support to the bi-directional [`BufferSerializer`](./bufferserializer.md). This makes your custom type readily available within [`INetworkSerializable`](serialization/inetworkserializable.md) types and in the [`NetworkBehaviour.OnSynchronize()` method](../components/core/networkbehaviour-synchronize.md#prespawn-synchronization-with-onsynchronize):

[!code-cs[](../../Tests/Editor/DocumentationCodeSamples/Serialization/SerializationCustomization.cs#BufferSerializer)]

## Remote procedure call (RPCs)

> [!NOTE]
> RPCs can use the Network Variable flow, but NetworkVariables can't use the RPC flow. The RPC flow is more efficient when only RPCs need to serialize the type. When a type is used by both NetworkVariables and RPCs, you can implement just the NetworkVariable flow to lower maintenance requirements. Unity will select the RPC flow for RPCs if you have implemented both flows.

To serialize a custom type, or override an already handled type, you need to create extension methods for `FastBufferReader.ReadValueSafe()` and `FastBufferWriter.WriteValueSafe()` as [outlined above](#fastbufferreader-and-fastbufferwriter).

The code generation for RPCs will automatically pick up and use these functions, as they'll become available via `FastBufferWriter` and `FastBufferReader` directly.

## NetworkVariable

Implementing [`INetworkSerializable`](./serialization/inetworkserializable.md) is the cleanest and most straightforward way to customize the serialization of a type within a [`NetworkVariable`](../basics/networkvariable.md). `UserNetworkVariableSerialization` provides runtime configuration to further override serialization of a type.

First you will need to create extension methods for `FastBufferReader.ReadValueSafe()` and `FastBufferWriter.WriteValueSafe()` as [outlined above](#fastbufferreader-and-fastbufferwriter).

Secondly, somewhere in your application startup (before any `NetworkVariable`s using the affected types will be serialized), add the following:

```csharp
UserNetworkVariableSerialization<Health>.WriteValue = SerializationExtensions.WriteValueSafe;
UserNetworkVariableSerialization<Health>.ReadValue = SerializationExtensions.ReadValueSafe;
UserNetworkVariableSerialization<Health>.DuplicateValue = (in Health value, ref Health duplicatedValue) => duplicatedValue = value;
```

`DuplicateValue` should return a complete deep copy of the value that `NetworkVariable<T>` compares to a previous value, which is used to check whether the value has changed. `DuplicateValue` avoids re-serializing it over the network every frame when it hasn't changed.

> [!NOTE]
> `WriteValue`, `ReadValue` and `DuplicateValue` all need to be defined to customize your serialization.

> [!NOTE]
> `WriteValue` and `ReadValue` will not be used if a type implements `INetworkSerializable` or [`INetworkSerializeByMemcpy`](./serialization/inetworkserializebymemcpy.md).

### Serializing delta updates

Reading and writing a value provides the minimal amount of `NetworkVariable` functionality. This will synchronize your whole type whenever any value within the type value changes. To support sending delta updates rather than a full update whenever your type has changed, implement the following functions:

- `WriteDelta`
- `ReadDelta`

> [!NOTE]
> Both `WriteDelta` and `ReadDelta` need to be defined for either to be used.

Here is a full implementation of a custom type with the methods needed for `UserNetworkVariableSerialization`

[!code-cs[](../../Tests/Runtime/DocumentationCodeSamples/NetworkVariable/NetworkVariableSerialization.cs#HealthExample)]
