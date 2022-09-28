using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
    }

    /// <summary>
    /// Basic serializer for unmanaged types.
    /// This covers primitives, built-in unity types, and IForceSerializeByMemcpy
    /// Since all of those ultimately end up calling WriteUnmanagedSafe, this simplifies things
    /// by calling that directly - thus preventing us from having to have a specific T that meets
    /// the specific constraints that the various generic WriteValue calls require.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class UnmanagedTypeSerializer<T> : INetworkVariableSerializer<T> where T: unmanaged
    {
        internal delegate void WriteValueDelegate(FastBufferWriter writer, ref T value);
        internal delegate void ReadValueDelegate(FastBufferReader reader, ref T value);

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
    /// Serializer for FixedStrings, which does the same thing FastBufferWriter/FastBufferReader do,
    /// but is implemented to get the data it needs using open instance delegates that are passed in
    /// via reflection. This prevents needing T to meet any interface requirements (which isn't achievable
    /// without incurring GC allocs on every call to Write or Read - reflection + Open Instance Delegates
    /// circumvent that.)
    ///
    /// Tests show that calling these delegates doesn't cause any GC allocations even though they're
    /// obtained via reflection and Delegate.CreateDelegate() and called on types that, at compile time,
    /// aren't known to actually contain those methods.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class FixedStringSerializer<T> : INetworkVariableSerializer<T>
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
        public unsafe void Read(FastBufferReader reader, ref T value)
        {
            reader.ReadValueSafe(out int length);
            SetLength(ref value, length);
            reader.ReadBytesSafe(GetUnsafePtr(ref value), length);
        }
    }

    /// <summary>
    /// Serializer for INetworkSerializable types, which does the same thing
    /// FastBufferWriter/FastBufferReader do, but is implemented to call the NetworkSerialize() method
    /// via open instance delegates passed in via reflection. This prevents needing T to meet any interface
    /// requirements (which isn't achievable without incurring GC allocs on every call to Write or Read -
    /// reflection + Open Instance Delegates circumvent that.)
    ///
    /// Tests show that calling these delegates doesn't cause any GC allocations even though they're
    /// obtained via reflection and Delegate.CreateDelegate() and called on types that, at compile time,
    /// aren't known to actually contain those methods.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class UnmanagedNetworkSerializableSerializer<T> : INetworkVariableSerializer<T> where T: unmanaged
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
        public void Read(FastBufferReader reader, ref T value)
        {
            var bufferSerializer = new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
            ReadValue(ref value, bufferSerializer);
        }
    }

    internal class ManagedNetworkSerializableSerializer<T> : INetworkVariableSerializer<T> where T : new()
    {
        internal static void WriteInternalObject<TValueType>(ref TValueType value, BufferSerializer<BufferSerializerWriter> serializer) where TValueType : class, INetworkSerializable
        {
            bool isNull = (value == null);
            serializer.SerializeValue(ref isNull);
            if (!isNull)
            {
                value.NetworkSerialize(serializer);
            }
        }
        internal static void ReadInternalObject<TValueType>(ref TValueType value, BufferSerializer<BufferSerializerReader> serializer) where TValueType : class, INetworkSerializable, new()
        {
            bool isNull = false;
            serializer.SerializeValue(ref isNull);
            if (isNull)
            {
                value = null;
            }
            else
            {
                if (value == null)
                {
                    value = new TValueType();
                }
                value.NetworkSerialize(serializer);
            }
        }

        internal delegate void WriteValueDelegate(ref T value, BufferSerializer<BufferSerializerWriter> serializer);
        internal delegate void ReadValueDelegate(ref T value, BufferSerializer<BufferSerializerReader> serializer);

        internal WriteValueDelegate WriteValue;
        internal ReadValueDelegate ReadValue;
        public void Write(FastBufferWriter writer, ref T value)
        {
            var bufferSerializer = new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
            WriteValue(ref value, bufferSerializer);
        }
        public void Read(FastBufferReader reader, ref T value)
        {
            var bufferSerializer = new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
            ReadValue(ref value, bufferSerializer);
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

    public static class NetworkVariableSerializationTypes
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
    /// but there's no way to achieve the same thing with a class, this sets up various read/write schemes
    /// based on which constraints are met by `T` using reflection, which is done at module load time.
    /// </summary>
    /// <typeparam name="T">The type the associated NetworkVariable is templated on</typeparam>
    [Serializable]
    public static class NetworkVariableSerialization<T> where T: unmanaged
    {
        private static INetworkVariableSerializer<T> s_Serializer = GetSerializer();

        private static INetworkVariableSerializer<T> GetSerializer()
        {
            if (NetworkVariableSerializationTypes.BaseSupportedTypes.Contains(typeof(T)) || typeof(INetworkSerializeByMemcpy).IsAssignableFrom(typeof(T)) || typeof(Enum).IsAssignableFrom(typeof(T)))
            {
                return new UnmanagedTypeSerializer<T>();
            }

            if (typeof(INetworkSerializable).IsAssignableFrom(typeof(T)))
            {
                // Obtains "Open Instance Delegates" for the type's NetworkSerialize() methods -
                // one for an instance of the generic method taking BufferSerializerWriter as T,
                // one for an instance of the generic method taking BufferSerializerReader as T
                var writeMethod = (UnmanagedNetworkSerializableSerializer<T>.WriteValueDelegate)Delegate.CreateDelegate(typeof(UnmanagedNetworkSerializableSerializer<T>.WriteValueDelegate), null, typeof(T).GetMethod(nameof(INetworkSerializable.NetworkSerialize)).MakeGenericMethod(typeof(BufferSerializerWriter)));
                var readMethod = (UnmanagedNetworkSerializableSerializer<T>.ReadValueDelegate)Delegate.CreateDelegate(typeof(UnmanagedNetworkSerializableSerializer<T>.ReadValueDelegate), null, typeof(T).GetMethod(nameof(INetworkSerializable.NetworkSerialize)).MakeGenericMethod(typeof(BufferSerializerReader)));
                return new UnmanagedNetworkSerializableSerializer<T> { WriteValue = writeMethod, ReadValue = readMethod };
            }

            if (typeof(IUTF8Bytes).IsAssignableFrom(typeof(T)) && typeof(INativeList<byte>).IsAssignableFrom(typeof(T)))
            {
                // Get "OpenInstanceDelegates" for the Length property (get and set, which are prefixed
                // with "get_" and "set_" under the hood and emitted as methods) and GetUnsafePtr()
                var getLength = (FixedStringSerializer<T>.GetLengthDelegate)Delegate.CreateDelegate(typeof(FixedStringSerializer<T>.GetLengthDelegate), null, typeof(T).GetMethod("get_" + nameof(INativeList<byte>.Length)));
                var setLength = (FixedStringSerializer<T>.SetLengthDelegate)Delegate.CreateDelegate(typeof(FixedStringSerializer<T>.SetLengthDelegate), null, typeof(T).GetMethod("set_" + nameof(INativeList<byte>.Length)));
                var getUnsafePtr = (FixedStringSerializer<T>.GetUnsafePtrDelegate)Delegate.CreateDelegate(typeof(FixedStringSerializer<T>.GetUnsafePtrDelegate), null, typeof(T).GetMethod(nameof(IUTF8Bytes.GetUnsafePtr)));
                return new FixedStringSerializer<T> { GetLength = getLength, SetLength = setLength, GetUnsafePtr = getUnsafePtr };
            }

            return new FallbackSerializer<T>();
        }

        // Compares two values of the same unmanaged type by underlying memory
        // Ignoring any overridden value checks
        // Size is fixed
        private static unsafe bool ValueEquals(ref T a, ref T b)
        {
            // get unmanaged pointers
            var aptr = UnsafeUtility.AddressOf(ref a);
            var bptr = UnsafeUtility.AddressOf(ref b);

            // compare addresses
            return UnsafeUtility.MemCmp(aptr, bptr, sizeof(T)) == 0;
        }

        // Compares two values of the same unmanaged type by underlying memory
        // Ignoring any overridden value checks
        // Size is fixed
        internal static bool Equals(ref T a, ref T b)
        {
            return ValueEquals(ref a, ref b);
        }

        internal static void Write(FastBufferWriter writer, ref T value)
        {
            s_Serializer.Write(writer, ref value);
        }

        internal static void Read(FastBufferReader reader, ref T value)
        {
            s_Serializer.Read(reader, ref value);
        }
    }

    public static class ManagedNetworkVariableSerialization<T> where T : new()
    {
        private static INetworkVariableSerializer<T> s_Serializer = GetSerializer();

        private static INetworkVariableSerializer<T> GetSerializer()
        {
            if (typeof(INetworkSerializable).IsAssignableFrom(typeof(T)))
            {
                var genericWriter = typeof(ManagedNetworkSerializableSerializer<T>).GetMethod(nameof(ManagedNetworkSerializableSerializer<T>.WriteInternalObject), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(typeof(T));
                var writeMethod = (ManagedNetworkSerializableSerializer<T>.WriteValueDelegate)Delegate.CreateDelegate(typeof(ManagedNetworkSerializableSerializer<T>.WriteValueDelegate), null, genericWriter);
                var genericReader = typeof(ManagedNetworkSerializableSerializer<T>).GetMethod(nameof(ManagedNetworkSerializableSerializer<T>.ReadInternalObject), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(typeof(T));
                var readMethod = (ManagedNetworkSerializableSerializer<T>.ReadValueDelegate)Delegate.CreateDelegate(typeof(ManagedNetworkSerializableSerializer<T>.ReadValueDelegate), null, genericReader);
                return new ManagedNetworkSerializableSerializer<T> { WriteValue = writeMethod, ReadValue = readMethod };
            }

            return new FallbackSerializer<T>();
        }

        // Compares two values of the same unmanaged type by underlying memory
        // Ignoring any overridden value checks
        // Size is fixed
        internal static bool Equals(ref T a, ref T b)
        {
            if (a == null)
            {
                return b == null;
            }

            if (b == null)
            {
                return false;
            }

            if (a is IEquatable<T> equatable)
            {
                return equatable.Equals(b as IEquatable<T>);
            }

            return (object)a == (object)b;
        }

        internal static void Write(FastBufferWriter writer, ref T value)
        {
            s_Serializer.Write(writer, ref value);
        }

        internal static void Read(FastBufferReader reader, ref T value)
        {
            s_Serializer.Read(reader, ref value);
        }
    }
}
