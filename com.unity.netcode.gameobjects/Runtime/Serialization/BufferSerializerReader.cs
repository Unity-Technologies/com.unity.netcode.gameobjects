using System;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

namespace Unity.Netcode
{
    internal struct BufferSerializerReader : IReaderWriter
    {
        private FastBufferReader m_Reader;

        public BufferSerializerReader(FastBufferReader reader)
        {
            m_Reader = reader;
        }

        public bool IsReader => true;
        public bool IsWriter => false;

        public FastBufferReader GetFastBufferReader()
        {
            return m_Reader;
        }

        public FastBufferWriter GetFastBufferWriter()
        {
            throw new InvalidOperationException("Cannot retrieve a FastBufferWriter from a serializer where IsWriter = false");
        }

        public void SerializeValue(ref string s, bool oneByteChars = false) => m_Reader.ReadValueSafe(out s, oneByteChars);
        public void SerializeValue(ref byte value) => m_Reader.ReadByteSafe(out value);
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Reader.ReadValueSafe(out value);
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Reader.ReadValueSafe(out value);
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Reader.ReadValueSafe(out value);
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Reader.ReadValueSafe(out value);
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Reader.ReadValueSafe(out value);
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Reader.ReadValueSafe(out value);
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForNetworkSerializable unused = default) where T : INetworkSerializable, new() => m_Reader.ReadNetworkSerializableInPlace(ref value);
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForNetworkSerializable unused = default) where T : INetworkSerializable, new() => m_Reader.ReadValue(out value);

        public void SerializeValue<T>(ref T value, FastBufferWriter.ForFixedStrings unused = default)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes => m_Reader.ReadValueSafe(out value);

        public void SerializeValue(ref Vector2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Vector2[] value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Vector3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Vector3[] value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Vector2Int value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Vector2Int[] value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Vector3Int value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Vector3Int[] value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Vector4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Vector4[] value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Quaternion value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Quaternion[] value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Color value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Color[] value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Color32 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Color32[] value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Ray value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Ray[] value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Ray2D value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref Ray2D[] value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref bool2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref bool2x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref bool2x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref bool2x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref bool3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref bool3x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref bool3x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref bool3x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref bool4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref bool4x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref bool4x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref bool4x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref double2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref double2x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref double2x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref double2x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref double3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref double3x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref double3x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref double3x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref double4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref double4x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref double4x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref double4x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref float2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref float2x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref float2x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref float2x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref float3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref float3x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref float3x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref float3x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref float4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref float4x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref float4x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref float4x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref half value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref half2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref half3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref half4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref int2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref int2x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref int2x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref int2x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref int3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref int3x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref int3x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref int3x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref int4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref int4x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref int4x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref int4x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref quaternion value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref uint2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref uint2x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref uint2x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref uint2x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref uint3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref uint3x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref uint3x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref uint3x4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref uint4 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref uint4x2 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref uint4x3 value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref uint4x4 value) => m_Reader.ReadValueSafe(out value);

        public void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable, new() => m_Reader.ReadNetworkSerializable(out value);

        public bool PreCheck(int amount)
        {
            return m_Reader.TryBeginRead(amount);
        }

        public void SerializeValuePreChecked(ref string s, bool oneByteChars = false) => m_Reader.ReadValue(out s, oneByteChars);
        public void SerializeValuePreChecked(ref byte value) => m_Reader.ReadByte(out value);
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForFixedStrings unused = default)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Vector2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Vector2[] value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Vector3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Vector3[] value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Vector2Int value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Vector2Int[] value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Vector3Int value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Vector3Int[] value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Vector4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Vector4[] value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Quaternion value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Quaternion[] value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Color value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Color[] value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Color32 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Color32[] value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Ray value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Ray[] value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Ray2D value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref Ray2D[] value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref bool2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref bool2x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref bool2x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref bool2x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref bool3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref bool3x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref bool3x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref bool3x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref bool4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref bool4x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref bool4x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref bool4x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref double2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref double2x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref double2x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref double2x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref double3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref double3x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref double3x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref double3x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref double4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref double4x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref double4x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref double4x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref float2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref float2x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref float2x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref float2x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref float3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref float3x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref float3x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref float3x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref float4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref float4x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref float4x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref float4x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref half value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref half2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref half3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref half4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref int2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref int2x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref int2x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref int2x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref int3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref int3x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref int3x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref int3x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref int4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref int4x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref int4x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref int4x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref quaternion value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref uint2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref uint2x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref uint2x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref uint2x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref uint3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref uint3x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref uint3x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref uint3x4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref uint4 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref uint4x2 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref uint4x3 value) => m_Reader.ReadValue(out value);
        public void SerializeValuePreChecked(ref uint4x4 value) => m_Reader.ReadValue(out value);
    }
}
