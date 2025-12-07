using System;
using Unity.Collections;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Two-way serializer wrapping FastBufferReader or FastBufferWriter.
    ///
    /// Implemented as a ref struct to help enforce the requirement that
    /// the BufferSerializer cannot outlive the FBR/FBW it wraps or using it will cause a crash
    ///
    /// BufferSerializer doesn't wrap FastBufferReader or FastBufferWriter directly because it can't.
    /// ref structs can't implement interfaces, and in order to be able to have two different implementations with
    /// the same interface (which allows us to avoid an "if(IsReader)" on every call), the thing directly wrapping
    /// the struct has to implement an interface. So IReaderWriter exists as the interface,
    /// which is implemented by a normal struct, while the ref struct wraps the normal one to enforce the two above
    /// requirements. (Allowing direct access to the IReaderWriter struct would allow dangerous
    /// things to happen because the struct's lifetime could outlive the Reader/Writer's.)
    /// </summary>
    /// <typeparam name="TReaderWriter">The implementation struct</typeparam>
    public ref struct BufferSerializer<TReaderWriter> where TReaderWriter : IReaderWriter
    {
        private TReaderWriter m_Implementation;

        /// <summary>
        /// Check if the contained implementation is a reader
        /// </summary>
        public bool IsReader => m_Implementation.IsReader;

        /// <summary>
        /// Check if the contained implementation is a writer
        /// </summary>
        public bool IsWriter => m_Implementation.IsWriter;

        internal BufferSerializer(TReaderWriter implementation)
        {
            m_Implementation = implementation;
        }

        /// <summary>
        /// Retrieves the FastBufferReader instance. Only valid if IsReader = true, throws
        /// InvalidOperationException otherwise.
        /// </summary>
        /// <returns>Reader instance</returns>
        public FastBufferReader GetFastBufferReader()
        {
            return m_Implementation.GetFastBufferReader();
        }

        /// <summary>
        /// Retrieves the FastBufferWriter instance. Only valid if IsWriter = true, throws
        /// InvalidOperationException otherwise.
        /// </summary>
        /// <returns>Writer instance</returns>
        public FastBufferWriter GetFastBufferWriter()
        {
            return m_Implementation.GetFastBufferWriter();
        }


        /// <summary>
        /// Read or write a string
        /// </summary>
        /// <param name="s">The value to read/write</param>
        /// <param name="oneByteChars">If true, characters will be limited to one-byte ASCII characters</param>
        public void SerializeValue(ref string s, bool oneByteChars = false) => m_Implementation.SerializeValue(ref s, oneByteChars);

        /// <summary>
        /// Read or write a single byte
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValue(ref byte value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a primitive value (int, bool, etc)
        /// Accepts any value that implements the given interfaces, but is not guaranteed to work correctly
        /// on values that are not primitives.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of primitive values (int, bool, etc)
        /// Accepts any value that implements the given interfaces, but is not guaranteed to work correctly
        /// on values that are not primitives.
        /// </summary>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an enum value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of enum values
        /// </summary>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a struct value implementing ISerializeByMemcpy
        /// </summary>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of struct values implementing ISerializeByMemcpy
        /// </summary>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a NativeArray of struct values implementing ISerializeByMemcpy
        /// </summary>
        /// <param name="value">The values to read/write</param>
        /// <param name="allocator">The allocator to use to construct the resulting NativeArray when reading</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        public void SerializeValue<T>(ref NativeArray<T> value, Allocator allocator, FastBufferWriter.ForGeneric unused = default) where T : unmanaged => m_Implementation.SerializeValue(ref value, allocator);

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        /// <summary>
        /// Read or write a NativeList of struct values implementing ISerializeByMemcpy
        /// </summary>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        public void SerializeValue<T>(ref NativeList<T> value, FastBufferWriter.ForGeneric unused = default) where T : unmanaged => m_Implementation.SerializeValue(ref value);
#endif

        /// <summary>
        /// Read or write a struct or class value implementing INetworkSerializable
        /// </summary>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForNetworkSerializable unused = default) where T : INetworkSerializable, new() => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of struct or class values implementing INetworkSerializable
        /// </summary>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForNetworkSerializable unused = default) where T : INetworkSerializable, new() => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a Vector2 value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValue(ref Vector2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of Vector2 values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValue(ref Vector2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a Vector3 value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValue(ref Vector3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of Vector3 values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValue(ref Vector3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a Vector2Int value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValue(ref Vector2Int value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of Vector2Int values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValue(ref Vector2Int[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a Vector3Int value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValue(ref Vector3Int value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of Vector3Int values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValue(ref Vector3Int[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a Vector4 value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValue(ref Vector4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of Vector4 values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValue(ref Vector4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a Quaternion value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValue(ref Quaternion value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of Quaternion values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValue(ref Quaternion[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a Pose value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValue(ref Pose value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of Pose values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValue(ref Pose[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a Color value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValue(ref Color value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of Color values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValue(ref Color[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a Color32 value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValue(ref Color32 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of Color32 values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValue(ref Color32[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a Ray value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValue(ref Ray value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of Ray values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValue(ref Ray[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a Ray2D value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValue(ref Ray2D value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write an array of Ray2D values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValue(ref Ray2D[] value) => m_Implementation.SerializeValue(ref value);

        // There are many FixedString types, but all of them share the interfaces INativeList<bool> and IUTF8Bytes.
        // INativeList<bool> provides the Length property
        // IUTF8Bytes provides GetUnsafePtr()
        // Those two are necessary to serialize FixedStrings efficiently
        // - otherwise we'd just be memcpy'ing the whole thing even if
        // most of it isn't used.

        /// <summary>
        /// Read or write a FixedString value
        /// </summary>
        /// <typeparam name="T">The network serializable type</typeparam>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution of FixedStrings</param>
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForFixedStrings unused = default)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a NativeArray of FixedString values
        /// </summary>
        /// <typeparam name="T">The network serializable type</typeparam>
        /// <param name="value">The values to read/write</param>
        /// <param name="allocator">The allocator to use to construct the resulting NativeArray when reading</param>
        public void SerializeValue<T>(ref NativeArray<T> value, Allocator allocator)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes => m_Implementation.SerializeValue(ref value, allocator);

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        /// <summary>
        /// Read or write a NativeList of FixedString values
        /// </summary>
        /// <typeparam name="T">The network serializable type</typeparam>
        /// <param name="value">The values to read/write</param>
        public void SerializeValue<T>(ref NativeList<T> value)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes => m_Implementation.SerializeValue(ref value);
#endif

        /// <summary>
        /// Read or write a NetworkSerializable value.
        /// SerializeValue() is the preferred method to do this - this is provided for backward compatibility only.
        /// </summary>
        /// <typeparam name="T">The network serializable type</typeparam>
        /// <param name="value">The values to read/write</param>
        public void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable, new() => m_Implementation.SerializeNetworkSerializable(ref value);

        /// <summary>
        /// Performs an advance check to ensure space is available to read/write one or more values.
        /// This provides a performance benefit for serializing multiple values using the
        /// SerializeValuePreChecked methods. But note that the benefit is small and only likely to be
        /// noticeable if serializing a very large number of items.
        /// </summary>
        /// <param name="amount">The number of values that will need to be read or written</param>
        /// <returns>True if there is sufficient space available for the specified amount, false otherwise</returns>
        public bool PreCheck(int amount)
        {
            return m_Implementation.PreCheck(amount);
        }

        /// <summary>
        /// Serialize a string, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="s">The value to read/write</param>
        /// <param name="oneByteChars">If true, characters will be limited to one-byte ASCII characters</param>
        public void SerializeValuePreChecked(ref string s, bool oneByteChars = false) => m_Implementation.SerializeValuePreChecked(ref s, oneByteChars);

        /// <summary>
        /// Serialize a byte, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref byte value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a primitive, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The network serializable type</typeparam>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize an array of primitives, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The network serializable types in an array</typeparam>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution of primitives</param>
        public void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize an enum, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The network serializable type</typeparam>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution of enums</param>
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize an array of enums, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The network serializable types in an array</typeparam>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution of enums</param>
        public void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a struct, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The network serializable type</typeparam>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution of structs</param>
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize an array of structs, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The network serializable types in an array</typeparam>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution of structs</param>
        public void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a NativeArray of structs, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The network serializable types in an array</typeparam>
        /// <param name="value">The values to read/write</param>
        /// <param name="allocator">The allocator to use to construct the resulting NativeArray when reading</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution of structs</param>
        public void SerializeValuePreChecked<T>(ref NativeArray<T> value, Allocator allocator, FastBufferWriter.ForGeneric unused = default) where T : unmanaged => m_Implementation.SerializeValuePreChecked(ref value, allocator);

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        /// <summary>
        /// Serialize a NativeList of structs, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The network serializable types in an array</typeparam>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution of structs</param>
        public void SerializeValuePreChecked<T>(ref NativeList<T> value, FastBufferWriter.ForGeneric unused = default) where T : unmanaged => m_Implementation.SerializeValuePreChecked(ref value);
#endif

        /// <summary>
        /// Serialize a Vector2, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Vector2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Vector2 array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValuePreChecked(ref Vector2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Vector3, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Vector3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Vector3 array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValuePreChecked(ref Vector3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Vector2Int, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Vector2Int value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Vector2Int array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The values to read/write</param>
        public void SerializeValuePreChecked(ref Vector2Int[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Vector3Int, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Vector3Int value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Vector3Int array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Vector3Int[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Vector4, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Vector4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Vector4 array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Vector4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Quaternion, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Quaternion value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Quaternion array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Quaternion[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Pose, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Pose value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Pose array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Pose[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Color, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Color value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Color array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Color[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Color32, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Color32 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Color32 array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Color32[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Ray, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Ray value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Ray array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Ray[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Ray2D, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Ray2D value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a Ray2D array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        public void SerializeValuePreChecked(ref Ray2D[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        // There are many FixedString types, but all of them share the interfaces INativeList<bool> and IUTF8Bytes.
        // INativeList<bool> provides the Length property
        // IUTF8Bytes provides GetUnsafePtr()
        // Those two are necessary to serialize FixedStrings efficiently
        // - otherwise we'd just be memcpying the whole thing even if
        // most of it isn't used.

        /// <summary>
        /// Serialize a FixedString, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The network serializable type</typeparam>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter that can be used for enabling overload resolution for fixed strings</param>
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForFixedStrings unused = default)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes => m_Implementation.SerializeValuePreChecked(ref value);
    }
}
