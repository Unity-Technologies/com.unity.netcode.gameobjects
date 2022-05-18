using System;
using Unity.Collections;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Two-way serializer wrapping FastBufferReader or FastBufferWriter.
    ///
    /// Implemented as a ref struct for two reasons:
    /// 1. The BufferSerializer cannot outlive the FBR/FBW it wraps or using it will cause a crash
    /// 2. The BufferSerializer must always be passed by reference and can't be copied
    ///
    /// Ref structs help enforce both of those rules: they can't ref live the stack context in which they were
    /// created, and they're always passed by reference no matter what.
    ///
    /// BufferSerializer doesn't wrapp FastBufferReader or FastBufferWriter directly because it can't.
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

        public void SerializeValue(ref string s, bool oneByteChars = false) => m_Implementation.SerializeValue(ref s, oneByteChars);
        public void SerializeValue(ref byte value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Implementation.SerializeValue(ref value);
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Implementation.SerializeValue(ref value);
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Implementation.SerializeValue(ref value);
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Implementation.SerializeValue(ref value);
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Implementation.SerializeValue(ref value);
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Implementation.SerializeValue(ref value);
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForNetworkSerializable unused = default) where T : INetworkSerializable, new() => m_Implementation.SerializeValue(ref value);
        public void SerializeValue<T>(ref T[] value, FastBufferWriter.ForNetworkSerializable unused = default) where T : INetworkSerializable, new() => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Vector2 value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Vector2[] value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Vector3 value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Vector3[] value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Vector2Int value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Vector2Int[] value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Vector3Int value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Vector3Int[] value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Vector4 value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Vector4[] value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Quaternion value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Quaternion[] value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Color value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Color[] value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Color32 value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Color32[] value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Ray value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Ray[] value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Ray2D value) => m_Implementation.SerializeValue(ref value);
        public void SerializeValue(ref Ray2D[] value) => m_Implementation.SerializeValue(ref value);

        // There are many FixedString types, but all of them share the interfaces INativeList<bool> and IUTF8Bytes.
        // INativeList<bool> provides the Length property
        // IUTF8Bytes provides GetUnsafePtr()
        // Those two are necessary to serialize FixedStrings efficiently
        // - otherwise we'd just be memcpying the whole thing even if
        // most of it isn't used.
        public void SerializeValue<T>(ref T value, FastBufferWriter.ForFixedStrings unused = default)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes => m_Implementation.SerializeValue(ref value);

        public void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable, new() => m_Implementation.SerializeNetworkSerializable(ref value);

        public bool PreCheck(int amount)
        {
            return m_Implementation.PreCheck(amount);
        }

        public void SerializeValuePreChecked(ref string s, bool oneByteChars = false) => m_Implementation.SerializeValuePreChecked(ref s, oneByteChars);
        public void SerializeValuePreChecked(ref byte value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForPrimitives unused = default) where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T> => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForEnums unused = default) where T : unmanaged, Enum => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked<T>(ref T[] value, FastBufferWriter.ForStructs unused = default) where T : unmanaged, INetworkSerializeByMemcpy => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Vector2 value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Vector2[] value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Vector3 value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Vector3[] value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Vector2Int value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Vector2Int[] value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Vector3Int value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Vector3Int[] value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Vector4 value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Vector4[] value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Quaternion value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Quaternion[] value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Color value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Color[] value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Color32 value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Color32[] value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Ray value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Ray[] value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Ray2D value) => m_Implementation.SerializeValuePreChecked(ref value);
        public void SerializeValuePreChecked(ref Ray2D[] value) => m_Implementation.SerializeValuePreChecked(ref value);

        // There are many FixedString types, but all of them share the interfaces INativeList<bool> and IUTF8Bytes.
        // INativeList<bool> provides the Length property
        // IUTF8Bytes provides GetUnsafePtr()
        // Those two are necessary to serialize FixedStrings efficiently
        // - otherwise we'd just be memcpying the whole thing even if
        // most of it isn't used.
        public void SerializeValuePreChecked<T>(ref T value, FastBufferWriter.ForFixedStrings unused = default)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes => m_Implementation.SerializeValuePreChecked(ref value);
    }
}
