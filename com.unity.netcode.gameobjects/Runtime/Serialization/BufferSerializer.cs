using System;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

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

        /// <summary>
        /// Read or write a <see cref="bool2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref bool2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool2x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref bool2x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool2x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref bool2x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool2x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref bool2x4 value) => m_Implementation.SerializeValue(ref value);


        /// <summary>
        /// Read or write a <see cref="bool3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref bool3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool3x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref bool3x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool3x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref bool3x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool3x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref bool3x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref bool4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool4x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref bool4x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool4x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref bool4x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool4x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref bool4x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref double2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double2x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref double2x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double2x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref double2x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double2x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref double2x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref double3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double3x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref double3x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double3x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref double3x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double3x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref double3x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref double4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double4x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref double4x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double4x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref double4x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double4x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref double4x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref float2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float2x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref float2x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float2x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref float2x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float2x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref float2x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref float3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float3x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref float3x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float3x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref float3x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float3x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref float3x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref float4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float4x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref float4x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float4x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref float4x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float4x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref float4x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="half"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref half value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="half2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref half2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="half3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref half3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="half4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref half4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref int2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int2x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref int2x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int2x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref int2x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int2x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref int2x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref int3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int3x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref int3x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int3x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref int3x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int3x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref int3x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref int4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int4x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref int4x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int4x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref int4x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int4x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref int4x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="quaternion"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref quaternion value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref uint2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint2x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref uint2x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint2x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref uint2x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint2x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref uint2x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref uint3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint3x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref uint3x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint3x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref uint3x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint3x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref uint3x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref uint4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint4x2"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref uint4x2 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint4x3"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref uint4x3 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint4x4"/>
        /// </summary>
        /// <param name="value">the value to read/write</param>
        public void SerializeValue(ref uint4x4 value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref bool2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool2x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref bool2x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool2x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref bool2x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool2x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref bool2x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref bool3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool3x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref bool3x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool3x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref bool3x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool3x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref bool3x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref bool4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool4x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref bool4x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool4x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref bool4x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="bool4x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref bool4x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref double2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double2x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref double2x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double2x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref double2x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double2x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref double2x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref double3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double3x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref double3x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double3x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref double3x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double3x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref double3x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref double4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double4x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref double4x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double4x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref double4x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="double4x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref double4x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref float2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float2x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref float2x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float2x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref float2x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float2x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref float2x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref float3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float3x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref float3x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float3x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref float3x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float3x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref float3x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref float4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float4x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref float4x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float4x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref float4x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="float4x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref float4x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="half"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref half[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="half2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref half2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="half3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref half3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="half4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref half4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref int2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int2x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref int2x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int2x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref int2x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int2x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref int2x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref int3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int3x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref int3x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int3x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref int3x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int3x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref int3x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref int4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int4x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref int4x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int4x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref int4x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="int4x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref int4x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="quaternion"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref quaternion[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref uint2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint2x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref uint2x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint2x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref uint2x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint2x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref uint2x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref uint3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint3x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref uint3x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint3x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref uint3x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint3x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref uint3x4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref uint4[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint4x2"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref uint4x2[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint4x3"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref uint4x3[] value) => m_Implementation.SerializeValue(ref value);

        /// <summary>
        /// Read or write a <see cref="uint4x4"/> array
        /// </summary>
        /// <param name="value">the values to read/write</param>
        public void SerializeValue(ref uint4x4[] value) => m_Implementation.SerializeValue(ref value);

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
        /// <param name="amount"></param>
        /// <returns></returns>
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
        /// Serialize a Vector4Array, "pre-checked", which skips buffer checks.
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

        /// <summary>
        /// Serialize a <see cref="bool2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref bool2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool2x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref bool2x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool2x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref bool2x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool2x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref bool2x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref bool3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool3x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref bool3x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool3x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref bool3x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool3x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref bool3x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref bool4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool4x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref bool4x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool4x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref bool4x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool4x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref bool4x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref double2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double2x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref double2x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double2x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref double2x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double2x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref double2x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref double3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double3x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref double3x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double3x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref double3x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double3x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref double3x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref double4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double4x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref double4x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double4x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref double4x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double4x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref double4x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref float2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float2x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref float2x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float2x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref float2x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float2x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref float2x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref float3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float3x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref float3x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float3x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref float3x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float3x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref float3x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref float4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float4x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref float4x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float4x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref float4x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float4x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref float4x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="half"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref half value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="half2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref half2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="half3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref half3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="half4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref half4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref int2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int2x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref int2x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int2x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref int2x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int2x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref int2x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref int3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int3x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref int3x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int3x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref int3x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int3x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref int3x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref int4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int4x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref int4x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int4x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref int4x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int4x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref int4x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="quaternion"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref quaternion value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref uint2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint2x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref uint2x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint2x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref uint2x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint2x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref uint2x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref uint3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint3x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref uint3x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint3x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref uint3x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint3x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref uint3x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref uint4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint4x2"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref uint4x2 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint4x3"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref uint4x3 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint4x4"/>
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the value to read/write</param>
        public void SerializeValuePreChecked(ref uint4x4 value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref bool2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool2x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref bool2x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool2x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref bool2x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool2x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref bool2x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref bool3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool3x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref bool3x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool3x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref bool3x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool3x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref bool3x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref bool4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool4x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref bool4x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool4x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref bool4x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="bool4x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref bool4x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref double2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double2x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref double2x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double2x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref double2x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double2x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref double2x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref double3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double3x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref double3x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double3x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref double3x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double3x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref double3x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref double4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double4x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref double4x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double4x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref double4x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="double4x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref double4x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref float2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float2x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref float2x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float2x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref float2x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float2x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref float2x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref float3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float3x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref float3x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float3x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref float3x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float3x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref float3x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref float4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float4x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref float4x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float4x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref float4x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="float4x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref float4x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="half"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref half[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="half2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref half2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="half3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref half3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="half4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref half4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref int2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int2x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref int2x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int2x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref int2x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int2x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref int2x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref int3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int3x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref int3x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int3x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref int3x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int3x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref int3x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref int4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int4x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref int4x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int4x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref int4x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="int4x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref int4x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="quaternion"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref quaternion[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref uint2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint2x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref uint2x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint2x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref uint2x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint2x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref uint2x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref uint3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint3x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref uint3x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint3x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref uint3x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint3x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref uint3x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref uint4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint4x2"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref uint4x2[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint4x3"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref uint4x3[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        /// <summary>
        /// Serialize a <see cref="uint4x4"/> array
        /// In debug and editor builds, a check is made to ensure you've called "PreCheck" before
        /// calling this. In release builds, calling this without calling "PreCheck" may read or write
        /// past the end of the buffer, which will cause memory corruption and undefined behavior.
        /// <remarks>
        /// This method does no buffer checks and assumes you have called <see cref="PreCheck"/> before invoking.
        /// </remarks>
        /// <param name="value">the values to read/write</param>
        public void SerializeValuePreChecked(ref uint4x4[] value) => m_Implementation.SerializeValuePreChecked(ref value);

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
