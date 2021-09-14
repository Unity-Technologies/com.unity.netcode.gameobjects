
using System;
using UnityEngine;

namespace Unity.Netcode
{
    public static class FastBufferReaderExtensions
    {

        /// <summary>
        /// Reads a boxed object in a standard format
        /// Named differently from other ReadValue methods to avoid accidental boxing
        /// </summary>
        /// <param name="value">The object to read</param>
        /// <param name="type">The type to be read</param>
        /// <param name="isNullable">
        /// If true, reads a byte indicating whether or not the object is null.
        /// Should match the way the object was written.
        /// </param>
        public static void ReadObject(this ref FastBufferReader reader, out object value, Type type, bool isNullable = false)
        {
            if (isNullable || type.IsNullable())
            {
                reader.ReadValueSafe(out bool isNull);

                if (isNull)
                {
                    value = null;
                    return;
                }
            }

            var hasDeserializer = SerializationTypeTable.Deserializers.TryGetValue(type, out var deserializer);
            if (hasDeserializer)
            {
                deserializer(ref reader, out value);
                return;
            }

            if (type.IsArray && type.HasElementType)
            {
                reader.ReadValueSafe(out int length);

                var arr = Array.CreateInstance(type.GetElementType(), length);

                for (int i = 0; i < length; i++)
                {
                    reader.ReadObject(out object item, type.GetElementType());
                    arr.SetValue(item, i);
                }

                value = arr;
                return;
            }

            if (type.IsEnum)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        reader.ReadValueSafe(out byte boolVal);
                        value = Enum.ToObject(type, boolVal != 0);
                        return;
                    case TypeCode.Char:
                        reader.ReadValueSafe(out char charVal);
                        value = Enum.ToObject(type, charVal);
                        return;
                    case TypeCode.SByte:
                        reader.ReadValueSafe(out sbyte sbyteVal);
                        value = Enum.ToObject(type, sbyteVal);
                        return;
                    case TypeCode.Byte:
                        reader.ReadValueSafe(out byte byteVal);
                        value = Enum.ToObject(type, byteVal);
                        return;
                    case TypeCode.Int16:
                        reader.ReadValueSafe(out short shortVal);
                        value = Enum.ToObject(type, shortVal);
                        return;
                    case TypeCode.UInt16:
                        reader.ReadValueSafe(out ushort ushortVal);
                        value = Enum.ToObject(type, ushortVal);
                        return;
                    case TypeCode.Int32:
                        reader.ReadValueSafe(out int intVal);
                        value = Enum.ToObject(type, intVal);
                        return;
                    case TypeCode.UInt32:
                        reader.ReadValueSafe(out uint uintVal);
                        value = Enum.ToObject(type, uintVal);
                        return;
                    case TypeCode.Int64:
                        reader.ReadValueSafe(out long longVal);
                        value = Enum.ToObject(type, longVal);
                        return;
                    case TypeCode.UInt64:
                        reader.ReadValueSafe(out ulong ulongVal);
                        value = Enum.ToObject(type, ulongVal);
                        return;
                }
            }

            if (type == typeof(GameObject))
            {
                reader.ReadValueSafe(out GameObject go);
                value = go;
                return;
            }

            if (type == typeof(NetworkObject))
            {
                reader.ReadValueSafe(out NetworkObject no);
                value = no;
                return;
            }

            if (typeof(NetworkBehaviour).IsAssignableFrom(type))
            {
                reader.ReadValueSafe(out NetworkBehaviour nb);
                value = nb;
                return;
            }
            /*if (value is INetworkSerializable)
            {
                //TODO ((INetworkSerializable)value).NetworkSerialize(new NetworkSerializer(this));
                return;
            }*/

