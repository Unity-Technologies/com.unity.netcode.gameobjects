# Serialization overview

Netcode for GameObjects uses a default serialization pipeline when using [`RPC`](../../rpc-landing.md)'s, [`NetworkVariable`](../../networkvariables-landing.md)s, or any other Netcode-related tasks that require serialization. The serialization pipeline looks like this:

``
Custom Types => Built In Types => INetworkSerializable
``

When Netcode for GameObjects first receives a type, it checks for any custom types that you have registered for serialization, then it checks if it's a built-in type, such as a Vector3 or a float. These are handled by default. Otherwise, it checks if the type inherits [`INetworkSerializable`](inetworkserializable.md), and if it does, it calls its write methods.

By default, any type that satisfies the unmanaged generic constraint can be automatically serialized as RPC parameters. This includes all basic types (bool, byte, int, float, enum, for example), as well as any structs that contain only these basic types.

Serialization and deserialization is done via the structs [`FastBufferWriter` and `FastBufferReader`](fastbufferwriter-fastbufferreader.md). These have methods for serializing individual types and methods for serializing packed numbers, but in particular provide a high-performance method called `WriteValue()/ReadValue()` (for Writers and Readers, respectively) that can extremely quickly write an entire unmanaged struct to a buffer.

`FastBufferWriter` and `FastBufferReader` also contain the functions `FastBufferWriter.WriteNetworkSerializable()` and `FastBufferReader.ReadNetworkSerializable` for writing and reading values that use the `INetworkSerializable` interface.

## Built-in serialization

* [C# primitives](./cprimitives.md)
* [Unity primitives](./unity-primitives.md)
* [Enum types](./enum-types.md)
* [Arrays](./serialization-arrays.md)
* [Collections](../../basics/networkvariable.md#using-collections-with-networkvariables)

## Custom serialization

* [INetworkSerializable](./inetworkserializable.md)
* [INetworkSerializeByMemcpy](./inetworkserializebymemcpy.md)
* [Customizing serialization](../custom-serialization.md)
* [Custom NetworkVariable implementations](../../basics/custom-networkvariables.md)

## Additional resources

* [NetworkObject serialization](./networkobject-serialization.md)
* [FastBufferWriter and FastBufferReader](../fastbufferwriter-fastbufferreader.md)
* [BufferSerializer](../bufferserializer.md)
