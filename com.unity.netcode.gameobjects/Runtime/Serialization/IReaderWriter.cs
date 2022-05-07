using System;
using Unity.Collections;
using UnityEngine;

namespace Unity.Netcode
{
    public interface IReaderWriter
    {
        bool IsReader { get; }
        bool IsWriter { get; }

        FastBufferReader GetFastBufferReader();
        FastBufferWriter GetFastBufferWriter();

        void SerializeValue(ref string s, bool oneByteChars = false);
        void SerializeValue(ref byte value);
        void SerializeValue<T>(ref T value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T>;
        void SerializeValue<T>(ref T[] value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T>;
        void SerializeValue<T>(ref T value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum;
        void SerializeValue<T>(ref T[] value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum;
        void SerializeValue<T>(ref T value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy;
        void SerializeValue<T>(ref T[] value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy;
        void SerializeValue<T>(ref T value, FastBufferWriter.ForNetworkSerializable unused = default) where T : INetworkSerializable, new();
        void SerializeValue<T>(ref T[] value, FastBufferWriter.ForNetworkSerializable unused = default) where T : INetworkSerializable, new();
        void SerializeValue(ref Vector2 value);
        void SerializeValue(ref Vector2[] value);
        void SerializeValue(ref Vector3 value);
        void SerializeValue(ref Vector3[] value);
        void SerializeValue(ref Vector2Int value);
        void SerializeValue(ref Vector2Int[] value);
        void SerializeValue(ref Vector3Int value);
        void SerializeValue(ref Vector3Int[] value);
        void SerializeValue(ref Vector4 value);
        void SerializeValue(ref Vector4[] value);
        void SerializeValue(ref Quaternion value);
        void SerializeValue(ref Quaternion[] value);
        void SerializeValue(ref Color value);
        void SerializeValue(ref Color[] value);
        void SerializeValue(ref Color32 value);
        void SerializeValue(ref Color32[] value);
        void SerializeValue(ref Ray value);
        void SerializeValue(ref Ray[] value);
        void SerializeValue(ref Ray2D value);
        void SerializeValue(ref Ray2D[] value);

        void SerializeValue(ref FixedString32Bytes value);
        void SerializeValue(ref FixedString64Bytes value);
        void SerializeValue(ref FixedString128Bytes value);
        void SerializeValue(ref FixedString512Bytes value);
        void SerializeValue(ref FixedString4096Bytes value);

        // Has to have a different name to avoid conflicting with "where T: unmananged"
        void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable, new();

        bool PreCheck(int amount);
        void SerializeValuePreChecked(ref string s, bool oneByteChars = false);
        void SerializeValuePreChecked(ref byte value);
        void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T>;
        void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T>;
        void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum;
        void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum;
        void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy;
        void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy;

        void SerializeValuePreChecked(ref Vector2 value);
        void SerializeValuePreChecked(ref Vector2[] value);
        void SerializeValuePreChecked(ref Vector3 value);
        void SerializeValuePreChecked(ref Vector3[] value);
        void SerializeValuePreChecked(ref Vector2Int value);
        void SerializeValuePreChecked(ref Vector2Int[] value);
        void SerializeValuePreChecked(ref Vector3Int value);
        void SerializeValuePreChecked(ref Vector3Int[] value);
        void SerializeValuePreChecked(ref Vector4 value);
        void SerializeValuePreChecked(ref Vector4[] value);
        void SerializeValuePreChecked(ref Quaternion value);
        void SerializeValuePreChecked(ref Quaternion[] value);
        void SerializeValuePreChecked(ref Color value);
        void SerializeValuePreChecked(ref Color[] value);
        void SerializeValuePreChecked(ref Color32 value);
        void SerializeValuePreChecked(ref Color32[] value);
        void SerializeValuePreChecked(ref Ray value);
        void SerializeValuePreChecked(ref Ray[] value);
        void SerializeValuePreChecked(ref Ray2D value);
        void SerializeValuePreChecked(ref Ray2D[] value);

        void SerializeValuePreChecked(ref FixedString32Bytes value);
        void SerializeValuePreChecked(ref FixedString64Bytes value);
        void SerializeValuePreChecked(ref FixedString128Bytes value);
        void SerializeValuePreChecked(ref FixedString512Bytes value);
        void SerializeValuePreChecked(ref FixedString4096Bytes value);
    }
}
