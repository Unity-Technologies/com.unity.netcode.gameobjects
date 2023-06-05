using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Interface used by NetworkVariables to serialize them
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface INetworkVariableSerializer<T>
    {
        // Write has to be taken by ref here because of INetworkSerializable
        // Open Instance Delegates (pointers to methods without an instance attached to them)
        // require the first parameter passed to them (the instance) to be passed by ref.
        // So foo.Bar() becomes BarDelegate(ref foo);
        // Taking T as an in parameter like we do in other places would require making a copy
        // of it to pass it as a ref parameter.
        public void Write(FastBufferWriter writer, ref T value);
        public void Read(FastBufferReader reader, ref T value);
        internal void ReadWithAllocator(FastBufferReader reader, out T value, Allocator allocator);
        public void Duplicate(in T value, ref T duplicatedValue);
    }

    /// <summary>
    /// Packing serializer for shorts
    /// </summary>
    internal class ShortSerializer : INetworkVariableSerializer<short>
    {
        public void Write(FastBufferWriter writer, ref short value)
        {
            BytePacker.WriteValueBitPacked(writer, value);
        }
        public void Read(FastBufferReader reader, ref short value)
        {
            ByteUnpacker.ReadValueBitPacked(reader, out value);
        }

        void INetworkVariableSerializer<short>.ReadWithAllocator(FastBufferReader reader, out short value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in short value, ref short duplicatedValue)
        {
            duplicatedValue = value;
        }
    }

    /// <summary>
    /// Packing serializer for shorts
    /// </summary>
    internal class UshortSerializer : INetworkVariableSerializer<ushort>
    {
        public void Write(FastBufferWriter writer, ref ushort value)
        {
            BytePacker.WriteValueBitPacked(writer, value);
        }
        public void Read(FastBufferReader reader, ref ushort value)
        {
            ByteUnpacker.ReadValueBitPacked(reader, out value);
        }

        void INetworkVariableSerializer<ushort>.ReadWithAllocator(FastBufferReader reader, out ushort value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in ushort value, ref ushort duplicatedValue)
        {
            duplicatedValue = value;
        }
    }

    /// <summary>
    /// Packing serializer for ints
    /// </summary>
    internal class IntSerializer : INetworkVariableSerializer<int>
    {
        public void Write(FastBufferWriter writer, ref int value)
        {
            BytePacker.WriteValueBitPacked(writer, value);
        }
        public void Read(FastBufferReader reader, ref int value)
        {
            ByteUnpacker.ReadValueBitPacked(reader, out value);
        }

        void INetworkVariableSerializer<int>.ReadWithAllocator(FastBufferReader reader, out int value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in int value, ref int duplicatedValue)
        {
            duplicatedValue = value;
        }
    }

    /// <summary>
    /// Packing serializer for ints
    /// </summary>
    internal class UintSerializer : INetworkVariableSerializer<uint>
    {
        public void Write(FastBufferWriter writer, ref uint value)
        {
            BytePacker.WriteValueBitPacked(writer, value);
        }
        public void Read(FastBufferReader reader, ref uint value)
        {
            ByteUnpacker.ReadValueBitPacked(reader, out value);
        }

        void INetworkVariableSerializer<uint>.ReadWithAllocator(FastBufferReader reader, out uint value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in uint value, ref uint duplicatedValue)
        {
            duplicatedValue = value;
        }
    }

    /// <summary>
    /// Packing serializer for longs
    /// </summary>
    internal class LongSerializer : INetworkVariableSerializer<long>
    {
        public void Write(FastBufferWriter writer, ref long value)
        {
            BytePacker.WriteValueBitPacked(writer, value);
        }
        public void Read(FastBufferReader reader, ref long value)
        {
            ByteUnpacker.ReadValueBitPacked(reader, out value);
        }

        void INetworkVariableSerializer<long>.ReadWithAllocator(FastBufferReader reader, out long value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in long value, ref long duplicatedValue)
        {
            duplicatedValue = value;
        }
    }

    /// <summary>
    /// Packing serializer for longs
    /// </summary>
    internal class UlongSerializer : INetworkVariableSerializer<ulong>
    {
        public void Write(FastBufferWriter writer, ref ulong value)
        {
            BytePacker.WriteValueBitPacked(writer, value);
        }
        public void Read(FastBufferReader reader, ref ulong value)
        {
            ByteUnpacker.ReadValueBitPacked(reader, out value);
        }

        void INetworkVariableSerializer<ulong>.ReadWithAllocator(FastBufferReader reader, out ulong value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in ulong value, ref ulong duplicatedValue)
        {
            duplicatedValue = value;
        }
    }

    /// <summary>
    /// Basic serializer for unmanaged types.
    /// This covers primitives, built-in unity types, and IForceSerializeByMemcpy
    /// Since all of those ultimately end up calling WriteUnmanagedSafe, this simplifies things
    /// by calling that directly - thus preventing us from having to have a specific T that meets
    /// the specific constraints that the various generic WriteValue calls require.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class UnmanagedTypeSerializer<T> : INetworkVariableSerializer<T> where T : unmanaged
    {
        public void Write(FastBufferWriter writer, ref T value)
        {
            writer.WriteUnmanagedSafe(value);
        }
        public void Read(FastBufferReader reader, ref T value)
        {
            reader.ReadUnmanagedSafe(out value);
        }

        void INetworkVariableSerializer<T>.ReadWithAllocator(FastBufferReader reader, out T value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in T value, ref T duplicatedValue)
        {
            duplicatedValue = value;
        }
    }

    internal class UnmanagedArraySerializer<T> : INetworkVariableSerializer<NativeArray<T>> where T : unmanaged
    {
        public void Write(FastBufferWriter writer, ref NativeArray<T> value)
        {
            writer.WriteUnmanagedSafe(value);
        }
        public void Read(FastBufferReader reader, ref NativeArray<T> value)
        {
            value.Dispose();
            reader.ReadUnmanagedSafe(out value, Allocator.Persistent);
        }

        void INetworkVariableSerializer<NativeArray<T>>.ReadWithAllocator(FastBufferReader reader, out NativeArray<T> value, Allocator allocator)
        {
            reader.ReadUnmanagedSafe(out value, allocator);
        }

        public void Duplicate(in NativeArray<T> value, ref NativeArray<T> duplicatedValue)
        {
            if (!duplicatedValue.IsCreated || duplicatedValue.Length != value.Length)
            {
                if (duplicatedValue.IsCreated)
                {
                    duplicatedValue.Dispose();
                }

                duplicatedValue = new NativeArray<T>(value.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            duplicatedValue.CopyFrom(value);
        }
    }

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
    internal class UnmanagedListSerializer<T> : INetworkVariableSerializer<NativeList<T>> where T : unmanaged
    {
        public void Write(FastBufferWriter writer, ref NativeList<T> value)
        {
            writer.WriteUnmanagedSafe(value);
        }
        public void Read(FastBufferReader reader, ref NativeList<T> value)
        {
            reader.ReadUnmanagedSafeInPlace(ref value);
        }

        void INetworkVariableSerializer<NativeList<T>>.ReadWithAllocator(FastBufferReader reader, out NativeList<T> value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in NativeList<T> value, ref NativeList<T> duplicatedValue)
        {
            if (!duplicatedValue.IsCreated)
            {
                duplicatedValue = new NativeList<T>(value.Length, Allocator.Persistent);
            }
            else if (value.Length != duplicatedValue.Length)
            {
                duplicatedValue.ResizeUninitialized(value.Length);
            }

            duplicatedValue.CopyFrom(value);
        }
    }
#endif

    /// <summary>
    /// Serializer for FixedStrings
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class FixedStringSerializer<T> : INetworkVariableSerializer<T> where T : unmanaged, INativeList<byte>, IUTF8Bytes
    {
        public void Write(FastBufferWriter writer, ref T value)
        {
            writer.WriteValueSafe(value);
        }
        public void Read(FastBufferReader reader, ref T value)
        {
            reader.ReadValueSafeInPlace(ref value);
        }

        void INetworkVariableSerializer<T>.ReadWithAllocator(FastBufferReader reader, out T value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in T value, ref T duplicatedValue)
        {
            duplicatedValue = value;
        }
    }

    /// <summary>
    /// Serializer for FixedStrings
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class FixedStringArraySerializer<T> : INetworkVariableSerializer<NativeArray<T>> where T : unmanaged, INativeList<byte>, IUTF8Bytes
    {
        public void Write(FastBufferWriter writer, ref NativeArray<T> value)
        {
            writer.WriteValueSafe(value);
        }
        public void Read(FastBufferReader reader, ref NativeArray<T> value)
        {
            value.Dispose();
            reader.ReadValueSafe(out value, Allocator.Persistent);
        }

        void INetworkVariableSerializer<NativeArray<T>>.ReadWithAllocator(FastBufferReader reader, out NativeArray<T> value, Allocator allocator)
        {
            reader.ReadValueSafe(out value, allocator);
        }

        public void Duplicate(in NativeArray<T> value, ref NativeArray<T> duplicatedValue)
        {
            if (!duplicatedValue.IsCreated || duplicatedValue.Length != value.Length)
            {
                if (duplicatedValue.IsCreated)
                {
                    duplicatedValue.Dispose();
                }

                duplicatedValue = new NativeArray<T>(value.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            duplicatedValue.CopyFrom(value);
        }
    }

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
    /// <summary>
    /// Serializer for FixedStrings
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class FixedStringListSerializer<T> : INetworkVariableSerializer<NativeList<T>> where T : unmanaged, INativeList<byte>, IUTF8Bytes
    {
        public void Write(FastBufferWriter writer, ref NativeList<T> value)
        {
            writer.WriteValueSafe(value);
        }
        public void Read(FastBufferReader reader, ref NativeList<T> value)
        {
            reader.ReadValueSafeInPlace(ref value);
        }

        void INetworkVariableSerializer<NativeList<T>>.ReadWithAllocator(FastBufferReader reader, out NativeList<T> value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in NativeList<T> value, ref NativeList<T> duplicatedValue)
        {
            if (!duplicatedValue.IsCreated)
            {
                duplicatedValue = new NativeList<T>(value.Length, Allocator.Persistent);
            }
            else if (value.Length != duplicatedValue.Length)
            {
                duplicatedValue.ResizeUninitialized(value.Length);
            }

            duplicatedValue.CopyFrom(value);
        }
    }
#endif

    /// <summary>
    /// Serializer for unmanaged INetworkSerializable types
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class UnmanagedNetworkSerializableSerializer<T> : INetworkVariableSerializer<T> where T : unmanaged, INetworkSerializable
    {
        public void Write(FastBufferWriter writer, ref T value)
        {
            var bufferSerializer = new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
            value.NetworkSerialize(bufferSerializer);
        }
        public void Read(FastBufferReader reader, ref T value)
        {
            var bufferSerializer = new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
            value.NetworkSerialize(bufferSerializer);
        }

        void INetworkVariableSerializer<T>.ReadWithAllocator(FastBufferReader reader, out T value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in T value, ref T duplicatedValue)
        {
            duplicatedValue = value;
        }
    }

    /// <summary>
    /// Serializer for unmanaged INetworkSerializable types
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class UnmanagedNetworkSerializableArraySerializer<T> : INetworkVariableSerializer<NativeArray<T>> where T : unmanaged, INetworkSerializable
    {
        public void Write(FastBufferWriter writer, ref NativeArray<T> value)
        {
            writer.WriteNetworkSerializable(value);
        }
        public void Read(FastBufferReader reader, ref NativeArray<T> value)
        {
            value.Dispose();
            reader.ReadNetworkSerializable(out value, Allocator.Persistent);
        }

        void INetworkVariableSerializer<NativeArray<T>>.ReadWithAllocator(FastBufferReader reader, out NativeArray<T> value, Allocator allocator)
        {
            reader.ReadNetworkSerializable(out value, allocator);
        }

        public void Duplicate(in NativeArray<T> value, ref NativeArray<T> duplicatedValue)
        {
            if (!duplicatedValue.IsCreated || duplicatedValue.Length != value.Length)
            {
                if (duplicatedValue.IsCreated)
                {
                    duplicatedValue.Dispose();
                }

                duplicatedValue = new NativeArray<T>(value.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            duplicatedValue.CopyFrom(value);
        }
    }

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
    /// <summary>
    /// Serializer for unmanaged INetworkSerializable types
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class UnmanagedNetworkSerializableListSerializer<T> : INetworkVariableSerializer<NativeList<T>> where T : unmanaged, INetworkSerializable
    {
        public void Write(FastBufferWriter writer, ref NativeList<T> value)
        {
            writer.WriteNetworkSerializable(value);
        }
        public void Read(FastBufferReader reader, ref NativeList<T> value)
        {
            reader.ReadNetworkSerializableInPlace(ref value);
        }

        void INetworkVariableSerializer<NativeList<T>>.ReadWithAllocator(FastBufferReader reader, out NativeList<T> value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in NativeList<T> value, ref NativeList<T> duplicatedValue)
        {
            if (!duplicatedValue.IsCreated)
            {
                duplicatedValue = new NativeList<T>(value.Length, Allocator.Persistent);
            }
            else if (value.Length != duplicatedValue.Length)
            {
                duplicatedValue.ResizeUninitialized(value.Length);
            }

            duplicatedValue.CopyFrom(value);
        }
    }
#endif

    /// <summary>
    /// Serializer for managed INetworkSerializable types, which differs from the unmanaged implementation in that it
    /// has to be null-aware
    /// <typeparam name="T"></typeparam>
    internal class ManagedNetworkSerializableSerializer<T> : INetworkVariableSerializer<T> where T : class, INetworkSerializable, new()
    {
        public void Write(FastBufferWriter writer, ref T value)
        {
            var bufferSerializer = new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
            bool isNull = (value == null);
            bufferSerializer.SerializeValue(ref isNull);
            if (!isNull)
            {
                value.NetworkSerialize(bufferSerializer);
            }
        }
        public void Read(FastBufferReader reader, ref T value)
        {
            var bufferSerializer = new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
            bool isNull = false;
            bufferSerializer.SerializeValue(ref isNull);
            if (isNull)
            {
                value = null;
            }
            else
            {
                if (value == null)
                {
                    value = new T();
                }
                value.NetworkSerialize(bufferSerializer);
            }
        }

        void INetworkVariableSerializer<T>.ReadWithAllocator(FastBufferReader reader, out T value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in T value, ref T duplicatedValue)
        {
            using var writer = new FastBufferWriter(256, Allocator.Temp);
            var refValue = value;
            Write(writer, ref refValue);

            using var reader = new FastBufferReader(writer, Allocator.None);
            Read(reader, ref duplicatedValue);
        }
    }

    /// <summary>
    /// This class is used to register user serialization with NetworkVariables for types
    /// that are serialized via user serialization, such as with FastBufferReader and FastBufferWriter
    /// extension methods. Finding those methods isn't achievable efficiently at runtime, so this allows
    /// users to tell NetworkVariable about those extension methods (or simply pass in a lambda)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UserNetworkVariableSerialization<T>
    {
        /// <summary>
        /// The write value delegate handler definition
        /// </summary>
        /// <param name="writer">The <see cref="FastBufferWriter"/> to write the value of type `T`</param>
        /// <param name="value">The value of type `T` to be written</param>
        public delegate void WriteValueDelegate(FastBufferWriter writer, in T value);

        /// <summary>
        /// The read value delegate handler definition
        /// </summary>
        /// <param name="reader">The <see cref="FastBufferReader"/> to read the value of type `T`</param>
        /// <param name="value">The value of type `T` to be read</param>
        public delegate void ReadValueDelegate(FastBufferReader reader, out T value);

        /// <summary>
        /// The read value delegate handler definition
        /// </summary>
        /// <param name="reader">The <see cref="FastBufferReader"/> to read the value of type `T`</param>
        /// <param name="value">The value of type `T` to be read</param>
        public delegate void DuplicateValueDelegate(in T value, ref T duplicatedValue);

        /// <summary>
        /// Callback to write a value
        /// </summary>
        public static WriteValueDelegate WriteValue;

        /// <summary>
        /// Callback to read a value
        /// </summary>
        public static ReadValueDelegate ReadValue;

        /// <summary>
        /// Callback to create a duplicate of a value, used to check for dirty status.
        /// </summary>
        public static DuplicateValueDelegate DuplicateValue;
    }

    /// <summary>
    /// This class is instantiated for types that we can't determine ahead of time are serializable - types
    /// that don't meet any of the constraints for methods that are available on FastBufferReader and
    /// FastBufferWriter. These types may or may not be serializable through extension methods. To ensure
    /// the user has time to pass in the delegates to UserNetworkVariableSerialization, the existence
    /// of user serialization isn't checked until it's used, so if no serialization is provided, this
    /// will throw an exception when an object containing the relevant NetworkVariable is spawned.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class FallbackSerializer<T> : INetworkVariableSerializer<T>
    {
        public void Write(FastBufferWriter writer, ref T value)
        {
            if (UserNetworkVariableSerialization<T>.ReadValue == null || UserNetworkVariableSerialization<T>.WriteValue == null || UserNetworkVariableSerialization<T>.DuplicateValue == null)
            {
                throw new ArgumentException($"Type {typeof(T).FullName} is not supported by {typeof(NetworkVariable<>).Name}. If this is a type you can change, then either implement {nameof(INetworkSerializable)} or mark it as serializable by memcpy by adding {nameof(INetworkSerializeByMemcpy)} to its interface list. If not, assign serialization code to {nameof(UserNetworkVariableSerialization<T>)}.{nameof(UserNetworkVariableSerialization<T>.WriteValue)}, {nameof(UserNetworkVariableSerialization<T>)}.{nameof(UserNetworkVariableSerialization<T>.ReadValue)}, and {nameof(UserNetworkVariableSerialization<T>)}.{nameof(UserNetworkVariableSerialization<T>.DuplicateValue)}, or if it's serializable by memcpy (contains no pointers), wrap it in {typeof(ForceNetworkSerializeByMemcpy<>).Name}.");
            }
            UserNetworkVariableSerialization<T>.WriteValue(writer, value);
        }
        public void Read(FastBufferReader reader, ref T value)
        {
            if (UserNetworkVariableSerialization<T>.ReadValue == null || UserNetworkVariableSerialization<T>.WriteValue == null || UserNetworkVariableSerialization<T>.DuplicateValue == null)
            {
                throw new ArgumentException($"Type {typeof(T).FullName} is not supported by {typeof(NetworkVariable<>).Name}. If this is a type you can change, then either implement {nameof(INetworkSerializable)} or mark it as serializable by memcpy by adding {nameof(INetworkSerializeByMemcpy)} to its interface list. If not, assign serialization code to {nameof(UserNetworkVariableSerialization<T>)}.{nameof(UserNetworkVariableSerialization<T>.WriteValue)}, {nameof(UserNetworkVariableSerialization<T>)}.{nameof(UserNetworkVariableSerialization<T>.ReadValue)}, and {nameof(UserNetworkVariableSerialization<T>)}.{nameof(UserNetworkVariableSerialization<T>.DuplicateValue)}, or if it's serializable by memcpy (contains no pointers), wrap it in {typeof(ForceNetworkSerializeByMemcpy<>).Name}.");
            }
            UserNetworkVariableSerialization<T>.ReadValue(reader, out value);
        }

        void INetworkVariableSerializer<T>.ReadWithAllocator(FastBufferReader reader, out T value, Allocator allocator)
        {
            throw new NotImplementedException();
        }

        public void Duplicate(in T value, ref T duplicatedValue)
        {
            if (UserNetworkVariableSerialization<T>.ReadValue == null || UserNetworkVariableSerialization<T>.WriteValue == null || UserNetworkVariableSerialization<T>.DuplicateValue == null)
            {
                throw new ArgumentException($"Type {typeof(T).FullName} is not supported by {typeof(NetworkVariable<>).Name}. If this is a type you can change, then either implement {nameof(INetworkSerializable)} or mark it as serializable by memcpy by adding {nameof(INetworkSerializeByMemcpy)} to its interface list. If not, assign serialization code to {nameof(UserNetworkVariableSerialization<T>)}.{nameof(UserNetworkVariableSerialization<T>.WriteValue)}, {nameof(UserNetworkVariableSerialization<T>)}.{nameof(UserNetworkVariableSerialization<T>.ReadValue)}, and {nameof(UserNetworkVariableSerialization<T>)}.{nameof(UserNetworkVariableSerialization<T>.DuplicateValue)}, or if it's serializable by memcpy (contains no pointers), wrap it in {typeof(ForceNetworkSerializeByMemcpy<>).Name}.");
            }
            UserNetworkVariableSerialization<T>.DuplicateValue(value, ref duplicatedValue);
        }
    }

    /// <summary>
    /// This class contains initialization functions for various different types used in NetworkVariables.
    /// Generally speaking, these methods are called by a module initializer created by codegen (NetworkBehaviourILPP)
    /// and do not need to be called manually.
    ///
    /// There are two types of initializers: Serializers and EqualityCheckers. Every type must have an EqualityChecker
    /// registered to it in order to be used in NetworkVariable; however, not all types need a Serializer. Types without
    /// a serializer registered will fall back to using the delegates in <see cref="UserNetworkVariableSerialization{T}"/>.
    /// If no such delegate has been registered, a type without a serializer will throw an exception on the first attempt
    /// to serialize or deserialize it. (Again, however, codegen handles this automatically and this registration doesn't
    /// typically need to be performed manually.)
    /// </summary>
    public static class NetworkVariableSerializationTypes
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        internal static void InitializeIntegerSerialization()
        {
            NetworkVariableSerialization<short>.Serializer = new ShortSerializer();
            NetworkVariableSerialization<short>.AreEqual = NetworkVariableSerialization<short>.ValueEquals;
            NetworkVariableSerialization<ushort>.Serializer = new UshortSerializer();
            NetworkVariableSerialization<ushort>.AreEqual = NetworkVariableSerialization<ushort>.ValueEquals;
            NetworkVariableSerialization<int>.Serializer = new IntSerializer();
            NetworkVariableSerialization<int>.AreEqual = NetworkVariableSerialization<int>.ValueEquals;
            NetworkVariableSerialization<uint>.Serializer = new UintSerializer();
            NetworkVariableSerialization<uint>.AreEqual = NetworkVariableSerialization<uint>.ValueEquals;
            NetworkVariableSerialization<long>.Serializer = new LongSerializer();
            NetworkVariableSerialization<long>.AreEqual = NetworkVariableSerialization<long>.ValueEquals;
            NetworkVariableSerialization<ulong>.Serializer = new UlongSerializer();
            NetworkVariableSerialization<ulong>.AreEqual = NetworkVariableSerialization<ulong>.ValueEquals;
        }

        /// <summary>
        /// Registeres an unmanaged type that will be serialized by a direct memcpy into a buffer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeSerializer_UnmanagedByMemcpy<T>() where T : unmanaged
        {
            NetworkVariableSerialization<T>.Serializer = new UnmanagedTypeSerializer<T>();
        }

        /// <summary>
        /// Registeres an unmanaged type that will be serialized by a direct memcpy into a buffer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeSerializer_UnmanagedByMemcpyArray<T>() where T : unmanaged
        {
            NetworkVariableSerialization<NativeArray<T>>.Serializer = new UnmanagedArraySerializer<T>();
        }

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        /// <summary>
        /// Registeres an unmanaged type that will be serialized by a direct memcpy into a buffer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeSerializer_UnmanagedByMemcpyList<T>() where T : unmanaged
        {
            NetworkVariableSerialization<NativeList<T>>.Serializer = new UnmanagedListSerializer<T>();
        }
#endif

        /// <summary>
        /// Registers an unmanaged type that implements INetworkSerializable and will be serialized through a call to
        /// NetworkSerialize
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeSerializer_UnmanagedINetworkSerializable<T>() where T : unmanaged, INetworkSerializable
        {
            NetworkVariableSerialization<T>.Serializer = new UnmanagedNetworkSerializableSerializer<T>();
        }

        /// <summary>
        /// Registers an unmanaged type that implements INetworkSerializable and will be serialized through a call to
        /// NetworkSerialize
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeSerializer_UnmanagedINetworkSerializableArray<T>() where T : unmanaged, INetworkSerializable
        {
            NetworkVariableSerialization<NativeArray<T>>.Serializer = new UnmanagedNetworkSerializableArraySerializer<T>();
        }

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        /// <summary>
        /// Registers an unmanaged type that implements INetworkSerializable and will be serialized through a call to
        /// NetworkSerialize
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeSerializer_UnmanagedINetworkSerializableList<T>() where T : unmanaged, INetworkSerializable
        {
            NetworkVariableSerialization<NativeList<T>>.Serializer = new UnmanagedNetworkSerializableListSerializer<T>();
        }
#endif

        /// <summary>
        /// Registers a managed type that implements INetworkSerializable and will be serialized through a call to
        /// NetworkSerialize
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeSerializer_ManagedINetworkSerializable<T>() where T : class, INetworkSerializable, new()
        {
            NetworkVariableSerialization<T>.Serializer = new ManagedNetworkSerializableSerializer<T>();
        }

        /// <summary>
        /// Registers a FixedString type that will be serialized through FastBufferReader/FastBufferWriter's FixedString
        /// serializers
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeSerializer_FixedString<T>() where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            NetworkVariableSerialization<T>.Serializer = new FixedStringSerializer<T>();
        }

        /// <summary>
        /// Registers a FixedString type that will be serialized through FastBufferReader/FastBufferWriter's FixedString
        /// serializers
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeSerializer_FixedStringArray<T>() where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            NetworkVariableSerialization<NativeArray<T>>.Serializer = new FixedStringArraySerializer<T>();
        }

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        /// <summary>
        /// Registers a FixedString type that will be serialized through FastBufferReader/FastBufferWriter's FixedString
        /// serializers
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeSerializer_FixedStringList<T>() where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            NetworkVariableSerialization<NativeList<T>>.Serializer = new FixedStringListSerializer<T>();
        }
#endif

        /// <summary>
        /// Registers a managed type that will be checked for equality using T.Equals()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeEqualityChecker_ManagedIEquatable<T>() where T : class, IEquatable<T>
        {
            NetworkVariableSerialization<T>.AreEqual = NetworkVariableSerialization<T>.EqualityEqualsObject;
        }

        /// <summary>
        /// Registers an unmanaged type that will be checked for equality using T.Equals()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeEqualityChecker_UnmanagedIEquatable<T>() where T : unmanaged, IEquatable<T>
        {
            NetworkVariableSerialization<T>.AreEqual = NetworkVariableSerialization<T>.EqualityEquals;
        }

        /// <summary>
        /// Registers an unmanaged type that will be checked for equality using T.Equals()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeEqualityChecker_UnmanagedIEquatableArray<T>() where T : unmanaged, IEquatable<T>
        {
            NetworkVariableSerialization<NativeArray<T>>.AreEqual = NetworkVariableSerialization<T>.EqualityEqualsArray;
        }

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        /// <summary>
        /// Registers an unmanaged type that will be checked for equality using T.Equals()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeEqualityChecker_UnmanagedIEquatableList<T>() where T : unmanaged, IEquatable<T>
        {
            NetworkVariableSerialization<NativeList<T>>.AreEqual = NetworkVariableSerialization<T>.EqualityEqualsList;
        }
#endif

        /// <summary>
        /// Registers an unmanaged type that will be checked for equality using memcmp and only considered
        /// equal if they are bitwise equivalent in memory
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeEqualityChecker_UnmanagedValueEquals<T>() where T : unmanaged
        {
            NetworkVariableSerialization<T>.AreEqual = NetworkVariableSerialization<T>.ValueEquals;
        }

        /// <summary>
        /// Registers an unmanaged type that will be checked for equality using memcmp and only considered
        /// equal if they are bitwise equivalent in memory
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeEqualityChecker_UnmanagedValueEqualsArray<T>() where T : unmanaged
        {
            NetworkVariableSerialization<NativeArray<T>>.AreEqual = NetworkVariableSerialization<T>.ValueEqualsArray;
        }

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        /// <summary>
        /// Registers an unmanaged type that will be checked for equality using memcmp and only considered
        /// equal if they are bitwise equivalent in memory
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeEqualityChecker_UnmanagedValueEqualsList<T>() where T : unmanaged
        {
            NetworkVariableSerialization<NativeList<T>>.AreEqual = NetworkVariableSerialization<T>.ValueEqualsList;
        }
#endif

        /// <summary>
        /// Registers a managed type that will be checked for equality using the == operator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeEqualityChecker_ManagedClassEquals<T>() where T : class
        {
            NetworkVariableSerialization<T>.AreEqual = NetworkVariableSerialization<T>.ClassEquals;
        }
    }

    /// <summary>
    /// Support methods for reading/writing NetworkVariables
    /// Because there are multiple overloads of WriteValue/ReadValue based on different generic constraints,
    /// but there's no way to achieve the same thing with a class, this sets up various read/write schemes
    /// based on which constraints are met by `T` using reflection, which is done at module load time.
    /// </summary>
    /// <typeparam name="T">The type the associated NetworkVariable is templated on</typeparam>
    [Serializable]
    public static class NetworkVariableSerialization<T>
    {
        internal static INetworkVariableSerializer<T> Serializer = new FallbackSerializer<T>();

        internal delegate bool EqualsDelegate(ref T a, ref T b);
        internal static EqualsDelegate AreEqual;

        // Compares two values of the same unmanaged type by underlying memory
        // Ignoring any overridden value checks
        // Size is fixed
        internal static unsafe bool ValueEquals<TValueType>(ref TValueType a, ref TValueType b) where TValueType : unmanaged
        {
            // get unmanaged pointers
            var aptr = UnsafeUtility.AddressOf(ref a);
            var bptr = UnsafeUtility.AddressOf(ref b);

            // compare addresses
            return UnsafeUtility.MemCmp(aptr, bptr, sizeof(TValueType)) == 0;
        }

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        // Compares two values of the same unmanaged type by underlying memory
        // Ignoring any overridden value checks
        // Size is fixed
        internal static unsafe bool ValueEqualsList<TValueType>(ref NativeList<TValueType> a, ref NativeList<TValueType> b) where TValueType : unmanaged
        {
            if (a.IsCreated != b.IsCreated)
            {
                return false;
            }

            if (!a.IsCreated)
            {
                return true;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            var aptr = (TValueType*)a.GetUnsafePtr();
            var bptr = (TValueType*)b.GetUnsafePtr();
            return UnsafeUtility.MemCmp(aptr, bptr, sizeof(TValueType) * a.Length) == 0;
        }
#endif

        // Compares two values of the same unmanaged type by underlying memory
        // Ignoring any overridden value checks
        // Size is fixed
        internal static unsafe bool ValueEqualsArray<TValueType>(ref NativeArray<TValueType> a, ref NativeArray<TValueType> b) where TValueType : unmanaged
        {
            if (a.IsCreated != b.IsCreated)
            {
                return false;
            }

            if (!a.IsCreated)
            {
                return true;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            var aptr = (TValueType*)a.GetUnsafePtr();
            var bptr = (TValueType*)b.GetUnsafePtr();
            return UnsafeUtility.MemCmp(aptr, bptr, sizeof(TValueType) * a.Length) == 0;
        }

        internal static bool EqualityEqualsObject<TValueType>(ref TValueType a, ref TValueType b) where TValueType : class, IEquatable<TValueType>
        {
            if (a == null)
            {
                return b == null;
            }

            if (b == null)
            {
                return false;
            }

            return a.Equals(b);
        }

        internal static bool EqualityEquals<TValueType>(ref TValueType a, ref TValueType b) where TValueType : unmanaged, IEquatable<TValueType>
        {
            return a.Equals(b);
        }

#if UNITY_NETCODE_NATIVE_COLLECTION_SUPPORT
        // Compares two values of the same unmanaged type by underlying memory
        // Ignoring any overridden value checks
        // Size is fixed
        internal static unsafe bool EqualityEqualsList<TValueType>(ref NativeList<TValueType> a, ref NativeList<TValueType> b) where TValueType : unmanaged, IEquatable<TValueType>
        {
            if (a.IsCreated != b.IsCreated)
            {
                return false;
            }

            if (!a.IsCreated)
            {
                return true;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            var aptr = (TValueType*)a.GetUnsafePtr();
            var bptr = (TValueType*)b.GetUnsafePtr();
            for (var i = 0; i < a.Length; ++i)
            {
                if (!EqualityEquals(ref aptr[i], ref bptr[i]))
                {
                    return false;
                }
            }

            return true;
        }
#endif

        // Compares two values of the same unmanaged type by underlying memory
        // Ignoring any overridden value checks
        // Size is fixed
        internal static unsafe bool EqualityEqualsArray<TValueType>(ref NativeArray<TValueType> a, ref NativeArray<TValueType> b) where TValueType : unmanaged, IEquatable<TValueType>
        {
            if (a.IsCreated != b.IsCreated)
            {
                return false;
            }

            if (!a.IsCreated)
            {
                return true;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            var aptr = (TValueType*)a.GetUnsafePtr();
            var bptr = (TValueType*)b.GetUnsafePtr();
            for (var i = 0; i < a.Length; ++i)
            {
                if (!EqualityEquals(ref aptr[i], ref bptr[i]))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool ClassEquals<TValueType>(ref TValueType a, ref TValueType b) where TValueType : class
        {
            return a == b;
        }

        internal static void Write(FastBufferWriter writer, ref T value)
        {
            Serializer.Write(writer, ref value);
        }

        internal static void Read(FastBufferReader reader, ref T value)
        {
            Serializer.Read(reader, ref value);
        }
    }
}
