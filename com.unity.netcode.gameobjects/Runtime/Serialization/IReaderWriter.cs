using System;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

namespace Unity.Netcode
{
    /// <summary>
    /// Interface for an implementation of one side of a two-way serializer
    /// </summary>
    public interface IReaderWriter
    {
        /// <summary>
        /// Check whether this implementation is a "reader" - if it's been constructed to deserialize data
        /// </summary>
        bool IsReader { get; }
        /// <summary>
        /// Check whether this implementation is a "writer" - if it's been constructed to serialize data
        /// </summary>
        bool IsWriter { get; }

        /// <summary>
        /// Get the underlying FastBufferReader struct.
        /// Only valid when IsReader == true
        /// </summary>
        /// <returns>underlying FastBufferReader</returns>
        FastBufferReader GetFastBufferReader();
        /// <summary>
        /// Get the underlying FastBufferWriter struct.
        /// Only valid when IsWriter == true
        /// </summary>
        /// <returns>underlying FastBufferWriter</returns>
        FastBufferWriter GetFastBufferWriter();

        /// <summary>
        /// Read or write a string
        /// </summary>
        /// <param name="s">The value to read/write</param>
        /// <param name="oneByteChars">If true, characters will be limited to one-byte ASCII characters</param>
        void SerializeValue(ref string s, bool oneByteChars = false);

        /// <summary>
        /// Read or write a single byte
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref byte value);

        /// <summary>
        /// Read or write a primitive value (int, bool, etc)
        /// Accepts any value that implements the given interfaces, but is not guaranteed to work correctly
        /// on values that are not primitives.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        void SerializeValue<T>(ref T value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T>;

        /// <summary>
        /// Read or write an array of primitive values (int, bool, etc)
        /// Accepts any value that implements the given interfaces, but is not guaranteed to work correctly
        /// on values that are not primitives.
        /// </summary>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        void SerializeValue<T>(ref T[] value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T>;

        /// <summary>
        /// Read or write an enum value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        void SerializeValue<T>(ref T value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum;

        /// <summary>
        /// Read or write an array of enum values
        /// </summary>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        void SerializeValue<T>(ref T[] value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum;

        /// <summary>
        /// Read or write a struct value implementing ISerializeByMemcpy
        /// </summary>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        void SerializeValue<T>(ref T value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy;

        /// <summary>
        /// Read or write an array of struct values implementing ISerializeByMemcpy
        /// </summary>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        void SerializeValue<T>(ref T[] value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy;

        /// <summary>
        /// Read or write a struct or class value implementing INetworkSerializable
        /// </summary>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        void SerializeValue<T>(ref T value, FastBufferWriter.ForNetworkSerializable unused = default) where T : INetworkSerializable, new();

        /// <summary>
        /// Read or write an array of struct or class values implementing INetworkSerializable
        /// </summary>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        void SerializeValue<T>(ref T[] value, FastBufferWriter.ForNetworkSerializable unused = default) where T : INetworkSerializable, new();

        /// <summary>
        /// Read or write a FixedString value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter used for enabling overload resolution based on generic constraints</param>
        /// <typeparam name="T">The type being serialized</typeparam>
        void SerializeValue<T>(ref T value, FastBufferWriter.ForFixedStrings unused = default)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes;

        /// <summary>
        /// Read or write a Vector2 value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref Vector2 value);

        /// <summary>
        /// Read or write an array of Vector2 values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        void SerializeValue(ref Vector2[] value);

        /// <summary>
        /// Read or write a Vector3 value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref Vector3 value);

        /// <summary>
        /// Read or write an array of Vector3 values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        void SerializeValue(ref Vector3[] value);

        /// <summary>
        /// Read or write a Vector2Int value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref Vector2Int value);

        /// <summary>
        /// Read or write an array of Vector2Int values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        void SerializeValue(ref Vector2Int[] value);

        /// <summary>
        /// Read or write a Vector3Int value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref Vector3Int value);

        /// <summary>
        /// Read or write an array of Vector3Int values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        void SerializeValue(ref Vector3Int[] value);

        /// <summary>
        /// Read or write a Vector4 value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref Vector4 value);

        /// <summary>
        /// Read or write an array of Vector4 values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        void SerializeValue(ref Vector4[] value);

        /// <summary>
        /// Read or write a Quaternion value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref Quaternion value);

        /// <summary>
        /// Read or write an array of Quaternion values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        void SerializeValue(ref Quaternion[] value);

        /// <summary>
        /// Read or write a Color value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref Color value);

        /// <summary>
        /// Read or write an array of Color values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        void SerializeValue(ref Color[] value);

