---
title: BitWriter, BitReader & BitStream
permalink: /wiki/bitwriter-bitreader-bitstream/
---

Internally, the MLAPI uses Streams for it's data. This gives a ton of flexibility for the end user. If the end user for example doesn't want to use Streams but rather just byte arrays at their own level. They can do so by wrapping their arrays in MemoryStreams which doesn't create any garbage.


The MLAPI does have it's own prefered Stream that is used internally. It's called the BitStream.

## BitStream
The BitStream is a Stream implementation that functions in a similar way as the MemoryStream. The main difference is that the BitStream have methods for operating on the Bit level rather than the Byte level.

### PooledBitStream
Creating resizable BitStreams allocates a byte array to back it, just like a MemoryStream. To not create any allocations, the MLAPI has a built in Pool of BitStreams which is recommended to be used instead.

```csharp
using (PooledBitStream stream = PooledBitStream.Get())
{
    // Do stuff with the Pooled Stream. This stream is reset and ready for use, it will auto resize to fit all your data.
}
```

## Writer & Reader
While the BinaryWriter class built into .NET is great for reading and writing binary data, it's not very compact or efficient and doesn't offer a ton of flexibility. The BitWriter and BitReader solves this.

The BitWriter and BitReader can operate at the bit level when used with a BitStream. It also has many fancy write methods for compacting data.

Some of it's key features are:
#### Value VarInt
When using the "Packed" versions of a write or read, the output will be VarInted. That is, smaller values will take less space. If you write the value 50 as a ulong in the packed format, it will only take one byte in the output.

#### Diff Arrays
When using the "Diff" versions of an array write or read, the output will be the diff between two arrays, allowing for delta encoding.

#### Unity Types
The BitWriter & BitReader supports many data types by default such as Vector3, Vector2, Ray, Quaternion and more.

#### BitWise Writing
If you for example have an enum with 5 values. All those values could be fit into 3 bits. With the BitWriter, this can be done like this:

```csharp
writer.WriteBits((byte)MyEnum.MyEnumValue, 3);
MyEnum value = (Myenum)reader.ReadBits(3);
```

#### Performance concideration
When the stream is not aligned, (BitAligned == false, this occurs when writing bits that does fill the whole byte, or when writing bools as they are written as bits), performance is decreased for each write and read. This is only a big concern if you are about to write a large amount of data after not being aligned. To solve this, the BitWriter allows you to "WritePadBits" and the BitReader then lets you skip those bits with "SkipPadBits" to align the stream to the nearest byte.

```csharp
writer.WriteBool(true); //Now the stream is no longer aligned. Every byte has to be offset by 1 bit.
writer.WritePadBits(); //Writes 7 empty bits to make the stream aligned.
writer.WriteByteArray(myLargeArray, 1024); //Writes 1024 bytes without any bit adjustments


reader.ReadBool();
reader.SkipPadBits();
reader.ReadByteArray(myOutputArray, 1024);
```

### Pooled BitReader/Writer
The writer and reader also has pooled versions to avoid allocating the classes themselves. You might aswell use them.

```csharp
using (PooledBitReader reader = PooledBitReader.Get(myStreamToReadFrom))
{
    // Read here
}

using (PooledBitWriter writer = PooledBitWriter.Get(myStreamToWriteTo))
{
    // Write here
}
```