            throw new ArgumentException($"{nameof(FastBufferReader)} cannot read type {type.Name} - it does not implement {nameof(INetworkSerializable)}");
        }

        /// <summary>
        /// Read a GameObject
        /// </summary>
        /// <param name="value">value to read</param>
        public static void ReadValue(this ref FastBufferReader reader, out GameObject value)
        {
            reader.ReadValue(out ulong networkObjectId);

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                value = networkObject.gameObject;
                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                NetworkLog.LogWarning($"{nameof(FastBufferReader)} cannot find the {nameof(GameObject)} sent in the {nameof(NetworkSpawnManager.SpawnedObjects)} list, it may have been destroyed. {nameof(networkObjectId)}: {networkObjectId}");
            }

            value = null;
        }

        /// <summary>
        /// Read a GameObject
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling TryBeginRead.
        /// </summary>
        /// <param name="value">value to read</param>
        public static void ReadValueSafe(this ref FastBufferReader reader, out GameObject value)
        {
            reader.ReadValueSafe(out ulong networkObjectId);

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                value = networkObject.gameObject;
                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                NetworkLog.LogWarning($"{nameof(FastBufferReader)} cannot find the {nameof(GameObject)} sent in the {nameof(NetworkSpawnManager.SpawnedObjects)} list, it may have been destroyed. {nameof(networkObjectId)}: {networkObjectId}");
            }

            value = null;
        }

        /// <summary>
        /// Read an array of GameObjects
        /// </summary>
        /// <param name="value">value to read</param>
        public static void ReadValueSafe(this ref FastBufferReader reader, out GameObject[] value)
        {
            reader.ReadValueSafe(out int size);
            value = new GameObject[size];
            for (var i = 0; i < size; ++i)
            {
                reader.ReadValueSafe(out value[i]);
            }
        }

        /// <summary>
        /// Read a NetworkObject
        /// </summary>
        /// <param name="value">value to read</param>
        public static void ReadValue(this ref FastBufferReader reader, out NetworkObject value)
        {
            reader.ReadValue(out ulong networkObjectId);

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                value = networkObject;
                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                NetworkLog.LogWarning($"{nameof(FastBufferReader)} cannot find the {nameof(GameObject)} sent in the {nameof(NetworkSpawnManager.SpawnedObjects)} list, it may have been destroyed. {nameof(networkObjectId)}: {networkObjectId}");
            }

            value = null;
        }

        /// <summary>
        /// Read a NetworkObject
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling TryBeginRead.
        /// </summary>
        /// <param name="value">value to read</param>
        public static void ReadValueSafe(this ref FastBufferReader reader, out NetworkObject value)
        {
            reader.ReadValueSafe(out ulong networkObjectId);

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                value = networkObject;
                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                NetworkLog.LogWarning($"{nameof(FastBufferReader)} cannot find the {nameof(GameObject)} sent in the {nameof(NetworkSpawnManager.SpawnedObjects)} list, it may have been destroyed. {nameof(networkObjectId)}: {networkObjectId}");
            }

            value = null;
        }

        /// <summary>
        /// Read an array of NetworkObjects
        /// </summary>
        /// <param name="value">value to read</param>
        public static void ReadValueSafe(this ref FastBufferReader reader, out NetworkObject[] value)
        {
            reader.ReadValueSafe(out int size);
            value = new NetworkObject[size];
            for (var i = 0; i < size; ++i)
            {
                reader.ReadValueSafe(out value[i]);
            }
        }

        /// <summary>
        /// Read a NetworkBehaviour
        /// </summary>
        /// <param name="value">value to read</param>
        public static void ReadValue(this ref FastBufferReader reader, out NetworkBehaviour value)
        {
            reader.ReadValue(out ulong networkObjectId);
            reader.ReadValue(out ushort networkBehaviourId);

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                value = networkObject.GetNetworkBehaviourAtOrderIndex(networkBehaviourId);
                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                NetworkLog.LogWarning($"{nameof(FastBufferReader)} cannot find the {nameof(NetworkBehaviour)} sent in the {nameof(NetworkSpawnManager.SpawnedObjects)} list, it may have been destroyed. {nameof(networkObjectId)}: {networkObjectId}");
            }

            value = null;
        }

        /// <summary>
        /// Read a NetworkBehaviour
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple reads at once by calling TryBeginRead.
        /// </summary>
        /// <param name="value">value to read</param>
        public static void ReadValueSafe(this ref FastBufferReader reader, out NetworkBehaviour value)
        {
            reader.ReadValueSafe(out ulong networkObjectId);
            reader.ReadValueSafe(out ushort networkBehaviourId);

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                value = networkObject.GetNetworkBehaviourAtOrderIndex(networkBehaviourId);
                return;
            }

            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                NetworkLog.LogWarning($"{nameof(FastBufferReader)} cannot find the {nameof(NetworkBehaviour)} sent in the {nameof(NetworkSpawnManager.SpawnedObjects)} list, it may have been destroyed. {nameof(networkObjectId)}: {networkObjectId}");
            }

            value = null;
        }

        /// <summary>
        /// Read an array of NetworkBehaviours
        /// </summary>
        /// <param name="value">value to read</param>
        public static void ReadValueSafe(this ref FastBufferReader reader, out NetworkBehaviour[] value)
        {
            reader.ReadValueSafe(out int size);
            value = new NetworkBehaviour[size];
            for (var i = 0; i < size; ++i)
            {
                reader.ReadValueSafe(out value[i]);
            }
        }
    }
}