        /// <summary>
        /// Read or write a Color32 value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref Color32 value);

        /// <summary>
        /// Read or write an array of Color32 values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        void SerializeValue(ref Color32[] value);

        /// <summary>
        /// Read or write a Ray value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref Ray value);

        /// <summary>
        /// Read or write an array of Ray values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        void SerializeValue(ref Ray[] value);

        /// <summary>
        /// Read or write a Ray2D value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref Ray2D value);

        /// <summary>
        /// Read or write an array of Ray2D values
        /// </summary>
        /// <param name="value">The values to read/write</param>
        void SerializeValue(ref Ray2D[] value);

        void SerializeValue(ref bool2 value);

        void SerializeValue(ref bool2x2 value);

        void SerializeValue(ref bool2x3 value);

        void SerializeValue(ref bool2x4 value);

        void SerializeValue(ref bool3 value);

        void SerializeValue(ref bool3x2 value);

        void SerializeValue(ref bool3x3 value);

        void SerializeValue(ref bool3x4 value);

        void SerializeValue(ref bool4 value);

        void SerializeValue(ref bool4x2 value);

        void SerializeValue(ref bool4x3 value);

        void SerializeValue(ref bool4x4 value);

        void SerializeValue(ref double2 value);

        void SerializeValue(ref double2x2 value);

        void SerializeValue(ref double2x3 value);

        void SerializeValue(ref double2x4 value);

        void SerializeValue(ref double3 value);

        void SerializeValue(ref double3x2 value);

        void SerializeValue(ref double3x3 value);

        void SerializeValue(ref double3x4 value);

        void SerializeValue(ref double4 value);

        void SerializeValue(ref double4x2 value);

        void SerializeValue(ref double4x3 value);

        void SerializeValue(ref double4x4 value);

        void SerializeValue(ref float2 value);

        void SerializeValue(ref float2x2 value);

        void SerializeValue(ref float2x3 value);

        void SerializeValue(ref float2x4 value);

        void SerializeValue(ref float3 value);

        void SerializeValue(ref float3x2 value);

        void SerializeValue(ref float3x3 value);

        void SerializeValue(ref float3x4 value);

        void SerializeValue(ref float4 value);

        void SerializeValue(ref float4x2 value);

        void SerializeValue(ref float4x3 value);

        void SerializeValue(ref float4x4 value);

        /// <summary>
        /// Read or write a half value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref half value);

        /// <summary>
        /// Read or write a half2 value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref half2 value);

        /// <summary>
        /// Read or write a half3 value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref half3 value);

        /// <summary>
        /// Read or write a half4 value
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValue(ref half4 value);

        void SerializeValue(ref int2 value);

        void SerializeValue(ref int2x2 value);

        void SerializeValue(ref int2x3 value);

        void SerializeValue(ref int2x4 value);

        void SerializeValue(ref int3 value);

        void SerializeValue(ref int3x2 value);

        void SerializeValue(ref int3x3 value);

        void SerializeValue(ref int3x4 value);

        void SerializeValue(ref int4 value);

        void SerializeValue(ref int4x2 value);

        void SerializeValue(ref int4x3 value);

        void SerializeValue(ref int4x4 value);

        void SerializeValue(ref quaternion value);

        void SerializeValue(ref uint2 value);

        void SerializeValue(ref uint2x2 value);

        void SerializeValue(ref uint2x3 value);

        void SerializeValue(ref uint2x4 value);

        void SerializeValue(ref uint3 value);

        void SerializeValue(ref uint3x2 value);

        void SerializeValue(ref uint3x3 value);

        void SerializeValue(ref uint3x4 value);

        void SerializeValue(ref uint4 value);

        void SerializeValue(ref uint4x2 value);

        void SerializeValue(ref uint4x3 value);

        void SerializeValue(ref uint4x4 value);

        /// <summary>
        /// Read or write a NetworkSerializable value.
        /// SerializeValue() is the preferred method to do this - this is provided for backward compatibility only.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        /// <typeparam name="T">The network serializable type</typeparam>
        void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable, new();

        /// <summary>
        /// Performs an advance check to ensure space is available to read/write one or more values.
        /// This provides a performance benefit for serializing multiple values using the
        /// SerializeValuePreChecked methods. But note that the benefit is small and only likely to be
        /// noticeable if serializing a very large number of items.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        bool PreCheck(int amount);

        /// <summary>
        /// Serialize a string, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="s">The value to read/write</param>
        /// <param name="oneByteChars">If true, characters will be limited to one-byte ASCII characters</param>
        void SerializeValuePreChecked(ref string s, bool oneByteChars = false);

