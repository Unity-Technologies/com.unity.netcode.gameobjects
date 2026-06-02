# Serialization overview

Netcode uses a default serialization pipeline when using [`RPC`](../../rpc-landing.md)'s, [`NetworkVariable`](../../networkvariables-landing.md)s, or any other Netcode-related tasks that require serialization. The serialization pipeline looks like this:

``
Custom Types => Built In Types => INetworkSerializable
``

That is, when Netcode first gets hold of a type, it will check for any custom types that the user have registered for serialization, after that it will check if it's a built in type, such as a Vector3, float etc. These are handled by default. If not, it will check if the type inherits [`INetworkSerializable`](serialization/inetworkserializable.md), if it does, it will call it's write methods.

By default, any type that satisfies the `unmanaged` generic constraint can be automatically serialized as RPC parameters. This includes all basic types (bool, byte, int, float, enum, etc) as well as any structs that has only these basic types.

Serialization and deserialization is done via the structs [`FastBufferWriter` and `FastBufferReader`](fastbufferwriter-fastbufferreader.md). These have methods for serializing individual types and methods for serializing packed numbers, but in particular provide a high-performance method called `WriteValue()/ReadValue()` (for Writers and Readers, respectively) that can extremely quickly write an entire unmanaged struct to a buffer.

`FastBufferWriter` and `FastBufferReader` also contain the functions `FastBufferWriter.WriteNetworkSerializable()` and `FastBufferReader.ReadNetworkSerializable` for writing and reading values that use the `INetworkSerializable` interface.

## Built in serialization

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

## Other resources

* [NetworkObject serialization](./networkobject-serialization.md)
* [FastBufferWriter and FastBufferReader](../fastbufferwriter-fastbufferreader.md)
* [BufferSerializer](../bufferserializer.md)
