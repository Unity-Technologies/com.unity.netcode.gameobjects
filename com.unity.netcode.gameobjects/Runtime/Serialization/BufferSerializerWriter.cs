using System;
using Unity.Collections;
using UnityEngine;

namespace Unity.Netcode
{
    internal struct BufferSerializerWriter : IReaderWriter
    {
        private FastBufferWriter m_Writer;

        public BufferSerializerWriter(FastBufferWriter writer)
        {
            m_Writer = writer;
        }

        public bool IsReader => false;
        public bool IsWriter => true;

        public FastBufferReader GetFastBufferReader()
        {
            throw new InvalidOperationException("Cannot retrieve a FastBufferReader from a serializer where IsReader = false");
        }

        public FastBufferWriter GetFastBufferWriter()
        {
            return m_Writer;
        }

        public void SerializeValue(ref string s, bool oneByteChars = false) => m_Writer.WriteValueSafe(s, oneByteChars);
        public void SerializeValue(ref byte value) => m_Writer.WriteByteSafe(value);
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Writer.WriteValueSafe(value);
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Writer.WriteValueSafe(value);
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Writer.WriteValueSafe(value);
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Writer.WriteValueSafe(value);
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Writer.WriteValueSafe(value);
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Writer.WriteValueSafe(value);
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForNetworkSerializable unused = default) where T : INetworkSerializable, new() => m_Writer.WriteValue(value);
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForNetworkSerializable unused = default) where T : INetworkSerializable, new() => m_Writer.WriteValue(value);
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForFixedStrings unused = default)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes => m_Writer.WriteValueSafe(value);

        public void SerializeValue(ref Vector2 value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Vector2[] value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Vector3 value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Vector3[] value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Vector2Int value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Vector2Int[] value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Vector3Int value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Vector3Int[] value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Vector4 value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Vector4[] value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Quaternion value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Quaternion[] value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Color value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Color[] value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Color32 value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Color32[] value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Ray value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Ray[] value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Ray2D value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref Ray2D[] value) => m_Writer.WriteValueSafe(value);

        public void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable, new()
        {
            m_Writer.WriteNetworkSerializable(value);
        }

        public bool PreCheck(int amount)
        {
            return m_Writer.TryBeginWrite(amount);
        }

        public void SerializeValuePreChecked(ref string s, bool oneByteChars = false) => m_Writer.WriteValue(s, oneByteChars);
        public void SerializeValuePreChecked(ref byte value) => m_Writer.WriteByte(value);
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Writer.WriteValue(value);

        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForFixedStrings unused = default)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes => m_Writer.WriteValue(value);

        public void SerializeValuePreChecked(ref Vector2 value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Vector2[] value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Vector3 value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Vector3[] value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Vector2Int value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Vector2Int[] value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Vector3Int value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Vector3Int[] value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Vector4 value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Vector4[] value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Quaternion value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Quaternion[] value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Color value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Color[] value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Color32 value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Color32[] value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Ray value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Ray[] value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Ray2D value) => m_Writer.WriteValue(value);
        public void SerializeValuePreChecked(ref Ray2D[] value) => m_Writer.WriteValue(value);
    }
}
