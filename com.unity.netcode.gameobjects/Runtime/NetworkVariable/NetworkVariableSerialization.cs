using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace Unity.Netcode
{
    interface INetworkVariableSerializer<T>
    {
        public void Write(FastBufferWriter writer, ref T value);
        public void Read(FastBufferReader reader, out T value);
    }

    internal class UnmanagedTypeSerializer<T> : INetworkVariableSerializer<T> where T : unmanaged
    {
        public void Write(FastBufferWriter writer, ref T value)
        {
            writer.WriteUnmanagedSafe(value);
        }
        public void Read(FastBufferReader reader, out T value)
        {
            reader.ReadUnmanagedSafe(out value);
        }
    }

    internal class FixedStringSerializer<T> : INetworkVariableSerializer<T> where T : unmanaged
    {
        internal delegate int GetLengthDelegate(ref T value);
        internal delegate void SetLengthDelegate(ref T value, int length);
        internal unsafe delegate byte* GetUnsafePtrDelegate(ref T value);

        internal GetLengthDelegate GetLength;
        internal SetLengthDelegate SetLength;
        internal GetUnsafePtrDelegate GetUnsafePtr;

        public unsafe void Write(FastBufferWriter writer, ref T value)
        {
            int length = GetLength(ref value);
            byte* data = GetUnsafePtr(ref value);
            writer.WriteUnmanagedSafe(length);
            writer.WriteBytesSafe(data, length);
        }
        public unsafe void Read(FastBufferReader reader, out T value)
        {
            value = new T();
            reader.ReadValueSafe(out int length);
            SetLength(ref value, length);
            reader.ReadBytesSafe(GetUnsafePtr(ref value), length);
        }
    }

    internal class NetworkSerializableSerializer<T> : INetworkVariableSerializer<T> where T : unmanaged
    {
        internal delegate void WriteValueDelegate(ref T value, BufferSerializer<BufferSerializerWriter> serializer);
        internal delegate void ReadValueDelegate(ref T value, BufferSerializer<BufferSerializerReader> serializer);

        internal WriteValueDelegate WriteValue;
        internal ReadValueDelegate ReadValue;
        public void Write(FastBufferWriter writer, ref T value)
        {
            var bufferSerializer = new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
            WriteValue(ref value, bufferSerializer);
        }
        public void Read(FastBufferReader reader, out T value)
        {
            value = new T();
            var bufferSerializer = new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
            ReadValue(ref value, bufferSerializer);
        }
    }

    public class UserNetworkVariableSerializer<T> : INetworkVariableSerializer<T>
    {
        public delegate void WriteValueDelegate(FastBufferWriter writer, ref T value);
        public delegate void ReadValueDelegate(FastBufferReader reader, out T value);

        public static WriteValueDelegate WriteValue;
        public static ReadValueDelegate ReadValue;

        public void Write(FastBufferWriter writer, ref T value)
        {
            if (ReadValue == null || WriteValue == null)
            {
                throw new ArgumentException($"Type {typeof(T).FullName} is not supported by {typeof(NetworkVariable<>).Name}. If this is a type you can change, then either implement {nameof(INetworkSerializable)} or mark it as serializable by memcpy by adding {nameof(INetworkSerializeByMemcpy)} to its interface list. If not, assign serialization code to {nameof(UserNetworkVariableSerializer<T>)}.{nameof(UserNetworkVariableSerializer<T>.WriteValue)} and {nameof(UserNetworkVariableSerializer<T>)}.{nameof(UserNetworkVariableSerializer<T>.ReadValue)}, or if it's serializable by memcpy (contains no pointers), wrap it in {typeof(ForceNetworkSerializeByMemcpy<>).Name}.");
            }
            WriteValue(writer, ref value);
        }
        public void Read(FastBufferReader reader, out T value)
        {
            if (ReadValue == null || WriteValue == null)
            {
                throw new ArgumentException($"Type {typeof(T).FullName} is not supported by {typeof(NetworkVariable<>).Name}. If this is a type you can change, then either implement {nameof(INetworkSerializable)} or mark it as serializable by memcpy by adding {nameof(INetworkSerializeByMemcpy)} to its interface list. If not, assign serialization code to {nameof(UserNetworkVariableSerializer<T>)}.{nameof(UserNetworkVariableSerializer<T>.WriteValue)} and {nameof(UserNetworkVariableSerializer<T>)}.{nameof(UserNetworkVariableSerializer<T>.ReadValue)}, or if it's serializable by memcpy (contains no pointers), wrap it in {typeof(ForceNetworkSerializeByMemcpy<>).Name}.");
            }
            ReadValue(reader, out value);
        }
    }

    internal static class NetworkVariableSerializationTypes
    {
        internal static readonly HashSet<Type> BaseSupportedTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(char),
            typeof(decimal),
            typeof(double),
            typeof(float),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(short),
            typeof(ushort),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector2Int),
            typeof(Vector3Int),
            typeof(Vector4),
            typeof(Quaternion),
            typeof(Color),
            typeof(Color32),
            typeof(Ray),
            typeof(Ray2D)
        };
    }

    /// <summary>
    /// Support methods for reading/writing NetworkVariables
    /// Because there are multiple overloads of WriteValue/ReadValue based on different generic constraints,
    /// but there's no way to achieve the same thing with a class, this includes various read/write delegates
    /// based on which constraints are met by `T`. These constraints are set up by `NetworkVariableHelpers`,
    /// which is invoked by code generated by ILPP during module load.
    /// (As it turns out, IL has support for a module initializer that C# doesn't expose.)
    /// This installs the correct delegate for each `T` to ensure that each type is serialized properly.
    ///
    /// Any type that inherits from `NetworkVariableSerialization<T>` will implicitly result in any `T`
    /// passed to it being picked up and initialized by ILPP.
    ///
    /// The methods here, despite being static, are `protected` specifically to ensure that anything that
    /// wants access to them has to inherit from this base class, thus enabling ILPP to find and initialize it.
    /// </summary>
    [Serializable]
    public static class NetworkVariableSerialization<T> where T : unmanaged
    {
        private static INetworkVariableSerializer<T> s_Serializer = GetSerializer();

        private static INetworkVariableSerializer<T> GetSerializer()
        {
            if (NetworkVariableSerializationTypes.BaseSupportedTypes.Contains(typeof(T)))
            {
                return new UnmanagedTypeSerializer<T>();
            }
            if (typeof(INetworkSerializeByMemcpy).IsAssignableFrom(typeof(T)))
            {
                return new UnmanagedTypeSerializer<T>();
            }

            if (typeof(INetworkSerializable).IsAssignableFrom(typeof(T)))
            {
                var writeMethod = (NetworkSerializableSerializer<T>.WriteValueDelegate)Delegate.CreateDelegate(typeof(NetworkSerializableSerializer<T>.WriteValueDelegate), null, typeof(T).GetMethod(nameof(INetworkSerializable.NetworkSerialize)).MakeGenericMethod(typeof(BufferSerializerWriter)));
                var readMethod = (NetworkSerializableSerializer<T>.ReadValueDelegate)Delegate.CreateDelegate(typeof(NetworkSerializableSerializer<T>.ReadValueDelegate), null, typeof(T).GetMethod(nameof(INetworkSerializable.NetworkSerialize)).MakeGenericMethod(typeof(BufferSerializerReader)));
                return new NetworkSerializableSerializer<T> { WriteValue = writeMethod, ReadValue = readMethod };
            }

            if (typeof(IUTF8Bytes).IsAssignableFrom(typeof(T)) && typeof(INativeList<byte>).IsAssignableFrom(typeof(T)))
            {
                var getLength = (FixedStringSerializer<T>.GetLengthDelegate)Delegate.CreateDelegate(typeof(FixedStringSerializer<T>.GetLengthDelegate), null, typeof(T).GetMethod("get_" + nameof(INativeList<byte>.Length)));
                var setLength = (FixedStringSerializer<T>.SetLengthDelegate)Delegate.CreateDelegate(typeof(FixedStringSerializer<T>.SetLengthDelegate), null, typeof(T).GetMethod("set_" + nameof(INativeList<byte>.Length)));
                var getUnsafePtr = (FixedStringSerializer<T>.GetUnsafePtrDelegate)Delegate.CreateDelegate(typeof(FixedStringSerializer<T>.GetUnsafePtrDelegate), null, typeof(T).GetMethod(nameof(IUTF8Bytes.GetUnsafePtr)));
                return new FixedStringSerializer<T> { GetLength = getLength, SetLength = setLength, GetUnsafePtr = getUnsafePtr };
            }

            return new UserNetworkVariableSerializer<T>();
        }

        internal static void Write(FastBufferWriter writer, ref T value)
        {
            s_Serializer.Write(writer, ref value);
        }

        internal static void Read(FastBufferReader reader, out T value)
        {
            s_Serializer.Read(reader, out value);
        }
    }
}
