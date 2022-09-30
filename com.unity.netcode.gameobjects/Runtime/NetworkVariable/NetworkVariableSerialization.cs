using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

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
    }

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
    }

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
    }

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
        /// The <see cref="WriteValueDelegate"/> delegate handler declaration
        /// </summary>
        public static WriteValueDelegate WriteValue;

        /// <summary>
        /// The <see cref="ReadValueDelegate"/> delegate handler declaration
        /// </summary>
        public static ReadValueDelegate ReadValue;
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
            if (UserNetworkVariableSerialization<T>.ReadValue == null || UserNetworkVariableSerialization<T>.WriteValue == null)
            {
                throw new ArgumentException($"Type {typeof(T).FullName} is not supported by {typeof(NetworkVariable<>).Name}. If this is a type you can change, then either implement {nameof(INetworkSerializable)} or mark it as serializable by memcpy by adding {nameof(INetworkSerializeByMemcpy)} to its interface list. If not, assign serialization code to {nameof(UserNetworkVariableSerialization<T>)}.{nameof(UserNetworkVariableSerialization<T>.WriteValue)} and {nameof(UserNetworkVariableSerialization<T>)}.{nameof(UserNetworkVariableSerialization<T>.ReadValue)}, or if it's serializable by memcpy (contains no pointers), wrap it in {typeof(ForceNetworkSerializeByMemcpy<>).Name}.");
            }
            UserNetworkVariableSerialization<T>.WriteValue(writer, value);
        }
        public void Read(FastBufferReader reader, ref T value)
        {
            if (UserNetworkVariableSerialization<T>.ReadValue == null || UserNetworkVariableSerialization<T>.WriteValue == null)
            {
                throw new ArgumentException($"Type {typeof(T).FullName} is not supported by {typeof(NetworkVariable<>).Name}. If this is a type you can change, then either implement {nameof(INetworkSerializable)} or mark it as serializable by memcpy by adding {nameof(INetworkSerializeByMemcpy)} to its interface list. If not, assign serialization code to {nameof(UserNetworkVariableSerialization<T>)}.{nameof(UserNetworkVariableSerialization<T>.WriteValue)} and {nameof(UserNetworkVariableSerialization<T>)}.{nameof(UserNetworkVariableSerialization<T>.ReadValue)}, or if it's serializable by memcpy (contains no pointers), wrap it in {typeof(ForceNetworkSerializeByMemcpy<>).Name}.");
            }
            UserNetworkVariableSerialization<T>.ReadValue(reader, out value);
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
        /// <summary>
        /// Registeres an unmanaged type that will be serialized by a direct memcpy into a buffer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeSerializer_UnmanagedByMemcpy<T>() where T : unmanaged
        {
            NetworkVariableSerialization<T>.Serializer = new UnmanagedTypeSerializer<T>();
        }

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
        /// Registers an unmanaged type that will be checked for equality using memcmp and only considered
        /// equal if they are bitwise equivalent in memory
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void InitializeEqualityChecker_UnmanagedValueEquals<T>() where T : unmanaged
        {
            NetworkVariableSerialization<T>.AreEqual = NetworkVariableSerialization<T>.ValueEquals;
        }

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
