# FastBufferWriter and FastBufferReader

The serialization and deserialization is done via `FastBufferWriter` and `FastBufferReader`. These have methods for serializing individual types and methods for serializing packed numbers, but in particular provide a high-performance method called `WriteValue()/ReadValue()` (for Writers and Readers, respectively) that can extremely quickly write an entire unmanaged struct to a buffer.

There's a trade-off of CPU usage vs bandwidth in using this: Writing individual fields is slower (especially when it includes operations on unaligned memory), but allows the buffer to be filled more efficiently, both because it avoids padding for alignment in structs, and because it allows you to use `BytePacker.WriteValuePacked()`/`ByteUnpacker.ReadValuePacked()` and `BytePacker.WriteValueBitPacked()`/`ByteUnpacker.ReadValueBitPacked()`. The difference between these two is that the BitPacked variants pack more efficiently, but they reduce the valid range of values. See the section below for details on packing.


**Example**

```csharp
struct ExampleStruct
{
    private float f;
    private bool b;
    private int i;

    void Serialize(FastBufferWriter writer);
}
```

In this example struct, `Serialize` can be implemented in two ways:

```csharp
void Serialize(FastBufferWriter writer)
{
    if(!writer.TryBeginWrite(sizeof(float) + sizeof(bool) + sizeof(i)))
    {
		throw new OverflowException("Not enough space in the buffer");
    }
    writer.WriteValue(f);
    writer.WriteValue(b);
    writer.WriteValue(i);
}
```

```csharp
void Serialize(FastBufferWriter writer)
{
    if(!writer.TryBeginWrite(sizeof(ExampleStruct)))
    {
		throw new OverflowException("Not enough space in the buffer");
    }
    writer.WriteValue(this);
}
```
This creates efficiently packed data in the message, and can be further optimized by using `BytePacker.WriteValuePacked()` and `BytePacker.WriteValueBitPacked()`, but it has two downsides:
- First, it involves more method calls and more instructions, making it slower.
- Second, that it creates a greater opportunity for the serialize and deserialize code to become misaligned, since they must contain the same operations in the same order.

You can also use a hybrid approach if you have a few values that will need to be packed and several that won't:

```C#
struct ExampleStruct
{
    struct Embedded
    {
        private byte a;
        private byte b;
        private byte c;
        private byte d;
    }
    public Embedded embedded;
    public float f;
    public short i;

    void Serialize(FastBufferWriter writer)
    {
        writer.WriteValue(embedded);
        BytePacker.WriteValuePacked(writer, f);
        BytePacker.WriteValuePacked(writer, i);
    }
}
```

This allows the four bytes of the embedded struct to be rapidly serialized as a single action, then adds the compacted data at the end, resulting in better bandwidth usage than serializing the whole struct as-is, but better performance than serializing it one byte at a time.


## FastBufferWriter and FastBufferReader

`FastBufferWriter` and `FastBufferReader` are replacements for the old `NetworkWriter` and `NetworkReader`. For those familiar with the old classes, there are some key differences:

- `FastBufferWriter` uses `WriteValue()` as the name of the method for all types *except* [`INetworkSerializable`](serialization/inetworkserializable) types, which are serialized through `WriteNetworkSerializable()`
- `FastBufferReader` similarly uses `ReadValue()` for all types except INetworkSerializable (which is read through `ReadNetworkSerializable`), with the output changed from a return value to an `out` parameter to allow for method overload resolution to pick the correct value.
- `FastBufferWriter` and `FastBufferReader` outsource packed writes and reads to `BytePacker` and `ByteUnpacker`, respectively.
- `FastBufferWriter` and `FastBufferReader` are **structs**, not **classes**. This means they can be constructed and destructed without GC allocations.
- `FastBufferWriter` and `FastBufferReader` both use the same allocation scheme as Native Containers, allowing the internal buffers to be created and resized without creating any garbage and with the use of `Allocator.Temp` or `Allocator.TempJob`.
- `FastBufferReader` can be instantiated using `Allocator.None` to operate on an existing buffer with no allocations and no copies.
- Neither `FastBufferReader` nor `FastBufferWriter` inherits from nor has a `Stream`.
- `FastBufferReader` and `FastBufferWriter` are heavily optimized for speed, using aggressive inlining and unsafe code to achieve the fastest possible buffer storage and retrieval.
- `FastBufferReader` and `FastBufferWriter` use unsafe typecasts and `UnsafeUtility.MemCpy` operations on `byte*` values, achieving native memory copy performance with no need to iterate or do bitwise shifts and masks.
- `FastBufferReader` and `FastBufferWriter` are intended to make data easier to debug - one such thing to support will be a `#define MLAPI_FAST_BUFFER_UNPACK_ALL` that will disable all packing operations to make the buffers for messages that use them easier to read.
- `FastBufferReader` and `FastBufferWriter` don't support runtime type discovery - there is no `WriteObject` or `ReadObject` implementation. All types must be known at compile time. This is to avoid garbage and boxing allocations.

A core benefit of `NativeArray<byte>` is that it offers access to the allocation scheme of `Allocator.TempJob`. This uses a special type of allocation that is nearly as fast as stack allocation and involves no GC overhead, while being able to persist for a few frames. In general they're rarely if ever needed for more than a frame, but this does provide a efficient option for creating buffers as needed, which avoids the need to use a pool for them. The only downside is that buffers created this way must be manually disposed after use, as they're not garbage collected.

