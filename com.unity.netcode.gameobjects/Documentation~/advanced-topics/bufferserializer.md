# BufferSerializer

`BufferSerializer<TReaderWriter>` is the bi-directional serializer primarily used for serializing within [`INetworkSerializable`](inetworkserializable.md) types. It wraps [`FastBufferWriter` and `FastBufferReader`](fastbufferwriter-fastbufferreader.md) to provide high performance serialization, but has a couple of differences to make it more user-friendly:

- Rather than writing separate methods for serializing and deserializing, `BufferSerializer<TReaderWriter>` allows writing a single method that can handle both operations, which reduces the possibility of a mismatch between the two
- `BufferSerializer<TReaderWriter>` does bound checking on every read and write by default, making it easier to avoid mistakes around manual bounds checking required by `FastBufferWriter` and `FastBufferReader`

These aren't without downsides, however:

- `BufferSerializer<TReaderWriter>` has to operate on an existing mutable value due to its bi-directional nature, which means values like `List<T>.Count` have to be stored to a local variable before writing.
- `BufferSerializer<TReaderWriter>` is slightly slower than `FastBufferReader` and `FastBufferWriter` due to both the extra pass-through method calls and the mandatory bounds checking on each write.
- `BufferSerializer<TReaderWriter>` don't support any form of packed reads and writes.

However, when those downsides are unreasonable, `BufferSerializer<TReaderWriter>` offers two ways to perform more optimal serialization for either performance or bandwidth usage:

- For performance, you can use `PreCheck(int amount)` followed by `SerializeValuePreChecked()` to perform bounds checking for multiple fields at once.
- For both performance and bandwidth usage, you can obtain the wrapped underlying reader/writer via `serializer.GetFastBufferReader()` when `serializer.IsReader` is `true`, and `serializer.GetFastBufferWriter()` when `serializer.IsWriter` is `true`. These provide micro-performance improvements by removing a level of indirection, and also give you a type you can use with `BytePacker` and `ByteUnpacker`.