        /// <summary>
        /// Serialize a byte, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref byte value);

        /// <summary>
        /// Serialize a primitive, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The type being serialized</typeparam>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter that can be used for enabling overload resolution based on generic constraints</param>
        void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T>;

        /// <summary>
        /// Serialize an array of primitives, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The type being serialized</typeparam>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter that can be used for enabling overload resolution based on generic constraints</param>
        void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T>;

        /// <summary>
        /// Serialize an enum, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The type being serialized</typeparam>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter that can be used for enabling overload resolution based on generic constraints</param>
        void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum;

        /// <summary>
        /// Serialize an array of enums, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The type being serialized</typeparam>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter that can be used for enabling overload resolution based on generic constraints</param>
        void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum;

        /// <summary>
        /// Serialize a struct, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The type being serialized</typeparam>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter that can be used for enabling overload resolution based on generic constraints</param>
        void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy;

        /// <summary>
        /// Serialize an array of structs, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The type being serialized</typeparam>
        /// <param name="value">The values to read/write</param>
        /// <param name="unused">An unused parameter that can be used for enabling overload resolution based on generic constraints</param>
        void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy;

        /// <summary>
        /// Serialize a FixedString, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <typeparam name="T">The type being serialized</typeparam>
        /// <param name="value">The value to read/write</param>
        /// <param name="unused">An unused parameter that can be used for enabling overload resolution based on generic constraints</param>
        void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForFixedStrings unused = default)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes;

        /// <summary>
        /// Serialize a Vector2, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Vector2 value);

        /// <summary>
        /// Serialize a Vector2 array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The values to read/write</param>
        void SerializeValuePreChecked(ref Vector2[] value);

        /// <summary>
        /// Serialize a Vector3, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Vector3 value);

        /// <summary>
        /// Serialize a Vector3 array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The values to read/write</param>
        void SerializeValuePreChecked(ref Vector3[] value);

        /// <summary>
        /// Serialize a Vector2Int, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Vector2Int value);

        /// <summary>
        /// Serialize a Vector2Int array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The values to read/write</param>
        void SerializeValuePreChecked(ref Vector2Int[] value);

        /// <summary>
        /// Serialize a Vector3Int, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Vector3Int value);

        /// <summary>
        /// Serialize a Vector3Int array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Vector3Int[] value);

        /// <summary>
        /// Serialize a Vector4, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Vector4 value);

        /// <summary>
        /// Serialize a Vector4Array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Vector4[] value);

        /// <summary>
        /// Serialize a Quaternion, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Quaternion value);

        /// <summary>
        /// Serialize a Quaternion array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Quaternion[] value);

        /// <summary>
        /// Serialize a Color, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Color value);

        /// <summary>
        /// Serialize a Color array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Color[] value);

        /// <summary>
        /// Serialize a Color32, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Color32 value);

        /// <summary>
        /// Serialize a Color32 array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Color32[] value);

        /// <summary>
        /// Serialize a Ray, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Ray value);

        /// <summary>
        /// Serialize a Ray array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Ray[] value);

        /// <summary>
        /// Serialize a Ray2D, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Ray2D value);

        /// <summary>
        /// Serialize a Ray2D array, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref Ray2D[] value);

        /// <summary>
        /// Serialize a <see cref="bool2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref bool2 value);

        /// <summary>
        /// Serialize a <see cref="bool2x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref bool2x2 value);

        /// <summary>
        /// Serialize a <see cref="bool2x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref bool2x3 value);

        /// <summary>
        /// Serialize a <see cref="bool2x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref bool2x4 value);

        /// <summary>
        /// Serialize a <see cref="bool3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref bool3 value);

        /// <summary>
        /// Serialize a <see cref="bool3x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref bool3x2 value);

        /// <summary>
        /// Serialize a <see cref="bool3x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref bool3x3 value);

        /// <summary>
        /// Serialize a <see cref="bool3x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref bool3x4 value);

        /// <summary>
        /// Serialize a <see cref="bool4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref bool4 value);

        /// <summary>
        /// Serialize a <see cref="bool4x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref bool4x2 value);

        /// <summary>
        /// Serialize a <see cref="bool4x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref bool4x3 value);

        /// <summary>
        /// Serialize a <see cref="bool4x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref bool4x4 value);

        /// <summary>
        /// Serialize a <see cref="double2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref double2 value);

        /// <summary>
        /// Serialize a <see cref="double2x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref double2x2 value);

        /// <summary>
        /// Serialize a <see cref="double2x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref double2x3 value);

        /// <summary>
        /// Serialize a <see cref="double2x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref double2x4 value);

        /// <summary>
        /// Serialize a <see cref="double3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref double3 value);

        /// <summary>
        /// Serialize a <see cref="double3x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref double3x2 value);

        /// <summary>
        /// Serialize a <see cref="double3x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref double3x3 value);

        /// <summary>
        /// Serialize a <see cref="double3x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref double3x4 value);

        /// <summary>
        /// Serialize a <see cref="double4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref double4 value);

        /// <summary>
        /// Serialize a <see cref="double4x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref double4x2 value);

        /// <summary>
        /// Serialize a <see cref="double4x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref double4x3 value);

        /// <summary>
        /// Serialize a <see cref="double4x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref double4x4 value);

        /// <summary>
        /// Serialize a <see cref="float2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref float2 value);

        /// <summary>
        /// Serialize a <see cref="float2x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref float2x2 value);

        /// <summary>
        /// Serialize a <see cref="float2x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref float2x3 value);

        /// <summary>
        /// Serialize a <see cref="float2x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref float2x4 value);

        /// <summary>
        /// Serialize a <see cref="float3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref float3 value);

        /// <summary>
        /// Serialize a <see cref="float3x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref float3x2 value);

        /// <summary>
        /// Serialize a <see cref="float3x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref float3x3 value);

        /// <summary>
        /// Serialize a <see cref="float3x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref float3x4 value);

        /// <summary>
        /// Serialize a <see cref="float4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref float4 value);

        /// <summary>
        /// Serialize a <see cref="float4x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref float4x2 value);

        /// <summary>
        /// Serialize a <see cref="float4x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref float4x3 value);

        /// <summary>
        /// Serialize a <see cref="float4x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref float4x4 value);

        /// <summary>
        /// Serialize a <see cref="half"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref half value);

        /// <summary>
        /// Serialize a <see cref="half2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref half2 value);

        /// <summary>
        /// Serialize a <see cref="half3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref half3 value);

        /// <summary>
        /// Serialize a <see cref="half4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref half4 value);

        /// <summary>
        /// Serialize a <see cref="int2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref int2 value);

        /// <summary>
        /// Serialize a <see cref="int2x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref int2x2 value);

        /// <summary>
        /// Serialize a <see cref="int2x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref int2x3 value);

        /// <summary>
        /// Serialize a <see cref="int2x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref int2x4 value);

        /// <summary>
        /// Serialize a <see cref="int3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref int3 value);

        /// <summary>
        /// Serialize a <see cref="int3x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref int3x2 value);

        /// <summary>
        /// Serialize a <see cref="int3x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref int3x3 value);

        /// <summary>
        /// Serialize a <see cref="int3x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref int3x4 value);

        /// <summary>
        /// Serialize a <see cref="int4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref int4 value);

        /// <summary>
        /// Serialize a <see cref="int4x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref int4x2 value);

        /// <summary>
        /// Serialize a <see cref="int4x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref int4x3 value);

        /// <summary>
        /// Serialize a <see cref="int4x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref int4x4 value);

        /// <summary>
        /// Serialize a <see cref="quaternion"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref quaternion value);

        /// <summary>
        /// Serialize a <see cref="uint2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref uint2 value);

        /// <summary>
        /// Serialize a <see cref="uint2x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref uint2x2 value);

        /// <summary>
        /// Serialize a <see cref="uint2x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref uint2x3 value);

        /// <summary>
        /// Serialize a <see cref="uint2x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref uint2x4 value);

        /// <summary>
        /// Serialize a <see cref="uint3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref uint3 value);

        /// <summary>
        /// Serialize a <see cref="uint3x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref uint3x2 value);

        /// <summary>
        /// Serialize a <see cref="uint3x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref uint3x3 value);

        /// <summary>
        /// Serialize a <see cref="uint3x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref uint3x4 value);

        /// <summary>
        /// Serialize a <see cref="uint4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref uint4 value);

        /// <summary>
        /// Serialize a <see cref="uint4x2"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref uint4x2 value);

        /// <summary>
        /// Serialize a <see cref="uint4x3"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref uint4x3 value);

        /// <summary>
        /// Serialize a <see cref="uint4x4"/>, "pre-checked", which skips buffer checks.
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// </summary>
        /// <param name="value">The value to read/write</param>
        void SerializeValuePreChecked(ref uint4x4 value);
    }
}