## Creating and Disposing FastBufferWriters and FastBufferReaders

To create your own `FastBufferWriter`s and `FastBufferReader`s, it's important to note that struct default/parameterless constructors can't be removed or overridden, but `FastBufferWriter` and `FastBufferReader` require constructor behavior to be functional.

`FastBufferWriter` always owns its internal buffer and must be constructed with an initial size, an allocator, and a maximum size. If the maximum size isn't provided or is less than or equal to the initial size, the `FastBufferWriter` can't expand.

`FastBufferReader` can be constructed to either own its buffer or reference an existing one via `Allocator.None`. Not all types are compatible with `Allocator.None` - only `byte*`, `NativeArray<byte>`, and `FastBufferWriter` input types can provide `Allocator.None`. You can obtain a `byte*` from a `byte[]` using the following method:

```c#
byte[] byteArray;
fixed(byte* bytePtr = byteArray)
{
    // use bytePtr here
}
```

It's important to note with `Allocator.None` that the `FastBufferReader` will be directly referencing a position in memory, which means the `FastBufferReader` must not live longer than the input buffer it references - and if the input buffer is a `byte[]`, the `FastBufferReader` must not live longer than the `fixed()` statement, because outside of that statement, the garbage collector is free to move that memory, which will cause random and unpredictable errors.

Regardless which allocator you use (including `Allocator.None`), `FastBufferWriter` and `FastBufferReader` must always have `Dispose()` called on them when you're done with them. The best practice is to use them within `using` blocks.

## Bounds Checking

For performance reasons, by default, `FastBufferReader` and `FastBufferWriter` **don't do bounds checking** on each write. Rather, they require the use of specific bounds checking functions - `TryBeginRead(int amount)` and `TryBeginWrite(int amount)`, respectively. This improves performance by allowing you to verify the space exists for the multiple values in a single call, rather than doing that check on every single operation.

> [!NOTE]
> **In editor mode and development builds**, calling these functions records a watermark point, and any attempt to read or write past the watermark point will throw an exception. This ensures these functions are used properly, while avoiding the performance cost of per-operation checking in production builds. In production builds, attempting to read or write past the end of the buffer will cause undefined behavior, likely program instability and/or crashes.


For convenience, every `WriteValue()` and `ReadValue()` method has an equivalent `WriteValueSafe()` and `ReadValueSafe()` that does bounds checking for you, throwing `OverflowException` if the boundary is exceeded. Additionally, some methods, such as arrays (where the amount of data being read can't be known until the size value is read) and [`INetworkSerializable`](inetworkserializable.md) values (where the size can't be predicted outside the implementation) will always do bounds checking internally.

## Bitwise Reading and Writing

Writing values in sizes measured in bits rather than bytes comes with a cost
- First, it comes with a cost of having to track bitwise lengths and convert them to bytewise lenghts.
- Second, it comes with a cost of having to remember to add padding after your bitwise writes and reads to ensure the next bytewise write or read functions correctly, and to make sure the buffer length includes any trailing bits.

To address that, `FastBufferReader` and `FastBufferWriter` don't, themselves, have bitwise operations. When needed, however, you can create a `BitWriter` or `BitReader` instance, which is used ensure that no unaligned bits are left at the end - from the perspective of `FastBufferReader` and `FastBufferWriter`, only bytes are allowed. `BitWriter` and `BitReader` operate directly on the underlying buffer, so calling non-bitwise operations within a bitwise context is an error (and will raise an exception in non-production builds).

```csharp
FastBufferWriter writer = new FastBufferWriter(256, Allocator.TempJob);
using(var bitWriter = writer.EnterBitwiseContext())
{
	bitWriter.WriteBit(a);
	bitWriter.WriteBits(b, 5);
} // Dispose automatically adds 2 more 0 bits to pad to the next byte.
```

## Packing

Packing values is done using the utility classes `BytePacker` and `ByteUnpacker`. These generally offer two different ways of packing values:

- `BytePacker.WriteValuePacked()`/`ByteUnpacker.ReadValuePacked()` are the most versatile. They can write any range of values that fit into the type, and also have special built-in methods for many common Unity types that can automatically pack the values contained within.

- `BytePacker.WriteValueBitPacked()`/`ByteUnpacker.ReadValueBitPacked()` offer tighter/more optimal packing (the data in the buffer will never exceed `sizeof(type)`, which can happen with large values using `WriteValuePacked()`, and will usually be one byte smaller than with `WriteValuePacked()` except for values <= 240, which will be one byte with both methods), but come with the limitations that they can only be used on integral types, and they use some bits of the type to encode length information, meaning that they reduce the usable size of the type. The sizes allowed by these functions are as follows:

  | Type   | Usable Size                                                  |
  | ------ | ------------------------------------------------------------ |
  | short  | 14 bits + sign bit (-16,384 to 16,383)                       |
  | ushort | 15 bits (0 to 32,767)                                        |
  | int    | 29 bits + sign bit (-536,870,912 to 536,870,911)             |
  | uint   | 30 bits (0 to 1,073,741,824)                                 |
  | long   | 60 bits + sign bit (-1,152,921,504,606,846,976 to 1,152,921,504,606,846,975) |
  | ulong  | 61 bits (0 to 2,305,843,009,213,693,952)                     |
