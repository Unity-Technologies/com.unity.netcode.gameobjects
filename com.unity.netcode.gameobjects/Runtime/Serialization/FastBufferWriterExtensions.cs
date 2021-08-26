
using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Netcode
{
    public static class FastBufferWriterExtensions
    {
        
        /// <summary>
        /// Writes a boxed object in a standard format
        /// Named differently from other WriteValue methods to avoid accidental boxing
        /// </summary>
        /// <param name="value">The object to write</param>
        /// <param name="isNullable">
        /// If true, an extra byte will be written to indicate whether or not the value is null.
        /// Some types will always write this.
        /// </param>
        public static void WriteObject(this ref FastBufferWriter writer, object value, bool isNullable = false)
        {
            if (isNullable || value.GetType().IsNullable())
            {
                bool isNull = value == null || (value is UnityEngine.Object o && o == null);

                writer.WriteValueSafe(isNull);

                if (isNull)
                {
                    return;
                }
            }

            var type = value.GetType();
            var hasSerializer = SerializationTypeTable.Serializers.TryGetValue(type, out var serializer);
            if (hasSerializer)
            {
                serializer(ref writer, value);
                return;
            }

            if (value is Array array)
            {
                writer.WriteValueSafe(array.Length);

                for (int i = 0; i < array.Length; i++)
                {
                    writer.WriteObject(array.GetValue(i));
                }

                return;
            }

            if (value.GetType().IsEnum)
            {
                switch (Convert.GetTypeCode(value))
                {
                    case TypeCode.Boolean:
                        writer.WriteValueSafe((byte)value);
                        break;
                    case TypeCode.Char:
                        writer.WriteValueSafe((char)value);
                        break;
                    case TypeCode.SByte:
                        writer.WriteValueSafe((sbyte)value);
                        break;
                    case TypeCode.Byte:
                        writer.WriteValueSafe((byte)value);
                        break;
                    case TypeCode.Int16:
                        writer.WriteValueSafe((short)value);
                        break;
                    case TypeCode.UInt16:
                        writer.WriteValueSafe((ushort)value);
                        break;
                    case TypeCode.Int32:
                        writer.WriteValueSafe((int)value);
                        break;
                    case TypeCode.UInt32:
                        writer.WriteValueSafe((uint)value);
                        break;
                    case TypeCode.Int64:
                        writer.WriteValueSafe((long)value);
                        break;
                    case TypeCode.UInt64:
                        writer.WriteValueSafe((ulong)value);
                        break;
                }
                return;
            }
            if (value is GameObject)
            {
                writer.WriteValueSafe((GameObject)value);
                return;
            }
            if (value is NetworkObject)
            {
                writer.WriteValueSafe((NetworkObject)value);
                return;
            }
            if (value is NetworkBehaviour)
            {
                writer.WriteValueSafe((NetworkBehaviour)value);
                return;
            }
            if (value is INetworkSerializable)
            {
                //TODO ((INetworkSerializable)value).NetworkSerialize(new NetworkSerializer(this));
                return;
            }

            throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write type {value.GetType().Name} - it does not implement {nameof(INetworkSerializable)}");
        }

        /// <summary>
        /// Write an INetworkSerializable
        /// </summary>
        /// <param name="value">The value to write</param>
        /// <typeparam name="T"></typeparam>
        public static void WriteNetworkSerializable<T>(this ref FastBufferWriter writer, in T value) where T : INetworkSerializable
        {
            // TODO
        }

        /// <summary>
        /// Get the required amount of space to write a GameObject
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int GetWriteSize(GameObject value)
        {
            return sizeof(ulong);
        }

        /// <summary>
        /// Get the required amount of space to write a GameObject
        /// </summary>
        /// <returns></returns>
        public static int GetGameObjectWriteSize()
        {
            return sizeof(ulong);
        }

        /// <summary>
        /// Write a GameObject
        /// </summary>
        /// <param name="value">The value to write</param>
        public static void WriteValue(this ref FastBufferWriter writer, GameObject value)
        {
            value.TryGetComponent<NetworkObject>(out var networkObject);
            if (networkObject == null)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(GameObject)} types that does not has a {nameof(NetworkObject)} component attached. {nameof(GameObject)}: {(value).name}");
            }

            if (!networkObject.IsSpawned)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {(value).name}");
            }

            writer.WriteValue(networkObject.NetworkObjectId);
        }

        /// <summary>
        /// Write a GameObject
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling TryBeginWrite.
        /// </summary>
        /// <param name="value">The value to write</param>
        public static void WriteValueSafe(this ref FastBufferWriter writer, GameObject value)
        {
            value.TryGetComponent<NetworkObject>(out var networkObject);
            if (networkObject == null)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(GameObject)} types that does not has a {nameof(NetworkObject)} component attached. {nameof(GameObject)}: {(value).name}");
            }

            if (!networkObject.IsSpawned)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {(value).name}");
            }

            writer.WriteValueSafe(networkObject.NetworkObjectId);
        }

        /// <summary>
        /// Get the required size to write a NetworkObject
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int GetWriteSize(NetworkObject value)
        {
            return sizeof(ulong);
        }

        /// <summary>
        /// Get the required size to write a NetworkObject
        /// </summary>
        /// <returns></returns>
        public static int GetNetworkObjectWriteSize()
        {
            return sizeof(ulong);
        }


        /// <summary>
        /// Write a NetworkObject
        /// </summary>
        /// <param name="value">The value to write</param>
        public static void WriteValue(this ref FastBufferWriter writer, in NetworkObject value)
        {
            if (!value.IsSpawned)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {value.name}");
            }

            writer.WriteValue(value.NetworkObjectId);
        }

        /// <summary>
        /// Write a NetworkObject
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling TryBeginWrite.
        /// </summary>
        /// <param name="value">The value to write</param>
        public static void WriteValueSafe(this ref FastBufferWriter writer, NetworkObject value)
        {
            if (!value.IsSpawned)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {value.name}");
            }
            writer.WriteValueSafe(value.NetworkObjectId);
        }

        /// <summary>
        /// Get the required size to write a NetworkBehaviour
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int GetWriteSize(NetworkBehaviour value)
        {
            return sizeof(ulong) + sizeof(ushort);
        }


        /// <summary>
        /// Get the required size to write a NetworkBehaviour
        /// </summary>
        /// <returns></returns>
        public static int GetNetworkBehaviourWriteSize()
        {
            return sizeof(ulong) + sizeof(ushort);
        }


        /// <summary>
        /// Write a NetworkBehaviour
        /// </summary>
        /// <param name="value">The value to write</param>
        public static void WriteValue(this ref FastBufferWriter writer, NetworkBehaviour value)
        {
            if (!value.HasNetworkObject || !value.NetworkObject.IsSpawned)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(NetworkBehaviour)} types that are not spawned. {nameof(GameObject)}: {(value).gameObject.name}");
            }

            writer.WriteValue(value.NetworkObjectId);
            writer.WriteValue(value.NetworkBehaviourId);
        }

        /// <summary>
        /// Write a NetworkBehaviour
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling TryBeginWrite.
        /// </summary>
        /// <param name="value">The value to write</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="OverflowException"></exception>
        public static void WriteValueSafe(this ref FastBufferWriter writer, NetworkBehaviour value)
        {
            if (!value.HasNetworkObject || !value.NetworkObject.IsSpawned)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(NetworkBehaviour)} types that are not spawned. {nameof(GameObject)}: {(value).gameObject.name}");
            }

            if (!writer.TryBeginWriteInternal(sizeof(ulong) + sizeof(ushort)))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            writer.WriteValue(value.NetworkObjectId);
            writer.WriteValue(value.NetworkBehaviourId);
        }

    }
}