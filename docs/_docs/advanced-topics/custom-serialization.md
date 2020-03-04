---
title: Custom Serialization
permalink: /wiki/custom-serialization/
---

When using RPC's, NetworkedVar's or any other MLAPI related task that requires serialization. The MLAPI uses a default serialization pipeline that looks like this:

``
Custom Types => Built In Types => IBitWritable
``

That is, when the MLAPI first gets hold of a type, it will check for any custom types that the user have registered for serialization, after that it will check if it's a built in type, such as a Vector3, float etc. These are handled by default. If not, it will check if the type inherits IBitWritable, if it does, it will call it's write methods.

The builtin types that are handled by default are:

* Nullable types where the underlying type is supported
* An array where the element type is supported (can be multi dimensional as this is done recursivley)
* Byte
* SByte
* UInt16
* Int16
* UInt32
* Int32
* UInt64
* Int64
* Single
* Double
* String
* Bool
* Vector2
* Vector3
* Vector4
* Color
* Color32
* Ray
* Quaternion
* Char
* Enums
* GameObject (Only if the GameObject has a NetworkedObject component that is spawned)
* NetworkedObject (Only if spawned)
* NetworkedBehaviour (Only if spawned)

With this flow, you can override **ALL** serialization for **ALL** types, even built in types, and with the API provided, it can even be done with types that you have not defined yourself, those who are behind a 3rd party wall, such as .NET types.

To register a custom type, or override an already handeld type you need to register a handler pair.

```csharp
// Tells the MLAPI how to serialize and deserialize Url in the future.
SerializationManager.RegisterSerializationHandlers<Url>((Stream stream, Url instance) =>
{
    // This delegate gets ran when the MLAPI want's to serialize a Url type to the stream.
    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
    {
        writer.WriteStringPacked(instance.Value);
    }
}, (Stream stream) =>
{
    // This delegate gets ran when the MLAPI want's to deserialize a Url type from the stream.
    using (PooledBitReader reader = PooledBitReader.Get(stream))
    {
        return new Url(reader.ReadStringPacked().ToString());
    }
});
```

Everytime you call the Register method, you are overriding any previous serialization method previously defined by you, or the MLAPI library. This is the most respected serialization method.


You can also restore privilages to how they were previously for a type.
```csharp
// Removes the handlers for the type Url.
SerializationManager.RemoveSerializationHandlers<Url>();
```